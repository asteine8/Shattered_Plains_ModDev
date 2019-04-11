using System;
using System.Collections.Generic;
using System.Text;

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
using VRageMath;

namespace Asteroid_Territory_Spawner {
    public class VoxelSpawner {
        public const int MAX_VOXELS_IN_REGION = 10;
        public const int MAX_VOXELS_IN_CLUSTER = 7;
        public const int MAX_DISTANCE_BETWEEN_VOXELS_IN_CLUSTER = 500; // meters
        public const int MIN_DISTANCE_BETWEEN_VOXELS = 50; // meters

        public const double MAX_ASTEROID_SIZE = 256;
        public const double CLUSTER_SPAWN_CHANCE = 0.5;

        public const double REGION_SIZE = 5000; // The size of the cubical region of space. Gotta orgainize space somehow... (Floor divide by this number to get the region)


        public VoxelSpawner() {

        }

        // Gets a region that is adjacent or diagonally adjacent to the specififed region
        public static Vector3I GetRandomRegionAroundRegion(Vector3I region) {

            Vector3I randomRegionSelector;
            
            randomRegionSelector.X = (SpawnerCore.rand.Next(2) == 1) ? -1 : 1;
            randomRegionSelector.Y = (SpawnerCore.rand.Next(2) == 1) ? -1 : 1;
            randomRegionSelector.Z = (SpawnerCore.rand.Next(2) == 1) ? -1 : 1;
            // Populate regions around the current regions
            return (region + randomRegionSelector);
        }
        
        // Returns true if the region is not full. Will populate region one asteoroid at a time
        public static bool TrySpawnInRegion(Vector3I region) {
            if (GetNumAsteroidsInRegion(region) <= MAX_VOXELS_IN_REGION) { // We can add more voxels
                Vector3D spawnLocation = GetNewVoxelSpawnLocation(region);
                //VoxelSpawner.random VoxelSpawner.rand = new VoxelSpawner.random();
                //if (VoxelSpawner.rand.NextDouble() < CLUSTER_SPAWN_CHANCE) {

                //}

                TrySpawnVoxel(spawnLocation);

                return true;
            }
            else {
                return false;
            }
        }

        public static Vector3I GetRegionFromPosition(Vector3D position) {
            Vector3I Region = Vector3I.Zero;

            Region.X = Convert.ToInt32(Math.Floor(position.X / REGION_SIZE));
            Region.X = Convert.ToInt32(Math.Floor(position.Y / REGION_SIZE));
            Region.X = Convert.ToInt32(Math.Floor(position.Z / REGION_SIZE));

            return Region;
        }

        public static int GetNumAsteroidsInRegion(Vector3I region) {
            int numRoids = 0;

            List<IMyVoxelBase> voxelBases = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxelBases);

            foreach (IMyVoxelBase voxelBase in voxelBases) {
                if (VoxelSpawner.GetRegionFromPosition(voxelBase.GetPosition()) == region) {
                    numRoids++;
                }
            }

            //Test Logging
            //foreach (IMyVoxelBase voxelBase in voxelBases) {
            //    SpawnerCore.log.Log("VoxelBases: ");
            //    SpawnerCore.log.Log(voxelBase.Name + "|" + voxelBase.StorageName + "|" + VoxelSpawner.GetRegionFromPosition(voxelBase.GetPosition()).ToString());
            //}

            return numRoids;
        }

        public static Vector3D GetNewVoxelSpawnLocation(Vector3I region) {
            Vector3D spawnLocation = Vector3D.Zero;

            List<IMyVoxelBase> voxelBases = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxelBases);

            bool locationOccupied = true;
            while (locationOccupied) {
                locationOccupied = false;

                spawnLocation = (new Vector3D(region)) * REGION_SIZE;
                spawnLocation += (new Vector3D(SpawnerCore.rand.NextDouble())) * REGION_SIZE;

                foreach (IMyVoxelBase voxelBase in voxelBases) {
                    if ((voxelBase.GetPosition() - spawnLocation).Length() < (MIN_DISTANCE_BETWEEN_VOXELS + MAX_ASTEROID_SIZE/2)) {
                        locationOccupied = true;
                    }
                }
            }

            return spawnLocation;
        }

        public static bool TrySpawnVoxel(Vector3D position) {

            string storageName = "Proc_Astroid_" + position.X.ToString("0") + "_" + position.Y.ToString("0") + "_" + position.Z.ToString("0");
            long entityID = GetIDFromString(storageName);

            try {
                IMyStorage storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(new Vector3I((int)MAX_ASTEROID_SIZE));
                IMyVoxelBase voxel = MyAPIGateway.Session.VoxelMaps.CreateVoxelMap(storageName, storage, position, entityID);

                IMyVoxelShapeSphere fillSphere = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
                fillSphere.Radius = 25;
                fillSphere.Center = position;

                byte baseMaterial = SpawnerCore.voxelMaterialDefinitions.GetRandomMaterialByte("Stone");

                MyAPIGateway.Session.VoxelMaps.FillInShape(voxel, fillSphere, baseMaterial);

                SpawnerCore.log.Log("Spawned Asteroid " + storageName + " with id = " + entityID.ToString());
            } catch (Exception e) {
                SpawnerCore.log.Log("Error while spawning asteroid at " + position.ToString("0.00"));
                SpawnerCore.log.Log(e.Message);
                return false;
            }

            return true;
        }

        // Makes sure the spawned voxel doesn't throw a duplicate id error
        public static long GetIDFromString(string descriptor) {
            long entityID = 0;
            bool idNotDuplicate = false;

            while (!idNotDuplicate) {
                entityID = (long)Math.Abs(descriptor.GetHashCode() + SpawnerCore.rand.Next());

                if (MyAPIGateway.Entities.EntityExists(entityID) == false) {
                    idNotDuplicate = true;
                    break;
                }
            }

            return entityID;
        }

        public static void BuildBaseAsteroid(IMyVoxelBase voxelBase, Vector3D centerPosition) {

        }

        //public static int TrySpawnCluster(Vector3D centralPosition, int maxVoxelsSpawnable) {
        //    int numVoxelsSpawned = 0;

        //    maxVoxelsSpawnable = (maxVoxelsSpawnable > MAX_VOXELS_IN_CLUSTER) ? MAX_VOXELS_IN_CLUSTER : maxVoxelsSpawnable;

        //    return numVoxelsSpawned;
        //}
    }
}
