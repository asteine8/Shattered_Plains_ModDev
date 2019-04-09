using VRage.Game;

namespace DefenseShields.Support
{
    using System;
    using Sandbox.Game;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Character.Components;
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Utils;
    using VRageMath;

    public interface IThreadEvent
    {
        void Execute();
    }

    public class ShieldVsShieldThreadEvent : IThreadEvent
    {
        public readonly DefenseShields Shield;
        public readonly float Damage;
        public readonly Vector3D CollisionAvg;
        public readonly long AttackerId;

        public ShieldVsShieldThreadEvent(DefenseShields shield, float damage, Vector3D collisionAvg, long attackerId)
        {
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
            AttackerId = attackerId;
        }

        public void Execute()
        {
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(AttackerId, Damage, Session.Instance.MPEnergy, null, true, CollisionAvg);
            }
            else
            {
                Shield.EnergyHit = true;
                Shield.ImpactSize = Damage;
                Shield.WorldImpactPosition = CollisionAvg;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
        }
    }

    public class MissileThreadEvent : IThreadEvent
    {
        public readonly MyEntity Entity;
        public readonly DefenseShields Shield;

        public MissileThreadEvent(MyEntity entity, DefenseShields shield)
        {
            Entity = entity;
            Shield = shield;
        }

        public void Execute()
        {
            if (Entity == null || !Entity.InScene || Entity.MarkedForClose) return;
            var computedDamage = UtilsStatic.ComputeAmmoDamage(Entity);

            var damage = computedDamage * Shield.DsState.State.ModulateKinetic;
            if (computedDamage < 0) damage = computedDamage;

            var rayDir = Vector3D.Normalize(Entity.Physics.LinearVelocity);
            var ray = new RayD(Entity.PositionComp.WorldVolume.Center, rayDir);
            var intersect = CustomCollision.IntersectEllipsoid(Shield.DetectMatrixOutsideInv, Shield.DetectionMatrix, ray);
            var hitDist = intersect ?? 0;
            var hitPos = ray.Position + (ray.Direction * -hitDist);

            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, damage, Session.Instance.MPExplosion, null, true, hitPos);
                Entity.Close();
                Entity.InScene = false;
            }
            else
            {
                Shield.EnergyHit = true;
                Shield.WorldImpactPosition = hitPos;
                Shield.ImpactSize = damage;
                UtilsStatic.CreateFakeSmallExplosion(hitPos);
                Entity.Close();
                Entity.InScene = false;
            }
            Shield.WebDamage = true;
            Shield.Absorb += damage;
        }
    }

    public class FloaterThreadEvent : IThreadEvent
    {
        public readonly MyEntity Entity;
        public readonly DefenseShields Shield;

        public FloaterThreadEvent(MyEntity entity, DefenseShields shield)
        {
            Entity = entity;
            Shield = shield;
        }

        public void Execute()
        {
            if (Entity == null || Entity.MarkedForClose) return;
            var floater = (IMyFloatingObject)Entity;
            var entVel = Entity.Physics.LinearVelocity;
            var movingVel = entVel != Vector3.Zero ? entVel : -Shield.MyGrid.Physics.LinearVelocity;

            var rayDir = Vector3D.Normalize(movingVel);
            var ray = new RayD(Entity.PositionComp.WorldVolume.Center, rayDir);
            var intersect = CustomCollision.IntersectEllipsoid(Shield.DetectMatrixOutsideInv, Shield.DetectionMatrix, ray);
            var hitDist = intersect ?? 0;
            var hitPos = ray.Position + (ray.Direction * -hitDist);

            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, 1, Session.Instance.MPKinetic, null, false, hitPos);
                floater.DoDamage(9999999, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);
            }
            else
            {
                Shield.WorldImpactPosition = hitPos;
                Shield.ImpactSize = 10;
                floater.DoDamage(9999999, Session.Instance.MpIgnoreDamage, false, null, Shield.MyCube.EntityId);
            }
            Shield.WebDamage = true;
            Shield.Absorb += 1;
        }
    }
    public class CollisionDataThreadEvent : IThreadEvent
    {
        public readonly MyCollisionPhysicsData CollisionData;
        public readonly DefenseShields Shield;

        public CollisionDataThreadEvent(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Execute()
        {
            if (CollisionData.Entity1 == null || CollisionData.Entity2 == null || CollisionData.Entity1.MarkedForClose || CollisionData.Entity2.MarkedForClose) return;
            var tick = Session.Instance.Tick;
            EntIntersectInfo entInfo;

            var foundInfo = Shield.WebEnts.TryGetValue(CollisionData.Entity1, out entInfo);
            if (!foundInfo || entInfo.LastCollision == tick) return;

            if (entInfo.LastCollision >= tick - 8) entInfo.ConsecutiveCollisions++;
            else entInfo.ConsecutiveCollisions = 0;
            entInfo.LastCollision = tick;
            if (entInfo.ConsecutiveCollisions > 0) if (Session.Enforced.Debug >= 2) Log.Line($"Consecutive:{entInfo.ConsecutiveCollisions}");
            if (!CollisionData.E1IsStatic)
            {
                if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity1.Physics.ApplyImpulse(CollisionData.ImpDirection1, CollisionData.CollisionCorrection1);
                if (CollisionData.E2IsHeavier)
                {
                    var accelCap = CollisionData.E1IsStatic ? 10 : 50;
                    var accelClamp = MathHelper.Clamp(CollisionData.Mass2 / CollisionData.Mass1, 1, accelCap);
                    var collisions = entInfo.ConsecutiveCollisions + 1;
                    var sizeAccel = accelClamp > collisions ? accelClamp : collisions;
                    var forceMulti = (CollisionData.Mass1 * (collisions * sizeAccel));
                    if (CollisionData.Entity1.Physics.LinearVelocity.Length() <= (Session.Instance.MaxEntitySpeed * 0.75))
                        CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, null, CollisionData.Immediate);
                }
            }

            if (!CollisionData.E2IsStatic)
            {
                if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity2.Physics.ApplyImpulse(CollisionData.ImpDirection2, CollisionData.CollisionCorrection2);
                if (CollisionData.E1IsHeavier)
                {
                    var accelCap = CollisionData.E1IsStatic ? 10 : 50;
                    var accelClamp = MathHelper.Clamp(CollisionData.Mass1 / CollisionData.Mass2, 1, accelCap);
                    var collisions = entInfo.ConsecutiveCollisions + 1;
                    var sizeAccel = accelClamp > collisions ? accelClamp : collisions;
                    var forceMulti = (CollisionData.Mass2 * (collisions * sizeAccel));
                    if (CollisionData.Entity2.Physics.LinearVelocity.Length() <= (Session.Instance.MaxEntitySpeed * 0.75))
                        CollisionData.Entity2.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force2, null, null, null, CollisionData.Immediate);
                }
            }
        }
    }

    public class StationCollisionDataThreadEvent : IThreadEvent
    {
        public readonly MyCollisionPhysicsData CollisionData;
        public readonly DefenseShields Shield;

        public StationCollisionDataThreadEvent(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Execute()
        {
            if (CollisionData.Entity1 == null || CollisionData.Entity1.MarkedForClose) return;
            var tick = Session.Instance.Tick;
            EntIntersectInfo entInfo;

            var foundInfo = Shield.WebEnts.TryGetValue(CollisionData.Entity1, out entInfo);
            if (!foundInfo || entInfo.LastCollision == tick) return;

            if (entInfo.LastCollision >= tick - 8) entInfo.ConsecutiveCollisions++;
            else entInfo.ConsecutiveCollisions = 0;
            entInfo.LastCollision = tick;
            if (entInfo.ConsecutiveCollisions > 0) if (Session.Enforced.Debug >= 2) Log.Line($"Consecutive Station hits:{entInfo.ConsecutiveCollisions}");

            if (entInfo.ConsecutiveCollisions == 0) CollisionData.Entity1.Physics.ApplyImpulse(CollisionData.ImpDirection1, CollisionData.CollisionAvg);

            var collisions = entInfo.ConsecutiveCollisions + 1;
            var forceMulti = CollisionData.Mass1 * (collisions * 60);
            if (CollisionData.Entity1.Physics.LinearVelocity.Length() <= (Session.Instance.MaxEntitySpeed * 0.75))
                CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, null, CollisionData.Immediate);

            var transformInv = Shield.DetectMatrixOutsideInv;
            var normalMat = MatrixD.Transpose(transformInv);
            var localNormal = Vector3D.Transform(CollisionData.CollisionAvg, transformInv);
            var surfaceNormal = Vector3D.Normalize(Vector3D.TransformNormal(localNormal, normalMat));
            CollisionData.Entity1.Physics.ApplyImpulse((CollisionData.Mass1 * 0.075) * CollisionData.ImpDirection2, CollisionData.CollisionAvg);
        }
    }

    public class PlayerCollisionThreadEvent : IThreadEvent
    {
        public readonly MyCollisionPhysicsData CollisionData;
        public readonly DefenseShields Shield;

        public PlayerCollisionThreadEvent(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Execute()
        {
            const int forceMulti = 200000;
            CollisionData.Entity1.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force1, null, null, null, CollisionData.Immediate);
            var character = CollisionData.Entity1 as IMyCharacter;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(CollisionData.Entity1.EntityId, 1, Session.Instance.MPKinetic, null, false, CollisionData.CollisionAvg);
                character?.DoDamage(1f, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);
            }
            else
            {
                Shield.ImpactSize = 1;
                Shield.WorldImpactPosition = CollisionData.CollisionAvg;
                character?.DoDamage(1f, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);
            }
        }
    }

    public class CharacterEffectThreadEvent : IThreadEvent
    {
        public readonly IMyCharacter Character;
        public readonly DefenseShields Shield;

        public CharacterEffectThreadEvent(IMyCharacter character, DefenseShields shield)
        {
            Character = character;
            Shield = shield;
        }

        public void Execute()
        {
            var npcname = Character.ToString();
            if (npcname.Equals("Space_Wolf"))
            {
                Character.Delete();
            }
        }
    }

    public class ManyBlocksThreadEvent : IThreadEvent
    {
        public readonly DefenseShields Shield;
        public readonly HashSet<CubeAccel> AccelSet;
        public readonly float Damage;
        public readonly Vector3D CollisionAvg;
        public readonly long AttackerId;

        public ManyBlocksThreadEvent(HashSet<CubeAccel> accelSet, DefenseShields shield, float damage, Vector3D collisionAvg, long attackerId)
        {
            AccelSet = accelSet;
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
            AttackerId = attackerId;
        }

        public void Execute()
        {
            foreach (var accel in AccelSet)
            {
                EntIntersectInfo entInfo;
                if (accel.Grid != accel.Block.CubeGrid)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo))
                    {
                        entInfo.RefreshNow = true;
                    }
                    return;
                }

                if (accel.Block.IsDestroyed)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo)) entInfo.RefreshNow = true;
                    return;
                }

                accel.Block.DoDamage(accel.Block.MaxIntegrity, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);

                if (accel.Block.IsDestroyed)
                {
                    if (Shield.WebEnts.TryGetValue(accel.Grid, out entInfo)) entInfo.RefreshNow = true;
                }
            }

            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(AttackerId, Damage, Session.Instance.MPKinetic, null, true, CollisionAvg);
            }
            else
            {
                Shield.ImpactSize = Damage;
                Shield.WorldImpactPosition = CollisionAvg;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
        }
    }

    public class VoxelCollisionDmgThreadEvent : IThreadEvent
    {
        public readonly MyEntity Entity;
        public readonly DefenseShields Shield;
        public readonly float Damage;
        public readonly Vector3D CollisionAvg;

        public VoxelCollisionDmgThreadEvent(MyEntity entity, DefenseShields shield, float damage, Vector3D collisionAvg)
        {
            Entity = entity;
            Shield = shield;
            Damage = damage;
            CollisionAvg = collisionAvg;
        }

        public void Execute()
        {
            if (Entity == null || Entity.MarkedForClose) return;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Entity.EntityId, Damage, Session.Instance.MPKinetic, null, false, CollisionAvg);
            }
            else
            {
                Shield.WorldImpactPosition = CollisionAvg;
                Shield.ImpactSize = 12000;
            }
            Shield.WebDamage = true;
            Shield.Absorb += Damage;
        }
    }

    public class VoxelCollisionPhysicsThreadEvent : IThreadEvent
    {
        public readonly MyCollisionPhysicsData CollisionData;
        public readonly DefenseShields Shield;

        public VoxelCollisionPhysicsThreadEvent(MyCollisionPhysicsData collisionPhysicsData, DefenseShields shield)
        {
            CollisionData = collisionPhysicsData;
            Shield = shield;
        }

        public void Execute()
        {
                Vector3 velAtPoint;
                var point = CollisionData.CollisionCorrection2;
                CollisionData.Entity2.Physics.GetVelocityAtPointLocal(ref point, out velAtPoint);
                var speed = MathHelper.Clamp(velAtPoint.Length(), 2f, 20f);
                var forceMulti = (CollisionData.Mass2 * 10) * speed;
                CollisionData.Entity2.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceMulti * CollisionData.Force2, null, null, speed, CollisionData.Immediate);
        }
    }

    public class VoxelDmgThreadEvent : IThreadEvent
    {
        public readonly MyVoxelBase VoxelBase;
        public readonly DefenseShields Shield;

        public VoxelDmgThreadEvent(MyVoxelBase voxelBase, DefenseShields shield)
        {
            VoxelBase = voxelBase;
            Shield = shield;
        }

        public void Execute()
        {
            if (VoxelBase == null || VoxelBase.RootVoxel.MarkedForClose || VoxelBase.RootVoxel.Closed) return;
            VoxelBase.RootVoxel.RequestVoxelOperationElipsoid(Vector3.One * 1.0f, Shield.DetectMatrixOutside, 0, MyVoxelBase.OperationType.Cut);
        }
    }

    public class MeteorDmgThreadEvent : IThreadEvent
    {
        public readonly IMyMeteor Meteor;
        public readonly DefenseShields Shield;

        public MeteorDmgThreadEvent(IMyMeteor meteor, DefenseShields shield)
        {
            Meteor = meteor;
            Shield = shield;
        }

        public void Execute()
        {
            if (Meteor == null || Meteor.MarkedForClose) return;
            var damage = 5000 * Shield.DsState.State.ModulateEnergy;
            if (Session.Instance.MpActive)
            {
                Shield.AddShieldHit(Meteor.EntityId, damage, Session.Instance.MPKinetic, null, false, Meteor.PositionComp.WorldVolume.Center);
                Meteor.DoDamage(10000f, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);
            }
            else
            {
                Shield.WorldImpactPosition = Meteor.PositionComp.WorldVolume.Center;
                Shield.ImpactSize = damage;
                Meteor.DoDamage(10000f, Session.Instance.MpIgnoreDamage, true, null, Shield.MyCube.EntityId);
            }
            Shield.WebDamage = true;
            Shield.Absorb += damage;
        }
    }

    public class ForceDataThreadEvent : IThreadEvent
    {
        public readonly MyForceData ForceData;
        public readonly DefenseShields Shield;

        public ForceDataThreadEvent(MyForceData forceData, DefenseShields shield)
        {
            ForceData = forceData;
            Shield = shield;
        }

        public void Execute()
        {
            if (ForceData.Entity == null || ForceData.Entity.MarkedForClose) return;
            ForceData.Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, ForceData.Force, null, Vector3D.Zero, ForceData.MaxSpeed, ForceData.Immediate);
        }
    }

    public class ImpulseDataThreadEvent : IThreadEvent
    {
        public readonly MyImpulseData ImpulseData;
        public readonly DefenseShields Shield;

        public ImpulseDataThreadEvent(MyImpulseData impulseData, DefenseShields shield)
        {
            ImpulseData = impulseData;
            Shield = shield;
        }

        public void Execute()
        {
            if (ImpulseData.Entity == null || ImpulseData.Entity.MarkedForClose) return;
            ImpulseData.Entity.Physics.ApplyImpulse(ImpulseData.Direction, ImpulseData.Position);
        }
    }
}
