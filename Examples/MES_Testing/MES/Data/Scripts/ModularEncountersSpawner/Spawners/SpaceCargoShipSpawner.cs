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
	
	public static class SpaceCargoShipSpawner{
		
		public static string AttemptSpawn(Vector3D startCoords){
			
			if(Settings.General.UseMaxNpcGrids == true){
				
				var totalNPCs = NPCWatcher.ActiveNPCs.Count;
				
				if(totalNPCs >= Settings.General.MaxGlobalNpcGrids){
					
					return "Spawning Aborted. Max Global NPCs Limit Reached.";
					
				}
				
			}
			
			if(NPCWatcher.ActiveNpcTypeLimitReachedForArea("SpaceCargoShip", startCoords, Settings.SpaceCargoShips.MaxShipsPerArea, Settings.SpaceCargoShips.AreaSize) == true){
				
				return "Too Many Space Cargo Ship Grids in Player Area";
				
			}
			
			var spawnGroupList = GetSpaceCargoShips(startCoords);
			
			if(spawnGroupList.Count == 0){
				
				return "No Eligible Spawn Groups Could Be Found To Spawn Near Player.";
				
			}
			
			var spawnGroup = spawnGroupList[SpawnResources.rnd.Next(0, spawnGroupList.Count)];
			var startPathCoords = Vector3D.Zero;
			var endPathCoords = Vector3D.Zero;
			bool successfulPath = false;
			MyPlanet planet = SpawnResources.GetNearestPlanet(startCoords);
			
			if(SpawnResources.LunarSpawnEligible(startCoords) == false){
				
				successfulPath = CalculateRegularTravelPath(spawnGroup.SpawnGroup, startCoords, out startPathCoords, out endPathCoords);
				
			}else{
				
				successfulPath = CalculateLunarTravelPath(spawnGroup.SpawnGroup, startCoords, out startPathCoords, out endPathCoords);
				
			}
			
			if(successfulPath == false){
				
				return "Could Not Generate Safe Travel Path For SpawnGroup.";
				
			}
			
			//Get Directions
			var spawnForwardDir = Vector3D.Normalize(endPathCoords - startPathCoords);
			var spawnUpDir = Vector3D.CalculatePerpendicularVector(spawnForwardDir);
			var spawnMatrix = MatrixD.CreateWorld(startPathCoords, spawnForwardDir, spawnUpDir);
			
			foreach(var prefab in spawnGroup.SpawnGroup.Prefabs){
				
				var options = SpawnGroupManager.CreateSpawningOptions(spawnGroup, prefab);
				var spawnPosition = Vector3D.Transform((Vector3D)prefab.Position, spawnMatrix);
				var speedL = prefab.Speed * (Vector3)spawnForwardDir;
				var speedA = Vector3.Zero;
				var gridList = new List<IMyCubeGrid>();
				long gridOwner = 0;
				
				//Speed Management
				if(Settings.SpaceCargoShips.UseMinimumSpeed == true && prefab.Speed < Settings.SpaceCargoShips.MinimumSpeed){
					
					speedL = Settings.SpaceCargoShips.MinimumSpeed * (Vector3)spawnForwardDir;
					
				}
				
				if(Settings.SpaceCargoShips.UseSpeedOverride == true){
					
					speedL = Settings.SpaceCargoShips.SpeedOverride * (Vector3)spawnForwardDir;
					
				}
				
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
						
						for(int i = 0; i < SpawnGroupManager.prefabBackupList[prefab.SubtypeId].Count; i++){
							
							var clonedGridOb = SpawnGroupManager.prefabBackupList[prefab.SubtypeId][i].Clone();
							prefabDef.CubeGrids[i] = clonedGridOb as MyObjectBuilder_CubeGrid;
							
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
					
					for(int i = 0; i < prefabDef.CubeGrids.Length; i++){
						
						bool processShields = false;
						
						if(i == 0){
							
							processShields = true;
							
						}
						
						var clonedGridOb = prefabDef.CubeGrids[i].Clone();
						backupGridList.Add(clonedGridOb as MyObjectBuilder_CubeGrid);
						GridBuilderManipulation.ProcessGrid(prefabDef.CubeGrids[i], true, false, processShields);
						
					}
					
					if(SpawnGroupManager.prefabBackupList.ContainsKey(prefab.SubtypeId) == false){
						
						SpawnGroupManager.prefabBackupList.Add(prefab.SubtypeId, backupGridList);
						
					}
	
				}
				//Weapon Randomizer - End

				try{
					
					MyAPIGateway.PrefabManager.SpawnPrefab(gridList, prefab.SubtypeId, spawnPosition, spawnForwardDir, spawnUpDir, speedL, speedA, prefab.BeaconText, options, gridOwner);
					
				}catch(Exception exc){
					
					Logger.AddMsg("Something Went Wrong With Prefab Spawn Manager.", true);
					
				}
				
				var pendingNPC = new ActiveNPC();
				pendingNPC.Name = prefab.SubtypeId;
				pendingNPC.GridName = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId).CubeGrids[0].DisplayName;
				pendingNPC.StartCoords = startPathCoords;
				pendingNPC.CurrentCoords = startPathCoords;
				pendingNPC.EndCoords = endPathCoords;
				pendingNPC.SpawnType = "SpaceCargoShip";
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
			
			Logger.SkipNextMessage = false;
			return "Spawning Group - " + spawnGroup.SpawnGroup.Id.SubtypeName;
			
		}
		
		public static bool CalculateRegularTravelPath(MySpawnGroupDefinition spawnGroup, Vector3D startCoords, out Vector3D startPathCoords, out Vector3D endPathCoords){
			
			startPathCoords = Vector3D.Zero;
			endPathCoords = Vector3D.Zero;
			SpawnResources.RefreshEntityLists();
			MyPlanet planet = SpawnResources.GetNearestPlanet(startCoords);
			List<IMyEntity> nearbyEntities = new List<IMyEntity>();
			
			for(int i = 0; i < Settings.SpaceCargoShips.MaxSpawnAttempts; i++){
				
				var randDir = Vector3D.Normalize(MyUtils.GetRandomVector3D());
				
				var closestPathDist = (double)SpawnResources.rnd.Next((int)Settings.SpaceCargoShips.MinPathDistanceFromPlayer, (int)Settings.SpaceCargoShips.MaxPathDistanceFromPlayer);
				var closestPathPoint = randDir * closestPathDist + startCoords;
				
				bool tryInvertedDir = SpawnResources.IsPositionInGravity(closestPathPoint, planet);
				
				if(tryInvertedDir == true){
					
					randDir = randDir * -1;
					closestPathPoint = randDir * closestPathDist + startCoords;
					
					if(SpawnResources.IsPositionInGravity(closestPathPoint, planet) == true){
						
						continue;
						
					}
					
				}
				
				var pathDist = (double)SpawnResources.rnd.Next((int)Settings.SpaceCargoShips.MinPathDistance, (int)Settings.SpaceCargoShips.MaxPathDistance);
				var pathDir = Vector3D.Normalize(MyUtils.GetRandomPerpendicularVector(ref randDir));
				var pathHalfDist = pathDist / 2;
				
				var tempPathStart = pathDir * pathHalfDist + closestPathPoint;
				pathDir = pathDir * -1;
				var tempPathEnd = pathDir * pathHalfDist + closestPathPoint;
				
				bool badPath = false;
				
				IHitInfo hitInfo = null;
				
				if(MyAPIGateway.Physics.CastLongRay(tempPathStart, tempPathEnd, out hitInfo, true) == true){
					
					continue;
					
				}
					
				foreach(var entity in SpawnResources.EntityList){
					
					if(Vector3D.Distance(tempPathStart, entity.GetPosition()) < Settings.SpaceCargoShips.MinSpawnDistFromEntities){
						
						badPath = true;
						break;
						
					}
					
				}
				
				if(badPath == true){
					
					continue;
					
				}
				
				var upDir = Vector3D.CalculatePerpendicularVector(pathDir);
				var pathMatrix = MatrixD.CreateWorld(tempPathStart, pathDir, upDir);
				
				foreach(var prefab in spawnGroup.Prefabs){
					
					double stepDistance = 0;
					var tempPrefabStart = Vector3D.Transform((Vector3D)prefab.Position, pathMatrix);
					
					while(stepDistance < pathDist){

						stepDistance += Settings.SpaceCargoShips.PathCheckStep;
						var pathCheckCoords = pathDir * stepDistance + tempPrefabStart;
						
						if(SpawnResources.IsPositionInSafeZone(pathCheckCoords) == true || SpawnResources.IsPositionInGravity(pathCheckCoords, planet) == true){
							
							badPath = true;
							break;
							
						}
												
					}
					
					if(badPath == true){
							
						break;
						
					}

				}

				if(badPath == true){
					
					continue;
					
				}
				
				startPathCoords = tempPathStart;
				endPathCoords = tempPathEnd;
				return true;
				
			}
			
			return false;
			
		}
		
		public static bool CalculateLunarTravelPath(MySpawnGroupDefinition spawnGroup, Vector3D startCoords, out Vector3D startPathCoords, out Vector3D endPathCoords){
			
			startPathCoords = Vector3D.Zero;
			endPathCoords = Vector3D.Zero;
			SpawnResources.RefreshEntityLists();
			MyPlanet planet = SpawnResources.GetNearestPlanet(startCoords);
			
			if(planet == null){
				
				return false;
				
			}
			
			var planetEntity = planet as IMyEntity;
			
			for(int i = 0; i < Settings.SpaceCargoShips.MaxSpawnAttempts; i++){

				var spawnAltitude = (double)SpawnResources.rnd.Next((int)Settings.SpaceCargoShips.MinLunarSpawnHeight, (int)Settings.SpaceCargoShips.MaxLunarSpawnHeight);
				var abovePlayer = SpawnResources.CreateDirectionAndTarget(planetEntity.GetPosition(), startCoords, startCoords, spawnAltitude);
				var midpointDist = (double)SpawnResources.rnd.Next((int)Settings.SpaceCargoShips.MinPathDistanceFromPlayer, (int)Settings.SpaceCargoShips.MaxPathDistanceFromPlayer);
				var pathMidpoint = SpawnResources.GetRandomCompassDirection(abovePlayer, planet) * midpointDist + abovePlayer;
				var pathDist = (double)SpawnResources.rnd.Next((int)Settings.SpaceCargoShips.MinPathDistance, (int)Settings.SpaceCargoShips.MaxPathDistance);
				var pathDir = SpawnResources.GetRandomCompassDirection(abovePlayer, planet);
				var pathHalfDist = pathDist / 2;
				
				var tempPathStart = pathDir * pathHalfDist + pathMidpoint;
				pathDir = pathDir * -1;
				var tempPathEnd = pathDir * pathHalfDist + pathMidpoint;
				
				bool badPath = false;
				
				IHitInfo hitInfo = null;
				
				if(MyAPIGateway.Physics.CastLongRay(tempPathStart, tempPathEnd, out hitInfo, true) == true){
					
					continue;
					
				}
				
					
				foreach(var entity in SpawnResources.EntityList){
					
					if(Vector3D.Distance(tempPathStart, entity.GetPosition()) < Settings.SpaceCargoShips.MinSpawnDistFromEntities){
						
						badPath = true;
						break;
						
					}
					
				}
				
				if(badPath == true){
					
					continue;
					
				}
				
				var upDir = Vector3D.CalculatePerpendicularVector(pathDir);
				var pathMatrix = MatrixD.CreateWorld(tempPathStart, pathDir, upDir);
				
				foreach(var prefab in spawnGroup.Prefabs){
					
					double stepDistance = 0;
					var tempPrefabStart = Vector3D.Transform((Vector3D)prefab.Position, pathMatrix);
					
					while(stepDistance < pathDist){

						stepDistance += Settings.SpaceCargoShips.PathCheckStep;
						var pathCheckCoords = pathDir * stepDistance + tempPrefabStart;
						
						if(SpawnResources.IsPositionInSafeZone(pathCheckCoords) == true || SpawnResources.IsPositionInGravity(pathCheckCoords, planet) == true){
							
							badPath = true;
							break;
							
						}
												
					}
					
					if(badPath == true){
							
						break;
						
					}

				}

				if(badPath == true){
					
					continue;
					
				}
				
				startPathCoords = tempPathStart;
				endPathCoords = tempPathEnd;
				
				return true;
				
			}
			
			return false;
			
		}
		
		public static List<ImprovedSpawnGroup> GetSpaceCargoShips(Vector3D playerCoords){
			
			MyPlanet planet = SpawnResources.GetNearestPlanet(playerCoords);
			bool allowLunar = false;
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
			
			if(SpawnResources.IsPositionInGravity(playerCoords, planet) == true){
				
				if(SpawnResources.LunarSpawnEligible(playerCoords) == true){
					
					allowLunar = true;
					
				}else{
					
					return new List<ImprovedSpawnGroup>();
					
				}
				
			}
			
			string planetName = "";
			
			if(planet != null){
				
				planetName = planet.Generator.Id.SubtypeId.ToString();
				
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
				
				if(spawnGroup.SpaceCargoShip == false){
					
					if(allowLunar == true){
						
						if(spawnGroup.LunarCargoShip == false){
							
							continue;
							
						}
						
					}else{
						
						continue;
						
					}
					
				}
				
				if(spawnGroup.UniqueEncounter == true && SpawnGroupManager.UniqueGroupsSpawned.Contains(spawnGroup.SpawnGroup.Id.SubtypeName) == true){
					
					continue;
					
				}
				
				if(SpawnGroupManager.DistanceFromCenterCheck(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(planetName != ""){
					
					if(SpawnGroupManager.CheckSpawnGroupPlanetLists(spawnGroup, planet) == false){
					
						continue;
						
					}
					
				}
				
				if(SpawnResources.TerritoryValidation(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(spawnGroup.UseThreatLevelCheck == true){
					
					var threatLevel = SpawnResources.GetThreatLevel(spawnGroup, playerCoords);
					threatLevel -= (float)Settings.General.ThreatReductionHandicap;
					
					if(threatLevel < (float)spawnGroup.ThreatScoreMinimum && (float)spawnGroup.ThreatScoreMinimum > 0){
						
						continue;
						
					}
					
					if(threatLevel > (float)spawnGroup.ThreatScoreMaximum && (float)spawnGroup.ThreatScoreMaximum > 0){
						
						continue;
						
					}
					
				}
				
				if(spawnGroup.Frequency > 0){
					
					if(Settings.SpaceCargoShips.UseMaxSpawnGroupFrequency == true && spawnGroup.Frequency > Settings.SpaceCargoShips.MaxSpawnGroupFrequency * 10){
						
						spawnGroup.Frequency = (int)Math.Round((double)Settings.SpaceCargoShips.MaxSpawnGroupFrequency * 10);
						
					}
					
					for(int i = 0; i < spawnGroup.Frequency; i++){
						
						eligibleGroups.Add(spawnGroup);
						
					}
					
				}
				
			}
			
			return eligibleGroups;
			
		}
			
	}
	
}