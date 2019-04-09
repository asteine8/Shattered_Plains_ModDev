using System;
using DefenseShields.Support;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace DefenseShields
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, "DSControlLCD", "DSControlLCDWide")]
    public partial class Displays : MyGameLogicComponent
    {
        public override void OnAddedToContainer()
        {
            if (!ContainerInited)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

                Display = (IMyTextPanel)Entity;
                ContainerInited = true;
                if (Session.Enforced.Debug == 3) Log.Line($"ContainerInited: DisplayId [{Display.EntityId}]");
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

        public override bool IsSerialized()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (Display.Storage != null)
                {
                    State.SaveState();
                }
            }
            return false;
        }

        public override void OnAddedToScene()
        {
            try
            {
                MyGrid = (MyCubeGrid)Display.CubeGrid;
                MyCube = Display as MyCubeBlock;
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToScene: {ex}"); }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            try
            {
                if (!_bInit) BeforeInit();
                else if (_bCount < SyncCount * _bTime)
                {
                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    if (ShieldComp?.DefenseShields?.MyGrid == MyGrid) _bCount++;
                }
                else _readyToSync = true;
            }
            catch (Exception ex) { Log.Line($"Exception in UpdateOnceBeforeFrame: {ex}"); }
        }

        public override void UpdateBeforeSimulation100()
        {
            Tick = Session.Instance.Tick;
            if (ClientUiUpdate || SettingsUpdated) NewSettings();
            if (Set.Settings.Report == 0)
            {
                if (ShieldEnabled)
                {
                    ShieldEnabled = false;
                    Display.ClearImagesFromSelection();
                    Display.WritePublicText(string.Empty);
                }
                return;
            }
            ShieldEnabled = true;

            var pEventId = Session.Instance.PlayerEventId;
            var pEvent = pEventId != _pEventIdWas;
            var owner = State.Value.ClientOwner;

            if (_isDedicated)
            {
                if (!State.Value.Release && (owner == 0 || pEvent && !Session.Instance.Players.ContainsKey(owner)))
                {
                    AbandonDisplay(true);
                }
                _pEventIdWas = pEventId;
                return;
            }

            var clientId = MyAPIGateway.Multiplayer.MyId;
            var playerId = MyAPIGateway.Session.Player.IdentityId;

            if (Vector3D.DistanceSquared(MyAPIGateway.Session.Camera.Position, Display.WorldAABB.Center) > 40000)
            {
                if (State.Value.ClientOwner == playerId)
                {
                    if (Session.Enforced.Debug >= 2) Log.Line($"[Abandon]:{_myDisplay} - Tick:{Tick} - Client:{clientId} - PlayerId:{playerId} - Owner:{owner}");
                    Session.Instance.ClaimDisplay(clientId, playerId, _mId++, Display.EntityId, true);
                }
                return;
            }

            _myDisplay = _isServer && !_isDedicated || owner == playerId;
            //if (Session.Enforced.Debug >= 2) Log.Line($"MyDisplay:{_myDisplay} - Tick:{Tick} - Client:{clientId} - PlayerId:{playerId} - Owner:{owner}");
            if (!_myDisplay)
            {
                if (State.Value.Release)
                {
                    if (State.Value.ClientOwner == 0 || _waitCount++ >= 1)
                    {
                        if (Session.Enforced.Debug >= 2) Log.Line($"[NotActiveRequest]: Tick:{Tick} - Client:{clientId} - PlayerId:{playerId} - Owner:{owner}");
                        Session.Instance.ClaimDisplay(clientId, playerId, _mId++, Display.EntityId,false);
                        _waitCount = 0;
                    }
                }
                else _waitCount = 0;
                return;
            }

            if (!ActiveDisplay()) return;
            UpdateDisplay();
        }

        public override void OnRemovedFromScene()
        {
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}"); }
        }

        public override void Close()
        {
            base.Close();
            try
            {
                //if (Session.Instance.Displays.Contains(this)) Session.Instance.Displays.Remove(this);
            }
            catch (Exception ex) { Log.Line($"Exception in Close: {ex}"); }
        }

        public override void MarkForClose()
        {
            base.MarkForClose();
            try
            {
            }
            catch (Exception ex) { Log.Line($"Exception in MarkForClose: {ex}"); }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene) OnRemovedFromScene();
        }
    }
}
