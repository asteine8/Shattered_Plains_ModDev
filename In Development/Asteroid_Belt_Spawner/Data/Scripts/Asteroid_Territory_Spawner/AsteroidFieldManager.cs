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
    public static class AsteroidFieldManager {

        // Read Field Data From config (Currently just sets values because I'm too lazy to implement a XML parser)
        public static List<MyAsteroidField> GetFields() {
            List<MyAsteroidField> fields = new List<MyAsteroidField>();

            MyAsteroidField field1 = new MyAsteroidField(MyAsteroidField.FieldShape.SPHERE);
            field1.Name = "Asteroid Field A";
            field1.Center = new Vector3D(0, 750000, -200000);
            field1.OuterRadius = 200000;
            field1.Deposits = new List<Deposit> {
                new Deposit("Iron", 1),
                new Deposit("Nickel", 0.5),
                new Deposit("Cobalt", 0.25),
                new Deposit("Silicon", 0.35),
                new Deposit("Gold", 0.25),
                new Deposit("Silver", 0.25)
            };

            fields.Add(field1);

            MyAsteroidField field2 = new MyAsteroidField(MyAsteroidField.FieldShape.TOROID);
            field2.Name = "Asteroid Field B";
            field2.Center = new Vector3D(0, 750000, -200000);
            field2.OuterRadius = 200000;
            field2.Deposits = new List<Deposit> {
                new Deposit("Iron", 1),
                new Deposit("Nickel", 0.5),
                new Deposit("Cobalt", 0.25),
                new Deposit("Silicon", 0.35),
                new Deposit("Gold", 1),
                new Deposit("Silver", 1),
                new Deposit("Platnium", 0.25)
            };

            fields.Add(field2);

            foreach(MyAsteroidField field in fields) {
                SpawnerCore.log.Log("Added asteroid field '" + field.Name + "'");
            }

            return fields;
        }
    }
}
