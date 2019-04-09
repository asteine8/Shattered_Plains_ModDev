namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;

    public class DisplayState
    {
        internal DisplayStateValues Value = new DisplayStateValues();
        internal readonly IMyFunctionalBlock Display;

        internal DisplayState(IMyFunctionalBlock display)
        {
            Display = display;
        }

        public void StorageInit()
        {
            if (Display.Storage == null)
            {
                Display.Storage = new MyModStorageComponent {[Session.Instance.DisplayStateGuid] = ""};
            }
        }

        public void SaveState(bool createStorage = false)
        {
            if (createStorage && Display.Storage == null) Display.Storage = new MyModStorageComponent();
            else if (Display.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Value);
            Display.Storage[Session.Instance.DisplayStateGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadState()
        {
            if (Display.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Display.Storage.TryGetValue(Session.Instance.DisplayStateGuid, out rawData))
            {
                DisplayStateValues loadedState = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedState = MyAPIGateway.Utilities.SerializeFromBinary<DisplayStateValues>(base64);

                if (loadedState != null)
                {
                    Value = loadedState;
                    loadedSomething = true;
                }

                if (Session.Enforced.Debug == 3)
                    Log.Line($"Loaded - DisplayId [{Display.EntityId}]:\n{Value.ToString()}");
            }

            return loadedSomething;
        }

        #region Network
        public void NetworkUpdate()
        {

            if (Session.Instance.IsServer)
            {
                Value.MId++;
                Session.Instance.PacketizeToClientsInRange(Display, new DataDisplayState(Display.EntityId, Value)); // update clients with server's settings
            }
        }
        #endregion
    }

    public class DisplaySettings
    {
        internal DisplaySettingsValues Settings = new DisplaySettingsValues();
        internal readonly IMyFunctionalBlock Display;

        internal DisplaySettings(IMyFunctionalBlock display)
        {
            Display = display;
        }

        public void SaveSettings(bool createStorage = false)
        {
            if (createStorage && Display.Storage == null) Display.Storage = new MyModStorageComponent();
            else if (Display.Storage == null) return;

            var binary = MyAPIGateway.Utilities.SerializeToBinary(Settings);
            Display.Storage[Session.Instance.DisplaySettingsGuid] = Convert.ToBase64String(binary);
        }

        public bool LoadSettings()
        {
            if (Display.Storage == null) return false;

            string rawData;
            bool loadedSomething = false;

            if (Display.Storage.TryGetValue(Session.Instance.DisplaySettingsGuid, out rawData))
            {
                DisplaySettingsValues loadedSettings = null;
                var base64 = Convert.FromBase64String(rawData);
                loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<DisplaySettingsValues>(base64);

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    loadedSomething = true;
                }

                if (Session.Enforced.Debug == 3)
                    Log.Line($"Loaded - DisplayId [{Display.EntityId}]:\n{Settings.ToString()}");
            }

            return loadedSomething;
        }

        #region Network

        public void NetworkUpdate()
        {
            Settings.MId++;
            if (Session.Instance.IsServer)
            {
                Session.Instance.PacketizeToClientsInRange(Display, new DataDisplaySettings(Display.EntityId, Settings)); // update clients with server's settings
            }
            else // client, send settings to server
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataDisplaySettings(Display.EntityId, Settings));
                MyAPIGateway.Multiplayer.SendMessageToServer(Session.PACKET_ID, bytes);
            }
        }
        #endregion
    }
}
