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
	
	public static class ModMessages{
		
		public static void ModMessageHandler(object obj){
			
			var message = obj as string;
			
			if(message == null){
				
				return;
				
			}
			
			//IgnoreCleanup
			if(message.StartsWith("MES.IgnoreCleanup.") == true){
				
				var msgSplit = message.Split('.');
				
				if(msgSplit.Length != 3){
					
					return;
					
				}
				
				long entityId = 0;
				
				if(long.TryParse(msgSplit[2], out entityId) == false){
					
					return;
					
				}
				
				IMyEntity entity = null;
				
				if(MyAPIGateway.Entities.TryGetEntityById(entityId, out entity) == false){
					
					return;
					
				}
				
				if(entity as IMyCubeGrid == null){
					
					return;
					
				}
				
				if(NPCWatcher.ActiveNPCs.ContainsKey(entity as IMyCubeGrid) == true){
					
					var cubeGrid = entity as IMyCubeGrid;
					NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore = true;
					
					if(cubeGrid.Storage == null){
					
						cubeGrid.Storage = new MyModStorageComponent();
						
					}
					
					if(cubeGrid.Storage.ContainsKey(NPCWatcher.GuidIgnoreCleanup) == false){
				
						cubeGrid.Storage.Add(NPCWatcher.GuidIgnoreCleanup, NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore.ToString());
						
					}else{
						
						cubeGrid.Storage[NPCWatcher.GuidIgnoreCleanup] = NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore.ToString();
						
					}
					
					Logger.AddMsg("Received Mod Message. Marked CubeGrid [" + cubeGrid.CustomName + " / " + entityId.ToString() + "] As Ignored By Spawner Cleanup.");
					
				}
				
			}
			
			//UseCleanup
			if(message.StartsWith("MES.UseCleanup.") == true){
				
				var msgSplit = message.Split('.');
				
				if(msgSplit.Length != 3){
					
					return;
					
				}
				
				long entityId = 0;
				
				if(long.TryParse(msgSplit[2], out entityId) == false){
					
					return;
					
				}
				
				IMyEntity entity = null;
				
				if(MyAPIGateway.Entities.TryGetEntityById(entityId, out entity) == false){
					
					return;
					
				}
				
				if(entity as IMyCubeGrid == null){
					
					return;
					
				}
				
				if(NPCWatcher.ActiveNPCs.ContainsKey(entity as IMyCubeGrid) == true){
					
					var cubeGrid = entity as IMyCubeGrid;
					NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore = false;
					
					if(cubeGrid.Storage == null){
					
						cubeGrid.Storage = new MyModStorageComponent();
						
					}
					
					if(cubeGrid.Storage.ContainsKey(NPCWatcher.GuidIgnoreCleanup) == false){
				
						cubeGrid.Storage.Add(NPCWatcher.GuidIgnoreCleanup, NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore.ToString());
						
					}else{
						
						cubeGrid.Storage[NPCWatcher.GuidIgnoreCleanup] = NPCWatcher.ActiveNPCs[cubeGrid].CleanupIgnore.ToString();
						
					}
					
					Logger.AddMsg("Received Mod Message. Marked CubeGrid [" + cubeGrid.CustomName + " / " + entityId.ToString() + "] As Considered By Spawner Cleanup.");
					
				}
				
			}
			
		}

	}
	
}