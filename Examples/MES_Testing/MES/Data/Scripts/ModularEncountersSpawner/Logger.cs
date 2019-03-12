using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
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
using ModularEncountersSpawner.Configuration;

namespace ModularEncountersSpawner{
	
	public static class Logger{
		
		public static bool LoggerDebugMode = false;
		public static bool SkipNextMessage = false;
		public static string LogDefaultIdentifier = "Modular Encounters Spawner: ";
		public static Stopwatch PerformanceTimer = new Stopwatch();
		
		public static void AddMsg(string message, bool debugOnly = false, string identifier = ""){
			
			if(LoggerDebugMode == false && debugOnly == true){
				
				return;
				
			}
			
			if(LoggerDebugMode == false && SkipNextMessage == true){
				
				SkipNextMessage = false;
				return;
				
			}
			
			SkipNextMessage = false;
			
			string thisIdentifier = "";
			
			if(identifier == ""){
				
				thisIdentifier = LogDefaultIdentifier;
				
			}
			
			MyLog.Default.WriteLineAndConsole(thisIdentifier + message);
			
			if(LoggerDebugMode == true){
				
				MyVisualScriptLogicProvider.ShowNotificationToAll(message, 5000);
				
			}
			
		}
		
		public static void StartTimer(){
			
			if(LoggerDebugMode == false){
				
				return;
				
			}
			
			PerformanceTimer = Stopwatch.StartNew();
			
		}
		
		public static string StopTimer(){
			
			if(LoggerDebugMode == false){
				
				return "";
				
			}
			
			PerformanceTimer.Stop();
			return PerformanceTimer.Elapsed.ToString();
			
		}
		
		public static void CreateDebugGPS(string name, Vector3D coords){
			
			if(LoggerDebugMode == false){
				
				return;
				
			}
			
			var gps = MyAPIGateway.Session.GPS.Create(name, "", coords, true);
			MyAPIGateway.Session.GPS.AddLocalGps(gps);
			
		}
		
		public static string GetActiveNPCs(){
			
			var sb = new StringBuilder();
			
			if(NPCWatcher.ActiveNPCs.Keys.Count == 0){
				
				return "";
				
			}
			
			foreach(var npc in NPCWatcher.ActiveNPCs.Keys){
				
				sb.Append("Name: ").Append(NPCWatcher.ActiveNPCs[npc].Name).AppendLine();
				
				if(npc != null && MyAPIGateway.Entities.Exist(npc) == true){
					
					sb.Append("EntityId: ").Append(npc.EntityId.ToString()).AppendLine();
					
				}
				
				sb.Append("Start: ").Append(NPCWatcher.ActiveNPCs[npc].StartCoords.ToString()).AppendLine();
				sb.Append("End: ").Append(NPCWatcher.ActiveNPCs[npc].EndCoords.ToString()).AppendLine();
				
				if(NPCWatcher.ActiveNPCs[npc].SpawnType.Contains("CargoShip") == true && npc != null && MyAPIGateway.Entities.Exist(npc) == true){
					
					var distance = Vector3D.Distance(npc.GetPosition(), NPCWatcher.ActiveNPCs[npc].EndCoords);
					sb.Append("Distance Remaining: ").Append(distance.ToString()).AppendLine();
					
				}
				
				sb.Append("Type: ").Append(NPCWatcher.ActiveNPCs[npc].SpawnType).AppendLine();
				sb.Append("Clean Ignore: ").Append(NPCWatcher.ActiveNPCs[npc].CleanupIgnore.ToString()).AppendLine();
				sb.Append("Clean Timer: ").Append(NPCWatcher.ActiveNPCs[npc].CleanupTime.ToString()).AppendLine();
				sb.Append("NPC Owned: ").Append(NPCWatcher.ActiveNPCs[npc].FullyNPCOwned.ToString()).AppendLine();
				
				sb.AppendLine();
				
			}
			
			return sb.ToString();
			
		}
		
		public static string GetPlayerWatcherData(){
			
			var sb = new StringBuilder();
			
			if(MES_SessionCore.playerWatchList.Keys.Count == 0){
				
				return "";
				
			}
			
			foreach(var player in MES_SessionCore.playerWatchList.Keys){
				
				if(player == null){
					
					continue;
					
				}
				
				if(player.Character == null){
					
					continue;
					
				}
				
				sb.Append("Player Name: ").Append(player.DisplayName).AppendLine();
				sb.Append("Space Cargo Ship Timer: ").Append(MES_SessionCore.playerWatchList[player].SpaceCargoShipTimer.ToString()).AppendLine();
				sb.Append("Atmo Cargo Ship Timer: ").Append(MES_SessionCore.playerWatchList[player].AtmoCargoShipTimer.ToString()).AppendLine();
				sb.Append("Random Encounter Timer: ").Append(MES_SessionCore.playerWatchList[player].RandomEncounterCheckTimer.ToString()).AppendLine();
				sb.Append("Random Encounter Cooldown Timer: ").Append(MES_SessionCore.playerWatchList[player].RandomEncounterCoolDownTimer.ToString()).AppendLine();
				sb.Append("Random Encounter Travel Distance: ").Append(Vector3D.Distance(player.GetPosition(), MES_SessionCore.playerWatchList[player].RandomEncounterDistanceCoordCheck).ToString()).AppendLine();
				sb.Append("Planetary Installation Timer: ").Append(MES_SessionCore.playerWatchList[player].PlanetaryInstallationCheckTimer.ToString()).AppendLine();
				sb.Append("Planetary Installation Cooldown: ").Append(MES_SessionCore.playerWatchList[player].PlanetaryInstallationCooldownTimer.ToString()).AppendLine();
				sb.Append("Planetary Installation Travel Distance: ").Append(Vector3D.Distance(player.GetPosition(), MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck).ToString()).AppendLine();
				sb.Append("Boss Encounter Timer: ").Append(MES_SessionCore.playerWatchList[player].BossEncounterCheckTimer.ToString()).AppendLine();
				sb.Append("Boss Encounter Cooldown Timer: ").Append(MES_SessionCore.playerWatchList[player].BossEncounterCooldownTimer.ToString()).AppendLine();
				sb.Append("Boss Encounter Active: ").Append(MES_SessionCore.playerWatchList[player].BossEncounterActive.ToString()).AppendLine();
				sb.AppendLine();
				
			}
			
			return sb.ToString();
			
		}
		
		public static string GetSpawnedUniqueEncounters(){
			
			var sb = new StringBuilder();
			
			if(SpawnGroupManager.UniqueGroupsSpawned.Count == 0){
				
				return "";
				
			}
			
			foreach(var groupName in SpawnGroupManager.UniqueGroupsSpawned){
				
				sb.Append(groupName).AppendLine();
				
			}
			
			return sb.ToString();
			
		}
		
		public static string SpawnGroupResults(){
			
			var sb = new StringBuilder();
			sb.Append("======SPAWN GROUPS======").AppendLine();
			sb.AppendLine();
			sb.AppendLine();
			
			if(SpawnGroupManager.SpawnGroups.Count == 0 && TerritoryManager.Territories.Count == 0){
				
				return "";
				
			}
			
			foreach(var spawnGroup in SpawnGroupManager.SpawnGroups){
				
				sb.Append("Spawn Group Name: ").Append(spawnGroup.SpawnGroup.Id.SubtypeName).AppendLine();
				sb.Append("SpaceCargoShip: ").Append(spawnGroup.SpaceCargoShip.ToString()).AppendLine();
				sb.Append("LunarCargoShip: ").Append(spawnGroup.LunarCargoShip.ToString()).AppendLine();
				sb.Append("AtmosphericCargoShip: ").Append(spawnGroup.AtmosphericCargoShip.ToString()).AppendLine();
				sb.Append("SpaceRandomEncounter: ").Append(spawnGroup.SpaceRandomEncounter.ToString()).AppendLine();
				sb.Append("PlanetaryInstallation: ").Append(spawnGroup.PlanetaryInstallation.ToString()).AppendLine();
				sb.Append("PlanetaryInstallationType: ").Append(spawnGroup.PlanetaryInstallationType).AppendLine();
				sb.Append("DerelictInstallationA: ").Append(spawnGroup.DerelictInstallationA).AppendLine();
				sb.Append("BossEncounterSpace: ").Append(spawnGroup.BossEncounterSpace.ToString()).AppendLine();
				sb.Append("BossEncounterAtmo: ").Append(spawnGroup.BossEncounterAtmo.ToString()).AppendLine();
				sb.Append("BossEncounterAny: ").Append(spawnGroup.BossEncounterAny.ToString()).AppendLine();
				sb.Append("Frequency: ").Append(spawnGroup.Frequency.ToString()).AppendLine();
				sb.Append("UniqueEncounter: ").Append(spawnGroup.UniqueEncounter.ToString()).AppendLine();
				sb.Append("FactionOwner: ").Append(spawnGroup.FactionOwner).AppendLine();
				sb.Append("IgnoreCleanupRules: ").Append(spawnGroup.IgnoreCleanupRules.ToString()).AppendLine();
				sb.Append("ForceStaticGrid: ").Append(spawnGroup.ForceStaticGrid.ToString()).AppendLine();
				sb.Append("MinSpawnFromWorldCenter: ").Append(spawnGroup.MinSpawnFromWorldCenter.ToString()).AppendLine();
				sb.Append("MaxSpawnFromWorldCenter: ").Append(spawnGroup.MaxSpawnFromWorldCenter.ToString()).AppendLine();
				sb.Append("PlanetBlackList: ").Append(string.Join(",", spawnGroup.PlanetBlacklist)).AppendLine();
				sb.Append("PlanetWhiteList: ").Append(string.Join(",", spawnGroup.PlanetWhitelist)).AppendLine();
				sb.Append("PlanetRequiresVacuum: ").Append(spawnGroup.PlanetRequiresVacuum.ToString()).AppendLine();
				sb.Append("PlanetRequiresAtmo: ").Append(spawnGroup.PlanetRequiresAtmo.ToString()).AppendLine();
				sb.Append("PlanetRequiresOxygen: ").Append(spawnGroup.PlanetRequiresOxygen.ToString()).AppendLine();
				sb.Append("PlanetMinimumSize: ").Append(spawnGroup.PlanetMinimumSize.ToString()).AppendLine();
				sb.Append("PlanetMaximumSize: ").Append(spawnGroup.PlanetMaximumSize.ToString()).AppendLine();
				
				sb.Append("RequiredAllMods: ");
				foreach(var mod in spawnGroup.RequireAllMods){
					
					sb.Append(mod.ToString()).Append(", ");
					
				}
				sb.AppendLine();
				
				sb.Append("RequiredAnyMods: ");
				foreach(var mod in spawnGroup.RequireAnyMods){
					
					sb.Append(mod.ToString()).Append(", ");
					
				}
				sb.AppendLine();
				
				sb.Append("ExcludedAllMods: ");
				foreach(var mod in spawnGroup.ExcludeAllMods){
					
					sb.Append(mod.ToString()).Append(", ");
					
				}
				sb.AppendLine();
				
				sb.Append("ExcludedAnyMods: ");
				foreach(var mod in spawnGroup.ExcludeAnyMods){
					
					sb.Append(mod.ToString()).Append(", ");
					
				}
				sb.AppendLine();
				
				sb.Append("Territory: ").Append(spawnGroup.Territory).AppendLine();
				sb.Append("MinDistanceFromTerritoryCenter: ").Append(spawnGroup.MinDistanceFromTerritoryCenter.ToString()).AppendLine();
				sb.Append("MaxDistanceFromTerritoryCenter: ").Append(spawnGroup.MaxDistanceFromTerritoryCenter.ToString()).AppendLine();
				sb.Append("RotateFirstCockpitToForward: ").Append(spawnGroup.RotateFirstCockpitToForward.ToString()).AppendLine();
				sb.Append("SpawnRandomCargo: ").Append(spawnGroup.SpawnRandomCargo.ToString()).AppendLine();
				sb.Append("DisableDampeners: ").Append(spawnGroup.DisableDampeners.ToString()).AppendLine();
				sb.Append("ReactorsOn: ").Append(spawnGroup.ReactorsOn.ToString()).AppendLine();
				sb.AppendLine();
				
			}
			
			sb.AppendLine();
			sb.Append("======TERRITORIES======").AppendLine();
			sb.AppendLine();
			
			foreach(var territory in TerritoryManager.Territories){
				
				sb.Append("Name: ").Append(territory.Name).AppendLine();
				sb.Append("TagOld: ").Append(territory.TagOld).AppendLine();
				sb.Append("Position: ").Append(territory.Position.ToString()).AppendLine();
				sb.Append("Type: ").Append(territory.Type).AppendLine();
				sb.Append("Radius: ").Append(territory.Radius.ToString()).AppendLine();
				sb.Append("ScaleRadiusWithPlanetSize: ").Append(territory.ScaleRadiusWithPlanetSize.ToString()).AppendLine();
				sb.Append("AnnounceArriveDepart: ").Append(territory.AnnounceArriveDepart.ToString()).AppendLine();
				sb.Append("CustomArriveMessage: ").Append(territory.CustomArriveMessage).AppendLine();
				sb.Append("CustomDepartMessage: ").Append(territory.CustomDepartMessage).AppendLine();
				sb.Append("PlanetGeneratorName: ").Append(territory.PlanetGeneratorName).AppendLine();
				sb.Append("BadTerritory: ").Append(territory.BadTerritory.ToString()).AppendLine();
				sb.AppendLine();
				
			}
			
			return sb.ToString();
			
		}
		
	}
	
}