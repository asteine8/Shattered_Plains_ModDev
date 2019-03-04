using Sandbox.Common.ObjectBuilders;
using VRage.Game.Components;

namespace MWI
{

    #region Turret Casting
        #region Missile-Types
        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "HeavyDefenseTurret")]
        public class HeavyDefenseTurret : Core
        {
            public override void Setup()
            {
                #region HeavyDefenseTurret Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.2f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

                    //SoundPair = new MySoundPair("HDTCannonShot");
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BattleshipCannon")]
        public class Mark1 : Core
        {
            public override void Setup()
            {
                #region Mark1 Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.3f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MK1CannonShot");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BattleshipCannonMK2")]
        public class Mark2 : Core
        {
            public override void Setup()
            {
                #region Mark2 Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.4f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MK2CannonShot");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BattleshipCannonMK22")]
        public class Mark2Round : Core
        {
            public override void Setup()
            {
                #region Mark2Round Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.4f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MK2CannonShot");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BattleshipCannonMK3")]
        public class Mark3 : Core
        {
            public override void Setup()
            {
                #region Mark3 Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.45f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MK3CannonShot");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BFG_M")]
        public class OrbitalCoilCannon : Core
        {
            public override void Setup()
            {
                #region OrbitalCoilCannon Settings

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.55f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MK1CoilCannon");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "TelionAF")]
        public class TelionFlak : Core
        {
            public override void Setup()
            {
                #region TelionFlak Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.05f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("TelionAntiFighter");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "TelionAF_small")]
        public class TelionFlakSmall : Core
        {
            public override void Setup()
            {
                #region TelionFlakSmall Settings
                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.05f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("TelionAntiFighter");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof (MyObjectBuilder_LargeMissileTurret), false, "BFTriCannon")]
        public class X3TriCannon : Core
        {
            public override void Setup()
            {
                #region X3TriCannon Settings
                    isAnimated          = true;

                    BarrelTravelDist    = 2.5f;
                    BarrelPunchSpeed    = 1.25f;
                    BarrelRestoreSpeed  = 0.1f;

                    //ParticleType = "Explosion_Missile";
                    ParticleScale       = 0.475f;

                    TargetMeteors       = "TargetMeteors_Off";
                    TargetMissiles      = "TargetMissiles_Off";
                    TargetSmallShips    = "TargetSmallShips_Off";
                    TargetLargeShips    = "TargetLargeShips_On";
                    TargetCharacters    = "TargetCharacters_Off";
                    TargetStations      = "TargetStations_On";
                    TargetNeutrals      = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("MexHeavyCannon");
            #endregion
            }
        }
        #endregion

        #region Gatling-Types
        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "TelionAFGen2")]
        public class TelionAntiFighter : Core
        {
            public override void Setup()
            {
                #region TelionAntiFighter Settings
                    // ParticleType     = "Explosion_Missile";
                    ParticleScale       = 0.035f;

                    TargetMeteors       = "TargetMeteors_On";
                    TargetMissiles      = "TargetMissiles_Off";
                    TargetSmallShips    = "TargetSmallShips_On";
                    TargetLargeShips    = "TargetLargeShips_On";
                    TargetCharacters    = "TargetCharacters_Off";
                    TargetStations      = "TargetStations_Off";
                    TargetNeutrals      = "TargetNeutrals_On";

            //SoundPair = new MySoundPair("TelionAntiFighter");
            #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false, "TelionAMACM")]
        public class TelionAntiMissile : Core
        {
            public override void Setup()
            {
                #region TelionAntiMissile Settings
                    useMuzzleLogic = false;

                    TargetMeteors = "TargetMeteors_On";
                    TargetMissiles = "TargetMissiles_On";
                    TargetSmallShips = "TargetSmallShips_Off";
                    TargetLargeShips = "TargetLargeShips_Off";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_Off";
                    TargetNeutrals = "TargetNeutrals_On";
            #endregion
            }
        }
        #endregion
    #endregion



    #region Launcher Casting
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "StaticBattery1")]
     public class StaticBattery1 : Core
    {
        public override void Setup()
        {
            #region StaticBattery1 Settings
                // ParticleType = "Explosion_Missile";
                ParticleScale = 0.420f;

                //SoundPair = new MySoundPair("TelionBattery");
            #endregion
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "StaticBattery1Stack")]
    public class StaticBattery1Stack : Core
    {
        public override void Setup()
        {
            #region StaticBattery1Stack Settings
                // ParticleType = "Explosion_Missile";
                ParticleScale = 0.420f;

            //SoundPair = new MySoundPair("TelionBattery");
            #endregion
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "StaticBattery2")]
    public class StaticBattery2 : Core
    {
        public override void Setup()
        {
            #region StaticBattery2 Settings
                // ParticleType = "Explosion_Missile";
                ParticleScale = 0.420f;

            //SoundPair = new MySoundPair("TelionBattery");
            #endregion
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "Static150mm")]
    public class StaticCannon : Core
    {
        public override void Setup()
        {
            #region StaticCannon Settings
                // ParticleType = "Explosion_Missile";
                ParticleScale = 0.25f;

            //SoundPair = new MySoundPair("TelionBattery");
            #endregion
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "Static30mm")]
    public class StaticCannonSmall : Core
    {
        public override void Setup()
        {
            #region StaticCannonSmall Settings
                // ParticleType = "Explosion_Missile";
                ParticleScale = 0.05f;

            //SoundPair = new MySoundPair("TelionBattery");
            #endregion
        }
    }
    #endregion Launcher Casting



    #region Vanilla Casting
        #region Missile-Types
        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "")]
        public class MissileTurretLarge : Core
        {
            public override void Setup()
            {
                #region MissileTurretLarge Settings
                    useCustomMuzzle = false;

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.075f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

                //SoundPair = new MySoundPair("TelionBattery");
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "")]
        public class MissileTurretSmall : Core
        {
            public override void Setup()
            {
                #region MissileTurretSmall Settings
                    useCustomMuzzle = false;

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.075f;

                    TargetMeteors = "TargetMeteors_Off";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_Off";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";

                //SoundPair = new MySoundPair("TelionBattery");
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false, "LargeMissileLauncher")]
        public class LargeRocketLauncher : Core
        {
            public override void Setup()
            {
                #region LargeRocketLauncher Settings
                    useCustomMuzzle = false;

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.09f;

                    //SoundPair = new MySoundPair("TelionBattery");
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false, "")]
        public class SmallRocketLauncher : Core
        {
            public override void Setup()
            {
                #region SmallRocketLauncher Settings
                    useCustomMuzzle = false;

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.06f;

                    //SoundPair = new MySoundPair("TelionBattery");
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false, "SmallRocketLauncherReload")]
        public class SmallRocketLauncherReload : Core
        {
            public override void Setup()
            {
                #region SmallRocketLauncherReload Settings
                    useCustomMuzzle = false;

                    // ParticleType = "Explosion_Missile";
                    ParticleScale = 0.06f;

                    //SoundPair = new MySoundPair("TelionBattery");
                #endregion
            }
        }
        #endregion

        #region Gatling-Types
        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false, "")]
        public class GatlingTurretLarge : Core
        {
            public override void Setup()
            {
                #region GatlingTurretLarge Settings
                    useMuzzleLogic = false;

                    TargetMeteors = "TargetMeteors_On";
                    TargetMissiles = "TargetMissiles_On";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_On";
                    TargetCharacters = "TargetCharacters_On";
                    TargetStations = "TargetStations_On";
                    TargetNeutrals = "TargetNeutrals_On";
                #endregion
            }
        }

        [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false, "SmallGatlingTurret")]
        public class GatlingTurretSmall : Core
        {
            public override void Setup()
            {
                #region GatlingTurretSmall Settings
                    useMuzzleLogic = false;

                    TargetMeteors = "TargetMeteors_On";
                    TargetMissiles = "TargetMissiles_Off";
                    TargetSmallShips = "TargetSmallShips_On";
                    TargetLargeShips = "TargetLargeShips_Off";
                    TargetCharacters = "TargetCharacters_On";
                    TargetStations = "TargetStations_Off";
                    TargetNeutrals = "TargetNeutrals_On";
                #endregion
            }
        }
        #endregion
    #endregion

}