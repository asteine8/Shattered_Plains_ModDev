namespace DefenseShields
{
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using VRage.ModAPI;
    using VRage.Utils;

    internal static class DisUi
    {
        #region Create UI
        internal static void CreateUi(IMyTerminalBlock display)
        {
            Session.Instance.CreateDisplayUi(display);
            Session.Instance.DisplayReport.Enabled = block => true;
            Session.Instance.DisplayReport.Visible = ShowControl;
            Session.Instance.DisSep1.Visible = ShowControl;
            //Session.Instance.DisSep2.Visible = ShowControl;
        }

        private static readonly List<MyTerminalControlComboBoxItem> ReportList = new List<MyTerminalControlComboBoxItem>()
        {
            new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Off") },
            new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Stats") },
            new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Graphics") }
        };

        internal static bool ShowControl(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Displays>();
            return comp != null;
        }

        internal static long GetReport(IMyTerminalBlock block)
        {
            var comp = block?.GameLogic?.GetAs<Displays>();
            if (comp == null) return 0;

            return comp.Set.Settings.Report;
        }

        internal static void SetReport(IMyTerminalBlock block, long newValue)
        {
            var comp = block?.GameLogic?.GetAs<Displays>();
            if (comp == null) return;
            comp.Set.Settings.Report = newValue;
            comp.SettingsUpdated = true;
            comp.ClientUiUpdate = true;
        }

        internal static void ListReport(List<MyTerminalControlComboBoxItem> reportList)
        {
            foreach (var report in ReportList) reportList.Add(report);
        }
        #endregion
    }
}
