using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;



//Muzzle Flash and Recoil Animations by Digi. Updated by PhoenixTheSage.
namespace MWI
{
    public class Core : MyGameLogicComponent
    {
        #region Settings
            protected bool useMuzzleLogic       = true;  // set false for weapons with gatling ammo types
            protected bool useCustomMuzzle      = true;  // set false if there's no recoil dummy for a little extra optimization
            protected bool isAnimated           = false; // set true if weapon is rigged for recoil

            protected float BarrelTravelDist    = 2.5f;
            protected float BarrelPunchSpeed    = 1.25f;
            protected float BarrelRestoreSpeed  = 0.1f;

            protected string ParticleType       = "Explosion_Missile";
            protected float ParticleScale       = 0.475f;

            // default AI filters
            protected string TargetMeteors      = "TargetMeteors_On";
            protected string TargetMissiles     = "TargetMissiles_Off";
            protected string TargetSmallShips   = "TargetSmallShips_On";
            protected string TargetLargeShips   = "TargetLargeShips_On";
            protected string TargetCharacters   = "TargetCharacters_Off";
            protected string TargetStations     = "TargetStations_On";
            protected string TargetNeutrals     = "TargetNeutrals_On";

            //protected MySoundPair SoundPair = null; // new MySoundPair("SoundName without Arc or Real prefix");

            public virtual void Setup() { }
        #endregion Settings

        #region Do Not Change
            public bool first = true;
            public bool justPlaced;
            
            private long lastShotTime;
            private readonly List<AnimatedBarrel> animatingBarrels = new List<AnimatedBarrel>();
            private MatrixD muzzleLocalMatrix;
            private MatrixD localWorldMatrix;
            private Vector3D effectWorldPos;
            private uint effectID;
            private IMyCubeBlock block;

             private bool test = true;

            //private MyEntity3DSoundEmitter soundEmitter;
        #endregion

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                block = Entity as IMyCubeBlock;

                if (block?.CubeGrid.Physics == null) // ignore ghost grids
                    return;

                var gun = Entity as IMyGunObject<MyGunBase>;

                if (gun == null) return;

                if(first)
                {
                    first = false;

                    lastShotTime = gun.GunBase.LastShootTime.Ticks;
                    muzzleLocalMatrix = gun.GunBase.GetMuzzleLocalMatrix();

                    //if (SoundPair != null)
                    //    soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);

                    Setup(); // call the overwriteable method for other guns to change this class' variables

                    if (justPlaced)
                    {
                        justPlaced = false;

                        var placed = block as IMyLargeTurretBase;
                        if (placed != null)
                        {
                            //MyAPIGateway.Utilities.ShowNotification("[ Valid turret, attempting to set filters ]", 10000, MyFontEnum.Blue);
                            placed.ApplyAction(TargetMeteors);
                            placed.ApplyAction(TargetMissiles);
                            placed.ApplyAction(TargetSmallShips);
                            placed.ApplyAction(TargetLargeShips);
                            placed.ApplyAction(TargetCharacters);
                            placed.ApplyAction(TargetStations);
                            placed.ApplyAction(TargetNeutrals);
                        }
                    }
                }

                if (!block.IsFunctional) // block broken, pause everything (even ongoing animations)
                    return;

                if (!useMuzzleLogic)
                    return;

                #region Animating barrels individually
                // only animate if custom recoil dummy is setup with appropriate barrel subparts
                if (isAnimated)
                {
                    for (var i = animatingBarrels.Count - 1; i >= 0; i--) // looping backwards to allow removing mid-loop
                    {
                        var data = animatingBarrels[i];

                        if (data.subpart.Closed) // subpart for some reason no longer exists
                        {
                            animatingBarrels.RemoveAt(i);
                            continue;
                        }

                        var m = data.subpart.PositionComp.LocalMatrix;

                        if (!data.restoring) // recoiling/moving backwards
                        {
                            data.travel += Math.Max(2f - data.travel / 2.5f, 0.001f); // a damping effect near the end
                            m.Translation = data.initialTranslation + m.Down * Math.Min(data.travel, BarrelTravelDist);

                            if (data.travel >= BarrelTravelDist)
                            {
                                data.travel = BarrelTravelDist;
                                data.restoring = true;
                            }
                        }
                        else // restoring/moving forwards
                        {
                            if (data.travel > BarrelTravelDist / 3)
                                data.travel -= Math.Max(0.1f - data.travel / 65, 0.0005f); // first part of retraction, accelerating
                            else
                                data.travel *= 0.96f; // second part, deaccelerating

                            if (data.travel <= 0.001f)
                            {
                                m.Translation = data.initialTranslation;
                                animatingBarrels.RemoveAt(i);
                            }
                            else
                            {
                                m.Translation = data.initialTranslation + m.Down * data.travel;
                            }
                        }

                        data.subpart.PositionComp.SetLocalMatrix(ref m, gun, true);
                    }
                }

                #endregion Animating barrels individually

                #region Turret shooting
                var shotTime = gun.GunBase.LastShootTime.Ticks;

                if (shotTime > lastShotTime) // just shot
                {
                    lastShotTime = shotTime;
                    localWorldMatrix = muzzleLocalMatrix * gun.GunBase.WorldMatrix;
                    //soundEmitter.SetPosition(localWorldMatrix.Translation);

                    if (useCustomMuzzle)
                    {
                        Dictionary<string, MyEntitySubpart> subparts = null;

                        if (gun is IMyLargeTurretBase)
                        {
                            var subpartYaw = block.GetSubpart("MissileTurretBase1");
                            var subpartPitch = subpartYaw.GetSubpart("MissileTurretBarrels");
                            subparts = subpartPitch.Subparts;
                        }
                        else if (gun is IMySmallMissileLauncherReload)
                        {
                            var blockInternal = (MyEntity) Entity;
                            subparts = blockInternal.Subparts;
                        }

                        // find the closest subpart to the muzzle position
                        var closestDistSq = double.MaxValue;
                        MyEntitySubpart closestSubpart = null;

                        if (subparts != null)
                            foreach (var kv in subparts)
                            {
                                if (!kv.Key.StartsWith("recoil"))
                                    continue;

                                var distSq = Vector3D.DistanceSquared(kv.Value.WorldMatrix.Translation,
                                    localWorldMatrix.Translation);

                                if (distSq < closestDistSq)
                                {
                                    closestDistSq = distSq;
                                    closestSubpart = kv.Value;
                                }
                            }

                        if (isAnimated)
                        {
                            var alreadyAnimated = false;

                            foreach (var data in animatingBarrels)
                            {
                                if (data.subpart == closestSubpart)
                                {
                                    alreadyAnimated = true;

                                    // make it recoil again
                                    data.restoring = false;
                                    break;
                                }
                            }

                            if (!alreadyAnimated)
                                animatingBarrels.Add(new AnimatedBarrel(closestSubpart));
                        }

                        //if custom recoil dummy is present, use it
                        if (closestSubpart != null)
                            localWorldMatrix = closestSubpart.WorldMatrix;
                    }

                    MyParticleEffect effect;

                    if (MyParticlesManager.TryCreateParticleEffect(ParticleType, ref localWorldMatrix, ref effectWorldPos, effectID, out effect))
                    {
                        effect.Loop = false;
                        effect.UserScale = ParticleScale;
                    }

                    //if(soundEmitter != null)
                    //{
                    //    soundEmitter.PlaySound(SoundPair, stopPrevious: false);
                    //}

                    muzzleLocalMatrix = gun.GunBase.GetMuzzleLocalMatrix(); // update to the next muzzle position
                }
                #endregion Turret shooting

            }
            catch(Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("[ Error in " + GetType().FullName + ": " + e.Message + " ]", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }
    }
}