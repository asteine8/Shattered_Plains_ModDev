using System;
using System.Collections.Generic;
using System.Text;
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
    class MyPlayerInfo {

        public IMyPlayer Player;
        public Vector3D LastKnownPosition;
        public double DistanceTraveled;

        public MyPlayerInfo(IMyPlayer player) {
            this.Player = player;
            this.LastKnownPosition = this.Player.GetPosition();
            this.DistanceTraveled = 0;
        }

        public void UpdatePosition() {
            Vector3D CurrentPosition = this.Player.GetPosition();
            this.DistanceTraveled += (this.LastKnownPosition - CurrentPosition).Length();
            this.LastKnownPosition = CurrentPosition;
        }
        public double GetDistanceTraveled() {
            double temp = this.DistanceTraveled;
            this.DistanceTraveled = 0;
            return temp;
        }
    }
}
