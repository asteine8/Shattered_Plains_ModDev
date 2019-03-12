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
	
	public static class SpawnGroupManager{
		
		public static List<ImprovedSpawnGroup> SpawnGroups = new List<ImprovedSpawnGroup>();
		public static List<string> PlanetNames = new List<string>();
		
		public static List<string> UniqueGroupsSpawned = new List<string>();
		
		public static Dictionary<string, List<MyObjectBuilder_CubeGrid>> prefabBackupList = new Dictionary<string, List<MyObjectBuilder_CubeGrid>>(); //Temporary Until Thraxus Spawner Is Added
		
		public static string AdminSpawnGroup = "";
		
		public static SpawningOptions CreateSpawningOptions(ImprovedSpawnGroup spawnGroup, MySpawnGroupDefinition.SpawnGroupPrefab prefab){
			
			var options = SpawningOptions.None;
			
			if(spawnGroup.RotateFirstCockpitToForward == true){
				
				options |= SpawningOptions.RotateFirstCockpitTowardsDirection;
				
			}
			
			if(spawnGroup.SpawnRandomCargo == true){
				
				options |= SpawningOptions.SpawnRandomCargo;
				
			}
			
			if(spawnGroup.DisableDampeners == true){
				
				options |= SpawningOptions.DisableDampeners;
				
			}
			
			//options |= SpawningOptions.SetNeutralOwner;
			
			if(spawnGroup.ReactorsOn == false){
				
				options |= SpawningOptions.TurnOffReactors;
				
			}
			
			if(prefab.PlaceToGridOrigin == true){
				
				options |= SpawningOptions.UseGridOrigin;
				
			}
			
			return options;
			
		}
		
		public static void CreateSpawnLists(){
			
			//Planet Names First
			var planetDefList = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
			foreach(var planetDef in planetDefList){
				
				PlanetNames.Add(planetDef.Id.SubtypeName);
				
			}
			
			//Get Regular SpawnGroups
			var regularSpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();

			//Get Actual SpawnGroups
			foreach(var spawnGroup in regularSpawnGroups){
				
				if(spawnGroup.Enabled == false){
					
					continue;
					
				}
				
				if(TerritoryManager.IsSpawnGroupATerritory(spawnGroup) == true){
					
					continue;
					
				}
				
				var improveSpawnGroup = new ImprovedSpawnGroup();

				if(spawnGroup.DescriptionText != null){
					
					if(spawnGroup.DescriptionText.Contains("[Modular Encounters SpawnGroup]") == true){
					
						improveSpawnGroup = GetNewSpawnGroupDetails(spawnGroup);
						SpawnGroups.Add(improveSpawnGroup);
						continue;
						
					}
					
				}

				improveSpawnGroup = GetOldSpawnGroupDetails(spawnGroup);
				SpawnGroups.Add(improveSpawnGroup);

			}
			
		}

		public static bool CheckSpawnGroupPlanetLists(ImprovedSpawnGroup spawnGroup, MyPlanet planet){
			
			string planetName = "";
				
			if(planet != null){
				
				planetName = planet.Generator.Id.SubtypeId.ToString();
				
			}else{
				
				if(spawnGroup.AtmosphericCargoShip == true){
					
					return false;
					
				}
				
				return true;
				
			}
			
			if(spawnGroup.PlanetBlacklist.Count > 0 && Settings.General.IgnorePlanetBlacklists == false){
				
				if(spawnGroup.PlanetBlacklist.Contains(planetName) == true){
					
					return false;
					
				}
				
			}
			
			if(spawnGroup.PlanetWhitelist.Count > 0 && Settings.General.IgnorePlanetWhitelists == false){
				
				if(spawnGroup.PlanetWhitelist.Contains(planetName) == false){
					
					return false;
					
				}
				
			}
			
			var planetEntity = planet as IMyEntity;
			var sealevel = Vector3D.Up * (double)planet.MinimumRadius + planetEntity.GetPosition();

			if(spawnGroup.PlanetRequiresVacuum == true && planet.GetAirDensity(sealevel) > 0){
				
				return false;
				
			}

			if(spawnGroup.PlanetRequiresAtmo == true && planet.GetAirDensity(sealevel) == 0){
				
				return false;
				
			}

			if(spawnGroup.PlanetRequiresOxygen == true && planet.GetOxygenForPosition(sealevel) == 0){
				
				return false;
				
			}

			if(spawnGroup.PlanetMinimumSize > 0 && planet.MinimumRadius * 2 < spawnGroup.PlanetMinimumSize){
				
				return false;
				
			}

			if(spawnGroup.PlanetMaximumSize > 0 && planet.MaximumRadius * 2 < spawnGroup.PlanetMaximumSize){
				
				return false;
				
			}
			
			return true;
			
		}
		
		public static bool DistanceFromCenterCheck(ImprovedSpawnGroup spawnGroup, Vector3D checkCoords){
			
			if(spawnGroup.MinSpawnFromWorldCenter > 0){
				
				if(Vector3D.Distance(Vector3D.Zero, checkCoords) < spawnGroup.MinSpawnFromWorldCenter){
					
					return false;
					
				}
				
			}
			
			if(spawnGroup.MaxSpawnFromWorldCenter > 0){
				
				if(Vector3D.Distance(Vector3D.Zero, checkCoords) > spawnGroup.MaxSpawnFromWorldCenter){
					
					return false;
					
				}
				
			}
			
			return true;
			
		}
		
		public static ImprovedSpawnGroup GetNewSpawnGroupDetails(MySpawnGroupDefinition spawnGroup){
			
			var improveSpawnGroup = new ImprovedSpawnGroup();
			var descSplit = spawnGroup.DescriptionText.Split('\n');
			bool badParse = false;
			improveSpawnGroup.SpawnGroup = spawnGroup;
			bool setDampeners = false;
			bool setAtmoRequired = false;
			bool setForceStatic = false;
						
			foreach(var tag in descSplit){

				//SpaceCargoShip
				if(tag.Contains("[SpaceCargoShip") == true){

					improveSpawnGroup.SpaceCargoShip = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
										
				}
				
				//LunarCargoShip
				if(tag.Contains("[LunarCargoShip") == true){

					improveSpawnGroup.LunarCargoShip = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//AtmosphericCargoShip
				if(tag.Contains("[AtmosphericCargoShip") == true){

					improveSpawnGroup.AtmosphericCargoShip = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//SpaceRandomEncounter
				if(tag.Contains("[SpaceRandomEncounter") == true){

					improveSpawnGroup.SpaceRandomEncounter = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetaryInstallation
				if(tag.Contains("[PlanetaryInstallation:") == true){

					improveSpawnGroup.PlanetaryInstallation = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetaryInstallationType
				if(tag.Contains("[PlanetaryInstallationType") == true){

					improveSpawnGroup.PlanetaryInstallationType = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(improveSpawnGroup.PlanetaryInstallationType == ""){
						
						improveSpawnGroup.PlanetaryInstallationType = "Small";
						
					}
					
				}
				
				//DerelictInstallationA
				if(tag.Contains("[DerelictInstallationA") == true){

					improveSpawnGroup.DerelictInstallationA = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationA, out badParse);
						
				}
				
				//DerelictInstallationB
				if(tag.Contains("[DerelictInstallationB") == true){

					improveSpawnGroup.DerelictInstallationB = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationB, out badParse);
						
				}
				
				//DerelictInstallationC
				if(tag.Contains("[DerelictInstallationC") == true){

					improveSpawnGroup.DerelictInstallationC = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationC, out badParse);
						
				}
				
				//DerelictInstallationD
				if(tag.Contains("[DerelictInstallationD") == true){

					improveSpawnGroup.DerelictInstallationD = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationD, out badParse);
						
				}
				
				//DerelictInstallationE
				if(tag.Contains("[DerelictInstallationE") == true){

					improveSpawnGroup.DerelictInstallationE = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationE, out badParse);
						
				}
				
				//DerelictInstallationF
				if(tag.Contains("[DerelictInstallationF") == true){

					improveSpawnGroup.DerelictInstallationF = TagIntCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.DerelictInstallationF, out badParse);
						
				}
				
				//BossEncounterSpace
				if(tag.Contains("[BossEncounterSpace") == true){

					improveSpawnGroup.BossEncounterSpace = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossEncounterAtmo
				if(tag.Contains("[BossEncounterAtmo") == true){

					improveSpawnGroup.BossEncounterAtmo = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossEncounterAny
				if(tag.Contains("[BossEncounterAny") == true){

					improveSpawnGroup.BossEncounterAny = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				
				//Frequency
				improveSpawnGroup.Frequency = (int)Math.Round((double)spawnGroup.Frequency * 10);
				
				//UniqueEncounter
				if(tag.Contains("[UniqueEncounter") == true){

					improveSpawnGroup.UniqueEncounter = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//FactionOwner
				if(tag.Contains("[FactionOwner") == true){

					improveSpawnGroup.FactionOwner = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					
					if(improveSpawnGroup.FactionOwner == ""){
						
						improveSpawnGroup.FactionOwner = "SPRT";
						
					}
					
				}
				
				//IgnoreCleanupRules
				if(tag.Contains("[IgnoreCleanupRules") == true){

					improveSpawnGroup.IgnoreCleanupRules = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//RandomizeWeapons
				if(tag.Contains("[RandomizeWeapons") == true){

					improveSpawnGroup.RandomizeWeapons = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//ForceStaticGrid
				if(tag.Contains("[ForceStaticGrid") == true){

					improveSpawnGroup.ForceStaticGrid = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					setForceStatic = true;
					
				}
				
				//AdminSpawnOnly
				if(tag.Contains("[AdminSpawnOnly") == true){

					improveSpawnGroup.AdminSpawnOnly = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//MinSpawnFromWorldCenter
				if(tag.Contains("[MinSpawnFromWorldCenter") == true){

					improveSpawnGroup.MinSpawnFromWorldCenter = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.MinSpawnFromWorldCenter, out badParse);
						
				}
				
				//MaxSpawnFromWorldCenter
				if(tag.Contains("[MaxSpawnFromWorldCenter") == true){

					improveSpawnGroup.MaxSpawnFromWorldCenter = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.MinSpawnFromWorldCenter, out badParse);
						
				}
				
				//PlanetBlacklist
				if(tag.Contains("[PlanetBlacklist") == true){

					improveSpawnGroup.PlanetBlacklist = TagStringListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetWhitelist
				if(tag.Contains("[PlanetWhitelist") == true){

					improveSpawnGroup.PlanetWhitelist = TagStringListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetRequiresVacuum
				if(tag.Contains("[PlanetRequiresVacuum") == true){

					improveSpawnGroup.PlanetRequiresVacuum = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetRequiresAtmo
				if(tag.Contains("[PlanetRequiresAtmo") == true){

					improveSpawnGroup.PlanetRequiresAtmo = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					setAtmoRequired = true;
						
				}
				
				//PlanetRequiresOxygen
				if(tag.Contains("[PlanetRequiresOxygen") == true){

					improveSpawnGroup.PlanetRequiresOxygen = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PlanetMinimumSize
				if(tag.Contains("[PlanetMinimumSize") == true){

					improveSpawnGroup.PlanetMinimumSize = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.PlanetMinimumSize, out badParse);
						
				}
				
				//PlanetMaximumSize
				if(tag.Contains("[PlanetMaximumSize") == true){

					improveSpawnGroup.PlanetMaximumSize = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.PlanetMaximumSize, out badParse);
						
				}
				
				//RequireAllMods
				if(tag.Contains("[RequiredMods") == true || tag.Contains("[RequireAllMods") == true){

					improveSpawnGroup.RequireAllMods = TagUlongListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//ExcludeAnyMods
				if(tag.Contains("[ExcludedMods") == true || tag.Contains("[ExcludeAnyMods") == true){

					improveSpawnGroup.ExcludeAnyMods = TagUlongListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//RequireAnyMods
				if(tag.Contains("[RequireAnyMods") == true){

					improveSpawnGroup.RequireAnyMods = TagUlongListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//ExcludeAllMods
				if(tag.Contains("[ExcludeAllMods") == true){

					improveSpawnGroup.ExcludeAllMods = TagUlongListCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//Territory
				if(tag.Contains("[Territory") == true){

					improveSpawnGroup.Territory = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//MinDistanceFromTerritoryCenter
				if(tag.Contains("[MinDistanceFromTerritoryCenter") == true){

					improveSpawnGroup.MinDistanceFromTerritoryCenter = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.MinSpawnFromWorldCenter, out badParse);
						
				}
				
				//MaxDistanceFromTerritoryCenter
				if(tag.Contains("[MaxDistanceFromTerritoryCenter") == true){

					improveSpawnGroup.MaxDistanceFromTerritoryCenter = TagDoubleCheck(tag, spawnGroup.Id.SubtypeName, improveSpawnGroup.MinSpawnFromWorldCenter, out badParse);
						
				}
				
				//RotateFirstCockpitToForward
				if(tag.Contains("[RotateFirstCockpitToForward") == true){

					improveSpawnGroup.RotateFirstCockpitToForward = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//PositionAtFirstCockpit
				if(tag.Contains("[PositionAtFirstCockpit") == true){

					improveSpawnGroup.PositionAtFirstCockpit = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//SpawnRandomCargo
				if(tag.Contains("[SpawnRandomCargo") == true){

					improveSpawnGroup.SpawnRandomCargo = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//DisableDampeners
				if(tag.Contains("[DisableDampeners") == true){

					improveSpawnGroup.DisableDampeners = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
					setDampeners = true;
						
				}
				
				//ReactorsOn
				if(tag.Contains("[ReactorsOn") == true){

					improveSpawnGroup.ReactorsOn = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossCustomAnnounceEnable
				if(tag.Contains("[BossCustomAnnounceEnable") == true){

					improveSpawnGroup.BossCustomAnnounceEnable = TagBoolCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossCustomAnnounceAuthor
				if(tag.Contains("[BossCustomAnnounceAuthor") == true){

					improveSpawnGroup.BossCustomAnnounceAuthor = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossCustomAnnounceMessage
				if(tag.Contains("[BossCustomAnnounceMessage") == true){

					improveSpawnGroup.BossCustomAnnounceMessage = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
				
				//BossCustomGPSLabel
				if(tag.Contains("[BossCustomGPSLabel") == true){

					improveSpawnGroup.BossCustomGPSLabel = TagStringCheck(tag, spawnGroup.Id.SubtypeName, out badParse);
						
				}
								
			}
			
			if(improveSpawnGroup.SpaceCargoShip == true && setDampeners == false){
				
				improveSpawnGroup.DisableDampeners = true;
				
			}
				
			if(improveSpawnGroup.AtmosphericCargoShip == true && setAtmoRequired == false){
				
				improveSpawnGroup.PlanetRequiresAtmo = true;
				
			}
			
			if(improveSpawnGroup.PlanetaryInstallation == true && setForceStatic == false){
				
				improveSpawnGroup.ForceStaticGrid = true;
				
			}
							
			return improveSpawnGroup;

		}
		
		public static ImprovedSpawnGroup GetOldSpawnGroupDetails(MySpawnGroupDefinition spawnGroup){
			
			var thisSpawnGroup = new ImprovedSpawnGroup();
			var factionList = MyAPIGateway.Session.Factions.Factions;
			var factionTags = new List<string>();
			factionTags.Add("Nobody");
			
			foreach(var faction in factionList.Keys){
				
				if(factionList[faction].IsEveryoneNpc() == true && factionList[faction].AcceptHumans == false){
					
					factionTags.Add(factionList[faction].Tag);
					
				}
				
			}
			
			thisSpawnGroup.SpawnGroup = spawnGroup;
			
			//SpawnGroup Type
			if(spawnGroup.Id.SubtypeName.Contains("(Atmo)") == true){
				
				thisSpawnGroup.AtmosphericCargoShip = true;
				thisSpawnGroup.DisableDampeners = false;
				thisSpawnGroup.PlanetRequiresAtmo = true;
				
			}
			
			if(spawnGroup.Id.SubtypeName.Contains("(Inst-") == true){
				
				thisSpawnGroup.ForceStaticGrid = true;
				thisSpawnGroup.PlanetaryInstallation = true;
				
				if(spawnGroup.Id.SubtypeName.Contains("(Inst-1)") == true){
					
					thisSpawnGroup.PlanetaryInstallationType = "Small";
					
				}
				
				if(spawnGroup.Id.SubtypeName.Contains("(Inst-2)") == true){
					
					thisSpawnGroup.PlanetaryInstallationType = "Medium";
					
				}
				
				if(spawnGroup.Id.SubtypeName.Contains("(Inst-3)") == true){
					
					thisSpawnGroup.PlanetaryInstallationType = "Large";
					
				}
				
			}
			
			if(spawnGroup.IsPirate == false && spawnGroup.IsEncounter == false && Settings.General.EnableLegacySpaceCargoShipDetection == true){
				
				thisSpawnGroup.DisableDampeners = true;
				thisSpawnGroup.SpaceCargoShip = true;
				
			}else if(spawnGroup.IsCargoShip == true){
				
				thisSpawnGroup.DisableDampeners = true;
				thisSpawnGroup.SpaceCargoShip = true;
			
			}
			
			if(spawnGroup.IsPirate == false && spawnGroup.IsEncounter == true){
				
				thisSpawnGroup.SpaceRandomEncounter = true;
				thisSpawnGroup.ReactorsOn = false;
				thisSpawnGroup.FactionOwner = "Nobody";
				
			}
			
			if(spawnGroup.IsPirate == true && spawnGroup.IsEncounter == true){
				
				thisSpawnGroup.SpaceRandomEncounter = true;
				thisSpawnGroup.FactionOwner = "SPRT";
				
			}
			
			//Factions
			foreach(var tag in factionTags){
				
				if(spawnGroup.Id.SubtypeName.Contains("(" + tag + ")") == true){
					
					thisSpawnGroup.FactionOwner = tag;
					break;
					
				}
				
			}
			
			//Planet Whitelist & Blacklist
			foreach(var planet in PlanetNames){
				
				if(spawnGroup.Id.SubtypeName.Contains("(" + planet + ")") == true && thisSpawnGroup.PlanetWhitelist.Contains(planet) == false){
					
					thisSpawnGroup.PlanetWhitelist.Add(planet);
					
				}
				
				if(spawnGroup.Id.SubtypeName.Contains("(!" + planet + ")") == true && thisSpawnGroup.PlanetBlacklist.Contains(planet) == false){
					
					thisSpawnGroup.PlanetBlacklist.Add(planet);
					
				}
				
			}
			
			//Unique
			if(spawnGroup.Id.SubtypeName.Contains("(Unique)") == true){
				
				thisSpawnGroup.UniqueEncounter = true;
				
			}
			
			//Derelict
			if(spawnGroup.Id.SubtypeName.Contains("(Wreck)") == true){
				
				thisSpawnGroup.DerelictInstallationA = 0;
				thisSpawnGroup.DerelictInstallationB = 0;
				thisSpawnGroup.DerelictInstallationC = 0;
				thisSpawnGroup.DerelictInstallationD = 0;
				thisSpawnGroup.DerelictInstallationE = 0;
				thisSpawnGroup.DerelictInstallationF = 0;
				
			}
			
			//Frequency
			thisSpawnGroup.Frequency = (int)Math.Round((double)spawnGroup.Frequency * 10);
			
			return thisSpawnGroup;
			
		}
		
		public static bool ModRestrictionCheck(ImprovedSpawnGroup spawnGroup){
			
			//Require All
			if(spawnGroup.RequireAllMods.Count > 0){
				
				foreach(var item in spawnGroup.RequireAllMods){
				
					if(MES_SessionCore.ActiveMods.Contains(item) == false){
						
						return false;
						
					}
					
				}
				
			}

			//Require Any
			if(spawnGroup.RequireAnyMods.Count > 0){
				
				bool gotMod = false;
				
				foreach(var item in spawnGroup.RequireAnyMods){
				
					if(MES_SessionCore.ActiveMods.Contains(item) == true){
						
						gotMod = true;
						break;
						
					}
					
				}
				
				if(gotMod == false){
					
					return false;
					
				}
				
			}
			
			//Exclude All
			if(spawnGroup.ExcludeAllMods.Count > 0){
				
				foreach(var item in spawnGroup.ExcludeAllMods){
				
					if(MES_SessionCore.ActiveMods.Contains(item) == true){
						
						return false;
						
					}
					
				}
				
			}
			
			//Exclude Any
			if(spawnGroup.ExcludeAnyMods.Count > 0){

				bool conditionMet = false;
				
				foreach(var item in spawnGroup.ExcludeAnyMods){
				
					if(MES_SessionCore.ActiveMods.Contains(item) == false){
						
						conditionMet = true;
						break;
						
					}
					
				}
				
				if(conditionMet == false){
					
					return false;
					
				}
				
			}
			
			return true;
			
		}
		
		public static bool IsSpawnGroupInBlacklist(string spawnGroupName){
			
			//Get Blacklist
			var blacklistGroups = new List<string>(Settings.General.NpcSpawnGroupBlacklist.ToList());
			
			//Check Blacklist
			if(blacklistGroups.Contains(spawnGroupName) == true){
				
				return true;
				
			}
			
			return false;
				
		}

		public static string [] ProcessTag(string tag){
			
			var thisTag = tag;
			thisTag = thisTag.Replace("[", "");
			thisTag = thisTag.Replace("]", "");
			var tagSplit = thisTag.Split(':');
			return tagSplit;
			
		}
		
		public static bool TagBoolCheck(string tag, string spawnGroupName, out bool badParse){
			
			bool result = false;
			badParse = false;
			var tagSplit = ProcessTag(tag);
					
			if(tagSplit.Length == 2){
				
				if(bool.TryParse(tagSplit[1], out result) == false){
					
					Logger.AddMsg("Could not process Bool tag " + tag + " from SpawnGroup " + spawnGroupName);
					badParse = true;
					
				}
				
			}else{
				
				Logger.AddMsg("Could not process Bool tag " + tag + " from SpawnGroup " + spawnGroupName);
				badParse = true;
				
			}
			
			return result;
			
		}
		
		public static double TagDoubleCheck(string tag, string spawnGroupName, double defaultValue, out bool badParse){
			
			double result = defaultValue;
			badParse = false;
			var tagSplit = ProcessTag(tag);
					
			if(tagSplit.Length == 2){
				
				if(double.TryParse(tagSplit[1], out result) == false){
					
					Logger.AddMsg("Could not process Double tag " + tag + " from SpawnGroup " + spawnGroupName);
					badParse = true;
					
				}
				
			}else{
				
				Logger.AddMsg("Could not process Double tag " + tag + " from SpawnGroup " + spawnGroupName);
				badParse = true;
				
			}
			
			return result;
			
		}
		
		public static int TagIntCheck(string tag, string spawnGroupName, int defaultValue, out bool badParse){
			
			int result = defaultValue;
			badParse = false;
			var tagSplit = ProcessTag(tag);
					
			if(tagSplit.Length == 2){
				
				if(int.TryParse(tagSplit[1], out result) == false){
					
					Logger.AddMsg("Could not process Int tag " + tag + " from SpawnGroup " + spawnGroupName);
					badParse = true;
					
				}
				
			}else{
				
				Logger.AddMsg("Could not process Int tag " + tag + " from SpawnGroup " + spawnGroupName);
				badParse = true;
				
			}
			
			return result;
			
		}
		
		public static string TagStringCheck(string tag, string spawnGroupName, out bool badParse){
			
			string result = "";
			badParse = false;
			var tagSplit = ProcessTag(tag);
			
			if(tagSplit.Length == 2){
				
				result = tagSplit[1];
				
			}else{
				
				Logger.AddMsg("Could not process String tag " + tag + " from SpawnGroup " + spawnGroupName + ". Array Length Is: " + tagSplit.Length.ToString());
				badParse = true;
				
			}
			
			return result;
			
		}
		
		public static List<string> TagStringListCheck(string tag, string spawnGroupName, out bool badParse){
			
			List<string> result = new List<string>();
			badParse = false;
			var tagSplit = ProcessTag(tag);
			
			if(tagSplit.Length == 2){
				
				var array = tagSplit[1].Split(',');
				
				foreach(var item in array){
					
					if(item == "" || item == " " || item == null){
						
						continue;
						
					}
					
					result.Add(item);
					
				}

			}else{
				
				Logger.AddMsg("Could not process String List tag " + tag + " from SpawnGroup " + spawnGroupName);
				badParse = true;
				
			}
			
			return result;
			
		}
		
		public static List<ulong> TagUlongListCheck(string tag, string spawnGroupName, out bool badParse){
			
			List<ulong> result = new List<ulong>();
			badParse = false;
			var tagSplit = ProcessTag(tag);
			
			if(tagSplit.Length == 2){
				
				var array = tagSplit[1].Split(',');
				
				foreach(var item in array){
					
					if(item == "" || item == " " || item == null){
						
						continue;
						
					}
					
					ulong modId = 0;
					
					if(ulong.TryParse(item, out modId) == false){
						
						Logger.AddMsg("Could not parse ulong List item " + item + " from SpawnGroup " + spawnGroupName);
						badParse = true;
						
					}
					
					result.Add(modId);
					
				}

			}else{
				
				Logger.AddMsg("Could not process ulong List tag " + tag + " from SpawnGroup " + spawnGroupName);
				badParse = true;
				
			}
			result.RemoveAll(item => item == 0);
			return result;
			
		}
		
	}
	
}