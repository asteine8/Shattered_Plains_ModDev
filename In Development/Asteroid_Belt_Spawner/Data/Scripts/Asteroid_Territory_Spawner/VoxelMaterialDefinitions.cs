using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Collections;
using VRageMath;

namespace Asteroid_Territory_Spawner {
    public class VoxelMaterialDefinitions {
        public static Dictionary<int, string> VoxelMaterialNames = new Dictionary<int, string> {
            {0,"Iron"},
            {1,"Nickle"},
            {2,"Cobalt"},
            {3,"Silicon"},
            {4,"Magnesium"},
            {5,"Uranium"},
            {6,"Silver"},
            {7,"Gold"},
            {8,"Platnium"},
            {9,"Ice"},
            {10,"Stone"}
        };

        public Dictionary<string, List<MyVoxelMaterialDefinition>> VoxelMaterialsByResource;

        public VoxelMaterialDefinitions() {
            this.VoxelMaterialsByResource = new Dictionary<string, List<MyVoxelMaterialDefinition>>();
            this.updateDefinitions();
        }

        public void updateDefinitions() {
            foreach (MyVoxelMaterialDefinition materialDefinition in MyDefinitionManager.Static.GetVoxelMaterialDefinitions()) {
                foreach(KeyValuePair<int, string> VoxelMaterialName in VoxelMaterialNames) {
                    // Only add materials that spawn in asteroids
                    if (materialDefinition.MinedOre == VoxelMaterialName.Value && materialDefinition.SpawnsInAsteroids) {
                        if (!this.VoxelMaterialsByResource.ContainsKey(VoxelMaterialName.Value)) {
                            this.VoxelMaterialsByResource.Add(VoxelMaterialName.Value, new List<MyVoxelMaterialDefinition>());
                        }
                        this.VoxelMaterialsByResource[VoxelMaterialName.Value].Add(materialDefinition);
                    }
                }
            }
        }

        public byte GetRandomMaterialByte(string resourceString) {
            Random rand = new Random();

            List<MyVoxelMaterialDefinition> resourceDefinitions = this.VoxelMaterialsByResource[resourceString];

            return resourceDefinitions[rand.Next(resourceDefinitions.Count)].Index;
        }
    }
}
