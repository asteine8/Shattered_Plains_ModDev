using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Asteroid_Territory_Spawner {
    public class Logger {

        private TextWriter writer;
        public Logger(string fileName) {
            this.writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logger));
        }

        public void Log(string str) {
            writer.WriteLine(str);
            writer.Flush();
        }

        public void Close() {
            this.Log("End of Log");
            writer.Flush();
            writer.Close();
        }
    }
}
