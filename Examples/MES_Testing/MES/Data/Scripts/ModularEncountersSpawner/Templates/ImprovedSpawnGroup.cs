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

namespace ModularEncountersSpawner.Templates{
	
	public class ImprovedSpawnGroup{
		
		public MySpawnGroupDefinition SpawnGroup {get; set;}
		
		public bool SpaceCargoShip {get; set;}
		public bool LunarCargoShip {get; set;}
		public bool AtmosphericCargoShip {get; set;}
		
		public bool SpaceRandomEncounter {get; set;}
		
		public bool PlanetaryInstallation {get; set;}
		public string PlanetaryInstallationType {get; set;}
		public int DerelictInstallationA {get; set;}
		public int DerelictInstallationB {get; set;}
		public int DerelictInstallationC {get; set;}
		public int DerelictInstallationD {get; set;}
		public int DerelictInstallationE {get; set;}
		public int DerelictInstallationF {get; set;}
		
		public bool BossEncounterSpace {get; set;}
		public bool BossEncounterAtmo {get; set;}
		public bool BossEncounterAny {get; set;}
		
		public int Frequency {get; set;}
		public bool UniqueEncounter {get; set;}
		public string FactionOwner {get; set;}
		public bool IgnoreCleanupRules {get; set;}
		public bool RandomizeWeapons {get; set;}
		public bool ReplenishSystems {get; set;}
		public bool ForceStaticGrid {get; set;}
		public bool AdminSpawnOnly {get; set;}
		
		public double MinSpawnFromWorldCenter {get; set;}
		public double MaxSpawnFromWorldCenter {get; set;}
		
		public List<string> PlanetBlacklist {get; set;}
		public List<string> PlanetWhitelist {get; set;}
		public bool PlanetRequiresVacuum {get; set;}
		public bool PlanetRequiresAtmo {get; set;}
		public bool PlanetRequiresOxygen {get; set;}
		public double PlanetMinimumSize {get; set;}
		public double PlanetMaximumSize {get; set;}
		
		public bool UseThreatLevelCheck {get; set;}
		public double ThreatLevelCheckRange {get; set;}
		public bool ThreatIncludeOtherNpcOwners {get; set;}
		public int ThreatScoreMinimum {get; set;}
		public int ThreatScoreMaximum {get; set;}
		
		public List<ulong> RequireAllMods {get; set;}
		public List<ulong> RequireAnyMods {get; set;}
		public List<ulong> ExcludeAllMods {get; set;}
		public List<ulong> ExcludeAnyMods {get; set;}

		public string Territory {get; set;}
		public double MinDistanceFromTerritoryCenter {get; set;}
		public double MaxDistanceFromTerritoryCenter {get; set;}
		
		public bool BossCustomAnnounceEnable {get; set;}
		public string BossCustomAnnounceAuthor {get; set;}
		public string BossCustomAnnounceMessage {get; set;}
		public string BossCustomGPSLabel {get; set;}
		
		public bool RotateFirstCockpitToForward {get; set;}
		public bool PositionAtFirstCockpit {get; set;}
		public bool SpawnRandomCargo {get; set;}
		public bool DisableDampeners {get; set;}
		public bool ReactorsOn {get; set;}
		public bool UseBoundingBoxCheck {get; set;}
		
		public ImprovedSpawnGroup(){
			
			SpawnGroup = null;
			
			SpaceCargoShip = false;
			LunarCargoShip = false;
			AtmosphericCargoShip = false;
			
			SpaceRandomEncounter = false;
						
			PlanetaryInstallation = false;
			PlanetaryInstallationType = "Small";
			DerelictInstallationA = 0;
			DerelictInstallationB = 0;
			DerelictInstallationC = 0;
			DerelictInstallationD = 0;
			DerelictInstallationE = 0;
			DerelictInstallationF = 0;
			
			BossEncounterSpace = false;
			BossEncounterAtmo = false;
			BossEncounterAny = false;
			
			Frequency = 0;
			UniqueEncounter = false;
			FactionOwner = "SPRT";
			IgnoreCleanupRules = false;
			RandomizeWeapons = false;
			ReplenishSystems = false;
			ForceStaticGrid = false;
			AdminSpawnOnly = false;
			
			MinSpawnFromWorldCenter = -1;
			MaxSpawnFromWorldCenter = -1;
			
			PlanetBlacklist = new List<string>();
			PlanetWhitelist = new List<string>();
			PlanetRequiresVacuum = false;
			PlanetRequiresAtmo = false;
			PlanetRequiresOxygen = false;
			PlanetMinimumSize = -1;
			PlanetMaximumSize = -1;
			
			UseThreatLevelCheck = false;
			ThreatLevelCheckRange = 5000;
			ThreatIncludeOtherNpcOwners = false;
			ThreatScoreMinimum = -1;
			ThreatScoreMaximum = -1;
			
			RequireAllMods = new List<ulong>();
			RequireAnyMods = new List<ulong>();
			ExcludeAllMods = new List<ulong>();
			ExcludeAnyMods = new List<ulong>();
			
			Territory = "";
			MinDistanceFromTerritoryCenter = -1;
			MaxDistanceFromTerritoryCenter = -1;
			
			BossCustomAnnounceEnable = false;
			BossCustomAnnounceAuthor = "";
			BossCustomAnnounceMessage = "";
			BossCustomGPSLabel = "Dangerous Encounter";
			
			RotateFirstCockpitToForward = true;
			PositionAtFirstCockpit = false;
			SpawnRandomCargo = true;
			DisableDampeners = false;
			ReactorsOn = true;
			UseBoundingBoxCheck = false;
			
		}
				
	}

}