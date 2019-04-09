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
    class Player_Registrar {
        public List<MyPlayerInfo> PlayerInfos;
        public Player_Registrar() {
            this.PlayerInfos = new List<MyPlayerInfo>();
        }

        public void Init() {
            this.RegisterPlayers();
        }

        public void UpdateDistanceTraveled() {
            foreach(MyPlayerInfo playerInfo in this.PlayerInfos) {
                playerInfo.UpdatePosition();
            }
        }

        public void RegisterPlayers() {
            List<MyPlayerInfo> NewPlayerInfos = new List<MyPlayerInfo>();

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            for (int i = 0; i < players.Count; i++) {
                // Don't register non-human characters
                if (players[i].IsBot || players[i].Character == null) {continue;}
                if (players[i].Character.IsDead) {continue;}

                bool isRegistered = false;
                foreach(MyPlayerInfo playerInfo in this.PlayerInfos) {
                    if (playerInfo.Player.IdentityId == players[i].IdentityId) {
                        // Player already is registered
                        isRegistered = true;
                        NewPlayerInfos.Add(playerInfo);
                        break;
                    }
                }
                // We need to add this player to the registry
                if (!isRegistered) {
                    NewPlayerInfos.Add(new MyPlayerInfo(players[i]));
                }
            }

            this.PlayerInfos = NewPlayerInfos;
        }
    }
}
