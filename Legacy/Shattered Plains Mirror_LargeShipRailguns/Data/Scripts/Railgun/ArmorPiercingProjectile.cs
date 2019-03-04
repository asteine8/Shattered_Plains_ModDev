using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Weapons;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.Game;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Definitions;
using Whiplash.Railgun;
using VRage.Collections;
using VRage.Voxels;

namespace Whiplash.ArmorPiercingProjectiles
{
    public class ArmorPiercingProjectile
    {
        const float _tick = 1f / 60f;
        const int _maxTracerFadeTicks = 180;
        const float _trailColorDecayRatio = 0.97f;

        Vector3D _origin;
        Vector3D _lastPositionChecked;
        Vector3D _position;
        Vector3D _velocity;
        Vector3D _lastVelocity;
        Vector3D _direction;
        Vector3D _hitPosition;
        readonly float _explosionDamage;
        readonly float _explosionRadius;
        readonly float _projectileSpeed;
        readonly float _maxTrajectory;
        readonly float _minimumArmDistance = 0f;
        readonly float _penetrationRange;
        readonly float _penetrationDamage;
        readonly float _deviationAngle;
        int _checkIntersectionIndex = 4;
        public bool Killed = false;
        bool _positionChecked = false;
        readonly bool _shouldExplode;
        readonly bool _shouldPenetrate;
        readonly bool _drawTrail;
        readonly bool _drawTracer;
        bool _targetHit = false;
        bool _penetratedObjectsSorted = false;
        bool _penetratedObjectsDamaged = false;
        int _currentTracerFadeTicks = 0;
        readonly long _gunEntityID;
        Vector4 _lineColor;
        readonly MyStringId _material = MyStringId.GetOrCompute("WeaponLaser");
        readonly MyStringId _bulletMaterial = MyStringId.GetOrCompute("ProjectileTrailLine");
        readonly Vector3 _tracerColor;
        readonly float _tracerScale;
        readonly List<PenetratedEntityContainer> _objectsToPenetrate = new List<PenetratedEntityContainer>();
        readonly List<MyLineSegmentOverlapResult<MyEntity>> _overlappingEntities = new List<MyLineSegmentOverlapResult<MyEntity>>();
        readonly List<Vector3I> _hitPositions = new List<Vector3I>();
        readonly List<MyLineSegmentOverlapResult<MyVoxelBase>> _voxelOverlap = new List<MyLineSegmentOverlapResult<MyVoxelBase>>();
        Vector3D? _cachedSurfacePoint = null;

        readonly List<Vector3D> _trajectoryPoints = new List<Vector3D>();
        readonly List<RailgunTracerData> _tracerData = new List<RailgunTracerData>();

        public ArmorPiercingProjectile(RailgunFireData fireData, RailgunProjectileData projectileData)
        {
            // Weapon data
            _tracerColor = projectileData.ProjectileTrailColor;
            _lineColor = new Vector4(_tracerColor, 1f);
            _tracerScale = projectileData.ProjectileTrailScale;
            _maxTrajectory = projectileData.MaxTrajectory;
            _projectileSpeed = projectileData.DesiredSpeed;
            _deviationAngle = projectileData.DeviationAngle;
            _gunEntityID = projectileData.ShooterID;

            // Config data
            _drawTrail = RailgunCore.MyConfig.DrawProjectileTrails;
            _explosionDamage = RailgunCore.MyConfig.ExplosionDamage;
            _explosionRadius = RailgunCore.MyConfig.ExplosionRadius;
            _penetrationDamage = RailgunCore.MyConfig.PenetrationDamage;
            _penetrationRange = RailgunCore.MyConfig.PenetrationRange;
            _shouldExplode = RailgunCore.MyConfig.ShouldExplode;
            _shouldPenetrate = RailgunCore.MyConfig.ShouldPenetrate;

            // Fire data
            var temp = fireData.Direction;
            _direction = Vector3D.IsUnit(ref temp) ? temp : Vector3D.Normalize(temp);
            _direction = GetDeviatedVector(_direction, _deviationAngle);
            _origin = fireData.Origin;
            _position = _origin;
            _velocity = fireData.ShooterVelocity + _direction * _projectileSpeed;
            _lastVelocity = _velocity;
            _lastPositionChecked = _origin;

            _trajectoryPoints.Add(_position);
        }

        public static Vector3 GetDeviatedVector(Vector3 direction, float deviationAngle)
        {
            float elevationAngle = MyUtils.GetRandomFloat(-deviationAngle, deviationAngle);
            float rotationAngle = MyUtils.GetRandomFloat(0f, MathHelper.TwoPi);
            Vector3 normal = -new Vector3(MyMath.FastSin(elevationAngle) * MyMath.FastCos(rotationAngle), MyMath.FastSin(elevationAngle) * MyMath.FastSin(rotationAngle), MyMath.FastCos(elevationAngle));
            var mat = Matrix.CreateFromDir(direction);
            return Vector3.TransformNormal(normal, mat);
        }

        public void Update()
        {
            if (_targetHit)
            {
                Kill();
                return;
            }

            // Update velocity due to gravity
            Vector3D totalGravity = MyParticlesManager.CalculateGravityInPoint(_position); // Does this get affected by artificial grav? If so... cooooool
            Vector3D naturalGravity = RailgunCore.GetNaturalGravityAtPoint(_position);
            Vector3D artificialGravity = totalGravity - naturalGravity;
            _velocity += (naturalGravity * RailgunCore.MyConfig.NaturalGravityMultiplier + artificialGravity * RailgunCore.MyConfig.ArtificialGravityMultiplier) * _tick;

            // Update direction if velocity has changed
            if (!_velocity.Equals(_lastVelocity, 1e-3))
                _direction = Vector3D.Normalize(_velocity);

            _lastVelocity = _velocity;

            // Update position
            _position += _velocity * _tick;
            var _toOrigin = _position - _origin;

            //draw tracer line
            if (_drawTrail && _currentTracerFadeTicks < _maxTracerFadeTicks)
            {
                _lineColor *= _trailColorDecayRatio;
                _currentTracerFadeTicks++;
            }

            if (_toOrigin.LengthSquared() > _maxTrajectory * _maxTrajectory)
            {
                MyLog.Default.WriteLine(">> Max range hit");

                _targetHit = true;
                _hitPosition = _position;
                Kill();
                if (_shouldExplode)
                    CreateExplosion(_position, _direction, _explosionRadius, _explosionDamage);
                return;
            }

            _checkIntersectionIndex = ++_checkIntersectionIndex % 5;
            if (_checkIntersectionIndex != 0 && _positionChecked)
            {
                return;
            }

            // Add current position to trajectory list
            _trajectoryPoints.Add(_position);

            var to = _position; //_position + 5.0 * _velocity * _tick;
            var from = _lastPositionChecked;
            _positionChecked = true;
            _lastPositionChecked = _position;

            IHitInfo hitInfo;
            bool hit = false;
            if (Vector3D.DistanceSquared(to, from) > 50 * 50)
            {
                // Use faster raycast if ray is long enough
                hit = MyAPIGateway.Physics.CastLongRay(from, to, out hitInfo, true);
            }
            else
            {
                hit = MyAPIGateway.Physics.CastRay(from, to, out hitInfo, 0);
            }

            if (hit)
            {
                MyLog.Default.WriteLine(">> Raycast hit");
                _hitPosition = hitInfo.Position + -0.5 * _direction;
                if ((_hitPosition - _origin).LengthSquared() > _minimumArmDistance * _minimumArmDistance) //only explode if beyond arm distance
                {
                    if (_shouldExplode)
                        CreateExplosion(_hitPosition, _direction, _explosionRadius, _explosionDamage);

                    if (_shouldPenetrate)
                        GetObjectsToPenetrate(_hitPosition, _hitPosition + _direction * _penetrationRange);

                    _targetHit = true;
                    Kill();
                }
                else
                {
                    _targetHit = true;
                    _hitPosition = _position;
                    Kill();
                }
                return;
            }

            // implied else
            var line = new LineD(from, to);
            MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref line, _voxelOverlap);
            foreach (var result in _voxelOverlap)
            {
                MatrixD matrix;
                MatrixD matrixInv;
                Vector3 sizeInMetersHalf;

                MyPlanet planet = result.Element as MyPlanet;
                IMyVoxelMap voxelMap = result.Element as IMyVoxelMap;
                if (planet == null && voxelMap == null)
                    continue;

                if (planet != null)
                {
                    matrix = planet.WorldMatrix;
                    matrixInv = planet.PositionComp.WorldMatrixInvScaled;
                    sizeInMetersHalf = planet.SizeInMetresHalf;
                }
                else
                {
                    matrix = voxelMap.WorldMatrix;
                    matrixInv = voxelMap.PositionComp.WorldMatrixInvScaled;
                    sizeInMetersHalf = new Vector3(voxelMap.Storage.Size) * 0.5f;
                }

                Vector3 localTo;
                Vector3 localFrom;
                MyVoxelCoordSystems.WorldPositionToLocalPosition(from, matrix, matrixInv, sizeInMetersHalf, out localFrom);
                MyVoxelCoordSystems.WorldPositionToLocalPosition(to, matrix, matrixInv, sizeInMetersHalf, out localTo);
                var localLine = new LineD(localFrom, localTo);

                if (planet != null && ((IMyStorage)(planet.Storage)).Intersect(ref localLine))
                {
                    MyLog.Default.WriteLine(">> Railgun projectile hit planet");
                    _hitPosition = _position;
                    _targetHit = true;
                    Kill();
                    return;
                }

                // This is very broken
                //if (voxelMap != null && ((IMyStorage)(voxelMap.Storage)).Intersect(ref localLine))
                //{
                //    MyLog.Default.WriteLine(">> Railgun projectile hit voxel");
                //    _hitPosition = _position;
                //    _targetHit = true;
                //    Kill();
                //    return;
                //}
            }
        }

        void CreateExplosion(Vector3D position, Vector3D direction, float radius, float damage, float scale = 1f)
        {
            var m_explosionFullSphere = new BoundingSphere(position, radius);

            MyExplosionInfo info = new MyExplosionInfo()
            {
                PlayerDamage = 100,
                Damage = damage,
                ExplosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02,
                ExplosionSphere = m_explosionFullSphere,
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                ParticleScale = scale,
                Direction = direction,
                VoxelExplosionCenter = m_explosionFullSphere.Center,
                ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS | MyExplosionFlags.CREATE_PARTICLE_EFFECT | MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 0.1f,
                PlaySound = true,
                ApplyForceAndDamage = false, //to stop from flinging objects to orbit
                ObjectsRemoveDelayInMiliseconds = 40
            };

            MyExplosions.AddExplosion(ref info);
        }

        void GetObjectsToPenetrate(Vector3D start, Vector3D end)
        {
            MyLog.Default.WriteLine($">>>>>>>>> Getting railgun penetrated objects START <<<<<<<<<");

            _objectsToPenetrate.Clear();
            var testRay = new LineD(start, end);
            MyGamePruningStructure.GetAllEntitiesInRay(ref testRay, _overlappingEntities);

            foreach (var hit in _overlappingEntities)
            {
                var destroyable = hit.Element as IMyDestroyableObject;
                if (destroyable != null)
                {
                    MyLog.Default.WriteLine($"Destroyable object found");
                    var penetratedEntity = new PenetratedEntityContainer()
                    {
                        PenetratedEntity = destroyable,
                        WorldPosition = hit.Element.PositionComp.GetPosition(),
                    };

                    _objectsToPenetrate.Add(penetratedEntity);
                    continue;
                }

                var grid = hit.Element as IMyCubeGrid;
                if (grid != null)
                {
                    MyLog.Default.WriteLine($"Cube grid found");
                    IMySlimBlock slimBlock;

                    grid.RayCastCells(start, end, _hitPositions);

                    if (_hitPositions.Count == 0)
                    {
                        MyLog.Default.WriteLine(" No slim block found in intersection");
                        continue;
                    }

                    MyLog.Default.WriteLine($" {_hitPositions.Count} slim blocks in intersection");

                    foreach (var position in _hitPositions)
                    {
                        slimBlock = grid.GetCubeBlock(position);
                        if (slimBlock == null)
                            continue;
 
                        var penetratedEntity = new PenetratedEntityContainer()
                        {
                            PenetratedEntity = slimBlock,
                            WorldPosition = Vector3D.Transform(position * grid.GridSize, grid.WorldMatrix),
                        };
                        _objectsToPenetrate.Add(penetratedEntity);
                    }
                    continue;
                }
            }

            MyLog.Default.WriteLine($"<<<<<<<<< Getting railgun penetrated objects END >>>>>>>>>");
        }

        void SortObjectsToPenetrate(Vector3D start)
        {
            MyLog.Default.WriteLine($">>>>>>>>> Sorting railgun penetrated objects START <<<<<<<<<");
            // Sort objects to penetrate by distance, closest first
            _objectsToPenetrate.Sort((x, y) => Vector3D.DistanceSquared(start, x.WorldPosition).CompareTo(Vector3D.DistanceSquared(start, y.WorldPosition)));
            MyLog.Default.WriteLine($"<<<<<<<<< Sorting railgun penetrated objects END >>>>>>>>>");
        }

        void DamageObjectsToPenetrate(float damage)
        {
            MyLog.Default.WriteLine(">>>>>>>>>> Railgun penetration START <<<<<<<<<<");
            MyLog.Default.WriteLine($"Railgun initial pooled damage: {damage}");

            foreach (var item in _objectsToPenetrate)
            {
                if (damage <= 0)
                {
                    MyLog.Default.WriteLine("> Pooled damage expended");
                    break;
                }

                var destroyableObject = item.PenetratedEntity;

                var slimBlock = destroyableObject as IMySlimBlock;
                if (slimBlock != null)
                {
                    MyLog.Default.WriteLine($"> Slim block found");

                    var blockIntegrity = slimBlock.Integrity;
                    var cube = slimBlock.FatBlock;
                    MyLog.Default.WriteLine($"cube type: {(cube == null ? "null" : cube.GetType().ToString())}");
                    MyLog.Default.WriteLine($"pooled damage before: {damage}");
                    MyLog.Default.WriteLine($"block integrity before: {blockIntegrity}");

                    var invDamageMultiplier = 1f;
                    var cubeDef = slimBlock.BlockDefinition as MyCubeBlockDefinition;
                    if (cubeDef != null)
                    {
                        MyLog.Default.WriteLine($"block damage mult: {cubeDef.GeneralDamageMultiplier}");
                        invDamageMultiplier = 1f / cubeDef.GeneralDamageMultiplier;
                    }

                    try
                    {
                        if (damage > blockIntegrity)
                            slimBlock.DoDamage(blockIntegrity * invDamageMultiplier, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID); //because some blocks have a stupid damage intake modifier
                        else
                            slimBlock.DoDamage(damage * invDamageMultiplier, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                    }
                    catch (Exception ex)
                    {
                        MyLog.Default.WriteLine(ex);
                    }

                    if (damage < blockIntegrity)
                    {
                        damage = 0;
                        MyLog.Default.WriteLine($"pooled damage after: {damage}");
                        MyLog.Default.WriteLine($"block integrity after: {slimBlock.Integrity}");
                        break;
                    }
                    else
                    {
                        damage -= blockIntegrity;
                    }

                    MyLog.Default.WriteLine($"pooled damage after: {damage}");
                    MyLog.Default.WriteLine($"block integrity after: {slimBlock.Integrity}");

                    continue;
                }

                var character = destroyableObject as IMyCharacter;
                if (character != null)
                {
                    MyLog.Default.WriteLine($"> Character found");
                    character.Kill();
                    continue;
                }

                MyLog.Default.WriteLine($"> Destroyable entity found");
                var cachedIntegrity = destroyableObject.Integrity;

                destroyableObject.DoDamage(damage, MyStringHash.GetOrCompute("Railgun"), false, default(MyHitInfo), _gunEntityID);
                if (damage < cachedIntegrity)
                    damage = 0;
                else
                    damage -= cachedIntegrity;

            }
            MyLog.Default.WriteLine("<<<<<<<<<< Railgun penetration END >>>>>>>>>>");
        }

        void Kill()
        {
            if (_shouldPenetrate && _targetHit)
            {
                if (!_penetratedObjectsSorted)
                {
                    SortObjectsToPenetrate(_hitPosition);
                    _penetratedObjectsSorted = true;
                    return;
                }

                if (!_penetratedObjectsDamaged)
                {
                    DamageObjectsToPenetrate(_penetrationDamage);
                    _penetratedObjectsDamaged = true;
                }
            }

            if (_drawTrail && _currentTracerFadeTicks < _maxTracerFadeTicks)
            {
                _lineColor *= _trailColorDecayRatio;
                _currentTracerFadeTicks++;
                return;
            }

            Killed = true;
        }

        public List<RailgunTracerData> GetTracerData()
        {
            _tracerData.Clear();
            RailgunTracerData thisTracer;

            if (_trajectoryPoints.Count == 0)
                return _tracerData;

            // Draw projectile trail
            if (_drawTrail)
            {
                for (int i = 0; i < _trajectoryPoints.Count - 1; i++)
                {
                    var start = _trajectoryPoints[i];
                    var end = _trajectoryPoints[i + 1];

                    thisTracer = new RailgunTracerData()
                    {
                        ShooterID = _gunEntityID,
                        DrawTracer = false,
                        LineColor = _lineColor,
                        LineFrom = start,
                        LineTo = end,
                        ProjectileDirection = Vector3D.Zero,
                        DrawTrail = true,
                    };

                    _tracerData.Add(thisTracer);
                }
            }

            Vector3D trailStart = _trajectoryPoints[_trajectoryPoints.Count - 1];

            // Get important bullet parameters
            float lengthMultiplier = 40f * _tracerScale;
            lengthMultiplier *= 0.8f;
            var startPoint = _position - _direction * lengthMultiplier;

            bool shouldDraw = false;
            if (lengthMultiplier > 0f && !_targetHit && Vector3D.DistanceSquared(_position, _origin) > lengthMultiplier * lengthMultiplier && !Killed)
                shouldDraw = true;

            thisTracer = new RailgunTracerData()
            {
                ShooterID = _gunEntityID,
                DrawTracer = shouldDraw,
                DrawTrail = _drawTrail,
                LineColor = _lineColor,
                LineFrom = trailStart,
                LineTo = _targetHit ? _hitPosition : _position,
                ProjectileDirection = _direction,
            };

            _tracerData.Add(thisTracer);

            return _tracerData;
        }

        public struct PenetratedEntityContainer
        {
            public IMyDestroyableObject PenetratedEntity;
            public Vector3D WorldPosition;
        }
    }
}
