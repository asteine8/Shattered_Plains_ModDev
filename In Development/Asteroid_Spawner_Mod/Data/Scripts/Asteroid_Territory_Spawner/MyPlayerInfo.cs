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
    public class MyPlayerInfo {

        public IMyPlayer Player;

        public Vector3I Region;

        public MyPlayerInfo(IMyPlayer player) {
            this.Player = player;
            this.Region = Vector3I.Zero;
        }

        public bool InNewRegion() {
            Vector3I currentRegion = VoxelSpawner.GetRegionFromPosition(this.Player.GetPosition());
            if (currentRegion != this.Region) {
                this.Region = currentRegion; // Update the region
                return true;
            }
            else {
                return false;
            }
        }
    }
}
