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
    public struct Deposit {
        public double SpawnProb;
        public string ResourceType;

        public Deposit(string type) {
            this.SpawnProb = 1;
            this.ResourceType = type;
        }

        public Deposit(string type, double prob) {
            this.SpawnProb = prob;
            this.ResourceType = type;
        }
    }
}
