using System;
using System.Collections;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using VRageMath;

namespace Asteroid_Territory_Spawner {

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class SpawnerCore : MySessionComponentBase {

        public const double REGION_SIZE = 5000; // The size of the cubical region of space. Gotta orgainize space somehow... (Floor divide by this number to get the region)

        public const int VOXEL_UPDATE_PERIOD = 30;
        public const int PLAYER_UPDATE_PERIOD = 600;
        public int clk = 0;

        public bool sessionReady = false;

        public static Logger log;
        public static VoxelMaterialDefinitions voxelMaterialDefinitions;
        public static Random rand;

        public Player_Registrar PlayerRegistrar;

        public Queue<Vector3I> RegionsUpdating;

        public static List<MyAsteroidField> Fields;

        public void Init() {

        }

        public void InitializeSession() {
            // Create Logger for debug
            log = new Logger("asteroid_spawner_log.txt");
            log.DebugMode = true;
            rand = new Random();

            // Create voxel material definitions from world settings (Allows for modded input)
            voxelMaterialDefinitions = new VoxelMaterialDefinitions();

            // Initialize Player Registrar and start tracking players
            this.PlayerRegistrar = new Player_Registrar();
            this.PlayerRegistrar.Init();

            // Get Asteroid Fields
            Fields = AsteroidFieldManager.GetFields();

            // Initialized Queue
            RegionsUpdating = new Queue<Vector3I>();

            log.Log("Initialized Session");

            this.sessionReady = true;
        }
        public override void UpdateBeforeSimulation() {
            // Initialize the session once it is ready
            if (this.sessionReady == false && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null) {
                this.InitializeSession();
            }

            // Main Loop once session is initialized
            if (this.sessionReady) {
                if ((this.clk % VOXEL_UPDATE_PERIOD) == 0 && this.RegionsUpdating.Count != 0) { // Spawn in voxels one at a time

                    // Spawn in a roid into the queued region
                    MyAsteroidField regionField = Fields[0]; // Just use the first field's settings if none availible
                    foreach(MyAsteroidField field in Fields) {
                        if (field.RegionInField(this.RegionsUpdating.Peek()) == true) {
                            regionField = field;
                            break;
                        }
                    }
                    log.Log("Attempting Spawn in Region at " + this.RegionsUpdating.Peek().ToString());
                    if (VoxelSpawner.TrySpawnInRegion(regionField, this.RegionsUpdating.Peek()) == false) {
                        log.Log("Finished Populating Region at " + this.RegionsUpdating.Dequeue().ToString() + " in field " + regionField.Name);
                    }
                }

                if (this.clk == PLAYER_UPDATE_PERIOD) { // Reset the update period and do a player update

                    // Check for new players and update known players
                    this.PlayerRegistrar.RegisterPlayers();
                    this.PlayerRegistrar.UpdatePlayerFieldLocations();
                    List<Vector3I> regions = this.PlayerRegistrar.GetRegionsThatNeedUpdate();
                    foreach (Vector3I region in regions) {
                        // Only add regions that are not already queued
                        if (RegionsUpdating.Contains(region) == false) {
                            RegionsUpdating.Enqueue(region);
                        }
                    }

                    this.clk = 0;
                }

                this.clk++;
            }
        }

        protected override void UnloadData() {
            // Flush and close the log when closing the world
            SpawnerCore.log.Close();
            base.UnloadData();
        }
    }
}
