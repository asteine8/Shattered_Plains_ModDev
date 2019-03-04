using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Zkillerproxy.JumpInhibitorMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class JumpInhibitorServer : MySessionComponentBase
    {
        private JumpInhibitorConfig Config;
        private List<long> JumpInhibitors = new List<long>();
        private static readonly ushort InhibitorReport = 60101;
        private static readonly ushort InhibitorInfoRequest = 60102;
        private static readonly ushort InhibitorInfoSend = 60103;
        private static readonly ushort ConfigRequest = 60104;
        private static readonly ushort ConfigSend = 60105;
        private static readonly ushort UpdateServerTerminalSend = 60106;
        private static readonly ushort UpdateClientTerminalSend = 60107;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                if (MyAPIGateway.Utilities.FileExistsInGlobalStorage("JumpInhibitor.cfg") == true)
                {
                    string ConfigString = MyAPIGateway.Utilities.ReadFileInGlobalStorage("JumpInhibitor.cfg").ReadToEnd();

                    try
                    {
                        Config = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorConfig>(ConfigString);
                        Log("Config Loaded.");
                    }
                    catch
                    {
                        Config = new JumpInhibitorConfig(0.02f, false, true, true);
                        Log("Unable to serialize config, assuming defult.");
                    }
                }
                else
                {
                    Config = new JumpInhibitorConfig(0.02f, false, true, true);
                    Log("Did not find config in global storage, a new default config will be saved.");

                    string ConfigXML = MyAPIGateway.Utilities.SerializeToXML(Config);
                    using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage("JumpInhibitor.cfg"))
                    {
                        writer.Write(ConfigXML);
                    }
                    Log("Config Saved");
                }
                
                MyAPIGateway.Multiplayer.RegisterMessageHandler(ConfigRequest, ConfigRequestHandler);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(InhibitorReport, InhibitorReportHandler);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(InhibitorInfoRequest, InhibitorInfoRequestHandler);
                MyAPIGateway.Multiplayer.RegisterMessageHandler(UpdateServerTerminalSend, UpdateServerTerminalHandler);
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConfigRequest, ConfigRequestHandler);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(InhibitorReport, InhibitorReportHandler);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(InhibitorInfoRequest, InhibitorInfoRequestHandler);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(UpdateServerTerminalSend, UpdateServerTerminalHandler);
            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (Server): " + "Unloaded Message Handlers.");
        }

        private void ConfigRequestHandler(byte[] obj)
        {
            ulong SenderID = 0;

            try
            {
                SenderID = MyAPIGateway.Utilities.SerializeFromBinary<ulong>(obj);
            }
            catch
            {
                Log("ERROR: Failed to serialize incoming byte[] for config requester!");
            }

            MyAPIGateway.Multiplayer.SendMessageTo(ConfigSend, MyAPIGateway.Utilities.SerializeToBinary(MyAPIGateway.Utilities.SerializeToXML(Config)), SenderID);
        }

        private void InhibitorReportHandler(byte[] obj)
        {
            long InhibitorID = 0;

            try
            {
                InhibitorID = MyAPIGateway.Utilities.SerializeFromBinary<long>(obj);
            }
            catch
            {
                Log("ERROR: Failed to serialize incoming byte[] for inhibitor reporter!");
            }

            IMyEntity InhibitorEntity = MyAPIGateway.Entities.GetEntityById(InhibitorID);
            
            if (InhibitorEntity != null)
            {
                if (JumpInhibitors.Contains(InhibitorID) == false)
                {
                    JumpInhibitors.Add(InhibitorID);
                }
            }
        }

        private void InhibitorInfoRequestHandler(byte[] obj)
        {
            JumpInhibitorPackage Info = null;

            try
            {
                Info = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorPackage>(MyAPIGateway.Utilities.SerializeFromBinary<string>(obj));
            }
            catch
            {
                Log("ERROR: Failed to serialize incoming byte[] for info requester!");
            }

            if (Info != null)
            {
                JumpInhibitorPackage Package = new JumpInhibitorPackage();

                foreach (long InhibitorID in JumpInhibitors)
                {
                    IMyBeacon Beacon = MyAPIGateway.Entities.GetEntityById(InhibitorID) as IMyBeacon;

                    if (Beacon != null && Beacon.IsWorking)
                    {
                        JumpInhibitorBlock Inhibitor = Beacon.GameLogic.GetAs<JumpInhibitorBlock>();

                        if (Inhibitor != null)
                        {
                            Package.Info.Add(new JumpInhibitorInfoPackage(Inhibitor.TerminalSettings, Beacon.GetPosition(), Beacon.Radius));
                        }
                    }
                }

                Vector3D? Pos = GetGpsPos(Info.GPSHash);
                if (Pos != null)
                {
                    Package.Pos = (Vector3D)Pos;
                }
                
                MyAPIGateway.Multiplayer.SendMessageTo(InhibitorInfoSend, MyAPIGateway.Utilities.SerializeToBinary(MyAPIGateway.Utilities.SerializeToXML(Package)), Info.SenderID);
            }
        }

        private Vector3D? GetGpsPos(int hash)
        {
            if (hash != 0)
            {
                List<IMyGps> GpsList = new List<IMyGps>();
                List<IMyPlayer> Players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(Players);

                foreach (IMyPlayer Player in Players)
                {
                    GpsList.AddList(MyAPIGateway.Session.GPS.GetGpsList(Player.IdentityId));
                }

                foreach (IMyGps GPS in GpsList)
                {
                    if (GPS.Hash == hash)
                    {
                        return GPS.Coords;
                    }
                }
            }
            else
            {
                return Vector3D.PositiveInfinity;
            }
            return null;
        }

        private void UpdateServerTerminalHandler(byte[] obj)
        {
            JumpInhibitorInfoPackage InhibitorInfo = null;
            
            try
            {
                InhibitorInfo = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorInfoPackage>(MyAPIGateway.Utilities.SerializeFromBinary<string>(obj));
            }
            catch
            {
                Log("ERROR: Failed to serialize incoming byte[] for server terminal updator!");
            }

            if (InhibitorInfo != null)
            {
                IMyEntity ServerEntity = MyAPIGateway.Entities.GetEntityById(InhibitorInfo.EntityID);

                if (ServerEntity != null)
                {
                    JumpInhibitorBlock ServerInhibitor = ServerEntity.GameLogic.GetAs<JumpInhibitorBlock>();

                    if (ServerInhibitor != null)
                    {
                        ServerInhibitor.TerminalSettings = InhibitorInfo.TerminalSettings;
                        ServerInhibitor.SaveTerminalSettings(ServerEntity);
                        MyAPIGateway.Multiplayer.SendMessageToOthers(UpdateClientTerminalSend, MyAPIGateway.Utilities.SerializeToBinary(MyAPIGateway.Utilities.SerializeToXML(new JumpInhibitorInfoPackage(ServerInhibitor.TerminalSettings, ServerInhibitor.EntityID))));
                    }
                }
            }           
        }

        private void Log(string Input)
        {
            if(Config.ShowLogIngame)
            {
                MyAPIGateway.Utilities.ShowMessage("Jump Inhibitor (Server)", Input);
            }

            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (Server): " + Input);
        }
    }

    public class JumpInhibitorConfig
    {
        public float PowerJPerM;
        public bool ShowLogIngame;
        public bool AllowInhibitingJumpsIn;
        public bool AllowInhibitingJumpsOut;

        public JumpInhibitorConfig(float powerJPerM, bool showLogIngame, bool allowIn, bool allowOut)
        {
            PowerJPerM = powerJPerM;
            ShowLogIngame = showLogIngame;
            AllowInhibitingJumpsIn = allowIn;
            AllowInhibitingJumpsOut = allowOut;
        }

        public JumpInhibitorConfig()
        {
            //Parameterless constructor for serialization.
        }
    }

    public class JumpInhibitorPackage
    {
        public int GPSHash;
        public ulong SenderID;
        public Vector3D Pos;
        public JumpInhibitorBlock Inhibitor;
        public List<JumpInhibitorInfoPackage> Info = new List<JumpInhibitorInfoPackage>();
        

        public JumpInhibitorPackage(int GPSHash, ulong SenderID)
        {
            this.GPSHash = GPSHash;
            this.SenderID = SenderID;
        }

        public JumpInhibitorPackage(JumpInhibitorBlock Inhibitor)
        {
            this.Inhibitor = Inhibitor;
        }

        public JumpInhibitorPackage()
        {
            //Parameterless constructor for serialization.
        }
    }

    public class JumpInhibitorInfoPackage
    {
        public JumpInhibitorTerminalSettings TerminalSettings;
        public Vector3D InhibitorPos;
        public float Radius;
        public long EntityID;

        public JumpInhibitorInfoPackage(JumpInhibitorTerminalSettings TerminalSettings, Vector3D InhibitorPos, float Radius)
        {
            this.TerminalSettings = TerminalSettings;
            this.InhibitorPos = InhibitorPos;
            this.Radius = Radius;
        }

        public JumpInhibitorInfoPackage(JumpInhibitorTerminalSettings TerminalSettings, long EntityID)
        {
            this.TerminalSettings = TerminalSettings;
            this.EntityID = EntityID;
        }

        public JumpInhibitorInfoPackage()
        {
            //Parameterless constructor for serialization.
        }
    }
}
