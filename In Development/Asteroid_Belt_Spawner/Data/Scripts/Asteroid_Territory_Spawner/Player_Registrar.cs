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
    public class Player_Registrar {
        public List<MyPlayerInfo> PlayerInfos;

        public Player_Registrar() {
            this.PlayerInfos = new List<MyPlayerInfo>();
        }

        public void Init() {
            this.RegisterPlayers();
        }

        // Update the regions adjacent to these regions
        public List<Vector3I> GetRegionsThatNeedUpdate() {
            List<Vector3I> Regions = new List<Vector3I>();
            
            foreach (MyPlayerInfo player in PlayerInfos) {
                // Get regions that surround new regions and that have a field assigned to them
                if (player.InNewRegion() && player.Field != null) {
                    Regions.Add(player.Region + new Vector3I(1, 0, 0));
                    Regions.Add(player.Region + new Vector3I(-1, 0, 0));
                    Regions.Add(player.Region + new Vector3I(0, 1, 0));
                    Regions.Add(player.Region + new Vector3I(0, -1, 0));
                    Regions.Add(player.Region + new Vector3I(0, 0, 1));
                    Regions.Add(player.Region + new Vector3I(0, 0, -1));
                }
            }
            return Regions;
        }

        public void UpdatePlayerFieldLocations() {
            foreach (MyPlayerInfo player in this.PlayerInfos) {
                MyAsteroidField CurrentField = null;

                foreach (MyAsteroidField field in SpawnerCore.Fields) {
                    if (field.PositionInField(player.Player.GetPosition())) {
                        CurrentField = field;
                        break;
                    }
                }

                if (CurrentField != player.Field) {
                    bool leaving = CurrentField == null;
                    if (leaving) {
                        NotifyPlayerOnTerritoryChange(player, leaving, player.Field);
                    }
                    else {
                        NotifyPlayerOnTerritoryChange(player, leaving, CurrentField);
                    }
                }
                player.Field = CurrentField;
            }
        }

        public void NotifyPlayerOnTerritoryChange(MyPlayerInfo player, bool leaving, MyAsteroidField field) {
            string note;
            if (leaving) {
                note = "Leaving " + field.Name;
            }
            else {
                note = "Entering " + field.Name;
            }

            SpawnerCore.log.Log("Sent '" + note + "' to Player with id: " + player.Player.IdentityId.ToString());
            MyVisualScriptLogicProvider.ShowNotification(note, 5000, "White", player.Player.IdentityId);
        }

        public void RegisterPlayers() {
            SpawnerCore.log.Log("Updating player registry");

            List <MyPlayerInfo> NewPlayerInfos = new List<MyPlayerInfo>();

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

                    SpawnerCore.log.Log("Registerd new Player " + players[i].DisplayName + " with ID: " + players[i].IdentityId.ToString());
                }
            }

            this.PlayerInfos = NewPlayerInfos;
        }
    }
}
