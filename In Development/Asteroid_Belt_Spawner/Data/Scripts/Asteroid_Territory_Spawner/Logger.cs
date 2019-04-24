using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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

namespace Asteroid_Territory_Spawner {
    public class Logger {

        private TextWriter writer;
        private string FileName;

        public bool DebugMode;
        public Logger(string fileName) {
            DebugMode = false;
            this.Init(fileName);
        }

        public void Init(string fileName) {
            this.FileName = fileName;

            if (MyAPIGateway.Utilities == null) {
                return;
            }
            if (this.writer != null) {
                this.Close();
            }
            this.writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logger));

            this.Log("Initialized Log");
        }

        public void Log(string str) {
            try {
                string output = MyAPIGateway.Session.ElapsedPlayTime.ToString();
                output += ": " + str;
                this.writer.WriteLine(output);

                if (DebugMode) {
                    MyAPIGateway.Utilities.ShowNotification(output, 5000);
                }
            } catch(Exception e) {
                this.Init(this.FileName);
                return; // Do something maybe?
            }
        }

        public void Close() {
            this.Log("Closed Log");

            this.writer.Flush();
            this.writer.Close();
        }

    }
}
