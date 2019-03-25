using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Ingame = VRage.Game.ModAPI.Ingame;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Phoenix.Stargate
{
    using Extensions;
    using SpaceEngineers.Game.ModAPI;
    #region MP messaging
    public enum MessageType : ushort
    {
        OpenGate = 1,
        CloseGate,
        MoveEntity,
    }

    public enum MessageSide
    {
        ServerSide,
        ClientSide
    }
    #endregion

    /// <summary>
    /// This class is a quick workaround to get an abstract class deserialized. It is to be removed when using a byte serializer.
    /// </summary>
    [ProtoContract]
    public class MessageContainer
    {
        [ProtoMember(1)]
        public MessageBase Content;
    }

    public static class MessageUtils
    {
        public static List<byte> Client_MessageCache = new List<byte>();
        public static Dictionary<ulong, List<byte>> Server_MessageCache = new Dictionary<ulong, List<byte>>();

        public static readonly ushort MessageId = 19841;
        static readonly int MAX_MESSAGE_SIZE = 4096;

        public static void SendMessageToServer(MessageBase message)
        {
            message.Side = MessageSide.ServerSide;
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
            var xml = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(xml);
            Logger.Instance.LogDebug(string.Format("SendMessageToServer {0} {1} {2}, {3}b", message.SenderSteamId, message.Side, message.GetType().Name, byteData.Length));
            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageToServer(MessageId, byteData);
            else
                SendMessageParts(byteData, MessageSide.ServerSide);
        }

        /// <summary>
        /// Creates and sends an entity with the given information for the server and all players.
        /// </summary>
        /// <param name="content"></param>
        public static void SendMessageToAll(MessageBase message, bool syncAll = true)
        {
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;

            if (syncAll || !MyAPIGateway.Multiplayer.IsServer)
                SendMessageToServer(message);
            SendMessageToAllPlayers(message);
        }

        public static void SendMessageToAllPlayers(MessageBase messageContainer)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content))); <- does not work as expected ... so it doesn't work at all?
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && !p.IsHost());
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player.SteamUserId, messageContainer);
        }

        public static void SendMessageToPlayer(ulong steamId, MessageBase message)
        {
            message.Side = MessageSide.ClientSide;
            var xml = MyAPIGateway.Utilities.SerializeToXML(new MessageContainer() { Content = message });
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(xml);

            Logger.Instance.LogDebug(string.Format("SendMessageToPlayer {0} {1} {2}, {3}b", steamId, message.Side, message.GetType().Name, byteData.Length));

            if (byteData.Length <= MAX_MESSAGE_SIZE)
                MyAPIGateway.Multiplayer.SendMessageTo(MessageId, byteData, steamId);
            else
                SendMessageParts(byteData, MessageSide.ClientSide, steamId);
        }

        #region Message Splitting
        /// <summary>
        /// Calculates how many bytes can be stored in the given message.
        /// </summary>
        /// <param name="message">The message in which the bytes will be stored.</param>
        /// <returns>The number of bytes that can be stored until the message is too big to be sent.</returns>
        public static int GetFreeByteElementCount(MessageIncomingMessageParts message)
        {
            message.Content = new byte[1];
            var xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var oneEntry = System.Text.Encoding.UTF8.GetBytes(xmlText).Length;

            message.Content = new byte[4];
            xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var twoEntries = System.Text.Encoding.UTF8.GetBytes(xmlText).Length;

            // we calculate the difference between one and two entries in the array to get the count of bytes that describe one entry
            // we divide by 3 because 3 entries are stored in one block of the array
            var difference = (double)(twoEntries - oneEntry) / 3d;

            // get the size of the message without any entries
            var freeBytes = MAX_MESSAGE_SIZE - oneEntry - Math.Ceiling(difference);

            int count = (int)Math.Floor((double)freeBytes / difference);

            // finally we test if the calculation was right
            message.Content = new byte[count];
            xmlText = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = message });
            var finalLength = System.Text.Encoding.UTF8.GetBytes(xmlText).Length;
            Logger.Instance.LogDebug(string.Format("FinalLength: {0}", finalLength));
            if (MAX_MESSAGE_SIZE >= finalLength)
                return count;
            else
                throw new Exception(string.Format("Calculation failed. OneEntry: {0}, TwoEntries: {1}, Difference: {2}, FreeBytes: {3}, Count: {4}, FinalLength: {5}", oneEntry, twoEntries, difference, freeBytes, count, finalLength));
        }

        private static void SendMessageParts(byte[] byteData, MessageSide side, ulong receiver = 0)
        {
            Logger.Instance.LogDebug(string.Format("SendMessageParts {0} {1} {2}", byteData.Length, side, receiver));

            var byteList = byteData.ToList();

            while (byteList.Count > 0)
            {
                // we create an empty message part
                var messagePart = new MessageIncomingMessageParts()
                {
                    Side = side,
                    SenderSteamId = side == MessageSide.ServerSide ? MyAPIGateway.Session.Player.SteamUserId : 0,
                    LastPart = false,
                };

                try
                {
                    // let's check how much we could store in the message
                    int freeBytes = GetFreeByteElementCount(messagePart);

                    int count = freeBytes;

                    // we check if that might be the last message
                    if (freeBytes > byteList.Count)
                    {
                        messagePart.LastPart = true;

                        // since we changed LastPart, we should make sure that we are still able to send all the stuff
                        if (GetFreeByteElementCount(messagePart) > byteList.Count)
                        {
                            count = byteList.Count;
                        }
                        else
                            throw new Exception("Failed to send message parts. The leftover could not be sent!");
                    }

                    // fill the message with content
                    messagePart.Content = byteList.GetRange(0, count).ToArray();
                    var xmlPart = MyAPIGateway.Utilities.SerializeToXML<MessageContainer>(new MessageContainer() { Content = messagePart });
                    var bytes = System.Text.Encoding.UTF8.GetBytes(xmlPart);

                    // and finally send the message
                    switch (side)
                    {
                        case MessageSide.ClientSide:
                            if (MyAPIGateway.Multiplayer.SendMessageTo(MessageId, bytes, receiver))
                                byteList.RemoveRange(0, count);
                            else
                                throw new Exception("Failed to send message parts to client.");
                            break;
                        case MessageSide.ServerSide:
                            if (MyAPIGateway.Multiplayer.SendMessageToServer(MessageId, bytes))
                                byteList.RemoveRange(0, count);
                            else
                                throw new Exception("Failed to send message parts to server.");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                    return;
                }
            }
        }
        #endregion

        public static void HandleMessage(byte[] rawData)
        {
            try
            {
                var data = System.Text.Encoding.UTF8.GetString(rawData);
                var message = MyAPIGateway.Utilities.SerializeFromXML<MessageContainer>(data);

                Logger.Instance.LogDebug("HandleMessage()");
                if (message != null && message.Content != null)
                {
                    message.Content.InvokeProcessing();
                }
                return;
            }
            catch (Exception e)
            {
                // Don't warn the user of an exception, this can happen if two mods with the same message id receive an unknown message
                Logger.Instance.LogMessage(string.Format("Processing message exception. Exception: {0}", e.ToString()));
                //Logger.Instance.LogException(e);
            }

        }
    }

    /// <summary>
    /// Resets the gates
    /// </summary>
    [ProtoContract]
    public class MessageReset : MessageBase
    {
        [ProtoMember(1)]
        public long Gate;

        [ProtoMember(2)]
        public bool Force;

        public override void ProcessClient()
        {
            IMyEntity gate;
            if (MyAPIGateway.Entities.TryGetEntityById(Gate, out gate))
            {
                gate.GameLogic.GetAs<Stargate>().ResetGate(Force);
            }
        }

        public override void ProcessServer()
        {
            // do nothing
        }
    }

    /// <summary>
    /// Resets the gates
    /// </summary>
    [ProtoContract]
    public class MessageResetDHD : MessageBase
    {
        [ProtoMember(1)]
        public long DHD;

        public override void ProcessClient()
        {
            IMyEntity dhd;
            if (MyAPIGateway.Entities.TryGetEntityById(DHD, out dhd))
            {
                dhd.GameLogic.GetAs<DHD>().ResetDHD(true);
            }
        }

        public override void ProcessServer()
        {
            IMyEntity dhd;
            if (MyAPIGateway.Entities.TryGetEntityById(DHD, out dhd))
            {
                dhd.GameLogic.GetAs<DHD>().ResetDHD(true);
            }
            MessageUtils.SendMessageToAllPlayers(this);
        }
    }

    [ProtoContract]
    public abstract class MessageDHDEvent : MessageBase
    {
        [ProtoMember(1)]
        public long DHD;

        /* Temporary for working around creation bug */
        [ProtoMember(2)]
        public long Grid;

        [ProtoMember(3)]
        public SerializableVector3I DHDPosition;

        protected IMyEntity GetDHD()
        {
            IMyEntity dhd;
            MyAPIGateway.Entities.TryGetEntityById(DHD, out dhd);

            if (dhd == null)
            {
                IMyEntity grid;
                if (MyAPIGateway.Entities.TryGetEntityById(Grid, out grid))
                {
                    var block = (grid as IMyCubeGrid).GetCubeBlock(DHDPosition);
                    if (block.FatBlock is IMyButtonPanel)
                        dhd = block.FatBlock;
                }

            }
            return dhd;
        }
    }

    [ProtoContract]
    public class MessageQuickDial : MessageDHDEvent
    {
        [ProtoMember(1)]
        public string Address;

        public override void ProcessClient()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                dhd.GameLogic.GetAs<DHD>()?.QuickDial_Client(Address);
            }
        }

        public override void ProcessServer()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                if (dhd?.GameLogic.GetAs<DHD>()?.QuickDial_Server() == true)
                {
                    var gate = DoorDHDExtensions.GetNamedGate(dhd as IMyTerminalBlock, dhd.GameLogic.GetAs<DHD>()?.Data?.Destination);
                    if (gate != null)
                        Address = gate?.GameLogic.GetAs<Stargate>()?.Address;

                    Logger.Instance.LogDebug("Address: " + Address);
                    Logger.Instance.LogDebug("Message validated, sending to clients");
                    if (MyAPIGateway.Session.Player != null)
                        dhd.GameLogic.GetAs<DHD>()?.QuickDial_Client(Address);
                    MessageUtils.SendMessageToAllPlayers(this);
                }
            }
        }
    }

    [ProtoContract]
    public class MessageSetQuickDestination : MessageDHDEvent
    {
        [ProtoMember(1)]
        public string Address;

        public override void ProcessClient()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                dhd.GameLogic.GetAs<DHD>().Data.Destination = Address;
            }
        }

        public override void ProcessServer()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                dhd.GameLogic.GetAs<DHD>().Data.Destination = Address;
                MessageUtils.SendMessageToAllPlayers(this);
            }
        }
    }

    [ProtoContract]
    public class MessageButtonPress : MessageDHDEvent
    {
        [ProtoMember(1)]
        public int ButtonIndex;

        public override void ProcessClient()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                dhd?.GameLogic.GetAs<DHD>()?.ButtonPressed_Client(ButtonIndex);
            }
        }

        public override void ProcessServer()
        {
            IMyEntity dhd;

            if ((dhd = GetDHD()) != null)
            {
                Logger.Instance.LogDebug("DHD: " + (dhd as IMyTerminalBlock).CustomName);
                if (dhd?.GameLogic.GetAs<DHD>()?.ButtonPressed_Server(ButtonIndex) == true)
                {
                    Logger.Instance.LogDebug("Message validated, sending to clients");
                    if (MyAPIGateway.Session.Player != null)
                        dhd.GameLogic.GetAs<DHD>()?.ButtonPressed_Client(ButtonIndex);
                    MessageUtils.SendMessageToAllPlayers(this);
                }
            }
            else
            {
                Logger.Instance.LogDebug("No DHD: " + DHD);
            }
        }

    }

    [ProtoContract]
    public class MessageDial : MessageDHDEvent /* TODO: change back to MessageBase after Button fix */
    {
        [ProtoMember(1)]
        public bool Incoming;

        [ProtoMember(2)]
        public long Gate;

        [ProtoMember(3)]
        public long RemoteGate;

        [ProtoMember(4)]
        public new long DHD;

        [ProtoMember(5)]
        public long ActivatingPlayer;

        void DoWork()
        {
            IMyEntity gate;
            IMyEntity dhd;
            IMyEntity remote;
            List<IMyPlayer> players = new List<IMyPlayer>();
            dhd = GetDHD();
            //MyAPIGateway.Entities.TryGetEntityById(DHD, out dhd);
            MyAPIGateway.Entities.TryGetEntityById(RemoteGate, out remote);

            if (MyAPIGateway.Entities.TryGetEntityById(Gate, out gate))
            {
                gate.GameLogic.GetAs<Stargate>().RemoteGate = remote as IMyTerminalBlock;

                if (Incoming)
                    gate.GameLogic.GetAs<Stargate>()?.DialIncoming((IMyTerminalBlock)remote, true);
                else
                    gate.GameLogic.GetAs<Stargate>()?.DialGate((IMyFunctionalBlock)dhd, true, player: ActivatingPlayer);
            }
        }

        public override void ProcessClient()
        {
            DoWork();
        }

        public override void ProcessServer()
        {
            DoWork();
            MessageUtils.SendMessageToAllPlayers(this);
        }
    }

    [ProtoContract]
    public abstract class MessageEntityAction : MessageBase
    {
        [ProtoMember(1)]
        public long Entity;
    }

    [ProtoContract]
    public class MessageIris : MessageEntityAction
    {
        [ProtoMember(1)]
        public bool Activate;

        public override void ProcessClient()
        {
            DoWork();
        }
        public override void ProcessServer()
        {
            DoWork();
            MessageUtils.SendMessageToAllPlayers(this);
        }

        private void DoWork()
        {
            IMyEntity gate = null;
            if (MyAPIGateway.Entities.TryGetEntityById(Entity, out gate))
            {
                if ((gate as IMyTerminalBlock)?.GetGateType() == GateType.Stargate)
                    (gate as IMyTerminalBlock).GameLogic.GetAs<Stargate>().Data.IrisActive = Activate;
            }
        }
    }

    [ProtoContract]
    public class MessageAlwaysAnimate : MessageEntityAction
    {
        [ProtoMember(1)]
        public bool AlwaysAnimate;

        public override void ProcessClient()
        {
            DoWork();
        }

        public override void ProcessServer()
        {
            DoWork();
            MessageUtils.SendMessageToAllPlayers(this);
        }

        private void DoWork()
        {
            IMyEntity gate = null;
            if (MyAPIGateway.Entities.TryGetEntityById(Entity, out gate))
            {
                Stargate sg = (gate as IMyTerminalBlock).GameLogic.GetAs<Stargate>();
                if (sg != null)
                    sg.Data.AlwaysAnimateLongDial = AlwaysAnimate;
            }
        }
    }

    [ProtoContract]
    public class MessagePlaySound : MessageEntityAction
    {
        [ProtoMember(1)]
        public string SoundName;
        [ProtoMember(2)]
        public bool Force;
        //[ProtoMember(3)]
        //public bool StopPrevious;

        public override void ProcessClient()
        {
            IMyEntity soundSource = null;
            if (MyAPIGateway.Entities.TryGetEntityById(Entity, out soundSource))
            {
                if ((soundSource as IMyTerminalBlock)?.GetGateType() != GateType.Invalid)
                    (soundSource as IMyTerminalBlock).GameLogic.GetAs<Stargate>()?.PlaySound(SoundName, Force);
            }
        }
        public override void ProcessServer()
        {
            // do nothing, sent from server
        }
    }

    [ProtoContract]
    public class MessageMove : MessageBase
    {
        [ProtoMember(1)]
        public long sourceGate;
        [ProtoMember(2)]
        public long destinationGate;
        [ProtoMember(3)]
        public List<Tuple2<long, VRageMath.MatrixD, VRageMath.Vector3D>> positions;  // List of positions for entities
        [ProtoMember(4)]
        public bool TurnOffDampers = false;

        public override void ProcessClient()
        {
            DoMove();
        }

        public override void ProcessServer()
        {
            DoMove();
        }

        private void DoMove()
        {
            IMyEntity sourceGateEntity = null;
            MyAPIGateway.Entities.TryGetEntityById(sourceGate, out sourceGateEntity);
            IMyEntity destinationGateEntity = null;
            MyAPIGateway.Entities.TryGetEntityById(destinationGate, out destinationGateEntity);

            Logger.Instance.LogAssert(sourceGateEntity != null, "Source gate is null! id: " + sourceGate.ToString());
            Logger.Instance.LogAssert(destinationGateEntity != null, "Destination gate is null! id: " + destinationGate.ToString());

            foreach (var objToMove in positions)
            {
                IMyEntity entity = null;
                MyAPIGateway.Entities.TryGetEntityById(objToMove.Item1, out entity);

                if (entity != null)
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        var glgate = destinationGateEntity.GameLogic.GetAs<Stargate>();
                        glgate.AddSafeEntity(entity);
                    }
                    Logger.Instance.LogDebug("Moving entity: " + entity.GetType());
                    VRageMath.BoundingBoxD aggregatebox = sourceGateEntity.WorldAABB;
                    var box = entity.PositionComp.WorldAABB;
                    aggregatebox.Include(ref box);
                    MyAPIGateway.Physics.EnsurePhysicsSpace(aggregatebox);
                    entity.PositionComp.SetWorldMatrix(objToMove.Item2);
                    var relVelocity = objToMove.Item3;
                    var oldvelocity = VRageMath.Vector3D.Zero;

                    if (entity.Physics != null)
                    {
                        oldvelocity = entity.Physics.LinearVelocity;
                        entity.Physics.LinearVelocity = relVelocity;
                    }

                    // Keep moving forward until we no longer collide with the gate
                    if (MyAPIGateway.Session.IsServer)
                        Stargate.BumpEntityForward(entity, destinationGateEntity, relVelocity);

                    if (entity is IMyControllableEntity && MyAPIGateway.Session.Player != null && MyAPIGateway.Players.GetPlayerControllingEntity(entity) == MyAPIGateway.Session.Player)
                    {
                        // Turn off dampers if we are going to a moving grid
                        if (MyAPIGateway.Session.Player.Controller.ControlledEntity.EnabledDamping && TurnOffDampers)
                        {
                            MyAPIGateway.Session.Player.Controller.ControlledEntity.SwitchDamping();
                        }
                        // Workaround for bug where jetpack still uses planet consumption when teleported to space
                        if (MyAPIGateway.Session.Player.Controller.ControlledEntity.EnabledThrusts)
                        {
                            MyAPIGateway.Session.Player.Controller.ControlledEntity.SwitchThrusts();
                            MyAPIGateway.Session.Player.Controller.ControlledEntity.SwitchThrusts();
                        }
                    }

                    Logger.Instance.LogMessage(string.Format("updated {0} to: {1:F0}, {2:F0}, {3:F0}", entity.DisplayName, objToMove.Item2.Translation.X, objToMove.Item2.Translation.Y, objToMove.Item2.Translation.Z));
                }
            }
        }
    }

    [ProtoContract]
    public class MessageSave : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            StargateAdmin.SaveConfig();
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Config saved" });
        }
    }

    [ProtoContract]
    public class MessageUpgrade : MessageBase
    {
        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            StargateMissionComponent.Instance.Upgrade = true;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat()
            {
                Sender = Globals.ModName,
                MessageText = "Upgrading legacy blocks\nReplace small grid DHDs manually.\nSave and reload the world if there are DHD problems."
            });
        }
    }

    [ProtoContract]
    public class MessageChat : MessageBase
    {
        public string Sender;
        public string MessageText;

        public override void ProcessClient()
        {
            MyAPIGateway.Utilities.ShowMessage(Sender, MessageText);
        }

        public override void ProcessServer()
        {
            // None
        }
    }

    [ProtoContract]
    public class MessageAntenna : MessageBase
    {
        [ProtoMember(1)]
        public OnOffTriState AntennaMode;
        public bool Force;

        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            StargateAdmin.Configuration.AntennaMode = AntennaMode;
            StargateAdmin.Configuration.AntennaForced = Force;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Antenna mode " + (Force ? "forced " : "") + StargateAdmin.Configuration.AntennaMode.ToString() });
        }
    }

    [ProtoContract]
    public class MessageBuildable : MessageBase
    {
        [ProtoMember(1)]
        public bool Buildable;

        public override void ProcessClient()
        {
            StargateAdmin.Configuration.Buildable = Buildable;
        }

        public override void ProcessServer()
        {
            StargateAdmin.Configuration.Buildable = Buildable;
            MessageUtils.SendMessageToAllPlayers(this);
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Buildable gates " + StargateAdmin.Configuration.Buildable.ToString() });
        }
    }

    [ProtoContract]
    public class MessageToggleItems : MessageBuildable
    {
        [ProtoMember(1)]
        public BlockCategoryMenuToggle Group;

        public override void ProcessClient()
        {
            StargateAdmin.ToggleGMenu(Group, Buildable);
        }

        public override void ProcessServer()
        {
            StargateAdmin.ToggleGMenu(Group, Buildable);

            MessageUtils.SendMessageToAllPlayers(this);
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = Group.ToString() + (Buildable ? " shown" : " hidden") });
        }
    }

    [ProtoContract]
    public class MessageIndestructible : MessageBase
    {
        [ProtoMember(1)]
        public bool Destructible;

        public override void ProcessClient()
        {
            // never processed here
        }

        public override void ProcessServer()
        {
            StargateAdmin.Configuration.Destructible = Destructible;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Gate damage " + StargateAdmin.Configuration.Destructible.ToString() });
        }
    }

    [ProtoContract]
    public class MessageHardcore : MessageBase
    {
        [ProtoMember(1)]
        public bool Hardcore;

        public override void ProcessClient()
        {
            StargateAdmin.Configuration.Hardcore = Hardcore;
        }

        public override void ProcessServer()
        {
            StargateAdmin.Configuration.Hardcore = Hardcore;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Hardcore mode " + StargateAdmin.Configuration.Hardcore });
        }
    }

    [ProtoContract]
    public class MessageVortex : MessageBase
    {
        public enum MessageType { Visibility, Damage };
        [ProtoMember(1)]
        public MessageType Type = MessageType.Visibility;
        [ProtoMember(2)]
        public bool Flag;

        public override void ProcessClient()
        {
            if (Type == MessageType.Visibility)
            {
                StargateAdmin.Configuration.VortexVisible = Flag;
                StargateAdmin.Configuration.VortexDamage = Flag;
            }
            else
            {
                StargateAdmin.Configuration.VortexDamage = Flag;
            }
        }

        public override void ProcessServer()
        {
            if (Type == MessageType.Visibility)
            {
                // This toggles both visibility, and damage. Damage can also be controlled separately.
                StargateAdmin.Configuration.VortexVisible = Flag;
                StargateAdmin.Configuration.VortexDamage = Flag;
                MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Vortex " + StargateAdmin.Configuration.VortexVisible.ToString() });
            }
            else
            {
                // This toggles both visibility, and damage. Damage can also be controlled separately.
                StargateAdmin.Configuration.VortexDamage = Flag;
                MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Vortex damage " + StargateAdmin.Configuration.VortexDamage.ToString() });
            }
        }
    }

    [ProtoContract]
    public class MessageEventHorizon : MessageBase
    {
        [ProtoMember(1)]
        public long Parent = 0;
        [ProtoMember(2)]
        public string SubpartName;
        [ProtoMember(3)]
        public bool Remove;
        [ProtoMember(4)]
        public long SubgridId;

        public override void ProcessClient()
        {
            IMyEntity parent = null;
            IMyEntity subgrid = null;
            MyAPIGateway.Entities.TryGetEntityById(Parent, out parent);

            if (parent != null)
            {
                parent.GetSubpart(SubpartName).Render.Visible = !Remove;

                Logger.Instance.LogMessage("subgrid: " + SubgridId);
                if (MyAPIGateway.Entities.TryGetEntityById(SubgridId, out subgrid))
                {
                    Logger.Instance.LogMessage("subpart found");
                    if (Remove)
                        parent.GameLogic.GetAs<Stargate>().RemoveEventHorizon(SubpartName.Contains("EventHorizon"));
                    else
                        parent.Hierarchy.AddChild(subgrid, true);
                }
            }
        }

        public override void ProcessServer()
        {
            // Do nothing
        }
    }

    [ProtoContract]
    public class MessageGateEvent : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;
        [ProtoMember(2)]
        public string Address;
        [ProtoMember(3)]
        public string GateName;
        [ProtoMember(4)]
        public string GridName;
        [ProtoMember(5)]
        public MyRelationsBetweenPlayerAndBlock Relation;
        [ProtoMember(6)]
        public bool Remove;
        [ProtoMember(7)]
        public GateType GateType;
        [ProtoMember(8)]
        public long OwnerId;
        [ProtoMember(9)]
        public MyOwnershipShareModeEnum ShareMode;

        public override void ProcessClient()
        {
            DoWork();
        }

        public override void ProcessServer()
        {
            DoWork();
        }

        private void DoWork()
        {
            StargateMissionComponent.Instance.KnownGates.Remove(EntityId);
            if (!Remove)
            {
                StargateMissionComponent.Instance.KnownGates.Add(EntityId, new KnownGate()
                {
                    Address = Address,
                    GridName = GridName,
                    Name = GateName,
                    GateType = GateType,
                    OwnerId = OwnerId,
                    ShareMode = ShareMode,
                    Relation = Relation
                });
                Logger.Instance.LogMessage(string.Format("Added known gate: {0}; {1}", GateName, Address));
            }
            else
            {
                Logger.Instance.LogMessage(string.Format("Removed known gate: {0}", EntityId));
            }
        }
    }

    [ProtoContract]
    public class MessageConfig : MessageBase
    {
        public StargateConfig Configuration;

        public override void ProcessClient()
        {
            StargateAdmin.SetConfig(Configuration);
        }

        public override void ProcessServer()
        {
        }
    }

    [ProtoContract]
    public class MessageClientConnected : MessageBase
    {
        public override void ProcessClient()
        {
        }

        public override void ProcessServer()
        {
            Logger.Instance.LogMessage(string.Format("Sending gate list to new client: {0}", SenderSteamId));
            // Send new clients the configuration
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageConfig() { Configuration = StargateAdmin.Configuration });
            foreach(var gate in StargateMissionComponent.Instance.KnownGates)
            {
                MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageGateEvent()
                {
                    Address = gate.Value.Address,
                    EntityId = gate.Key,
                    GateName = gate.Value.Name,
                    GridName = gate.Value.GridName,
                    GateType = gate.Value.GateType,
                    OwnerId = gate.Value.OwnerId,
                    ShareMode = gate.Value.ShareMode,
                    Remove = false
                });
            }
        }
    }

    [ProtoContract]
    public abstract class MessageEntityValue : MessageBase
    {
        [ProtoMember(1)]
        public long EntityId;
    }

    [ProtoContract]
    public abstract class MessageEntityValueBoolean : MessageEntityValue
    {
        [ProtoMember(1)]
        public bool Value;
    }

    [ProtoContract]
    public class MessageAutoClear : MessageEntityValueBoolean
    {
        public override void ProcessClient()
        {
            DoWork();
        }

        public override void ProcessServer()
        {
            DoWork();
            MessageUtils.SendMessageToAllPlayers(this);
        }

        private void DoWork()
        {
            IMyEntity block;
            if (MyAPIGateway.Entities.TryGetEntityById(EntityId, out block))
            {
                (block as IMyTerminalBlock).GameLogic.GetAs<DHD>().Data.AutoClear = Value;
            }
        }
    }

    [ProtoContract]
    public class MessageTeleportGridsAllowed : MessageBase
    {
        [ProtoMember(1)]
        public bool AllowGridTeleport;

        public override void ProcessClient()
        {
            // do nothing
        }

        public override void ProcessServer()
        {
            StargateAdmin.Configuration.TeleportGrids = AllowGridTeleport;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Allow grid teleport " + StargateAdmin.Configuration.TeleportGrids });
        }
    }

    [ProtoContract]
    public class MessageDebug : MessageBase
    {
        [ProtoMember(1)]
        public bool DebugMode;

        public override void ProcessClient()
        {
            EnableDebug();
        }

        public override void ProcessServer()
        {
            EnableDebug();
        }

        private void EnableDebug()
        {
            StargateAdmin.Configuration.Debug = DebugMode;
            Logger.Instance.Debug = DebugMode;
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Debug mode " + StargateAdmin.Configuration.Debug.ToString() });
        }
    }

    [ProtoContract]
    public abstract class MessageDoubleValue : MessageBase
    {
        [ProtoMember(1)]
        public double Value;
    }

    [ProtoContract]
    public class MessageGateInfluence : MessageDoubleValue
    {
        public override void ProcessClient()
        {
            DoWork();
        }

        public override void ProcessServer()
        {
            DoWork();
            MessageUtils.SendMessageToAllPlayers(this);
            MessageUtils.SendMessageToPlayer(SenderSteamId, new MessageChat() { Sender = Globals.ModName, MessageText = "Gate Influence " + StargateAdmin.Configuration.GateInfluenceRadius.ToString() });
        }

        private void DoWork()
        {
            StargateAdmin.Configuration.GateInfluenceRadius = Value;
        }
    }

    #region Message Splitting
    [ProtoContract]
    public class MessageIncomingMessageParts : MessageBase
    {
        [ProtoMember(1)]
        public byte[] Content;

        [ProtoMember(2)]
        public bool LastPart;

        public override void ProcessClient()
        {
            MessageUtils.Client_MessageCache.AddRange(Content.ToList());

            if (LastPart)
            {
                MessageUtils.HandleMessage(MessageUtils.Client_MessageCache.ToArray());
                MessageUtils.Client_MessageCache.Clear();
            }
        }

        public override void ProcessServer()
        {
            if (MessageUtils.Server_MessageCache.ContainsKey(SenderSteamId))
                MessageUtils.Server_MessageCache[SenderSteamId].AddRange(Content.ToList());
            else
                MessageUtils.Server_MessageCache.Add(SenderSteamId, Content.ToList());

            if (LastPart)
            {
                MessageUtils.HandleMessage(MessageUtils.Server_MessageCache[SenderSteamId].ToArray());
                MessageUtils.Server_MessageCache[SenderSteamId].Clear();
            }
        }

    }
    #endregion

    /// <summary>
    /// This is a base class for all messages
    /// </summary>
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [XmlInclude(typeof(MessageIncomingMessageParts))]
    [XmlInclude(typeof(MessageAntenna))]
    [XmlInclude(typeof(MessageSave))]
    [XmlInclude(typeof(MessageMove))]
    [XmlInclude(typeof(MessageIndestructible))]
    [XmlInclude(typeof(MessageHardcore))]
    [XmlInclude(typeof(MessageVortex))]
    [XmlInclude(typeof(MessageDebug))]
    [XmlInclude(typeof(MessageBuildable))]
    [XmlInclude(typeof(MessageToggleItems))]
    [XmlInclude(typeof(MessageTeleportGridsAllowed))]
    [XmlInclude(typeof(MessageChat))]
    [XmlInclude(typeof(MessageConfig))]
    [XmlInclude(typeof(MessageClientConnected))]
    [XmlInclude(typeof(MessageEventHorizon))]
    [XmlInclude(typeof(MessageReset))]
    [XmlInclude(typeof(MessageDial))]
    [XmlInclude(typeof(MessageButtonPress))]
    [XmlInclude(typeof(MessageResetDHD))]
    [XmlInclude(typeof(MessageGateEvent))]
    [XmlInclude(typeof(MessageQuickDial))]
    [XmlInclude(typeof(MessageSetQuickDestination))]
    [XmlInclude(typeof(MessageEntityValue))]
    [XmlInclude(typeof(MessageEntityValueBoolean))]
    [XmlInclude(typeof(MessageAutoClear))]
    [XmlInclude(typeof(MessageUpgrade))]
    [XmlInclude(typeof(MessageDoubleValue))]
    [XmlInclude(typeof(MessageGateInfluence))]
    [XmlInclude(typeof(MessageEntityAction))]
    [XmlInclude(typeof(MessagePlaySound))]
    [XmlInclude(typeof(MessageIris))]
    [XmlInclude(typeof(MessageAlwaysAnimate))]

    [ProtoContract]
    public abstract class MessageBase
    {
        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(2)]
        public MessageSide Side = MessageSide.ClientSide;

        /// <summary>
        /// Name of mod. Used to determine if message belongs to us.
        /// </summary>
        [ProtoMember(3)]
        public string ModName = Globals.ModName;

        /// <summary>
        /// Send updated options to all clients
        /// </summary>
        [ProtoMember(4)]
        public bool ResyncSettings = false;

        /*
        [ProtoAfterDeserialization]
        void InvokeProcessing() // is not invoked after deserialization from xml
        {
            Logger.Debug("START - Processing");
            switch (Side)
            {
                case MessageSide.ClientSide:
                    ProcessClient();
                    break;
                case MessageSide.ServerSide:
                    ProcessServer();
                    break;
            }
            Logger.Debug("END - Processing");
        }
        */

        public void InvokeProcessing()
        {
            if (ModName != Globals.ModName)
            {
                Logger.Instance.LogDebug("Message came from another Stargate mod (" + ModName + "), ignored.");
                return;
            }

            switch (Side)
            {
                case MessageSide.ClientSide:
                    InvokeClientProcessing();
                    break;
                case MessageSide.ServerSide:
                    InvokeServerProcessing();
                    break;
            }
        }

        private void InvokeClientProcessing()
        {
            Logger.Instance.LogDebug(string.Format("START - Processing [Client] {0}", this.GetType().Name));
            try
            {
                ProcessClient();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
            Logger.Instance.LogDebug(string.Format("END - Processing [Client] {0}", this.GetType().Name));
        }

        private void InvokeServerProcessing()
        {
            Logger.Instance.LogDebug(string.Format("START - Processing [Server] {0}", this.GetType().Name));

            try
            {
                ProcessServer();

                // Sync config if required
                if (ResyncSettings)
                {
                    Logger.Instance.LogDebug("Sending config update to clients");
                    MessageUtils.SendMessageToAllPlayers(new MessageConfig() { Configuration = StargateAdmin.Configuration });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }

            Logger.Instance.LogDebug(string.Format("END - Processing [Server] {0}", this.GetType().Name));
        }

        public abstract void ProcessClient();
        public abstract void ProcessServer();
    }
}
// vim: tabstop=4 expandtab shiftwidth=4 nobackup
