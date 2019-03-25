using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace Phoenix.Stargate
{
    internal static class Globals
    {
        // NOTE: THIS MUST BE DIFFERENT FOR PORTAL AND STARGATE; Portal = 1, Stargate = 2
        public const int Priority = 2;

        public const string LastUpdate = "2018-05-05";
        public const string UpdateFile = "updates";
        public static readonly string ModName;
        public const int DefaultCloseTime = 6;          // 6 = 10 seconds, see AutoCloseTime property for details
        public static bool ModEnabled = true;           // If multiple mods are detected, subsequent ones will be automatically disabled
        public static IMyModContext ModContext;

        static Globals()
        {
            ModName = typeof(Globals).ToString().Split('.')[1];     // Get the namespace to determine mod name
        }
    }

    internal static class Constants
    {
        #region GUIDS
        public static readonly Guid VanillaCustomDataKey = new Guid("74DE02B3-27F9-4960-B1C4-27351F2B06D1");
        public static readonly Guid StargateDataKey = new Guid("C713C142-11D4-4DC3-964B-25487F74DF5C");
        #endregion GUIDS

        public const double GateInfluenceRadiusNewWorld = 10000;
        public const double GateInfluenceRadiusOldWorld = 1000;
        public const string HashAlphabet = "ABCDEFGHIJKLMNoPQRSTUVWXYZ0123456789/[";
        public const float MaxGateLifetime = 38 * 60 * 1000;    // 38 Minutes
        public const float SuperGateRadius = 180;
        public const float GateRadiusOuter = 3.0f;
        public const float GateRadiusInner = 2.3f;
        public const float GateDepthHalf = 0.25f;

        public const float BaseGateForwardOffset = 1.32f;
        public const float BaseGateUpOffset = 0.6f;
        public const float BaseGateForwardOffsetSphere = 1.5f;

        public const float GateUpOffset = 0.4f;

        public const float BaseGateDifference = BaseGateUpOffset + GateUpOffset;

        public const string IrisName = "Iris";
        public const string ShieldName = "Shield";
        public const string EventHorizonSubpartName = "EventHorizon";
        public const string RingSubpartName = "Ring";
        public const ulong PortalWorkshopID = 377773977;
        public const ulong StargateWorkshopID = 754173702;
        public const int ExitSafeTime = 2;  // in seconds

        public const float ChevronSpacingDegrees_SG1 = 9.230769f;
        public static readonly float ChevronSpacingRadians_SG1 = MathHelper.ToRadians(ChevronSpacingDegrees_SG1);

        // Chevron colors
        public static readonly Color Chevron_SG1 = Color.OrangeRed;
        public static readonly Color Chevron_SGA = new Color(54, 189, 255, 255);
        public static readonly Color Chevron_SGU = Color.White;
        public static readonly Color Chevron_Off = Color.DarkGray;

        // DHD Colors
        public static readonly Color DHD_Dome_Light_Active_SGA = new Color(133, 248, 235, 255);
        public static readonly Color DHD_Dome_Light_Active_SG1 = Color.Red;
        public static readonly Color DHD_Dome_Light_Inactive_SGA = Color.LightPink;
        public static readonly Color DHD_Dome_Light_Inactive_SG1 = Color.LightBlue;
        public static readonly Color DHD_Glyph_Light_Active_SGA = Color.White;
        public static readonly Color DHD_Glyph_Light_Active_SG1 = Color.Goldenrod;

        // Sounds
        public const string Dialing_Sound_Name_SG1_Long = "SG1_Dial_Long";
        public const string Dialing_Sound_Name_SG1_Short = "SG1_Dial_Short";
        public const string Dialing_Sound_Name_SGA_Long = "SGA_Dial_Long";
        public const string Dialing_Sound_Name_SGA_Short = "SGA_Dial_Short";
        public const string Dialing_Sound_Name_SGU_Long = "SGU_Dial_Long";
        public const string Dialing_Sound_Name_SGU_Short = "SGU_Dial_Short";
        public const string Dialing_Sound_Name_SGU_Long1 = "SGU_Dial_Long1";

        // I don't like this, but since the server can't play sounds, there's no reliable callback there
        public const double Dialing_Sound_Length_SG1_Long = 5634;
        public const double Dialing_Sound_Length_SG1_Short = 1066;

        public const double Dialing_Sound_Length_SGA_Long = 5031;
        public const double Dialing_Sound_Length_SGA_Short = 1176;

        public const double Dialing_Sound_Length_SGU_Long1 = 6540;
        public const double Dialing_Sound_Length_SGU_Long = 5252;
        public const double Dialing_Sound_Length_SGU_Short = 1041;

        // Universe angle constants
        public static readonly float GlyphAngleDeltaAverageRadians_SGU = MathHelper.ToRadians(6.8942f);
        public static readonly float GlyphGroupDistanceRadians_SGU = MathHelper.ToRadians(40f);
        public static readonly float GlyphGroupStartRadians_SGU = MathHelper.ToRadians(4f);

        public static readonly IReadOnlyDictionary<string, double> Dialing_Sounds = new Dictionary<string, double>()
        {
            { Dialing_Sound_Name_SG1_Long, Dialing_Sound_Length_SG1_Long },
            { Dialing_Sound_Name_SG1_Short, Dialing_Sound_Length_SG1_Short },
            { Dialing_Sound_Name_SGA_Long, Dialing_Sound_Length_SGA_Long },
            { Dialing_Sound_Name_SGA_Short, Dialing_Sound_Length_SGA_Short },
            { Dialing_Sound_Name_SGU_Long, Dialing_Sound_Length_SGU_Long },
            { Dialing_Sound_Name_SGU_Short, Dialing_Sound_Length_SGU_Short },
            { Dialing_Sound_Name_SGU_Long1, Dialing_Sound_Length_SGU_Long1 },
        };

        public static readonly IReadOnlyDictionary<char, float> RingSymbolAngles_SG1 = new Dictionary<char, float>()
        {
            #region SG1
            { ' ', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 0},
            { 'J', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 1},
            { '2', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 2},
            { '3', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 3},
            { '6', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 4},
            { 'U', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 5},
            { '[', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 6},
            { 'M', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 7},
            { '5', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 8},
            { 'B', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 9},
            { '0', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 10},
            { 'Y', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 11},
            { '8', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 12},
            { 'H', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 13},
            { 'P', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 14},
            { 'S', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 15},
            { 'I', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 16},
            { 'G', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 17},
            { '9', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 18},
            { '4', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 19},
            { '1', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 20},
            { '/', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 21},
            { 'F', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 22},
            { 'T', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 23},
            { 'K', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 24},
            { 'E', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 25},
            { 'N', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 26},
            { 'A', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 27},
            { 'V', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 28},
            { 'R', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 29},
            { 'o', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 30},
            { '7', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 31},
            { 'C', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 32},
            { 'X', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 33},
            { 'Q', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 34},
            { 'L', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 35},
            { 'Z', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 36},
            { 'W', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 37},
            { 'D', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 38},
            #endregion SG1
        };

        public static readonly IReadOnlyDictionary<char, float> RingSymbolAngles_SGU = new Dictionary<char, float>()
        {
            // SGU gate is in 9 groups of 4 glyphs each
            #region SGU
            { ' ', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 0},    // DO NOT USE FOR DIALING

            { 'J', (GlyphGroupDistanceRadians_SGU * 0f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { '2', (GlyphGroupDistanceRadians_SGU * 0f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { '3', (GlyphGroupDistanceRadians_SGU * 0f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { '6', (GlyphGroupDistanceRadians_SGU * 0f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'U', (GlyphGroupDistanceRadians_SGU * 1f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { '[', (GlyphGroupDistanceRadians_SGU * 1f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'M', (GlyphGroupDistanceRadians_SGU * 1f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { '5', (GlyphGroupDistanceRadians_SGU * 1f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'B', (GlyphGroupDistanceRadians_SGU * 2f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { '0', (GlyphGroupDistanceRadians_SGU * 2f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'Y', (GlyphGroupDistanceRadians_SGU * 2f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { '8', (GlyphGroupDistanceRadians_SGU * 2f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'H', (GlyphGroupDistanceRadians_SGU * 3f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { 'P', (GlyphGroupDistanceRadians_SGU * 3f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'S', (GlyphGroupDistanceRadians_SGU * 3f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { 'I', (GlyphGroupDistanceRadians_SGU * 3f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'G', (GlyphGroupDistanceRadians_SGU * 4f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { '9', (GlyphGroupDistanceRadians_SGU * 4f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { '4', (GlyphGroupDistanceRadians_SGU * 4f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { '1', (GlyphGroupDistanceRadians_SGU * 4f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { '/', (GlyphGroupDistanceRadians_SGU * 5f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { 'F', (GlyphGroupDistanceRadians_SGU * 5f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'T', (GlyphGroupDistanceRadians_SGU * 5f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { 'K', (GlyphGroupDistanceRadians_SGU * 5f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'E', (GlyphGroupDistanceRadians_SGU * 6f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { 'N', (GlyphGroupDistanceRadians_SGU * 6f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'A', (GlyphGroupDistanceRadians_SGU * 6f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { 'V', (GlyphGroupDistanceRadians_SGU * 6f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'R', (GlyphGroupDistanceRadians_SGU * 7f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { 'o', (GlyphGroupDistanceRadians_SGU * 7f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { '7', (GlyphGroupDistanceRadians_SGU * 7f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { 'C', (GlyphGroupDistanceRadians_SGU * 7f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            { 'X', (GlyphGroupDistanceRadians_SGU * 8f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 0)},
            { 'Q', (GlyphGroupDistanceRadians_SGU * 8f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 1)},
            { 'L', (GlyphGroupDistanceRadians_SGU * 8f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 2)},
            { 'Z', (GlyphGroupDistanceRadians_SGU * 8f) + GlyphGroupStartRadians_SGU + (GlyphAngleDeltaAverageRadians_SGU * 3)},

            //{ 'W', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 37},
            //{ 'D', MathHelper.ToRadians(ChevronSpacingDegrees_SG1) * 38},
            #endregion SGU
        };

        public static readonly char[] ButtonsToCharacters = (" " + HashAlphabet).ToCharArray();

        public const double DHDTimeoutMS = 30 * 1000;
    }
}
