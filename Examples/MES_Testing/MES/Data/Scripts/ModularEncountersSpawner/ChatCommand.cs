using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using ModularEncountersSpawner;
using ModularEncountersSpawner.Configuration;
using ModularEncountersSpawner.Templates;
using ModularEncountersSpawner.Spawners;

namespace ModularEncountersSpawner{
	
	public static class ChatCommand{
				
		public static void MESChatCommand(string messageText, ref bool sendToOthers){
			
			var thisPlayer = MyAPIGateway.Session.LocalHumanPlayer;
			bool isAdmin = false;
			
			if(thisPlayer == null){
				
				return;
				
			}
			
			if(thisPlayer.PromoteLevel == MyPromoteLevel.Admin || thisPlayer.PromoteLevel == MyPromoteLevel.Owner){
				
				isAdmin = true;
				
			}
			
			if(isAdmin == false){
				
				MyVisualScriptLogicProvider.ShowNotification("Access Denied. Spawner Chat Commands Only Available To Admin Players.", 5000, "Red", thisPlayer.IdentityId);
				return;
				
			}
			
			if(messageText.StartsWith("/MES.") == true){
				
				sendToOthers = false;
				var chatMsg = "MESChatMsg\n" + thisPlayer.IdentityId.ToString() + "\n" + thisPlayer.SteamUserId.ToString() + "\n" + messageText;
				var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(chatMsg);
				var sendMsg = MyAPIGateway.Multiplayer.SendMessageToServer(8877, sendData);
				
			}
			
		}
		
		public static void MESMessageHandler(byte[] data){
			
			var receivedData = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
			
			if(receivedData.StartsWith("MESChatMsg") == true){
				
				ServerChatProcessing(receivedData);
				
			}
			
			if(receivedData.StartsWith("MESClipboard") == true){
				
				ClipboardProcessing(receivedData);
				
			}
			
			if(receivedData.StartsWith("MESBossGPS") == true){
				
				BossEncounterGpsManager(receivedData);
				
			}
			
			if(receivedData.StartsWith("ClientGetBossGPS") == true){
				
				ClientGetBossGPS(receivedData);
				
			}
			
			
		}
		
		public static void BossEncounterGpsManager(string receivedMsg){
			
			var dataSplit = receivedMsg.Split('\n');
			
			if(dataSplit.Length != 3){
				
				return;
				
			}
			
			var player = MyAPIGateway.Session.LocalHumanPlayer;
			
			if(player == null){
			
				return;
				
			}
			
			if(dataSplit[0] == "MESBossGPSRemove"){
								
				if(MES_SessionCore.BossEncounterGps != null){
					
					try{
						
						MyAPIGateway.Session.GPS.RemoveLocalGps(MES_SessionCore.BossEncounterGps);
						MES_SessionCore.BossEncounterGps = null;
						
					}catch(Exception exc){
						
						
						
					}
					
				}
				
				return;
				
			}
			
			if(dataSplit[0] != "MESBossGPSCreate"){
				
				return;
				
			}
			
			var gpsCoords = Vector3D.Zero;
			
			if(Vector3D.TryParse(dataSplit[2], out gpsCoords) == false){
				
				return;
				
			}
			
			foreach(var gps in MyAPIGateway.Session.GPS.GetGpsList(player.IdentityId)){
				
				if(gps.Coords == gpsCoords){
					
					Logger.AddMsg("Boss Encounter GPS Or Other GPS Already Exist At Coordinates.");
					return;
					
				}
				
			}
			
			Logger.AddMsg("Boss Encounter GPS Created.");
			MES_SessionCore.BossEncounterGps = MyAPIGateway.Session.GPS.Create(dataSplit[1], "", gpsCoords, true);
			
			try{
				
				MyAPIGateway.Session.GPS.AddLocalGps(MES_SessionCore.BossEncounterGps);
				MyVisualScriptLogicProvider.SetGPSColor(dataSplit[1], new Color(255,55,255), player.IdentityId);
				
			}catch(Exception exp){
				
				
				
			}

		}
		
		public static void ClientGetBossGPS(string receivedMsg){
			
			var dataSplit = receivedMsg.Split('\n');
			
			if(dataSplit.Length != 4){
				
				return;
				
			}
			
			long playerId = 0;
			ulong steamId = 0;
			string msg = dataSplit[3];
			
			if(long.TryParse(dataSplit[1], out playerId) == false || ulong.TryParse(dataSplit[2], out steamId) == false){
				
				return;
				
			}
			
			foreach(var boss in NPCWatcher.BossEncounters){
				
				if(boss.PlayersInEncounter.Contains(playerId) == true){
					
					string clientPayload = "MESBossGPSCreate\n";
					clientPayload += boss.GpsTemplate.Name + "\n";
					clientPayload += boss.GpsTemplate.Coords.ToString();
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(clientPayload);
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					
				}
				
			}
			
		}
		
		public static void GetThreatScore(string receivedMsg){
			
			// /MES.GetThreatScore.SpawnGroup
			var dataSplit = receivedMsg.Split('\n');
			
			if(dataSplit.Length != 4){
				
				return;
				
			}
			
			long playerId = 0;
			ulong steamId = 0;
			string msg = dataSplit[3];
			
			if(long.TryParse(dataSplit[1], out playerId) == false || ulong.TryParse(dataSplit[2], out steamId) == false){
				
				return;
				
			}
			
			foreach(var boss in NPCWatcher.BossEncounters){
				
				if(boss.PlayersInEncounter.Contains(playerId) == true){
					
					string clientPayload = "MESBossGPSCreate\n";
					clientPayload += boss.GpsTemplate.Name + "\n";
					clientPayload += boss.GpsTemplate.Coords.ToString();
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(clientPayload);
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					
				}
				
			}
			
		}
		
		public static void ClipboardProcessing(string receivedMsg){
			
			receivedMsg = receivedMsg.Replace("MESClipboard", "");
			var player = MyAPIGateway.Session.LocalHumanPlayer;
			
			if(player == null || string.IsNullOrEmpty(receivedMsg) == true){
				
				return;
				
			}
						
			VRage.Utils.MyClipboardHelper.SetClipboard(receivedMsg);
			
		}
				
		public static void ServerChatProcessing(string receivedMsg){
			
			var dataSplit = receivedMsg.Split('\n');
			
			if(dataSplit.Length != 4){
				
				return;
				
			}
			
			long playerId = 0;
			ulong steamId = 0;
			string msg = dataSplit[3];
			
			if(long.TryParse(dataSplit[1], out playerId) == false || ulong.TryParse(dataSplit[2], out steamId) == false){
				
				return;
				
			}
			
			//Debug Commands
			if(msg.StartsWith("/MES.") == true){
				
				//Enable Debug Mode
				if(msg.StartsWith("/MES.EnableDebugMode.") == true){
					
					var msgSplit = msg.Split('.');
					
					if(msgSplit.Length != 3){
						
						MyVisualScriptLogicProvider.ShowNotification("Invalid Command Received", 5000, "White", playerId);
						return;
						
					}
					
					bool mode = false;
					
					if(bool.TryParse(msgSplit[2], out mode) == false){
						
						MyVisualScriptLogicProvider.ShowNotification("Invalid Command Received", 5000, "White", playerId);
						return;
						
					}
					
					Logger.LoggerDebugMode = mode;
					MyVisualScriptLogicProvider.ShowNotification("Debug Mode Enabled: " + mode.ToString(), 5000, "White", playerId);
					return;
					
				}
				
				//Settings
				if(msg.StartsWith("/MES.Settings.") == true){
					
					var result = SettingsEditor.EditSettings(msg);
					MyVisualScriptLogicProvider.ShowNotification(result, 5000, "White", playerId);
					return;
					
				}
				
				//TryRandomSpawn
				if(msg.StartsWith("/MES.Spawn.") == true){
					
					var playerList = new List<IMyPlayer>();
					MyAPIGateway.Players.GetPlayers(playerList);
					IMyPlayer thisPlayer = null;
					
					foreach(var player in playerList){
						
						if(player.IdentityId == playerId){
							
							thisPlayer = player;
							break;
							
						}
						
					}
					
					if(thisPlayer == null){
						
						MyVisualScriptLogicProvider.ShowNotification("Could Not Spawn Encounter: Player Not In Watch List", 5000, "White", playerId);
						return;
						
					}
					
					bool success = false;
					
					if(msg.Contains("SpaceCargoShip") == true || msg.Contains("AllSpawns") == true){
						
						if(MES_SessionCore.playerWatchList.ContainsKey(thisPlayer) == true){
							
							SpawnGroupManager.AdminSpawnGroup = SpecificSpawnGroupRequest(msg, "SpaceCargoShip");
							MES_SessionCore.PlayerWatcherTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].SpaceCargoShipTimer = 0;
							MyVisualScriptLogicProvider.ShowNotification("Attempting Random Spawn: Space Cargo Ship", 5000, "White", playerId);
							success = true;
							
						}
						
					}
					
					if(msg.Contains("PlanetaryCargoShip") == true){
						
						if(MES_SessionCore.playerWatchList.ContainsKey(thisPlayer) == true){
							
							SpawnGroupManager.AdminSpawnGroup = SpecificSpawnGroupRequest(msg, "PlanetaryCargoShip");
							MES_SessionCore.playerWatchList[thisPlayer].AtmoCargoShipTimer = 0;
							MES_SessionCore.PlayerWatcherTimer = 0;
							MyVisualScriptLogicProvider.ShowNotification("Attempting Random Spawn: Planetary Cargo Ship", 5000, "White", playerId);
							success = true;
							
						}
						
					}
					
					if(msg.Contains("RandomEncounter") == true){
						
						if(MES_SessionCore.playerWatchList.ContainsKey(thisPlayer) == true){
							
							SpawnGroupManager.AdminSpawnGroup = SpecificSpawnGroupRequest(msg, "RandomEncounter");
							
							MES_SessionCore.PlayerWatcherTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].RandomEncounterCheckTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].RandomEncounterCoolDownTimer = 0;
							var fakeDistance = Settings.RandomEncounters.PlayerTravelDistance + 1000;
							MES_SessionCore.playerWatchList[thisPlayer].RandomEncounterDistanceCoordCheck = fakeDistance * Vector3D.Up + thisPlayer.GetPosition();
							MyVisualScriptLogicProvider.ShowNotification("Attempting Random Spawn: Random Encounter", 5000, "White", playerId);
							success = true;
							
						}
						
					}
					
					if(msg.Contains("PlanetaryInstallation") == true){
						
						if(MES_SessionCore.playerWatchList.ContainsKey(thisPlayer) == true){
							
							SpawnGroupManager.AdminSpawnGroup = SpecificSpawnGroupRequest(msg, "PlanetaryInstallation");
							
							MES_SessionCore.PlayerWatcherTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].PlanetaryInstallationCheckTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].PlanetaryInstallationCooldownTimer = 0;
							
							var fakeDistance = Settings.PlanetaryInstallations.PlayerDistanceSpawnTrigger + 1000;
							var randomDir = SpawnResources.GetRandomCompassDirection(thisPlayer.GetPosition(), SpawnResources.GetNearestPlanet(thisPlayer.GetPosition()));
							
							MES_SessionCore.playerWatchList[thisPlayer].InstallationDistanceCoordCheck = fakeDistance * randomDir + thisPlayer.GetPosition();
							MyVisualScriptLogicProvider.ShowNotification("Attempting Random Spawn: Planetary Installation", 5000, "White", playerId);
							success = true;
							
						}
						
					}
					
					if(msg.Contains("BossEncounter") == true){
						
						if(BossEncounterSpawner.IsPlayerInBossEncounter(thisPlayer.IdentityId) == true){
							
							MyVisualScriptLogicProvider.ShowNotification("Boss Encounter Already Active", 5000, "White", playerId);
							
						}
						
						if(MES_SessionCore.playerWatchList.ContainsKey(thisPlayer) == true){
							
							SpawnGroupManager.AdminSpawnGroup = SpecificSpawnGroupRequest(msg, "BossEncounter");
							
							MES_SessionCore.PlayerWatcherTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].BossEncounterCooldownTimer = 0;
							MES_SessionCore.playerWatchList[thisPlayer].BossEncounterCheckTimer = 0;
							MyVisualScriptLogicProvider.ShowNotification("Attempting Random Spawn: Boss Encounter", 5000, "White", playerId);
							success = true;
							
						}
						
					}
					
					if(success == false){
						
						MyVisualScriptLogicProvider.ShowNotification("Could Not Spawn Encounter: Player Not In Watch List", 5000, "White", playerId);
						
					}
					
					return;
					
				}
				
				//Enable Territory
				if(msg.StartsWith("/MES.EnableTerritory.") == true){
					
					var messageReplace = msg.Replace("/MES.EnableTerritory.", "");
					
					if(messageReplace == ""){
						
						MyVisualScriptLogicProvider.ShowNotification("Invalid Command Received: No Territory Name Provided", 5000, "White", playerId);
						return;
						
					}
					
					MyAPIGateway.Utilities.SetVariable<bool>("MES-Territory-" + messageReplace, true);
					TerritoryManager.TerritoryRefresh();
					MyVisualScriptLogicProvider.ShowNotification("Territory Enabled: " + messageReplace, 5000, "White", playerId);
					return;
					
				}
				
				//Disable Territory
				if(msg.StartsWith("/MES.DisableTerritory.") == true){
					
					var messageReplace = msg.Replace("/MES.DisableTerritory.", "");
					
					if(messageReplace == ""){
						
						MyVisualScriptLogicProvider.ShowNotification("Invalid Command Received: No Territory Name Provided", 5000, "White", playerId);
						return;
						
					}
					
					MyAPIGateway.Utilities.SetVariable<bool>("MES-Territory-" + messageReplace, false);
					TerritoryManager.TerritoryRefresh();
					MyVisualScriptLogicProvider.ShowNotification("Territory Disabled: " + messageReplace, 5000, "White", playerId);
					return;
					
				}
								
				//Get SpawnGroups
				if(msg.StartsWith("/MES.GetSpawnGroups") == true){
					
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>("MESClipboard" + Logger.SpawnGroupResults());
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					MyVisualScriptLogicProvider.ShowNotification("Spawn Group Data To Clipboard. Success: " + sendStatus.ToString(), 5000, "White", playerId);
					return;
					
				}
				
				//Get Active NPCs
				if(msg.StartsWith("/MES.GetActiveNPCs") == true){
					
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>("MESClipboard" + Logger.GetActiveNPCs());
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					MyVisualScriptLogicProvider.ShowNotification("Active NPC Data To Clipboard. Success: " + sendStatus.ToString(), 5000, "White", playerId);
					return;
					
				}
				
				//Get Player Watch Lists
				if(msg.StartsWith("/MES.GetPlayerWatchList") == true){
					
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>("MESClipboard" + Logger.GetPlayerWatcherData());
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					MyVisualScriptLogicProvider.ShowNotification("Player Watch Data To Clipboard. Success: " + sendStatus.ToString(), 5000, "White", playerId);
					return;
					
				}
				
				//Get Player Watch Lists
				if(msg.StartsWith("/MES.GetThreatScore") == true){
					
					var messageReplace = msg.Replace("/MES.GetThreatScore.", "");
					
					if(messageReplace == ""){
						
						MyVisualScriptLogicProvider.ShowNotification("Invalid Command Received: No SpawnGroup Name Provided", 5000, "White", playerId);
						return;
						
					}
					
					var playerList = new List<IMyPlayer>();
					MyAPIGateway.Players.GetPlayers(playerList);
					IMyPlayer thisPlayer = null;
					
					foreach(var player in playerList){
						
						if(player.IdentityId == playerId){
							
							thisPlayer = player;
							break;
							
						}
						
					}
					
					if(thisPlayer == null){
						
						MyVisualScriptLogicProvider.ShowNotification("Command Failed: Apparently you don't exist?", 5000, "White", playerId);
						return;
						
					}
					
					ImprovedSpawnGroup selectedSpawnGroup = null;
					
					foreach(var spawnGroup in SpawnGroupManager.SpawnGroups){
						
						if(spawnGroup.SpawnGroup.Id.SubtypeName == messageReplace){
							
							selectedSpawnGroup = spawnGroup;
							break;
							
						}
						
					}
					
					if(selectedSpawnGroup == null){
						
						MyVisualScriptLogicProvider.ShowNotification("Could Not Find SpawnGroup With Name: " + messageReplace, 5000, "White", playerId);
						return;
						
					}
					
					SpawnResources.RefreshEntityLists();
					SpawnResources.LastThreatRefresh = SpawnResources.GameStartTime;
					var threatLevel = SpawnResources.GetThreatLevel(selectedSpawnGroup, thisPlayer.GetPosition());
					
					MyVisualScriptLogicProvider.ShowNotification("Threat Level Score Near You: " + threatLevel.ToString(), 5000, "White", playerId);
					return;
					
				}
				
				//Reset Active Territories
				if(msg.StartsWith("/MES.ResetActiveTerritories") == true){
					
					TerritoryManager.TerritoryRefresh(true);
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>("MESResetActiveTerritories");
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					MyVisualScriptLogicProvider.ShowNotification("Active Territories Reset To Default Values.", 5000, "White", playerId);
					return;
					
				}
				
				//Get Spawned Unique Encounters
				if(msg.StartsWith("/MES.GetSpawnedUniqueEncounters") == true){
					
					var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>("MESClipboard" + Logger.GetSpawnedUniqueEncounters());
					bool sendStatus = MyAPIGateway.Multiplayer.SendMessageTo(8877, sendData, steamId);
					MyVisualScriptLogicProvider.ShowNotification("Spawned Unique Encounters List Sent To Clipboard. Success: " + sendStatus.ToString(), 5000, "White", playerId);
					return;
					
				}
				
			}
			
			//Settings Commands
			
		}
		
		public static string SpecificSpawnGroupRequest(string msg, string spawnType){
			
			if(msg.Contains(spawnType + ".") == false){
				
				return "";
				
			}
			
			var result = msg.Replace("/MES.Spawn." + spawnType + ".", "");
			
			if(string.IsNullOrEmpty(result) == true){
				
				return "";
				
			}

			return result;
			
		}
		
	}
	
}