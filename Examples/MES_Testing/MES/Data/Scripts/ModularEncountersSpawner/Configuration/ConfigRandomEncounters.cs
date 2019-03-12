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

namespace ModularEncountersSpawner.Configuration{
	
	public class ConfigRandomEncounters{
		
		public float ModVersion {get; set;}
		public int PlayerSpawnCooldown {get; set;}
		public int SpawnTimerTrigger {get; set;}
		public double PlayerTravelDistance {get; set;}
		public int MaxShipsPerArea {get; set;}
		public double AreaSize {get; set;}
		public double MinSpawnDistanceFromPlayer {get; set;}
		public double MaxSpawnDistanceFromPlayer {get; set;}
		public double MinDistanceFromOtherEntities {get; set;}
		public int SpawnAttempts {get; set;}
		
		public bool UseMaxSpawnGroupFrequency {get; set;}
		public int MaxSpawnGroupFrequency {get; set;}
		
		public double DespawnDistanceFromPlayer {get; set;}
		
		public bool UseCleanupSettings {get; set;}
		public bool CleanupUseDistance {get; set;}
		public bool CleanupUseTimer {get; set;}
		public bool CleanupUseBlockLimit {get; set;}
		public bool CleanupDistanceStartsTimer {get; set;}
		public bool CleanupResetTimerWithinDistance {get; set;}
		public double CleanupDistanceTrigger {get; set;}
		public int CleanupTimerTrigger {get; set;}
		public int CleanupBlockLimitTrigger {get; set;}
		public bool CleanupIncludeUnowned {get; set;}
		public bool CleanupUnpoweredOverride {get; set;}
		public double CleanupUnpoweredDistanceTrigger {get; set;}
		public int CleanupUnpoweredTimerTrigger {get; set;}
		
		public bool UseBlockDisable {get; set;}
		public bool DisableAirVent {get; set;}
		public bool DisableAntenna {get; set;}
		public bool DisableArtificialMass {get; set;}
		public bool DisableAssembler {get; set;}
		public bool DisableBattery {get; set;}
		public bool DisableBeacon {get; set;}
		public bool DisableCollector {get; set;}
		public bool DisableConnector {get; set;}
		public bool DisableConveyorSorter {get; set;}
		public bool DisableDecoy {get; set;}
		public bool DisableDrill {get; set;}
		public bool DisableJumpDrive {get; set;}
		public bool DisableGasGenerator {get; set;}
		public bool DisableGasTank {get; set;}
		public bool DisableGatlingGun {get; set;}
		public bool DisableGatlingTurret {get; set;}
		public bool DisableGravityGenerator {get; set;}
		public bool DisableGrinder {get; set;}
		public bool DisableGyro {get; set;}
		public bool DisableInteriorTurret {get; set;}
		public bool DisableLandingGear {get; set;}
		public bool DisableLaserAntenna {get; set;}
		public bool DisableLcdPanel {get; set;}
		public bool DisableLightBlock {get; set;}
		public bool DisableMedicalRoom {get; set;}
		public bool DisableMergeBlock {get; set;}
		public bool DisableMissileTurret {get; set;}
		public bool DisableOxygenFarm {get; set;}
		public bool DisableParachuteHatch {get; set;}
		public bool DisablePiston {get; set;}
		public bool DisableProgrammableBlock {get; set;}
		public bool DisableProjector {get; set;}
		public bool DisableReactor {get; set;}
		public bool DisableRefinery {get; set;}
		public bool DisableRocketLauncher {get; set;}
		public bool DisableReloadableRocketLauncher {get; set;}
		public bool DisableRotor {get; set;}
		public bool DisableSensor {get; set;}
		public bool DisableSolarPanel {get; set;}
		public bool DisableSoundBlock {get; set;}
		public bool DisableSpaceBall {get; set;}
		public bool DisableTimerBlock {get; set;}
		public bool DisableThruster {get; set;}
		public bool DisableWelder {get; set;}
		public bool DisableUpgradeModule {get; set;}
		
		public ConfigRandomEncounters(){
			
			ModVersion = MES_SessionCore.ModVersion;
			PlayerSpawnCooldown = 300;
			SpawnTimerTrigger = 60;
			PlayerTravelDistance = 15000;
			MaxShipsPerArea = 10;
			AreaSize = 25000;
			MinSpawnDistanceFromPlayer = 8000;
			MaxSpawnDistanceFromPlayer = 12000;
			MinDistanceFromOtherEntities = 3000;
			SpawnAttempts = 10;
			
			UseMaxSpawnGroupFrequency = false;
			MaxSpawnGroupFrequency = 5;
			
			DespawnDistanceFromPlayer = 1000;
			
			UseCleanupSettings = true;
			CleanupUseDistance = true;
			CleanupUseTimer = true;
			CleanupUseBlockLimit = false;
			CleanupDistanceStartsTimer = true;
			CleanupResetTimerWithinDistance = false;
			CleanupDistanceTrigger = 50000;
			CleanupTimerTrigger = 1800;
			CleanupBlockLimitTrigger = 0;
			CleanupIncludeUnowned = true;
			CleanupUnpoweredOverride = false;
			CleanupUnpoweredDistanceTrigger = 25000;
			CleanupUnpoweredTimerTrigger = 900;
			
			UseBlockDisable = false;
			DisableAirVent = false;
			DisableAntenna = false;
			DisableArtificialMass = false;
			DisableAssembler = false;
			DisableBattery = false;
			DisableBeacon = false;
			DisableCollector = false;
			DisableConnector = false;
			DisableConveyorSorter = false;
			DisableDecoy = false;
			DisableDrill = false;
			DisableJumpDrive = false;
			DisableGasGenerator = false;
			DisableGasTank = false;
			DisableGatlingGun = false;
			DisableGatlingTurret = false;
			DisableGravityGenerator = false;
			DisableGrinder = false;
			DisableGyro = false;
			DisableInteriorTurret = false;
			DisableLandingGear = false;
			DisableLaserAntenna = false;
			DisableLcdPanel = false;
			DisableLightBlock = false;
			DisableMedicalRoom = false;
			DisableMergeBlock = false;
			DisableMissileTurret = false;
			DisableOxygenFarm = false;
			DisableParachuteHatch = false;
			DisablePiston = false;
			DisableProgrammableBlock = false;
			DisableProjector = false;
			DisableReactor = false;
			DisableRefinery = false;
			DisableRocketLauncher = false;
			DisableReloadableRocketLauncher = false;
			DisableRotor = false;
			DisableSensor = false;
			DisableSolarPanel = false;
			DisableSoundBlock = false;
			DisableSpaceBall = false;
			DisableTimerBlock = false;
			DisableThruster = false;
			DisableWelder = false;
			DisableUpgradeModule = false;
			
		}
		
		public ConfigRandomEncounters LoadSettings(){
			
			if(MyAPIGateway.Utilities.FileExistsInWorldStorage("Config-RandomEncounters.xml", typeof(ConfigRandomEncounters)) == true){
				
				try{
					
					ConfigRandomEncounters config = null;
					var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("Config-RandomEncounters.xml", typeof(ConfigRandomEncounters));
					string configcontents = reader.ReadToEnd();
					config = MyAPIGateway.Utilities.SerializeFromXML<ConfigRandomEncounters>(configcontents);
					Logger.AddMsg("Loaded Existing Settings From Config-RandomEncounters.xml");
					return config;
					
				}catch(Exception exc){
					
					Logger.AddMsg("ERROR: Could Not Load Settings From Config-RandomEncounters.xml. Using Default Configuration.");
					var defaultSettings = new ConfigRandomEncounters();
					return defaultSettings;
					
				}
				
			}
			
			var settings = new ConfigRandomEncounters();
			
			try{
				
				using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-RandomEncounters.xml", typeof(ConfigRandomEncounters))){
				
					writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConfigRandomEncounters>(settings));
				
				}
				
			}catch(Exception exc){
				
				Logger.AddMsg("ERROR: Could Not Create Config-RandomEncounters.xml. Default Settings Will Be Used.");
				
			}
			
			return settings;
			
		}
		
		public string SaveSettings(ConfigRandomEncounters settings){
			
			try{
				
				using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("Config-RandomEncounters.xml", typeof(ConfigRandomEncounters))){
					
					writer.Write(MyAPIGateway.Utilities.SerializeToXML<ConfigRandomEncounters>(settings));
				
				}
				
				Logger.AddMsg("Settings In Config-RandomEncounters.xml Updated Successfully!");
				return "Settings Updated Successfully.";
				
			}catch(Exception exc){
				
				Logger.AddMsg("ERROR: Could Not Save To Config-RandomEncounters.xml. Changes Will Be Lost On World Reload.");
				
			}
			
			return "Settings Changed, But Could Not Be Saved To XML. Changes May Be Lost On Session Reload.";
			
		}
		
	}
		
}