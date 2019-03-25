/*
 * Script is Copyright © 2014-2015, Phoenix
 * Released under CC BY-SA 4.0 license
 **/
using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using Draygo.API;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.ModAPI.Weapons;
using System.Linq;
using Sandbox.Game.EntityComponents;

namespace Phoenix.Stargate
{
    using Extensions;
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Gui;
    using Sandbox.Game.Localization;
    using Sandbox.ModAPI.Interfaces;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using SpaceEngineers.Game.ModAPI;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Xml.Serialization;
    using VRage.Game.Entity;
    using VRage.Game.ObjectBuilders.ComponentSystem;
    using VRage.Input;
    using VRage.Serialization;
    using VRage.Utils;

    public struct KnownGate
    {
        public string Name;
        public string Address;
        public string GridName;
        public GateType GateType;
        public MyRelationsBetweenPlayerAndBlock Relation;
        public long OwnerId;
        public MyOwnershipShareModeEnum ShareMode;
    }

    /// <summary>
    /// This implements the heart of the DHD behavior, as well as cleans up old event horizons.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Globals.Priority)]
    class StargateMissionComponent : MySessionComponentBase
    {
        public static bool ModInitialized { get; private set; }
        private bool m_init = false;
        private long m_counter = 0;
        private HashidsNet.Hashids m_gateHasher;
        private HashidsNet.Hashids m_gateHasherAlternate;
        //private MyEntity3DSoundEmitter m_genericEmitter;

        public override string ToString()
        {
            return this.GetType().FullName;
        }

        public HashidsNet.Hashids GateAddressHasher
        {
            get
            {
                if (m_gateHasher == null)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByTag("SPRT");

                    if (faction != null)
                        m_gateHasher = new HashidsNet.Hashids(faction.FactionId.ToString(), 24, Constants.HashAlphabet);
                    else
                        Logger.Instance.LogMessage("faction null");
                }
                return m_gateHasher;
            }
        }

        /// <summary>
        /// Alternate gate hash, for when there are not enough digits (lots of repeated chars)
        /// </summary>
        public HashidsNet.Hashids GateAddressHasherAlternate
        {
            get
            {
                if (m_gateHasherAlternate == null)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionByName("Space Pirates");

                    if (faction != null)
                        m_gateHasherAlternate = new HashidsNet.Hashids(faction.FactionId.ToString(), 64, Constants.HashAlphabet);
                }
                return m_gateHasherAlternate;
            }
        }

        private static StargateMissionComponent m_instance;
        public static StargateMissionComponent Instance { get { return m_instance; } }
        private Dictionary<long, KnownGate> m_knownGates = new Dictionary<long, KnownGate>();
        public Dictionary<long, KnownGate> KnownGates { get { return m_knownGates; } }
        HUDTextNI.SpaceMessage m_frontHelpMessage;
        IMyBlockPlacerBase m_cubePlacer = null;
        private bool m_upgrade = false;
        public bool Upgrade
        {
            get { return m_upgrade; }
            set
            {
                m_upgrade = value;
                if (value)
                    UpgradeBlocks();
            }
        }

        static StargateMissionComponent()
        {
            ModInitialized = false;
        }

        public StargateMissionComponent()
        {
            m_instance = this;
        }

        /// <summary>
        /// Upgrades legacy blocks to new types
        /// </summary>
        private void UpgradeBlocks()
        {
            var grids = new HashSet<IMyEntity>();
            var blocks = new List<IMySlimBlock>();
            MyAPIGateway.Entities.GetEntities(grids, e => (e as IMyCubeGrid)?.GridSizeEnum == MyCubeSize.Large);

            foreach (IMyCubeGrid grid in grids)
            {
                blocks.Clear();
                grid.GetBlocks(blocks, e => (e.FatBlock as IMyDoor)?.BlockDefinition.SubtypeId.StartsWith("DHD") == true);
                foreach (var block in blocks)
                    UpgradeDHD(block.FatBlock);
            }
        }

        private void HookLegacyBlocks()
        {
            var grids = new HashSet<IMyEntity>();
            var blocks = new List<IMySlimBlock>();
            MyAPIGateway.Entities.GetEntities(grids, e => e is IMyCubeGrid);

            foreach (IMyCubeGrid grid in grids)
            {
                blocks.Clear();
                grid.GetBlocks(blocks, e => (e.FatBlock as IMyDoor)?.BlockDefinition.SubtypeId.StartsWith("DHD") == true);
                foreach (var block in blocks)
                    (block.FatBlock as IMyDoor).AppendingCustomInfo += DHD_AppendingCustomInfo;
            }
        }

        private void DHD_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2)
        {
            arg2.Append("Legacy DHD\n");
            arg2.Append("Enter chat command to upgrade:\n");
            arg2.Append(string.Format("/{0} upgrade", Globals.ModName));
        }

        public void UpgradeDHD(IMyEntity Entity)
        {
            if (!StargateMissionComponent.Instance?.Upgrade == true)
                return;

            if (!Globals.ModEnabled)
                return;

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                var rotate = false;
                var block = Entity as IMyDoor;
                var oldob = block.GetObjectBuilderCubeBlock(true) as MyObjectBuilder_Door;
                if (oldob == null)
                    return;

                var newsubtype = "Phoenix_Stargate_DHD_SG1";

                switch (oldob.SubtypeName)
                {
                    case "DHD Generic":
                        newsubtype = "Phoenix_Stargate_DHD_Generic";
                        break;
                    case "DHD Generic Small":
                        newsubtype = "Phoenix_Stargate_DHD_Generic_Small";
                        break;
                    case "DHD SGU":
                        newsubtype = "Phoenix_Stargate_DHD_SGU_Computer";
                        break;
                    case "DHD Atlantis":
                        newsubtype = "Phoenix_Stargate_DHD_SGA_Computer";
                        break;
                    case "DHD Computer":
                        newsubtype = "Phoenix_Stargate_DHD_SG1_Computer";
                        rotate = true;
                        break;
                    case "DHD Computer Small":
                        newsubtype = "Phoenix_Stargate_DHD_SG1_Computer_Small";
                        break;
                }

                var def = ((Entity as IMyCubeBlock)?.SlimBlock as IMySlimBlock)?.BlockDefinition as MyDoorDefinition;
                //var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ButtonPanel>(newsubtype);
                var ob = new MyObjectBuilder_ButtonPanel() { SubtypeName = newsubtype };
                ob.Toolbar = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Toolbar>();
                ob.Toolbar.ToolbarType = MyToolbarType.Character;
                ob.CustomName = string.Format("{0} [Old: {1}]", def?.DisplayNameText, oldob.CustomName);
                //ob.CustomName = oldob.CustomName;
                if (ob.CustomButtonNames != null)
                {
                    string name;
                    if (ob.CustomButtonNames.Dictionary.TryGetValue(0, out name) && name == "Dial")
                    {
                        return;
                    }
                }
                ob.CustomButtonNames = new SerializableDictionary<int, string>();

                // Fill in buttons in object builder
                ob.CustomButtonNames[0] = "Dial";

                var newdef = MyDefinitionManager.Static.GetCubeBlockDefinition(ob) as MyButtonPanelDefinition;
                if (newdef?.ButtonCount == 40)
                    for (int x = 1; x < Constants.ButtonsToCharacters.Length; x++)
                        ob.CustomButtonNames[x] = Constants.ButtonsToCharacters[x].ToString();

                var grid = block.CubeGrid;


                ob.Min = new VRage.SerializableVector3I(0, 0, 0);
                ob.EntityId = 0;

                var gridbuilder = new MyObjectBuilder_CubeGrid()
                {
                    CreatePhysics = true,
                    GridSizeEnum = grid.GridSizeEnum,
                    PositionAndOrientation = new VRage.MyPositionAndOrientation(block.PositionComp.GetPosition(), rotate ? block.WorldMatrix.Right : block.WorldMatrix.Forward, block.WorldMatrix.Up)
                };
                gridbuilder.CubeBlocks.Add(ob);
                var list = new List<MyObjectBuilder_CubeGrid>();
                list.Add(gridbuilder);
                var position = block.Position;
                block.CubeGrid.RazeBlock(block.Position);

                (grid as MyCubeGrid).PasteBlocksToGrid(list, 0, false, true);
                var newblock = grid.GetCubeBlock(position);
                newblock?.FatBlock?.Components?.Get<MyUseObjectsComponentBase>()?.RecreatePhysics();
                newblock?.FatBlock?.ReloadDetectors();
                return;
            }
        }

        HashSet<IMyEntity> m_cachedEntities = new HashSet<IMyEntity>();
        // This is called during saving, but *before* the entities are saved
        // SaveData is called *after* the entities are saved
        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            if (MyAPIGateway.Entities == null)
                return base.GetObjectBuilder();

            MyAPIGateway.Entities.GetEntities(m_cachedEntities, b => b is IMyCubeGrid);
            foreach (IMyCubeGrid grid in m_cachedEntities)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, b => b?.FatBlock is IMyTerminalBlock && b?.FatBlock?.GameLogic?.GetAs<SerializableBlock>() != null);
                foreach (var block in blocks)
                {
                    (block as IMySlimBlock).FatBlock.GameLogic.GetAs<SerializableBlock>().SerializeData();
                }
            }
            m_cachedEntities.Clear();
            return base.GetObjectBuilder();
        }

        #region Update
        public override void HandleInput()
        {
            base.HandleInput();
            if (MyAPIGateway.Input == null)
                return;

            // If the player is trying to copy or make a blueprint, save the block data
            if (MyAPIGateway.Gui?.GetCurrentScreen == MyTerminalPageEnum.None &&
                MyAPIGateway.Session?.Player?.Controller?.ControlledEntity is IMyCharacter &&
                MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Control) && !MyAPIGateway.Input.IsAnyMousePressed())
            {
                var matrix = MyAPIGateway.Session.Player.Controller.ControlledEntity.GetHeadMatrix(true);
                IHitInfo hitinfo;
                MyAPIGateway.Physics.CastLongRay(matrix.Translation, matrix.Forward * 1000, out hitinfo, false);
                if (hitinfo?.HitEntity is IMyCubeGrid)
                {
                    BoundingSphereD reference = hitinfo.HitEntity.GetTopMostParent().WorldVolume;
                    var entites = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref reference);
                    foreach (var grid in entites)
                    {
                        if (!(grid is IMyCubeGrid))
                            continue;

                        var blocks = new List<IMySlimBlock>();
                        (grid as IMyCubeGrid).GetBlocks(blocks, b => b?.FatBlock is IMyTerminalBlock && b?.FatBlock?.GameLogic?.GetAs<SerializableBlock>() != null);
                        foreach (var block in blocks)
                        {
                            (block as IMySlimBlock).FatBlock.GameLogic.GetAs<SerializableBlock>().SerializeData();
                        }
                    }
                }
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (!Globals.ModEnabled)
                return;

            if (!m_init)
            {
                if (MyAPIGateway.Entities == null)
                    return;

                if (MyAPIGateway.Session == null)
                    return;

                if (MyAPIGateway.Multiplayer == null || (!MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE))
                    return;

                if (!MyAPIGateway.Session.IsServer && MyAPIGateway.Session.Player == null)
                    return;

                // First run code
                Init();

                if (MyAPIGateway.Session.Player == null)
                    return;

                if (!MyAPIGateway.Multiplayer.IsServer)
                    return;

                // Search for stale event horizons on load, and clear them
                Dictionary<long, IMyEntity> horizons = GetEventHorizonList();
                List<IMySlimBlock> gates = DoorDHDExtensions.GetGateList();
                Dictionary<long, IMyEntity> remainder = horizons;

                // Enumerate all gates and check if they have active event horizons
                foreach (var gate in gates)
                {
                    if (gate.FatBlock == null || !(gate.FatBlock.GameLogic.GetAs<Stargate>() is Stargate) || (gate.FatBlock.GameLogic.GetAs<Stargate>() as Stargate).EventHorizon == null)
                        continue;

                    long key = (gate.FatBlock.GameLogic.GetAs<Stargate>() as Stargate).EventHorizon.EntityId;

                    if (horizons.ContainsKey(key))
                        remainder.Remove(key);
                }

                if (remainder.Count > 0)
                    Logger.Instance.LogMessage("Closing stale gates");

                // If we have any remainders, delete them
                foreach (var horizon in remainder)
                {
                    if (horizon.Value != null)
                    {
                        MyAPIGateway.Entities.RemoveEntity(horizon.Value);
                        horizon.Value.Close();
                    }
                }
            }
            else
            {
                if (MyAPIGateway.Session.Player != null)
                {

                    var placer = (MyAPIGateway.CubeBuilder as MyCubeBuilder);
                    var def = placer?.ToolbarBlockDefinition;

                    if (placer != null && StargateAdmin.Instance?.HelpHUD != null && StargateAdmin.Instance.HelpHUD.Heartbeat &&
                        m_cubePlacer != null && def != null &&
                        def.Id.TypeId == typeof(MyObjectBuilder_TerminalBlock) &&
                        (def.Id.SubtypeName.StartsWith("Stargate") || def.Id.SubtypeName.StartsWith("Supergate")))
                    {
                        var cubesize = (float)(def.CubeSize == MyCubeSize.Large ? 2.5 : 0.5);

                        var aabb = placer.GetBuildBoundingBox();
                        Vector3D pos = aabb.Center;

                        var isSupergate = def.Id.SubtypeName.StartsWith("Supergate");
                        var forwardOffset = aabb.Orientation.Forward * -(isSupergate ? 15 : 1);
                        var leftOffset = (aabb.Orientation.Right * -(isSupergate ? -15 : 0.2f));
                        //var upOffset = -aabb.Orientation.Forward * (float)(isSupergate ? -(aabb.HalfExtent.Y - 10) : 2.4f);

                        m_frontHelpMessage = new HUDTextNI.SpaceMessage(m_cubePlacer.EntityId, 10, isSupergate ? 20 : 0.25,
                                pos + /*upOffset + */forwardOffset + leftOffset,
                                aabb.Orientation.Forward,
                                -aabb.Orientation.Right,
                                "<color=Green>^ Front ^",
                                orientation: HUDTextNI.TextOrientation.center);

                        StargateAdmin.Instance.HelpHUD.Send(m_frontHelpMessage);
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!Globals.ModEnabled)
                return;

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            // Emulate MyGameLogicComponent.UpdateAfterSimulation10
            if ((++m_counter % 10) == 0)
            {
            }
        }
        #endregion Updates

        public override void LoadData()
        {
            Globals.ModContext = ModContext;
            Logger.Instance.Init(Globals.ModName);
            Logger.Instance.LogDebug("StargateMissionComponent.LoadData()");
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
                Logger.Instance.Close();
            }
            catch (Exception ex) { Logger.Instance.LogException(ex); }
        }

        private void Init()
        {
            try
            {
                Logger.Instance.LogMessage("StargateMissionComponent.Init()");
                HookLegacyBlocks();

                if (MyAPIGateway.Utilities != null)
                {
                    if (!Utils.HasSeenUpdate())
                    {
                        MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Bug Fixes for address crash and emissives!");
                        MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Addresses may have changed!");
                        MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "See the Steam Workshop page for details.");
                        MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "This will NOT show again this update.");

                        Utils.WriteUpdate();
                    }

                    MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
                }
                if (!MyAPIGateway.Multiplayer.IsServer)
                    MessageUtils.SendMessageToServer(new MessageClientConnected());

                m_init = true;
                ModInitialized = true;

                StargateAdmin.CheckAndDisableMod(this);
                Logger.Instance.LogDebug("StargateMissionComponent.Init() complete");
            }
            catch (Exception ex) { Logger.Instance.LogException(ex); }
        }

        #region Events
        private void Entities_OnEntityAdd(IMyEntity obj)
        {
            if (obj is IMyBlockPlacerBase)
                m_cubePlacer = obj as IMyBlockPlacerBase;
        }

        private void Entities_OnEntityRemove(IMyEntity obj)
        {
            if (obj is IMyBlockPlacerBase)
            {
                m_cubePlacer = null;
            }
        }
        #endregion Events

        public static Dictionary<long, IMyEntity> GetEventHorizonList()
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
            Dictionary<long, IMyEntity> evtList = new Dictionary<long, IMyEntity>();

            if (MyAPIGateway.Entities != null)
                MyAPIGateway.Entities.GetEntities(hash, (x) => x is IMyCubeGrid &&
                                    !string.IsNullOrEmpty(x.DisplayName) &&
                                    (x.DisplayName.Contains("Event Horizon") || x.DisplayName.Contains("Iris")));

            foreach (var entity in hash)
            {
                Logger.Instance.LogDebug("Found Event Horizon: " + entity.DisplayName);
                evtList.Add(entity.EntityId, entity);
            }

            return evtList;
        }
    }

    // TODO: Reestablish connection on load
    // Use physics == null to detect projections and pasting, to block that
    public class StargateData
    {
        public StargateData() { }

        public StargateData(Stargate logic)
        {
            Parent = logic;
        }

        [XmlIgnore]
        public Stargate Parent { get; set; }

        /// <summary>
        /// Current active chevrons
        /// </summary>
        [XmlIgnore]
        public Chevron Chevron
        {
            get { return m_chevron; }
            set
            {
                m_chevron = value;
                Parent?.ToggleEmissives();
            }
        }

        [XmlIgnore]
        private Chevron m_chevron = Chevron.None;

        /// <summary>
        /// Current state of the gate
        /// </summary>
        [XmlIgnore]
        public GateState State
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    Parent.RaiseStateChanged();
                }
            }
        }
        [XmlIgnore]
        public GateState m_state = GateState.Idle;

        /// <summary>
        /// The gate is currently active as the remote end
        /// </summary>
        [XmlIgnore]
        public bool IsRemote = false;

        /// <summary>
        /// Destination gate entity ID
        /// </summary>
        [XmlIgnore]
        public long DestinationEntityId
        {
            get { return m_destinationEntityId; }
            set
            {
                m_destinationEntityId = value;
                IMyEntity remote;
                MyAPIGateway.Entities.TryGetEntityById(m_destinationEntityId, out remote);
                Parent.RemoteGate = remote as IMyTerminalBlock;
            }
        }
        [XmlIgnore]
        private long m_destinationEntityId = 0;

        /// <summary>
        /// True if the gate should do the full dialing animation even if using "quick dial" from DHD.
        /// </summary>
        public bool AlwaysAnimateLongDial = false;

        /// <summary>
        /// Time to auto close, in seconds
        /// </summary>
        public int AutoCloseTime = 60;

        /// <summary>
        /// Auto close the gate when any object enters
        /// </summary>
        public bool AutoCloseWithAll = false;

        /// <summary>
        /// enable antenna transmission
        /// </summary>
        public bool Antenna = true;

        /// <summary>
        /// Player that activated the gate
        /// </summary>
        [XmlIgnore]
        public long ActivatingPlayerId = 0;

        /// <summary>
        /// Iris/Shield is active
        /// </summary>
        public bool IrisActive
        {
            get { return m_irisActive; }
            set
            {
                m_irisActive = value;
                Parent?.ToggleIris(value);
            }
        }
        [XmlIgnore]
        private bool m_irisActive = false;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false,
        "Stargate S",
        "Stargate SO",
        "Stargate S Small",
        "Stargate SO Small",
        "Stargate M",
        "Stargate O",
        "Stargate A",
        "Stargate U",
        "Stargate O Small",
        "Stargate A Small",
        "Stargate U Small",
        "Supergate")]
    public class Stargate : SerializableBlock
    {
        public StargateData Data { get { return m_gateData; } }
        StargateData m_gateData;
        IMyTerminalBlock m_gate = null;
        IMyCubeGrid m_eventHorizonGrid = null;
        public GateType StargateType { get; set; }
        private MyEntity3DSoundEmitter m_soundEmitter;
        public MyEntity3DSoundEmitter SoundEmitter { get { return m_soundEmitter; } }
        private MyEntity3DSoundEmitter m_soundEmitterLoop;
        public MyEntity3DSoundEmitter SoundEmitterLoop { get { return m_soundEmitterLoop; } }
        List<EntityExpire> m_movedEntities = new List<EntityExpire>();
        HUDTextNI.EntityMessage m_frontHelpMessage;
        private Matrix m_ringStartingMatrix;
        private bool m_bInit = false;
        private IMyTerminalBlock m_remoteGate;
        public IMyTerminalBlock RemoteGate
        {
            get { return m_remoteGate; }
            set { m_remoteGate = value; }
        }
        protected static bool m_ControlsInited = false;
        Timer m_callbackTimer = new Timer();
        public bool WasUsed { get; set; }
        private Timer m_gateLifetime = new Timer(Constants.MaxGateLifetime);
        private Timer m_gateAutoCloseTimer = new Timer(10 * 1000);
        private bool m_remoteServer = false;
        private int m_eventcounter = 0;
        private string m_gateAddress;
        public string Address { get { return m_gateAddress; } }
        Matrix m_startingMatrix;
        GateEdition m_gateEdition = GateEdition.None;

        public IMyEntity m_EventHorizon;
        public IMyEntity m_IrisShield;
        public IMyEntity m_Ring;

        public IMyEntity EventHorizon
        {
            get
            {
                try
                {
                    return (m_gate as MyEntity).Subparts[Constants.EventHorizonSubpartName];
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                m_EventHorizon = value;
            }
        }
        public IMyEntity IrisShield
        {
            get
            {
                try
                {
                    if((m_gate as MyEntity).Subparts.ContainsKey(Constants.IrisName))
                        return (m_gate as MyEntity).Subparts[Constants.IrisName];
                    else
                        return (m_gate as MyEntity).Subparts[Constants.ShieldName];
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                //m_IrisShield = value;
                //no-op
            }
        }

        public IMyEntity Ring
        {
            get
            {
                try
                {
                    return (m_gate as MyEntity).Subparts[Constants.RingSubpartName];
                }
                catch
                {
                    return null;
                }
            }
        }


        /// <summary>
        /// Trigged when gate state changes
        /// </summary>
        public event Action<IMyTerminalBlock, GateState> StateChanged;

        #region Runtime data
        // This data is needed for runtime, but should not be serialized.
        // DHD, if supplied, will reduce power requirements, and enable fast dialing
        IMyTerminalBlock m_dhd;
        string m_destination;

        #endregion Runtime data

        public Stargate()
        {
            m_gateData = new StargateData(this);
            m_gateAutoCloseTimer.Elapsed += gateAutoCloseTimer_Elapsed;
            m_gateAutoCloseTimer.AutoReset = false;
            m_gateLifetime.Elapsed += gateAutoCloseTimer_Elapsed;
            m_gateLifetime.AutoReset = false;
        }

        #region Core Entity Overrides
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            if (!(Container.Entity is IMyTerminalBlock))
                return;

            if (!Globals.ModEnabled)
                return;

            m_gate = Container.Entity as IMyTerminalBlock;
            StargateType = m_gate.GetGateType();
            if (StargateType != GateType.Invalid)
            {
                m_soundEmitter = new MyEntity3DSoundEmitter(m_gate as MyEntity);
                m_soundEmitterLoop = new MyEntity3DSoundEmitter(m_gate as MyEntity);

                //MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Buildable: " + StargateAdmin.Configuration.Buildable);
                if (StargateMissionComponent.ModInitialized && !StargateAdmin.Configuration.Buildable && MyAPIGateway.Multiplayer.IsServer)
                {
                    // Prevent building if server set not to
                    MyAPIGateway.Utilities.ShowMessage(Globals.ModName, "Gates not buildable.");
                    throw new Exception("Gates not buildable");
                }

                //DeserializeData();
                //m_gate.DoorStateChanged += gate_DoorStateChanged;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

                if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate U"))
                    m_gateEdition = GateEdition.First;
                else if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate O"))
                    m_gateEdition = GateEdition.Second;
                else if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate M"))
                    m_gateEdition = GateEdition.Second;
                else if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate A"))
                    m_gateEdition = GateEdition.Third;
            }
            else
            {
                m_gate = null;
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = Container.Entity.GetObjectBuilder(copy);
            return ob;
        }
        #endregion Core Entity Overrides

        #region Terminal Controls
        static void CreateTerminalControls()
        {
            if (m_ControlsInited)
                return;                         // This must be first!

            // Check for mod clash first
            if (StargateAdmin.CheckAndDisableMod())
                return;
            Logger.Instance.LogMessage("CreateTerminalControls");
            m_ControlsInited = true;

            var controls = new List<IMyTerminalControl>();

            // Do rest of init
            // Create controls first, so they can be referenced below
            // Dial button
            var dialname = "Phoenix.Stargate.Dial";
            var dialButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(dialname);
            IMyTerminalAction dialAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(dialname);        // Use for compatibility with stock drive

            dialButton.Title = MyStringId.GetOrCompute("Dial");
            dialButton.Visible = (b) => b.GetGateType() != GateType.Invalid;
            dialButton.Enabled = (b) => b.GetGateType() != GateType.Invalid && b.IsFunctional;
            dialButton.Action = (b) => b.GameLogic.GetAs<Stargate>().DialGate(player: MyAPIGateway.Session.Player?.IdentityId ?? 0);
            //MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(dialButton);

            // Dial action
            StringBuilder actionname = new StringBuilder();
            actionname.Append("Dial");
            dialAction.Name = actionname;
            dialAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            dialAction.ValidForGroups = false;
            dialAction.Enabled = (b) => b.GetGateType() != GateType.Invalid && b.IsFunctional;
            dialAction.Action = (b) => dialButton.Action(b);
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(dialAction);

            //AlwaysAnimateLongDial
            String alwaysAnimateName = "Phoenix.Stargate.AlwaysAnimateLongDial";
            IMyTerminalControlOnOffSwitch alwaysAnimateOnOff = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyTerminalBlock>(alwaysAnimateName);
            alwaysAnimateOnOff.Title = MyStringId.GetOrCompute("Always Animate");
            alwaysAnimateOnOff.OnText = MyStringId.GetOrCompute("True");
            alwaysAnimateOnOff.OffText = MyStringId.GetOrCompute("False");
            alwaysAnimateOnOff.Tooltip = MyStringId.GetOrCompute("Set true to always do the long gate spinning animation, even when quick dialing.");
            alwaysAnimateOnOff.Setter = (b, v) => MessageUtils.SendMessageToServer(new MessageAlwaysAnimate() { Entity = b.EntityId, AlwaysAnimate = v });
            alwaysAnimateOnOff.Getter = (b) => (b.GameLogic.GetAs<Stargate>()?.m_gateData?.AlwaysAnimateLongDial).GetValueOrDefault();
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(alwaysAnimateOnOff);

            // Iris Control
            var irisName = "Phoenix.Stargate.Iris";
            var irisOnOff = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyTerminalBlock>(irisName);
            irisOnOff.Title = MyStringId.GetOrCompute("Iris/Shield");
            irisOnOff.OnText = MyStringId.GetOrCompute("On"); ;
            irisOnOff.OffText = MyStringId.GetOrCompute("Off"); ;
            irisOnOff.Visible = (b) => b.GetGateType() == GateType.Stargate;
            irisOnOff.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            irisOnOff.Setter = (b, v) => MessageUtils.SendMessageToServer(new MessageIris() { Entity = b.EntityId, Activate = v });
            irisOnOff.Getter = (b) => (b.GameLogic.GetAs<Stargate>()?.m_gateData?.IrisActive).GetValueOrDefault();
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(irisOnOff);

            // Iris Action
            IMyTerminalAction irisAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(irisName);
            actionname = new StringBuilder();
            actionname.Append(irisOnOff.Title.String);
            irisAction.Name = actionname;
            irisAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds";
            irisAction.ValidForGroups = true;
            irisAction.Writer = (b, t) => t.Append((b.GameLogic.GetAs<Stargate>().Data.IrisActive ? irisOnOff.OnText : irisOnOff.OffText).String);
            irisAction.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            irisAction.Action = (b) => irisOnOff.Setter(b, !irisOnOff.Getter(b));
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(irisAction);

            // On
            actionname = new StringBuilder();
            actionname.Append(irisOnOff.Title).Append(" ").Append(irisOnOff.OnText);

            irisAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(irisName + "_On");
            irisAction.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds";
            irisAction.Name = actionname;
            irisAction.Action = (b) => irisOnOff.Setter(b, true);
            irisAction.Writer = (b, t) => t.Append((b.GameLogic.GetAs<Stargate>().Data.IrisActive ? irisOnOff.OnText : irisOnOff.OffText).String);
            irisAction.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            irisAction.Action = (b) => irisOnOff.Setter(b, true);
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(irisAction);

            // Off
            actionname = new StringBuilder();
            actionname.Append(irisOnOff.Title).Append(" ").Append(irisOnOff.OffText);

            irisAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(irisName + "_Off");
            irisAction.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds";
            irisAction.Name = actionname;
            irisAction.Action = (b) => irisOnOff.Setter(b, true);
            irisAction.Writer = (b, t) => t.Append((b.GameLogic.GetAs<Stargate>().Data.IrisActive ? irisOnOff.OnText : irisOnOff.OffText).String);
            irisAction.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            irisAction.Action = (b) => irisOnOff.Setter(b, false);
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(irisAction);

            // Reset gate button, in case of problems
            var resetName = "Phoenix." + Globals.ModName + ".Reset";
            var resetButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>(resetName);
            resetButton.Title = MySpaceTexts.ToolbarAction_Reset;
            //resetButton.Visible = (b) => b.GetGateType() != GateType.Invalid;
            resetButton.Visible = (b) => false;
            resetButton.Enabled = (b) => b.IsFunctional;
            resetButton.Action = (b) => b.GameLogic.GetAs<Stargate>().ResetGate();
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(resetButton);

            // Reset Action
            IMyTerminalAction resetAction = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>(resetName);
            resetAction.Name = MyTexts.Get(MySpaceTexts.ToolbarAction_Reset);
            resetAction.Icon = @"Textures\GUI\Icons\Actions\Reset.dds";
            resetAction.ValidForGroups = true;
            resetAction.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            resetAction.Action = (b) => resetButton.Action(b);
            MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(resetAction);

            // Destination PB property
            var destination = "Phoenix." + Globals.ModName + ".Destination";
            var destProperty = MyAPIGateway.TerminalControls.CreateProperty<string, IMyTerminalBlock>(destination);
            //destProperty.Visible = (b) => b.GetGateType() == GateType.Stargate;
            destProperty.Visible = (b) => false;
            destProperty.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            destProperty.Setter = (b, v) => b.GameLogic.GetAs<Stargate>().m_destination = v;
            destProperty.Getter = (b) => b.GameLogic.GetAs<Stargate>()?.m_destination ?? string.Empty;
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(destProperty);

            // Status PB property
            var statusname = "Phoenix." + Globals.ModName + ".Status";
            var statusProp = MyAPIGateway.TerminalControls.CreateProperty<string, IMyTerminalBlock>(statusname);
            //statusProp.Visible = (b) => b.GetGateType() == GateType.Stargate;
            statusProp.Visible = (b) => false;
            statusProp.Enabled = (b) => b.GetGateType() == GateType.Stargate && b.IsFunctional;
            statusProp.Setter = (b, v) => { };
            statusProp.Getter = (b) => b.GameLogic.GetAs<Stargate>()?.m_gateData?.State.ToString() ?? string.Empty;
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(statusProp);
        }

        public void ToggleIris(bool enable)
        {
            if (IrisShield == null)
            {
                LoadSubparts();

                if (IrisShield == null)
                    return;
            }
            Logger.Instance.LogDebug(string.Format("iris closed: " + IrisShield.Closed));
            Logger.Instance.LogAssert(!IrisShield.Closed, "!IrisShield.Closed");

            if (IrisShield.Visible == enable)
                return;

            if (enable)
            {
                PlaySound(IrisShield.DisplayName == Constants.IrisName ? "IrisClose" : "ShieldClose");

                if (IrisShield.Physics == null)
                    Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(IrisShield);

                IrisShield.Render.Visible = true;
                IrisShield.Physics.Activate();
            }
            else
            {
                PlaySound(IrisShield.DisplayName == Constants.IrisName ? "IrisOpen" : "ShieldOpen");
                IrisShield.Render.Visible = false;
                IrisShield.Physics?.Deactivate();
            }

            ActivateBlocks();
        }

        #endregion Terminal Controls

        #region Events
        public void RaiseStateChanged()
        {
            StateChanged?.Invoke(m_gate, Data.State);
        }

        private void OwnershipChanged(IMyTerminalBlock obj)
        {
            if (!string.IsNullOrEmpty(Address))
            {
                SendNewGate();
            }
        }

        private void AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder arg2)
        {
            arg2.Append(MyTexts.Get(MyStringId.GetOrCompute("BlockPropertiesText_Type")));
            arg2.Append(MyDefinitionManager.Static.GetCubeBlockDefinition(m_gate.BlockDefinition).DisplayNameText);
            arg2.AppendFormat("\n");
            var show = true;

            try
            {
                show = arg1.GetValueBool("ShowInTerminal");
            }
            catch
            { }

            arg2.AppendFormat("Gate address: {0}\n", show ? m_gateAddress : "Hidden");
        }

        private void gateAutoCloseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                try
                {
                    if (!Closed && !MarkedForClose)
                        ResetGate();
                }
                catch { }
            });
        }

        private void IsWorkingChanged(IMyCubeBlock obj)
        {
            if (!m_bInit)
                return;

            if (!obj.IsFunctional)
            {
                SendNewGate(true);
                ResetGate(true);
            }
            else
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                SendNewGate();
            }
        }

        private void ShowOnHUDChanged(IMyTerminalBlock obj)
        {
            if (obj.ShowOnHUD)
                m_frontHelpMessage = StargateExtensions.CreateHUDText(m_gate, m_gate.GetGateType());
        }

        private void OnMarkForClose(IMyEntity obj)
        {
            NeedsUpdate = MyEntityUpdateEnum.NONE;
            m_gate.OnMarkForClose -= OnMarkForClose;
            try
            {
                m_callbackTimer.Close();
                m_gateLifetime.Close();
                m_gateAutoCloseTimer.Close();
                ResetGate(true);
                SendNewGate(true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
            finally
            {
                MarkedForClose = true;
            }
            //m_gate.PropertiesChanged -= PropertiesChanged;
        }
        #endregion Events

        #region Data Serialization
        public override void SerializeData()
        {
            if (m_gate == null || m_gateData == null || !m_hasBeenDeserialized)
                return;

            if (m_gate.Storage == null)
                m_gate.Storage = new MyModStorageComponent();

            // Add vanilla entry so game doesn't crash
            if (!m_gate.Storage.ContainsKey(Constants.VanillaCustomDataKey))
                m_gate.CustomData = string.Empty;

            m_gate.Storage[Constants.StargateDataKey] = SerializeData(m_gateData);
        }

        static string SerializeData(StargateData data)
        {
            StringBuilder sb = new StringBuilder(50);
            // Format is: "sg:[ac=XX;did=YY]
            sb.Append(MyAPIGateway.Utilities.SerializeToXML(data));
            return sb.ToString();
        }

        public override void DeserializeData()
        {
            if (m_gate == null)
                return;

            if (m_gate.Storage == null)
                m_gate.Storage = new MyModStorageComponent();

            if (!m_gate.Storage.ContainsKey(Constants.VanillaCustomDataKey))
                m_gate.Storage[Constants.VanillaCustomDataKey] = string.Empty;

            string customname = m_gate.CustomName;
            StargateData data;

            if (m_gate.Storage.ContainsKey(Constants.StargateDataKey) && !m_gate.CustomName.Contains("sg:"))
            {
                customname = m_gate.Storage[Constants.StargateDataKey];
                data = DeserializeData(ref customname);
                if (data == null)
                {
                    customname = m_gate.CustomName;
                    data = DeserializeData(ref customname);
                    m_gate.SetCustomName(customname);
                }
            }
            else
            {
                data = DeserializeData(ref customname);
                m_gate.SetCustomName(customname);
            }

            if (data != null)
            {
                data.Chevron = m_gateData.Chevron;
                data.State = m_gateData.State;
                data.IsRemote = m_gateData.IsRemote;
                data.DestinationEntityId = m_gateData.DestinationEntityId;
                m_gateData = data;
            }

            //if (m_gateData.State == GateState.Active && !EventHorizon.Visible)
            //    ActivateGate();
            base.DeserializeData();
        }

        StargateData DeserializeData(ref string data)
        {
            StargateData sg = null;

            if (string.IsNullOrWhiteSpace(data))
                return sg;

            int cmdStartIdx = data.IndexOf(" sg:[");
            int cmdEndIdx = data.IndexOf(']', cmdStartIdx >= 0 ? cmdStartIdx : 0);
            // Check if we have custom commands in the name
            if (cmdStartIdx != -1 && cmdEndIdx != -1)
            {
                sg = new StargateData(this);
                string sCmd = data.Remove(cmdEndIdx).Remove(0, cmdStartIdx + 1);
                data = data.Remove(cmdStartIdx, cmdEndIdx - cmdStartIdx + 1);
                // Split the commands for parsing
                string[] cmds = sCmd.Split(new Char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmd in cmds)
                {
                    string tempCmd = cmd.Trim().ToUpperInvariant();

                    if (tempCmd.StartsWith("ACT"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            int timeInSeconds = 0;
                            if (int.TryParse(acopt[1], out timeInSeconds))
                            {
                                sg.AutoCloseTime = timeInSeconds;
                                //dhd.iAutoCloseTime = (int)Math.Ceiling(((timeInSeconds * 60) / 100));
                            }
                        }
                    }

                    if (tempCmd.StartsWith("ACA"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            bool val;
                            if (Boolean.TryParse(acopt[1], out val))
                                sg.AutoCloseWithAll = val;
                        }
                    }

                    if (tempCmd.StartsWith("ANT"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            bool val;
                            if (Boolean.TryParse(acopt[1], out val))
                                sg.Antenna = val;
                        }
                    }

                    //if (tempCmd.StartsWith("PID"))
                    //{
                    //    string[] acopt = tempCmd.Split(new Char[] { '=' });

                    //    if (acopt.Length == 2)
                    //    {
                    //        long lval;
                    //        if (long.TryParse(acopt[1], out lval))
                    //            sg.ActivatingPlayerId = lval;
                    //    }
                    //}

                    //if (tempCmd.StartsWith("ISR"))
                    //{
                    //    string[] acopt = tempCmd.Split(new Char[] { '=' });

                    //    if (acopt.Length == 2)
                    //    {
                    //        bool val;
                    //        if (Boolean.TryParse(acopt[1], out val))
                    //            sg.IsRemote = val;
                    //    }
                    //}

                    if (tempCmd.StartsWith("IRA"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            bool val;
                            if (Boolean.TryParse(acopt[1], out val))
                                sg.IrisActive = val;
                        }
                    }

                    if (tempCmd.StartsWith("GST"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            GateState val;
                            if (GateState.TryParse(acopt[1], out val))
                                sg.State = val;
                        }
                    }

                    if (tempCmd.StartsWith("CST"))
                    {
                        string[] acopt = tempCmd.Split(new Char[] { '=' });

                        if (acopt.Length == 2)
                        {
                            Chevron val;
                            if (Chevron.TryParse(acopt[1], out val))
                                sg.Chevron = val;
                        }
                    }
                }
            }
            else if (data.Contains("StargateData"))
            {
                sg = MyAPIGateway.Utilities.SerializeFromXML<StargateData>(data);
                sg.Parent = this;
                // Trigger events
                sg.IrisActive = sg.IrisActive;
            }

            return sg;
        }
        #endregion Data Serialization

        #region Event Horizon/Iris
        // Thanks to Midspace for figuring out how to spawn prefabs
        public IMyEntity SpawnEventHorizon(bool isIris = false, bool subpartonly = false)
        {
            String gateSubType = m_gate.BlockDefinition.SubtypeId;
            String prefabName = "Event Horizon";
            string modelName = "Event_horizon_generic";
            string name = gateSubType.StartsWith("Stargate A") ? Constants.ShieldName : Constants.IrisName;
            MyEntitySubpart subpart = (isIris ? IrisShield : EventHorizon) as MyEntitySubpart;
            string subpartname = Constants.EventHorizonSubpartName;

            Logger.Instance.LogMessage("SpawnEventHorizon");
            if (subpart == null)
            {
                if (isIris)
                {
                    modelName = "Iris" + (gateSubType.StartsWith("Stargate A") ? "_shield" : "");
                }
                else
                {
                    name = Constants.EventHorizonSubpartName;
                    switch (gateSubType)
                    {
                        case "Supergate":
                            modelName = "Supergate_horizon";
                            break;
                    }
                }
                var subparts = (m_gate as MyEntity).Subparts;
                subpart = new MyEntitySubpart();
                var model = Utils.GetModelsPath() + modelName + ".mwm";
                Logger.Instance.LogMessage("Loading model: " + model);
                subpart.Init(new System.Text.StringBuilder(name), model, m_gate as MyEntity, null, model);
                subpart.Render.EnableColorMaskHsv = m_gate.Render.EnableColorMaskHsv;
                subpart.Render.ColorMaskHsv = m_gate.Render.ColorMaskHsv;
                subpart.Render.PersistentFlags = MyPersistentEntityFlags2.CastShadows;
                subpart.Render.NeedsDrawFromParent = false;
                var matrix = Matrix.Identity;

                if (gateSubType == "Stargate M")
                    matrix.Translation += (matrix.Up * Constants.BaseGateUpOffset) + (matrix.Forward * Constants.BaseGateForwardOffset);
                else if (m_gate.GetGateType() != GateType.Supergate)
                    matrix.Translation -= (matrix.Up * Constants.GateUpOffset);

                subpart.PositionComp.LocalMatrix = matrix;

                // This is needed to draw glass
                m_gate.Render.NeedsDrawFromParent = false;
                m_gate.Render.NeedsDraw = false;

                if (m_gate.InScene)
                    subpart.OnAddedToScene(m_gate);

                if (isIris)
                    subparts[subpartname = Constants.IrisName] = subpart;
                else
                    subparts[subpartname = Constants.EventHorizonSubpartName] = subpart;

                //if (MyAPIGateway.Multiplayer.IsServer)
                //    MessageUtils.SendMessageToAllPlayers(new MessageEventHorizon() { Parent = m_gate.EntityId, SubpartName = subpartname });
            }

            if (m_gate.GetGateType() == GateType.Supergate)
            {
                subpart.Render.Visible = true;
                return subpart;
            }

            if (!isIris && !subpartonly && MyAPIGateway.Session.IsServer)
            {
                try
                {
                    if (!m_gateData.Antenna)
                        prefabName += "_noantenna";

                    //looks for the definition of the ship
                    Logger.Instance.LogDebug("Loading prefab: " + prefabName);
                    var prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
                    if (prefab != null && prefab.CubeGrids == null)
                    {
                        // If cubegrids is null, reload definitions and try again
                        MyDefinitionManager.Static.ReloadPrefabsFromFile(prefab.PrefabPath);
                        prefab = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
                    }

                    if (prefab == null || prefab.CubeGrids == null || prefab.CubeGrids.Length == 0)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Error loading prefab: " + prefabName, 7500);
                        return null;
                    }

                    //get the grid containing the ship
                    var grid = prefab.CubeGrids[0];

                    if (grid == null)
                    {
                        MyAPIGateway.Utilities.ShowNotification("Error loading prefab grid: " + prefabName, 7500);
                        return null;
                    }

                    var gridBuilder = (MyObjectBuilder_CubeGrid)grid.Clone();
                    gridBuilder.IsStatic = false;
                    gridBuilder.CreatePhysics = false;
                    gridBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene;
                    gridBuilder.DisplayName = "Event Horizon at " + m_gate.CustomName;

                    long player = m_gateData.ActivatingPlayerId;
                    foreach (var block in gridBuilder.CubeBlocks)
                    {
                        if (block is MyObjectBuilder_LaserAntenna && RemoteGate != null)
                        {
                            (block as MyObjectBuilder_LaserAntenna).gpsTarget = RemoteGate.GetPosition();
                            (block as MyObjectBuilder_LaserAntenna).gpsTargetName = RemoteGate.DisplayNameText;
                            (block as MyObjectBuilder_LaserAntenna).State = 4 | 0x8;    // 0x8 means permanent
                        }

                        if (block is MyObjectBuilder_RadioAntenna && player != 0)
                            (block as MyObjectBuilder_RadioAntenna).Enabled = m_gateData.Antenna;   // TODO: Check antenna on remote gate

                        block.Owner = player;
                    }
                    var tempList = new List<MyObjectBuilder_EntityBase>();

                    //give the grid containing the ship a new position
                    gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(subpart.PositionComp.GetPosition(), m_gate.WorldMatrix.Forward, m_gate.WorldMatrix.Up);

                    tempList.Add(gridBuilder);
                    MyAPIGateway.Entities.RemapObjectBuilderCollection(tempList);
                    var entity = MyAPIGateway.Entities.CreateFromObjectBuilderNoinit(tempList[0]);
                    entity.Save = false;
                    entity.Synchronized = true;
                    (entity as MyEntity).Init(tempList[0]);
                    MyAPIGateway.Entities.AddEntity(entity, true);
                    m_gate.Hierarchy.AddChild(entity, true);
                    m_eventHorizonGrid = entity as IMyCubeGrid;
                    MessageUtils.SendMessageToAllPlayers(new MessageEventHorizon() { Parent = m_gate.EntityId, SubgridId = entity.EntityId, SubpartName = subpartname });
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                }
            }
            Logger.Instance.LogDebug(string.Format("Gate: {0}; Subpart: {1}", m_gate.CustomName, subpart.DisplayName));
            return subpart;
        }

        public void RemoveEventHorizon(bool isIris = false)
        {
            try
            {
                if (isIris)
                {
                    if (IrisShield != null && IrisShield.Visible)
                    {
                        this.m_gate.PlaySound(IrisShield.DisplayName == Constants.IrisName ? "IrisOpen" : "ShieldOpen");
                        IrisShield.Visible = false;
                        IrisShield.Physics.Deactivate();

                        if (MyAPIGateway.Multiplayer.IsServer)
                            MessageUtils.SendMessageToAllPlayers(new MessageEventHorizon() { Parent = m_gate.EntityId, SubpartName = IrisShield.DisplayName, Remove = true });
                    }

                    //// Only change the update method if not active
                    //if (!IsActive)
                    //    NeedsUpdate &= ~(MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME);
                }
                else
                {
                    if (EventHorizon != null && EventHorizon.Visible)
                    {
                        EventHorizon.Visible = false;
                        if (MyAPIGateway.Session.IsServer)
                        {
                            //var grid = m_gate.Hierarchy.Children?.ElementAtOrDefault(0)?.Container?.Entity as IMyCubeGrid;
                            if (m_eventHorizonGrid != null)
                            {
                                var blocks = new List<IMySlimBlock>();
                                m_eventHorizonGrid.GetBlocks(blocks, (b) => b?.FatBlock is IMyFunctionalBlock);
                                foreach (var block in blocks)
                                    (block.FatBlock as IMyFunctionalBlock).RequestEnable(false);

                                if (m_eventHorizonGrid.SyncObject != null)
                                    MyEntities.SendCloseRequest(m_eventHorizonGrid);
                                else
                                    m_eventHorizonGrid.Close();

                                m_eventHorizonGrid = null;
                            }
                            else
                            {
                                Logger.Instance.LogMessage("Cannot close null EventHorizon grid");
                            }
                        }
                        if (MyAPIGateway.Multiplayer.IsServer)
                            MessageUtils.SendMessageToAllPlayers(new MessageEventHorizon() { Parent = m_gate.EntityId, SubpartName = EventHorizon.DisplayName, Remove = true });
                    }
                }

                ActivateBlocks();

                //if (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
                //    MessageUtils.SendMessageToAllPlayers(new MessageSpawn() { GateEntity = m_gate.EntityId, Iris = isIris, Remove = true });
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
        }
        #endregion Event Horizon/Iris

        #region Group Block Activation
        void ActivateBlocks(bool log = true)
        {
            SwitchLCDTexture(log);
            RunProgrammableBlocks();
            RunTimers();
        }

        void SwitchLCDTexture(bool log = true)
        {
            try
            {
                // Get a list of all LCD blocks on the grid with the gate
                List<IMySlimBlock> lcds = new List<IMySlimBlock>();
                (m_gate.CubeGrid as IMyCubeGrid).GetBlocks(lcds, (x) => x.FatBlock is IMyTextPanel
                                                                 && ((x.FatBlock as IMyTerminalBlock).CustomName.ToUpperInvariant().Contains("STARGATE")
                                                                    || (x.FatBlock as IMyTerminalBlock).CustomName.ToUpperInvariant().Contains("PORTAL")));

                var groupedblocks = GetGroupedBlocks(new MyObjectBuilderType(typeof(MyObjectBuilder_TextPanel)));

                // Add the named blocks to the grouped list
                foreach (var block in lcds)
                {
                    if (!groupedblocks.Contains(block.FatBlock as IMyTerminalBlock))
                        groupedblocks.Add(block.FatBlock as IMyTerminalBlock);
                }

                foreach (var block in groupedblocks)
                {
                    if (log)
                        Logger.Instance.LogMessage("Found LCD: " + (block as IMyTerminalBlock).CustomName);

                    var lcd = block as IMyTextPanel;
                    string lcdtext = "Stargate SG1 Idle";

                    switch (m_gateData.State)
                    {
                        case GateState.Active:
                            if (m_gateData.IsRemote)
                            {
                                if (m_gate.IsIrisActive())
                                    lcdtext = "Stargate SG1 Incoming Iris";
                                else
                                    lcdtext = "Stargate SG1 Incoming";
                            }
                            else
                            {
                                if (m_gate.IsIrisActive())
                                    lcdtext = "Stargate SG1 Outgoing Iris";
                                else
                                    lcdtext = "Stargate SG1 Outgoing";
                            }
                            break;
                        case GateState.Idle:
                            if (m_gate.IsIrisActive())
                                lcdtext = "Stargate SG1 Idle Iris";
                            break;
                        case GateState.Dialing:
                            lcdtext = "Stargate SG1 Dialing";
                            break;
                    }
                    if (lcd.CurrentlyShownImage != lcdtext)
                    {
                        lcd.ClearImagesFromSelection();

                        lcd.AddImageToSelection(lcdtext);
                        lcd.ShowTextureOnScreen();
                    }
                    Logger.Instance.LogDebug("Changing image to: " + lcdtext);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
        }

        private List<IMyTerminalBlock> GetGroupedBlocks()
        {
            return GetGroupedBlocks(new MyObjectBuilderType(typeof(MyObjectBuilder_EntityBase)));
        }

        private List<IMyTerminalBlock> GetGroupedBlocks(MyObjectBuilderType objtype)
        {
            return DoorDHDExtensions.GetGroupedBlocks(m_gate, objtype);
        }

        public void RunProgrammableBlocks(bool error = false)
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();

            try
            {
                var progblocks = GetGroupedBlocks(new MyObjectBuilderType(typeof(MyObjectBuilder_MyProgrammableBlock)));

                if (progblocks.Count == 0)
                {
                    List<IMySlimBlock> blockList = new List<IMySlimBlock>();
                    IMyCubeGrid grid = m_gate.GetTopMostParent() as IMyCubeGrid;
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();

                    grid.GetBlocks(blocks, (x) => x.FatBlock != null &&
                        x.FatBlock is IMyProgrammableBlock &&
                        (!string.IsNullOrEmpty((x.FatBlock as IMyTerminalBlock).CustomName)));

                    foreach (var block in blocks)
                    {
                        // Skip disabled or destroyed gates
                        if (block.IsDestroyed || block.FatBlock == null || !block.FatBlock.IsFunctional)
                            continue;

                        if (!string.IsNullOrEmpty((block.FatBlock as IMyTerminalBlock).CustomName))
                        {
                            string name = (block.FatBlock as IMyTerminalBlock).CustomName.ToUpperInvariant();

                            if (name.ToLower().Contains(Globals.ModName.ToLower()))
                                progblocks.Add(block.FatBlock as IMyTerminalBlock);
                        }
                    }
                }

                foreach (var block in progblocks)
                {
                    if (!(block as IMyTerminalBlock).HasPlayerAccess(m_gate.OwnerId))
                        continue;

                    Logger.Instance.LogDebug(string.Format("Running programmable block {0} with arguments: {1}", block.DisplayNameText, Data.State.ToString()));
                    (block as IMyProgrammableBlock).TryRun((error ? "Error" : Data.State.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
        }
        public void RunTimers(bool error = false)
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();

            try
            {
                var progblocks = GetGroupedBlocks(new MyObjectBuilderType(typeof(MyObjectBuilder_TimerBlock)));

                if (progblocks.Count == 0)
                {
                    List<IMySlimBlock> blockList = new List<IMySlimBlock>();
                    IMyCubeGrid grid = m_gate.GetTopMostParent() as IMyCubeGrid;
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();

                    grid.GetBlocks(blocks, (x) => x.FatBlock != null &&
                        x.FatBlock is IMyProgrammableBlock &&
                        (!string.IsNullOrEmpty((x.FatBlock as IMyTerminalBlock).CustomName)));

                    foreach (var block in blocks)
                    {
                        // Skip disabled or destroyed gates
                        if (block.IsDestroyed || block.FatBlock == null || !block.FatBlock.IsFunctional)
                            continue;

                        if (!string.IsNullOrEmpty((block.FatBlock as IMyTerminalBlock).CustomName))
                        {
                            string name = (block.FatBlock as IMyTerminalBlock).CustomName.ToUpperInvariant();

                            if (name.ToLower().Contains(Globals.ModName.ToLower()))
                                progblocks.Add(block.FatBlock as IMyTerminalBlock);
                        }
                    }
                }

                var regex = new Regex(@"\[\\s*" + Globals.ModName + ":\\s*?(.*?)\\s*?]", RegexOptions.IgnoreCase);
                foreach (var block in progblocks)
                {
                    var data = block.CustomData;
                    var matches = regex.Matches(data);

                    if (matches.Count == 0 || matches[0].Groups.Count == 0)
                        continue;

                    if (matches[0].Groups[1].Value.ToLower().Contains(Data.State.ToString().ToLower()))
                    {

                        if (!(block as IMyTerminalBlock).HasPlayerAccess(m_gate.OwnerId))
                            continue;

                        Logger.Instance.LogDebug(string.Format("Running timer block {0}", block.DisplayNameText));
                        (block as IMyTimerBlock).ApplyAction("TriggerNow");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException(ex);
            }
        }
        #endregion Group Block Activation

        #region Sounds
        public void PlaySound(string soundname, bool force = false, Action<MyEntity3DSoundEmitter> stoppedCallback = null, bool sync = false)
        {
            MyEntity3DSoundEmitter emitter = null;
            MyEntity3DSoundEmitter emitterLoop = null;

            if (m_gate.GetGateType() != GateType.Invalid)
            {
                emitter = m_gate.GameLogic.GetAs<Stargate>().SoundEmitter;
                emitterLoop = m_gate.GameLogic.GetAs<Stargate>().SoundEmitterLoop;
            }

            if (emitter != null)
            {
                if (string.IsNullOrEmpty(soundname))
                {
                    Logger.Instance.LogDebug("Gate StopSound");
                    emitterLoop.StopSound(force, true);
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

                        if (Data.State == GateState.Incoming)
                            length -= 150;


                        ElapsedEventHandler callback = null;

                        callback = delegate (object sender, ElapsedEventArgs e)
                        {
                            Logger.Instance.LogDebugOnGameThread("Inside callback");
                            m_callbackTimer.Elapsed -= callback;
                            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                            {
                                if (!Closed && !MarkedForClose)
                                {
                                    stoppedCallback(emitter);
                                    emitter.CustomMaxDistance = null;
                                }
                            });
                        };

                        // Must use a Timer, since sound callbacks are triggered early if
                        // on server, or player is far away from source.
                        m_callbackTimer.Interval = length;
                        m_callbackTimer.Elapsed += callback;
                        m_callbackTimer.Start();
                    }

                    if (sync && MyAPIGateway.Session.IsServer)
                        MessageUtils.SendMessageToAllPlayers(new MessagePlaySound() { Entity = m_gate.EntityId, SoundName = soundname, Force = force });

                    if (StargateType == GateType.Supergate)
                        emitter.CustomMaxDistance = 1000;

                    if (soundname == "StargateActive")
                        emitterLoop.PlaySound(sound, force);
                    else
                        emitter.PlaySound(sound, force);

                    if (soundname == "StargateShutdown")
                        emitterLoop.StopSound(force, true);
                }
            }
        }
        #endregion Sounds

        #region Dialing Sequence
        private float m_animationProgress = 0;  // between 0 and 1
        Matrix m_sourceMatrix;
        Matrix m_destMatrix;
        DateTime m_startTime;
        TimeSpan m_animationLength;
        bool m_animationEnabled = false;
        float m_animationToSoundPercent = 0.6f;
        Random m_glyphIndexGenerator = new Random();

        public void RotateRing()
        {
            if (!m_animationEnabled)
                return;

            if (Ring != null && m_animationProgress < 1.0)
            {
                m_animationProgress = (float)((DateTime.Now - m_startTime).TotalMilliseconds / (m_animationLength.TotalMilliseconds * m_animationToSoundPercent));
                if (m_animationProgress >= 1.0f)
                {
                    Ring.LocalMatrix = m_destMatrix;
                    m_animationEnabled = false;
                }
                else
                {
                    var newmatrix = MathUtility.MySlerp(m_sourceMatrix, m_destMatrix, m_animationProgress);
                    newmatrix.Translation = Ring.LocalMatrix.Translation;
                    Ring.LocalMatrix = newmatrix;
                }
            }
        }

        public void ActivateGate()
        {
            // Do actual connection work here
            if (Data.ActivatingPlayerId == 0)
                Data.ActivatingPlayerId = m_dhd?.OwnerId ?? m_gate.OwnerId;

            if (Data.IsRemote)
            {
                IMyEntity remote;
                MyAPIGateway.Entities.TryGetEntityById(Data.DestinationEntityId, out remote);
                m_remoteGate = remote as IMyTerminalBlock;
            }
            else
            {
                if (MyAPIGateway.Multiplayer.IsServer && RemoteGate == null)
                {
                    var remote = DoorDHDExtensions.GetNamedGate(m_dhd, m_destination, m_gate, true);
                    var gl = remote?.GameLogic.GetAs<Stargate>();
                    // Make sure PB scripts setting m_destination cannot dial without having actual address
                    if (remote != null && (gl?.Data?.State != GateState.Active))
                    {
                        Data.DestinationEntityId = remote.EntityId;
                        RemoteGate = remote;
                        RemoteGate.GameLogic.GetAs<Stargate>().DialIncoming(m_gate);
                    }
                    else
                    {
                        ResetGate();
                        return;
                    }
                }
                else if (RemoteGate != null)
                {
                    var gl = RemoteGate?.GameLogic.GetAs<Stargate>();
                    if (gl?.RemoteGate == m_gate &&
                        !(gl?.Data?.State == GateState.Active || gl?.Data?.State == GateState.Incoming))
                    {
                        ResetGate();
                        return;
                    }

                }
                if (MyAPIGateway.Multiplayer.IsServer)
                    m_gateLifetime.Start();
            }
            m_gateData.State = GateState.Active;

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                ActivateBlocks();
                Logger.Instance.LogDebug(string.Format("Gate: {0}, Update EACH_FRAME", m_gate.CustomName));
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }

            EventHorizon = SpawnEventHorizon();

            PlaySound("StargateActive");
        }

        public void CheckAndBeginDialingDestination()
        {
            if (Data.IsRemote)
                return;

            var remote = DoorDHDExtensions.GetNamedGate(m_dhd, m_destination, m_gate, true);
            var gl = remote?.GameLogic.GetAs<Stargate>();

            if (remote != null && ((gl?.Data?.State == GateState.Idle || gl?.Data?.State == GateState.Dialing) ||
                (gl?.RemoteGate == m_gate && (gl?.Data?.State == GateState.Incoming || gl?.Data?.State == GateState.Idle))) &&
                String.Compare(gl?.Address, m_destination, true) == 0)
            {
                if (MyAPIGateway.Session.IsServer)
                    MyAPIGateway.Multiplayer.ReplicateEntityForClient(remote.GetTopMostParent().EntityId, 0);
                Data.DestinationEntityId = remote.EntityId;
                remote.GameLogic.GetAs<Stargate>().DialIncoming(m_gate);
            }
        }

        public void DialIncoming(IMyTerminalBlock source, bool fromMessage = false)
        {
            Logger.Instance.LogMessage(m_gate.CustomName + ": Incoming wormhole");
            if (MyAPIGateway.Multiplayer.IsServer || fromMessage)
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                    MessageUtils.SendMessageToAllPlayers(new MessageDial() { Gate = m_gate?.EntityId ?? 0, RemoteGate = source?.EntityId ?? 0, Incoming = true });

                Logger.Instance.LogAssert(source != null, "source != null");
                m_callbackTimer.Stop();
                Data.DestinationEntityId = source?.EntityId ?? 0;
                Data.ActivatingPlayerId = source?.GameLogic.GetAs<Stargate>()?.Data?.ActivatingPlayerId ?? 0;
                Data.IsRemote = true;
                Data.State = GateState.Incoming;
                Data.Chevron = Chevron.None;
                DialChevron();
            }
            else
            {
                MessageUtils.SendMessageToServer(new MessageDial() { Gate = m_gate.EntityId, RemoteGate = source?.EntityId ?? 0, Incoming = true });
            }
        }

        public bool DialGate(IMyTerminalBlock dhd = null, bool fromMessage = false, string destination = null, long player = 0)
        {
            Logger.Instance.LogMessage(m_gate.CustomName + ": Initiating dialing sequence");
            Logger.Instance.LogMessage("Using DHD: " + dhd?.CustomName ?? "<null>");
            if (MyAPIGateway.Multiplayer.IsServer || fromMessage)
            {
                if (Data.State == GateState.Incoming)
                {
                    var msg = "Cannot dial: Incoming wormhole";
                    ShowMessageToUsersInRange(msg, 5000);
                    Logger.Instance.LogMessage(m_gate.CustomName + ": " + msg);
                    return false;
                }
                else if (Data.State == GateState.Active && Data.IsRemote)
                {
                    var msg = "Cannot dial: Incoming wormhole active";
                    ShowMessageToUsersInRange(msg, 5000);
                    Logger.Instance.LogMessage(m_gate.CustomName + ": " + msg);
                    return false;
                }
                else if (Data.State == GateState.Active || Data.State == GateState.Dialing)
                {
                    ResetGate();
                    return false;
                }

                if (MyAPIGateway.Multiplayer.IsServer)
                    MessageUtils.SendMessageToAllPlayers(new MessageDial()
                    {
                        Gate = m_gate.EntityId,
                        RemoteGate = RemoteGate?.EntityId ?? 0,
                        DHD = dhd?.EntityId ?? 0,
                        ActivatingPlayer = player,
                        Grid = dhd?.CubeGrid?.EntityId ?? 0,
                        DHDPosition = dhd?.Position ?? Vector3I.Zero
                    });

                m_dhd = dhd;
                if (!string.IsNullOrEmpty(destination))
                    m_destination = destination;

                m_gateData.ActivatingPlayerId = player;
                m_gateData.State = GateState.Dialing;
                ActivateBlocks();
                DialChevron();
            }
            else
            {
                MessageUtils.SendMessageToServer(new MessageDial() { Gate = m_gate.EntityId, RemoteGate = RemoteGate?.EntityId ?? 0, DHD = dhd?.EntityId ?? 0, ActivatingPlayer = MyAPIGateway.Session?.Player?.IdentityId ?? m_gate.OwnerId });
            }
            return true;
        }

        public void DialChevron(Chevron chevron = Chevron.None)
        {
            if (Data.State != GateState.Dialing && Data.State != GateState.Incoming)
                return;

            var currentGlyph = ' ';
            var fastDialDest = false;
            if (!m_gateData.AlwaysAnimateLongDial && m_dhd != null && !m_dhd.BlockDefinition.SubtypeName.Contains("Computer"))
                fastDialDest = true;

            // Grab the address, if we can (for dialing correct chevrons
            var destination = DoorDHDExtensions.GetNamedGate(m_dhd, m_destination, m_gate)?.GameLogic.GetAs<Stargate>()?.Address ?? m_destination;

            if (StargateType == GateType.Supergate)
                fastDialDest = true;

            var newchevron = chevron;
            if (chevron == Chevron.None)
            {
                newchevron = Data.Chevron;
                // Dial next chevron
                // Note this is in reverse order
                if (newchevron.HasFlag(Chevron.Seven))
                {
                    ActivateGate();
                    return;
                }
                else if (newchevron.HasFlag(Chevron.Six))
                {
                    newchevron |= Chevron.Seven;
                    currentGlyph = ' ';
                }
                else if (newchevron.HasFlag(Chevron.Five))
                {
                    // Remote gates dial all chevrons
                    if (!fastDialDest)
                        CheckAndBeginDialingDestination();
                    newchevron |= Chevron.Six;
                    currentGlyph = destination?.ElementAtOrDefault(5) ?? '\0';
                }
                else if (newchevron.HasFlag(Chevron.Four))
                {
                    newchevron |= Chevron.Five;
                    currentGlyph = destination?.ElementAtOrDefault(4) ?? '\0';
                }
                // Remote gates show all chevrons
                else if (newchevron.HasFlag(Chevron.Nine))
                {
                    newchevron |= Chevron.Four;
                    currentGlyph = destination?.ElementAtOrDefault(3) ?? '\0';
                }
                else if (newchevron.HasFlag(Chevron.Eight))
                {
                    newchevron |= Chevron.Nine;
                    currentGlyph = destination?.ElementAtOrDefault(8) ?? '\0';
                }
                else if (newchevron.HasFlag(Chevron.Three))
                {
                    if (Data.IsRemote)
                    {
                        newchevron |= Chevron.Eight;
                        currentGlyph = destination?.ElementAtOrDefault(7) ?? '\0';
                    }
                    else
                    {
                        newchevron |= Chevron.Four;
                        currentGlyph = destination?.ElementAtOrDefault(3) ?? '\0';
                    }
                }

                else if (newchevron.HasFlag(Chevron.Two))
                {
                    newchevron |= Chevron.Three;
                    currentGlyph = destination?.ElementAtOrDefault(2) ?? '\0';
                }
                else if (newchevron.HasFlag(Chevron.One))
                {
                    newchevron |= Chevron.Two;
                    currentGlyph = destination?.ElementAtOrDefault(1) ?? '\0';
                }
                else if (newchevron == Chevron.None)
                {
                    if (fastDialDest)
                        CheckAndBeginDialingDestination();
                    newchevron |= Chevron.One;
                    currentGlyph = destination?.ElementAtOrDefault(0) ?? '\0';
                }
            }

            Func<int> random = () => m_glyphIndexGenerator.Next(1, Constants.ButtonsToCharacters.Length - 1);

            // If the address wasn't found, and we ran into an invalid character, just pick a random one
            if (currentGlyph == 0 || !Enumerable.Contains(Constants.ButtonsToCharacters, currentGlyph))
                currentGlyph = Constants.ButtonsToCharacters[random()];

            var cachedstate = Data.State;

            Action<MyEntity3DSoundEmitter> callback = null;
            callback = delegate (MyEntity3DSoundEmitter sender)
            {
                sender.StoppedPlaying -= callback;
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    if (!Closed && !MarkedForClose)
                    {
                        if (Data.State != cachedstate)
                            return;                         // If the state changes, stop further processing (ie. outgoing change to incoming)
                        var chevronDiff = Data.Chevron ^ newchevron;
                        Logger.Instance.LogMessage(m_gate.CustomName + string.Format(": Chevron {0} encoded", chevronDiff.ToString()?.Split(',')?.Last()?.Trim() ?? string.Empty));
                        Data.Chevron = newchevron;
                        DialChevron();
                    }
                });
            };

            var shortsound = Constants.Dialing_Sound_Name_SG1_Short;
            var longsound = Constants.Dialing_Sound_Name_SG1_Long;
            var animationPercent = 0.6f;
            var glyphList = Constants.RingSymbolAngles_SG1;

            if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate A"))
            {
                shortsound = Constants.Dialing_Sound_Name_SGA_Short;
                longsound = Constants.Dialing_Sound_Name_SGA_Long;
                animationPercent = 0.8f;
            }
            if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate U"))
            {
                shortsound = Constants.Dialing_Sound_Name_SGU_Short;
                longsound = newchevron == Chevron.One ? Constants.Dialing_Sound_Name_SGU_Long1 : Constants.Dialing_Sound_Name_SGU_Long;
                animationPercent = 0.8f;
                glyphList = Constants.RingSymbolAngles_SGU;
            }

            bool isShort = fastDialDest || Data.IsRemote;
            PlaySound(isShort ? shortsound : longsound, stoppedCallback: callback);

            Logger.Instance.LogDebug(string.Format("isShort: {0}; fastDialDest: {1}", isShort, fastDialDest));
            // Ring animation setup
            if (Ring != null && !isShort)
            {
                try
                {
                    Logger.Instance.LogDebug("Glyph: " + currentGlyph);
                    m_sourceMatrix = Ring.LocalMatrix;
                    m_startingMatrix = m_ringStartingMatrix;
                    var angle = glyphList.ContainsKey(currentGlyph) ? glyphList[currentGlyph] : random();
                    var rot = Quaternion.CreateFromAxisAngle(m_startingMatrix.Forward, angle);

                    if (isShort)
                    {
                        angle = (float)(2 * Math.PI) - 0.01f;
                        rot = Quaternion.CreateFromAxisAngle(m_startingMatrix.Forward, angle);
                        m_animationLength = new TimeSpan(0, 0, 0, 0, (int)Constants.Dialing_Sounds[shortsound] * 9);
                    }
                    else
                    {
                        m_animationLength = new TimeSpan(0, 0, 0, 0, (int)Constants.Dialing_Sounds[longsound]);
                    }
                    rot.Normalize();
                    m_destMatrix = Matrix.Transform(m_startingMatrix, rot);
                    m_destMatrix.Translation = m_startingMatrix.Translation;
                    m_animationProgress = 0;
                    m_startTime = DateTime.Now;
                    m_animationToSoundPercent = animationPercent;
                    m_animationEnabled = true;
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                }
            }
        }

        public void ToggleEmissives()
        {
            if (MyAPIGateway.Session.Player == null)
                return;

            var emissivity = 1.0f;
            var emissivityOff = 0.0f;
            var color = Constants.Chevron_SG1;

            if (!m_gate.IsFunctional)
                color = Constants.Chevron_Off;
            else if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate A"))
                color = Constants.Chevron_SGA;
            else if (m_gate.BlockDefinition.SubtypeId.StartsWith("Stargate U"))
                color = Constants.Chevron_SGU;

            SetEmissives("Chevron1",
                m_gateData.Chevron.HasFlag(Chevron.One) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.One) ? emissivity : emissivityOff);
            SetEmissives("Chevron2",
                m_gateData.Chevron.HasFlag(Chevron.Two) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Two) ? emissivity : emissivityOff);
            SetEmissives("Chevron3",
                m_gateData.Chevron.HasFlag(Chevron.Three) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Three) ? emissivity : emissivityOff);
            SetEmissives("Chevron4",
                m_gateData.Chevron.HasFlag(Chevron.Four) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Four) ? emissivity : emissivityOff);
            SetEmissives("Chevron5",
                m_gateData.Chevron.HasFlag(Chevron.Five) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Five) ? emissivity : emissivityOff);
            SetEmissives("Chevron6",
                m_gateData.Chevron.HasFlag(Chevron.Six) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Six) ? emissivity : emissivityOff);
            // Chevron0 emissive is top center (Chevron.Seven)
            SetEmissives("Chevron0",
                m_gateData.Chevron.HasFlag(Chevron.Seven) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Seven) ? emissivity : emissivityOff);
            SetEmissives("Chevron7",
                m_gateData.Chevron.HasFlag(Chevron.Eight) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Eight) ? emissivity : emissivityOff);
            SetEmissives("Chevron8",
                m_gateData.Chevron.HasFlag(Chevron.Nine) ? color : Constants.Chevron_Off,
                m_gateData.Chevron.HasFlag(Chevron.Nine) ? emissivity : emissivityOff);
        }

        private void SetEmissives(string emissiveName, Color emissivePartColor, float emissivity)
        {
            m_gate.SetEmissiveParts(emissiveName, emissivePartColor, emissivity);
            m_gate.SetEmissivePartsForSubparts(emissiveName, emissivePartColor, emissivity);
        }

        #endregion Dialing Sequence

        #region Update Methods
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (!m_hasBeenDeserialized)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            // This is triggered during the save frame,
            // but immediately clear the serialized data on the next update
            // so the block name stays clean
            if (m_gate == null || !StargateMissionComponent.ModInitialized)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }

            if (!m_bInit && m_gate != null && Globals.ModEnabled && StargateMissionComponent.ModInitialized)
            {
                m_bInit = true;
                CreateTerminalControls();

                if (!Globals.ModEnabled)
                    return;

                m_gate.ShowOnHUDChanged += ShowOnHUDChanged;
                m_gate.OnMarkForClose += OnMarkForClose;
                m_gate.IsWorkingChanged += IsWorkingChanged;
                m_gate.AppendingCustomInfo += AppendingCustomInfo;
                m_gate.OwnershipChanged += OwnershipChanged;
                StateChanged += Stargate_StateChanged;
                //SerializeData();

                GenerateAddress();
                LoadSubparts();
                ToggleEmissives();
            }
            LoadSubparts();

            if (MyAPIGateway.Input.IsAnyCtrlKeyPressed())
            {
                // Don't deserialize if in the middle of a copy/blueprint operation
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }
            DeserializeData();
        }

        private void Stargate_StateChanged(IMyTerminalBlock arg1, GateState arg2)
        {

        }

        public override void UpdateBeforeSimulation()
        {
            if (!Globals.ModEnabled)
                return;

            if (!m_bInit)
                return;

            // Other reactor mods can trigger this update.
            if (m_gate == null)
                return;

            // Check if player position is at event horizon
            // This distance is based on local origin point for the model and player/cockpit
            // A more accurate method would be to constrain the bounds to the plane parallel
            // to the gate and through the center, and within the bounding box of the model.
            if (Data.State == GateState.Active)
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    if (RemoteGate == null)
                    {
                        Logger.Instance.LogAssert(RemoteGate == null, "RemoteGate == null");
                        return;                 // We should never hit here
                    }

                    // If the iris is open, don't allow entering
                    if (m_gate.IsIrisActive())
                        return;

                    // Calculate bounding box, which is the depth of the gate
                    BoundingBoxD gateBB = m_gate.WorldAABB;
                    var gateOBB = CalculateGateBox();

                    // Calculate sphere, which is the radius of the event horizon
                    var sphere = new BoundingSphereD(m_gate.GetPosition(), m_gate.GameLogic.GetAs<Stargate>()?.StargateType == GateType.Supergate ? Constants.SuperGateRadius : Constants.GateRadiusInner);

                    if (m_gate.BlockDefinition.SubtypeId == "Stargate M")
                        sphere.Center = sphere.Center - (m_gate.WorldMatrix.Backward * Constants.BaseGateForwardOffset) + (m_gate.WorldMatrix.Up * Constants.BaseGateUpOffset);
                    else if (m_gate.GetGateType() == GateType.Stargate)
                        sphere.Center = sphere.Center - (m_gate.WorldMatrix.Up * Constants.GateUpOffset);

                    if (StargateAdmin.Configuration.Debug && MyAPIGateway.Session.Player != null)
                    {
                        // Green box is standard BoundingBox for block
                        // Orange box is OrientedBoundingBox for model
                        // Red sphere is event horizon sphere
                        // Draw debug shapes that shows entity detection range
                        // Entities are detected if they are inside both the sphere, and oriented bounding box

                        var color = Color.Orange;
                        var matrix = MatrixD.CreateWorld(Vector3D.Zero);
                        MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref gateBB, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f);
                        MathUtility.DrawOrientedBoundingBox(gateOBB);

                        // Draw debug shapes that shows entity detection range
                        var scolor = Color.Red;
                        var smatrix = MatrixD.CreateWorld(sphere.Center);
                        MySimpleObjectDraw.DrawTransparentSphere(ref smatrix, (float)sphere.Radius, ref scolor, MySimpleObjectRasterizer.SolidAndWireframe, 20);
                    }

                    var updates = new Dictionary<long, Tuple2<ulong, MatrixD, Vector3D>>();
                    var uniqueEntities = MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere);
                    bool destroyedEntity = false;

                    foreach (var entity in uniqueEntities)
                    {
                        if (entity == null)
                            continue;

                        // Exclude invalid ones
                        if (entity.GetTopMostParent().DisplayName == null
                            || entity.GetTopMostParent().DisplayName.StartsWith("Event Horizon at ")
                            || entity.GetTopMostParent().DisplayName.StartsWith("Event Horizon Vortex")
                            || entity.GetTopMostParent().DisplayName == "Iris"
                            || entity.GetTopMostParent().EntityId == m_gate.GetTopMostParent().EntityId
                            || entity.GetTopMostParent().EntityId == RemoteGate.GetTopMostParent().EntityId
                            || ((entity.GetTopMostParent() is IMyCubeGrid)
                                && (entity.GetTopMostParent() as IMyCubeGrid).IsStatic)      // Exclude stations
                            || entity.PositionComp == null || entity.WorldMatrix == null                    // blocks in process of being placed
                            || entity is IMyEngineerToolBase
                            || entity is IMyGunBaseUser
                            || entity.GetTopMostParent().Physics == null
                            || entity.GetTopMostParent()?.PositionComp?.WorldMatrix == null
                            || HasPistonOrRotorTop(entity)                                                  // We'll deal with piston/motor sub-grids later
                            || (m_gate.GetGateType() == GateType.Stargate && !StargateAdmin.Configuration.TeleportGrids)    // If world doesn't allow sending grids through
                            )
                        {
                            //Logger.Instance.LogDebug("Excluding invalid entity: " + entity.GetTopMostParent().DisplayName);
                            continue;
                        }

                        // TODO: Dithering for making blocks invisible as they enter the gate
                        //if (entity is IMyCubeGrid)
                        //{
                        //    // Hide blocks
                        //    var blocks = new List<IMySlimBlock>();
                        //    (entity as IMyCubeGrid).GetBlocks(blocks, (b) =>
                        //    {
                        //        Vector3D pos;
                        //        b.ComputeWorldCenter(out pos);
                        //        return gateOBB.Contains(ref pos);
                        //    });
                        //    blocks.ForEach((b) => b.Dithering = 1.0f);
                        //}

                        // Draw grids nearby
                        if (StargateAdmin.Configuration.Debug && MyAPIGateway.Session.Player != null)
                            MathUtility.DrawOrientedBoundingBox(MathUtility.CreateOrientedBoundingBox(entity), Color.LightBlue);

                        // Create a small box which will trigger detection before a single point will
                        // This will make checking which side the point is on more accurately
                        var entityPos = entity.GetTopMostParent().PositionComp.WorldAABB.Center;
                        var direction = entity.WorldMatrix.Forward;

                        if (entity.Physics.LinearVelocity != Vector3D.Zero)
                            direction = entity.Physics.LinearVelocity;

                        direction.Normalize();
                        var extents = new Vector3D(0.5);

                        var entityobb = new MyOrientedBoundingBoxD(entityPos, extents, Quaternion.CreateFromAxisAngle(direction, 0));

                        if (StargateAdmin.Configuration.Debug && MyAPIGateway.Session.Player != null)
                            MathUtility.DrawOrientedBoundingBox(entityobb);

                        // Check if entity intersects
                        if (gateOBB.Contains(ref entityobb) == ContainmentType.Disjoint)
                            continue;

                        if (sphere.Contains(entityPos) != ContainmentType.Contains)
                            continue;

                        // Don't enter the gate if the ship is too large
                        double smallestSide = entity.GetTopMostParent().PositionComp.WorldAABB.Size.Min();
                        double gateSize = Math.Max(Math.Max(m_gate.PositionComp.WorldAABB.Size.X, m_gate.PositionComp.WorldAABB.Size.Y), m_gate.PositionComp.WorldAABB.Size.Z);

                        if (StargateAdmin.Configuration.Debug)
                            ShowMessageToUsersInRange(string.Format("Smallest side: {0:F1}, gate: {1:F1}", smallestSide, gateSize, 5000));
                        Logger.Instance.LogMessage(string.Format("Smallest side: {0:F1}, gate: {1:F1}", smallestSide, gateSize));

                        if (smallestSide > gateSize)
                            continue;

                        if (StargateAdmin.Configuration.Debug)
                            ShowMessageToUsersInRange("Object entering gate: " + entity.GetTopMostParent().DisplayName);
                        Logger.Instance.LogMessage("Object entering gate: " + entity.GetTopMostParent().DisplayName);

                        // This try...catch must be inside the foreach loop
                        // that way all the objects will continue to get processed.
                        // We don't want a state where only part of a ship got moved
                        try
                        {
                            if (Data.IsRemote || RemoteGate.IsIrisActive())
                            {
                                IMyPlayer player = MyAPIGateway.Players.GetPlayerControllingEntity(entity);

                                // Check if destination iris/shield is active
                                // Kill the player at the source, to remove possible respawn exploits
                                if (!Data.IsRemote && RemoteGate.OwnerId != 0 &&
                                    IsAllowedViaGDO(player, RemoteGate as IMyTerminalBlock))
                                {
                                    // Player owns, or is friendly with, owning faction of destination gate, disable iris
                                    // Unowned gates will never auto-open the iris
                                    RemoteGate.GetActionWithName("Phoenix." + Globals.ModName + ".Iris")?.Apply(RemoteGate);

                                    if (MoveAndOrientObject(m_gate, RemoteGate, entity.GetTopMostParent(), updates) || Data.AutoCloseWithAll)
                                    {
                                        m_gateAutoCloseTimer.Stop();
                                        m_gateAutoCloseTimer.Interval = Data.AutoCloseTime * 1000;
                                        m_gateAutoCloseTimer.Start();
                                        //WasUsed = true;                             // Close the gate if someone enters
                                    }
                                }
                                else
                                {
                                    // Check if the entity was recently transferred
                                    // And if so, give a safe time to move out of the way of the gate before they die
                                    // This is so they don't immediately die when exiting due to MP lag
                                    if (m_movedEntities.Where(e => e.Entity.EntityId == entity.EntityId && e.Expires > DateTime.Now).Count() > 30)
                                        continue;

                                    if (entity is IMyCharacter)
                                    {
                                        if (player == null || (player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity.EntityId != entity.EntityId))
                                        {
                                            if (entity.SyncObject != null)
                                                MyEntities.SendCloseRequest(entity);
                                            else
                                                entity.Close();
                                        }
                                        else
                                        {
                                            if (StargateAdmin.Configuration.Debug)
                                                MyAPIGateway.Utilities.ShowNotification("killed player: " + entity.DisplayName, 1000);

                                            if ((entity as IMyCharacter).Integrity > 0)
                                                destroyedEntity = true;

                                            Logger.Instance.LogMessage("killed player: " + entity.DisplayName);
                                            MyDamageInformation damageInfo = new MyDamageInformation();
                                            damageInfo.Amount = 999999;
                                            damageInfo.Type = MyDamageType.Destruction;
                                            (entity as IMyCharacter).Kill(damageInfo);
                                        }
                                    }
                                    else
                                    {
                                        // DS comes here for client player entity
                                        if (player != null && MyAPIGateway.Multiplayer.IsServerPlayer(player.Client))
                                        {
                                            // This is a server owned object
                                            // Also single player
                                            if (StargateAdmin.Configuration.Debug)
                                                MyAPIGateway.Utilities.ShowNotification("destroyed server object: " + (string.IsNullOrEmpty(entity.DisplayName) ? entity.Name : entity.DisplayName), 5000);
                                            Logger.Instance.LogMessage("destroyed server object: " + entity.DisplayName);
                                            if (entity.SyncObject != null)
                                                MyEntities.SendCloseRequest(entity);
                                            else
                                                entity.Close();
                                            destroyedEntity = true;
                                        }
                                        else
                                        {
                                            // Need to know if the entity is the ControlledEntity of another client
                                            // If so, do NOT delete it on the server, only the client
                                            List<IMyPlayer> players = new List<IMyPlayer>();
                                            MyAPIGateway.Players.GetPlayers(players, (x) => x.Controller.ControlledEntity != null &&
                                                x.Controller.ControlledEntity.Entity != null &&
                                                x.Controller.ControlledEntity.Entity.EntityId == entity.EntityId);

                                            if (players.Count != 0)
                                            {
                                                // This entity is another player
                                                // Only delete it on that client
                                                if (MyAPIGateway.Session.Player != null && !MyAPIGateway.Multiplayer.IsServer)
                                                {
                                                    if (StargateAdmin.Configuration.Debug)
                                                        MyAPIGateway.Utilities.ShowNotification("destroyed player object: " + (string.IsNullOrEmpty(entity.DisplayName) ? entity.Name : entity.DisplayName), 5000);
                                                    Logger.Instance.LogMessage("destroyed player object: " + entity.DisplayName);

                                                    if (entity.SyncObject != null)
                                                        MyEntities.SendCloseRequest(entity);
                                                    else
                                                        entity.Close();
                                                    destroyedEntity = true;
                                                }
                                            }
                                            else
                                            {
                                                // It is not a direct player entity, safe to delete everywhere
                                                if (StargateAdmin.Configuration.Debug)
                                                    MyAPIGateway.Utilities.ShowNotification("destroyed client object: " + (string.IsNullOrEmpty(entity.DisplayName) ? entity.Name : entity.DisplayName), 5000);
                                                Logger.Instance.LogMessage("destroyed client object: " + entity.DisplayName);

                                                if (entity.SyncObject != null)
                                                    MyEntities.SendCloseRequest(entity);
                                                else
                                                    entity.Close();
                                                destroyedEntity = true;
                                            }
                                        }
                                    }
                                }
                                // Stop here, do not try to move the player, even in creative
                                continue;
                            }

                            if (RemoteGate.GameLogic.GetAs<Stargate>().m_remoteServer)
                            {
                                Logger.Instance.LogDebug("Sending player to: " + RemoteGate.GetTopMostParent().DisplayName);
                                MyAPIGateway.Multiplayer.JoinServer(RemoteGate.GetTopMostParent().DisplayName);
                            }
                            else if (MoveAndOrientObject(m_gate, RemoteGate, entity.GetTopMostParent(), updates) || Data.AutoCloseWithAll)
                            {
                                m_gateAutoCloseTimer.Stop();
                                m_gateAutoCloseTimer.Interval = Data.AutoCloseTime * 1000;
                                m_gateAutoCloseTimer.Start();
                                //WasUsed = true;                             // Close the gate if someone enters
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogException(ex);
                        }
                        finally
                        {
                            Logger.Instance.IndentLevel = 0;
                        }
                    }

                    if (updates.Count > 0)
                    {
                        bool dampers = false;

                        if (RemoteGate?.GetTopMostParent()?.Physics != null)
                            dampers = RemoteGate.GetTopMostParent().Physics.LinearVelocity != Vector3D.Zero;

                        SendPositionUpdates(updates, dampers);
                    }

                    if (destroyedEntity)
                        m_remoteGate?.PlaySound(m_remoteGate?.GameLogic.GetAs<Stargate>()?.m_gateEdition == GateEdition.Third ? "Stargate_Hit_Shield" : "Stargate_Hit_Iris", sync: true);
                }
            }
            else if (Data.State == GateState.Dialing)
            {
                RotateRing();
            }
        }

        Vector3D cachedPosition;
        private string GenerateAddress()
        {
            if (m_gate.Physics == null && (m_gate.CubeGrid as MyCubeGrid).Projector != null)
                return null;

            if (cachedPosition == m_gate.PositionComp.GetPosition())
                return m_gateAddress;

            cachedPosition = m_gate.PositionComp.GetPosition();

            var location = new ulong[]
            {
                    (ulong)(Math.Round(cachedPosition.X / StargateAdmin.Configuration.GateInfluenceRadius, MidpointRounding.AwayFromZero) * StargateAdmin.Configuration.GateInfluenceRadius),
                    (ulong)(Math.Round(cachedPosition.Y / StargateAdmin.Configuration.GateInfluenceRadius, MidpointRounding.AwayFromZero) * StargateAdmin.Configuration.GateInfluenceRadius),
                    (ulong)(Math.Round(cachedPosition.Z / StargateAdmin.Configuration.GateInfluenceRadius, MidpointRounding.AwayFromZero) * StargateAdmin.Configuration.GateInfluenceRadius)
            };

            //// For each vector element that's negative, add a bit
            //// This will be use to shift the address hash a variable number of characters,
            //// depending on the number of negatives.
            //// This prevents negative coordinates from having the same address as positive ones.
            //// Negative numbers will be converted to positive by casting.
            //var skipNegative = 0;
            //if (location[0] < 0)
            //{
            //    skipNegative += 1 << 1;
            //    location[0] = Math.Abs(location[0]);
            //}
            //if (location[1] < 0)
            //{
            //    skipNegative += 1 << 2;
            //    location[1] = Math.Abs(location[1]);
            //}
            //if (location[2] < 0)
            //{
            //    skipNegative += 1 << 3;
            //    location[2] = Math.Abs(location[2]);
            //}

            Logger.Instance.LogMessage("Hashing location: " + cachedPosition.ToString());
            Logger.Instance.LogMessage(string.Format("Hashing rounded: {{X:{0}, Y:{1}, Z:{2}}}", location[0], location[1], location[2]));
            if(StargateMissionComponent.Instance?.GateAddressHasher == null)
                Logger.Instance.LogMessage("GateAddressHasher null");

            //var array = StargateMissionComponent.Instance?.GateAddressHasher?.EncodeLong(location)?.ToCharArray()?.Distinct()?.Skip(skipNegative)?.Take(6)?.ToArray();
            var array = StargateMissionComponent.Instance?.GateAddressHasher?.EncodeLong(location)?.ToCharArray()?.Distinct()?.Take(6)?.ToArray();
            var str = StargateMissionComponent.Instance?.GateAddressHasher?.EncodeLong(location);
            //Logger.Instance.LogDebug(string.Format("Generated hash: {0}", str));
            if (array != null)
            {
                if (array.Length < 6)
                {
                    // Length was too short, use alternate method
                    //array = StargateMissionComponent.Instance?.GateAddressHasherAlternate?.EncodeLong(location)?.ToCharArray()?.Distinct()?.Skip(skipNegative)?.Take(6)?.ToArray();
                    array = StargateMissionComponent.Instance?.GateAddressHasherAlternate?.EncodeLong(location)?.ToCharArray()?.Distinct()?.Take(6)?.ToArray();
                    str = StargateMissionComponent.Instance?.GateAddressHasherAlternate?.EncodeLong(location);
                    Logger.Instance.LogDebug(string.Format("Generated alternate hash: {0}", str));
                    Logger.Instance.LogAssert(array != null, "array != null");
                }

                var newaddress = new StringBuilder().Append(array).ToString();
                if (newaddress.CompareTo(m_gateAddress) != 0)
                {
                    Logger.Instance.LogDebug(string.Format("Generated hash: {0}", str));
                    m_gateAddress = newaddress;
                    if (!string.IsNullOrEmpty(m_gateAddress))
                    {
                        SendNewGate();
                    }
                }
            }

            return m_gateAddress;
        }

        public void SendNewGate(bool remove = false)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MessageUtils.SendMessageToAll(new MessageGateEvent()
                {
                    Address = Address,
                    EntityId = m_gate.EntityId,
                    GateName = m_gate.CustomName,
                    GridName = m_gate.CubeGrid.DisplayName,
                    GateType = m_gate.GetGateType(),
                    OwnerId = m_gate.OwnerId,
                    ShareMode = m_gate.GetObjectBuilderCubeBlock().ShareMode,
                    Remove = remove
                });
            }
        }
        public override void UpdateBeforeSimulation100()
        {
            try
            {
                GenerateAddress();
                m_gate.RefreshCustomInfo();

                if (MyAPIGateway.Multiplayer?.IsServer != true)
                    return;

                // Clear out expired entities from list
                m_movedEntities = m_movedEntities.Where(e => e.Entity?.Closed != true && !e.Entity.MarkedForClose && e.Expires > DateTime.Now).ToList();

                if (EventHorizon != null && MyAPIGateway.Session.IsServer && (!Data.IsRemote && m_eventcounter++ % 5 == 0))
                {
                    //MyEntitySubpart subpart;
                    //EventHorizon.TryGetSubpart(Constants.EventHorizonSubpartName, out subpart);
                    var grid = m_gate?.Hierarchy?.Children.ElementAtOrDefault(0)?.Container?.Entity as IMyCubeGrid;

                    if (grid != null)
                    {
                        try
                        {
                            var antennas = new List<IMySlimBlock>();
                            (grid as IMyCubeGrid).GetBlocks(antennas, (x) => x.FatBlock != null && x.FatBlock is IMyLaserAntenna);
                            if (antennas.Count == 2)
                            {
                                var ant1 = antennas[0].FatBlock as IMyLaserAntenna;
                                var ant2 = antennas[1].FatBlock as IMyLaserAntenna;

                                if (string.IsNullOrEmpty(ant1.DetailedInfo) || string.IsNullOrEmpty(ant2.DetailedInfo))
                                    return;

                                if (!ant1.DetailedInfo.Contains("Connected to") && ant2.DetailedInfo.Contains("Searching for a laser"))
                                {
                                    ant2.GetActionWithName("OnOff_Off").Apply(ant2);
                                    ant1.GetActionWithName("OnOff_On").Apply(ant1);
                                }

                                if (!ant2.DetailedInfo.Contains("Connected to") && ant1.DetailedInfo.Contains("Searching for a laser"))
                                {
                                    ant1.GetActionWithName("OnOff_Off").Apply(ant1);
                                    ant2.GetActionWithName("OnOff_On").Apply(ant2);
                                }
                            }
                        }
                        catch (Exception ex) { Logger.Instance.LogException(ex); }
                    }
                }
            }
            catch { }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (!Globals.ModEnabled)
                return;

            // Other reactor mods can trigger this update.
            if (m_gate == null)
                return;

            if (Data.State == GateState.Active && !m_gate.IsFunctional)
            {
                // Closing gate, inform clients
                //DoorDHDExtensions.SendMessage(MessageType.CloseGate, m_gate, RemoteGate);
                ResetGate();
            }
        }
        #endregion Update Methods

        #region Misc
        private void LoadSubparts()
        {
            try
            {
                Logger.Instance.LogAssert(m_gate != null, "m_gate != null");
                Logger.Instance.LogAssert((m_gate as MyEntity).Subparts != null, "(m_gate as MyEntity).Subparts != null");

                if ((m_gate as MyEntity).Subparts == null)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => LoadSubparts());
                    return;
                }

                // If the model contains the iris or event horizon subpart dummies load them
                // If not, create the subparts dynamically
                MyEntitySubpart subpart;
                if (IrisShield == null || IrisShield.MarkedForClose || IrisShield.Closed)
                {
                    if (m_gate.TryGetSubpart(Constants.IrisName, out subpart))
                    {
                        IrisShield = subpart;
                        IrisShield.DisplayName = Constants.IrisName;
                    }
                    else if (m_gate.TryGetSubpart(Constants.ShieldName, out subpart))
                    {
                        IrisShield = subpart;
                        IrisShield.DisplayName = Constants.ShieldName;
                    }
                    else
                    {
                        IrisShield = SpawnEventHorizon(true);
                    }
                }
                if (IrisShield != null)
                {
                    Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(IrisShield);
                    //Sandbox.Engine.Physics.MyPhysicsHelper.InitModelPhysics(IrisShield, RigidBodyFlag.RBF_STATIC, 18);
                    if ((m_gate as MyEntity).Subparts.ContainsKey(Constants.IrisName))
                        IrisShield.DisplayName = Constants.IrisName;
                    else
                        IrisShield.DisplayName = Constants.ShieldName;

                    if (!Data.IrisActive)
                    {
                        IrisShield.Visible = false;
                        IrisShield.Physics.Deactivate();
                    }
                    IrisShield.OnClose += Subpart_OnClose;
                }

                if (EventHorizon == null || EventHorizon.MarkedForClose || EventHorizon.Closed)
                {
                    if (m_gate.TryGetSubpart(Constants.EventHorizonSubpartName, out subpart))
                    {
                        EventHorizon = subpart;
                        EventHorizon.DisplayName = Constants.EventHorizonSubpartName;
                        Logger.Instance.LogDebug("Loaded subpart: " + EventHorizon.DisplayName);
                    }
                    else
                    {
                        EventHorizon = SpawnEventHorizon(false, true);
                    }
                }
                if (EventHorizon != null)
                {
                    if (Data.State == GateState.Active)
                        SpawnEventHorizon(false);
                    else
                        EventHorizon.Visible = false;

                    EventHorizon.OnClose += Subpart_OnClose;
                }

                if( Ring != null)
                {
                    m_ringStartingMatrix = Ring.LocalMatrix;
                    Ring.DisplayName = Constants.RingSubpartName;
                    Ring.Visible = true;
                    Ring.OnClose += Subpart_OnClose;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(ex.Message);
                Logger.Instance.LogMessage(ex.StackTrace);
            }
        }

        private void Subpart_OnClose(IMyEntity obj)
        {
            if (obj == m_EventHorizon)
            {
                m_EventHorizon = null;
            }
            else if (obj == m_IrisShield)
            {
                m_IrisShield = null;
            }
            MyAPIGateway.Utilities.InvokeOnGameThread(() => LoadSubparts());
        }

        private void ResetTimer()
        {
            var timer = new Timer();
            timer.AutoReset = false;
            m_callbackTimer.Stop();
            m_callbackTimer.Close();
            m_callbackTimer = timer;
        }

        public void AddSafeEntity(IMyEntity entity)
        {
            m_movedEntities.Add(new EntityExpire() { Entity = entity, Expires = DateTime.Now.AddSeconds(Constants.ExitSafeTime) });
        }

        bool IsAllowedViaGDO(IMyPlayer player, IMyTerminalBlock gate)
        {
            // If we have a problem, assume we are not allowed
            if (player == null || gate == null)
                return false;

            return gate.HasPlayerAccess(player.IdentityId);
        }

        public void ResetGate(bool force = false)
        {
            if (m_gate == null)
                return;

            if (!force && Data.IsRemote && Data.State == GateState.Active &&
                RemoteGate?.GameLogic.GetAs<Stargate>()?.Data.State == GateState.Active)
                return;

            if (MyAPIGateway.Multiplayer.IsServer)
                MessageUtils.SendMessageToAllPlayers(new MessageReset() { Gate = m_gate.EntityId, Force = force });

            if (m_gateData.State == GateState.Active)
                PlaySound("StargateShutdown", true);

            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            m_gateLifetime.Stop();
            m_gateAutoCloseTimer.Stop();
            ResetTimer();
            m_gateData.ActivatingPlayerId = 0;
            m_gateData.Chevron = Chevron.None;
            m_gateData.IsRemote = false;
            m_gateData.State = GateState.Idle;
            var remote = RemoteGate;
            RemoteGate = null;
            m_gateData.DestinationEntityId = 0;
            remote?.GameLogic?.GetAs<Stargate>()?.ResetGate(true);

            RemoveEventHorizon();
            //PlaySound(null);
            ActivateBlocks();
        }

        private void ShowMessageToUsersInRange(string message, int time = 2000)
        {
            Utils.ShowMessageToUsersInRange(m_gate, message, time);
        }
        #endregion Misc

        #region Movement
        public MyOrientedBoundingBoxD CalculateGateBox()
        {
            return CalculateGateBox(m_gate);
        }

        // NOTE: This depends on the actual models. If those change, revisit this
        public static MyOrientedBoundingBoxD CalculateGateBox(IMyEntity gate)
        {
            var gatelBB = (gate as Sandbox.Game.Entities.MyCubeBlock).GetGeometryLocalBox();
            var gateOBB = MathUtility.CreateOrientedBoundingBox(gate as IMyCubeBlock, gatelBB.Extents);

            if (gate is IMyCubeBlock && (gate as IMyCubeBlock).BlockDefinition.SubtypeId == "Stargate M")
            {
                gateOBB.Center = gateOBB.Center + (gateOBB.Orientation.Forward * Constants.BaseGateForwardOffsetSphere);
                gateOBB.HalfExtent = new Vector3D(Constants.GateRadiusOuter, Constants.GateRadiusOuter, Constants.GateDepthHalf);
            }

            return gateOBB;
        }

        public void SendPositionUpdates(Dictionary<long, Tuple2<ulong, MatrixD, Vector3D>> updates, bool turnOffDampers)
        {
            var entitiesByPlayer = new Dictionary<ulong, List<Tuple2<long, MatrixD, Vector3D>>>();

            // Reorganize entities by player
            foreach (var entity in updates)
            {
                var player = entity.Value.Item1;

                if (!entitiesByPlayer.ContainsKey(player))
                    entitiesByPlayer.Add(player, new List<Tuple2<long, MatrixD, Vector3D>>());

                entitiesByPlayer[player].Add(MyTuple.Create(entity.Key, entity.Value.Item2, entity.Value.Item3));
            }

            foreach (var player in entitiesByPlayer)
            {
                var message = new MessageMove()
                {
                    destinationGate = RemoteGate.EntityId,
                    sourceGate = m_gate.EntityId,
                    positions = player.Value,
                    TurnOffDampers = turnOffDampers
                };
                MessageUtils.SendMessageToServer(message);
                Logger.Instance.LogDebug("Sending update to: " + player.Key);
                MessageUtils.SendMessageToPlayer(player.Key, message);
            }
        }

        public static void BumpEntityForward(IMyEntity objToMove, IMyEntity destinationReference, Vector3D relVelocity)
        {
            Vector3D bump = objToMove.PositionComp.GetPosition();
            var destOBB = CalculateGateBox(destinationReference);
            var obj = objToMove.WorldAABB;

            //while (destinationReference.WorldAABB.Intersects(objToMove.WorldAABB))
            while (destOBB.Intersects(ref obj))
            {
                if (relVelocity != Vector3.Zero)
                {
                    // If the object is already moving, use its velocity direction
                    bump += (Vector3D.Normalize(relVelocity));
                }
                else
                {
                    if (objToMove.Physics.LinearAcceleration != Vector3.Zero)
                        // If there's at least an acceleration vector, use that
                        bump += (objToMove.Physics.LinearAcceleration * 1f);
                    else
                        // Otherwise just move in front of the gate (risk of death when walking through the gate!)
                        bump += (destinationReference.PositionComp.WorldMatrix.Backward * 1f);
                }
                objToMove.PositionComp.SetPosition(bump);
                obj = objToMove.WorldAABB;
            }
        }

        /// <summary>
        /// Move the player or object to a destination, relative to a gate
        /// </summary>
        /// <param name="sourceReference">Source reference object</param>
        /// <param name="destinationReference">Destination reference object</param>
        /// <param name="objToMove">Entity to move (player or ship)</param>
        /// <returns>true if player was moved, false if not</returns>
        public bool MoveAndOrientObject(IMyEntity sourceReference, IMyEntity destinationReference, IMyEntity objToMove, Dictionary<long, Tuple2<ulong, MatrixD, Vector3D>> updates)
        {
            if (sourceReference == null || destinationReference == null || objToMove == null)
                return false;

            var relVelocity = Vector3D.Zero;

            // We need to get the velocity of the player/object, relative to the source gate
            // Then set the player/object's velocity to that of the destination gate plus the relative calculated before.

            if (objToMove.Physics != null)
                relVelocity = objToMove.Physics.LinearVelocity;

            Logger.Instance.LogDebug("Gate Velocity: " + (sourceReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity.Length());

            if ((sourceReference.GetTopMostParent() as IMyCubeGrid).Physics != null)
                relVelocity -= (sourceReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity;

            Logger.Instance.LogDebug("Relative Velocity: " + relVelocity.Length());
            //if( MyAPIGateway.Session.Player != null )
            //    MyAPIGateway.Session.Player.AddGrid(objToMove.EntityId);

            bool playerWasMoved = false;
            // This is (usually) better
            // This uses quaternions to angle the ship according to it's orientation relative to the gate it's entering
            // Note, since we want to exit the destination gate looking like we passed straight through the ring
            // We need to treat the source gate as if we went through the back, otherwise Left/right will be flipped
            var mat = sourceReference.PositionComp.WorldMatrix;
            var savedPlayerMatrix = objToMove.PositionComp.WorldMatrix;

            // Apply orientation and set position
            var newObjMatrix = CalculateDestinationMatrix(sourceReference.PositionComp.WorldMatrix, destinationReference.PositionComp.WorldMatrix, objToMove, ref relVelocity);
            var savedObjPosition = objToMove.WorldMatrix;

            var objOBB = MathUtility.CreateOrientedBoundingBox(objToMove);
            var sourceOBB = CalculateGateBox(sourceReference);
            var destOBB = CalculateGateBox(destinationReference);
            var direction = destinationReference.PositionComp.WorldMatrix.Backward;

            // Figure out which side of the gate it's on, and place the entity on the correct side
            if (relVelocity != Vector3D.Zero)
            {
                var plane = new PlaneD(sourceOBB.Center, -sourceOBB.Orientation.Forward);

                // Value is negative if entering from the 'back' of the gate, positive if front.
                if (plane.DistanceToPoint(objOBB.Center + (2 * relVelocity)) < 0)
                    direction = destinationReference.PositionComp.WorldMatrix.Forward;
            }

            newObjMatrix.Translation = newObjMatrix.Translation + (direction * objOBB.HalfExtent.Max());

            // Adjust for the height difference between regular and the base gates
            if ((sourceReference as IMyTerminalBlock)?.BlockDefinition.SubtypeId == "Stargate M")
                newObjMatrix.Translation -= sourceOBB.Orientation.Up * (Constants.BaseGateDifference - 0.1f);       // TODO remove 0.1 when collision model is fixed

            if ((destinationReference as IMyTerminalBlock)?.BlockDefinition.SubtypeId == "Stargate M")
                newObjMatrix.Translation += destOBB.Orientation.Up * Constants.BaseGateDifference;

            Vector3D newVel = relVelocity;

            // Set object velocity
            if (objToMove.Physics != null)
            {
                if ((destinationReference.GetTopMostParent() as IMyCubeGrid).Physics != null)
                {
                    Logger.Instance.LogDebug("Dest Velocity: " + (destinationReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity.Length());
                    newVel += (destinationReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity;
                }
                Logger.Instance.LogDebug("New object Velocity: " + newVel.Length());
            }

            // Save new information for updating later
            var player = MyAPIGateway.Players.GetPlayerControllingEntity(objToMove);
            if (player == null)
            {
                Logger.Instance.LogMessage(string.Format("local update {0} to: {1:F0}, {2:F0}, {3:F0}", objToMove.DisplayName, newObjMatrix.Translation.X, newObjMatrix.Translation.Y, newObjMatrix.Translation.Z));

                // Prevent cluster split from making ships disappear (or otherwise act wonky)
                BoundingBoxD aggregatebox = sourceReference.WorldAABB;
                var box = destinationReference.PositionComp.WorldAABB;
                aggregatebox.Include(ref box);
                MyAPIGateway.Physics.EnsurePhysicsSpace(aggregatebox);

                objToMove.PositionComp.SetWorldMatrix(newObjMatrix);
                objToMove.PositionComp.SetPosition(newObjMatrix.Translation);

                if (objToMove.Physics != null)
                    objToMove.Physics.LinearVelocity = newVel;

                // Keep moving forward until we no longer collide with the gate
                BumpEntityForward(objToMove, destinationReference, relVelocity);

                //if( objToMove.SyncObject != null )
                //    objToMove.SyncObject.UpdatePosition();
            }
            else
            {
                Logger.Instance.LogMessage(string.Format("remote update {0} to: {1:F0}, {2:F0}, {3:F0}", objToMove.DisplayName, newObjMatrix.Translation.X, newObjMatrix.Translation.Y, newObjMatrix.Translation.Z));

                if (!updates.ContainsKey(objToMove.EntityId))
                    updates.Add(objToMove.EntityId, MyTuple.Create(player.SteamUserId, newObjMatrix, newVel));
            }

            (sourceReference as IMyTerminalBlock)?.PlaySound("Stargate_Enter_Wormhole", sync: true);
            (destinationReference as IMyTerminalBlock)?.PlaySound("Stargate_Enter_Wormhole", sync: true);

            // Now move piston/rotor subgrids
            // If the player passed through, keep track of that, unless it was a remote controlled drone
            //if (MoveSubGrids(objToMove, savedPlayerMatrix, newObjMatrix, (sourceReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity, (destinationReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity, updates) ||
            //    (MyAPIGateway.Session.Player != null && (objToMove.GetTopMostParent().EntityId == MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent().EntityId &&
            //    !(MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity is VRage.Game.ModAPI.IMyCubeBlock &&
            //    (MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.IMyCubeBlock).BlockDefinition.TypeIdString == "MyObjectBuilder_RemoteControl"))))
            //    playerWasMoved = true;
            if (MoveSubGrids(objToMove, savedPlayerMatrix, newObjMatrix, (sourceReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity, (destinationReference.GetTopMostParent() as IMyCubeGrid).Physics.LinearVelocity, updates) ||
                (player != null && (!(player.Controller.ControlledEntity.Entity is IMyCubeBlock) ||
                                    ((player.Controller.ControlledEntity.Entity is IMyCubeBlock) &&
                                        (player.Controller.ControlledEntity.Entity as IMyCubeBlock).BlockDefinition.TypeIdString != "MyObjectBuilder_RemoteControl"))))
                playerWasMoved = true;

            return playerWasMoved;
        }
        /// <summary>
        /// Given a source reference point, calculate the new position and orientation relative to the destination
        /// </summary>
        /// <param name="sourceReference"></param>
        /// <param name="destinationReference"></param>
        /// <param name="objToMove"></param>
        /// <param name="velocity">new velocity unit vector</param>
        /// <param name="isGate">If true, reverse forward and backward directions of gate</param>
        /// <returns>new matrix representing translation at destination point</returns>
        private MatrixD CalculateDestinationMatrix(MatrixD sourceReference, MatrixD destinationReference, IMyEntity objToMove, ref Vector3D velocity, bool isGate = true)
        {
            // This uses quaternions to angle the ship according to it's orientation relative to the gate it's entering
            // Note, since we want to exit the destination gate looking like we passed straight through the ring
            // We need to treat the source gate as if we went through the back, otherwise Left/right will be flipped
            var mat = sourceReference;
            var savedPlayerMatrix = objToMove.PositionComp.WorldMatrix;

            // Get quaternions that represent the rotations of the gates
            var srcGateQuat = Quaternion.CreateFromForwardUp((isGate ? mat.Backward : mat.Forward), mat.Up);
            var dstGateQuat = Quaternion.CreateFromForwardUp(destinationReference.Forward, destinationReference.Up);
            var newObjMatrix = objToMove.WorldMatrix;

            // Find the rotational difference between the source and destination gates
            var srcInvQuat = srcGateQuat;
            srcInvQuat.Conjugate();
            var gateRot = dstGateQuat * srcInvQuat;

            // Set object orientation (does not change velocity vector)!
            newObjMatrix = MatrixD.Transform(newObjMatrix, gateRot);

            // Calculate new velocity vector
            velocity = Vector3.Transform(velocity, gateRot);

            //// Calculate position
            // Get the world to local translation for the source gate
            var transMat = sourceReference;
            //var transMat = (isGate ? VRageMath.Matrix.CreateRotationX(0) : sourceReference);
            transMat.Translation = sourceReference.Translation;
            transMat = MatrixD.Invert(transMat);

            // Convert the object to a local reference point to the source gate
            var pos = ConvertPointReference(objToMove.PositionComp.WorldMatrix.Translation, transMat);

            // Get back to a world reference, but relative to the destination source
            pos = ConvertPointReference(pos, destinationReference);

            // Apply orientation and set position
            newObjMatrix.Translation = pos;

            return newObjMatrix;
        }

        public static bool HasPistonOrRotorTop(IMyEntity obj)
        {
            // This could be optimized, since we only care if there are ANY, 
            // we only need to know if at least one exists.
            Dictionary<long, long> list = GetPistonOrRotorTops(obj);

            return (list.Count > 0);
        }

        /// <summary>
        /// This function gets a list of motor/piston 'tops'
        /// </summary>
        /// <param name="obj">Grid to search</param>
        /// <returns></returns>
        public static Dictionary<long, long> GetPistonOrRotorTops(IMyEntity obj)
        {
            List<IMySlimBlock> list = new List<IMySlimBlock>();
            Dictionary<long, long> topParts = new Dictionary<long, long>();

            if (obj.GetTopMostParent() is IMyCubeGrid)
            {
                (obj.GetTopMostParent() as IMyCubeGrid).GetBlocks(list, (x) => x.FatBlock != null && (x.FatBlock is IMyMotorRotor || x.FatBlock is IMyPistonTop));
            }

            foreach (var entity in list)
            {
                //if (entity.FatBlock.BlockDefinition.TypeId == new MyObjectBuilderType(typeof(MyObjectBuilder_PistonTop)) ||
                //Logger.Instance.LogDebug(string.Format("Found top: {0}, {1}", (entity.FatBlock as IMyCubeBlock).DisplayNameText, entity.FatBlock.EntityId));
                long? baseId = 0;

                if (entity.FatBlock is IMyPistonTop)
                    baseId = (entity.FatBlock as IMyPistonTop).Piston?.EntityId;
                else if (entity.FatBlock is IMyMotorRotor)
                    baseId = (entity.FatBlock as IMyMotorRotor).Stator?.EntityId;

                if (baseId > 0)
                    topParts.Add(entity.FatBlock.EntityId, baseId ?? 0);
            }
            return topParts;
        }

        /// <summary>
        /// This retrieves all the piston/rotor/wheel bases on a ship
        /// </summary>
        /// <param name="obj">Parent cubegrid</param>
        /// <returns></returns>
        public static Dictionary<long, long> GetPistonOrRotorBase(IMyEntity obj)
        {
            List<IMySlimBlock> list = new List<IMySlimBlock>();
            Dictionary<long, long> baseParts = new Dictionary<long, long>();

            if (obj.GetTopMostParent() is IMyCubeGrid)
            {
                (obj.GetTopMostParent() as IMyCubeGrid).GetBlocks(list, (x) => x.FatBlock is IMyPistonBase || x.FatBlock is IMyMotorBase || x.FatBlock is IMyShipConnector);
            }

            foreach (var entity in list)
            {
                //Logger.Instance.LogDebug("Found item: " + entity.FatBlock.BlockDefinition.TypeId.ToString());
                long? topId = null;
                Logger.Instance.LogDebug("Found base: " + entity.FatBlock.GetType().ToString());

                if (entity.FatBlock is IMyPistonBase)
                    topId = (entity.FatBlock as IMyPistonBase).Top?.EntityId;
                else if (entity.FatBlock is IMyMotorBase)
                    topId = (entity.FatBlock as IMyMotorBase).Rotor?.EntityId;
                else if (entity.FatBlock is IMyShipConnector)
                    topId = (entity.FatBlock as IMyShipConnector).OtherConnector?.EntityId;

                Logger.Instance.LogDebug("Connected to: " + topId);

                baseParts.Add(entity.FatBlock.EntityId, topId ?? 0);
            }

            return baseParts;
        }
        internal static VRageMath.Vector3 ConvertPointReference(VRageMath.Vector3 point, VRageMath.Matrix refMatrix)
        {
            VRageMath.Vector3 result = new VRageMath.Vector3();

            result.X = (point.X * refMatrix.M11) + (point.Y * refMatrix.M21) + (point.Z * refMatrix.M31) + refMatrix.M41;
            result.Y = (point.X * refMatrix.M12) + (point.Y * refMatrix.M22) + (point.Z * refMatrix.M32) + refMatrix.M42;
            result.Z = (point.X * refMatrix.M13) + (point.Y * refMatrix.M23) + (point.Z * refMatrix.M33) + refMatrix.M43;

            return result;
        }
        internal static Vector3D ConvertPointReference(Vector3D point, MatrixD refMatrix)
        {
            Vector3D result = new Vector3D();

            result.X = (point.X * refMatrix.M11) + (point.Y * refMatrix.M21) + (point.Z * refMatrix.M31) + refMatrix.M41;
            result.Y = (point.X * refMatrix.M12) + (point.Y * refMatrix.M22) + (point.Z * refMatrix.M32) + refMatrix.M42;
            result.Z = (point.X * refMatrix.M13) + (point.Y * refMatrix.M23) + (point.Z * refMatrix.M33) + refMatrix.M43;

            return result;
        }

        /// <summary>
        /// Gets a list of all connected grids.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="grids"></param>
        /// <returns>true on success; false if at least one grid is static</returns>
        public bool GetConnectedGrids(IMyCubeGrid parent, HashSet<IMyCubeGrid> grids)
        {
            var blocks = new List<IMySlimBlock>();
            parent?.GetBlocks(blocks, (b) => b.FatBlock != null &&
                            (b.FatBlock is IMyMotorBase || b.FatBlock is IMyMotorRotor ||
                            b.FatBlock is IMyPistonBase || b.FatBlock is IMyPistonTop ||
                            b.FatBlock is IMyShipConnector));

            foreach (var block in blocks)
            {
                var nextgrid = (block.FatBlock as IMyMotorBase)?.RotorGrid ??
                            (block.FatBlock as IMyMotorRotor)?.Stator?.CubeGrid ??
                            (block.FatBlock as IMyPistonBase)?.TopGrid ??
                            (block.FatBlock as IMyPistonTop)?.Piston?.CubeGrid ??
                            (block.FatBlock as IMyShipConnector)?.OtherConnector?.CubeGrid;

                if (nextgrid != null)
                {
                    Logger.Instance.LogDebug("Piston/rotor connected to " + nextgrid.DisplayName);
                    if (!grids.Contains(nextgrid))
                    {
                        if (nextgrid.IsStatic)
                            return false;

                        var locked = (block.FatBlock as IMyPistonBase)?.IsLocked ??
                                    (block.FatBlock as IMyMotorStator)?.IsLocked ??
                                    (block.FatBlock as IMyPistonTop)?.Piston?.IsLocked ??
                                    ((block.FatBlock as IMyMotorRotor)?.Stator as IMyMotorStator)?.IsLocked ?? false;

                        if (!locked)
                            grids.Add(nextgrid);

                        if (!GetConnectedGrids(nextgrid, grids))
                            return false;
                    }
                }
                else
                {
                    Logger.Instance.LogDebug("Piston/rotor not connected: " + block.FatBlock.DisplayNameText);
                }
            }
            return true;
        }

        /// <summary>
        /// Given the parent objects original position, calculate the new position of the sub-grid based on the destination of the parent
        /// </summary>
        /// <param name="parentObj">Parent object</param>
        /// <param name="srcMatrix">Parent worldmatrix before moving</param>
        /// <param name="destMatrix">Parent worldmatrix after moving</param>
        /// <returns>true if player was moved, false if not</returns>
        public bool MoveSubGrids(IMyEntity parentObj, MatrixD srcMatrix, MatrixD destMatrix, Vector3D srcVelocity, Vector3D destVelocity, Dictionary<long, Tuple2<ulong, MatrixD, Vector3D>> updates)
        {
            Logger.Instance.LogMessage("MoveSubGrids()");

            if (!(parentObj is IMyCubeGrid))
                return false;
            try
            {
                Logger.Instance.IndentLevel++;
                bool playerWasMoved = false;
                var gridList = new HashSet<IMyCubeGrid>();
                gridList.Add(parentObj as IMyCubeGrid);

                if (!GetConnectedGrids(parentObj.GetTopMostParent() as IMyCubeGrid, gridList))
                {
                    Logger.Instance.LogMessage("Not moving static grid.");
                    return false;
                }
                Logger.Instance.LogDebug("Number of grids: " + gridList.Count);

                // If we have any subgrids move, do that
                foreach (var grid in gridList)
                {
                    var objToMove = grid;
                    Logger.Instance.LogDebug(string.Format("grid: {0}, parent: {1}", objToMove.GetTopMostParent().EntityId, parentObj.GetTopMostParent().EntityId));

                    if (objToMove.GetTopMostParent().EntityId == parentObj.GetTopMostParent().EntityId)
                        continue;

                    var relVelocity = Vector3D.Zero;

                    if (objToMove.Physics != null)
                        relVelocity = objToMove.Physics.LinearVelocity;

                    var newObjPosition = CalculateDestinationMatrix(srcMatrix, destMatrix, objToMove, ref relVelocity, false);
                    var savedObjPosition = objToMove.WorldMatrix;

                    if (objToMove.Physics != null)
                        relVelocity = objToMove.Physics.LinearVelocity;

                    relVelocity -= srcVelocity;

                    // Set object velocity
                    if (objToMove.Physics != null)
                        relVelocity += destVelocity;

                    // Save new information for updating later
                    var player = MyAPIGateway.Players.GetPlayerControllingEntity(objToMove);
                    if (player == null)
                    {
                        Logger.Instance.LogMessage(string.Format("local update {0} to: {1:F0}, {2:F0}, {3:F0}", objToMove.DisplayName, newObjPosition.Translation.X, newObjPosition.Translation.Y, newObjPosition.Translation.Z));
                        objToMove.PositionComp.SetWorldMatrix(newObjPosition);

                        if (objToMove.Physics != null)
                            objToMove.Physics.LinearVelocity = relVelocity;

                        //if (objToMove.SyncObject != null)
                        //    objToMove.SyncObject.UpdatePosition();
                    }
                    else
                    {

                        Logger.Instance.LogMessage(string.Format("remote update {0} to: {1:F0}, {2:F0}, {3:F0}", objToMove.DisplayName, newObjPosition.Translation.X, newObjPosition.Translation.Y, newObjPosition.Translation.Z));
                        if (!updates.ContainsKey(objToMove.EntityId))
                            updates.Add(objToMove.EntityId, MyTuple.Create(player.SteamUserId, newObjPosition, destVelocity));
                    }

                    if (objToMove.GetTopMostParent().EntityId == MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent().EntityId)
                        playerWasMoved = true;

                    //// Recurse, in case there are nested grids (ie. piston attached to another piston)
                    //if ((objToMove is IMyCubeBlock && (objToMove as IMyCubeBlock).BlockDefinition.TypeId != new MyObjectBuilderType(typeof(MyObjectBuilder_ShipConnector))))
                    //    if (MoveSubGrids(objToMove, savedObjPosition, newObjPosition, srcVelocity, destVelocity, updates))
                    //        playerWasMoved = true;
                }
                return playerWasMoved;
            }
            finally
            {
                Logger.Instance.IndentLevel--;
            }
        }
        #endregion Movement
    }

    static class Utils
    {
        public static string GetModelsPath()
        {
            return Path.GetFullPath(string.Format(@"{0}\Models\", Globals.ModContext.ModPath));
        }

        static public void ShowMessageToUsersInRange(IMyEntity source, string message, int time = 5000)
        {
            bool isMe = false;

            if (MyAPIGateway.Players == null || MyAPIGateway.Entities == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null
                || MyAPIGateway.Session.Player == null || MyAPIGateway.Session.Player.Controller == null
                || MyAPIGateway.Session.Player.Controller.ControlledEntity == null)
                return;

            BoundingBoxD box = source.GetTopMostParent().PositionComp.WorldAABB;

            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInAABB(ref box);

            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;

                if (entity.EntityId == MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity.GetTopMostParent().EntityId &&
                    entity.PositionComp.WorldAABB.Intersects(box))
                {
                    isMe = true;
                    break;
                }
            }

            if ((MyAPIGateway.Players.GetPlayerControllingEntity(source.GetTopMostParent()) != null
                && MyAPIGateway.Session.Player != null
                && MyAPIGateway.Session.Player.IdentityId == MyAPIGateway.Players.GetPlayerControllingEntity(source.GetTopMostParent()).IdentityId)
                || isMe)
                MyAPIGateway.Utilities.ShowNotification(message, time);
        }

        static public long GetNearestPlayerTo(IMyEntity refobject)
        {
            IMyEntity nearest = null;

            var sphere = new VRageMath.BoundingSphereD(refobject.PositionComp.GetPosition(), 10); // Get all players within 10m

            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            foreach (var entity in entities)
            {
                if (entity == null)
                    continue;

                if (StargateAdmin.Configuration.Debug)
                    Logger.Instance.LogDebug("Entity: " + entity.DisplayName);

                if (nearest == null ||
                    (entity is IMyControllableEntity &&
                    (entity.PositionComp.GetPosition() - refobject.PositionComp.GetPosition()).LengthSquared() <
                    (nearest.PositionComp.GetPosition() - refobject.PositionComp.GetPosition()).LengthSquared()))
                    nearest = entity;
            }

            if (StargateAdmin.Configuration.Debug)
                Logger.Instance.LogDebug("Nearest entity: " + (nearest != null ? nearest.DisplayName : "<null>"));

            if (nearest == null)
                return 0;

            if (MyAPIGateway.Players.GetPlayerControllingEntity(nearest) == null)
                return 0;

            return MyAPIGateway.Players.GetPlayerControllingEntity(nearest).IdentityId;
        }

        static public bool HasSeenUpdate()
        {

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Globals.UpdateFile, typeof(Stargate)))
            {
                System.IO.TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Globals.UpdateFile, typeof(Stargate));
                string line;
                DateTime update = new DateTime();
                DateTime.TryParse(Globals.LastUpdate, out update);

                while ((line = reader.ReadLine()) != null)
                {
                    string[] kvp = line.Split('=');
                    long ticks;

                    if (kvp.Length == 2)
                    {
                        if (long.TryParse(kvp[1], out ticks))
                        {
                            if (ticks >= update.Ticks)
                            {
                                reader.Close();
                                return true;
                            }
                        }
                    }
                }
                reader.Close();
            }
            return false;
        }

        static public void WriteUpdate()
        {
            System.IO.TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Globals.UpdateFile, typeof(Stargate));
            DateTime update = new DateTime();
            DateTime.TryParse(Globals.LastUpdate, out update);

            writer.WriteLine("updateseen={0}", update.Ticks.ToString());
            writer.Flush();
            writer.Close();
        }
    }

    namespace Extensions
    {
        public static class StargateExtensions
        {
            public static GateType GetGateType(this IMyTerminalBlock gate)
            {
                var type = GateType.Invalid;

                if (!gate.BlockDefinition.SubtypeId.Contains("Horizon") && !gate.BlockDefinition.SubtypeId.Contains("Iris"))
                {
                    if (gate.BlockDefinition.SubtypeId.StartsWith("Stargate"))
                        type = GateType.Stargate;
                    else if (gate.BlockDefinition.SubtypeId.StartsWith("Supergate"))
                        type = GateType.Supergate;
                    else if (gate.BlockDefinition.SubtypeId == "Super Supergate")
                        type = GateType.SuperSupergate;
                    else if (gate.BlockDefinition.SubtypeId.StartsWith("Microgate"))
                        type = GateType.Microgate;
                }
                return type;
            }

            public static void PlaySound(this IMyTerminalBlock source, string soundname, Action<MyEntity3DSoundEmitter> stoppedCallback = null, bool sync = false)
            {
                source?.GameLogic.GetAs<Stargate>()?.PlaySound(soundname, stoppedCallback: stoppedCallback, sync: sync);
            }

            public static HUDTextNI.EntityMessage CreateHUDText(IMyEntity gate, GateType type)
            {
                var isSupergate = type == GateType.Supergate;

                var text = new HUDTextNI.EntityMessage(gate.EntityId, 30, isSupergate ? 20 : 0.25, gate.EntityId,
                    new Vector3D(
                        (isSupergate ? 15 : 0.2f),                                      // Left/right offset
                        -(isSupergate ? 20 : 1),                                        // Forward/backward offset
                        -(isSupergate ? (gate.LocalAABB.HalfExtents.Y - 40) : 2.4f)     // Up/down offset
                    ),
                    Vector3D.Down,
                    Vector3D.Forward,
                    "<color=Green>^ Front ^",
                    orientation: HUDTextNI.TextOrientation.center);

                return text;
            }

            public static bool IsIrisActive(this IMyTerminalBlock gate)
            {
                try
                {
                    if (gate == null)
                        return false;
                    //Logger.Instance.LogMessage(string.Format("gate: {0}, type: {1}", (gate as IMyReactor).UseConveyorSystem, gate.GetType()));
                    if (gate.GameLogic.GetAs<Stargate>()?.StargateType == GateType.Stargate)
                        return gate.GameLogic.GetAs<Stargate>()?.Data.IrisActive ?? false;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogException(ex);
                    return false;
                }
            }
        }
    }
}
// vim: tabstop=4 expandtab shiftwidth=4 nobackup
