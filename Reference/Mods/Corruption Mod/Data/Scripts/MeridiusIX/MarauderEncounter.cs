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
using Sandbox.ModAPI.Ingame;
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

namespace MeridiusIX{
	
	public class MarauderEncounter{
		
		public string NameGPS {get; set;}
		public Vector3D EncounterCoords {get; set;}
		public List<IMyPlayer> EncounterPlayers {get; set;}
		public bool PlayerProximity {get; set;}
		public bool EncounterTriggered {get; set;}
		public int EncounterTimer {get; set;}
		public int CooldownTimer {get; set;}
		
		public MarauderEncounter(){
			
			NameGPS = "Dangerous Encounter";
			EncounterCoords = new Vector3D(0,0,0);
			EncounterPlayers = new List<IMyPlayer>();
			PlayerProximity = false;
			EncounterTriggered = false;
			EncounterTimer = 0;
			CooldownTimer = 0;
			
		}
		
	}
	
}