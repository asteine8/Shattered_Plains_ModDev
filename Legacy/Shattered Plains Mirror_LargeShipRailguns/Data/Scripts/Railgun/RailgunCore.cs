using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using Rexxar;
using Rexxar.Communication;
using Whiplash.ArmorPiercingProjectiles;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRageMath;
using VRage.Utils;

namespace Whiplash.Railgun
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class RailgunCore : MySessionComponentBase
    {
        #region Member Fields
        const string _configurationFileName = "RailgunConfig.sbc";

        public static RailgunConfig MyConfig { get; private set; } = new RailgunConfig()
        {
            VersionNumber = 1,
            ArtificialGravityMultiplier = 2,
            NaturalGravityMultiplier = 1,
            DrawProjectileTrails = true,
            PenetrationDamage = 33000,
            ExplosionRadius = 0f,
            ExplosionDamage = 0f,
            ShouldExplode = true,
            ShouldPenetrate = true,
            PenetrationRange = 100f,
        };

        public static bool IsServer;
        public static bool SessionInit { get; private set; } = false;
        private int _count;
        static List<ArmorPiercingProjectile> liveProjectiles = new List<ArmorPiercingProjectile>();
        static Dictionary<long, RailgunProjectileData> railgunDataDict = new Dictionary<long, RailgunProjectileData>();
        static HashSet<MyPlanet> _planets = new HashSet<MyPlanet>();
        #endregion

        #region Update and Init
        private void Initialize()
        {
            IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
            Communication.Register();

            if (IsServer)
                LoadConfig();
        }

        public override void UpdateAfterSimulation()
        {
            if (!SessionInit)
            {
                Initialize();
                SessionInit = true;
            }

            if (++_count % 10 == 0)
                Settings.SyncSettings();
        }

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                SimulateProjectiles();
        }
        #endregion

        #region Projectile Methods
        private static void SimulateProjectiles()
        {
            //projectile simulation
            for (int i = liveProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = liveProjectiles[i];
                projectile.Update();

                if (projectile.Killed)
                    liveProjectiles.RemoveAt(i);

                var tracerData = projectile.GetTracerData();

                foreach (var tracer in tracerData)
                {
                    RailgunMessage.SendToClients(tracer);
                }
            }
        }

        public static void ShootProjectileServer(RailgunFireData fireData)
        {
            RailgunProjectileData projectileData;
            bool registered = railgunDataDict.TryGetValue(fireData.ShooterID, out projectileData);
            if (!registered)
                return;

            var projectile = new ArmorPiercingProjectile(fireData, projectileData);
            AddProjectile(projectile);
        }

        public static void DrawProjectileClient(RailgunTracerData tracerData)
        {
            RailgunProjectileData projectileData;
            bool registered = railgunDataDict.TryGetValue(tracerData.ShooterID, out projectileData);
            if (!registered)
                return;

            var projectile = new ArmorPiercingProjectileClient(tracerData, projectileData);
            projectile.DrawTracer();
        }

        public static void AddProjectile(ArmorPiercingProjectile projectile)
        {
            liveProjectiles.Add(projectile);
        }
        #endregion

        #region Railgun Register
        public static void RegisterRailgun(long entityID, RailgunProjectileData data)
        {
            railgunDataDict[entityID] = data;
        }

        public static void UnregisterRailgun(long entityID)
        {
            if (railgunDataDict.ContainsKey(entityID))
                railgunDataDict.Remove(entityID);
        }
        #endregion

        #region Planets and Gravity
        private void AddPlanet(IMyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
                _planets.Add(planet);
        }

        private void RemovePlanet(IMyEntity entity)
        {
            var planet = entity as MyPlanet;
            if (planet != null)
                _planets.Remove(planet);
        }

        public static Vector3D GetNaturalGravityAtPoint(Vector3D point)
        {
            var gravity = Vector3D.Zero;
            foreach (var planet in _planets)
            {
                IMyGravityProvider gravityProvider = planet.Components.Get<MyGravityProviderComponent>();
                if (gravityProvider != null)
                    gravity += gravityProvider.GetWorldGravity(point);
            }
            return gravity;
        }
        #endregion

        #region Load and Unload Data
        public override void LoadData()
        {
            MyAPIGateway.Entities.OnEntityAdd += AddPlanet;
            MyAPIGateway.Entities.OnEntityRemove += RemovePlanet;

            base.LoadData();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Communication.Unregister();
            MyAPIGateway.Entities.OnEntityAdd -= AddPlanet;
            MyAPIGateway.Entities.OnEntityRemove -= RemovePlanet;
        }
        #endregion

        #region Config Save and Load
        private static void SaveConfig(RailgunConfig config)
        {
            using (var Writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_configurationFileName, typeof(RailgunCore)))
                Writer.Write(MyAPIGateway.Utilities.SerializeToXML(config));
            MyAPIGateway.Utilities.ShowNotification($"{_configurationFileName} saved to 'AppData\\Roaming\\SpaceEngineers\\Storage'", 10000, "Green");
        }

        private void LoadConfig()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(_configurationFileName, typeof(RailgunCore)))
                {
                    SaveConfig(MyConfig);
                }
                else
                {
                    bool refresh = false;
                    using (var Reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(_configurationFileName, typeof(RailgunCore)))
                    {
                        string settings = Reader.ReadToEnd();
                        var config = MyAPIGateway.Utilities.SerializeFromXML<RailgunConfig>(settings);

                        if (config.VersionNumber < MyConfig.VersionNumber)
                        {
                            refresh = true;
                        }
                        else
                        {
                            MyConfig = config;
                        }
                    }

                    if (refresh)
                    {
                        MyAPIGateway.Utilities.ShowNotification($"{_configurationFileName} out of date. Overwriting...", 10000, "Green");
                        MyAPIGateway.Utilities.DeleteFileInLocalStorage(_configurationFileName, typeof(RailgunCore));
                        SaveConfig(MyConfig);
                    }
                    else
                        MyAPIGateway.Utilities.ShowNotification($"{_configurationFileName} loaded from 'AppData\\Roaming\\SpaceEngineers\\Storage'", 10000, "Green");
                }
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in config load", 10000, "Red");
                MyLog.Default.WriteLine(e);
            }
        }

        public struct RailgunConfig
        {
            public int VersionNumber;
            public float ArtificialGravityMultiplier;
            public float NaturalGravityMultiplier;
            public bool DrawProjectileTrails;
            public bool ShouldExplode;
            public float ExplosionRadius;
            public float ExplosionDamage;
            public bool ShouldPenetrate;
            public float PenetrationDamage;
            public float PenetrationRange;
        }
        #endregion
    }
}
