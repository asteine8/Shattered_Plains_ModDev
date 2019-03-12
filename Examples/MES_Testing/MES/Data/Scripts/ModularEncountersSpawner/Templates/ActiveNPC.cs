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

namespace ModularEncountersSpawner.Templates{
	
	public class ActiveNPC{
		
		public string Name {get; set;}
		public string GridName {get; set;}
		public IMyCubeGrid CubeGrid {get; set;}
		public Vector3D StartCoords {get; set;}
		public Vector3D EndCoords {get; set;}
		public Vector3D CurrentCoords {get; set;}
		public MyPlanet Planet {get; set;}
		public float AutoPilotSpeed {get; set;}
		public IMyRemoteControl RemoteControl {get; set;}
		public List<IMyGasTank> HydrogenTanks {get; set;}
		public List<IMyGasGenerator> GasGenerators {get; set;}
		public string SpawnType {get; set;}
		public bool CleanupIgnore {get; set;}
		public int CleanupTime {get; set;}
		public bool KeenBehaviorCheck {get; set;}
		public string KeenAiName {get; set;}
		public float KeenAiTriggerDistance {get; set;}
		public bool FullyNPCOwned {get; set;}
		public bool FlagForDespawn {get; set;}
		public bool CheckedBlockCount {get; set;}
		public bool FixTurrets {get; set;}
		public bool ReplacedWeapons {get; set;}
		public bool ReplenishedSystems {get; set;}
		public bool DisabledBlocks {get; set;}
		public bool ForceStaticGrid {get; set;}
		
		public ActiveNPC(){
			
			Name = "";
			GridName = "";
			CubeGrid = null;
			StartCoords = Vector3D.Zero;
			EndCoords = Vector3D.Zero;
			CurrentCoords = Vector3D.Zero;
			Planet = null;
			AutoPilotSpeed = 0;
			RemoteControl = null;
			HydrogenTanks = new List<IMyGasTank>();
			GasGenerators = new List<IMyGasGenerator>();
			SpawnType = "Other";
			CleanupIgnore = false;
			CleanupTime = 0;
			KeenBehaviorCheck = false;
			KeenAiName = "";
			KeenAiTriggerDistance = 0;
			FullyNPCOwned = true;
			FlagForDespawn = false;
			CheckedBlockCount = false;
			FixTurrets = false;
			ReplacedWeapons = false;
			ReplenishedSystems = true;
			DisabledBlocks = false;
			ForceStaticGrid = false;
			
		}
		
	}
	
}