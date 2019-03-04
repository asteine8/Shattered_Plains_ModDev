using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
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
	
	public class DebugLogger{
		
		public TextWriter FileWrite;
		public string DebugLogName = "";
		
		public DebugLogger(){
			
			FileWrite = null;
			DebugLogName = "";
			
		}
		
		public void InitDebugLogger(){
			
			DateTime currentTime = DateTime.Now;
			this.DebugLogName = "DebugLog-" + currentTime.ToString("MMdddyyyyHHm");
			FileWrite = MyAPIGateway.Utilities.WriteFileInLocalStorage(this.DebugLogName, typeof(DebugLogger));
			
		}
		
	}
	
}