using System;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace DefenseShields
{
    public partial class Session
    {

        #region EMP
        private void PrepEmpBlast()
        {
            var stackCount = 0;
            var warHeadSize = 0;
            var warHeadYield = 0d;
            var epiCenter = Vector3D.Zero;

            WarHeadBlast empChild;
            while (EmpStore.TryDequeue(out empChild))
            {
                if (empChild.CustomData.Contains("@EMP"))
                {
                    stackCount++;
                    warHeadSize = empChild.WarSize;
                    warHeadYield = empChild.Yield;
                    epiCenter += empChild.Position;
                }
            }

            if (stackCount == 0)
            {
                EmpWork.EventComplete();
                return;
            }
            epiCenter /= stackCount;
            var rangeCap = MathHelper.Clamp(stackCount * warHeadYield, warHeadYield, SyncDist);

            _warHeadGridHits.Clear();
            _pruneWarGrids.Clear();

            var sphere = new BoundingSphereD(epiCenter, rangeCap);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, _pruneWarGrids);

            foreach (var ent in _pruneWarGrids)
            {
                var grid = ent as MyCubeGrid;
                if (grid != null)
                {
                    ShieldGridComponent sComp;
                    grid.Components.TryGet(out sComp);
                    if (sComp?.DefenseShields != null && sComp.DefenseShields.NotFailed) continue;

                    var gridCenter = grid.PositionComp.WorldVolume.Center;
                    var testDir = Vector3D.Normalize(gridCenter - epiCenter);
                    var impactPos = gridCenter + (testDir * -grid.PositionComp.WorldVolume.Radius);

                    IHitInfo hitInfo;
                    MyAPIGateway.Physics.CastRay(epiCenter, impactPos, out hitInfo, CollisionLayers.DefaultCollisionLayer);
                    if (hitInfo?.HitEntity == null) _warHeadGridHits.Add(grid);
                }
            }

            EmpWork.StoreEmpBlast(epiCenter, warHeadSize, warHeadYield, stackCount, rangeCap);
        }

        private void ComputeEmpBlast()
        {
            var epiCenter = EmpWork.EpiCenter;
            var rangeCap = EmpWork.RangeCap;
            var dirYield = EmpWork.DirYield;
            const double BlockInflate = 1.25;

            GetFilteredItems(epiCenter, rangeCap, dirYield);

            foreach (var cube in _warHeadCubeHits)
            {
                WarHeadHit warHit;
                var foundSphere = _warHeadGridShapes.TryGetValue(cube.CubeGrid, out warHit);
                if (foundSphere && warHit.Sphere.Contains(cube.PositionComp.WorldAABB.Center) != ContainmentType.Disjoint)
                {
                    var clearance = cube.CubeGrid.GridSize * BlockInflate;
                    var testDir = Vector3D.Normalize(epiCenter - cube.PositionComp.WorldAABB.Center);
                    var testPos = cube.PositionComp.WorldAABB.Center + (testDir * clearance);
                    var hit = cube.CubeGrid.RayCastBlocks(epiCenter, testPos);

                    if (hit == null)
                    {
                        BlockState blockState;
                        uint endTick;

                        var cubeId = cube.EntityId;
                        var oldState = _warEffectCubes.TryGetValue(cubeId, out blockState);

                        if (oldState) endTick = blockState.Endtick + (Tick + (warHit.Duration + 1));
                        else endTick = Tick + (warHit.Duration + 1);
                        var startTick = (((Tick + 1) / 20) * 20) + 20;

                        _warEffectCubes[cube.EntityId] = new BlockState(cube, startTick, endTick);
                    }
                    else if (cube.SlimBlock == cube.CubeGrid.GetCubeBlock(hit.Value))
                    {
                        BlockState blockState;
                        uint endTick;

                        var cubeId = cube.EntityId;
                        var oldState = _warEffectCubes.TryGetValue(cubeId, out blockState);

                        if (oldState) endTick = blockState.Endtick + (Tick + (warHit.Duration + 1));
                        else endTick = Tick + (warHit.Duration + 1);
                        var startTick = (((Tick + 1) / 20) * 20) + 20;

                        _warEffectCubes[cube.EntityId] = new BlockState(cube, startTick, endTick);
                    }
                }
            }
            EmpWork.ComputeComplete();
        }

        private void GetFilteredItems(Vector3D epiCenter, double rangeCap, double dirYield)
        {
            _warHeadCubeHits.Clear();
            _warHeadGridShapes.Clear();
            var myCubeList = new List<MyEntity>();
            foreach (var grid in _warHeadGridHits)
            {
                var invSqrDist = UtilsStatic.InverseSqrDist(epiCenter, grid.PositionComp.WorldAABB.Center, rangeCap);
                var damage = (uint)(dirYield * invSqrDist) * 5;
                var gridAabb = grid.PositionComp.WorldAABB;
                var sphere = CustomCollision.NewObbClosestTriCorners(grid, epiCenter);

                grid.Hierarchy.QueryAABB(ref gridAabb, myCubeList);
                _warHeadGridShapes.Add(grid, new WarHeadHit(sphere, damage));
            }

            for (int i = 0; i < myCubeList.Count; i++)
            {
                var myEntity = myCubeList[i];
                var myCube = myEntity as MyCubeBlock;

                if (myCube == null || myCube.MarkedForClose) continue;
                if ((myCube is IMyThrust || myCube is IMyUserControllableGun || myCube is IMyUpgradeModule) && myCube.IsFunctional && myCube.IsWorking)
                {
                    _warHeadCubeHits.Add(myCube);
                }
            }
            if (Enforced.Debug >= 2) Log.Line($"[ComputeEmpBlast] AllFat:{myCubeList.Count} - TrimmedFat:{_warHeadCubeHits.Count}");
        }

        private void EmpCallBack()
        {
            if (!DedicatedServer) EmpDrawExplosion();
            EmpDispatched = false;
            if (!_warEffectCubes.IsEmpty) _warEffect = true;
        }

        private void EmpDrawExplosion()
        {
            _effect?.Stop();
            var epiCenter = EmpWork.EpiCenter;
            var rangeCap = EmpWork.RangeCap;
            var radius = (float)(rangeCap * 0.01);
            var scale = 7f;

            if (radius < 7) scale = radius;

            var matrix = MatrixD.CreateTranslation(epiCenter);
            MyParticlesManager.TryCreateParticleEffect(6666, out _effect, ref matrix, ref epiCenter, uint.MaxValue, true); // 15, 16, 24, 25, 28, (31, 32) 211 215 53
            if (_effect == null)
            {
                EmpWork.EmpDrawComplete();
                return;
            }

            if (Enforced.Debug >= 2) Log.Line($"[EmpDraw] scale:{scale} - radius:{radius} - rangeCap:{rangeCap}");

            _effect.UserRadiusMultiplier = radius;
            _effect.UserEmitterScale = scale;
            _effect.UserColorMultiplier = new Vector4(255, 255, 255, 10);
            _effect.Play();
            EmpWork.EmpDrawComplete();
        }

        private void WarEffect()
        {
            foreach (var item in _warEffectCubes)
            {

                var cubeid = item.Key;
                var blockInfo = item.Value;
                var startTick = blockInfo.StartTick;
                var tick = Tick;

                var functBlock = blockInfo.FunctBlock;
                if (functBlock == null || functBlock.MarkedForClose)
                {
                    _warEffectPurge.Enqueue(cubeid);
                    continue;
                }

                if (tick <= startTick)
                {
                    if (tick < startTick) continue;
                    functBlock.Enabled = false;
                    functBlock.EnabledChanged += ForceDisable;
                }

                if (tick < blockInfo.Endtick)
                {
                    if (Tick60) functBlock.SetDamageEffect(true);
                }
                else
                {
                    functBlock.EnabledChanged -= ForceDisable;
                    functBlock.Enabled = blockInfo.EnableState;
                    functBlock.SetDamageEffect(false);
                    _warEffectPurge.Enqueue(cubeid);
                }
            }

            while (_warEffectPurge.Count != 0)
            {
                BlockState value;
                _warEffectCubes.TryRemove(_warEffectPurge.Dequeue(), out value);
            }

            if (_warEffectCubes.IsEmpty) _warEffect = false;
        }

        private void ForceDisable(IMyTerminalBlock myTerminalBlock)
        {
            ((IMyFunctionalBlock)myTerminalBlock).Enabled = false;
        }
        #endregion

    }
}
