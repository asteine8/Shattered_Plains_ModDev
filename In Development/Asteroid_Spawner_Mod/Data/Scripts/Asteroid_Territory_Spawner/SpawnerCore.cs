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
        public const int VOXEL_UPDATE_PERIOD = 60;
        public const int PLAYER_UPDATE_PERIOD = 1200;
        public int clk = 0;

        public bool sessionReady = false;

        public static Logger log;
        public static VoxelMaterialDefinitions voxelMaterialDefinitions;
        public static Random rand;

        public Player_Registrar PlayerRegistrar;

        public Queue<Vector3I> RegionsUpdating;

        public void Init() {

        }

        public void InitializeSession() {
            log = new Logger("asteroid_spawner_log.txt");
            log.DebugMode = true;
            rand = new Random();

            voxelMaterialDefinitions = new VoxelMaterialDefinitions();

            this.PlayerRegistrar = new Player_Registrar();
            this.PlayerRegistrar.Init();

            MyAPIGateway.Utilities.ShowNotification("Initialized", 10000);

            this.sessionReady = true;
            //this.clk = this.numTicksPerUpdate;

            /*
            Vector3D position = new Vector3D(140800, 107000, 100000);

            IMyStorage storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(new Vector3I(128));
            IMyVoxelBase voxel = MyAPIGateway.Session.VoxelMaps.CreateVoxelMap("Test_Asteroid_7", storage, position, 123456789876737);

            IMyVoxelShapeSphere fillSphere = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
            fillSphere.Radius = 25;
            fillSphere.Center = position;
            IMyVoxelShape fillShape = fillSphere as IMyVoxelShape;

            fillShape.Transform = MatrixD.Identity;
            fillShape.Transform *= MatrixD.CreateScale(25);
            fillShape.Transform *= MatrixD.CreateTranslation(position);

            byte baseMaterial = SpawnerCore.voxelMaterialDefinitions.GetRandomMaterialByte("Stone");

            MyAPIGateway.Session.VoxelMaps.FillInShape(voxel, fillSphere, baseMaterial);

            log.Log("Created Voxel Shape with bounds " + fillShape.GetWorldBoundary().Size.ToString("0.00"));
            */

        }
        public override void UpdateBeforeSimulation() {
            if (this.sessionReady == false && MyAPIGateway.Session != null && MyAPIGateway.Utilities != null) {
                this.InitializeSession();
            }

            if (this.sessionReady) {
                if ((this.clk % VOXEL_UPDATE_PERIOD) == 0) { // Spawn in voxels one at a time
                    if (VoxelSpawner.TrySpawnInRegion(RegionsUpdating.Peek()) == false) {
                        // If the region is full, dequeue the region from the queue
                        log.Log("Finished Updating Region at " + RegionsUpdating.Dequeue().ToString());
                    }
                }

                if (this.clk == PLAYER_UPDATE_PERIOD) { // Reset the update period and do a player update
                    this.clk = 0;

                    List<Vector3I> regionsToUpdate = this.PlayerRegistrar.GetRegionsThatNeedUpdate();
                    foreach (Vector3I region in regionsToUpdate) {
                        // Only add regions that are not already queued for voxel population
                        if (RegionsUpdating.Contains(region) == false) {
                            RegionsUpdating.Enqueue(region);
                        }
                    }

                    //MyAPIGateway.Utilities.ShowNotification(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds.ToString("0.0"), 990);
                }

                this.clk++;
            }
        }

        protected override void UnloadData() {
            SpawnerCore.log.Close();
            base.UnloadData();
        }
    }
}
