using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Ingame = VRage.Game.ModAPI.Ingame;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Phoenix.Stargate
{
    using Extensions;
    using Sandbox.Game.Entities;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Phoenix_Stargate_EventHorizon_Vortex")]
    class StargateVortex : MyGameLogicComponent
    {
        const int m_maxLifetime = (int)(60 * 2);      // Todo, make this based on actual UPS, not hardcoded
        MyObjectBuilder_EntityBase m_objectBuilder;
        bool m_init = false;
        IMyTerminalBlock m_referenceGate = null;
        IMyEntity m_referenceHorizon = null;
        int m_lifetime = 0;
        bool m_reverse = false;
        bool m_damageDealt = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!StargateAdmin.Configuration.VortexVisible)
                return;

            if ((Container.Entity as IMyFunctionalBlock).BlockDefinition.SubtypeId != "Phoenix_Stargate_EventHorizon_Vortex")
                return;

            if (!Globals.ModEnabled)
                return;

            m_objectBuilder = objectBuilder;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            //Container.Entity.Visible = false;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)m_objectBuilder.Clone() : m_objectBuilder;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!Globals.ModEnabled)
                return;

            if (!m_init)
            {
                if (!StargateAdmin.Configuration.VortexVisible)
                    return;
                try
                {
                    Container.Entity.OnClosing += Entity_OnClosing;
                    m_init = true;
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                    var sphere = Container.Entity.WorldVolume;

                    var entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                    // Need to find the closest one to the vortex, that's the target gate.
                    entities.Sort((l, r) =>
                    {
                        var llen = (Entity.PositionComp.GetPosition() - l.PositionComp.GetPosition()).LengthSquared();
                        var rlen = (Entity.PositionComp.GetPosition() - r.PositionComp.GetPosition()).LengthSquared();
                        if (llen < rlen)
                            return -1;
                        else if (llen > rlen)
                            return 1;
                        else if ((rlen - rlen) < 0.01)
                            return 0;
                        else
                            return 1;
                    });
                    Logger.Instance.LogDebug(sphere.ToString());
                    foreach (var entity in entities)
                    {
                        if (entity is IMyTerminalBlock && (entity as IMyTerminalBlock).IsWorking && (entity as IMyTerminalBlock).GetGateType() != GateType.Invalid)
                        {
                            m_referenceGate = entity as IMyTerminalBlock;

                            VRage.Game.Entity.MyEntitySubpart subpart;
                            m_referenceGate.TryGetSubpart(Constants.EventHorizonSubpartName, out subpart);
                            m_referenceHorizon = subpart;
                            if (subpart != null)
                                break;
                        }
                        //else if (entity is IMyCubeBlock && (entity as IMyCubeBlock).BlockDefinition.SubtypeId.StartsWith("Event Horizon"))
                        //    m_referenceHorizon = entity.GetTopMostParent();
                    }
                    Logger.Instance.LogAssert(m_referenceGate != null, "m_referenceGate != null");
                    Logger.Instance.LogAssert(m_referenceHorizon != null, "m_referenceHorizon != null");
                    if (m_referenceGate == null || m_referenceGate.GetGateType() != GateType.Stargate)
                    {
                        NeedsUpdate = MyEntityUpdateEnum.NONE;
                        // If we get here, we couldn't find the gate, maybe due to world streaming
                        // Just ignore and delete the vortex.
                        Container.Entity.GetTopMostParent().Close();
                        return;
                    }
                    //(Container.Entity as IMyCubeBlock).SetDamageEffect(true);
                    m_referenceHorizon.Visible = false;
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        if (m_referenceGate.IsIrisActive())
                        {
                            if (Container.Entity.SyncObject != null)
                                MyEntities.SendCloseRequest(Container.Entity);
                            else
                                Container.Entity.Close();
                        }
                    }
                }
                catch { }
            }
        }

        void Entity_OnClosing(IMyEntity obj)
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;

            if (m_referenceHorizon != null && m_referenceGate != null && m_referenceGate.IsFunctional)
                m_referenceHorizon.Visible = true;
            Container.Entity.OnClosing -= Entity_OnClosing;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!Globals.ModEnabled)
                return;

            if (!m_init)
                return;

            if (m_referenceGate != null && m_referenceGate.MarkedForClose || m_referenceGate.Closed)
                m_referenceGate = null;

            Logger.Instance.LogAssert(m_referenceGate != null, "m_referenceGate != null");
            if (m_referenceGate == null)
            {
                if (Entity.SyncObject != null)
                    MyEntities.SendCloseRequest(Entity);
                else
                    Entity.Close();
                return;
            }

            var matrix1 = m_referenceGate.WorldMatrix;
            var matrix2 = m_referenceGate.WorldMatrix;
            var radius = 1.75f;
            Vector3D baseGateOffset = Vector3D.Zero;

            matrix1.Translation = matrix1.Translation + (matrix1.Backward * 2.5) + (matrix1.Down * Constants.GateUpOffset);
            matrix2.Translation = matrix2.Translation + (matrix2.Backward * 1) + (matrix2.Down * Constants.GateUpOffset);

            if ((m_referenceGate as IMyCubeBlock)?.GetObjectBuilderCubeBlock()?.SubtypeName == "Stargate M")
            {
                baseGateOffset = (m_referenceGate.WorldMatrix.Up * 1) - (m_referenceGate.WorldMatrix.Backward * Constants.BaseGateForwardOffset);
                matrix1.Translation = matrix1.Translation + baseGateOffset;
                matrix2.Translation = matrix2.Translation + baseGateOffset;
            }

            // Draw spheres around damage area for debug visualization
            if (StargateAdmin.Configuration.Debug && MyAPIGateway.Session.Player != null)
            {
                var color1 = Color.Red;

                MySimpleObjectDraw.DrawTransparentSphere(ref matrix1, radius, ref color1, MySimpleObjectRasterizer.Solid, 20);
                MySimpleObjectDraw.DrawTransparentSphere(ref matrix2, radius, ref color1, MySimpleObjectRasterizer.Solid, 20);
            }

            // Skip frames to speed up processing
            if (m_lifetime++ % 1 != 0)
                return;

            if (m_lifetime >= m_maxLifetime || m_referenceGate.IsIrisActive())
            {
                if (m_reverse)
                {
                    // We want to get rid of the vortex is the timer runs out, or the iris is active
                    NeedsUpdate = MyEntityUpdateEnum.NONE;
                    Entity.GetTopMostParent().Close();
                    return;
                }
                else
                {
                    m_reverse = true;
                    m_lifetime = 0;

                    // TODO: Workaround since the vortex is single-sided; remove this eventually
                    if (m_referenceHorizon != null && m_referenceGate != null && m_referenceGate.IsFunctional)
                        m_referenceHorizon.Visible = true;
                }
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (((float)m_lifetime / m_maxLifetime) > 0.5 && !m_reverse)
                {
                    if (!m_referenceGate.IsIrisActive())
                    {
                        DamageNearby(matrix1, matrix2, radius);
                    }
                }
            }

            if (m_reverse)
                m_lifetime++;       // Go in reverse twice as fast

            // Translations must be applied in this order: scale -> rotation -> translation
            if (MyAPIGateway.Session.Player != null)
            {
                try
                {
                    // Calculate scale
                    // Scale is based on a sine wave of the percentage of the timer count.
                    //var percent = ((((float)m_lifetime / (float)m_maxLifetime) - 0.5) * 2) * 0.9;
                    //var percent = ((((float)m_lifetime * 1.1) / (((float)m_maxLifetime / 2))));
                    var percent = Math.Sin(((float)m_lifetime / ((float)m_maxLifetime / 2)) * (Math.PI / 2));

                    if (percent > 1.0)
                        percent = 1.0;

                    //Logger.Instance.LogDebug(string.Format("Percent: {0:P}", percent));
                    double scale = 1.0;

                    if (percent > 1)
                        percent = 1;

                    scale = percent;

                    // Double check for sanity
                    if (double.IsNaN(scale) || double.IsInfinity(scale))
                        scale = 0.0;

                    var currentMat = Container.Entity.GetTopMostParent().WorldMatrix;
                    var scaleVec = new Vector3D(1.0, 1.0, scale);
                    //scaleVec *= scale;
                    Logger.Instance.LogAssert(m_referenceGate != null, "m_referenceGate != null");
                    var newMat = MatrixD.CreateFromTransformScale(Quaternion.CreateFromForwardUp((m_reverse ? m_referenceGate.WorldMatrix.Forward : m_referenceGate.WorldMatrix.Backward), m_referenceGate.WorldMatrix.Up), m_referenceGate.WorldMatrix.Translation, scaleVec);

                    // Calculate rotation for animation
                    var currentRot = Quaternion.CreateFromAxisAngle((m_reverse ? newMat.Backward : newMat.Forward), 0);
                    var worldRot = Quaternion.CreateFromAxisAngle((m_reverse ? newMat.Backward : newMat.Forward), -0.035f * (m_maxLifetime - m_lifetime));
                    currentRot.Conjugate();
                    var newRot = worldRot * currentRot;
                    newRot.Normalize();                     // Normalize prevents overflow from long periods of rotation
                    newMat = MatrixD.Transform(newMat, newRot);

                    var velocity = m_referenceGate?.GetTopMostParent()?.Physics?.LinearVelocity ?? Vector3D.Zero;

                    // Calculate Translation (should stay near gate
                    newMat.Translation = m_referenceGate.WorldMatrix.Translation + (m_referenceGate.WorldMatrix.Down * Constants.GateUpOffset);
                    newMat.Translation = newMat.Translation + baseGateOffset;       // Offset for gate with base, if needed
                    newMat.Translation += (velocity / 60f);
                    Container.Entity.GetTopMostParent().PositionComp.SetWorldMatrix(newMat);
                    Container.Entity.GetTopMostParent().PositionComp.SetPosition(newMat.Translation);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                }
            }
        }
        private void DamageNearby(MatrixD matrix1, MatrixD matrix2, float radius)
        {
            // Do damage, if enabled
            if (StargateAdmin.Configuration.VortexDamage && !m_damageDealt)
            {
                m_damageDealt = true;
                try
                {
                    // Destroy blocks in vortex path!
                    var testSphere = new BoundingSphereD(matrix1.Translation, radius);
                    var testSphere1 = new BoundingSphereD(matrix2.Translation, radius);
                    var testSphereBig = testSphere1;
                    testSphereBig.Include(testSphere);

                    //var testCapsule = new CapsuleD(m_referenceGate.WorldMatrix.Translation, m_referenceGate.WorldMatrix.Translation + (m_referenceGate.WorldMatrix.Backward * 2.5), 1.75);

                    Logger.Instance.LogDebug(string.Format("Damage sphere radius: {0}", testSphereBig.Radius));
                    var selectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref testSphereBig);
                    HashSet<IMyEntity> entitiesToDamage = new HashSet<IMyEntity>();

                    // Find entities in the way
                    foreach (var entity in selectedEntities)
                    {
                        Logger.Instance.LogDebug("Found Entity: " + (string.IsNullOrEmpty(entity.DisplayName) ? entity.GetType().ToString() : entity.DisplayName));

                        if ((entity is IMyCubeBlock &&
                                (entity.GetTopMostParent().EntityId == Container.Entity.GetTopMostParent().EntityId ||
                                entity.GetTopMostParent().DisplayName.StartsWith("Event Horizon at ")
                                )) ||
                                (entity is IMyCubeGrid &&
                                ((entity as IMyCubeGrid).EntityId == Container.Entity.GetTopMostParent().EntityId ||
                                (entity as IMyCubeGrid).DisplayName.StartsWith("Event Horizon at")
                            )))
                            continue;

                        if (entity.WorldAABB.Contains(testSphere) == ContainmentType.Disjoint)
                            continue;

                        entitiesToDamage.Add(entity);
                    }

                    //// Parse entities for blocks to damage
                    foreach (var entity in entitiesToDamage)
                    {
                        Logger.Instance.LogDebug("Found Entity to Destroy: " + (string.IsNullOrEmpty(entity.GetTopMostParent().DisplayName) ? entity.GetType().ToString() : entity.GetTopMostParent().DisplayName));
                        if (entity is IMyCubeGrid)
                        {
                            ApplyBlockDamage(entity as IMyCubeGrid, testSphere);
                            ApplyBlockDamage(entity as IMyCubeGrid, testSphere1);
                        }
                        else if (entity is IMyCharacter)
                        {
                            if (MyAPIGateway.Players.GetPlayerControllingEntity(entity) == null)
                            {
                                if (entity.SyncObject != null)
                                    MyEntities.SendCloseRequest(entity);
                                else
                                    entity.Close();
                            }
                            else
                            {
                                MyDamageInformation damageInfo = new MyDamageInformation();
                                damageInfo.Amount = 999999;
                                damageInfo.Type = MyDamageType.Destruction;
                                (entity as IMyCharacter).Kill(damageInfo);
                            }
                        }
                        else if (entity is IMyFloatingObject)
                        {
                            if (entity.SyncObject != null)
                                MyEntities.SendCloseRequest(entity);
                            else
                                entity.Close();
                        }
                    }

                    Vector3 lastPos = new Vector3D();
                    var voxel = MyAPIGateway.Session.VoxelMaps.GetOverlappingWithSphere(ref testSphere);
                    if (voxel != null)
                    {
                        Logger.Instance.LogDebug("Cutting into voxel");
                        var shape = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
                        shape.Radius = (float)testSphere.Radius;
                        shape.Center = m_referenceGate.WorldMatrix.Translation;
                        MyAPIGateway.Session.VoxelMaps.CutOutShape(voxel, shape);
                        shape.Center = m_referenceGate.WorldMatrix.Translation + (m_referenceGate.WorldMatrix.Forward);
                        MyAPIGateway.Session.VoxelMaps.CutOutShape(voxel, shape);
                        shape.Center = testSphere.Center;
                        MyAPIGateway.Session.VoxelMaps.CutOutShape(voxel, shape);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                }
            }
        }

        void ApplyBlockDamage(IMyCubeGrid grid, BoundingSphereD testSphere)
        {
            var blocks = grid.GetBlocksInsideSphere(ref testSphere);
            var rand = new Random();
            foreach (var block in blocks)
            {
                if (block.FatBlock != null && block.FatBlock.BlockDefinition.SubtypeId.Contains("Stargate"))
                    continue;
                BoundingBoxD blockAABB;
                block.GetWorldBoundingBox(out blockAABB);

                if (blockAABB.Contains(testSphere) == ContainmentType.Contains)
                {
                    // Blocks completely inside the sphere are removed
                    block.CubeGrid.RemoveBlock(block, true);
                }
                else
                {
                    // Blocks partially inside have a random damage and deformation applied
                    var damage = (float)rand.NextDouble();
                    block.DoDamage(block.MaxIntegrity * damage, MyDamageType.Destruction, true);
                    block.CubeGrid.ApplyDestructionDeformation(block);
                }
            }
        }
    }
}
