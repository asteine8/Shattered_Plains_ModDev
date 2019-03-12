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

	public static class SpawnResources{
		
		public static HashSet<IMyEntity> EntityList = new HashSet<IMyEntity>();
		public static List<MySafeZone> safezoneList = new List<MySafeZone>();
		public static List<MyPlanet> PlanetList = new List<MyPlanet>();
		public static DateTime LastEntityRefresh = DateTime.Now;
		public static DateTime GameStartTime = DateTime.Now;
		
		public static Dictionary<IMyCubeGrid, float> GridThreatLevels = new Dictionary<IMyCubeGrid, float>();
		public static DateTime LastThreatRefresh = DateTime.Now;
		
		public static Random rnd = new Random();
		
		public static void RefreshEntityLists(){
			/*
			var currentTime = DateTime.Now;
			var timeDifference = currentTime - LastEntityRefresh;
			
			if(timeDifference.TotalMilliseconds < 50){
				
				return;
				
			}
			
			LastEntityRefresh = currentTime;
			*/
			EntityList.Clear();
			safezoneList.Clear();
			PlanetList.Clear();
			
			MyAPIGateway.Entities.GetEntities(EntityList);
			
			foreach(var entity in EntityList){
				
				if(entity as MySafeZone != null){
					
					safezoneList.Add(entity as MySafeZone);
					
				}
				
				if(entity as MyPlanet != null){
					
					PlanetList.Add(entity as MyPlanet);
					
				}
				
			}
	
		}
		
		public static Vector3D CreateDirectionAndTarget(Vector3D startDirCoords, Vector3D endDirCoords, Vector3D startPathCoords, double pathDistance){
	
			var direction = Vector3D.Normalize(endDirCoords - startDirCoords);
			var coords = direction * pathDistance + startPathCoords;
			return coords;
			
		}
				
		public static double GetDistanceFromSurface(Vector3D position, MyPlanet planet){
			
			if(planet == null){
				
				return 0;
				
			}
			
			var thisPosition = position;
			var surfacePoint = planet.GetClosestSurfacePointGlobal(ref thisPosition);
			return Vector3D.Distance(thisPosition, surfacePoint);
			
		}
		
		public static void GetGridThreatLevels(){
		
			var currentTime = DateTime.Now;
			TimeSpan threatTimeDifference = currentTime - LastThreatRefresh;
			
			if(threatTimeDifference.TotalMilliseconds < Settings.General.ThreatRefreshTimerMinimum * 1000){
				
				return;
				
			}
			
			LastThreatRefresh = currentTime;
			
			GridThreatLevels.Clear();
			
			var specialModdedBlocks = new Dictionary<string, float>();
			specialModdedBlocks.Add("SELtdSmallNanobotBuildAndRepairSystem", 10);
			specialModdedBlocks.Add("SELtdLargeNanobotBuildAndRepairSystem", 10);
			specialModdedBlocks.Add("LargeShipSmallShieldGeneratorBase", 10);
			specialModdedBlocks.Add("LargeShipLargeShieldGeneratorBase", 20);
			specialModdedBlocks.Add("SmallShipSmallShieldGeneratorBase", 7);
			specialModdedBlocks.Add("SmallShipMicroShieldGeneratorBase", 3);
			specialModdedBlocks.Add("DefenseShieldsLS", 10);
			specialModdedBlocks.Add("DefenseShieldsSS", 7);
			specialModdedBlocks.Add("DefenseShieldsST", 20);
			specialModdedBlocks.Add("LargeNaniteFactory", 15);
			
			foreach(var entity in EntityList){
				
				float gridThreat = 0;
				
				var cubeGrid = entity as IMyCubeGrid;
				
				if(cubeGrid == null){
					
					continue;
					
				}
				
				var blockList = new List<IMySlimBlock>();
				cubeGrid.GetBlocks(blockList);
				
				foreach(var block in blockList){
					
					if(block.FatBlock == null || block.CubeGrid.EntityId != cubeGrid.EntityId){
						
						continue;
						
					}
					
					if(block.FatBlock.IsFunctional == false){
						
						continue;
						
					}
					
					if(specialModdedBlocks.ContainsKey(block.BlockDefinition.Id.SubtypeName) == true){
						
						gridThreat += specialModdedBlocks[block.BlockDefinition.Id.SubtypeName];
						continue;
						
					}
					
					//Weapons
					if(block.FatBlock as IMyUserControllableGun != null){
						
						gridThreat += 5;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 5;
							
						}
						
						continue;
						
					}
					
					//Production
					if(block.FatBlock as IMyProductionBlock != null){
						
						gridThreat += 1.5f;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 1.5f;
							
						}
						
						continue;
						
					}
					
					//ToolBlock
					if(block.FatBlock as IMyShipToolBase != null){
						
						gridThreat += 1;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 1;
							
						}
						
						continue;
						
					}
					
					//Thruster
					if(block.FatBlock as IMyThrust != null){
						
						gridThreat += 1;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 1;
							
						}
						
						continue;
						
					}
					
					//Reactor
					if(block.FatBlock as IMyReactor != null){
						
						gridThreat += 2;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 2;
							
						}
						
						continue;
						
					}
					
					//Cargo
					if(block.FatBlock as IMyCargoContainer != null){
						
						gridThreat += 0.5f;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 0.5f;
							
						}
						
						continue;
						
					}
					
					//Antenna
					if(block.FatBlock as IMyCargoContainer != null){
						
						gridThreat += 4;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 4;
							
						}
						
						continue;
						
					}
					
					//Beacon
					if(block.FatBlock as IMyCargoContainer != null){
						
						gridThreat += 3;
						
						if(string.IsNullOrEmpty(block.BlockDefinition.Context.ModId) == false){
							
							gridThreat += 3;
							
						}
						
						continue;
						
					}
					
				}
				
				float blockCount = (float)blockList.Count / 100;
				gridThreat += blockCount;
				
				if(cubeGrid.GridSizeEnum == MyCubeSize.Large){
					
					gridThreat *= 2.5f;
					
				}else{
					
					gridThreat *= 0.5f;
					
				}
				
				if(GridThreatLevels.ContainsKey(cubeGrid) == false){
					
					GridThreatLevels.Add(cubeGrid, gridThreat);
					
				}
				
			}
			
		}
		
		public static Vector3D GetNearestSurfacePoint(Vector3D position, MyPlanet planet){
			
			if(planet == null){
				
				return Vector3D.Zero;
				
			}
			
			var thisPosition = position;
			var surfacePoint = planet.GetClosestSurfacePointGlobal(ref thisPosition);
			return surfacePoint;
			
		}
		
		public static IMyPlayer GetNearestPlayer(Vector3D checkCoords){
			
			IMyPlayer thisPlayer = null;
			double distance = -1;
			
			foreach(var player in MES_SessionCore.PlayerList){
				
				if(player.Character == null || player.IsBot == true){
					
					continue;
					
				}
				
				var currentDist = Vector3D.Distance(player.GetPosition(), checkCoords);
				
				if(thisPlayer == null){
					
					thisPlayer = player;
					distance = currentDist;
					
				}
				
				if(currentDist < distance){
					
					thisPlayer = player;
					distance = currentDist;
					
				}
				
			}
			
			return thisPlayer;
			
		}
		
		public static MyPlanet GetNearestPlanet(Vector3D position){
			
			MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);
			
			return planet;
			
		}
		
		public static Vector3D GetRandomCompassDirection(Vector3D position, MyPlanet planet){
			
			if(planet == null){
				
				return Vector3D.Zero;
				
			}
			
			var planetEntity = planet as IMyEntity;
			var upDir = Vector3D.Normalize(position - planetEntity.GetPosition());
			var forwardDir = MyUtils.GetRandomPerpendicularVector(ref upDir);
			return Vector3D.Normalize(forwardDir);
			
		}
		
		public static double GetRandomPathDist(double minValue, double maxValue){
			
			return (double)rnd.Next((int)minValue, (int)maxValue);
			
		}
		
		public static float GetThreatLevel(ImprovedSpawnGroup spawnGroup, Vector3D startCoords){
			
			float totalThreatLevel = 0;
			
			GetGridThreatLevels();
			
			foreach(var cubeGrid in GridThreatLevels.Keys.ToList()){
				
				if(cubeGrid == null || MyAPIGateway.Entities.Exist(cubeGrid) == false){
					
					GridThreatLevels.Remove(cubeGrid);
					continue;
					
				}
				
				if(Vector3D.Distance(startCoords, cubeGrid.GetPosition()) > spawnGroup.ThreatLevelCheckRange){

					continue;
					
				}
				
				bool validOwner = false;
				
				if(cubeGrid.BigOwners.Count > 0){

					foreach(var owner in cubeGrid.BigOwners){
						
						if(owner == 0){
							
							Logger.AddMsg("NoOwner", true);
							continue;
							
						}
						
						if(NPCWatcher.NPCFactionTagToFounder.ContainsKey(spawnGroup.FactionOwner) == true){
							
							if(NPCWatcher.NPCFactionTagToFounder[spawnGroup.FactionOwner] == owner){
								
								break;
								
							}
							
						}
						
						if(spawnGroup.ThreatIncludeOtherNpcOwners == false && NPCWatcher.NPCFactionFounders.Contains(owner) == true){
							
							continue;
							
						}
						
						validOwner = true;
						
					}
										
				}
				
				if(validOwner == false){
					
					continue;
					
				}
				
				totalThreatLevel += GridThreatLevels[cubeGrid];
				
			}
			
			return totalThreatLevel - Settings.General.ThreatReductionHandicap;
			
		}
		
		public static bool IsPositionNearEntities(Vector3D coords, double distance){
			
			foreach(var entity in EntityList){
				
				if(entity as IMyCubeGrid == null && entity as IMyCharacter == null){
					
					continue;
					
				}
				
				if(Vector3D.Distance(coords, entity.GetPosition()) < distance){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		public static bool IsPositionInGravity(Vector3D position, MyPlanet planet){
			
			if(planet == null){
				
				return false;
				
			}
			
			var planetEntity = planet as IMyEntity;
			var gravityProvider = planetEntity.Components.Get<MyGravityProviderComponent>();
			
			if(gravityProvider.IsPositionInRange(position) == true){
							
				return true;
				
			}
			
			return false;
			
		}
		
		public static bool IsPositionInSafeZone(Vector3D position){
			
			foreach(var safezone in safezoneList){
				
				var zoneEntity = safezone as IMyEntity;
				var checkPosition = position;
				bool inZone = false;
				
				if (safezone.Shape == MySafeZoneShape.Sphere){
					
					if(zoneEntity.PositionComp.WorldVolume.Contains(checkPosition) == ContainmentType.Contains){
						
						inZone = true;
						
					}
					
				}else{
					
					MyOrientedBoundingBoxD myOrientedBoundingBoxD = new MyOrientedBoundingBoxD(zoneEntity.PositionComp.LocalAABB, zoneEntity.PositionComp.WorldMatrix);
					inZone = myOrientedBoundingBoxD.Contains(ref checkPosition);
				
				}
				
				if(inZone == true){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		public static bool TerritoryValidation(ImprovedSpawnGroup spawnGroup, Vector3D position){
			
			List<Territory> inPositionTerritories = new List<Territory>();
				
			foreach(var territory in TerritoryManager.TerritoryList){
				
				if(territory.TerritoryDefinition.BadTerritory == true || territory.Active == false){
					
					continue;
					
				}
					
				double distanceFromCenter = Vector3D.Distance(position, territory.Position);
				
				if(distanceFromCenter < territory.Radius){
					
					if(territory.NoSpawnZone == true){
						
						return false;
						
					}
					
					if(spawnGroup.Territory != ""){}
					
					if(spawnGroup.MinDistanceFromTerritoryCenter > 0 && distanceFromCenter < spawnGroup.MinDistanceFromTerritoryCenter){
						
						continue;
						
					}
					
					if(spawnGroup.MaxDistanceFromTerritoryCenter > 0 && distanceFromCenter > spawnGroup.MaxDistanceFromTerritoryCenter){
						
						continue;
						
					}
					
					inPositionTerritories.Add(territory);
					
				}
				
			}
			
			if(inPositionTerritories.Count == 0 && spawnGroup.Territory == ""){
				
				return true;
				
			}
			
			if(inPositionTerritories.Count == 0 && spawnGroup.Territory != ""){
				
				return false;
				
			}
			
			bool territoryPass = false;
			bool strictPass = false;
			bool strictFail = false;
			bool whitelistPass = false;
			bool whitelistFail = false;
			bool blacklistPass = false;
			bool blacklistFail = false;
			
			foreach(var territory in inPositionTerritories){
				
				if(spawnGroup.Territory == territory.Name){
					
					territoryPass = true;
					
				}
				
				if(territory.StrictTerritory == true && spawnGroup.Territory != territory.Name){
					
					strictFail = true;
					
				}
				
				if(territory.StrictTerritory == true && spawnGroup.Territory == territory.Name){
					
					strictPass = true;
					
				}
				
				if(territory.FactionTagWhitelist != new List<string>() && territory.FactionTagWhitelist.Contains(spawnGroup.FactionOwner) == true){
					
					whitelistPass = true;
					
				}
				
				if(territory.FactionTagWhitelist != new List<string>() && territory.FactionTagWhitelist.Contains(spawnGroup.FactionOwner) == false){
					
					whitelistFail = true;
					
				}
				
				if(territory.FactionTagBlacklist != new List<string>() && territory.FactionTagBlacklist.Contains(spawnGroup.FactionOwner) == true){
					
					blacklistPass = true;
					
				}
				
				if(territory.FactionTagBlacklist != new List<string>() && territory.FactionTagBlacklist.Contains(spawnGroup.FactionOwner) == false){
					
					blacklistFail = true;
					
				}
				
			}
			
			bool strictConflict = false;
			bool whitelistConflict = false;
			bool blacklistConflict = false;
			
			if(strictPass == true && strictFail == true){
				
				strictConflict = true;
				
			}
			
			if(blacklistPass == true && blacklistFail == true){
				
				blacklistConflict = true;
				
			}
			
			if(whitelistPass == true && whitelistFail == true){
				
				whitelistConflict = true;
				
			}
			
			if(territoryPass == false && spawnGroup.Territory != ""){
				
				return false;
				
			}
			
			if(strictFail == true && strictConflict == false){
				
				return false;
				
			}
			
			if(whitelistFail == true && whitelistConflict == false){
				
				return false;
				
			}
			
			if(blacklistFail == true && blacklistConflict == false){
				
				return false;
				
			}
			
			return true;
			
		}
		
		public static bool IsPositionNearEntity(Vector3D coords, double distance){
			
			foreach(var entity in EntityList){
				
				if(entity as IMyCubeGrid == null && entity as IMyCharacter == null){
					
					continue;
					
				}
				
				if(Vector3D.Distance(entity.GetPosition(), coords) < distance){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		public static bool LunarSpawnEligible(Vector3D checkCoords){
			
			MyPlanet planet = GetNearestPlanet(checkCoords);
			
			if(planet == null){
				
				return false;
				
			}
			
			IMyEntity planetEntity = planet as IMyEntity;
			var upDir = Vector3D.Normalize(checkCoords - planetEntity.GetPosition());
			var closestPathPoint = upDir * Settings.SpaceCargoShips.MinLunarSpawnHeight + checkCoords;
			
			if(SpawnResources.IsPositionInGravity(closestPathPoint, planet) == true){
				
				return false;
				
			}
			
			return true;
			
		}
				
	}
	
}