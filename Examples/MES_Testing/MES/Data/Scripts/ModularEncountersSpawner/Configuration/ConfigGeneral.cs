using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace ModularEncountersSpawner.Configuration{
	
	//General
	
	public class ConfigGeneral{
		
		public float ModVersion {get; set;}
		public bool EnableSpaceCargoShips {get; set;}
		public bool EnablePlanetaryCargoShips {get; set;}
		public bool EnableRandomEncounters {get; set;}
		public bool EnablePlanetaryInstallations {get; set;}
		public bool EnableBossEncounters {get; set;}
		public bool EnableGlobalNPCWeaponRandomizer {get; set;}
		public bool EnableLegacySpaceCargoShipDetection {get; set;}
		public bool UseMaxNpcGrids {get; set;}
		public bool UseGlobalEventsTimers {get; set;}
		public bool IgnorePlanetWhitelists {get; set;}
		public bool IgnorePlanetBlacklists {get; set;}
		public int ThreatRefreshTimerMinimum {get; set;}
		public int ThreatReductionHandicap {get; set;}
		public int MaxGlobalNpcGrids {get; set;}
		public int PlayerWatcherTimerTrigger {get; set;}
		public int NpcDistanceCheckTimerTrigger {get; set;}
		public int NpcOwnershipCheckTimerTrigger {get; set;}
		public int NpcCleanupCheckTimerTrigger {get; set;}
		public int NpcBlacklistCheckTimerTrigger {get; set;}
		public int SpawnedVoxelCheckTimerTrigger {get; set;}
		public double SpawnedVoxelMinimumGridDistance {get; set;}
		public string[] PlanetSpawnsDisableList {get; set;}
		public string[] NpcGridNameBlacklist {get; set;}
		public string[] NpcSpawnGroupBlacklist {get; set;}
		
		public ConfigGeneral(){
			
			ModVersion = MES_SessionCore.ModVersion;
			EnableSpaceCargoShips = true;
			EnablePlanetaryCargoShips = true;
			EnableRandomEncounters = true;
			EnablePlanetaryInstallations = true;
			EnableBossEncounters = true;
			EnableGlobalNPCWeaponRandomizer = false;
			EnableLegacySpaceCargoShipDetection = true;
			UseMaxNpcGrids = false;
			UseGlobalEventsTimers = true;
			IgnorePlanetWhitelists = false;
			IgnorePlanetBlacklists = false;
			ThreatRefreshTimerMinimum = 20;
			ThreatReductionHandicap = 0;
			MaxGlobalNpcGrids = 50;
			PlayerWatcherTimerTrigger = 10;
			NpcDistanceCheckTimerTrigger = 1;
			NpcOwnershipCheckTimerTrigger = 10;
			NpcCleanupCheckTimerTrigger = 60;
			NpcBlacklistCheckTimerTrigger = 5;
			SpawnedVoxelCheckTimerTrigger = 900;
			SpawnedVoxelMinimumGridDistance = 1000;
			PlanetSpawnsDisableList = new string[]{"Planet_SubtypeId_Here", "Planet_SubtypeId_Here"};
			NpcGridNameBlacklist = new string[]{"BlackList_Grid_Name_Here", "BlackList_Grid_Name_Here"};
			NpcSpawnGroupBlacklist = new string[]{"BlackList_SpawnGroup_Here", "BlackList_SpawnGroup_Here"};
			
		}
		
		public ConfigGeneral LoadSettings(){
			
			if(MyAPIGateway.Utilities.FileExistsInWorldStorage("Config-General.xml", typeof(ConfigGeneral)) == true){
				
				try{
					
					ConfigGeneral config = null;
					var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Config-General.xml", typeof(ConfigGeneral));
					string configcontents = reader.ReadToEnd();
					config = MyAPIGateway.Utilities.SerializeFromXML<ConfigGeneral>(configcontents);
					Logger.AddMsg("Loaded Existing Settings From Config-General.xml");
					return config;
					
				}catch(Exception exc){
					
					Logger.AddMsg("ERROR: Could Not Load Settings From Config-General.xml. Using Default Configuration.");
					var defaultSettings = new ConfigGeneral();
					return defaultSettings;
					
				}
				
			}
			
			var settings = new ConfigGeneral();
			
			try{
				
				using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-General.xml", typeof(ConfigGeneral))){
				
					writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConfigGeneral>(settings));
				
				}
				
			}catch(Exception exc){
				
				Logger.AddMsg("ERROR: Could Not Create Config-General.xml. Default Settings Will Be Used.");
				
			}
			
			return settings;
			
		}
		
		public string SaveSettings(ConfigGeneral settings){
			
			try{
				
				using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-General.xml", typeof(ConfigGeneral))){
					
					writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConfigGeneral>(settings));
				
				}
				
				Logger.AddMsg("Settings In Config-General.xml Updated Successfully!");
				return "Settings Updated Successfully.";
				
			}catch(Exception exc){
				
				Logger.AddMsg("ERROR: Could Not Save To Config-General.xml. Changes Will Be Lost On World Reload.");
				
			}
			
			return "Settings Changed, But Could Not Be Saved To XML. Changes May Be Lost On Session Reload.";
			
		}
		
	}
	
}