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

namespace ModularEncountersSpawner.Spawners{
	
	public static class PlanetaryInstallationSpawner{
		
		public static string AttemptSpawn(Vector3D startCoords, IMyPlayer player){
			
			if(Settings.General.UseMaxNpcGrids == true){
				
				var totalNPCs = NPCWatcher.ActiveNPCs.Count;
				
				if(totalNPCs >= Settings.General.MaxGlobalNpcGrids){
					
					return "Spawning Aborted. Max Global NPCs Limit Reached.";
					
				}
				
			}
			
			if(NPCWatcher.ActiveNpcTypeLimitReachedForArea("PlanetaryInstallation", startCoords, Settings.PlanetaryInstallations.MaxShipsPerArea, Settings.PlanetaryInstallations.AreaSize) == true){
				
				return "Too Many Planetary Installation Grids in Player Area";
				
			}
			
			MyPlanet planet = SpawnResources.GetNearestPlanet(startCoords);
			
			if(planet == null){
				
				return "No Planets In Game World Found.";
				
			}else{
				
				if(SpawnResources.GetDistanceFromSurface(startCoords, planet) > Settings.PlanetaryInstallations.PlayerMaximumDistanceFromSurface || SpawnResources.IsPositionInGravity(startCoords, planet) == false){
					
					return "Player Not In Planet Gravity Or Too Far From Surface.";
					
				}
				
			}
			
			var planetEntity = planet as IMyEntity;
			
			if(MES_SessionCore.playerWatchList.ContainsKey(player) == true){
				
				var playerSurface = SpawnResources.GetNearestSurfacePoint(player.GetPosition(), planet);
				
				if(MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck == Vector3D.Zero){
					
					MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck = playerSurface;
					return "New Player Detected. Storing Position On Planet.";
					
				}
				
				if(Vector3D.Distance(MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck, playerSurface) < Settings.PlanetaryInstallations.PlayerDistanceSpawnTrigger){
					
					Logger.AddMsg("Player Travelled: " + Vector3D.Distance(MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck, playerSurface) + " Distance From Last Saved Position.");
					return "Player Hasn't Traveled Far Enough Yet.";
					
				}
				
				MES_SessionCore.playerWatchList[player].InstallationDistanceCoordCheck = playerSurface;
				
			}else{
				
				return "Player Not In Watcher List... Although They Probably Should Be If The Script Got Here.";
				
			}
			
			var smallStations = new List<ImprovedSpawnGroup>();
			var mediumStations = new List<ImprovedSpawnGroup>();
			var largeStations = new List<ImprovedSpawnGroup>();
			var spawnGroupList = GetPlanetaryInstallations(startCoords, out smallStations, out mediumStations, out largeStations);
			
			if(spawnGroupList.Count == 0){
				
				return "No Eligible Spawn Groups Could Be Found To Spawn Near Player.";
				
			}
			
			Logger.AddMsg("Found " + (spawnGroupList.Count / 10).ToString() + " Potential Spawn Groups. Small: " + (smallStations.Count / 10).ToString() + " // Medium: " + (mediumStations.Count / 10).ToString() + " // Large: " + (largeStations.Count / 10).ToString(), true);
			
			string stationSize = "Small";
			spawnGroupList = smallStations;
			
			bool skippedAbsentSmall = false;
			bool skippedAbsentMedium = false;
			bool skippedAbsentLarge = false;
			
			//Start With Small Station Always, Try Chance For Medium.
			if(stationSize == "Small" && smallStations.Count == 0){
				
				//No Small Stations Available For This Area, So Try Medium.
				skippedAbsentSmall = true;
				stationSize = "Medium";
				spawnGroupList = mediumStations;
				
			}else if(stationSize == "Small" && smallStations.Count != 0){
				
				int mediumChance = 0;
				string varName = "MES-" + planetEntity.EntityId.ToString() + "-Medium";
				
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out mediumChance) == false){
					
					mediumChance = Settings.PlanetaryInstallations.MediumSpawnChanceBaseValue;
					MyAPIGateway.Utilities.SetVariable<int>(varName, mediumChance);
					
				}
				
				if(SpawnResources.rnd.Next(0, 100) < mediumChance){
					
					stationSize = "Medium";
					spawnGroupList = mediumStations;
					
				}
				
			}
			
			if(stationSize == "Medium" && mediumStations.Count == 0){
				
				//No Medium Stations Available For This Area, So Try Large.
				skippedAbsentMedium = true;
				stationSize = "Large";
				spawnGroupList = largeStations;
				
			}else if(stationSize == "Medium" && mediumStations.Count != 0){
				
				int largeChance = 0;
				string varName = "MES-" + planetEntity.EntityId.ToString() + "-Large";
				
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out largeChance) == false){
					
					largeChance = Settings.PlanetaryInstallations.LargeSpawnChanceBaseValue;
					MyAPIGateway.Utilities.SetVariable<int>(varName, largeChance);
					
				}
				
				if(SpawnResources.rnd.Next(0, 100) < largeChance){
					
					stationSize = "Large";
					spawnGroupList = largeStations;
					
				}
				
			}
			
			if(stationSize == "Large" && largeStations.Count == 0){
				
				skippedAbsentLarge = true;
				stationSize = "Medium";
				spawnGroupList = mediumStations;
				
				if(mediumStations.Count == 0){
					
					skippedAbsentMedium = true;
					stationSize = "Small";
					spawnGroupList = smallStations;
					
				}
				
			}
			
			if(spawnGroupList.Count == 0){
				
				return "Could Not Find Station Of Suitable Size For This Spawn Instance.";
				
			}
			
			var spawnGroup = spawnGroupList[SpawnResources.rnd.Next(0, spawnGroupList.Count)];
			Vector3D spawnCoords = Vector3D.Zero;
			Logger.StartTimer();
			
			if(GetSpawnCoords(spawnGroup, startCoords, out spawnCoords) == false){
				
				Logger.AddMsg("Planetary Installation Spawn Coord Calculation Time: " + Logger.StopTimer(), true);
				return "Could Not Find Safe Position To Spawn " + stationSize + " Installation.";
				
			}
			
			Logger.AddMsg("Planetary Installation Spawn Coord Calculation Time: " + Logger.StopTimer(), true);

			//Get Directions
			var upDir = Vector3D.Normalize(spawnCoords - planetEntity.GetPosition());
			var forwardDir = Vector3D.CalculatePerpendicularVector(upDir);
			var spawnMatrix = MatrixD.CreateWorld(spawnCoords, forwardDir, upDir);
			var successfulVoxelSpawn = false;
			
			foreach(var voxel in spawnGroup.SpawnGroup.Voxels){
				
				var voxelSpawningPosition = Vector3D.Transform((Vector3D)voxel.Offset, spawnMatrix);
				
				try{
					
					var voxelSpawn = MyAPIGateway.Session.VoxelMaps.CreateVoxelMapFromStorageName(voxel.StorageName, voxel.StorageName, voxelSpawningPosition);
					NPCWatcher.SpawnedVoxels.Add(voxelSpawn.EntityId.ToString(), voxelSpawn as IMyEntity);
					successfulVoxelSpawn = true;
					
				}catch(Exception exc){
					
					Logger.AddMsg("Voxel Spawning For " + voxel.StorageName + " Failed");
					
				}
				
			}
			
			if(successfulVoxelSpawn == true){
				
				var voxelIdList = new List<string>(NPCWatcher.SpawnedVoxels.Keys.ToList());
				string[] voxelIdArray = voxelIdList.ToArray();
				MyAPIGateway.Utilities.SetVariable<string[]>("MES-SpawnedVoxels", voxelIdArray);
				
			}
			
			for(int i = 0; i < spawnGroup.SpawnGroup.Prefabs.Count; i++){
				
				var prefab = spawnGroup.SpawnGroup.Prefabs[i];
				var options = SpawnGroupManager.CreateSpawningOptions(spawnGroup, prefab);				
				var spawnPosition = Vector3D.Transform((Vector3D)prefab.Position, spawnMatrix);
				
				//Realign to Terrain
				var offsetSurfaceCoords = SpawnResources.GetNearestSurfacePoint(spawnPosition, planet);
				var offsetSurfaceMatrix = MatrixD.CreateWorld(offsetSurfaceCoords, forwardDir, upDir);
				var finalCoords = Vector3D.Transform(new Vector3D(0, (double)prefab.Position.Y, 0), offsetSurfaceMatrix);
				
				var newForward = offsetSurfaceMatrix.Forward;
				var newUp = offsetSurfaceMatrix.Up;
				
				GetDerelictDirections(spawnGroup, i, finalCoords, ref newForward, ref newUp);
				
				var speedL = Vector3.Zero;
				var speedA = Vector3.Zero;
				var gridList = new List<IMyCubeGrid>();
				long gridOwner = 0;
				
				if(NPCWatcher.NPCFactionTagToFounder.ContainsKey(spawnGroup.FactionOwner) == true){
					
					gridOwner = NPCWatcher.NPCFactionTagToFounder[spawnGroup.FactionOwner];
					
				}else{
					
					Logger.AddMsg("Could Not Find Faction Founder For: " + spawnGroup.FactionOwner);
					
				}
				
				//Weapon Randomizer - Start
				MyPrefabDefinition prefabDef = null;
				
				if(SpawnGroupManager.prefabBackupList.ContainsKey(prefab.SubtypeId) == true){
					
					prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);
					
					if(SpawnGroupManager.prefabBackupList[prefab.SubtypeId].Count == prefabDef.CubeGrids.Length){
						
						for(int j = 0; j < SpawnGroupManager.prefabBackupList[prefab.SubtypeId].Count; j++){
							
							var clonedGridOb = SpawnGroupManager.prefabBackupList[prefab.SubtypeId][j].Clone();
							prefabDef.CubeGrids[j] = clonedGridOb as MyObjectBuilder_CubeGrid;
							
						}
						
					}
					
					SpawnGroupManager.prefabBackupList.Remove(prefab.SubtypeId);
					
				}
				
				var replacedWeapons = false;
				if(spawnGroup.RandomizeWeapons == true || MES_SessionCore.NPCWeaponUpgradesModDetected == true || Settings.General.EnableGlobalNPCWeaponRandomizer == true){
					
					replacedWeapons = true;
					
					if(prefabDef == null){
						
						prefabDef = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId);
						
					}
					
					var backupGridList = new List<MyObjectBuilder_CubeGrid>();
					
					for(int j = 0; j < prefabDef.CubeGrids.Length; j++){
						
						var clonedGridOb = prefabDef.CubeGrids[j].Clone();
						backupGridList.Add(clonedGridOb as MyObjectBuilder_CubeGrid);
						GridBuilderManipulation.ProcessGrid(prefabDef.CubeGrids[j], true, false);
						
					}
					
					if(SpawnGroupManager.prefabBackupList.ContainsKey(prefab.SubtypeId) == false){
						
						SpawnGroupManager.prefabBackupList.Add(prefab.SubtypeId, backupGridList);
						
					}
	
				}
				//Weapon Randomizer - End
				
				try{
					
					MyAPIGateway.PrefabManager.SpawnPrefab(gridList, prefab.SubtypeId, finalCoords, newForward, newUp, speedL, speedA, prefab.BeaconText, options, gridOwner);
					
				}catch(Exception exc){
					
					
					
				}
				
				var pendingNPC = new ActiveNPC();
				pendingNPC.Name = prefab.SubtypeId;
				pendingNPC.GridName = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId).CubeGrids[0].DisplayName;
				pendingNPC.StartCoords = finalCoords;
				pendingNPC.CurrentCoords = finalCoords;
				pendingNPC.EndCoords = finalCoords;
				pendingNPC.SpawnType = "PlanetaryInstallation";
				pendingNPC.CleanupIgnore = spawnGroup.IgnoreCleanupRules;
				pendingNPC.ForceStaticGrid = spawnGroup.ForceStaticGrid;
				pendingNPC.ReplacedWeapons = replacedWeapons;
				pendingNPC.KeenAiName = prefab.Behaviour;
				pendingNPC.KeenAiTriggerDistance = prefab.BehaviourActivationDistance;
				
				if(spawnGroup.RandomizeWeapons == true || MES_SessionCore.NPCWeaponUpgradesModDetected == true || Settings.General.EnableGlobalNPCWeaponRandomizer == true){
					
					pendingNPC.ReplenishedSystems = false;
					
				}else if(spawnGroup.ReplenishSystems == true){
					
					pendingNPC.ReplenishedSystems = false;
					
				}
				
				NPCWatcher.PendingNPCs.Add(pendingNPC);
				
			}
			
			if(spawnGroup.PlanetaryInstallationType == "Small"){
				
				int mediumChance = 0;
				string varName = "MES-" + planetEntity.EntityId.ToString() + "-Medium";
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out mediumChance) == true){
					
					mediumChance += Settings.PlanetaryInstallations.MediumSpawnChanceIncrement;
					MyAPIGateway.Utilities.SetVariable<int>(varName, mediumChance);
					
				}
				
				Logger.AddMsg("Medium Installation Spawning Chance Now Set To: " + mediumChance.ToString() + " / 100", true);
				
			}
			
			if(spawnGroup.PlanetaryInstallationType == "Medium" || skippedAbsentMedium == true){
				
				int mediumChance = 0;
				string varName = "MES-" + planetEntity.EntityId.ToString() + "-Medium";
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out mediumChance) == true){
					
					mediumChance = Settings.PlanetaryInstallations.MediumSpawnChanceBaseValue;
					MyAPIGateway.Utilities.SetVariable<int>(varName, mediumChance);
					
				}
				
				Logger.AddMsg("Medium Installation Spawning Chance Now Set To: " + mediumChance.ToString() + " / 100", true);
				
				int largeChance = 0;
				varName = "MES-" + planetEntity.EntityId.ToString() + "-Large";
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out largeChance) == true){
					
					largeChance += Settings.PlanetaryInstallations.LargeSpawnChanceIncrement;
					MyAPIGateway.Utilities.SetVariable<int>(varName, largeChance);
					
				}
				
				Logger.AddMsg("Large Installation Spawning Chance Now Set To: " + largeChance.ToString() + " / 100", true);
				
			}
			
			if(spawnGroup.PlanetaryInstallationType == "Large" || skippedAbsentLarge == true){
				
				int largeChance = 0;
				string varName = "MES-" + planetEntity.EntityId.ToString() + "-Large";
				if(MyAPIGateway.Utilities.GetVariable<int>(varName, out largeChance) == true){
					
					largeChance = Settings.PlanetaryInstallations.LargeSpawnChanceBaseValue;
					MyAPIGateway.Utilities.SetVariable<int>(varName, largeChance);
					
				}
				
				Logger.AddMsg("Large Installation Spawning Chance Now Set To: " + largeChance.ToString() + " / 100", true);
				
			}
			
			if(spawnGroup.UniqueEncounter == true){
				
				SpawnGroupManager.UniqueGroupsSpawned.Add(spawnGroup.SpawnGroup.Id.SubtypeName);
				string[] uniqueSpawnedArray = SpawnGroupManager.UniqueGroupsSpawned.ToArray();
				MyAPIGateway.Utilities.SetVariable<string[]>("MES-UniqueGroupsSpawned", uniqueSpawnedArray);
				
			}
			
			Logger.SkipNextMessage = false;
			return "Spawning Group - " + spawnGroup.SpawnGroup.Id.SubtypeName;
			
		}
		
		public static void GetDerelictDirections(ImprovedSpawnGroup spawnGroup, int index, Vector3D coords, ref Vector3D forward, ref Vector3D up){
			
			if(index >= 6){
				
				return;
				
			}
			
			int derelictProfile = 0;
			
			if(index == 0){
				
				derelictProfile = spawnGroup.DerelictInstallationA;
				
			}
			
			if(index == 1){
				
				derelictProfile = spawnGroup.DerelictInstallationB;
				
			}
			
			if(index == 2){
				
				derelictProfile = spawnGroup.DerelictInstallationC;
				
			}
			
			if(index == 3){
				
				derelictProfile = spawnGroup.DerelictInstallationD;
				
			}
			
			if(index == 4){
				
				derelictProfile = spawnGroup.DerelictInstallationE;
				
			}
			
			if(index == 5){
				
				derelictProfile = spawnGroup.DerelictInstallationF;
				
			}
			
			if(derelictProfile == 0){
				
				return;
				
			}
			
			if(derelictProfile == 1){
				
				var prefabMatrix = MatrixD.CreateWorld(coords, forward, up);
				var offset = new Vector3D(0,20,0);
				offset.X = SpawnResources.GetRandomPathDist(-5, 6);
				offset.Z = SpawnResources.GetRandomPathDist(-5, 6);
				var offsetTrans = Vector3D.Transform(offset, prefabMatrix);
				up = Vector3D.Normalize(offsetTrans - coords);
				forward = Vector3D.Normalize(MyUtils.GetRandomPerpendicularVector(ref up));
				return;
				
			}
			
			var derelictForward = Vector3D.Zero;
			var derelictUp = Vector3D.Zero;
			bool doTransform = false;
			
			if(derelictProfile == 2){
				
				derelictForward = new Vector3D(0.131272882223129,-0.2370775192976,-0.962580740451813);
				derelictUp = new Vector3D(0.471277594566345,0.869170486927032,-0.149800226092339);
				doTransform = true;
				
			}
			
			if(derelictProfile == 3){
				
				derelictForward = new Vector3D(0.0755081623792648,0.384217709302902,-0.920149564743042);
				derelictUp = new Vector3D(-0.333788454532623,0.87928694486618,0.339764207601547);
				doTransform = true;
				
			}
			
			if(derelictProfile == 4){
				
				derelictForward = new Vector3D(0.152337029576302,-0.119354099035263,-0.981095314025879);
				derelictUp = new Vector3D(0.578473806381226,0.815646708011627,-0.00940560176968575);
				doTransform = true;
				
			}
			
			if(derelictProfile == 5){
				
				derelictForward = new Vector3D(0.111227437853813,-0.543674468994141,-0.83189332485199);
				derelictUp = new Vector3D(0.0449797473847866,0.838983714580536,-0.542294383049011);
				doTransform = true;
				
			}
			
			if(derelictProfile == 6){
				
				derelictForward = new Vector3D(0.0794214978814125,0.494431376457214,-0.8655806183815);
				derelictUp = new Vector3D(-0.37333881855011,0.819878816604614,0.434070110321045);
				doTransform = true;
				
			}
			
			if(doTransform == true){
				
				var prefabMatrix = MatrixD.CreateWorld(coords, forward, up);
				var tempDirUp = Vector3D.Transform(derelictUp, prefabMatrix);
				var tempDirForward = Vector3D.Transform(derelictForward, prefabMatrix);
				up = Vector3D.Normalize(tempDirUp - coords);
				forward = Vector3D.Normalize(tempDirForward - coords);
				return;
				
			}
			
		}
		
		public static List<ImprovedSpawnGroup> GetPlanetaryInstallations(Vector3D playerCoords, out List<ImprovedSpawnGroup> smallStations, out List<ImprovedSpawnGroup> mediumStations, out List<ImprovedSpawnGroup> largeStations){
			
			smallStations = new List<ImprovedSpawnGroup>();
			mediumStations = new List<ImprovedSpawnGroup>();
			largeStations = new List<ImprovedSpawnGroup>();
			
			MyPlanet planet = SpawnResources.GetNearestPlanet(playerCoords);
			string specificGroup = "";
			var planetRestrictions = new List<string>(Settings.General.PlanetSpawnsDisableList.ToList());
			
			if(planet != null){
				
				if(planetRestrictions.Contains(planet.Generator.Id.SubtypeName) == true){
					
					return new List<ImprovedSpawnGroup>();
					
				}
				
			}
			
			if(SpawnGroupManager.AdminSpawnGroup != ""){
				
				specificGroup = SpawnGroupManager.AdminSpawnGroup;
				SpawnGroupManager.AdminSpawnGroup = "";
				
			}
			
			if(SpawnResources.IsPositionInGravity(playerCoords, planet) == false){
				
				return new List<ImprovedSpawnGroup>();
				
			}
			
			var eligibleGroups = new List<ImprovedSpawnGroup>();
			
			
			//Filter Eligible Groups To List
			foreach(var spawnGroup in SpawnGroupManager.SpawnGroups){
				
				if(specificGroup != "" && spawnGroup.SpawnGroup.Id.SubtypeName != specificGroup){
					
					continue;
					
				}
				
				if(specificGroup == "" && spawnGroup.AdminSpawnOnly == true){
					
					continue;
					
				}
				
				if(SpawnGroupManager.ModRestrictionCheck(spawnGroup) == false){
					
					continue;
					
				}
				
				if(SpawnGroupManager.IsSpawnGroupInBlacklist(spawnGroup.SpawnGroup.Id.SubtypeName) == true){
					
					continue;
					
				}
				
				if(spawnGroup.PlanetaryInstallation == false){
					
					continue;
					
				}
				
				if(spawnGroup.PlanetaryInstallationType != "Small" && spawnGroup.PlanetaryInstallationType != "Medium" && spawnGroup.PlanetaryInstallationType != "Large"){
					
					continue;
					
				}
				
				if(spawnGroup.UniqueEncounter == true && SpawnGroupManager.UniqueGroupsSpawned.Contains(spawnGroup.SpawnGroup.Id.SubtypeName) == true){
					
					continue;
					
				}
				
				if(SpawnGroupManager.DistanceFromCenterCheck(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(SpawnGroupManager.CheckSpawnGroupPlanetLists(spawnGroup, planet) == false){
				
					continue;
					
				}
				
				if(SpawnResources.TerritoryValidation(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(spawnGroup.Frequency > 0){
					
					if(Settings.PlanetaryInstallations.UseMaxSpawnGroupFrequency == true && spawnGroup.Frequency > Settings.PlanetaryInstallations.MaxSpawnGroupFrequency * 10){
						
						spawnGroup.Frequency = (int)Math.Round((double)Settings.PlanetaryInstallations.MaxSpawnGroupFrequency * 10);
						
					}
					
					for(int i = 0; i < spawnGroup.Frequency; i++){
						
						eligibleGroups.Add(spawnGroup);
						
						if(spawnGroup.PlanetaryInstallationType == "Small"){
					
							smallStations.Add(spawnGroup);
					
						}
						
						if(spawnGroup.PlanetaryInstallationType == "Medium"){
					
							mediumStations.Add(spawnGroup);
					
						}
						
						if(spawnGroup.PlanetaryInstallationType == "Large"){
					
							largeStations.Add(spawnGroup);
					
						}
						
					}
					
				}
		
			}
			
			return eligibleGroups;
			
		}
		
		public static bool GetSpawnCoords(ImprovedSpawnGroup spawnGroup, Vector3D startCoords, out Vector3D spawnCoords){
			
			spawnCoords = Vector3D.Zero;
			SpawnResources.RefreshEntityLists();
			MyPlanet planet = SpawnResources.GetNearestPlanet(startCoords);
			var planetEntity = planet as IMyEntity;
			double extraDistance = 0;
			double terrainVarianceCheckTarget = Settings.PlanetaryInstallations.SmallTerrainCheckDistance;
			
			if(planetEntity == null || planet == null){
				
				Logger.AddMsg("Planet Somehow Doesn't Exist... Even Though It Should If Script Got Here...", true);
				return false;
				
			}
			
			if(spawnGroup.PlanetaryInstallationType == "Medium"){
				
				extraDistance = Settings.PlanetaryInstallations.MediumSpawnDistanceIncrement;
				terrainVarianceCheckTarget = Settings.PlanetaryInstallations.MediumTerrainCheckDistance;
				
			}
			
			if(spawnGroup.PlanetaryInstallationType == "Large"){
				
				extraDistance = Settings.PlanetaryInstallations.LargeSpawnDistanceIncrement;
				terrainVarianceCheckTarget = Settings.PlanetaryInstallations.LargeTerrainCheckDistance;
				
			}
			
			var startDist = Settings.PlanetaryInstallations.MinimumSpawnDistanceFromPlayers + extraDistance;
			var endDist = Settings.PlanetaryInstallations.MaximumSpawnDistanceFromPlayers + extraDistance;
			var upDir = Vector3D.Normalize(startCoords - planetEntity.GetPosition());
			var forwardDir = Vector3D.Normalize(MyUtils.GetRandomPerpendicularVector(ref upDir));
			var searchMatrix = MatrixD.CreateWorld(startCoords, forwardDir, upDir);
			
			//Searches in 8 directions from the player position
			var searchDirections = new List<Vector3D>();
			searchDirections.Add(searchMatrix.Forward);
			searchDirections.Add(searchMatrix.Backward);
			searchDirections.Add(searchMatrix.Left);
			searchDirections.Add(searchMatrix.Right);
			
			if(Settings.PlanetaryInstallations.AggressivePathCheck == true){
				
				searchDirections.Add(Vector3D.Normalize(searchMatrix.Forward + searchMatrix.Left));
				searchDirections.Add(Vector3D.Normalize(searchMatrix.Forward + searchMatrix.Right));
				searchDirections.Add(Vector3D.Normalize(searchMatrix.Backward + searchMatrix.Left));
				searchDirections.Add(Vector3D.Normalize(searchMatrix.Backward + searchMatrix.Right));
				
			}

			int debugSpawnPointAttempts = 0;
			int searchDirectionAttempts = 0;
			
			foreach(var searchDirection in searchDirections){
				
				searchDirectionAttempts++;
				double searchIncrement = startDist;
				
				while(searchIncrement < endDist){
					
					debugSpawnPointAttempts++;
					var checkCoords = searchDirection * searchIncrement + startCoords;
					var surfaceCoords = SpawnResources.GetNearestSurfacePoint(checkCoords, planet);
					
					if(SpawnResources.IsPositionNearEntity(surfaceCoords, Settings.PlanetaryInstallations.MinimumSpawnDistanceFromOtherGrids) == true || SpawnResources.IsPositionInSafeZone(surfaceCoords) == true){
						
						searchIncrement += Settings.PlanetaryInstallations.SearchPathIncrement;
						continue;
						
					}
					
					var checkUpDir = Vector3D.Normalize(surfaceCoords - planetEntity.GetPosition());
					var checkForwardDir = Vector3D.Normalize(MyUtils.GetRandomPerpendicularVector(ref checkUpDir));
					var checkMatrix = MatrixD.CreateWorld(surfaceCoords, checkForwardDir, checkUpDir);
					
					var checkDirections = new List<Vector3D>();
					checkDirections.Add(checkMatrix.Forward);
					checkDirections.Add(checkMatrix.Backward);
					checkDirections.Add(checkMatrix.Left);
					checkDirections.Add(checkMatrix.Right);
					
					if(Settings.PlanetaryInstallations.AggressiveTerrainCheck == true){
						
						checkDirections.Add(Vector3D.Normalize(checkMatrix.Forward + checkMatrix.Left));
						checkDirections.Add(Vector3D.Normalize(checkMatrix.Forward + checkMatrix.Right));
						checkDirections.Add(Vector3D.Normalize(checkMatrix.Backward + checkMatrix.Left));
						checkDirections.Add(Vector3D.Normalize(checkMatrix.Backward + checkMatrix.Right));
						
					}
					
					var distToCore = Vector3D.Distance(surfaceCoords, planetEntity.GetPosition());
					bool badPosition = false;
					
					foreach(var checkDirection in checkDirections){
						
						double terrainCheckIncrement = 0;
						
						while(terrainCheckIncrement < terrainVarianceCheckTarget){
							
							var checkTerrainCoords = checkDirection * terrainCheckIncrement + surfaceCoords;
							var checkTerrainSurfaceCoords = SpawnResources.GetNearestSurfacePoint(checkTerrainCoords, planet);
							var checkDistToCore = Vector3D.Distance(checkTerrainSurfaceCoords, planetEntity.GetPosition());
							var elevationDiff = checkDistToCore - distToCore;
							
							if(elevationDiff < Settings.PlanetaryInstallations.MinimumTerrainVariance || elevationDiff > Settings.PlanetaryInstallations.MaximumTerrainVariance){
								
								badPosition = true;
								break;
								
							}
							
							terrainCheckIncrement += Settings.PlanetaryInstallations.TerrainCheckIncrementDistance;
						
						}
						
						if(badPosition == true){
							
							break;
							
						}
						
					}
					
					if(badPosition == false){
						
						spawnCoords = surfaceCoords;
						Logger.AddMsg("Found Installation Site After: " + debugSpawnPointAttempts.ToString() + " Attempts", true);
						Logger.AddMsg("Search Directions Used: " + searchDirectionAttempts.ToString(), true);
						return true;
						
					}
					
					searchIncrement += Settings.PlanetaryInstallations.SearchPathIncrement;
					
				}
				
			}
			
			Logger.AddMsg("Could Not Find Installation Site After: " + debugSpawnPointAttempts.ToString() + " Attempts", true);
			return false;
			
		}
			
	}
	
}