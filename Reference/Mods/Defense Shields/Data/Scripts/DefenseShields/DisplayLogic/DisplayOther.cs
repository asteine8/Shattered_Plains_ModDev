using DefenseShields.Support;
using VRage.ModAPI;

namespace DefenseShields
{
    public partial class Displays
    {
        private void BeforeInit()
        {
            _imagesDetected = Session.Instance.ThyaImages;
             Session.Instance.Displays.Add(this);
            _isServer = Session.Instance.IsServer;
            _isDedicated = Session.Instance.DedicatedServer;
            _mpActive = Session.Instance.MpActive;
            DisUi.CreateUi(Display);
            _bTime = 100;
            _bInit = true;
        }

        private void StorageSetup()
        {
            if (Set == null) Set = new DisplaySettings(Display);
            if (State == null) State = new DisplayState(Display);
            State.StorageInit();
            State.LoadState();
            Set.LoadSettings();
        }

        internal void ClaimDisplay(long playerId)
        {
            State.Value.ClientOwner = playerId;
            State.Value.Release = false;
            State.NetworkUpdate();
            if (Session.Enforced.Debug >= 2) Log.Line($"AcceptClaim: {State.Value.ClientOwner}");
        }

        internal void AbandonDisplay(bool timeout = false)
        {
            if (Session.Enforced.Debug >= 2) Log.Line($"AbandonClaim: {State.Value.ClientOwner} - TimeOut:{timeout}");

            _waitCount = 0;
            State.Value.ClientOwner = 0;
            State.Value.Release = true;
            State.NetworkUpdate();
        }

        private bool ActiveDisplay()
        {
            if (ShieldComp?.DefenseShields?.MyGrid != Display.CubeGrid)
            {
                Display.CubeGrid.Components.TryGet(out ShieldComp);
            }
            return ShieldComp?.DefenseShields?.Shield != null;
        }

        private void UpdateDisplay()
        {
            var ds = ShieldComp.DefenseShields;
            if (_imagesDetected && Set.Settings.Report == 2)
            {
                if (Display.ShowText) Display.ShowTextureOnScreen();
                var image = UtilsStatic.GetShieldThyaFromFloat(ds.DsState.State.ShieldPercent, 0);
                var oldImage = Display.CurrentlyShownImage;

                if (oldImage != image)
                {
                    Display.RemoveImageFromSelection(oldImage, true);
                    Display.AddImageToSelection(image);
                    Display.NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                }
            }
            else
            {
                if (!Display.ShowText)
                {
                    Display.ShowPublicTextOnScreen();
                    if (Display.FontSize <= 1) Display.FontSize = 1.30f;
                }
                Display.WritePublicText(ds.Shield.CustomInfo);
            }
        }

        internal void UpdateState(DisplayStateValues newState)
        {
            if (newState.MId > State.Value.MId)
            {
                State.Value = newState;
                if (Session.Enforced.Debug >= 3) Log.Line($"UpdateState: DisplayId [{Display.EntityId}]");
            }
        }

        internal void UpdateSettings(DisplaySettingsValues newSettings)
        {
            if (newSettings.MId > Set.Settings.MId)
            {
                SettingsUpdated = true;
                Set.Settings = newSettings;
                if (Session.Enforced.Debug == 3) Log.Line("UpdateSettings for display");
            }
        }

        private void NewSettings()
        {
            if (SettingsUpdated)
            {
                SettingsUpdated = false;
                Set.SaveSettings();
            }
            else if (ClientUiUpdate)
            {
                ClientUiUpdate = false;
                if (!_isServer) Set.NetworkUpdate();
            }
        }

        private void SaveAndSendAll()
        {
            _firstSync = true;
            if (!_isServer) return;
            State.SaveState();
            State.NetworkUpdate();
            if (Session.Enforced.Debug >= 3) Log.Line($"SaveAndSendAll: EnhancerId [{Display.EntityId}]");
        }
    }
}
