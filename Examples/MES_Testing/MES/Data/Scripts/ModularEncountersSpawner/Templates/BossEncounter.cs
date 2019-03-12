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
	
	public class BossEncounter{
		
		public ImprovedSpawnGroup SpawnGroup {get; set;}
		public string Type {get; set;}
		public Vector3D Position {get; set;}
		public List<long> PlayersInEncounter {get; set;}
		public int Timer {get; set;}
		public int SpawnAttempts {get; set;}
		public IMyGps GpsTemplate {get; set;}
		
		public BossEncounter(){
			
			SpawnGroup = new ImprovedSpawnGroup();
			Type = "";
			Position = new Vector3D(0,0,0);
			PlayersInEncounter = new List<long>();
			Timer = Settings.BossEncounters.SignalActiveTimer;
			SpawnAttempts = 0;
			GpsTemplate = null;
			
		}
		
	}
	
}