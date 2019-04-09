using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Vicizlat.MultifloorElevator
{
    public partial class MultifloorElevator
    {
        private static bool ControlsInited = false;
        private List<IMyTerminalControl> ControlsList = new List<IMyTerminalControl>();
        private string AddToCustomData;
        private bool TerminalControlsChanged;

        private void LoadTerminalControlsSettings()
        {
            if (Elevator_block.CustomData.Contains("IgnoreDistanceLockOff")) IgnoreDistanceLock = false;
            else IgnoreDistanceLock = true;
            if (Elevator_block.CustomData.Contains("StopElevatorMusic")) PlayElevatorMusic = false;
            else PlayElevatorMusic = true;
            if (!Elevator_block.CustomData.Contains("MusicSelector")) MusicSelector = 1;
            else MusicSelector = GetNumber("MusicSelector");
            if (Elevator_block.CustomData.Contains("StopElevatorMusicNearCabin")) PlayElevatorMusicNearCabin = false;
            else PlayElevatorMusicNearCabin = true;
            if (!Elevator_block.CustomData.Contains("MusicVolume")) MusicVolume = 10;
            else MusicVolume = GetNumber("MusicVolume");
            if (!Elevator_block.CustomData.Contains("CabinLightRed")) CabinLightColor.R = 255;
            else CabinLightColor.R = (byte)GetNumber("CabinLightRed");
            if (!Elevator_block.CustomData.Contains("CabinLightGreen")) CabinLightColor.G = 255;
            else CabinLightColor.G = (byte)GetNumber("CabinLightGreen");
            if (!Elevator_block.CustomData.Contains("CabinLightBlue")) CabinLightColor.B = 255;
            else CabinLightColor.B = (byte)GetNumber("CabinLightBlue");
            if (!Elevator_block.CustomData.Contains("CabinLightRange")) CabinLightRange = 3;
            else CabinLightRange = GetNumberFloat("CabinLightRange");
            if (!Elevator_block.CustomData.Contains("CabinLightIntensity")) CabinLightIntensity = 2;
            else CabinLightIntensity = GetNumberFloat("CabinLightIntensity");
            //if (Elevator_block.CustomData.Contains("HideChristmasLights")) ShowChristmasLights = false;
            //else ShowChristmasLights = true;
        }

        private int GetNumber(string Name)
        {
            int ValuePosition = Elevator_block.CustomData.IndexOf("[", Elevator_block.CustomData.IndexOf(Name)) + 1;
            int ValueLenght = Elevator_block.CustomData.IndexOf("]", Elevator_block.CustomData.IndexOf(Name)) - ValuePosition;
            char[] _charsValue = new char[ValueLenght];
            Elevator_block.CustomData.CopyTo(ValuePosition, _charsValue, 0, ValueLenght);
            return Int32.Parse(new string(_charsValue, 0, ValueLenght));
        }

        private float GetNumberFloat(string Name)
        {
            int ValuePosition = Elevator_block.CustomData.IndexOf("[", Elevator_block.CustomData.IndexOf(Name)) + 1;
            int ValueLenght = Elevator_block.CustomData.IndexOf("]", Elevator_block.CustomData.IndexOf(Name)) - ValuePosition;
            char[] _charsValue = new char[ValueLenght];
            Elevator_block.CustomData.CopyTo(ValuePosition, _charsValue, 0, ValueLenght);
            return Single.Parse(new string(_charsValue, 0, ValueLenght));
        }

        private void CreateTerminalControls()
        {
            if (ControlsInited) return;
            ControlsInited = true;
            var IgnoreDistanceCheck = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAdvancedDoor>("IgnoreDistanceCheck");
            IgnoreDistanceCheck.Title = MyStringId.GetOrCompute("Ignore Safe Distance Lock");
            IgnoreDistanceCheck.Tooltip = MyStringId.GetOrCompute("Ignore the Distance Lock that prevents the use of the Elevator beyond 50 000 kilometers from world center.\nMy tests showed this problem may have been resolved but I am leaving the check for now, just in case.\nIgnore is ON by default, but if you are having problems with the Elevator operation, you may need to turn it OFF.");
            IgnoreDistanceCheck.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            IgnoreDistanceCheck.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().IgnoreDistanceLock;
            IgnoreDistanceCheck.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().IgnoreDistanceLock = v;
            IgnoreDistanceCheck.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(IgnoreDistanceCheck);

            var ElevatorMusicCheck = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAdvancedDoor>("ElevatorMusicCheck");
            ElevatorMusicCheck.Title = MyStringId.GetOrCompute("Play Elevator Music");
            ElevatorMusicCheck.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            ElevatorMusicCheck.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().PlayElevatorMusic;
            ElevatorMusicCheck.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().PlayElevatorMusic = v;
            ElevatorMusicCheck.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(ElevatorMusicCheck);

            var MusicSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAdvancedDoor>("MusicSlider");
            MusicSlider.Title = MyStringId.GetOrCompute("Select Elevator Music");
            MusicSlider.Tooltip = MyStringId.GetOrCompute("Select Elevator Music track between 1 and 4.");
            MusicSlider.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            MusicSlider.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().MusicSelector;
            MusicSlider.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().MusicSelector = (int)v;
            MusicSlider.SetLimits(1, 4);
            MusicSlider.Writer = (b, s) => s.Append(b.GameLogic.GetAs<MultifloorElevator>().MusicSelector);
            MusicSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(MusicSlider);

            var MusicNearCabinCheck = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAdvancedDoor>("MusicNearCabinCheck");
            MusicNearCabinCheck.Title = MyStringId.GetOrCompute("Play Music Near Cabin");
            MusicNearCabinCheck.Tooltip = MyStringId.GetOrCompute("Select if you want to play the Elevator Music also when you are near the Cabin, not only while moving.\nIf Elevator Music is disabled by the Check Box above this has no effect.");
            MusicNearCabinCheck.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            MusicNearCabinCheck.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().PlayElevatorMusicNearCabin;
            MusicNearCabinCheck.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().PlayElevatorMusicNearCabin = v;
            MusicNearCabinCheck.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(MusicNearCabinCheck);

            var MusicVolumeSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAdvancedDoor>("MusicVolumeSlider");
            MusicVolumeSlider.Title = MyStringId.GetOrCompute("Music Volume");
            MusicVolumeSlider.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            MusicVolumeSlider.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().MusicVolume;
            MusicVolumeSlider.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().MusicVolume = (int)v;
            MusicVolumeSlider.SetLimits(0, 100);
            MusicVolumeSlider.Writer = (b, s) => s.Append(b.GameLogic.GetAs<MultifloorElevator>().MusicVolume);
            MusicVolumeSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(MusicVolumeSlider);

            var Separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyAdvancedDoor>(string.Empty);
            Separator.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(Separator);

            var CabinLightLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyAdvancedDoor>("CabinLightLabel");
            CabinLightLabel.Label = MyStringId.GetOrCompute("Cabin Light Controls");
            CabinLightLabel.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(CabinLightLabel);

            var CabinLightColorControl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyAdvancedDoor>("CabinLightColorControl");
            CabinLightColorControl.Title = MyStringId.GetOrCompute("Color");
            CabinLightColorControl.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            CabinLightColorControl.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightColor;
            CabinLightColorControl.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightColor = v;
            CabinLightColorControl.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(CabinLightColorControl);

            var CabinLightRangeSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAdvancedDoor>("CabinLightRangeSlider");
            CabinLightRangeSlider.Title = MyStringId.GetOrCompute("Range");
            CabinLightRangeSlider.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            CabinLightRangeSlider.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightRange;
            CabinLightRangeSlider.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightRange = v;
            CabinLightRangeSlider.SetLimits(1f, 10f);
            CabinLightRangeSlider.Writer = (b, s) => s.Append(b.GameLogic.GetAs<MultifloorElevator>().CabinLightRange);
            CabinLightRangeSlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(CabinLightRangeSlider);

            var CabinLightIntensitySlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyAdvancedDoor>("CabinLightIntensitySlider");
            CabinLightIntensitySlider.Title = MyStringId.GetOrCompute("Intensity");
            CabinLightIntensitySlider.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            CabinLightIntensitySlider.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightIntensity;
            CabinLightIntensitySlider.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().CabinLightIntensity = v;
            CabinLightIntensitySlider.SetLimits(0.1f, 10f);
            CabinLightIntensitySlider.Writer = (b, s) => s.Append(b.GameLogic.GetAs<MultifloorElevator>().CabinLightIntensity);
            CabinLightIntensitySlider.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(CabinLightIntensitySlider);

            //var ChristmasLightsCheck = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyAdvancedDoor>("ChristmasLightsCheck");
            //ChristmasLightsCheck.Title = MyStringId.GetOrCompute("Show Christmas Lights");
            //ChristmasLightsCheck.Visible = (b) => b.GameLogic.GetAs<MultifloorElevator>() != null;
            //ChristmasLightsCheck.Getter = (b) => b.GameLogic.GetAs<MultifloorElevator>().ShowChristmasLights;
            //ChristmasLightsCheck.Setter = (b, v) => b.GameLogic.GetAs<MultifloorElevator>().ShowChristmasLights = v;
            //ChristmasLightsCheck.SupportsMultipleBlocks = true;
            //MyAPIGateway.TerminalControls.AddControl<IMyAdvancedDoor>(ChristmasLightsCheck);

            MyAPIGateway.TerminalControls.GetControls<Sandbox.ModAPI.Ingame.IMyAdvancedDoor>(out ControlsList);
            ControlsList.First((control) => control.Id == "Open").Visible = (b) => b is IMyAdvancedDoor ? false : true;
            ControlsList.Clear();
        }

        private void CheckTerminalControlSettings()
        {
            if (!TerminalControlsChanged)
            {
                if (IgnoreDistanceLock != oldIgnoreDistanceLock)
                {
                    TerminalControlsChanged = true;
                    oldIgnoreDistanceLock = IgnoreDistanceLock;
                }
                if (PlayElevatorMusic != oldPlayElevatorMusic)
                {
                    TerminalControlsChanged = true;
                    oldPlayElevatorMusic = PlayElevatorMusic;
                }
                if (MusicSelector != oldMusicSelector)
                {
                    TerminalControlsChanged = true;
                    oldMusicSelector = MusicSelector;
                }
                if (PlayElevatorMusicNearCabin != oldPlayElevatorMusicNearCabin)
                {
                    TerminalControlsChanged = true;
                    oldPlayElevatorMusicNearCabin = PlayElevatorMusicNearCabin;
                }
                if (MusicVolume != oldMusicVolume)
                {
                    TerminalControlsChanged = true;
                    oldMusicVolume = MusicVolume;
                }
                if (CabinLightColor != oldCabinLightColor)
                {
                    TerminalControlsChanged = true;
                    oldCabinLightColor = CabinLightColor;
                }
                if (CabinLightRange != oldCabinLightRange)
                {
                    TerminalControlsChanged = true;
                    oldCabinLightRange = CabinLightRange;
                }
                if (CabinLightIntensity != oldCabinLightIntensity)
                {
                    TerminalControlsChanged = true;
                    oldCabinLightIntensity = CabinLightIntensity;
                }
                //if (ShowChristmasLights != oldShowChristmasLights)
                //{
                //    TerminalControlsChanged = true;
                //    oldShowChristmasLights = ShowChristmasLights;
                //}
            }
        }

        private void SaveTerminalControlSettings()
        {
            if (TerminalControlsChanged)
            {
                AddToCustomData = "";
                if (!IgnoreDistanceLock) AddToCustomData += "IgnoreDistanceLockOff\n";
                if (!PlayElevatorMusic) AddToCustomData += "StopElevatorMusic\n";
                AddToCustomData += "MusicSelector[" + MusicSelector + "]\n";
                if (!PlayElevatorMusicNearCabin) AddToCustomData += "StopElevatorMusicNearCabin\n";
                AddToCustomData += "MusicVolume[" + MusicVolume + "]\n";
                AddToCustomData += "CabinLightRed[" + CabinLightColor.R + "]\n";
                AddToCustomData += "CabinLightGreen[" + CabinLightColor.G + "]\n";
                AddToCustomData += "CabinLightBlue[" + CabinLightColor.B + "]\n";
                AddToCustomData += "CabinLightRange[" + CabinLightRange + "]\n";
                AddToCustomData += "CabinLightIntensity[" + CabinLightIntensity + "]\n";
                //if (!ShowChristmasLights) AddToCustomData += "HideChristmasLights";
                Elevator_block.CustomData = AddToCustomData;
                TerminalControlsChanged = false;
            }
        }
    }
}