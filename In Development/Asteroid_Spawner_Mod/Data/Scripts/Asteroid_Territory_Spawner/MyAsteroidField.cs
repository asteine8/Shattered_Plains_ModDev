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
using VRage.Utils;
using VRageMath;

namespace Asteroid_Territory_Spawner {
    public class MyAsteroidField {
        public FieldShape Shape;
        public Vector3D Center;
        public double InnerRadius;
        public double OuterRadius;
        public enum FieldShape {
            SPHERE, SHELL, TOROID
        }

        public MyAsteroidField(FieldShape shape) {
            this.Shape = shape;
        }

        public bool RegionInField(Vector3I region) {
            bool inField = false;
            Vector3D regionPosition = (new Vector3D(region)) * VoxelSpawner.REGION_SIZE;
            double distanceToCenter = (regionPosition - this.Center).Length();

            switch (this.Shape) {
                case FieldShape.SPHERE:
                    if (distanceToCenter < OuterRadius) {
                        inField = true;
                    }
                    break;
                case FieldShape.SHELL:
                    break;
                case FieldShape.TOROID:
                    break;
            }

            return inField;
        }


    }
}
