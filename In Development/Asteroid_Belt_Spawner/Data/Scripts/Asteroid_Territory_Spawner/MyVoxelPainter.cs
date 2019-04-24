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
    class MyVoxelPainter {

        const double MAX_DEPOSIT_RADIUS = 100;
        const double MIN_DEPOSIT_RADIUS = 20;

        public List<string> BaseResources;
        public List<string> DepositResources;
        public MyVoxelPainter() {

        }

        public MyVoxelPainter(List<string> baseResources, List<string> depositResources) {
            this.BaseResources = baseResources;
            this.DepositResources = depositResources;
        }

        public void PaintVoxel(IMyVoxelBase voxelMap) {

            // Paint Entire Voxel with Base Material
            double radius = (double)(voxelMap.LocalAABB.Max - voxelMap.LocalAABB.Min).Length();
            IMyVoxelShape fillShape = MyAPIGateway.Session.VoxelMaps.GetSphereVoxelHand();
            fillShape.Transform *= MatrixD.CreateScale(radius);

            byte baseMaterial = SpawnerCore.voxelMaterialDefinitions.GetRandomMaterialByte(BaseResources[SpawnerCore.rand.Next(BaseResources.Count)]);

            MyAPIGateway.Session.VoxelMaps.PaintInShape(voxelMap, fillShape, baseMaterial);

            // Randomly Add 
        }
    }
}
