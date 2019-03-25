using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using ProtoBuf;
using Sandbox.Definitions;
using VRage.Game.Definitions;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using System.Timers;
using VRageMath;
using VRage.Serialization;
using Sandbox.Game.Localization;
using VRage.Utils;

namespace Phoenix.Stargate
{
    using Extensions;
    using Sandbox.Game.Components;
    using Sandbox.Game.EntityComponents;
    using System.Xml.Serialization;
    using VRage;

    /// <summary>
    /// This keeps track of data required by the DoorDHDExtensions.
    /// If the DHD were using a MyGameLogicComponent class, these would be data members of that class.
    /// NOTE: This is a class so it will be stored in the dictionary by reference (so updating is trivial).
    /// </summary>
    public class DHDDataOld
    {
        // Runtime state
        public IMyFunctionalBlock outgoingGate;                 // Keep track of status of outgoing gate
        public bool bDoReset;                                   // True if DHD needs to reset

        // Options
        public bool bAutoClose = false;                         // Whether to always autoclose when an object enters the gate (normally only players)
        public int iAutoCloseTime = Globals.DefaultCloseTime;   // Time to wait before closing the gate
        public bool? bUseAntenna = false;
    }

    public class DHDData
    {
        public string Destination;
        public bool AutoCloseWithAll = false;
        public int AutoCloseTime = Globals.DefaultCloseTime;
        public bool AutoClear = false;
    }

    #region New DHD
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ButtonPanel), false,
        "Phoenix_Stargate_DHD_Generic",
        "Phoenix_Stargate_DHD_Generic_Small",
        "Phoenix_Stargate_DHD_SG1",
        "Phoenix_Stargate_DHD_SGA",
        "Phoenix_Stargate_DHD_SG1_Small",
        "Phoenix_Stargate_DHD_SGA_Small",
        "Phoenix_Stargate_DHD_SG1_Computer",
        "Phoenix_Stargate_DHD_SG1_Computer_Small",
        "Phoenix_Stargate_DHD_SGU_Computer",
        "Phoenix_Stargate_DHD_SGU_Computer_Small",
        "Phoenix_Stargate_DHD_SGA_Computer"
    )]
    public class DHD : SerializableBlock
    {
        static bool m_ControlsInited = false;
        IMyButtonPanel m_dhd;
        DHDData m_dhdData = new DHDData();
        public DHDData Data { get { return m_dhdData; } }
        List<char> m_currentAddress = new List<char>(6);
        private MyEntity3DSoundEmitter m_soundEmitter;
        public MyEntity3DSoundEmitter SoundEmitter { get { return m_soundEmitter; } }
        IMyTerminalBlock m_currentGate;
        bool m_bInit = false;
        Timer m_timeoutTimer = new Timer(Constants.DHDTimeoutMS);
        GateEdition m_dhdEdition = GateEdition.None;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return Container.Entity.GetObjectBuilder(copy);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_timeoutTimer.AutoReset = false;
            m_timeoutTimer.Elapsed += (s, e) => ResetDHD();
            base.Init(objectBuilder);
            m_dhd = Container.Entity as IMyButtonPanel;

            if (m_dhd.BlockDefinition.SubtypeId == "Phoenix_Stargate_DHD_SG1")
                m_dhdEdition = GateEdition.Second;
            else if (m_dhd.BlockDefinition.SubtypeId == "Phoenix_Stargate_DHD_SGU_Computer")
                m_dhdEdition = GateEdition.Second;
            else if (m_dhd.BlockDefinition.SubtypeId == "Phoenix_Stargate_DHD_SG1_Computer")
                m_dhdEdition = GateEdition.Second;
            else if (m_dhd.BlockDefinition.SubtypeId == "Phoenix_Stargate_DHD_SGA")
                m_dhdEdition = GateEdition.Third;
            else if (m_dhd.BlockDefinition.SubtypeId == "Phoenix_Stargate_DHD_SGA_Computer")
                m_dhdEdition = GateEdition.Third;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            m_soundEmitter = new MyEntity3DSoundEmitter(m_dhd as MyEntity);
            m_dhd.ButtonPressed += dhd_ButtonPressed;
            m_dhd.OnClose += dhd_OnClose;
            m_dhd.IsWorkingChanged += dhd_IsWorkingChanged;
            ResetDHD();
        }

        #region Networking
        // Server; return true if validated, false if not
        public bool ButtonPressed_Server(int button)
        {
            Logger.Instance.LogAssert(m_currentGate != null, "m_currentGate != null");
            Logger.Instance.LogDebug("ButtonPressed_Server()");
            if (m_currentGate == null || (m_currentGate != null && m_currentGate.GameLogic.GetAs<Stargate>()?.Data.IsRemote == true))
                return false;

            if (button == 0)
            {
                Logger.Instance.LogDebug("ButtonPressed_Server() button == 0");

                m_timeoutTimer.Stop();
                //MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity.ShowTerminal();
                if (m_currentAddress.Count == 0 && !string.IsNullOrWhiteSpace(m_dhdData?.Destination))
                {
                    StartQuickDial();
                    return false;
                }
                else if (m_currentAddress.Count != 6)
                {
                    ResetDHD();
                    return false;
                }
                else
                {
                    var source = DoorDHDExtensions.GetNearestGate(m_dhd);
                    if (m_currentGate != source)
                    {
                        m_currentGate = source;
                        m_currentGate.GameLogic.GetAs<Stargate>().StateChanged += Gate_StateChanged;
                    }
                    return m_currentGate.GameLogic.GetAs<Stargate>().DialGate(m_dhd, false, new StringBuilder().Append(m_currentAddress.ToArray()).ToString(), MyAPIGateway.Session.Player?.IdentityId ?? 0);
                }
            }
            else
            {
                Logger.Instance.LogDebug("ButtonPressed_Server() button != 0");

                if (m_currentGate.GameLogic.GetAs<Stargate>()?.Data.State == GateState.Active)
                    return false;

                m_timeoutTimer.Start();

                if (m_currentAddress.Count > 6)
                    ResetDHD();
                else
                {
                    var symbol = Constants.ButtonsToCharacters[button];
                    if (m_currentAddress.Contains(symbol))
                        return false;

                    m_currentAddress.Add(symbol);
                }
            }
            return true;
        }

        // Client
        public void ButtonPressed_Client(int button, bool sound = true)
        {
            Logger.Instance.LogDebug("Button: " + button);
            if (button == 0)
            {
                LightUpDialButton();
            }
            else
            {
                var color = Constants.DHD_Glyph_Light_Active_SG1;
                if (m_dhdEdition == GateEdition.Third)
                    color = Constants.DHD_Glyph_Light_Active_SGA;

                m_dhd.SetEmissiveParts("Glyph" + button.ToString("00"), color, 0.5f);
                if (sound)
                    PlaySound(m_dhdEdition == GateEdition.Third ? "DHD_Atlantis" : "DHDDial");
            }
        }

        // Syncs button press to server and all clients
        // Server validates and sends back to clients
        private void RaiseButtonPressedSync(int button)
        {
            MessageUtils.SendMessageToServer(new MessageButtonPress()
            {
                DHD = m_dhd.EntityId,
                ButtonIndex = button,
                DHDPosition = m_dhd.Position,
                Grid = m_dhd.CubeGrid.EntityId
            });
        }

        public void QuickDial_Client(string address = null)
        {
            if (address == null)
            {
                var gate = DoorDHDExtensions.GetNamedGate(m_dhd, m_dhdData.Destination, m_currentGate);
                address = gate?.GameLogic.GetAs<Stargate>()?.Address;
            }

            // Convert a manually entered quick address (nickname) to a proper address for lighting up glyphs
            var newaddress = StargateMissionComponent.Instance.KnownGates.Values.Where(g => string.Compare(g.Name, address) == 0).FirstOrDefault();

            if (!string.IsNullOrEmpty(newaddress.Name))
                address = newaddress.Address;

            // Light up glyphs
            var chars = address.ToCharArray();
            for (int x = 0; x < Constants.ButtonsToCharacters.Count(); x++)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    if (Constants.ButtonsToCharacters[x] == chars[i])
                        ButtonPressed_Client(x, false);
                }
            }
            LightUpDialButton();
        }

        public bool QuickDial_Server()
        {
            var address = m_dhdData?.Destination;
            if (!string.IsNullOrEmpty(m_dhdData?.Destination))
            {
                var source = DoorDHDExtensions.GetNearestGate(m_dhd);
                if (m_currentGate != source)
                {
                    m_currentGate = source;
                    m_currentGate.GameLogic.GetAs<Stargate>().StateChanged += Gate_StateChanged;
                }
                var gate = DoorDHDExtensions.GetNamedGate(m_dhd, m_dhdData.Destination, m_currentGate);
                if (gate == null)
                    return false;
                MyAPIGateway.Utilities.InvokeOnGameThread(() => m_currentGate.GameLogic.GetAs<Stargate>().DialGate(m_dhd, false, gate?.GameLogic.GetAs<Stargate>()?.Address, MyAPIGateway.Session.Player?.IdentityId ?? 0));
                return true;
            }
            return false;
        }

        private void RaiseQuickDialSync()
        {
            MessageUtils.SendMessageToServer(new MessageQuickDial()
            {
                DHD = m_dhd.EntityId,
                Address = m_dhdData.Destination,
                Grid = m_dhd.CubeGrid.EntityId,
                DHDPosition = m_dhd.Position
            });
        }

        #endregion Networking

        #region Events
        private void dhd_IsWorkingChanged(IMyCubeBlock obj)
        {
            if (m_dhd.IsFunctional)
                RebuildWithButtonText();
        }

        private void dhd_OnClose(IMyEntity obj)
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;

            if (m_dhd == null)
                return;

            if (m_currentGate?.GameLogic?.GetAs<Stargate>() != null)
            {
                m_currentGate.GameLogic.GetAs<Stargate>().StateChanged -= Gate_StateChanged;
                m_currentGate = null;
            }
            m_dhd.OnClose -= dhd_OnClose;
            m_dhd.IsWorkingChanged -= dhd_IsWorkingChanged;
            m_dhd.ButtonPressed -= dhd_ButtonPressed;
            m_soundEmitter.Cleanup();
            m_timeoutTimer.Close();
        }

        private void dhd_ButtonPressed(int button)
        {
            RaiseButtonPressedSync(button);
        }

        private void Gate_StateChanged(IMyTerminalBlock gate, GateState state)
        {
            Logger.Instance.LogDebug("Gate_StateChanged: " + state.ToString());
            switch (state)
            {
                case GateState.Idle:
                    ResetDHD();
                    break;
                case GateState.Incoming:
                case GateState.Active:
                    LightUpDialButton();
                    if (Data.AutoClear)
                    {
                        MessageUtils.SendMessageToAll(new MessageSetQuickDestination()
                        {
                            DHD = m_dhd.EntityId,
                            Address = string.Empty,
                            Grid = m_dhd.CubeGrid.EntityId,
                            DHDPosition = m_dhd.Position
                        });
                    }
                    break;
            }
        }
        #endregion Events

        #region Update
        public override void UpdateOnceBeforeFrame()
        {
            if (!m_hasBeenDeserialized)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!m_bInit)
            {
                // This is triggered during the save frame,
                // but immediately clear the serialized data on the next update
                // so the block name stays clean
                if (MyAPIGateway.Multiplayer == null)
                {
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    return;
                }

                if (MyAPIGateway.Multiplayer.IsServer)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

                m_bInit = true;

                if (!Globals.ModEnabled)
                    return;

                //var screenAreaRender = Entity.Render.GetAs<MyRenderComponentScreenAreas>();

                //if (screenAreaRender == null)
                //{
                //    screenAreaRender = new MyRenderComponentScreenAreas((MyEntity)Entity);
                //    //Entity.Render.Container.Add(screenAreaRender);
                //}
                //if (screenAreaRender != null)
                //{
                //    screenAreaRender.AddScreenArea(new uint[] { Entity.Render.GetRenderObjectID() }, "ScreenArea");
                //    screenAreaRender.ChangeTexture(0, @"D:\Program Files\Steam\SteamApps\common\SpaceEngineers\Content\Textures\Logo\se.dds");
                //}
                CreateTerminalControls();
                //SerializeData();

            }

            if (MyAPIGateway.Input.IsAnyCtrlKeyPressed())
            {
                // Don't deserialize if in the middle of a copy/blueprint operation
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }
            DeserializeData();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            try
            {
                Logger.Instance.Active = false;
                var gate = DoorDHDExtensions.GetNearestGate(m_dhd, showMessage: false);
                Logger.Instance.Active = true;

                if (gate != m_currentGate)
                {
                    if (m_currentGate != null && !m_currentGate.Closed && !m_currentGate.MarkedForClose)
                        m_currentGate.GameLogic.GetAs<Stargate>().StateChanged -= Gate_StateChanged;

                    if (gate != null)
                    {
                        gate.GameLogic.GetAs<Stargate>().StateChanged += Gate_StateChanged;
                        Gate_StateChanged(gate, gate.GameLogic.GetAs<Stargate>().Data.State);
                    }
                    m_currentGate = gate;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
        }
        #endregion Update

        #region Terminal Controls
        public static void CreateTerminalControls()
        {
            if (m_ControlsInited)
                return;

            Func<IMyTerminalBlock, bool> enabledCheck = delegate (IMyTerminalBlock b)
            {
                return b.IsFunctional && b.GameLogic.GetAs<DHD>() != null;
            };

            m_ControlsInited = true;

            MyAPIGateway.TerminalControls.CustomControlGetter -= TerminalControls_CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter -= TerminalControls_CustomActionGetter;

            MyAPIGateway.TerminalControls.CustomControlGetter += TerminalControls_CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += TerminalControls_CustomActionGetter;

            // Separator
            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyButtonPanel>(string.Empty);
            sep.Visible = (b) => b.BlockDefinition.SubtypeId.Contains("DHD");
            MyAPIGateway.TerminalControls.AddControl<IMyButtonPanel>(sep);

            // Create controls at once, so they can all be referenced below
            var quickDialText = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyButtonPanel>("Phoenix.Stargate.Destination");
            var quickDialButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyButtonPanel>("Phoenix.Stargate.QuickDialList");
            var quickAction = MyAPIGateway.TerminalControls.CreateAction<IMyButtonPanel>("Phoenix.Stargate.QuickDial");
            var quickList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyButtonPanel>("Phoenix.Stargate.Presets");
            var quickDialClear = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyButtonPanel>("Phoenix.Stargate.ClearQuickDial");

            // Quick dial text box
            if (quickDialText != null)
            {
                quickDialText.Enabled = enabledCheck;
                quickDialText.Visible = enabledCheck;
                quickDialText.Title = MyStringId.GetOrCompute("Quick Dial Destination");
                quickDialText.Tooltip = MyStringId.GetOrCompute("Destination Gate name or address to quick dial.\nVisible to all players.");
                quickDialText.Getter = (b) => new StringBuilder(enabledCheck(b) ? b?.GameLogic.GetAs<DHD>()?.m_dhdData?.Destination : String.Empty);
                quickDialText.Setter = (b, v) =>
                {
                    if (enabledCheck(b))
                        b.GameLogic.GetAs<DHD>().m_dhdData.Destination = v.ToString();

                    MessageUtils.SendMessageToAll(new MessageSetQuickDestination()
                    {
                        DHD = b.EntityId,
                        Address = v.ToString(),
                        Grid = b.CubeGrid.EntityId,
                        DHDPosition = b.Position
                    });
                };
                quickDialText.SupportsMultipleBlocks = true;
            }
            MyAPIGateway.TerminalControls.AddControl<IMyButtonPanel>(quickDialText);

            // Quick dial button
            if (quickDialButton != null)
            {
                quickDialButton.Enabled = enabledCheck;
                quickDialButton.Visible = enabledCheck;
                quickDialButton.Title = MyStringId.GetOrCompute("Quick Dial");
                quickDialButton.Tooltip = MyStringId.GetOrCompute("Dials destination in Quick Dial text field.");
                quickDialButton.Action = b =>
                {
                    b.GameLogic.GetAs<DHD>()?.StartQuickDial();
                    quickList.UpdateVisual();
                };
                quickDialButton.SupportsMultipleBlocks = true;
                quickDialButton.Tooltip = MyStringId.GetOrCompute("Dials quick preset.");
            }
            MyAPIGateway.TerminalControls.AddControl<IMyButtonPanel>(quickDialButton);

            // Quick dial action
            if (quickAction != null)
            {
                quickAction.Name = new StringBuilder(quickDialButton.Title.String);
                quickAction.Enabled = enabledCheck;
                quickAction.Action = quickDialButton.Action;
            }
            MyAPIGateway.TerminalControls.AddAction<IMyButtonPanel>(quickAction);

            // Clear quick dial check box
            if (quickDialClear != null)
            {
                quickDialClear.Enabled = enabledCheck;
                quickDialClear.Visible = enabledCheck;
                quickDialClear.Title = MyStringId.GetOrCompute("Clear after dialing");
                quickDialClear.Tooltip = MyStringId.GetOrCompute("Automatically clear quick dial after successful dialing.");
                quickDialClear.Getter = (b) => (Boolean)(enabledCheck(b) ? b?.GameLogic.GetAs<DHD>()?.m_dhdData?.AutoClear : false);
                quickDialClear.Setter = (b, v) =>
                {
                    if (enabledCheck(b))
                    {
                        b.GameLogic.GetAs<DHD>().m_dhdData.AutoClear = v;
                        MessageUtils.SendMessageToServer(new MessageAutoClear()
                        {
                            EntityId = b.EntityId,
                            Value = v
                        });
                        quickDialClear.UpdateVisual();
                    }
                };
                quickDialClear.SupportsMultipleBlocks = true;
            }
            MyAPIGateway.TerminalControls.AddControl<IMyButtonPanel>(quickDialClear);

            if (quickList != null)
            {
                quickList.Enabled = enabledCheck;
                quickList.Visible = enabledCheck;
                quickList.SupportsMultipleBlocks = false;
                quickList.VisibleRowsCount = 6;
                quickList.Tooltip = MyStringId.GetOrCompute(
                    "This is the list of gates 'known' to you.\r\n" +
                    "# means you own it\r\n" +
                    "* means the gate is shared with you\r\n" +
                    "+ means gate owner is neutral to you\r\n" +
                    "- means gate is unowned\r\n" +
                    "Enemy gates are not visible, but can be reached if address is known.\r\n" +
                    "=> between the address and grid name means supergate\r\n" +
                    "If there are multiple gates with the same address, only one is reachable.\r\n" +
                    "This list is unique to you, and is not visible to others."
                );

                quickList.ListContent = (b, l, s) =>
                {
                    var list = new List<MyTerminalControlListBoxItem>();

                    try
                    {
                        var gates = new Dictionary<long, KnownGate>(StargateMissionComponent.Instance.KnownGates).Values;
                        foreach (var gate in gates)
                        {
                            var player = gate.OwnerId;
                            var relation = Sandbox.Game.Entities.MyIDModule.GetRelation(player, MyAPIGateway.Session.Player.IdentityId, gate.ShareMode);

                            if (relation == MyRelationsBetweenPlayerAndBlock.Enemies)
                            {
                                Logger.Instance.LogDebug("Skipping enemy gate: " + gate.Address);
                                continue;
                            }

                            // Don't display the gate we'd dial from
                            if (string.Compare(b.GameLogic.GetAs<DHD>()?.m_currentGate?.GameLogic.GetAs<Stargate>()?.Address, gate.Address, true) == 0)
                            {
                                Logger.Instance.LogDebug("Skipping gate with same address: " + gate.Address);
                                continue;
                            }

                            Logger.Instance.LogDebug(System.Environment.CurrentManagedThreadId + "; Gate: " + b.GameLogic.GetAs<DHD>()?.m_currentGate?.GameLogic.GetAs<Stargate>()?.Address);

                            // Prefix will be used to sort
                            var ownerPrefix = String.Empty;
                            if (relation == MyRelationsBetweenPlayerAndBlock.Owner)
                                ownerPrefix = "#";
                            else if (relation == MyRelationsBetweenPlayerAndBlock.FactionShare)
                                ownerPrefix = "*";
                            else if (relation == MyRelationsBetweenPlayerAndBlock.Neutral)
                                ownerPrefix = "+";
                            else if (relation == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                                ownerPrefix = "-";

                            var item = new MyTerminalControlListBoxItem(
                                MyStringId.GetOrCompute(string.Format("{0}{1}{2}{3}", ownerPrefix, gate.Address, gate.GateType == GateType.Supergate ? "=>" : "->", gate.GridName)),
                                MyStringId.NullOrEmpty, gate);
                            list.Add(item);

                            Logger.Instance.LogDebug("DHD List Item: " + item.Text.String);
                            if (string.Compare(gate.Address, b.GameLogic.GetAs<DHD>()?.m_dhdData.Destination, true) == 0 ||
                                string.Compare(gate.Name, b.GameLogic.GetAs<DHD>()?.m_dhdData.Destination, true) == 0)
                                s.Add(item);
                        }
                        l.AddList(list.OrderBy(i => i.Text.String).Distinct().ToList());
                        l.ForEach((i) => Logger.Instance.LogDebug("Saved: " + i.Text.String));
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogException(ex);
                    }

                    Logger.Instance.LogDebug("Item count: " + l.Count);
                };
                quickList.ItemSelected = (b, i) =>
                {
                    if (i.Count == 1)
                    {
                        var address = (i[0].UserData as KnownGate?)?.Address;
                        b.GameLogic.GetAs<DHD>().m_dhdData.Destination = address;
                        MessageUtils.SendMessageToAll(new MessageSetQuickDestination()
                        {
                            DHD = b.EntityId,
                            Address = address,
                            Grid = b.CubeGrid.EntityId,
                            DHDPosition = b.Position
                        });
                    }

                    quickDialText.UpdateVisual();
                };
            }
            MyAPIGateway.TerminalControls.AddControl<IMyButtonPanel>(quickList);
        }

        private static void TerminalControls_CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            var actionssToShow = new List<IMyTerminalAction>(actions);

            foreach (var action in actionssToShow)
            {
                if (block.BlockDefinition.SubtypeName?.Contains("DHD") != true)
                {
                    if (action.Id == "Phoenix.Stargate.QuickDial" ||
                        action.Id == "Phoenix.Stargate.ClearQuickDial"
                        )
                        actions.Remove(action);
                }
            }
        }

        private static void TerminalControls_CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            var controlsToShow = new List<IMyTerminalControl>(controls);

            foreach (var control in controlsToShow)
            {
                if (block.BlockDefinition.SubtypeName?.Contains("DHD") == true)
                {
                    if (control.Id == "Open Toolbar" ||
                            control.Id == "ButtonText" ||
                            control.Id == "ButtonName" ||
                            control.Id == "Open Toolbar"
                        )
                        controls.Remove(control);
                }
            }
        }
        #endregion Terminal Controls

        #region Data Serialization
        public override void SerializeData()
        {
            if (m_dhd == null || m_dhdData == null || !m_bInit || !m_hasBeenDeserialized)
                return;

            if (m_dhd.Storage == null)
                m_dhd.Storage = new MyModStorageComponent();

            // Add vanilla entry so game doesn't crash
            if (!m_dhd.Storage.ContainsKey(Constants.VanillaCustomDataKey))
                m_dhd.CustomData = string.Empty;

            m_dhd.Storage[Constants.StargateDataKey] = SerializeData(m_dhdData);
        }

        static string SerializeData(DHDData data)
        {
            StringBuilder sb = new StringBuilder(50);
            // Format is: "sg:[ac=XX;did=YY]
            sb.Append(MyAPIGateway.Utilities.SerializeToXML(data));
            return sb.ToString();
        }

        public override void DeserializeData()
        {
            if (m_dhd == null)
                return;

            Logger.Instance.LogDebug("DHD: Deserializing: " + m_dhd.EntityId.ToString() + "; " + m_dhd.CustomName);
            if (m_dhd.Storage == null)
                m_dhd.Storage = new MyModStorageComponent();

            if (!m_dhd.Storage.ContainsKey(Constants.VanillaCustomDataKey))
                m_dhd.Storage[Constants.VanillaCustomDataKey] = string.Empty;

            string customname = m_dhd.CustomName;
            DHDData data;

            if (m_dhd.Storage.ContainsKey(Constants.StargateDataKey) && !m_dhd.CustomName.Contains("sg:"))
            {
                customname = m_dhd.Storage[Constants.StargateDataKey];
                data = DeserializeData(ref customname);
                if (data == null)
                {
                    customname = m_dhd.CustomName;
                    data = DeserializeData(ref customname);
                    m_dhd.SetCustomName(customname);
                }
            }
            else
            {
                data = DeserializeData(ref customname);
                m_dhd.SetCustomName(customname);
            }

            if (data != null)
                m_dhdData = data;

            base.DeserializeData();
        }

        DHDData DeserializeData(ref string data)
        {
            DHDData dd = null;

            if (string.IsNullOrWhiteSpace(data))
                return dd;

            int cmdStartIdx = data.IndexOf(" sg:[");
            int cmdEndIdx = data.IndexOf(']', cmdStartIdx >= 0 ? cmdStartIdx : 0);
            // Check if we have custom commands in the name
            if (cmdStartIdx != -1 && cmdEndIdx != -1)
            {
                dd = new DHDData();
                string sCmd = data.Remove(cmdEndIdx).Remove(0, cmdStartIdx + 1);
                data = data.Remove(cmdStartIdx, cmdEndIdx - cmdStartIdx + 1);
                // Split the commands for parsing
                string[] cmds = sCmd.Split(new Char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmd in cmds)
                {
                    string tempCmd = cmd.Trim();

                    if (tempCmd.StartsWith("ACT", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            int timeInSeconds = 0;
                            if (int.TryParse(acopt[1], out timeInSeconds))
                            {
                                dd.AutoCloseTime = timeInSeconds;
                                //dhd.iAutoCloseTime = (int)Math.Ceiling(((timeInSeconds * 60) / 100));
                            }
                        }
                    }

                    if (tempCmd.StartsWith("ACA", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            bool val;
                            if (Boolean.TryParse(acopt[1], out val))
                                dd.AutoCloseWithAll = val;
                        }
                    }

                    if (tempCmd.StartsWith("DST", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            dd.Destination = acopt[1];
                        }
                    }

                    if (tempCmd.StartsWith("CLR", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            bool val;
                            if (Boolean.TryParse(acopt[1], out val))
                                dd.AutoClear = val;
                        }
                    }
                }
            }
            else if (data.Contains("DHDData"))
            {
                dd = MyAPIGateway.Utilities.SerializeFromXML<DHDData>(data);
            }

            return dd;
        }
        #endregion Data Serialization

        #region Sounds
        public void PlaySound(string soundname, Action<MyEntity3DSoundEmitter> stoppedCallback = null)
        {
            MyEntity3DSoundEmitter emitter = SoundEmitter;

            if (emitter != null)
            {
                if (string.IsNullOrEmpty(soundname))
                {
                    Logger.Instance.LogDebug("Gate StopSound");
                    emitter.StopSound(false, true);
                }
                else
                {
                    Logger.Instance.LogDebug("PlaySound: " + soundname);
                    MySoundPair sound = new MySoundPair(soundname);

                    if (stoppedCallback != null)
                    {
                        Logger.Instance.LogDebug("Setting callback: " + stoppedCallback);
                        double length = Constants.Dialing_Sound_Length_SG1_Long;

                        if (Constants.Dialing_Sounds.ContainsKey(soundname))
                            length = Constants.Dialing_Sounds[soundname];

                        //ElapsedEventHandler callback = null;

                        //callback = delegate (object sender, ElapsedEventArgs e)
                        //{
                        //    m_callbackTimer.Elapsed -= callback;
                        //    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                        //    {
                        //        if (!Closed && !MarkedForClose)
                        //            stoppedCallback(emitter);
                        //    });
                        //};

                        //// Must use a Timer, since sound callbacks are triggered early if
                        //// on server, or player is far away from source.
                        //m_callbackTimer.Interval = length;
                        //m_callbackTimer.Elapsed += callback;
                        //m_callbackTimer.AutoReset = false;
                        //m_callbackTimer.Start();
                    }
                    emitter.PlaySound(sound);
                }
            }
        }
        #endregion Sounds

        #region Misc
        private void RebuildWithButtonText()
        {
            if (MyAPIGateway.Multiplayer == null)
                return;

            var block = Entity as IMyButtonPanel;
            var def = ((Entity as IMyCubeBlock)?.SlimBlock as IMySlimBlock).BlockDefinition as MyButtonPanelDefinition;

            if (block.GetButtonName(0) == "Dial")
                return;

            block.SetCustomButtonName(0, "Dial");

            if (def?.ButtonCount == 40)
                for (int x = 1; x < Constants.ButtonsToCharacters.Length; x++)
                    block.SetCustomButtonName(x, Constants.ButtonsToCharacters[x].ToString());
        }

        private void StartQuickDial()
        {
            RaiseQuickDialSync();
        }

        private void LightUpDialButton()
        {
            var color = Constants.DHD_Dome_Light_Active_SG1;
            if (m_dhdEdition == GateEdition.Third)
                color = Constants.DHD_Dome_Light_Active_SGA;

            m_dhd.SetEmissiveParts("DialButton", color, 0.5f);
        }

        public void ResetDHD(bool fromMessage = false)
        {
            if (!fromMessage && MyAPIGateway.Multiplayer != null)
                MessageUtils.SendMessageToServer(new MessageResetDHD() { DHD = m_dhd.EntityId });

            var color = Constants.DHD_Dome_Light_Inactive_SG1;
            if (m_dhdEdition == GateEdition.Third)
                color = Constants.DHD_Dome_Light_Inactive_SGA;

            m_timeoutTimer.Stop();
            m_dhd.SetEmissiveParts("DialButton", color, 0.0f);
            for (var x = 1; x < 40; x++)
                m_dhd.SetEmissiveParts("Glyph" + x.ToString("00"), Color.DarkGray, 0.0f);
            m_currentAddress.Clear();
            PlaySound("DHDClose");
        }
#endregion Misc
    }

#endregion New DHD

#region Old DHD
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), false,
        //"DHD Generic",
        //"DHD Generic Small",
        "DHD"
        //"DHD Atlantis",
        //"DHD SGU",
        //"DHD Computer",
        //"DHD Computer Small"
        )]
    public class LegacyDHD : MyGameLogicComponent
    {
        IMyDoor m_dhd;

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return Container.Entity.GetObjectBuilder(copy);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            m_dhd = Container.Entity as IMyDoor;
            m_dhd.AppendingCustomInfo += dhd_AppendingCustomInfo;
        }

        private void dhd_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2)
        {
            arg2.Append("Legacy DHD\n");
            arg2.Append("Enter chat command to upgrade:\n");
            arg2.Append(string.Format("/{0} upgrade", Globals.ModName));
        }
    }

    // TODO: Deprecate and get rid of this eventually
    namespace Extensions
    {
        public static class DoorDHDExtensions
        {
            public static Dictionary<long, DHDDataOld> dhdMap;

            static DoorDHDExtensions()
            {
                dhdMap = new Dictionary<long, DHDDataOld>();
            }

            //public static DHDData AddNewDHD(IMyDoor dhd)
            //{
            //    // New object, do stuff
            //    var dhdd = new DHDData();
            //    DoorDHDExtensions.dhdMap.Add(dhd.EntityId, dhdd);               // Add the new block to the map, for referencing later
            //    dhd.DoorStateChanged += dhd.dhd_DoorStateChanged;               // Hook into the door event, this is how the majority of it works.
            //    ((IMyTerminalBlock)dhd).CustomNameChanged += dhd_CustomNameChanged;
            //    dhd.OnClose += new Action<IMyEntity>(dhd_OnClose);              // This will handle cleanup when the entity is removed
            //    dhd.ParseNameArguments();                                       // Make sure parameters are parsed

            //    return dhdd;
            //}

            public static bool IsValid(this IMyTerminalBlock dhd, IMySlimBlock gate, IMyTerminalBlock sourceGate)
            {
                Logger.Instance.LogDebug("IsValid()");
                Logger.Instance.IndentLevel++;

                if (MyAPIGateway.Utilities == null)
                    return false;

                if (gate == null || sourceGate == null || gate.FatBlock == null || gate.FatBlock.GameLogic.GetAs<Stargate>() == null || sourceGate.GameLogic.GetAs<Stargate>() == null)
                {
                    Logger.Instance.LogDebug("Invalid condition");
                    Logger.Instance.IndentLevel--;
                    return false;
                }

                // Skip disabled or destroyed gates
                // Also skip gates that don't match the source (eg. regular gate vs supergate)
                if (gate == null || gate.IsDestroyed || gate.FatBlock == null || !gate.FatBlock.IsFunctional ||
                    (gate.CubeGrid == null && (gate.CubeGrid as MyCubeGrid).Projector != null) ||
                    (sourceGate != null &&
                        (
                            !(gate.FatBlock.GameLogic.GetAs<Stargate>()?.StargateType == sourceGate.GameLogic.GetAs<Stargate>()?.StargateType) ||
                            gate.FatBlock.EntityId == sourceGate.EntityId ||
                            gate.FatBlock.GetTopMostParent().EntityId == sourceGate.GetTopMostParent().EntityId ||
                            (dhd != null && gate.FatBlock.GetTopMostParent().EntityId == dhd.GetTopMostParent().EntityId)
                        )
                    ))
                {
                    Logger.Instance.LogDebug("Gate not valid: " + (gate != null ? (gate.FatBlock as IMyTerminalBlock).CustomName : "<null>"));
                    Logger.Instance.IndentLevel--;
                    return false;
                }
                Logger.Instance.LogDebug("Gate is valid: " + (gate.FatBlock as IMyTerminalBlock).CustomName);
                Logger.Instance.IndentLevel--;
                return true;
            }

            // This will get the closest gate to the activated DHD
            public static IMyTerminalBlock GetNearestGate(this IMyTerminalBlock source, List<IMySlimBlock> gateList = null, bool showMessage = true)
            {
                if (showMessage)
                {
                    Logger.Instance.LogDebug("GetNearestGate()");
                    Logger.Instance.IndentLevel++;
                }

                IMyTerminalBlock nearest = null;
                HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
                double distance = 0.0d;

                // Look for a gate in a group first, before searching
                var gates = DoorDHDExtensions.GetGroupedBlocks(source, new MyObjectBuilderType(typeof(MyObjectBuilder_TerminalBlock)), "STARGATE");

                if (gates != null && gates.Count == 0)
                {
                    if (gateList == null)
                        gateList = GetGateList();

                    foreach (var gate in gateList)
                    {
                        // Skip disabled or destroyed gates
                        if (gate.IsDestroyed || gate.FatBlock == null || !gate.FatBlock.IsFunctional ||
                            (gate.FatBlock.GameLogic.GetAs<Stargate>() == null))
                        {
                            if (showMessage)
                                Logger.Instance.LogDebug("Skipping: " + gate.FatBlock.DisplayNameText);
                            continue;
                        }

                        gates.Add(gate.FatBlock as IMyTerminalBlock);
                    }
                }

                foreach (var gate in gates)
                {
                    if (distance == 0.0d || (source.GetPosition() - gate.GetPosition()).Length() < distance)
                    {
                        nearest = gate as IMyTerminalBlock;
                        distance = (source.GetPosition() - gate.GetPosition()).Length();
                    }

                    // If the gate is on the same grid, give it priority
                    if (gate.GetTopMostParent().EntityId == source.GetTopMostParent().EntityId)
                    {
                        if (nearest.GetTopMostParent().EntityId != source.GetTopMostParent().EntityId ||
                            distance == 0.0d || (source.GetPosition() - gate.GetPosition()).Length() < distance)
                        {
                            nearest = gate as IMyTerminalBlock;
                            distance = (source.GetPosition() - gate.GetPosition()).Length();
                        }
                    }
                }

                if (showMessage && StargateAdmin.Configuration.Debug && nearest != null)
                    Logger.Instance.LogDebug("Found closest Gate: " + nearest.DisplayNameText + " on " + nearest.GetTopMostParent().DisplayName + ", distance: " + (source.GetPosition() - nearest.GetPosition()).Length());

                if (showMessage)
                    Logger.Instance.IndentLevel--;
                return nearest;
            }

            // This will get the closest gate to the activated DHD
            public static IMyTerminalBlock GetNamedGate(this IMyTerminalBlock dhd, string gateName, IMyTerminalBlock sourceGate = null, bool matchExact = false)
            {
                Logger.Instance.LogDebug("GetNamedGate()");
                Logger.Instance.IndentLevel++;

                HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
                List<IMySlimBlock> gateList = GetGateList();
                List<IMySlimBlock> namedList = new List<IMySlimBlock>();

                if (dhd != null && string.Compare(gateName, "AutoDHD", true) == 0)
                    return dhd.GetNearestGateOnDifferentGrid(sourceGate);

                //if (StargateAdmin.Configuration.Debug)
                //    MyAPIGateway.Utilities.ShowNotification("Searching for gate named: " + gateName, 7500);
                Logger.Instance.LogDebug("Searching for gate named: " + gateName);

                foreach (var gate in gateList)
                {
                    if (StargateAdmin.Configuration.Debug)
                        MyAPIGateway.Utilities.ShowNotification("Gate: " + (gate.FatBlock as IMyTerminalBlock).CustomName);
                    Logger.Instance.LogDebug("Gate: " + (gate.FatBlock as IMyTerminalBlock).CustomName);

                    if (!IsValid(dhd, gate, sourceGate))
                        continue;

                    if (string.Compare(gate.FatBlock.GameLogic.GetAs<Stargate>().Address, gateName, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        (!matchExact && string.Compare((gate.FatBlock as IMyTerminalBlock).CustomName, gateName, StringComparison.InvariantCultureIgnoreCase) == 0))
                    {
                        if (StargateAdmin.Configuration.Debug)
                            MyAPIGateway.Utilities.ShowNotification("Found match: " + (gate.FatBlock as IMyTerminalBlock).CustomName, 7500);
                        Logger.Instance.LogDebug("Found match: " + (gate.FatBlock as IMyTerminalBlock).CustomName);
                        namedList.Add(gate);
                    }
                    else
                    {
                        Logger.Instance.LogDebug(string.Format("Gate not matched to {0}: {1}; {2}", gateName, gate.FatBlock.GameLogic.GetAs<Stargate>().Address, (gate.FatBlock as IMyTerminalBlock).CustomName));
                    }
                }
                Logger.Instance.IndentLevel--;

                if (namedList.Count > 1)
                    return GetNearestGate(sourceGate, namedList);
                else if (namedList.Count == 1)
                    return namedList[0].FatBlock as IMyTerminalBlock;
                else
                    return null;
            }

            /// <summary>
            /// This gets all the DHDs in the world, and maps them to data for usage.
            /// TODO: Move this to a separate thread for performance.
            /// </summary>
            /// <returns></returns>
            public static List<IMySlimBlock> GetDHDs()
            {
                HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
                List<IMySlimBlock> dhdList = new List<IMySlimBlock>();

                MyAPIGateway.Entities.GetEntities(hash, (x) => x is IMyCubeGrid);

                foreach (var entity in hash)
                {
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    IMyCubeGrid grid = entity as IMyCubeGrid;

                    grid.GetBlocks(blocks, (x) => x.FatBlock != null && x.FatBlock is IMyDoor &&
                        !String.IsNullOrEmpty(x.FatBlock.BlockDefinition.SubtypeId) &&
                        (x.FatBlock.BlockDefinition.SubtypeId.Contains("DHD")));

                    foreach (var block in blocks)
                        dhdList.Add(block);
                }

                return dhdList;
            }

            // DHD could be either programmable block, or a door
            public static IMyFunctionalBlock GetNearestGateOnDifferentGrid(this IMyTerminalBlock dhd, IMyTerminalBlock sourceGate)
            {
                Logger.Instance.LogDebug("GetNearestGateOnDifferentGrid()");
                Logger.Instance.IndentLevel++;
                List<IMySlimBlock> gateList = GetGateList();
                double distance = 0.0d;
                IMyFunctionalBlock nearest = null;

                foreach (var gate in gateList)
                {
                    Logger.Instance.LogDebug("Gate: " + gate.FatBlock.BlockDefinition.SubtypeId);
                    // Skip active, disabled, or destroyed gates, also make sure supergates aren't matched to regular gates
                    // Skip if on same grid as source gate or dhd
                    if (gate.IsDestroyed || !gate.FatBlock.IsFunctional || gate.FatBlock.GameLogic.GetAs<Stargate>() == null ||
                        sourceGate == null ||
                        gate.FatBlock.GameLogic.GetAs<Stargate>().StargateType != sourceGate.GameLogic.GetAs<Stargate>().StargateType ||
                        gate.FatBlock.GameLogic.GetAs<Stargate>()?.Data.State == GateState.Active &&
                        gate.FatBlock.GetTopMostParent().EntityId == sourceGate.GetTopMostParent().EntityId ||
                        gate.FatBlock.GetTopMostParent().EntityId == dhd.GetTopMostParent().EntityId ||
                        !(gate.FatBlock as IMyTerminalBlock).HasPlayerAccess(dhd.OwnerId))        // Make sure DHD owner has access
                        continue;

                    // Then find the closest.
                    if (distance == 0.0d || (dhd.GetPosition() - gate.FatBlock.GetPosition()).Length() < distance)
                    {
                        nearest = gate.FatBlock as IMyFunctionalBlock;
                        distance = (dhd.GetPosition() - gate.FatBlock.GetPosition()).Length();
                    }
                }

                if (nearest != null)
                {
                    if (StargateAdmin.Configuration.Debug)
                        Utils.ShowMessageToUsersInRange(dhd, "Found closest remote Gate: " + nearest.DisplayNameText + " on " + nearest.GetTopMostParent().DisplayName + ", distance: " + (dhd.GetPosition() - nearest.GetPosition()).Length(), 10000);
                    Logger.Instance.LogDebug("Found closest remote Gate: " + nearest.DisplayNameText + " on " + nearest.GetTopMostParent().DisplayName + ", distance: " + (dhd.GetPosition() - nearest.GetPosition()).Length());
                }
                Logger.Instance.IndentLevel--;
                return nearest;
            }

            public static List<IMySlimBlock> GetGateList()
            {
                HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
                List<IMySlimBlock> gateList = new List<IMySlimBlock>();

                MyAPIGateway.Entities.GetEntities(hash, (x) => x is IMyCubeGrid);

                foreach (var entity in hash)
                {
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    IMyCubeGrid grid = entity as IMyCubeGrid;

                    grid.GetBlocks(blocks, (x) => x.FatBlock != null && x.FatBlock is IMyTerminalBlock &&
                        !String.IsNullOrEmpty(x.FatBlock.BlockDefinition.SubtypeId) &&
                        (x.FatBlock.BlockDefinition.SubtypeId.Contains("Stargate") || x.FatBlock.BlockDefinition.SubtypeId.Contains("Supergate")) &&
                        !x.FatBlock.BlockDefinition.SubtypeId.Contains("Horizon"));

                    foreach (var block in blocks)
                    {
                        if (StargateAdmin.Configuration.Debug)
                            Logger.Instance.LogDebug("Found gate: " + block.FatBlock.DisplayNameText);
                        gateList.Add(block);
                    }
                }

                return gateList;
            }


            public static List<IMyTerminalBlock> GetGroupedBlocks(IMyTerminalBlock reference, MyObjectBuilderType objtype, string subtype = null)
            {
                var blocks = new List<IMyTerminalBlock>();

                if (MyAPIGateway.TerminalActionsHelper == null)
                    return blocks;

                if (reference == null)
                    return blocks;

                var terminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(reference.CubeGrid as IMyCubeGrid);

                if (terminal == null)   // This will get hit when leaving the world
                    return blocks;

                var groups = new List<IMyBlockGroup>();
                var blocksInGroup = new List<IMyTerminalBlock>();
                terminal.GetBlockGroups(groups);

                // Scan each group, looking for the FTL
                foreach (var group in groups)
                {
                    group.GetBlocks(blocksInGroup);

                    if (blocksInGroup.Contains(reference))
                    {
                        // We found one, grab the blocks we want
                        foreach (var block in blocksInGroup)
                        {
                            // Make sure the blocks match the type we're looking for
                            if (block.BlockDefinition.TypeId == objtype
                                && (string.IsNullOrEmpty(subtype)
                                    || block.BlockDefinition.SubtypeId.ToUpperInvariant().Contains(subtype)))
                                blocks.Add(block as IMyTerminalBlock);
                        }
                    }
                }
                return blocks;
            }
        }
    }
#endregion Old DHD
}
