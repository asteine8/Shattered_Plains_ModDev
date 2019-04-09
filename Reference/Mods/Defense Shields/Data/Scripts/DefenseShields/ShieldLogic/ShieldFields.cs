﻿
namespace DefenseShields
{
    using VRage.Game.ModAPI;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Support;
    using Sandbox.Game.Entities;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Collections;
    using VRageMath;
    using ParallelTasks;

    public partial class DefenseShields 
    {
        #region Setup
        internal readonly MyDefinitionId GId = MyResourceDistributorComponent.ElectricityId;

        internal readonly object SubLock = new object();
        internal readonly object SubUpdateLock = new object();

        internal readonly int[] ExpChargeReductions = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };
        internal readonly float[] ReserveScaler = { float.MaxValue * 0.001f, 0.001f, 1, 1000, 1000000 };

        internal readonly List<MyEntity> PruneList = new List<MyEntity>();
        internal readonly List<ShieldHit> ShieldHits = new List<ShieldHit>();
        internal readonly Queue<ShieldHitValues> ShieldHitsToSend = new Queue<ShieldHitValues>();

        internal readonly HashSet<MyEntity> AuthenticatedCache = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> IgnoreCache = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> EntityBypass = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> EnemyShields = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> Missiles = new HashSet<MyEntity>();
        internal readonly HashSet<MyEntity> FriendlyMissileCache = new HashSet<MyEntity>();

        internal readonly Dictionary<MyEntity, ProtectCache> ProtectedEntCache = new Dictionary<MyEntity, ProtectCache>();
        internal readonly CachingDictionary<MyCubeBlock, uint> DirtyCubeBlocks = new CachingDictionary<MyCubeBlock, uint>();

        internal readonly ConcurrentDictionary<MyCubeGrid, BlockSets> BlockSets = new ConcurrentDictionary<MyCubeGrid, BlockSets>();
        internal readonly ConcurrentDictionary<MyEntity, EntIntersectInfo> WebEnts = new ConcurrentDictionary<MyEntity, EntIntersectInfo>();
        internal readonly ConcurrentDictionary<MyEntity, MoverInfo> EntsByMe = new ConcurrentDictionary<MyEntity, MoverInfo>();
        internal readonly ConcurrentDictionary<MyVoxelBase, int> VoxelsToIntersect = new ConcurrentDictionary<MyVoxelBase, int>();

        internal readonly ConcurrentQueue<SubGridComputedInfo> AddSubGridInfo = new ConcurrentQueue<SubGridComputedInfo>();

        internal const int ConvToHp = 100;
        internal const float ConvToDec = 0.01f;
        internal const float ConvToWatts = 0.01f;

        internal volatile int LogicSlot;
        internal volatile int MonitorSlot;
        internal volatile int LostPings;
        internal volatile bool MoverByShield;
        internal volatile bool PlayerByShield;
        internal volatile bool NewEntByShield;
        internal volatile bool Asleep;
        internal volatile bool WasPaused;
        internal volatile uint LastWokenTick;
        internal volatile bool ReInforcedShield;

        internal BoundingBoxD WebBox = new BoundingBoxD();
        internal MatrixD OldShieldMatrix;
        internal ShieldGridComponent ShieldComp;
        internal BoundingBoxD ShieldBox3K = new BoundingBoxD();
        internal MyOrientedBoundingBoxD SOriBBoxD = new MyOrientedBoundingBoxD();
        internal BoundingSphereD ShieldSphere = new BoundingSphereD(Vector3D.Zero, 1);
        internal BoundingBox ShieldAabbScaled = new BoundingBox(Vector3D.One, -Vector3D.One);
        internal BoundingSphereD ShieldSphere3K = new BoundingSphereD(Vector3D.Zero, 1f);
        internal BoundingSphereD WebSphere = new BoundingSphereD(Vector3D.Zero, 1f);

        internal bool InControlPanel => MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel;
        internal bool InThisTerminal => Session.Instance.LastTerminalId == Shield.EntityId;

        private const int ReModulationCount = 300;
        private const int ShieldDownCount = 1200;
        private const int EmpDownCount = 3600;
        private const int PowerNoticeCount = 600;
        private const int CapacitorDrainCount = 60;
        private const int CapacitorStableCount = 600;
        private const int OverHeat = 600;
        private const int HeatingStep = 600;
        private const int CoolingStep = 1200;
        private const int FallBackStep = 10;
        private const double MagicRatio = 2.40063050674088;
        private const float ChargeRatio = 1.25f;
        private const int SyncCount = 60;

        private const string SpaceWolf = "Space_Wolf";
        private const string ModelMediumReflective = "\\Models\\Cubes\\ShieldPassive11.mwm";
        private const string ModelHighReflective = "\\Models\\Cubes\\ShieldPassive.mwm";
        private const string ModelLowReflective = "\\Models\\Cubes\\ShieldPassive10.mwm";
        private const string ModelRed = "\\Models\\Cubes\\ShieldPassive09.mwm";
        private const string ModelBlue = "\\Models\\Cubes\\ShieldPassive08.mwm";
        private const string ModelGreen = "\\Models\\Cubes\\ShieldPassive07.mwm";
        private const string ModelPurple = "\\Models\\Cubes\\ShieldPassive06.mwm";
        private const string ModelGold = "\\Models\\Cubes\\ShieldPassive05.mwm";
        private const string ModelOrange = "\\Models\\Cubes\\ShieldPassive04.mwm";
        private const string ModelCyan = "\\Models\\Cubes\\ShieldPassive03.mwm";

        private readonly List<MyResourceSourceComponent> _powerSources = new List<MyResourceSourceComponent>();
        private readonly List<MyCubeBlock> _functionalBlocks = new List<MyCubeBlock>();
        private readonly List<IMyBatteryBlock> _batteryBlocks = new List<IMyBatteryBlock>();
        private readonly List<IMyTextPanel> _displayBlocks = new List<IMyTextPanel>();
        private readonly List<KeyValuePair<MyEntity, EntIntersectInfo>> _webEntsTmp = new List<KeyValuePair<MyEntity, EntIntersectInfo>>();
        private readonly List<KeyValuePair<MyEntity, ProtectCache>> _protectEntsTmp = new List<KeyValuePair<MyEntity, ProtectCache>>();
        private readonly RunningAverage _dpsAvg = new RunningAverage(2);
        private readonly RunningAverage _hpsAvg = new RunningAverage(2);
        private readonly EllipsoidOxygenProvider _ellipsoidOxyProvider = new EllipsoidOxygenProvider(Matrix.Zero);
        private readonly EllipsoidSA _ellipsoidSa = new EllipsoidSA(double.MinValue, double.MinValue, double.MinValue);
        private readonly Vector3D[] _resetEntCorners = new Vector3D[8];
        private readonly Vector3D[] _obbCorners = new Vector3D[8];
        private readonly Vector3D[] _obbPoints = new Vector3D[9];

        private uint _tick;
        private uint _shieldEntRendId;
        private uint _subTick;
        private uint _funcTick;
        private uint _fatTick;
        private uint _shapeTick;
        private uint _capacitorTick;
        private uint _messageTick;
        private uint _heatVentingTick = uint.MaxValue;
        private uint _lastSendDamageTick = uint.MaxValue;

        private float _power = 0.001f;
        private float _powerNeeded;
        private float _otherPower;
        private float _batteryMaxPower;
        private float _batteryCurrentOutput;
        private float _batteryCurrentInput;
        private float _shieldPeakRate;
        private float _shieldMaxChargeRate;
        private float _damageReadOut;
        private float _accumulatedHeat;
        private float _shieldMaintaintPower;
        private float _shieldConsumptionRate;
        private float _oldShieldFudge;
        private float _empScaleHp = 1f;
        private float _runningDamage;
        private float _runningHeal;

        private double _oldEllipsoidAdjust;
        private double _ellipsoidSurfaceArea;
        private double _shieldVol;
        private double _sizeScaler;

        private int _count = -1;
        private int _powerNoticeLoop;
        private int _capacitorLoop;
        private int _overLoadLoop = -1;
        private int _empOverLoadLoop = -1;
        private int _reModulationLoop = -1;
        private int _heatCycle = -1;
        private int _fallbackCycle;
        private int _currentHeatStep;
        private int _empScaleTime = 1;
        private int _prevLod;
        private int _onCount;
        private int _shieldRatio = 1;
        private int _expChargeReduction;
        private int _bCount;
        private int _bTime;
        private bool _bInit;

        private long _gridOwnerId = -1;
        private long _controllerOwnerId = -1;

        private bool _firstLoop = true;
        private bool _enablePhysics = true;
        private bool _needPhysics;
        private bool _allInited;
        private bool _containerInited;
        private bool _forceBufferSync;
        private bool _comingOnline;
        private bool _tick20;
        private bool _tick60;
        private bool _tick180;
        private bool _tick300;
        private bool _tick600;
        private bool _tick1800;
        private bool _resetEntity;
        private bool _empOverLoad;
        private bool _isDedicated;
        private bool _mpActive;
        private bool _isServer;
        private bool _shieldPowered;
        private bool _slaveLink;
        private bool _subUpdate;
        private bool _updateGridDistributor;
        private bool _hideShield;
        private bool _hideColor;
        private bool _supressedColor;
        private bool _shapeChanged;
        private bool _entityChanged;
        private bool _updateRender;
		private bool _functionalAdded;
        private bool _functionalRemoved;
        private bool _functionalChanged;
        private bool _functionalEvent;
        private bool _blockAdded;
		private bool _blockRemoved;
        private bool _blockChanged;
		private bool _blockEvent;
        private bool _shapeEvent;
        private bool _updateMobileShape;
        private bool _clientNotReady;
        private bool _clientAltered;
        private bool _clientOn;
        private bool _viewInShield;
        private bool _powerFail;
        private bool _halfExtentsChanged;
        private bool _checkForDistributor;
        private bool _updatePowerSources;
        private bool _readyToSync;
        private bool _firstSync;
        private bool _adjustShape;

        private string _modelActive = "\\Models\\Cubes\\ShieldActiveBase.mwm";
        private string _modelPassive = string.Empty;

        private readonly Vector2D _shieldIconPos = new Vector2D(-0.89, -0.86);
        private Vector3D _localImpactPosition;
        private Vector3D _oldGridHalfExtents;

        private Quaternion _sQuaternion;
        private Color _oldPercentColor = Color.Transparent;

        private MyResourceSinkInfo _resourceInfo;
        private MyResourceSinkComponent _sink;

        private MyEntity _shellPassive;
        private MyEntity _shellActive;
        private MyParticleEffect _effect1 = new MyParticleEffect();
        private MyParticleEffect _effect2 = new MyParticleEffect();

        private DSUtils Dsutil1 { get; set; } = new DSUtils();
        #endregion

        public enum Ent
        {
            Ignore,
            Protected,
            Friendly,
            EnemyPlayer,
            EnemyInside,
            NobodyGrid,
            EnemyGrid,
            Shielded,
            Other,
            VoxelBase,
            Authenticated,
            Floater
        }

        internal enum State
        {
            Active,
            Failure,
            Init,
            Lowered,
            Sleep,
            Wake
        }

        internal enum ShieldType
        {
            Station,
            LargeGrid,
            SmallGrid,
            Unknown
        }

        public enum PlayerNotice
        {
            EmitterInit,
            FieldBlocked,
            OverLoad,
            EmpOverLoad,
            Remodulate,
            NoPower,
            NoLos
        }

        public int KineticCoolDown { get; internal set; } = -1;
        public int EnergyCoolDown { get; internal set; } = -1;
        public int HitCoolDown { get; private set; } = -11;
        public int DtreeProxyId { get; set; } = -1;

        internal IMyUpgradeModule Shield { get; set; }
        internal ShieldType ShieldMode { get; set; }
        internal MyCubeGrid MyGrid { get; set; }
        internal MyCubeBlock MyCube { get; set; }
        internal MyEntity ShieldEnt { get; set; }

        internal MyResourceDistributorComponent MyResourceDist { get; set; }

        internal ControllerSettings DsSet { get; set; }
        internal ControllerState DsState { get; set; }
        internal ShieldHitValues ShieldHit { get; set; } = new ShieldHitValues();
        internal Icosphere.Instance Icosphere { get; set; }

        internal uint ResetEntityTick { get; set; }
        internal uint LosCheckTick { get; set; }
        internal uint TicksWithNoActivity { get; set; }
        internal uint EffectsCleanTick { get; set; }

        internal int ReserveScale { get; set; }

        internal float ShieldChargeRate { get; set; }
        internal float ShieldMaxCharge { get; set; }
        internal float GridMaxPower { get; set; }
        internal float GridCurrentPower { get; set; }
        internal float GridAvailablePower { get; set; }
        internal float ShieldCurrentPower { get; set; }
        internal float ShieldAvailablePower { get; set; }
        internal float ShieldMaxPower { get; set; }
        internal float ShieldHpBase { get; set; }
        internal float HpScaler { get; set; } = 1f;

        internal double BoundingRange { get; set; }
        internal double EllipsoidVolume { get; set; }

        internal bool NotFailed { get; set; }
        internal bool DeformEnabled { get; set; }
        internal bool WarmedUp { get; set; }
        internal bool Warming { get; set; }
        internal bool UpdateDimensions { get; set; }
        internal bool FitChanged { get; set; }
        internal bool GridIsMobile { get; set; }
        internal bool SettingsUpdated { get; set; }
        internal bool ClientUiUpdate { get; set; }
        internal bool IsStatic { get; set; }
        internal bool WebDamage { get; set; }
        internal bool IsFunctional { get; set; }
        internal bool IsWorking { get; set; }
        internal bool EntCleanUpTime { get; set; }
        internal bool ModulateGrids { get; set; }
        internal bool EnergyHit { get; set; }
        internal bool EffectsDirty { get; set; }
        internal bool ShieldActive { get; set; }

        internal Vector3D MyGridCenter { get; set; }
        internal Vector3D DetectionCenter { get; set; }

        internal MatrixD DetectMatrixOutsideInv { get; set; }
        internal MatrixD ShieldShapeMatrix { get; set; }
        internal MatrixD DetectMatrixOutside { get; set; }
        internal MatrixD ShieldMatrix { get; set; }

        internal MatrixD OffsetEmitterWMatrix { get; set; }

        internal Task FuncTask { get; set; }
        internal float ImpactSize { get; set; } = 9f;
        internal float Absorb { get; set; }

        internal DamageHandlerHit HandlerImpact { get; set; } = new DamageHandlerHit();
        internal Vector3D WorldImpactPosition { get; set; } = new Vector3D(Vector3D.NegativeInfinity);
        internal Vector3D ShieldSize { get; set; }

        internal MatrixD DetectionMatrix
        {
            get
            {
                return DetectMatrixOutside;
            }

            set
            {
                DetectMatrixOutside = value;
                DetectMatrixOutsideInv = MatrixD.Invert(value);
            }
        }
    }
}
