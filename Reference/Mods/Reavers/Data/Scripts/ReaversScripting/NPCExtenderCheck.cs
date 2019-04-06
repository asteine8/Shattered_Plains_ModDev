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

namespace ReaversScripting{	
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	
	public class NPCExtenderCheck : MySessionComponentBase{
		
		bool scriptInit = false;
		
		public override void UpdateBeforeSimulation(){
			
			if(scriptInit == false){
				
				scriptInit = true;
				var modsList = MyAPIGateway.Session.Mods;
				
				foreach(var mod in modsList){
					
					if(mod.PublishedFileId == 1400364273){
						
						return;
						
					}
					
				}
				
				string msg = "Reavers Mod Requires 'NPC Programming Extender' Mod To Properly Function.";
				MyVisualScriptLogicProvider.ShowNotificationToAll(msg, 30000, "Red");
				msg = "Please Add The Required Mod And Restart Your Session.";
				MyVisualScriptLogicProvider.ShowNotificationToAll(msg, 30000, "Red");
				
			}
			
		}
		
	}
	
}