using System;
using System.Collections;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using VRageMath;

namespace Asteroid_Territory_Spawner {

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpawnerCore : MySessionComponentBase {
        public int numTicksPerUpdate = 60;
        public int clk = 0;

        public bool sessionReady = false;

        public Logger log;

        public void Init() {

        }

        public void InitializeSession() {
            this.log = new Logger("LOG.txt");
            MyAPIGateway.Utilities.ShowNotification("Initialized", 10000, "Yello");
            this.sessionReady = true;
        }
        public override void UpdateBeforeSimulation() {
            if (this.sessionReady == false && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null) {
                this.InitializeSession();
            }

            if (this.clk == this.numTicksPerUpdate && this.sessionReady) {
                this.clk = 0;


                this.log.Log(MyAPIGateway.Session.ElapsedPlayTime.ToString());

                MyAPIGateway.Utilities.ShowNotification(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds.ToString("0.0"),990);
            }

            this.clk++;
        }

        protected override void UnloadData() {
            this.log.Close();
            base.UnloadData();
        }
    }
}
