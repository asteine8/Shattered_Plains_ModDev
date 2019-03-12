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
	
	public static class Debug{
		
		public static bool DebugMode = true;
		
		public static void CheckZone(){
			
			if(DebugMode == false){
				
				return;
				
			}
			
			var player = MyAPIGateway.Session.LocalHumanPlayer;
			SpawnResources.RefreshEntityLists();
			var check = SpawnResources.IsPositionInSafeZone(player.GetPosition());
			
			if(check == true){
				
				Logger.AddMsg("In Zone!", true);
				
			}
			
		}
		
	}
	
}