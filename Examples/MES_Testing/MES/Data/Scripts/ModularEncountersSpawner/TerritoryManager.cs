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
	
	public static class TerritoryManager{
		
		public static List<NPCTerritory> Territories = new List<NPCTerritory>();
		public static List<Territory> TerritoryList = new List<Territory>();
		
		public static bool FirstRun = false;
		
		public static NPCTerritory GetNewTerritoryDetails(MySpawnGroupDefinition spawnGroup){
			
			var territory = new NPCTerritory();
			var descSplit = spawnGroup.DescriptionText.Split('\n');
			var tempCoords = Vector3D.Zero;
			
			foreach(var tag in descSplit){
				
				//Name
				if(tag.Contains("[Name") == true){

					bool badParse = false;
					territory.Name = SpawnGroupManager.TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//Type
				if(tag.Contains("[Type") == true){
					
					bool badParse = false;
					territory.Type = SpawnGroupManager.TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//Active
				if(tag.Contains("[Active") == true){
					
					bool badParse = false;
					territory.Active = SpawnGroupManager.TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//Radius
				if(tag.Contains("[Radius") == true){
					
					bool badParse = false;
					territory.Radius = SpawnGroupManager.TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, territory.Radius, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//ScaleRadiusWithPlanetSize
				if(tag.Contains("[ScaleRadiusWithPlanetSize") == true){
					
					bool badParse = false;
					territory.ScaleRadiusWithPlanetSize = SpawnGroupManager.TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//NoSpawnZone
				if(tag.Contains("[NoSpawnZone") == true){
					
					bool badParse = false;
					territory.NoSpawnZone = SpawnGroupManager.TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//StrictTerritory
				if(tag.Contains("[StrictTerritory") == true){
					
					bool badParse = false;
					territory.StrictTerritory = SpawnGroupManager.TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//FactionTagWhitelist
				if(tag.Contains("[FactionTagWhitelist") == true){
					
					bool badParse = false;
					territory.FactionTagWhitelist = SpawnGroupManager.TagStringListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//FactionTagBlacklist
				if(tag.Contains("[FactionTagBlacklist") == true){
					
					bool badParse = false;
					territory.FactionTagBlacklist = SpawnGroupManager.TagStringListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
					
				//CoordsX
				if(tag.Contains("[CoordsX") == true){
					
					bool badParse = false;
					tempCoords.X = SpawnGroupManager.TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, territory.Position.X, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//CoordsY
				if(tag.Contains("[CoordsY") == true){
					
					bool badParse = false;
					tempCoords.Y = SpawnGroupManager.TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, territory.Position.Y, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//CoordsZ
				if(tag.Contains("[CoordsZ") == true){
					
					bool badParse = false;
					tempCoords.Z = SpawnGroupManager.TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, territory.Position.Z, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//AnnounceArriveDepart
				if(tag.Contains("[AnnounceArriveDepart") == true){
					
					bool badParse = false;
					territory.AnnounceArriveDepart = SpawnGroupManager.TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//CustomArriveMessage
				if(tag.Contains("[CustomArriveMessage") == true){
					
					bool badParse = false;
					territory.CustomArriveMessage = SpawnGroupManager.TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//CustomDepartMessage
				if(tag.Contains("[CustomDepartMessage") == true){
					
					bool badParse = false;
					territory.CustomDepartMessage = SpawnGroupManager.TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
				//PlanetGeneratorName
				if(tag.Contains("[PlanetGeneratorName") == true){
					
					bool badParse = false;
					territory.PlanetGeneratorName = SpawnGroupManager.TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(territory.BadTerritory == false && badParse == true){
						
						territory.BadTerritory = true;
						
					}
					
				}
				
			}

			territory.Position = tempCoords;

			return territory;
			
		}
		
		public static NPCTerritory GetOldTerritoryDetails(MySpawnGroupDefinition spawnGroup){
			
			var territory = new NPCTerritory();
			territory.Name = spawnGroup.Id.SubtypeName;
			territory.TagOld = spawnGroup.Prefabs[0].BeaconText;
			territory.Position = (Vector3D)spawnGroup.Prefabs[0].Position;
			territory.Radius = (double)spawnGroup.Prefabs[0].Speed;
			return territory;
			
		}
		
		public static bool GetSetTerritoryData(string territoryName, bool defaultValue){
			
			try{
				
				var enabled = false;
				if(MyAPIGateway.Utilities.GetVariable<bool>("MES-Territory-" + territoryName, out enabled) == false){
					
					return defaultValue;
					
				}
				
				return enabled;
				
			}catch(Exception exc){
				
				
				
			}
			
			MyAPIGateway.Utilities.SetVariable<bool>("MES-Territory-" + territoryName, defaultValue);
			return defaultValue;
						
		}
		
		public static List<NPCTerritory> GetTerritoriesAtPosition(Vector3D checkCoords){
			
			var territoryList = new List<NPCTerritory>();
			
			foreach(var territory in Territories){
				
				if(Vector3D.Distance(checkCoords, territory.Position) < territory.Radius){
					
					territoryList.Add(territory);
					
				}
				
			}
			
			return territoryList;
			
		}
		
		public static bool IsSpawnGroupATerritory(MySpawnGroupDefinition spawnGroup){
			
			if(spawnGroup.DescriptionText != null){
				
				if(spawnGroup.DescriptionText.Contains("[Modular Encounters Territory]") == true){
				
					return true;
				
				}
				
			}

			if(spawnGroup.Prefabs.Count > 0){
				
				if(spawnGroup.Prefabs[0].SubtypeId == "TerritoryPlaceholder"){
					
					return true;
					
				}
				
			}
			
			return false;
			
		}
		
		public static void ResetStoredTerritoryData(string territoryName, bool defaultValue){
			
			MyAPIGateway.Utilities.SetVariable<bool>("MES-Territory-" + territoryName, defaultValue);
						
		}
		
		public static void TerritoryRefresh(bool resetStoredData = false){
			
			Territories.Clear();
			TerritoryList.Clear();
			FirstRun = false;
			
			var regularSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
			
			foreach(var spawnGroup in regularSpawnGroups){
				
				if(spawnGroup.Enabled == false){
					
					continue;
					
				}
				
				//Check For New Tags
				if(spawnGroup.DescriptionText != null){
					
					if(spawnGroup.DescriptionText.Contains("[Modular Encounters Territory]") == true){
					
						var territory = GetNewTerritoryDetails(spawnGroup);
						
						if(resetStoredData == false){
							
							territory.Active = GetSetTerritoryData(territory.Name, territory.Active);
							
						}else{
							
							ResetStoredTerritoryData(territory.Name, territory.Active);
							
						}
						
						Territories.Add(territory);
						Logger.AddMsg("Custom Territory Found: " + territory.Name);
						continue;
						
					}
					
				}

				//Check For Old Tags
				if(spawnGroup.Prefabs.Count > 0){
					
					if(spawnGroup.Prefabs[0].SubtypeId == "TerritoryPlaceholder"){
						
						var territory = GetOldTerritoryDetails(spawnGroup);
						
						if(resetStoredData == false){
							
							territory.Active = GetSetTerritoryData(territory.Name, territory.Active);
							
						}else{
							
							ResetStoredTerritoryData(territory.Name, territory.Active);
							
						}
						
						Territories.Add(territory);
						Logger.AddMsg("Custom Territory Found: " + territory.Name);
						continue;
						
					}
					
				}
				
			}
			
			
			foreach(var fullTerritory in Territories){
				
				if(fullTerritory.BadTerritory == true && fullTerritory.Active == false){
					
					continue;
					
				}
				
				if(fullTerritory.Type == "Static"){
					
					var territory = new Territory();
					territory.Name = fullTerritory.Name;
					territory.TerritoryDefinition = fullTerritory;
					territory.Active = fullTerritory.Active;
					territory.Position = fullTerritory.Position;
					territory.Radius = fullTerritory.Radius;
					territory.NoSpawnZone = fullTerritory.NoSpawnZone;
					territory.StrictTerritory = fullTerritory.StrictTerritory;
					territory.FactionTagWhitelist = fullTerritory.FactionTagWhitelist;
					territory.FactionTagBlacklist = fullTerritory.FactionTagBlacklist;
					territory.AnnounceArriveDepart = fullTerritory.AnnounceArriveDepart;
					territory.CustomArriveMessage = fullTerritory.CustomArriveMessage;
					territory.CustomDepartMessage = fullTerritory.CustomDepartMessage;
					TerritoryList.Add(territory);
					
				}else{
					
					foreach(var planet in SpawnResources.PlanetList){
						
						if(planet == null){
							
							continue;
							
						}
						
						var planetName = planet.Generator.Id.SubtypeId.ToString();


						if(fullTerritory.PlanetGeneratorName != planetName){
							
							continue;
							
						}

						var surfaceCoords = Vector3D.Zero;
						var planetEntity = planet as IMyEntity;

						// Logger.AddMsg("PlanetPos:" + planetEntity.GetPosition().ToString("0.00"));
						
						if(fullTerritory.Position != Vector3D.Zero){
							
							var directional = fullTerritory.Position * 1000 + planetEntity.GetPosition();
							surfaceCoords = SpawnResources.GetNearestSurfacePoint(directional, planet);

							Logger.AddMsg("Calculated closest surface position\n"+surfaceCoords.ToString("0.00"));
							
						}else{
							
							surfaceCoords = planetEntity.GetPosition();

							Logger.AddMsg("Calculated center of planet\n"+surfaceCoords.ToString("0.00"));
							
						}
						
						var territory = new Territory();
						territory.Name = fullTerritory.Name;
						territory.TerritoryDefinition = fullTerritory;
						territory.Active = fullTerritory.Active;
						territory.Position = surfaceCoords;
						
						if(fullTerritory.ScaleRadiusWithPlanetSize == true){
							
							var unit = territory.Radius / 60000;
							var radius = (double)planet.AverageRadius * unit;
							territory.Radius = radius;
							
						}else{
							
							territory.Radius = fullTerritory.Radius;
							
						}
						
						territory.NoSpawnZone = fullTerritory.NoSpawnZone;
						territory.StrictTerritory = fullTerritory.StrictTerritory;
						territory.FactionTagWhitelist = fullTerritory.FactionTagWhitelist;
						territory.FactionTagBlacklist = fullTerritory.FactionTagBlacklist;
						territory.AnnounceArriveDepart = fullTerritory.AnnounceArriveDepart;
						territory.CustomArriveMessage = fullTerritory.CustomArriveMessage;
						territory.CustomDepartMessage = fullTerritory.CustomDepartMessage;
						TerritoryList.Add(territory);
						
					}
					
				}				
				
			}
			
		}
		
		public static void TerritoryWatcher(){
			
			foreach(var player in MES_SessionCore.PlayerList){
				
				if(player == null){
					
					continue;
					
				}
				
				if(player.Character == null){
					
					continue;
					
				}
				
				foreach(var territory in TerritoryList){
					
					if(territory.AnnounceArriveDepart == false){
						
						continue;
						
					}
					
					if(Vector3D.Distance(player.GetPosition(), territory.Position) < territory.Radius){
						
						if(territory.PlayersInTerritory.Contains(player.IdentityId) == false){
							
							territory.PlayersInTerritory.Add(player.IdentityId);
							
							if(territory.CustomArriveMessage == ""){
								
								if(FirstRun == true){
									
									MyVisualScriptLogicProvider.ShowNotification("Entering Territory: " + territory.Name, 5000, "White", player.IdentityId);
									
								}
								
							}else{
								
								if(FirstRun == true){
									
									MyVisualScriptLogicProvider.ShowNotification(territory.CustomArriveMessage, 5000, "White", player.IdentityId);
									
								}
								
							}
							
						}
						
					}else{
						
						if(territory.PlayersInTerritory.Contains(player.IdentityId) == true){
							
							territory.PlayersInTerritory.Remove(player.IdentityId);
							
							if(territory.CustomDepartMessage == ""){
								
								if(FirstRun == true){
									
									MyVisualScriptLogicProvider.ShowNotification("Leaving Territory: " + territory.Name, 5000, "White", player.IdentityId);
									
								}
								
							}else{
								
								if(FirstRun == true){
									
									MyVisualScriptLogicProvider.ShowNotification(territory.CustomDepartMessage, 5000, "White", player.IdentityId);
									
								}
								
							}
							
						}
					
					}
				
				}
				
			}
			
			FirstRun = true;
			
		}
		
	}
	
}