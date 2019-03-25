using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;

namespace Phoenix.Stargate
{
    internal class HardcoreMode
    {
        public const int StandardNaquadah = 100;
        public const int StandardInteriorPlates = 12;
        public const int HardcoreNaquadah = 300;
        public const int HardcoreInteriorPlates = 52;

        readonly MyDefinitionId[] m_blockTypes =
        {
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate S"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate SO"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate S Small"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate SO Small"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate A"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate U"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate M"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate O"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate A Small"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate U Small"),
            new MyDefinitionId(typeof(MyObjectBuilder_TerminalBlock), "Stargate O Small"),
        };

        readonly Dictionary<MyDefinitionId, MyObjectBuilder_CubeBlockDefinition> m_blockDefinitions = new Dictionary<MyDefinitionId, MyObjectBuilder_CubeBlockDefinition>();

        static HardcoreMode _instance = new HardcoreMode();
        public static HardcoreMode Instance
        {
            get
            {
                //if (!_initialized)
                //    Init();
                return _instance;
            }
        }

        public static void Clean()
        {
            Instance.ResetDefinitions();
            _instance = null;
        }

        private HardcoreMode()
        {
            LoadDefinitions();
        }

        public static void ChangeComponentLevelRatio(MyCubeBlockDefinition definition, float percentage)
        {
            for(int x = 0; x < definition.Components.Count(); x++)
            {
                definition.Components[x].Count = (int)(definition.Components[x].Count * (percentage * 100)) / 100;
            }
        }

        public void SwitchToHardcore()
        {
            Logger.Instance.LogMessage("SwitchToHardcore");
            ChangeGateComponents(HardcoreNaquadah, HardcoreInteriorPlates);
        }

        public void SwitchToStandard()
        {
            Logger.Instance.LogMessage("SwitchToStandard");
            ChangeGateComponents(StandardNaquadah, StandardInteriorPlates);
        }

        public void ChangeGateComponents(int naq, int plate)
        {
            foreach (var defid in m_blockTypes)
            {
                switch (defid.SubtypeName)
                {
                    case "Stargate S":
                    case "Stargate SO":
                    case "Stargate S Small":
                    case "Stargate SO Small":
                        if (Globals.ModName == "Portal")
                            ChangeComponents(defid, naq, plate);
                        break;
                    default:
                        if (Globals.ModName == "Stargate")
                            ChangeComponents(defid, naq, plate);
                        break;
                }
            }
        }

        private void ChangeComponents(MyDefinitionId defid, int naq, int plate)
        {
            Logger.Instance.LogMessage("ChangeComponents");
            MyCubeBlockDefinition def;
            if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defid, out def))
            {
                def.Public = StargateAdmin.Configuration.Buildable;
                for (int x = 0; x < def.Components.Count(); x++)
                {
                    if (def.Components[x].Definition.Id.SubtypeName == "Naquadah")
                    {
                        def.Components[x].Count = naq;
                        continue;
                    }
                    else if (def.Components[x].Definition.Id.SubtypeName == "InteriorPlate")
                    {
                        def.Components[x].Count = plate;
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// Loads all block definitions from the game, to preserve originals.
        /// Call this before changing anything.
        /// </summary>
        public void LoadDefinitions()
        {
            Logger.Instance.LogMessage("LoadDefinitions");
            foreach(var defid in m_blockTypes)
            {
                MyCubeBlockDefinition def;
                if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defid, out def))
                {
                    // Store a *copy* of the definition, so changes can be reverted.
                    var ob = def.GetObjectBuilder() as MyObjectBuilder_CubeBlockDefinition;
                    Logger.Instance.LogAssert(ob != null, "ob != null: " + def.ToString());
                    m_blockDefinitions[defid] = ob;
                }
            }
        }

        /// <summary>
        /// Resets all block definitions in the game. Restores to defaults.
        /// </summary>
        public void ResetDefinitions()
        {
            Logger.Instance.LogMessage("ResetDefinitions");
            foreach (var defid in m_blockTypes)
            {
                MyCubeBlockDefinition def;
                if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defid, out def))
                {
                    var ob = m_blockDefinitions[defid];
                    Logger.Instance.LogAssert(ob != null, "ob != null: " + def.ToString());
                    def.Public = ob.Public;
                    for (int x = 0; x < ob.Components.Count(); x++)
                    {
                        def.Components[x].Count = ob.Components[x].Count;
                    }
                }
            }
        }
    }
}
