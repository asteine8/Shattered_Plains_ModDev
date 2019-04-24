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
    public static class VoxelSpawner {

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
        public static bool TrySpawnInRegion(MyAsteroidField field, Vector3I region) {
            if (GetNumAsteroidsInRegion(region) <= field.MaxVoxelsInRegion) { // We can add more voxels
                Vector3D spawnLocation = GetNewVoxelSpawnLocation(field, region);
                //VoxelSpawner.random VoxelSpawner.rand = new VoxelSpawner.random();
                //if (VoxelSpawner.rand.NextDouble() < CLUSTER_SPAWN_CHANCE) {

                //}

                TrySpawnVoxel(field, spawnLocation);

                return true;
            }
            else {
                return false;
            }
        }

        public static Vector3I GetRegionFromPosition(Vector3D position) {
            Vector3I Region = Vector3I.Zero;

            Region.X = Convert.ToInt32(Math.Floor(position.X / SpawnerCore.REGION_SIZE));
            Region.Y = Convert.ToInt32(Math.Floor(position.Y / SpawnerCore.REGION_SIZE));
            Region.Z = Convert.ToInt32(Math.Floor(position.Z / SpawnerCore.REGION_SIZE));

            return Region;
        }

        public static int GetNumAsteroidsInRegion(Vector3I region) {
            int numRoids = 0;

            List<IMyVoxelBase> voxelBases = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxelBases);

            foreach (IMyVoxelBase voxelBase in voxelBases) {
                if (GetRegionFromPosition(voxelBase.GetPosition()) == region) {
                    numRoids++;
                }
                //SpawnerCore.log.Log("Found voxel '" + voxelBase.Name + "' at " + GetRegionFromPosition(voxelBase.GetPosition()).ToString());
            }
            
            //SpawnerCore.log.Log("Found " + numRoids.ToString() + " asteroids in region " + region.ToString());

            return numRoids;
        }

        public static Vector3D GetNewVoxelSpawnLocation(MyAsteroidField field, Vector3I region) {
            Vector3D spawnLocation = Vector3D.Zero;

            List<IMyVoxelBase> voxelBases = new List<IMyVoxelBase>();
            MyAPIGateway.Session.VoxelMaps.GetInstances(voxelBases);

            bool locationOccupied = true;
            while (locationOccupied) {
                locationOccupied = false;

                spawnLocation = (new Vector3D(region)) * SpawnerCore.REGION_SIZE;
                spawnLocation += (new Vector3D(SpawnerCore.rand.NextDouble())) * SpawnerCore.REGION_SIZE;

                foreach (IMyVoxelBase voxelBase in voxelBases) {
                    if ((voxelBase.GetPosition() - spawnLocation).Length() < (field.MinDistanceBetweenVoxels + field.MaxAsteroidSize/2)) {
                        locationOccupied = true;
                    }
                }
            }

            return spawnLocation;
        }

        public static bool TrySpawnVoxel(MyAsteroidField field, Vector3D position) {

            string storageName = "Proc_Astroid_" + position.X.ToString("0") + "_" + position.Y.ToString("0") + "_" + position.Z.ToString("0");
            long entityID = GetIDFromString(storageName);

            try {
                IMyStorage storage = MyAPIGateway.Session.VoxelMaps.CreateStorage(new Vector3I((int)field.MaxAsteroidSize));
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
