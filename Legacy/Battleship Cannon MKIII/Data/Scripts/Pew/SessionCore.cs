using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Weapons.Guns;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace MWI
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SessionCore : MySessionComponentBase
    {
        #region Do Not Change
            private bool setup;
            private bool itemAdded;
            //private List<MyDefinitionId> ammoMagazineList = new List<MyDefinitionId>();
            //private readonly IMyCubeBlock cubeBlock = new MyCubeBlock();
            private readonly HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            //private List<IMyTerminalControl> weaponControls; // store weapon controls


            /*
            private static Dictionary<long, MyTerminalControlComboBoxItem> m_ammoSelectionItems;
            public static Dictionary<long, MyTerminalControlComboBoxItem> AmmoSelectionItems
            {
                get
                {
                    if (m_ammoSelectionItems == null)
                        m_ammoSelectionItems = new Dictionary<long, MyTerminalControlComboBoxItem>();

                    return m_ammoSelectionItems;
                }
            }
            */
        #endregion

        public override void UpdateAfterSimulation()
        {
            if (setup)
                return;

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            if (MyAPIGateway.Session == null)
                return;

            setup = true;
            SessionSetup();
        }

        private void SessionSetup()
        {
            try
            {
                MyAPIGateway.Entities.GetEntities(entities); // iterate existing grids in the world
                foreach (var entity in entities)
                {
                    IsEntityGrid(entity); // apply grid check and subscribe to events
                }

                MyAPIGateway.Entities.OnEntityAdd += IsEntityGrid; // subscribe new grids to grid check and further subscription

                /*
                #region Weapon Terminal Setup
                var turretAmmoSelect = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyLargeTurretBase>("TurretAmmoSelect");
                var launcherAmmoSelect = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMySmallMissileLauncher>("LauncherAmmoSelect");
                var reloadableAmmoSelect = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMySmallMissileLauncherReload>("ReloadableAmmoSelect");

                turretAmmoSelect.Title = MyStringId.GetOrCompute("Ammo Select");
                turretAmmoSelect.Tooltip = MyStringId.GetOrCompute("Select the priority ammo for this turret");

                launcherAmmoSelect.Title = MyStringId.GetOrCompute("Ammo Select");
                launcherAmmoSelect.Tooltip = MyStringId.GetOrCompute("Select the priority ammo for this launcher");

                reloadableAmmoSelect.Title = MyStringId.GetOrCompute("Ammo Select");
                reloadableAmmoSelect.Tooltip = MyStringId.GetOrCompute("Select the priority ammo for this launcher");

                MyAPIGateway.TerminalControls.AddControl<IMyLargeTurretBase>(turretAmmoSelect);
                MyAPIGateway.TerminalControls.AddControl<IMySmallMissileLauncher>(launcherAmmoSelect);
                MyAPIGateway.TerminalControls.AddControl<IMySmallMissileLauncherReload>(reloadableAmmoSelect);

                //MyAPIGateway.TerminalControls.GetControls<IMyUserControllableGun>(out weaponControls);

                turretAmmoSelect.ComboBoxContent = AmmoComboBox;
                //launcherAmmoSelect.ComboBoxContent = AmmoComboBox;
                //reloadableAmmoSelect.ComboBoxContent = AmmoComboBox;

                #endregion
                */
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        private void IsEntityGrid(IMyEntity entity)
        {
            try
            {
                var cubeGrid = entity as MyCubeGrid;
                if (cubeGrid != null)
                {
                    var blockCount = cubeGrid.BlocksCount;
                    var grid = (IMyCubeGrid)cubeGrid;

                    //Unsafe for some reason in 1.187.088
                    //var firstBlock = grid.GetCubeBlock(Vector3I.Zero); // get the starting block
                    //if (blockCount == 1)
                    //{
                    //    SlimBlockAdded(firstBlock); // makes sure the starting block of a new grid gets event treatment
                    //}

                    grid.OnBlockAdded += SlimBlockAdded; // subscribes to block placement event on collected grid
                    grid.OnClose += CubeGridRemoved;
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        // event handler that checks for newly placed blocks
        private void SlimBlockAdded(IMySlimBlock placedBlock)
        {
            try
            {
                var weaponBlock = placedBlock.FatBlock?.GameLogic.GetAs<Core>();
                if (weaponBlock == null) return;

                weaponBlock.justPlaced = true;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        /*
        private void AmmoComboBox(List<MyTerminalControlComboBoxItem> ammoMagazines)
        {
            try
            {
                var block = cubeBlock?.GameLogic.GetAs<Core>();

                if (block != null)
                {
                    var gun = (IMyGunObject<MyGunBase>) block.Entity;
                    var gunDefinition = gun.DefinitionId;

                    var weapon = new MyWeaponDefinition();
                    var ammoItem = new MyTerminalControlComboBoxItem();
                    var ammoIndex = weapon.GetAmmoMagazineIdArrayIndex(gunDefinition);

                    foreach (var mag in weapon.AmmoMagazinesId)
                    {
                        var magName = mag.SubtypeName;

                        for (var i = 0; i < ammoIndex; i++)
                        {
                            ammoMagazines.Add(ammoItem);
                        }
                    }

                    var terminalBlock = gun as IMyTerminalBlock;
                    if (terminalBlock != null)
                    {
                        if (!AmmoSelectionItems.ContainsKey(terminalBlock.EntityId))
                            AmmoSelectionItems.Add(terminalBlock.EntityId, new MyTerminalControlComboBoxItem());
                    }

                    var defaultAmmoId = gun.GunBase.CurrentAmmoMagazineId;
                    var defaultAmmoName = gun.GunBase.CurrentAmmoMagazineDefinition.DisplayNameText;
                    var defaultAmmoSubtype = defaultAmmoId.SubtypeName;

                    var itemName = MyStringId.GetOrCompute(defaultAmmoName + "[ " + defaultAmmoSubtype + " ]");
                    ammoItem.Value = itemName;
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }
        */

        #region Session and Grid Close
        public void CubeGridRemoved(IMyEntity entity)
        {
            var cubeGrid = entity as MyCubeGrid;
            if (cubeGrid != null)
            {
                var grid = (IMyCubeGrid) cubeGrid;

                grid.OnBlockAdded -= SlimBlockAdded;
                grid.OnClose -= CubeGridRemoved;
            }
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= IsEntityGrid;

            entities.Clear();
            //weaponControls.Clear();
        }
        #endregion

    }
}
