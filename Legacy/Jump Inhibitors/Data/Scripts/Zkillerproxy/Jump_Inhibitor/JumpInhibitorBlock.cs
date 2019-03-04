using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using VRage.ModAPI;

namespace Zkillerproxy.JumpInhibitorMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), true, "ZMKJumpInhibitor_Large")]
    public class JumpInhibitorBlock : MyGameLogicComponent
    {
        private bool RemoveConfigHandlerNextUpdate = false;
        public JumpInhibitorTerminalSettings TerminalSettings;
        public long EntityID;
        private Guid StorageGUID = new Guid("525a38e0-fe23-4bc2-b0c4-e260b48ff5b4");
        public JumpInhibitorConfig Config;
        private static readonly ushort InhibitorReport = 60101;
        private static readonly ushort ConfigRequest = 60104;
        private static readonly ushort ConfigSend = 60105;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            IMyBeacon Inhibitor = Entity as IMyBeacon;
            
            if (Inhibitor != null)
            {
                LoadTerminalSettings(Inhibitor);
                EntityID = Entity.EntityId;
                
                Inhibitor.CustomNameChanged += NameChangedHandeler;
                IMyTerminalBlock TerminalInhibitor = Inhibitor as IMyTerminalBlock;

                (TerminalInhibitor.GetProperty("Radius") as IMyTerminalControlSlider).Title = MyStringId.GetOrCompute("Inhibition Field Range");
                (TerminalInhibitor.GetActionWithName("IncreaseRadius") as IMyTerminalAction).Name = new StringBuilder("Increase Inhibition Field Range");
                (TerminalInhibitor.GetActionWithName("DecreaseRadius") as IMyTerminalAction).Name = new StringBuilder("Decrease Inhibition Field Range");

                MyAPIGateway.Multiplayer.SendMessageToServer(InhibitorReport, MyAPIGateway.Utilities.SerializeToBinary(EntityID));
                MyAPIGateway.Utilities.InvokeOnGameThread(RegisterConfigHandler);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (RemoveConfigHandlerNextUpdate)
            {
                UnregisterConfigHandler();
                RemoveConfigHandlerNextUpdate = false;
            }
        }

        private void RegisterConfigHandler()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(ConfigSend, ConfigSendHandler);
            MyAPIGateway.Multiplayer.SendMessageToServer(ConfigRequest, MyAPIGateway.Utilities.SerializeToBinary(MyAPIGateway.Multiplayer.MyId));
            // Entity.Components.Get<MyResourceSinkComponent>().SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 100000);
            // Log("MaxPow = " + Entity.Components.Get<MyResourceSinkComponent>().MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId).ToString("0"));
            Entity.Components.Get<MyResourceSinkComponent>().SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, ComputePower);
            Entity.Components.Get<MyResourceSinkComponent>().Update();
        }

        private void UnregisterConfigHandler()
        {
            if (Entity != null)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(ConfigSend, ConfigSendHandler);
            }
        }

        private void ConfigSendHandler(byte[] obj)
        {
            if (Config == null)
            {
                JumpInhibitorConfig config = null;

                try
                {
                    config = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorConfig>(MyAPIGateway.Utilities.SerializeFromBinary<string>(obj));
                }
                catch
                {
                    Log("ERROR: Failed to serialize incoming byte[] for config reciever!");
                }

                if (config != null)
                {
                    Config = config;
                }
                else
                {
                    Config = new JumpInhibitorConfig(0.02f, false, true, true);
                }

                RemoveConfigHandlerNextUpdate = true;
            }
        }

        private void LoadTerminalSettings(IMyEntity Inhibitor)
        {
            if (Inhibitor.Storage == null)
            {
                TerminalSettings = new JumpInhibitorTerminalSettings(false, false);
            }
            else if (Inhibitor.Storage.ContainsKey(StorageGUID) == false)
            {
                TerminalSettings = new JumpInhibitorTerminalSettings(false, false);
            }
            else
            {
                bool ErrorOccurred = false;
                JumpInhibitorTerminalSettings SaveData = null;

                try
                {
                    SaveData = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorTerminalSettings>(Inhibitor.Storage[StorageGUID]);
                }
                catch (Exception)
                {
                    ErrorOccurred = true;
                }

                if (ErrorOccurred == false)
                {
                    TerminalSettings = SaveData;
                }
                else
                {
                    TerminalSettings = new JumpInhibitorTerminalSettings(false, false);
                }
            }
        }

        public void SaveTerminalSettings(IMyEntity Inhibitor)
        {
            if (Inhibitor == null)
            {
                return;
            }

            string SaveData = MyAPIGateway.Utilities.SerializeToXML(TerminalSettings);

            if (Inhibitor.Storage == null)
            {
                Inhibitor.Storage = new MyModStorageComponent();
                Inhibitor.Storage[StorageGUID] = SaveData;
            }
            else
            {
                Inhibitor.Storage[StorageGUID] = SaveData;
            }
        }

        public float ComputePower()
        {
            if (Config != null)
            {
                // Log(((Entity as IMyBeacon).Radius * Config.PowerJPerM).ToString("0.00"));
                float r = !TerminalSettings.EnableJumpOut ? (Entity as IMyBeacon).Radius * Config.PowerJPerM : 5;
                r = !TerminalSettings.EnableJumpWithin ? (Entity as IMyBeacon).Radius * Config.PowerJPerM + r: r + 5;
                return r;
            }
            else
            {
                Log("Failed to set Inhibitor power draw from config, using default until a config is found.");
                return 0.02f;
            }
        }

        private void NameChangedHandeler(IMyTerminalBlock obj)
        {
            if (obj.CustomName.ToLower().Contains("jump inhibitor") == false)
            {
                obj.CustomName = obj.CustomName.Insert(0, "Jump Inhibitor ");
            }
        }

        private void Log(string Input)
        {
            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (Block): " + Input);
        }
    }

    public class JumpInhibitorTerminalSettings
    {
        public bool EnableJumpWithin;
        public bool EnableJumpOut;

        public JumpInhibitorTerminalSettings(bool enableJumpWithin, bool enableJumpOut)
        {
            EnableJumpWithin = enableJumpWithin;
            EnableJumpOut = enableJumpOut;
        }

        public JumpInhibitorTerminalSettings()
        {
            //Parameterless constructor for serialization.
        }
    }
}
