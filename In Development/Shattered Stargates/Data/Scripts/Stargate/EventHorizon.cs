using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Phoenix.Stargate
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "Phoenix_Stargate_EventHorizon_Reactor")]
    public class EventHorizon : MyGameLogicComponent
    {
        private MyObjectBuilder_EntityBase m_objectBuilder;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!Globals.ModEnabled)
                return;

            if ((Container.Entity as IMyReactor).CubeGrid.DisplayName == "Iris")
                return;

            m_objectBuilder = objectBuilder;

            if ((Container.Entity as IMyFunctionalBlock) == null)
                return;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (!Globals.ModEnabled)
                return;

            if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.Player == null)
                return;

            // Disable physics
            var parent = (Container.Entity as IMyCubeBlock).CubeGrid;
            if (parent.Physics != null)
            {
                parent.Physics.Deactivate();
            }
            var blocks = new List<IMySlimBlock>();
            parent.GetBlocks(blocks, (x) => x.FatBlock != null && x.FatBlock is IMyFunctionalBlock);

            foreach (var block in blocks)
            {
                if (block.FatBlock is IMyRadioAntenna)
                    continue;

                (block.FatBlock as IMyFunctionalBlock).GetActionWithName("OnOff_On").Apply(block.FatBlock);

                if (block.FatBlock is IMyLaserAntenna)
                    (block.FatBlock as IMyLaserAntenna).Connect();
            }

            if (StargateAdmin.Configuration.VortexVisible && Globals.ModEnabled)
            {
                // Spawn vortex
                var gridObjectBuilder = new MyObjectBuilder_CubeGrid()
                {
                    PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                    GridSizeEnum = MyCubeSize.Large,
                    IsStatic = true,
                    CreatePhysics = false,
                    LinearVelocity = new SerializableVector3(0, 0, 0),
                    AngularVelocity = new SerializableVector3(0, 0, 0),
                    PositionAndOrientation = new MyPositionAndOrientation(Container.Entity.GetPosition(), Container.Entity.WorldMatrix.Backward, Container.Entity.WorldMatrix.Up),
                    DisplayName = "Event Horizon Vortex"
                };

                MyObjectBuilder_Reactor cube = new MyObjectBuilder_Reactor()
                {
                    Min = new SerializableVector3I(0, 0, 0),
                    SubtypeName = "Phoenix_Stargate_EventHorizon_Vortex",
                    ColorMaskHSV = new SerializableVector3(0, -1, 0),
                    EntityId = 0,
                    Owner = 0,
                    BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up),
                    ShareMode = MyOwnershipShareModeEnum.All,
                    CustomName = "Event Horizon Vortex",
                };
                gridObjectBuilder.CubeBlocks.Add(cube);
                var tempList = new List<MyObjectBuilder_EntityBase>();
                tempList.Add(gridObjectBuilder);
                MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
                //tempList.ForEach(item => MyAPIGateway.Entities.CreateFromObjectBuilder(item));
                if (MyAPIGateway.Session.Player != null)
                {
                    var entity = MyAPIGateway.Entities.CreateFromObjectBuilder(tempList[0]);
                    (entity as MyEntity).IsPreview = true;
                    MyAPIGateway.Entities.AddEntity(entity, true);
                }
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? (MyObjectBuilder_EntityBase)m_objectBuilder.Clone() : m_objectBuilder;
        }
    }
}
