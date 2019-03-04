using System.Collections.Generic;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace Zkillerproxy.JumpInhibitorMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class JumpInhibitorClient : MySessionComponentBase
    {
        private static readonly ushort UpdateServerTerminalSend = 60106;
        private static readonly ushort UpdateClientTerminalSend = 60107;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += CreateTerminalControls;
            MyAPIGateway.TerminalControls.CustomActionGetter += CreateTerminalActions;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(UpdateClientTerminalSend, UpdateClientTerminalHandler);
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(UpdateClientTerminalSend, UpdateClientTerminalHandler);
            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (Client): " + "Unloaded Message Handlers.");
        }

        private void CreateTerminalControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block.GameLogic.GetAs<JumpInhibitorBlock>() != null)
            {
                JumpInhibitorBlock Inhibitor = block.GameLogic.GetAs<JumpInhibitorBlock>();

                // Grab config file

                JumpInhibitorMod.JumpInhibitorConfig config = Inhibitor.Config;


                if (config.AllowInhibitingJumpsOut) {
                    IMyTerminalControlOnOffSwitch allowJumpWithinSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyBeacon>("AllowJumpWithinSwitch");
                    allowJumpWithinSwitch.Getter = (x) => { return Inhibitor.TerminalSettings.EnableJumpWithin; };
                    allowJumpWithinSwitch.Setter = (x, y) => { Inhibitor.TerminalSettings.EnableJumpWithin = y; Inhibitor.SaveTerminalSettings(block); block.Components.Get<MyResourceSinkComponent>().Update(); block.RefreshCustomInfo(); UpdateServerTerminal(Inhibitor); };
                    allowJumpWithinSwitch.OnText = MyStringId.GetOrCompute("No");
                    allowJumpWithinSwitch.OffText = MyStringId.GetOrCompute("Yes");
                    allowJumpWithinSwitch.Title = MyStringId.GetOrCompute("Allow Jumping Within Field");
                    allowJumpWithinSwitch.Tooltip = MyStringId.GetOrCompute("Should ships be allowed to jump when they're in the field.");
                    allowJumpWithinSwitch.SupportsMultipleBlocks = true;
                    controls.Add(allowJumpWithinSwitch);
                }
                else {
                    Inhibitor.TerminalSettings.EnableJumpWithin = true;
                }

                if (config.AllowInhibitingJumpsIn) {
                    IMyTerminalControlOnOffSwitch allowJumpOutSwitch = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyBeacon>("AllowJumpOutSwitch");
                    allowJumpOutSwitch.Getter = (x) => { return Inhibitor.TerminalSettings.EnableJumpOut; };
                    allowJumpOutSwitch.Setter = (x, y) => { Inhibitor.TerminalSettings.EnableJumpOut = y; Inhibitor.SaveTerminalSettings(block); block.Components.Get<MyResourceSinkComponent>().Update(); block.RefreshCustomInfo(); UpdateServerTerminal(Inhibitor); };
                    allowJumpOutSwitch.OnText = MyStringId.GetOrCompute("No");
                    allowJumpOutSwitch.OffText = MyStringId.GetOrCompute("Yes");
                    allowJumpOutSwitch.Title = MyStringId.GetOrCompute("Allow Jumping Into Field");
                    allowJumpOutSwitch.Tooltip = MyStringId.GetOrCompute("Should ships be allowed to jump into the field from outside.");
                    allowJumpOutSwitch.SupportsMultipleBlocks = true;
                    controls.Add(allowJumpOutSwitch);
                }
                else {
                    Inhibitor.TerminalSettings.EnableJumpOut = true;
                }
            }
        }

        private void CreateTerminalActions(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (block.GameLogic.GetAs<JumpInhibitorBlock>() != null)
            {
                JumpInhibitorBlock Inhibitor = block.GameLogic.GetAs<JumpInhibitorBlock>();

                JumpInhibitorMod.JumpInhibitorConfig config = Inhibitor.Config;

                if  (config.AllowInhibitingJumpsOut) {
                    IMyTerminalAction allowJumpWithinSwitchAction = MyAPIGateway.TerminalControls.CreateAction<IMyBeacon>("AllowJumpWithinSwitchAction");
                    allowJumpWithinSwitchAction.Name = new StringBuilder("Allow Jumping Within Field");
                    allowJumpWithinSwitchAction.Icon = @"Textures\GUI\Controls\button_arrow_right_highlight.dds";
                    allowJumpWithinSwitchAction.Action = (x) => {
                        if (Inhibitor.TerminalSettings.EnableJumpWithin == true) {
                            Inhibitor.TerminalSettings.EnableJumpWithin = false;
                        } else {
                            Inhibitor.TerminalSettings.EnableJumpWithin = true;
                        } Inhibitor.SaveTerminalSettings(block);
                        // block.Components.Get<MyResourceSinkComponent>().SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, Inhibitor.ComputePower);
                        block.Components.Get<MyResourceSinkComponent>().Update();
                        block.RefreshCustomInfo();
                        UpdateServerTerminal(Inhibitor);
                    };
                    allowJumpWithinSwitchAction.Writer = (x, y) => { if (Inhibitor.TerminalSettings.EnableJumpWithin == true) { y.Append(new StringBuilder("No")); } else { y = y.Append(new StringBuilder("Yes")); } };
                    allowJumpWithinSwitchAction.ValidForGroups = true;
                    actions.Add(allowJumpWithinSwitchAction);
                }

                if (config.AllowInhibitingJumpsIn) {
                    IMyTerminalAction allowJumpOutSwitchAction = MyAPIGateway.TerminalControls.CreateAction<IMyBeacon>("AllowJumpOutSwitchAction");
                    allowJumpOutSwitchAction.Name = new StringBuilder("Allow Jumping Into Field");
                    allowJumpOutSwitchAction.Icon = @"Textures\GUI\Controls\button_arrow_left_highlight.dds";
                    allowJumpOutSwitchAction.Action = (x) => { 
                        if (Inhibitor.TerminalSettings.EnableJumpOut == true) {
                            Inhibitor.TerminalSettings.EnableJumpOut = false; 
                        } else {
                            Inhibitor.TerminalSettings.EnableJumpOut = true;
                        } 
                        Inhibitor.SaveTerminalSettings(block);
                        // block.Components.Get<MyResourceSinkComponent>().SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, Inhibitor.ComputePower);
                        block.Components.Get<MyResourceSinkComponent>().Update();
                        block.RefreshCustomInfo();
                        UpdateServerTerminal(Inhibitor);
                    };
                    allowJumpOutSwitchAction.Writer = (x, y) => { if (Inhibitor.TerminalSettings.EnableJumpOut == true) { y.Append(new StringBuilder("No")); } else { y = y.Append(new StringBuilder("Yes")); } };
                    allowJumpOutSwitchAction.ValidForGroups = true;
                    actions.Add(allowJumpOutSwitchAction);
                }
            }
        }

        private void UpdateServerTerminal(JumpInhibitorBlock Inhibitor)
        {
            MyAPIGateway.Multiplayer.SendMessageToServer(UpdateServerTerminalSend, MyAPIGateway.Utilities.SerializeToBinary(MyAPIGateway.Utilities.SerializeToXML(new JumpInhibitorInfoPackage(Inhibitor.TerminalSettings, Inhibitor.EntityID))));
        }

        private void UpdateClientTerminalHandler(byte[] obj)
        {
            JumpInhibitorInfoPackage ServerInhibitorInfo = null;

            try
            {
                ServerInhibitorInfo = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorInfoPackage>(MyAPIGateway.Utilities.SerializeFromBinary<string>(obj));
            }
            catch
            {
                Log("ERROR: Failed to serialize incoming byte[] for client terminal updateor!");
            }

            if (ServerInhibitorInfo != null)
            {
                IMyEntity ClientEntity = MyAPIGateway.Entities.GetEntityById(ServerInhibitorInfo.EntityID);

                if (ClientEntity != null)
                {
                    JumpInhibitorBlock ClientInhibitor = ClientEntity.GameLogic.GetAs<JumpInhibitorBlock>();

                    if (ClientInhibitor != null)
                    {
                        ClientInhibitor.TerminalSettings = ServerInhibitorInfo.TerminalSettings;
                        ClientInhibitor.SaveTerminalSettings(ClientEntity);
                    }
                }
            }
        }

        private void Log(string Input)
        {
            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (Client): " + Input);
        }
    }
}
