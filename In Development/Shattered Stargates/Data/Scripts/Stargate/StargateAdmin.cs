using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using Draygo.API;

// This handles server administrative functions
// Some of this code was take from Midspace's Admin helper
namespace Phoenix.Stargate
{
    public enum OnOffTriState
    {
        On,
        Off,
        Unset
    }

    public enum BlockCategoryMenuToggle
    {
        Gates,
        Supergate,
        DHDs,
        Production,
        Power,
        PuddleJumper,
        Misc,
        HandItems,
        //Ore,
        //Ships
    }

    /// <summary>
    /// This is the mod configuration data stored on disk
    /// </summary>
    public class StargateConfig
    {
        public OnOffTriState AntennaMode = OnOffTriState.Unset;
        public bool AntennaForced = false;
        public bool Destructible = true;
        public bool Debug = false;
        public bool VortexVisible = true;
        public bool VortexDamage = true;
        public double GateInfluenceRadius = Constants.GateInfluenceRadiusOldWorld;
        public bool FillInDHDText = true;
        public bool TeleportGrids = true;

        [XmlIgnore]
        private HashSet<BlockCategoryMenuToggle> m_hiddenGroups;

        /// <summary>
        /// Setter is for serialization only, don't manually set
        /// </summary>
        public HashSet<BlockCategoryMenuToggle> HiddenGroups
        {
            get { return m_hiddenGroups ?? (m_hiddenGroups = new HashSet<BlockCategoryMenuToggle>()); }

            set
            {
                Logger.Instance.LogMessage("Loading HiddenGroups");
                m_hiddenGroups = value;
                ApplyHiddenGroups();
            }
        }

        [XmlIgnore]
        private bool m_bHardcore = false;
        public bool Hardcore
        {
            get { return m_bHardcore; }
            set
            {
                m_bHardcore = value;
                if (m_bHardcore)
                    HardcoreMode.Instance.SwitchToHardcore();
                else
                    HardcoreMode.Instance.SwitchToStandard();
            }
        }

        [XmlIgnore]
        private bool m_bBuildable = true;
        public bool Buildable
        {
            get { return m_bBuildable; }
            set
            {
                m_bBuildable = value;
                StargateAdmin.ToggleGMenu();
            }
        }

        public StargateConfig()
        {
            // For the Portal mod, default is hardcore
            if (Globals.ModName == "Portal")
                m_bHardcore = true;
        }

        public void ApplyHiddenGroups()
        {
            Logger.Instance.LogMessage(string.Format("Loading {0} hidden groups", m_hiddenGroups?.Count ?? 0));
            if (m_hiddenGroups != null)
            {
                // Show everything
                foreach (BlockCategoryMenuToggle item in Enum.GetValues(typeof(BlockCategoryMenuToggle)))
                {
                    StargateAdmin.ToggleGMenu(item, true, false);
                }

                // Hide selected values
                foreach (var item in m_hiddenGroups)
                {
                    StargateAdmin.ToggleGMenu(item, false, false);
                }
            }
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Globals.Priority)]
    class StargateAdmin : MySessionComponentBase
    {
        #region Static data
        static bool _isInitialized = false;
        public static StargateAdmin Instance { get; private set; }
        private static StargateConfig m_configuration;
        public static StargateConfig Configuration
        {
            get { return m_configuration; }
            private set
            {
                m_configuration = value;

                // Call the update methods, since they won't be triggered automatically by serialization
                m_configuration.ApplyHiddenGroups();
            }
        }
        #endregion

        HUDTextNI m_helpHud;
        public HUDTextNI HelpHUD { get { return m_helpHud; } }
        private const string ConfigFileName = "Config_{0}.cfg";

        static StargateAdmin()
        {
            Configuration = new StargateConfig();
        }

        public StargateAdmin()
        {
            Instance = this;
        }

        public override string ToString()
        {
            return this.GetType().FullName;
        }

        public static void SetConfig(StargateConfig config)
        {
            // The settings need to be merged into the existing client options
            var clientconfig = Configuration;   // current client settings
            Configuration = config;             // Replace client settings 

            // Copy over settings we really need to preserve (client overridable options)
            Configuration.VortexVisible = clientconfig.VortexVisible;

            Logger.Instance.LogDebug("Loading new settings from server");
        }

        private void Init()
        {
            _isInitialized = true;
            LoadConfig();
            Logger.Instance.LogDebug("StargateAdmin.Init()");
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(MessageUtils.MessageId, MessageUtils.HandleMessage);
            m_helpHud = new HUDTextNI((long)Constants.PortalWorkshopID);           // Init Draygo's hud text

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);

            StargateAdmin.CheckAndDisableMod(this);

            if (MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated)
            {
                AddConcealmentExemption(_gateSubtypes);
                AddConcealmentExemption("Supergate");
            }
        }

        public override void LoadData()
        {
            Logger.Instance.Init(Globals.ModName);
            Logger.Instance.LogDebug("StargateAdmin.LoadData()");
        }

        public override void UpdateBeforeSimulation()
        {
            if (!_isInitialized && MyAPIGateway.Session != null)
                Init();
        }

        protected override void UnloadData()
        {
            try
            {
                m_helpHud?.Close();
                m_helpHud = null;

                MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
                MyAPIGateway.Multiplayer?.UnregisterMessageHandler(MessageUtils.MessageId, MessageUtils.HandleMessage);
                HardcoreMode.Clean();
            }
            catch (Exception ex) { Logger.Instance.LogException(ex); }

        }

        public override void SaveData()
        {
            SaveConfig();
        }

        private static void DamageHandler(object target, ref MyDamageInformation info)
        {
            if (Configuration.Destructible)
                return;

            if (target == null)
                return;

            if (target is IMySlimBlock)
            {
                if ((target as IMySlimBlock).FatBlock != null && (target as IMySlimBlock).FatBlock is IMyTerminalBlock)
                {
                    var block = (target as IMySlimBlock).FatBlock as IMyTerminalBlock;
                    if (block.GameLogic.GetAs<Stargate>() is Stargate && block.GameLogic.GetAs<Stargate>().StargateType != GateType.Invalid)
                    {
                        Logger.Instance.LogDebug("Damage taken: " + info.Type);
                        //info.Type == Sandbox.Common.ObjectBuilders.Definitions.MyDamageType.
                        info.Amount = 0;
                        info.IsDeformation = false;
                        info.Type = MyDamageType.Unknown;
                    }
                }
                // Handle event horizon blocks
                if ((target as IMySlimBlock).FatBlock != null && (target as IMySlimBlock).FatBlock.BlockDefinition.SubtypeId.StartsWith("Phoenix_Stargate_EventHorizon"))
                {
                    info.Amount = 0;
                    info.IsDeformation = false;
                }
            }

        }

        public static bool CheckAndDisableMod(MySessionComponentBase component = null)
        {
            foreach (var mod in MyAPIGateway.Session.Mods)
            {
                if ((mod.PublishedFileId == Constants.StargateWorkshopID || mod.Name == "Stargate") && Globals.ModName == "Portal")
                {
                    Logger.Instance.LogMessage("StargateMissionComponent: Another mod detected, disabling " + Globals.ModName);
                    Globals.ModEnabled = false;

                    if( component != null )
                        MyAPIGateway.Utilities.InvokeOnGameThread(() => component.SetUpdateOrder(MyUpdateOrder.NoUpdate));

                    MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
                    MyAPIGateway.Multiplayer?.UnregisterMessageHandler(MessageUtils.MessageId, MessageUtils.HandleMessage);
                    return true;
                }
            }
            return false;
        }

        static void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (ProcessClientMessage(messageText))
                sendToOthers = false;
        }

        public static bool ProcessClientMessage(string messageText)
        {
            if (!_isInitialized || string.IsNullOrEmpty(messageText))
                return false;

            string invalidOptionErrorText = "Invalid argument supplied. Type /" + Globals.ModName.ToLowerInvariant() + " help to show valid commands and options";

            Logger.Instance.LogDebug("Processing message");

            var commands = messageText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length == 0)
                return false;

            var match = Regex.Match(messageText, @"(/" + Globals.ModName.ToLowerInvariant() + @")\s+(?<Key>[^\s]+)((\s+(?<Value>.+))|)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var key = match.Groups["Key"].Value;
                var value = match.Groups["Value"].Value.Trim();
                bool bval = false;
                double dval = 0.0f;
                OnOffTriState onoffval = OnOffTriState.Unset;
                bool force = false;

                // Allowed client options
                switch (key.ToLowerInvariant())
                {
                    case "vortex":
                    case "save":
                        break;
                    default:
                        if (!MyAPIGateway.Session.Player.IsAdmin)
                        {
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "You must be a server admin to use this.");
                            return true;
                        }
                        break;
                }

                switch (key.ToLowerInvariant())
                {
                    case "antenna":
                    case "broadcast":
                        // Check for 'force' keyword'
                        if (value.StartsWith("force", StringComparison.InvariantCultureIgnoreCase))
                        {
                            force = true;
                            value = value.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries)[1];
                        }
                        OnOffTriState mode;
                        if (Enum.TryParse<OnOffTriState>(value, true, out mode))
                            MessageUtils.SendMessageToServer(new MessageAntenna() { AntennaMode = mode, Force = force });
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "destructible":
                    case "destructable":
                    case "damage":
                        bool damage = true;
                        OnOffTriState dmg = OnOffTriState.Unset;
                        if (bool.TryParse(value, out damage) || OnOffTriState.TryParse(value, true, out dmg))
                        {
                            if (dmg != OnOffTriState.Unset)
                                damage = dmg == OnOffTriState.On ? true : false;

                            MessageUtils.SendMessageToServer(new MessageIndestructible() { Destructible = damage });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "hide":
                        BlockCategoryMenuToggle categoryToHide;
                        if (Enum.TryParse<BlockCategoryMenuToggle>(value, true, out categoryToHide))
                            MessageUtils.SendMessageToServer(new MessageToggleItems() { Buildable = false, Group = categoryToHide });
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "show":
                        BlockCategoryMenuToggle categoryToShow;
                        if (Enum.TryParse<BlockCategoryMenuToggle>(value, true, out categoryToShow))
                            MessageUtils.SendMessageToServer(new MessageToggleItems() { Buildable = true, Group = categoryToShow });
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;

                    case "buildable":
                    case "build":
                        bool build = true;
                        OnOffTriState bld = OnOffTriState.Unset;
                        if (bool.TryParse(value, out build) || OnOffTriState.TryParse(value, true, out bld))
                        {
                            if (bld != OnOffTriState.Unset)
                                build = bld == OnOffTriState.On ? true : false;

                            MessageUtils.SendMessageToServer(new MessageBuildable() { Buildable = build });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "teleportgrids":
                    case "teleportships":
                        bval = true;
                        onoffval = OnOffTriState.Unset;
                        if (bool.TryParse(value, out bval) || OnOffTriState.TryParse(value, true, out onoffval))
                        {
                            if (onoffval != OnOffTriState.Unset)
                                bval = onoffval == OnOffTriState.On ? true : false;

                            MessageUtils.SendMessageToServer(new MessageTeleportGridsAllowed() { AllowGridTeleport = bval });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "hardcore":
                    case "hard":
                        bval = true;
                        onoffval = OnOffTriState.Unset;
                        if (bool.TryParse(value, out bval) || OnOffTriState.TryParse(value, true, out onoffval))
                        {
                            if (onoffval != OnOffTriState.Unset)
                                bval = onoffval == OnOffTriState.On ? true : false;

                            MessageUtils.SendMessageToAll(new MessageHardcore() { Hardcore = bval });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "vortex":
                        // Check for 'force' keyword'
                        if (value.StartsWith("global", StringComparison.InvariantCultureIgnoreCase) ||
                            value.StartsWith("server", StringComparison.InvariantCultureIgnoreCase) ||
                            value.StartsWith("force", StringComparison.InvariantCultureIgnoreCase))
                        {
                            force = true;
                            value = value.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries)[1];

                            if (!MyAPIGateway.Session.Player.IsAdmin)
                            {
                                MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "You must be a server admin to use this.");
                                return true;
                            }
                        }

                        bval = true;
                        onoffval = OnOffTriState.Unset;
                        if (bool.TryParse(value, out bval) || OnOffTriState.TryParse(value, true, out onoffval))
                        {
                            if (onoffval != OnOffTriState.Unset)
                                bval = onoffval == OnOffTriState.On ? true : false;

                            if (force)
                            {
                                MessageUtils.SendMessageToServer(new MessageVortex() { Flag = bval, ResyncSettings = true });
                            }
                            else
                            {
                                StargateAdmin.Configuration.VortexVisible = bval;
                                MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Client vortex " + StargateAdmin.Configuration.VortexVisible.ToString());
                            }
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "vortexdamage":
                    case "vortexdmg":
                        bval = true;
                        onoffval = OnOffTriState.Unset;
                        if (bool.TryParse(value, out bval) || OnOffTriState.TryParse(value, true, out onoffval))
                        {
                            if (onoffval != OnOffTriState.Unset)
                                bval = onoffval == OnOffTriState.On ? true : false;

                            MessageUtils.SendMessageToServer(new MessageVortex() { Type = MessageVortex.MessageType.Damage, Flag = bval });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "gateinfluence":
                    case "gateinfluencerange":
                    case "gaterange":
                        dval = 0.0;
                        if (double.TryParse(value, out dval))
                        {
                            MessageUtils.SendMessageToServer(new MessageGateInfluence() { Value = dval });
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "debug":
                        bool debug = true;
                        OnOffTriState dbg = OnOffTriState.Unset;
                        if (bool.TryParse(value, out debug) || OnOffTriState.TryParse(value, true, out dbg))
                        {
                            if (dbg != OnOffTriState.Unset)
                                debug = dbg == OnOffTriState.On ? true : false;

                            var message = new MessageDebug() { DebugMode = debug };
                            message.InvokeProcessing();
                            MessageUtils.SendMessageToServer(message);
                        }
                        else
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, invalidOptionErrorText);
                        break;
                    case "save":
                        // Check for 'force' keyword'
                        if (value.StartsWith("global", StringComparison.InvariantCultureIgnoreCase) ||
                            value.StartsWith("server", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (MyAPIGateway.Session.Player.IsAdmin)
                            {
                                MessageUtils.SendMessageToServer(new MessageSave());
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "You must be a server admin to use this.");
                                return true;
                            }
                        }
                        else
                        {
                            StargateAdmin.SaveConfig();
                            var message = "Client options saved";

                            if (MyAPIGateway.Session.Player.IsAdmin)
                                message += ". '/" + Globals.ModName + " save server' to save server settings";

                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, message);
                        }
                        break;
                    case "upgrade":
                        // Check for 'force' keyword'
                        if (MyAPIGateway.Session.Player.IsAdmin)
                        {
                            MessageUtils.SendMessageToServer(new MessageUpgrade());
                        }
                        else
                        {
                            MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "You must be a server admin to use this.");
                            return true;
                        }
                        break;
                    case "help":
                        MyAPIGateway.Utilities.ShowMissionScreen(Globals.ModName + " Admin Help",
                            "/" + Globals.ModName.ToLowerInvariant() + " <command> [value]", "",
                            "Current commands:\r\n" +
                            "build[able] <bool> - Allow/disallow buildable gates\r\n" +
                            "damage <bool> - Allow/disallow damage to gates (includes grinding)\r\n" +
                            "antenna [force] <on|off|default> - Enable/disable antenna broadcasting, optional forcing (overrides DHD)\r\n" +
                            "vortex <bool> - Enable/disable unstable vortex visibility (also affects damage).\r\n" +
                            "vortexdamage <bool> - Enable/disable unstable vortex damage (remains visible)\r\n" +
                            "gateinfluence <radius in meters> - Radius of address range. Only one gate inside this area is reachable.\r\n" +
                            "teleportgrids <bool> - Enable/disable teleporting of grids through small gates.\r\n" +
                            "hide <category> - Hide blocks/items from G-Menu and assembler.\r\n" + 
                            "show <category> - Show blocks/items previously hidden with 'hide'.\r\n" +
                            "     <category> - One of: " + string.Join(", ", Enum.GetNames(typeof(BlockCategoryMenuToggle))) + "\r\n" +
                            "upgrade - Upgrade legacy blocks (one-time)\r\n" +
                            "debug <bool> - Enable/disable debug logging\r\n" +
                            "save [server] - Save settings (default client)\r\n" +
                            "\r\n" +
                            "These commands are server-wide and can only be run by server administrators.\r\n" +
                            "Items with [ ] are optional, < > are required.\r\n" +
                            "All <bool> arguments accept: <on|off|true|false>\r\n" +
                            "<on|off> means pick 'on' or 'off'.\r\n" +
                            ""
                            );
                        break;
                    default:
                        MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Invalid command, type /" + Globals.ModName.ToLowerInvariant() + " help to show valid commands");
                        break;
                }
                return true;
            }

            return false;
        }

        void LoadConfig()
        {
            try
            {
                var worldname = MyAPIGateway.Session.Name;
                worldname = Regex.Replace(worldname, "[<>:\"/\\|?*]", "");  // Remove invalid filename chars
                var oldFilename = string.Format(ConfigFileName, worldname);
                var filename = string.Format("{0}.cfg", Globals.ModName);

                System.IO.TextReader reader;

                // Read file in new legacy location first
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, typeof(StargateAdmin)))
                {
                    Logger.Instance.LogMessage(string.Format("Loading saved mod configuration from World: {0}", filename));
                    reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, typeof(StargateAdmin));
                }
                else
                {
                    Logger.Instance.LogMessage(string.Format("Loading saved mod configuration from Local: {0}", oldFilename));
                    reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(oldFilename, typeof(StargateAdmin));
                }


                var xmlData = reader.ReadToEnd();
                Configuration = MyAPIGateway.Utilities.SerializeFromXML<StargateConfig>(xmlData);
                reader.Close();

                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(oldFilename, typeof(StargateAdmin)))
                    MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldFilename, typeof(StargateAdmin));

                Logger.Instance.Debug = Configuration.Debug;
            }
            catch
            {
                // ignore errors
                Configuration.GateInfluenceRadius = Constants.GateInfluenceRadiusNewWorld;
            }
        }

        static public void SaveConfig()
        {
            string debugFilename = string.Empty;

            try
            {
                var worldname = MyAPIGateway.Session.Name;
                worldname = Regex.Replace(worldname, "[<>:\"/\\|?*]", "");  // Remove invalid filename chars

                var xmlData = MyAPIGateway.Utilities.SerializeToXML<StargateConfig>(Configuration);
                var oldFilename = string.Format(ConfigFileName, worldname);
                var filename = string.Format("{0}.cfg", Globals.ModName);
                debugFilename = MyAPIGateway.Utilities.GamePaths.SavesPath + "Storage\\*.sbm_Stargate\\" + filename;

                // Use the old name if this is a DS client (so the load menu isn't polluted)
                System.IO.TextWriter writer;
                if (MyAPIGateway.Session.IsServer)
                {
                    // Cleanup old config files (if they exist)
                    if (MyAPIGateway.Utilities.FileExistsInLocalStorage(oldFilename, typeof(StargateAdmin)))
                        MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldFilename, typeof(StargateAdmin));

                    writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(filename, typeof(StargateAdmin));
                }
                else
                {
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(oldFilename, typeof(StargateAdmin));
                }
                writer.Write(xmlData);
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(debugFilename);
                Logger.Instance.LogException(ex);
            }
        }

        private static string[] _gateSubtypes = new string[]
                    {
                        "Stargate S",
                        "Stargate M",
                        "Stargate O",
                        "Stargate A",
                        "Stargate U",
                        "Stargate O Small",
                        "Stargate A Small",
                        "Stargate U Small",
                    };

        public static void ToggleGMenu(BlockCategoryMenuToggle toggle = BlockCategoryMenuToggle.Gates, bool? makePublic = null, bool updateGroups = true)
        {
            var subtypes = new Dictionary<MyObjectBuilderType, string[]>();
            var visible = Configuration.Buildable;

            if (makePublic != null)
                visible = makePublic.Value;

            if (updateGroups)
            {
                if (makePublic == true)
                    Configuration.HiddenGroups.Remove(toggle);
                else if (makePublic == false)
                    Configuration.HiddenGroups.Add(toggle);
            }

            switch (toggle)
            {
                case BlockCategoryMenuToggle.Gates:
                    subtypes[typeof(MyObjectBuilder_TerminalBlock)] = _gateSubtypes;
                    break;
                case BlockCategoryMenuToggle.Supergate:
                    subtypes[typeof(MyObjectBuilder_TerminalBlock)] = new string[] { "Supergate" };
                    break;
                case BlockCategoryMenuToggle.DHDs:
                    subtypes[typeof(MyObjectBuilder_ButtonPanel)] = new string[]
                    {
                        "Phoenix_Stargate_DHD_Generic",
                        "Phoenix_Stargate_DHD_Generic_Small",
                        "Phoenix_Stargate_DHD_SG1",
                        "Phoenix_Stargate_DHD_SGA",
                        "Phoenix_Stargate_DHD_SGU_Computer",
                        "Phoenix_Stargate_DHD_SGA_Computer",
                        "Phoenix_Stargate_DHD_SG1_Computer",
                        "Phoenix_Stargate_DHD_SG1_Computer_Small",
                        "Phoenix_Stargate_DHD_SG1_Small",
                        "Phoenix_Stargate_DHD_SGA_Small",
                    };
                    break;
                case BlockCategoryMenuToggle.PuddleJumper:
                    subtypes[typeof(MyObjectBuilder_Cockpit)] = new string[] { "Puddle Jumper" };
                    subtypes[typeof(MyObjectBuilder_PistonBase)] = new string[] { "Puddle Piston Base" };
                    subtypes[typeof(MyObjectBuilder_PistonTop)] = new string[] { "Puddle Piston Top" };
                    subtypes[typeof(MyObjectBuilder_SmallMissileLauncher)] = new string[] { "Wing R", "Wing L" };
                    subtypes[typeof(MyObjectBuilder_SmallGatlingGun)] = new string[] { "Wing R Gatling", "Wing L Gatling" };
                    subtypes[typeof(MyObjectBuilder_Thrust)] = new string[] { "Puddle Jumper Thrust" };
                    subtypes[typeof(MyObjectBuilder_CubeBlock)] = new string[] { "Puddle Jumper Info" };
                    break;
                case BlockCategoryMenuToggle.Power:
                    subtypes[typeof(MyObjectBuilder_Reactor)] = new string[] { "Naquadah Generator large", "Naquadah Generator small" };
                    subtypes[typeof(MyObjectBuilder_BatteryBlock)] = new string[] { "ZPMHub" };
                    break;
                case BlockCategoryMenuToggle.Production:
                    subtypes[typeof(MyObjectBuilder_Assembler)] = new string[] { "Naquadah Production Facility" };
                    break;
                case BlockCategoryMenuToggle.Misc:
                    subtypes[typeof(MyObjectBuilder_Cockpit)] = new string[] { "Chair" };
                    subtypes[typeof(MyObjectBuilder_ButtonPanel)] = new string[] { "Atlantis Button Panel" };
                    subtypes[typeof(MyObjectBuilder_CryoChamber)] = new string[] { "Sarcophagus Cryo" };
                    subtypes[typeof(MyObjectBuilder_MedicalRoom)] = new string[] { "Sarcophagus Medical" };
                    subtypes[typeof(MyObjectBuilder_Door)] = new string[] { "Atlantis Door Offset", "Atlantis Door" };
                    subtypes[typeof(MyObjectBuilder_CameraBlock)] = new string[] { "Malp" };
                    subtypes[typeof(MyObjectBuilder_Reactor)] = new string[] { "SGC" };
                    break;
                case BlockCategoryMenuToggle.HandItems:
                    subtypes[typeof(MyObjectBuilder_PhysicalGunObject)] = new string[] { "Zat", "Staff", "P90" };
                    subtypes[typeof(MyObjectBuilder_BlueprintDefinition)] = new string[] { "Zat", "Staff", "P90" };
                    break;
                //case BlockCategoryMenuToggle.Ore:
                //    subtypes[typeof(MyObjectBuilder_BlueprintDefinition)] = new string[] { "NaquadahOreToIngot", "TriniumOreToIngot", "NeutroniumOreToIngot" };
                //    break;
                //case BlockCategoryMenuToggle.Ships:
                //    subtypes[typeof(MyObjectBuilder_SpawnGroupDefinition)] = new string[] { "Al'kesh", "TelTacMk3", "Ancient Destiny Small", "AsgardShip", "DanielJackson" };
                //    break;
            }

            // Hide blocks from g-menu
            foreach (var types in subtypes)
            {
                foreach (var subtype in types.Value)
                {
                    var id = new MyDefinitionId(types.Key, subtype);
                    MyCubeBlockDefinition def;
                    MyBlueprintDefinitionBase blueprint = null;
                    MySpawnGroupDefinition spawngroup = null;
                    MyDefinitionBase generic = null;

                    if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out def))
                    {
                        if (def != null && def.Public != visible)
                        {
                            def.Public = visible;
                            Logger.Instance.LogMessage("Setting " + def.BlockPairName + " to " + visible);
                        }
                    }
                    else
                    {
                        // Check for and hide blueprints, if applicable (hand weapons and such)
                        blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(id);

                        if (blueprint != null && blueprint.Public != visible)
                        {
                            Logger.Instance.LogMessage("Setting blueprint " + blueprint.DisplayNameText + " to " + visible);
                            blueprint.Public = visible;
                            blueprint.Enabled = visible;
                        }
                        if (MyDefinitionManager.Static.TryGetDefinition(id, out spawngroup) && spawngroup.Public != visible)
                        {
                            Logger.Instance.LogMessage("Setting spawngroup " + spawngroup.Id.SubtypeId + " to " + visible);
                            spawngroup.Public = visible;
                            spawngroup.Enabled = visible;
                        }
                        if (MyDefinitionManager.Static.TryGetDefinition(id, out generic) && generic.Public != visible)
                        {
                            Logger.Instance.LogMessage("Setting definition " + generic.Id.ToString() + " to " + visible);
                            generic.Public = visible;
                            generic.Enabled = visible;
                        }
                    }
                }
            }
        }

        public void AddConcealmentExemption(params string[] subtypes)
        {
            Logger.Instance.LogMessage("Adding Stargate blocks to Essentials concealment (if present)");
            foreach (var subtype in subtypes)
            {
                byte[] data = Encoding.UTF8.GetBytes(subtype);
                MyAPIGateway.Multiplayer.SendMessageToServer(9007, data);
            }
        }
    }

    public static class AdminExtensions
    {
        /// <summary>
        /// Creates the objectbuilders in game, and syncs it to the server and all clients.
        /// </summary>
        /// <param name="entities"></param>
        public static void CreateAndSyncEntities(this List<VRage.ObjectBuilders.MyObjectBuilder_EntityBase> entities)
        {
            MyAPIGateway.Entities.RemapObjectBuilderCollection(entities);
            entities.ForEach(item => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(item));
            MyAPIGateway.Multiplayer.SendEntitiesCreated(entities);
        }

    }
}
// vim: tabstop=4 expandtab shiftwidth=4 nobackup
