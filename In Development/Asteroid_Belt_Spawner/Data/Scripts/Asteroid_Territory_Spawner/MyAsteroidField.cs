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
        public string Name = "";
        public string BaseMaterial = "Stone";
        public List<Deposit> Deposits;

        public double MaxVoxelsInRegion = 20;
        public double MinDistanceBetweenVoxels = 250;
        public double MaxAsteroidSize = 128;

        public const double MAX_ASTEROID_SIZE = 256;
        public const double CLUSTER_SPAWN_CHANCE = 0.5;

        public FieldShape Shape = FieldShape.SPHERE;
        public Vector3D Center = Vector3D.Zero;
        public Vector3D Normal = Vector3D.UnitZ;
        public double InnerRadius = 0;
        public double OuterRadius = 1;
        public enum FieldShape {
            SPHERE, SHELL, TOROID
        }

        public MyAsteroidField(FieldShape shape) {
            this.Shape = shape;
        }

        public bool RegionInField(Vector3I region) {
            Vector3D regionPosition = (new Vector3D(region)) * SpawnerCore.REGION_SIZE;
            return PositionInField(regionPosition);
        }

        public bool PositionInField(Vector3D position) {
            bool inField = false;
            double distanceToCenter = (position - this.Center).Length();

            switch (this.Shape) {
                case FieldShape.SPHERE:
                    if (distanceToCenter < OuterRadius) {
                        inField = true;
                    }
                    break;
                case FieldShape.SHELL:
                    if (distanceToCenter < OuterRadius && distanceToCenter > InnerRadius) {
                        inField = true;
                    }
                    break;
                case FieldShape.TOROID:
                    Vector3D CenterToLocation = position - Center;
                    Normal = Vector3D.Normalize(Normal);

                    double planarRadius = (CenterToLocation - Vector3D.Dot(CenterToLocation, Normal) * Normal).Length();
                    double heightOffPlane = Vector3D.Dot(CenterToLocation, Normal);
                    double toroidRadius = (OuterRadius - InnerRadius) / 2;

                    if ( (planarRadius-InnerRadius-toroidRadius) < Math.Sqrt(Math.Pow(toroidRadius,2) - Math.Pow(heightOffPlane,2)) ) {
                        inField = true;
                    }

                    break;
            }

            return inField;
        }
    }
}
