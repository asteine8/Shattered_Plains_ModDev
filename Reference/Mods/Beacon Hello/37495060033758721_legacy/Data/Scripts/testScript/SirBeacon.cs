using Sandbox.ModAPI;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication2
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    public class MyBeaconLogic : MyGameLogicComponent
    {
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase m_objectBuilder = null;
        private bool m_greeted;
        public override void Close()
        {
        }

        public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_objectBuilder = objectBuilder;
        }

        public override void MarkForClose()
        {
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void UpdateAfterSimulation10()
        {
        }

        public override void UpdateAfterSimulation100()
        {
        }

        public override void UpdateBeforeSimulation()
        {
        }

        public override void UpdateBeforeSimulation10()
        {
        }

        public override void UpdateBeforeSimulation100()
        {
            if (MyAPIGateway.Session.Player == null)
            {
                return;
            }
            if ((MyAPIGateway.Session.Player.GetPosition() - Entity.GetPosition()).Length() < 10)
            {
                if (!m_greeted)
                {
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Hello I am {0}", (Entity as Sandbox.ModAPI.Ingame.IMyTerminalBlock).DisplayNameText), 1000, MyFontEnum.Red);
                    m_greeted = true;
                }
            }
            else
                m_greeted = false;
        }

        public override void UpdateOnceBeforeFrame()
        {
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }
    }
}
