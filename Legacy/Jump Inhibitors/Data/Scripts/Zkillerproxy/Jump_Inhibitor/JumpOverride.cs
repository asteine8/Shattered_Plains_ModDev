using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Zkillerproxy.JumpInhibitorMod
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), true)]
    public class JumpOverride : MyGameLogicComponent
    {
        private bool ShouldBeJumping = false;
        private bool IsWorkingThisUpdate = false;
        private static readonly ushort InhibitorInfoRequest = 60102;
        private static readonly ushort InhibitorInfoSend = 60103;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyJumpDrive JumpDrive = (MyJumpDrive)Entity;

            if (JumpDrive != null)
            {
                List<IMyTerminalControl> TerminalControls = new List<IMyTerminalControl>();
                MyAPIGateway.TerminalControls.GetControls<IMyJumpDrive>(out TerminalControls);

                List<IMyTerminalAction> TerminalActions = new List<IMyTerminalAction>();
                MyAPIGateway.TerminalControls.GetActions<IMyJumpDrive>(out TerminalActions);

                IMyTerminalControlButton JumpButton = TerminalControls.Find(x => x.Id == "Jump") as IMyTerminalControlButton;
                IMyTerminalAction JumpAction = TerminalActions.Find(x => x.Id == "Jump") as IMyTerminalAction;

                if (JumpButton != null)
                {
                    JumpButton.Action = JumpTrigger;
                }

                if (JumpAction != null)
                {
                    JumpAction.Action = JumpTrigger;
                }

                MyAPIGateway.Utilities.InvokeOnGameThread(RegisterInfoSendHandler);
            }
        }

        public override void Close()
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(UnregisterInfoSendHandler);
        }

        public override void UpdateBeforeSimulation()
        {
            IsWorkingThisUpdate = false;
        }

        private void JumpTrigger(IMyTerminalBlock block)
        {
            if (Entity != null)
            {
                MyObjectBuilder_JumpDrive ObjectBuilder = (Entity as MyJumpDrive).GetObjectBuilderCubeBlock(true) as MyObjectBuilder_JumpDrive;

                int hash = ObjectBuilder.JumpTarget ?? 0;
                string package = MyAPIGateway.Utilities.SerializeToXML(new JumpInhibitorPackage(hash, MyAPIGateway.Multiplayer.MyId));

                ShouldBeJumping = true;
                MyAPIGateway.Multiplayer.SendMessageToServer(InhibitorInfoRequest, MyAPIGateway.Utilities.SerializeToBinary(package));
            }
        }

        private void RegisterInfoSendHandler()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(InhibitorInfoSend, InhibitorInfoSendHandler);
        }

        private void UnregisterInfoSendHandler()
        {
            if (Entity != null)
            {
                MyAPIGateway.Multiplayer.UnregisterMessageHandler(InhibitorInfoSend, InhibitorInfoSendHandler);
            }
        }

        private void HandleJump(List<JumpInhibitorInfoPackage> InfoList, Vector3D? JumpTarget)
        {
            foreach (JumpInhibitorInfoPackage Info in InfoList)
            {
                if (Info.TerminalSettings.EnableJumpWithin)
                {
                    if (Vector3D.DistanceSquared(Entity.GetPosition(), Info.InhibitorPos) < Math.Pow(Info.Radius, 2))
                    {
                        MyAPIGateway.Utilities.ShowNotification("JUMP NOT POSSIBLE: You are in an inhibition field", 3000, "Red");
                        return;
                    }
                }
                if (Info.TerminalSettings.EnableJumpOut)
                {
                    if (JumpTarget != null)
                    {
                        if (JumpTarget == Vector3D.PositiveInfinity)
                        {
                            if (Vector3D.DistanceSquared(ComputeBlindJumpPoint(Entity as IMyJumpDrive), Info.InhibitorPos) < Math.Pow(Info.Radius, 2))
                            {
                                MyAPIGateway.Utilities.ShowNotification("JUMP NOT POSSIBLE: Jump target is within an inhibiton field.", 3000, "Red");
                                return;
                            }
                        }
                        else if (Vector3D.DistanceSquared((Vector3D)JumpTarget, Info.InhibitorPos) < Math.Pow(Info.Radius, 2))
                        {
                            MyAPIGateway.Utilities.ShowNotification("JUMP NOT POSSIBLE: Jump target is within an inhibiton field.", 3000, "Red");
                            return;
                        }
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowNotification("JUMP NOT POSSIBLE: Jump target GPS does not exist! (Or belongs to a player who is not logged in)", 3000, "Red");
                        return;
                    }
                }
            }
            
            (Entity as IMyJumpDrive).Jump(true);
        }

        private Vector3D ComputeBlindJumpPoint(IMyJumpDrive JumpDrive)
        {
            MyObjectBuilder_JumpDrive ObjectBuilder = JumpDrive.GetObjectBuilderCubeBlock(true) as MyObjectBuilder_JumpDrive;
            Sandbox.ModAPI.Ingame.IMyShipController Controller = MyAPIGateway.Session.Player.Character.Parent as Sandbox.ModAPI.Ingame.IMyShipController;

            List<IMyTerminalControl> TerminalControls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyJumpDrive>(out TerminalControls);
            IMyTerminalControlSlider DistanceSlider = TerminalControls.Find(x => x.Id == "JumpDistance") as IMyTerminalControlSlider;
            
            Vector3D JumpDirection = Vector3D.Transform(Base6Directions.GetVector(Controller.Orientation.Forward), Controller.CubeGrid.WorldMatrix.GetOrientation());
            JumpDirection.Normalize();
            return JumpDrive.GetPosition() + JumpDirection * (GetMaxJumpDistance(MyAPIGateway.Session.Player.IdentityId, JumpDrive) * ObjectBuilder.JumpRatio);
        }

        //This is already in the game, but we dont have access to it, slight modificaitons have been made for my purposes.
        private double GetMaxJumpDistance(long userId, IMyJumpDrive JumpDrive)
        {
            double val1 = 0.0;
            double val2 = 0.0;
            double currentMass = JumpDrive.CubeGrid.Physics.Mass;
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            JumpDrive.CubeGrid.GetBlocks(Blocks, (x) => { if (x.FatBlock != null && x.FatBlock as IMyJumpDrive != null) { return true; } return false; });

            foreach (IMySlimBlock Block in Blocks)
            {
                MyJumpDrive jumpDrive = Block.FatBlock as MyJumpDrive;
                if (jumpDrive.CanJumpAndHasAccess(userId))
                {
                    val1 += jumpDrive.BlockDefinition.MaxJumpDistance;
                    val2 += jumpDrive.BlockDefinition.MaxJumpDistance * (jumpDrive.BlockDefinition.MaxJumpMass / currentMass);
                }
            }
            return Math.Min(val1, val2);
        }

        private void InhibitorInfoSendHandler(byte[] obj)
        {
            if ((Entity as IMyJumpDrive) != null && IsWorkingThisUpdate == false && ShouldBeJumping == true)
            {
                JumpInhibitorPackage Package = null;

                try
                {
                    Package = MyAPIGateway.Utilities.SerializeFromXML<JumpInhibitorPackage>(MyAPIGateway.Utilities.SerializeFromBinary<string>(obj));
                }
                catch
                {
                    Log("ERROR: Failed to serialize incoming byte[] for info reciever!");
                }

                if (Package != null)
                {
                    IsWorkingThisUpdate = true;
                    ShouldBeJumping = false;
                    HandleJump(Package.Info, Package.Pos);
                }
            }
        }

        private void Log(string Input)
        {
            MyLog.Default.WriteLineAndConsole("Jump Inhibitor (JumpDrive): " + Input);
        }
    }
}
