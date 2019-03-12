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
	
	public static class RandomEncounterSpawner{
		
		public static string AttemptSpawn(Vector3D startCoords){
			
			if(Settings.General.UseMaxNpcGrids == true){
				
				var totalNPCs = NPCWatcher.ActiveNPCs.Count;
				
				if(totalNPCs >= Settings.General.MaxGlobalNpcGrids){
					
					return "Spawning Aborted. Max Global NPCs Limit Reached.";
					
				}
				
			}
			
			if(NPCWatcher.ActiveNpcTypeLimitReachedForArea("RandomEncounter", startCoords, Settings.RandomEncounters.MaxShipsPerArea, Settings.RandomEncounters.AreaSize) == true){
				
				return "Too Many Random Encounter Grids in Player Area";
				
			}
			
			var spawnGroupList = GetRandomEncounters(startCoords);
			
			if(spawnGroupList.Count == 0){
				
				return "No Eligible Spawn Groups Could Be Found To Spawn Near Player.";
				
			}
			
			var spawnGroup = spawnGroupList[SpawnResources.rnd.Next(0, spawnGroupList.Count)];
			Vector3D spawnCoords = Vector3D.Zero;
			
			if(GetSpawnCoords(spawnGroup, startCoords, out spawnCoords) == false){
				
				return "Could Not Find Safe Position To Spawn Encounter";
				
			}

			//Get Directions
			var spawnMatrix = MatrixD.CreateWorld(spawnCoords);
			var successfulVoxelSpawn = false;
			var centerVoxelOffset = false;
			
			foreach(var voxel in spawnGroup.SpawnGroup.Voxels){
				
				spawnGroup.RotateFirstCockpitToForward = false;
				var voxelSpawningPosition = Vector3D.Transform((Vector3D)voxel.Offset, spawnMatrix);
				
				
				if(voxel.CenterOffset == true){
					
					voxelSpawningPosition = spawnCoords;
					
					try{
						
						var voxelSpawn = MyAPIGateway.Session.VoxelMaps.CreateVoxelMapFromStorageName(voxel.StorageName, voxel.StorageName, voxelSpawningPosition);
						
						spawnMatrix = MatrixD.CreateWorld(voxelSpawn.GetPosition());
						NPCWatcher.SpawnedVoxels.Add(voxelSpawn.EntityId.ToString(), voxelSpawn as IMyEntity);
						successfulVoxelSpawn = true;
						centerVoxelOffset = true;
						Logger.CreateDebugGPS("Matrix/BB Center Position", voxelSpawn.GetPosition());
						//Logger.CreateDebugGPS("New Spawn", voxelSpawn.GetPosition());
						Logger.CreateDebugGPS("Bottom Left", voxelSpawn.PositionLeftBottomCorner);
						Logger.AddMsg("Random Encounter -" + spawnGroup.SpawnGroup.Id.SubtypeName + "- Spawning With Unresolved Voxel Offset. Grids May Not Be Aligned To Voxels Properly.");
						
					}catch(Exception exc){
						
						Logger.AddMsg("Manual Voxel Spawning For " + voxel.StorageName + " Failed");
						
					}
					
				}else{
					
					try{
						
						var voxelSpawn = MyAPIGateway.Session.VoxelMaps.CreateVoxelMapFromStorageName(voxel.StorageName, voxel.StorageName, voxelSpawningPosition);
						NPCWatcher.SpawnedVoxels.Add(voxelSpawn.EntityId.ToString(), voxelSpawn as IMyEntity);
						successfulVoxelSpawn = true;
						
					}catch(Exception exc){
						
						Logger.AddMsg("Voxel Spawning For " + voxel.StorageName + " Failed");
						
					}
					
				}
			
			}
			
			if(successfulVoxelSpawn == true){
				
				var voxelIdList = new List<string>(NPCWatcher.SpawnedVoxels.Keys.ToList());
				string[] voxelIdArray = voxelIdList.ToArray();
				MyAPIGateway.Utilities.SetVariable<string[]>("MES-SpawnedVoxels", voxelIdArray);
				
			}
			
			foreach(var prefab in spawnGroup.SpawnGroup.Prefabs){
				
				var options = SpawnGroupManager.CreateSpawningOptions(spawnGroup, prefab);
				var spawnPosition = Vector3D.Transform((Vector3D)prefab.Position, spawnMatrix);
				Logger.CreateDebugGPS("Prefab Spawn Coords", spawnPosition);
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
						
						var clonedGridOb = prefabDef.CubeGrids[i].Clone();
						backupGridList.Add(clonedGridOb as MyObjectBuilder_CubeGrid);
						GridBuilderManipulation.ProcessGrid(prefabDef.CubeGrids[i], true, false);
						
					}
					
					if(SpawnGroupManager.prefabBackupList.ContainsKey(prefab.SubtypeId) == false){
						
						SpawnGroupManager.prefabBackupList.Add(prefab.SubtypeId, backupGridList);
						
					}
	
				}
				//Weapon Randomizer - End
				
				try{
					
					MyAPIGateway.PrefabManager.SpawnPrefab(gridList, prefab.SubtypeId, spawnPosition, spawnMatrix.Forward, spawnMatrix.Up, speedL, speedA, prefab.BeaconText, options, gridOwner);
					
				}catch(Exception exc){
					
					
					
				}
				
				var pendingNPC = new ActiveNPC();
				pendingNPC.Name = prefab.SubtypeId;
				pendingNPC.GridName = MyDefinitionManager.Static.GetPrefabDefinition(prefab.SubtypeId).CubeGrids[0].DisplayName;
				pendingNPC.StartCoords = spawnCoords;
				pendingNPC.CurrentCoords = spawnCoords;
				pendingNPC.EndCoords = spawnCoords;
				pendingNPC.SpawnType = "RandomEncounter";
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
		
		public static List<ImprovedSpawnGroup> GetRandomEncounters(Vector3D playerCoords){
			
			MyPlanet planet = SpawnResources.GetNearestPlanet(playerCoords);
			string specificGroup = "";
			
			if(SpawnGroupManager.AdminSpawnGroup != ""){
				
				specificGroup = SpawnGroupManager.AdminSpawnGroup;
				SpawnGroupManager.AdminSpawnGroup = "";
				
			}
			
			if(SpawnResources.IsPositionInGravity(playerCoords, planet) == true){
				
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
				
				if(spawnGroup.SpaceRandomEncounter == false){
					
					continue;
					
				}
				
				if(spawnGroup.UniqueEncounter == true && SpawnGroupManager.UniqueGroupsSpawned.Contains(spawnGroup.SpawnGroup.Id.SubtypeName) == true){
					
					continue;
					
				}
				
				if(SpawnGroupManager.DistanceFromCenterCheck(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(SpawnResources.TerritoryValidation(spawnGroup, playerCoords) == false){
					
					continue;
					
				}
				
				if(spawnGroup.Frequency > 0){
					
					if(Settings.RandomEncounters.UseMaxSpawnGroupFrequency == true && spawnGroup.Frequency > Settings.RandomEncounters.MaxSpawnGroupFrequency * 10){
						
						spawnGroup.Frequency = (int)Math.Round((double)Settings.RandomEncounters.MaxSpawnGroupFrequency * 10);
						
					}
					
					for(int i = 0; i < spawnGroup.Frequency; i++){
						
						eligibleGroups.Add(spawnGroup);
						
					}
					
				}
				
			}
			
			return eligibleGroups;
			
		}
		
		public static bool GetSpawnCoords(ImprovedSpawnGroup spawnGroup, Vector3D startCoords, out Vector3D spawnCoords){
			
			spawnCoords = Vector3D.Zero;
			SpawnResources.RefreshEntityLists();
			MyPlanet planet = SpawnResources.GetNearestPlanet(spawnCoords);
			
			for(int i = 0; i < Settings.RandomEncounters.SpawnAttempts; i++){
				
				var spawnDir = Vector3D.Normalize(MyUtils.GetRandomVector3D());
				var randDist = (double)SpawnResources.rnd.Next((int)Settings.RandomEncounters.MinSpawnDistanceFromPlayer, (int)Settings.RandomEncounters.MaxSpawnDistanceFromPlayer);
				var tempSpawnCoords = spawnDir * randDist + startCoords;
				
				if(SpawnResources.IsPositionInGravity(tempSpawnCoords, planet) == true){
					
					spawnDir *= -1;
					tempSpawnCoords = spawnDir * randDist + startCoords;
					
					if(SpawnResources.IsPositionInGravity(tempSpawnCoords, planet) == true){
						
						continue;
						
					}
					
				}
				
				var tempMatrix = MatrixD.CreateWorld(tempSpawnCoords);
				var badPath = false;
				
				foreach(var prefab in spawnGroup.SpawnGroup.Prefabs){
										
					var prefabCoords = Vector3D.Transform((Vector3D)prefab.Position, tempMatrix);
					planet = SpawnResources.GetNearestPlanet(prefabCoords);
					
					foreach(var entity in SpawnResources.EntityList){
						
						if(Vector3D.Distance(entity.GetPosition(), prefabCoords) < Settings.RandomEncounters.MinDistanceFromOtherEntities){
							
							badPath = true;
							break;
							
						}
						
					}

					if(SpawnResources.IsPositionInSafeZone(prefabCoords) == true || SpawnResources.IsPositionInGravity(prefabCoords, planet) == true){
						
						badPath = true;
						break;
						
					}
					
					if(badPath == true){
							
						break;
						
					}
					
				}
				
				if(badPath == true){
					
					continue;
					
				}
				
				spawnCoords = tempSpawnCoords;
				return true;
				
			}

			return false;
			
		}
			
	}
	
}