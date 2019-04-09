namespace DefenseShields
{
    using global::DefenseShields.Support;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Game.Entities;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.Components;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Game.ObjectBuilders.Definitions;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Utils;
    using VRageMath;
    using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "PlanetaryEmitterLarge")]
    public class PlanetShields : MyGameLogicComponent
    {

            private bool _clientNotReady;
            private bool _updateRender;
            private string _modelActive = "\\Models\\Cubes\\ShieldActiveBase.mwm";
            private string _modelPassive = string.Empty;
            private uint _tick;
            private uint _shieldEntRendId;
            private bool _hideShield;
            private bool _hideColor;
            private bool _supressedColor;
            private int _count = -1;
            private int _lCount;
            internal int RotationTime;
            internal bool ContainerInited;
            internal bool AllInited;
            internal bool IsStatic;

            internal bool SettingsUpdated;
            private bool _powered;
            private bool _isDedicated;
            private bool _mpActive;
            private bool _isServer;
            private bool _tick60;
            private bool _tick600;
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
            private const float Power = 0.01f;

            public IMyUpgradeModule PlanetShield => (IMyUpgradeModule)Entity;
            internal MyCubeGrid MyGrid;
            internal MyResourceDistributorComponent MyResourceDist;
            internal MyEntity ShieldEnt;
            private MyEntity _shellPassive;
            private MyEntity _shellActive;
            internal MyPlanet Planet;
            internal PlanetShieldSettings PlaSet;
            internal PlanetShieldState PlaState;
            internal DSUtils Dsutil1 = new DSUtils();

            internal MyResourceSinkInfo ResourceInfo;
            internal MyResourceSinkComponent Sink;
            internal MyStringId Square = MyStringId.GetOrCompute("Square");
            internal Icosphere.Instance Icosphere;

            internal BoundingSphereD ShieldSphere;
            internal BoundingBox ShieldBox;
            internal Vector3D ShieldSize;


            private static readonly MyDefinitionId GId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        /*

    public override void UpdateBeforeSimulation()
    {
        try
        {
            UpdateFields();
            if (!PostInit()) return;
            Timing();

            if (!PlanetShieldReady(_isServer)) return;
            //_shellActive.PositionComp.LocalMatrix = mobileMatrix;
            //var blue = Color.Blue;
            //var pMatrix = ShieldEnt.WorldMatrix;
            //var blue = Color.Blue;
            //MySimpleObjectDraw.DrawTransparentSphere(ref pMatrix, (float)ShieldSphere.Radius, ref blue, MySimpleObjectRasterizer.Wireframe, 12, Square, Square, 128, -1, null, BlendTypeEnum.LDR);
            //DsDebugDraw.DrawBox1(ShieldEnt.PositionComp.WorldAABB, Color.Red);
        }
        catch (Exception ex) { Log.Line($"Exception in UpdateBeforeSimulation: {ex}"); }
    }

    private void Timing()
    {
        if (_count++ == 59)
        {
            _count = 0;
            _lCount++;
            if (_lCount == 10) _lCount = 0;
        }

        if (_count == 29 && !Session.Instance.DedicatedServer && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
        {
            PlanetShield.RefreshCustomInfo();
           ((MyCubeBlock)PlanetShield).UpdateTerminal();
        }
    }

    private void ShellVisibility(bool forceInvisible = false)
    {
        if (forceInvisible)
        {
            _shellPassive.Render.UpdateRenderObject(false);
            _shellActive.Render.UpdateRenderObject(false);
            return;
        }

        _shellPassive.Render.UpdateRenderObject(true);
        _shellActive.Render.UpdateRenderObject(true);
        _shellActive.Render.UpdateRenderObject(false);
    }

    private bool PlanetShieldReady(bool server)
    {
        if (server)
        {
            if (!BlockWorking()) return false;
        }
        else
        {
            if (!PlaState.State.Online) return false;
        }

        return true;
    }

    private bool BlockWorking()
    {
        if (_count <= 0) _powered = Sink.IsPowerAvailable(GId, 0.01f);
        if (PlanetShield?.CubeGrid == null || !PlanetShield.Enabled || !PlanetShield.IsFunctional || !_powered)
        {
            NeedUpdate(PlaState.State.Online, false);
            return false;
        }

        if (!_isDedicated && PlanetShield != null && _count == 29)
        {
            PlanetShield.RefreshCustomInfo();
            ((MyCubeBlock)PlanetShield).UpdateTerminal();
        }

        if (!PlaState.State.Backup)
        {
            NeedUpdate(PlaState.State.Online, true);
            return true;
        }

        NeedUpdate(PlaState.State.Online, false);
        return false;
    }

    private void NeedUpdate(bool onState, bool turnOn)
    {
        if (!onState && turnOn)
        {
            PlaState.State.Online = true;
            PlaState.SaveState();
            PlaState.NetworkUpdate();
        }
        else if (onState & !turnOn)
        {
            PlaState.State.Online = false;
            PlaState.SaveState();
            PlaState.NetworkUpdate();
        }
    }

    public void UpdateState(ProtoPlanetShieldState newState)
    {
        PlaState.State = newState;
        if (Session.Enforced.Debug == 3) Log.Line($"UpdateState: PlanetShieldId [{PlanetShield.EntityId}]");
    }

    public bool PostInit()
    {
        try
        {
            if (AllInited) return true;
            if (PlanetShield.CubeGrid.Physics == null) return false;

            var isFunctional = PlanetShield.IsFunctional;
            if (_isServer && !isFunctional)
            {
                if (_tick600)
                {
                    PlanetShield.RefreshCustomInfo();
                }
                return false;
            }

            if (!_isServer && !PlaState.State.Online) return false;

            PsUi.CreateUi(PlanetShield);

            if (!isFunctional) return false;
            var psCenter = PlanetShield.PositionComp.WorldVolume.Center;
            var planet = MyGamePruningStructure.GetClosestPlanet(psCenter);
            if (Vector3D.Distance(psCenter, planet.PositionComp.WorldVolume.Center) < 150000)
            {
                Log.Line($"test");
                Planet = planet;
                InitEntities(true);
                SetShieldShape();
                AllInited = true;
                return true;
            }
            if (Session.Enforced.Debug == 3) Log.Line($"AllInited: PlanetShieldId [{PlanetShield.EntityId}]");
            return false;
        }
        catch (Exception ex) { Log.Line($"Exception in PlanetShield PostInit: {ex}"); }
        return !_isServer;
    }

    private void SetShieldShape()
    {
        var localMatrix = MatrixD.CreateScale(Planet.AtmosphereRadius);
        var worldMatrix = localMatrix;
        worldMatrix.Translation = Planet.PositionComp.WorldVolume.Center;

        if (!_isDedicated)
        {
            _shellPassive.PositionComp.LocalMatrix = Matrix.Zero;  // Bug - Cannot just change X coord, so I reset first.
            _shellActive.PositionComp.LocalMatrix = Matrix.Zero;
            _shellPassive.PositionComp.LocalMatrix = localMatrix;
            _shellActive.PositionComp.LocalMatrix = localMatrix;
        }
        ShieldEnt.PositionComp.LocalMatrix = Matrix.Zero;
        ShieldEnt.PositionComp.LocalMatrix = localMatrix;
        ShieldSphere.Center = Planet.PositionComp.WorldVolume.Center;
        ShieldSize = worldMatrix.Scale;
        ShieldSphere.Radius = ShieldSize.AbsMax();

        ShieldBox = new BoundingBox(ShieldSize, -ShieldSize);
        ShieldEnt.PositionComp.LocalAABB = ShieldBox;
        ShieldEnt.PositionComp.SetPosition(Planet.PositionComp.WorldVolume.Center);
        ShellVisibility();
    }


    internal void SelectPassiveShell()
    {
        try
        {
            /*
            switch (PlaSet.Settings.ShieldShell)
            {
                case 0:
                    _modelPassive = ModelMediumReflective;
                    _hideColor = false;
                    _supressedColor = false;
                    break;
                case 1:
                    _modelPassive = ModelHighReflective;
                    _hideColor = false;
                    _supressedColor = false;
                    break;
                case 2:
                    _modelPassive = ModelLowReflective;
                    _hideColor = false;
                    _supressedColor = false;
                    break;
                case 3:
                    _modelPassive = ModelRed;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 4:
                    _modelPassive = ModelBlue;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 5:
                    _modelPassive = ModelGreen;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 6:
                    _modelPassive = ModelPurple;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 7:
                    _modelPassive = ModelGold;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 8:
                    _modelPassive = ModelOrange;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                case 9:
                    _modelPassive = ModelCyan;
                    _hideColor = true;
                    _supressedColor = false;
                    break;
                default:
                    _modelPassive = ModelMediumReflective;
                    _hideColor = false;
                    _supressedColor = false;
                    break;
            }
            _modelPassive = ModelHighReflective;
            _hideColor = true;
            _supressedColor = false;
        }
        catch (Exception ex) { Log.Line($"Exception in SelectPassiveShell: {ex}"); }
    }

    private void InitEntities(bool fullInit)
    {
        ShieldEnt?.Close();
        _shellActive?.Close();
        _shellPassive?.Close();

        if (!fullInit)
        {
            if (Session.Enforced.Debug == 3) Log.Line($"InitEntities: mode:, remove complete - ShieldId [{PlanetShield.EntityId}]");
            return;
        }

        SelectPassiveShell();
        var parent = Planet;
        var ent = Spawn.EmptyEntity("test", null, new MyEntity(), false);
        var pos = Planet.PositionComp.WorldAABB.Center;
        ent.PositionComp.SetPosition(pos);
        if (!_isDedicated)
        {
            _shellPassive = Spawn.EmptyEntity("dShellPassive", $"{Session.Instance.ModPath()}{_modelPassive}", ent, true);
            _shellPassive.Render.CastShadows = false;
            _shellPassive.IsPreview = true;
            _shellPassive.Render.Visible = true;
            _shellPassive.Render.RemoveRenderObjects();
            _shellPassive.Render.UpdateRenderObject(true);
            _shellPassive.Render.UpdateRenderObject(false);
            _shellPassive.Save = false;
            _shellPassive.SyncFlag = false;

            _shellActive = Spawn.EmptyEntity("dShellActive", $"{Session.Instance.ModPath()}{_modelActive}", parent, true);
            _shellActive.Render.CastShadows = false;
            _shellActive.IsPreview = true;
            _shellActive.Render.Visible = true;
            _shellActive.Render.RemoveRenderObjects();
            _shellActive.Render.UpdateRenderObject(true);
            _shellActive.Render.UpdateRenderObject(false);
            _shellActive.Save = false;
            _shellActive.SyncFlag = false;
            _shellActive.SetEmissiveParts("ShieldEmissiveAlpha", Color.Transparent, 0f);
        }

        ShieldEnt = Spawn.EmptyEntity("dShield", null, parent);
        ShieldEnt.Render.CastShadows = false;
        ShieldEnt.Render.RemoveRenderObjects();
        ShieldEnt.Render.UpdateRenderObject(true);
        ShieldEnt.Render.Visible = true;
        ShieldEnt.Save = false;
        _shieldEntRendId = ShieldEnt.Render.GetRenderObjectID();
        _updateRender = true;

        if (Icosphere == null) Icosphere = new Icosphere.Instance(Session.Instance.Icosphere);
        if (Session.Enforced.Debug == 3) Log.Line($"InitEntities: mode: , spawn complete - ShieldId [{PlanetShield.EntityId}]");
    }

    internal void UpdateSettings(ProtoPlanetShieldSettings newSettings)
    {
        PlaSet.Settings = newSettings;
        SettingsUpdated = true;
        if (Session.Enforced.Debug == 4) Log.Line($"UpdateSettings - server:{Session.Instance.IsServer} - ShieldId [{PlanetShield.EntityId}]:\n{newSettings}");
    }

    private void StorageSetup()
    {
        try
        {
            var isServer = MyAPIGateway.Multiplayer.IsServer;

            if (PlaSet == null) PlaSet = new PlanetShieldSettings(PlanetShield);
            if (PlaState == null) PlaState = new PlanetShieldState(PlanetShield);
            if (PlanetShield.Storage == null) PlaState.StorageInit();
            if (!isServer)
            {
                var enforcement = Enforcements.LoadEnforcement(PlanetShield);
                if (enforcement != null) Session.Enforced = enforcement;
            }
            PlaSet.LoadSettings();
            if (!PlaState.LoadState() && !isServer) _clientNotReady = true;
            UpdateSettings(PlaSet.Settings);
            if (isServer)
            {
                //PlaState.State.Heat = 0;
            }
        }
        catch (Exception ex) { Log.Line($"Exception in StorageSetup: {ex}"); }
    }

    private void UpdateFields()
    {
        _tick = Session.Instance.Tick;
        _tick60 = _tick % 60 == 0;
        _tick600 = _tick % 600 == 0;
        MyGrid = PlanetShield.CubeGrid as MyCubeGrid;
        if (MyGrid != null) IsStatic = MyGrid.IsStatic;
    }


    public override void OnAddedToContainer()
    {
        if (!ContainerInited)
        {
            PowerPreInit();
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            ContainerInited = true;
            if (Session.Enforced.Debug == 3) Log.Line($"ContainerInited:  PlanetShieldId [{PlanetShield.EntityId}]");
        }
        if (Entity.InScene) OnAddedToScene();
    }


    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    {
        try
        {
            base.Init(objectBuilder);
            StorageSetup();
        }
        catch (Exception ex) { Log.Line($"Exception in EntityInit: {ex}"); }
    }

    public override void UpdateOnceBeforeFrame()
    {
        base.UpdateOnceBeforeFrame();
        try
        {
            _isServer = Session.Instance.IsServer;
            _isDedicated = Session.Instance.DedicatedServer;
            _mpActive = Session.Instance.MpActive;
            Session.Instance.PlanetShields.Add(this);
            PowerInit();
            PlanetShield.AppendingCustomInfo += AppendingCustomInfo;
            PlanetShield.RefreshCustomInfo();
            if (Icosphere == null) Icosphere = new Icosphere.Instance(Session.Instance.Icosphere);
        }
        catch (Exception ex) { Log.Line($"Exception in UpdateOnceBeforeFrame: {ex}"); }
    }

    public override bool IsSerialized()
    {
        if (Session.Instance.IsServer)
        {
            if (PlanetShield.Storage != null) PlaState.SaveState();
        }
        return false;
    }


    private void PowerPreInit()
    {
        try
        {
            if (Sink == null)
            {
                Sink = new MyResourceSinkComponent();
            }
            ResourceInfo = new MyResourceSinkInfo()
            {
                ResourceTypeId = GId,
                MaxRequiredInput = 0.02f,
                RequiredInputFunc = () => Power
            };
            Sink.Init(MyStringHash.GetOrCompute("Utility"), ResourceInfo);
            Sink.AddType(ref ResourceInfo);
            Entity.Components.Add(Sink);
        }
        catch (Exception ex) { Log.Line($"Exception in PowerPreInit: {ex}"); }
    }

    private void PowerInit()
    {
        try
        {
            var enableState = PlanetShield.Enabled;
            if (enableState)
            {
                PlanetShield.Enabled = false;
                PlanetShield.Enabled = true;
            }
            Sink.Update();
            if (Session.Enforced.Debug == 3) Log.Line($"PowerInit: PlanetShieldId [{PlanetShield.EntityId}]");
        }
        catch (Exception ex) { Log.Line($"Exception in AddResourceSourceComponent: {ex}"); }
    }

    private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder stringBuilder)
    {

        if (!PlaState.State.Backup)
        {
            stringBuilder.Append("[Online]: " + PlaState.State.Online +
                                 "\n" +
                                 "\n[Amplifying Shield]: " + PlaState.State.Online +
                                 "\n[Enhancer Mode]: Fortress" +
                                 "\n[Bonsus] MaxHP, Repel Grids");
        }
        else if (!PlaState.State.Backup)
        {
            stringBuilder.Append("[Online]: " + PlaState.State.Online +
                                 "\n" +
                                 "\n[Amplifying Shield]: " + PlaState.State.Online +
                                 "\n[Enhancer Mode]: " + Power.ToString("0") + "%");
        }
        else
        {
            stringBuilder.Append("[Backup]: " + PlaState.State.Backup);
        }
    }

    public override void OnRemovedFromScene()
    {
        try
        {
            if (Session.Instance.PlanetShields.Contains(this)) Session.Instance.PlanetShields.Remove(this);
        }
        catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}"); }
    }

    public override void OnBeforeRemovedFromContainer() { if (Entity.InScene) OnRemovedFromScene(); }
    public override void Close()
    {
        try
        {
            if (Session.Instance.PlanetShields.Contains(this)) Session.Instance.PlanetShields.Remove(this);
        }
        catch (Exception ex) { Log.Line($"Exception in Close: {ex}"); }
        base.Close();
    }

    public override void MarkForClose()
    {
        try
        {
        }
        catch (Exception ex) { Log.Line($"Exception in MarkForClose: {ex}"); }
        base.MarkForClose();
    }
            */
    }
}
