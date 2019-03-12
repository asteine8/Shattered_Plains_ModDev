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
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class MES_SessionCore : MySessionComponentBase{
		
		public static float ModVersion = 1.028f;
		public static int PlayerWatcherTimer = 0;
		public static Dictionary<IMyPlayer, PlayerWatcher> playerWatchList = new Dictionary<IMyPlayer, PlayerWatcher>();
		public static List<IMyPlayer> PlayerList = new List<IMyPlayer>();
		public static List<ulong> ActiveMods = new List<ulong>();
		
		public static long modId = 1521905890;
		
		public static IMyGps BossEncounterGps;
		
		public static bool NPCWeaponUpgradesModDetected = false;
		public static bool spawningInProgress = false;
		
		bool scriptInit = false;
		bool scriptFail = false;
		int tickCounter = 0;
		int tickCounterIncrement = 1;
		Random rnd = new Random();
		
		[Serializable]
		public struct SyncContents{
			
			long PlayerId;
			ulong SteamId;
			string MessageType;
			string Message;
			
		}
		
		public override void UpdateBeforeSimulation(){
			
			if(scriptInit == false){
				
				scriptInit = true;
				SetupScript();
				
			}
			
			if(scriptFail == true){
				
				return;
				
			}
						
			tickCounter += tickCounterIncrement;
			
			if(tickCounter < 60){
				
				return;
				
			}
			
			tickCounter = 0;
						
			if(MyAPIGateway.Multiplayer.IsServer == false){
				
				return;
				
			}

			PlayerWatcherTimer--;
			
			if(PlayerWatcherTimer <= 0){
				
				PlayerWatcherTimer = Settings.General.PlayerWatcherTimerTrigger;
				ProcessPlayerWatchList();
				
			}
			
			TerritoryManager.TerritoryWatcher();
			NPCWatcher.BossSignalWatcher();
			NPCWatcher.ActiveNpcMonitor();

		}
		
		public void SetupScript(){
			
			//Setup Watchers and Handlers
			MyAPIGateway.Multiplayer.RegisterMessageHandler(8877, ChatCommand.MESMessageHandler);
			MyAPIGateway.Utilities.MessageEntered += ChatCommand.MESChatCommand;	
			var thisPlayer = MyAPIGateway.Session.LocalHumanPlayer;

			if(MyAPIGateway.Multiplayer.IsServer == false){

				if(thisPlayer == null){
					
					Logger.AddMsg("Player Doesn't Exist. Cannot Search For Existing Boss GPS.");
					return;
					
				}

				Logger.AddMsg("Searching For Existing Boss Encounter GPS.");
				var chatMsg = "MESClientGetBossGPS\n" + thisPlayer.IdentityId.ToString() + "\n" + thisPlayer.SteamUserId.ToString() + "\n" + "Msg";
				var sendData = MyAPIGateway.Utilities.SerializeToBinary<string>(chatMsg);
				var sendMsg = MyAPIGateway.Multiplayer.SendMessageToServer(8877, sendData);
				
				return;
				
			}
			
			MyAPIGateway.Utilities.RegisterMessageHandler(1521905890, ModMessages.ModMessageHandler);
			
			Settings.InitSettings();
			NPCWatcher.InitFactionData();
			TerritoryManager.TerritoryRefresh();
			SpawnGroupManager.CreateSpawnLists();
			
			string[] uniqueSpawnedArray = new string[0];
			if(MyAPIGateway.Utilities.GetVariable<string[]>("MES-UniqueGroupsSpawned", out uniqueSpawnedArray) == true){
				
				SpawnGroupManager.UniqueGroupsSpawned = new List<string>(uniqueSpawnedArray.ToList());
				
			}else{
				
				Logger.AddMsg("Failed To Retrieve Previously Spawned Unique Encounters List or No Unique Encounters Have Spawned Yet.");
				
			}
			
			bool conflictingSettings = false;
			
			//Check Session Settings
			if(MyAPIGateway.Session.SessionSettings.CargoShipsEnabled == true){
				
				string msgA = "Conflicting World Settings Detected: Cargo Ships Enabled";
				MyVisualScriptLogicProvider.ShowNotificationToAll(msgA, 15000, "Red");
				Logger.AddMsg(msgA);
				conflictingSettings = true;
				
			}
			
			if(MyAPIGateway.Session.SessionSettings.EnableEncounters == true){
				
				string msgA = "Conflicting World Settings Detected: Random Encounters Enabled";
				MyVisualScriptLogicProvider.ShowNotificationToAll(msgA, 15000, "Red");
				Logger.AddMsg(msgA);
				conflictingSettings = true;
				
			}

			//Get Active Mods
			foreach(var mod in MyAPIGateway.Session.Mods){
				
				if(mod.PublishedFileId != 0){
					
					ActiveMods.Add(mod.PublishedFileId);
					
				}
				
				/*if(mod.PublishedFileId == 1135484377 || mod.PublishedFileId == 973528334){
					
					string msgA = "Conflicting Mod Detected: " + mod.FriendlyName;
					MyVisualScriptLogicProvider.ShowNotificationToAll(msgA, 15000, "Red");
					Logger.AddMsg(msgA);
					conflictingSettings = true;
					
				}*/
				
			}
			
			if(ActiveMods.Contains(1555044803) == true){
				
				Logger.AddMsg("NPC Weapon Upgrades Mod Detected. Enabling Weapon Randomization.");
				NPCWeaponUpgradesModDetected = true;
				
			}
			
			if(conflictingSettings == true){
				
				string msgB = "Modular Encounters Spawner Handles The Functionality Of Conflicting Settings.";
				string msgC = "Please Remove Conflicting Settings Listed Above To Ensure Proper Behavior.";
				MyVisualScriptLogicProvider.ShowNotificationToAll(msgB, 15000, "Red");
				Logger.AddMsg(msgB);
				MyVisualScriptLogicProvider.ShowNotificationToAll(msgC, 15000, "Red");
				Logger.AddMsg(msgC);
				
			}
				
			//Init Timers
			PlayerWatcherTimer = Settings.General.PlayerWatcherTimerTrigger;
			NPCWatcher.NpcDistanceCheckTimer = Settings.General.NpcDistanceCheckTimerTrigger;
			NPCWatcher.NpcOwnershipCheckTimer = Settings.General.NpcOwnershipCheckTimerTrigger;
			NPCWatcher.NpcCleanupCheckTimer = Settings.General.NpcCleanupCheckTimerTrigger;
			NPCWatcher.SpawnedVoxelCheckTimer = Settings.General.SpawnedVoxelCheckTimerTrigger;
			SpawnResources.RefreshEntityLists();
			
			//Setup Watchers and Handlers
			MyAPIGateway.Entities.OnEntityAdd += NPCWatcher.NewEntityDetected;
			
			//Get Initial Players
			PlayerList.Clear();
			MyAPIGateway.Players.GetPlayers(PlayerList);
			
			//Get Existing NPCs
			NPCWatcher.StartupScan();
			
			//Get Spawned Voxels From Save
			string[] tempSpawnedVoxels = new string[0];
			
			if(MyAPIGateway.Utilities.GetVariable<string[]>("MES-SpawnedVoxels", out tempSpawnedVoxels) == true){
				
				foreach(var voxelId in tempSpawnedVoxels){
					
					long tempId = 0;
					
					if(long.TryParse(voxelId, out tempId) == false){
						
						continue;
						
					}
					
					IMyEntity voxelEntity = null;
					
					if(MyAPIGateway.Entities.TryGetEntityById(tempId, out voxelEntity) == false){
						
						continue;
						
					}
					
					if(NPCWatcher.SpawnedVoxels.ContainsKey(voxelId) == false){
						
						NPCWatcher.SpawnedVoxels.Add(voxelId, voxelEntity);
						
					}
	
				}
				
			}
						
		}
		
		public void ProcessPlayerWatchList(){
			
			PlayerList.Clear();
			MyAPIGateway.Players.GetPlayers(PlayerList);
			
			foreach(var player in PlayerList){
				
				if(player.IsBot == true || player.Character == null){
					
					continue;
					
				}
				
				if(playerWatchList.ContainsKey(player) == true){

					//Regular Timers
					
					if(Settings.General.EnableSpaceCargoShips == true){
						
						playerWatchList[player].SpaceCargoShipTimer -= Settings.General.PlayerWatcherTimerTrigger;
						
					}
					
					if(Settings.General.EnablePlanetaryCargoShips == true){
						
						playerWatchList[player].AtmoCargoShipTimer -= Settings.General.PlayerWatcherTimerTrigger;
						
					}
					
					if(Settings.General.EnableRandomEncounters == true){
						
						//CoolDown Timers
						if(playerWatchList[player].RandomEncounterCoolDownTimer > 0){
							
							playerWatchList[player].RandomEncounterCoolDownTimer -= Settings.General.PlayerWatcherTimerTrigger;
							
						}else{
							
							playerWatchList[player].RandomEncounterCheckTimer -= Settings.General.PlayerWatcherTimerTrigger;
							
						}
						
						if(playerWatchList[player].RandomEncounterDistanceCoordCheck == Vector3D.Zero){
						
							playerWatchList[player].RandomEncounterDistanceCoordCheck = player.GetPosition();
							
						}
						
					}
					
					if(Settings.General.EnablePlanetaryInstallations  == true){
						
						if(playerWatchList[player].PlanetaryInstallationCooldownTimer > 0){
							
							playerWatchList[player].PlanetaryInstallationCooldownTimer -= Settings.General.PlayerWatcherTimerTrigger;
							
						}else{
							
							playerWatchList[player].PlanetaryInstallationCheckTimer -= Settings.General.PlayerWatcherTimerTrigger;
							
						}
						
					}
					
					if(Settings.General.EnableBossEncounters   == true){
						
						if(BossEncounterSpawner.IsPlayerInBossEncounter(player.IdentityId) == false){
							
							if(playerWatchList[player].BossEncounterCooldownTimer > 0){
							
								playerWatchList[player].BossEncounterCooldownTimer -= Settings.General.PlayerWatcherTimerTrigger;
								
							}else{
								
								playerWatchList[player].BossEncounterCheckTimer -= Settings.General.PlayerWatcherTimerTrigger;
								
							}
							
						}
						
					}
					
					//Apply Increment to Timers and Engage Spawners When Appropriate
					if(playerWatchList[player].SpaceCargoShipTimer <= 0 && spawningInProgress == false){
						
						spawningInProgress = true;
						//MyAPIGateway.Parallel.Start(delegate{
							
							playerWatchList[player].SpaceCargoShipTimer = rnd.Next(Settings.SpaceCargoShips.MinSpawnTime, Settings.SpaceCargoShips.MaxSpawnTime);
							Logger.SkipNextMessage = true;
							Logger.AddMsg("Attempting Space/Lunar Cargo Ship Spawn Near Player: " + player.DisplayName);
							Logger.SkipNextMessage = true;
							var spawnResult = SpaceCargoShipSpawner.AttemptSpawn(player.GetPosition());
							Logger.AddMsg(spawnResult);
							spawningInProgress = false;
						
						//});
						
					}
					
					if(playerWatchList[player].AtmoCargoShipTimer <= 0){
						
						playerWatchList[player].AtmoCargoShipTimer = rnd.Next(Settings.PlanetaryCargoShips.MinSpawnTime, Settings.PlanetaryCargoShips.MaxSpawnTime);
						Logger.SkipNextMessage = true;
						Logger.AddMsg("Attempting Planetary Cargo Ship Spawn Near Player: " + player.DisplayName);
						Logger.SkipNextMessage = true;
						var spawnResult = PlanetaryCargoShipSpawner.AttemptSpawn(player.GetPosition());
						Logger.AddMsg(spawnResult);
						
					}
					
					if(playerWatchList[player].RandomEncounterCheckTimer <= 0 && playerWatchList[player].RandomEncounterCoolDownTimer <= 0){
						
						playerWatchList[player].RandomEncounterCheckTimer = Settings.RandomEncounters.SpawnTimerTrigger;
						
						if(Vector3D.Distance(player.GetPosition(), playerWatchList[player].RandomEncounterDistanceCoordCheck) >= Settings.RandomEncounters.PlayerTravelDistance){
							
							playerWatchList[player].RandomEncounterDistanceCoordCheck = player.GetPosition();
							Logger.SkipNextMessage = true;
							Logger.AddMsg("Attempting Random Encounter Spawn Near Player: " + player.DisplayName);
							Logger.SkipNextMessage = true;
							var spawnResult = RandomEncounterSpawner.AttemptSpawn(player.GetPosition());
							Logger.AddMsg(spawnResult);
							
							if(spawnResult.StartsWith("Spawning Group - ") == true){
								
								playerWatchList[player].RandomEncounterCoolDownTimer = Settings.RandomEncounters.PlayerSpawnCooldown;
								
							}
							
						}
						
					}
					
					if(playerWatchList[player].PlanetaryInstallationCheckTimer <= 0 && playerWatchList[player].PlanetaryInstallationCooldownTimer <= 0 ){
						
						playerWatchList[player].PlanetaryInstallationCheckTimer = Settings.PlanetaryInstallations.SpawnTimerTrigger;
						Logger.SkipNextMessage = true;
						Logger.AddMsg("Attempting Planetary Installation Spawn Near Player: " + player.DisplayName);
						Logger.SkipNextMessage = true;
						var spawnResult = PlanetaryInstallationSpawner.AttemptSpawn(player.GetPosition(), player);
						Logger.AddMsg(spawnResult);
						
						if(spawnResult.StartsWith("Spawning Group - ") == true){
							
							playerWatchList[player].PlanetaryInstallationCooldownTimer = Settings.PlanetaryInstallations.PlayerSpawnCooldown;
							
						}
						
					}
					
					if(playerWatchList[player].BossEncounterCheckTimer <= 0){
						
						playerWatchList[player].BossEncounterCheckTimer = Settings.BossEncounters.SpawnTimerTrigger;
						Logger.SkipNextMessage = true;
						Logger.AddMsg("Attempting Boss Encounter Spawn Near Player: " + player.DisplayName);
						Logger.SkipNextMessage = true;
						var spawnResult = BossEncounterSpawner.AttemptSpawn(player.GetPosition());
						Logger.AddMsg(spawnResult);
						
						if(spawnResult.StartsWith("Boss Encounter GPS Created") == true){
							
							playerWatchList[player].BossEncounterCooldownTimer = Settings.BossEncounters.PlayerSpawnCooldown;
							
						}
								
					}
					
				}else{
					
					var newPlayerWatcher = new PlayerWatcher();
					playerWatchList.Add(player, newPlayerWatcher);
					
				}
				
			}
			
		}
		
		public void ModMessageReceiver(object payload){
			
			var payloadString = payload as string;
			
			if(payloadString == null){
				
				return;
				
			}
			
		}
		
		
		protected override void UnloadData(){
			
			MyAPIGateway.Utilities.MessageEntered -= ChatCommand.MESChatCommand;
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(8877, ChatCommand.MESMessageHandler);
			
			if(MyAPIGateway.Multiplayer.IsServer == false){
				
				return;
				
			}
			
			MyAPIGateway.Utilities.UnregisterMessageHandler(1521905890, ModMessages.ModMessageHandler);
			MyAPIGateway.Entities.OnEntityAdd -= NPCWatcher.NewEntityDetected;
			
		}
		
	}
	
}