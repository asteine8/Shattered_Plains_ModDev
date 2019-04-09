using System;
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
using VRageMath;

namespace Digi.CameraPanning
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), useEntityUpdate: false)]
    public class CameraBlock : MyGameLogicComponent
    {
        private bool controlling = false;
        private bool recenter = false;
        private Matrix originalMatrix;
        private Matrix rotatedMatrix;
        private Vector3 positionOffset;
        private float currentPitch = 0;
        private float currentYaw = 0;
        private float currentRoll = 0;
        private float currentSpeed = 0;
        private float prevFOV = 0;
        private byte soundRotateStopDelay = 0;
        private byte soundZoomStopDelay = 0;
        private MyEntity3DSoundEmitter soundRotateEmitter = null;
        private MyEntity3DSoundEmitter soundZoomEmitter = null;

        private static IMyHudNotification notification = null;
        private static int notificationTimeoutTicks = 0;

        private const float SPEED_MUL = 0.1f; // rotation input multiplier as it is too fast raw compared to the rest of the game
        private const float MAX_SPEED = 1.8f; // using a max speed to feel like it's on actual servos
        private const byte SOUND_ROTATE_STOP_DELAY = 10; // game ticks
        private const byte SOUND_ZOOM_STOP_DELAY = 30; // game ticks
        private const float SOUND_ROTATE_VOLUME = 0.5f;
        private const float SOUND_ZOOM_VOLUME = 0.2f;
        private static readonly MySoundPair SOUND_ROTATE_PAIR = new MySoundPair("BlockRotor"); // sound pair used for camera rotation, without the 'Arc' or 'Real' prefix.
        private static readonly MySoundPair SOUND_ZOOM_PAIR = new MySoundPair("WepShipGatlingRotation"); // sound pair used for camera zooming, without the 'Arc' or 'Real' prefix.
        private const float EPSILON = 0.0001f;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                var block = (MyCubeBlock)Entity;

                if(block.CubeGrid.IsPreview || block.CubeGrid.Physics == null) // ignore ghost grids
                    return;

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                originalMatrix = Entity.LocalMatrix;
                rotatedMatrix = originalMatrix;

                // HACK temporary fix for camera being in the center of the block
                TryFixCameraPosition(); // moves the camera view's position towards the default mount point by gridSize/2.

                if(soundRotateEmitter == null)
                {
                    soundRotateEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);
                    soundRotateEmitter.CustomVolume = SOUND_ROTATE_VOLUME;
                }

                if(soundZoomEmitter == null)
                {
                    soundZoomEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity);
                    soundZoomEmitter.CustomVolume = SOUND_ZOOM_VOLUME;
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public override void Close()
        {
            try
            {
                if(soundRotateEmitter != null)
                    soundRotateEmitter.StopSound(true, true);

                if(soundZoomEmitter != null)
                    soundZoomEmitter.StopSound(true, true);
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                bool rotating = Update(); // returns true if the camera is rotating, false otherwise

                if(soundRotateEmitter != null)
                {
                    if(rotating && soundRotateStopDelay > 0) // reset the stop delay
                        soundRotateStopDelay = 0;

                    if(soundRotateEmitter.IsPlaying) // dynamically adjust sound volume depending on last rotation speed
                        soundRotateEmitter.CustomVolume = 0.2f + (MathHelper.Clamp(currentSpeed / MAX_SPEED, 0, 1) * 0.6f);

                    if(!rotating && soundRotateEmitter != null && soundRotateEmitter.IsPlaying)
                    {
                        if(soundRotateStopDelay == 0)
                            soundRotateStopDelay = SOUND_ZOOM_STOP_DELAY;

                        if(--soundRotateStopDelay == 0)
                            soundRotateEmitter.StopSound(true);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        private bool Update()
        {
            if(MyAPIGateway.Session.CameraController != Entity)
            {
                if(controlling) // if it was being controlled, restore the local matrix, stop sounds, etc
                {
                    controlling = false;
                    notificationTimeoutTicks = 150; // (int)((notification.AliveTime / 1000f) * 60f)

                    //Entity.Render.Visible = true; // restore camera model
                    Entity.SetLocalMatrix(originalMatrix); // reset the camera's matrix to avoid seeing its model skewed if the model gets updated with the local matrix
                    Entity.Render.UpdateRenderObject(true); // force model to be recalculated to avoid invisible models on merge/unmerge while camera is viewed

                    if(soundZoomEmitter != null)
                        soundZoomEmitter.StopSound(true);
                }

                return false; // not controlled, no need to update further
            }

            if(notificationTimeoutTicks > 0)
                notificationTimeoutTicks--;

            var lookaroundControl = MyAPIGateway.Input.GetGameControl(MyControlsSpace.LOOKAROUND);
            var rotationTypeControl = MyAPIGateway.Input.GetGameControl(MyControlsSpace.SPRINT);
            var cameraModeControl = MyAPIGateway.Input.GetGameControl(MyControlsSpace.CAMERA_MODE);
            var FOV = MyAPIGateway.Session.Camera.FovWithZoom;

            if(!controlling) // just taken control of this camera
            {
                controlling = true; // only show this message once per camera control

                //Entity.Render.Visible = false; // hide the camera model to avoid weirdness

                originalMatrix = Entity.LocalMatrix; // recalculate original matrix and rotated matrix in case the block was "moved" (by merge or who knows what else)
                RotateCamera(0, 0, 0, true);

                Entity.SetLocalMatrix(rotatedMatrix); // restore the last view matrix
                prevFOV = FOV;

                if(notification == null)
                    notification = MyAPIGateway.Utilities.CreateNotification("");

                notification.AliveTime = 2500;
                notification.Text = "Hold " + GetControlAssignedName(lookaroundControl) + " to pan camera, " + GetControlAssignedName(lookaroundControl) + "+" + GetControlAssignedName(rotationTypeControl) + " to change rotation type and " + GetControlAssignedName(cameraModeControl) + " to reset.";
                notification.Show();
            }
            else
            {
                if(Math.Abs(FOV - prevFOV) > 0.05f)
                {
                    prevFOV = FOV;

                    if(soundZoomEmitter != null && !soundZoomEmitter.IsPlaying)
                    {
                        soundZoomEmitter.PlaySound(SOUND_ZOOM_PAIR, stopPrevious: true, skipIntro: true, alwaysHearOnRealistic: true, force2D: true);
                        soundZoomStopDelay = SOUND_ZOOM_STOP_DELAY;
                    }

                    if(notification != null)
                    {
                        notification.AliveTime = 300;
                        notification.Text = Math.Round(MathHelper.ToDegrees(FOV), 0) + "°";
                        notification.Show();
                    }
                }
                else
                {
                    if(soundZoomEmitter != null && soundZoomEmitter.IsPlaying)
                    {
                        if(soundZoomStopDelay == 0 || --soundZoomStopDelay == 0)
                            soundZoomEmitter.StopSound(true);
                    }
                }
            }

            if(!MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible)
            {
                if(!recenter && cameraModeControl.IsNewPressed())
                {
                    recenter = true;
                    return false; // no reason to compute further
                }

                if(lookaroundControl.IsPressed())
                {
                    if(notificationTimeoutTicks > 0)
                    {
                        notificationTimeoutTicks = 0;
                        notification.Hide();
                    }

                    var rot = MyAPIGateway.Input.GetRotation();
                    bool rollToggle = rotationTypeControl.IsPressed();

                    if(Math.Abs(rot.X) > EPSILON || Math.Abs(rot.Y) > EPSILON)
                    {
                        if(recenter)
                            recenter = false;

                        if(rollToggle)
                        {
                            // slowly reset yaw while giving control to roll
                            if(RotateCamera(rot.X * SPEED_MUL, MathHelper.Lerp(currentYaw, 0f, 0.1f), rot.Y * SPEED_MUL))
                                return true;
                        }
                        else
                        {
                            if(RotateCamera(rot.X * SPEED_MUL, rot.Y * SPEED_MUL, 0))
                                return true;
                        }
                    }
                    else if(rollToggle && Math.Abs(currentYaw) > EPSILON) // not moving mouse but holding modifier should result in yaw being slowly reset to 0
                    {
                        if(RotateCamera(0, MathHelper.Lerp(currentYaw, 0f, 0.1f), 0))
                            return true;
                    }
                }
            }

            if(recenter)
            {
                if(Math.Abs(currentPitch) > EPSILON || Math.Abs(currentYaw) > EPSILON || Math.Abs(currentRoll) > EPSILON)
                {
                    if(RotateCamera(MathHelper.Lerp(currentPitch, 0f, 0.1f), MathHelper.Lerp(currentYaw, 0f, 0.1f), MathHelper.Lerp(currentRoll, 0f, 0.1f)))
                        return true;
                }
                else
                {
                    recenter = false;

                    // ensure it's perfectly centered
                    currentPitch = 0;
                    currentYaw = 0;
                    currentRoll = 0;
                    Entity.SetLocalMatrix(rotatedMatrix);
                    return true;
                }
            }

            return false;
        }

        private float ClampAngle(float value, float limit = 0)
        {
            if(limit > 0)
                value = MathHelper.Clamp(value, -limit, limit);

            if(value > 180)
                value = -180 + (value - 180);
            else if(value < -180)
                value = 180 - (value - 180);

            return value;
        }

        private float ClampMaxSpeed(float value)
        {
            return MathHelper.Clamp(value, -MAX_SPEED, MAX_SPEED);
        }

        private bool RotateCamera(float pitchMod, float yawMod, float rollMod, bool forceRecalculate = false)
        {
            var camera = (IMyCameraBlock)Entity;
            float angleLimit = camera.RaycastConeLimit;

            pitchMod = ClampMaxSpeed(pitchMod);
            yawMod = ClampMaxSpeed(yawMod);
            rollMod = ClampMaxSpeed(rollMod);

            var setPitch = ClampAngle(currentPitch - pitchMod, angleLimit);
            var setYaw = ClampAngle(currentYaw - yawMod, angleLimit);
            var setRoll = ClampAngle(currentRoll - rollMod);

            if(forceRecalculate || Math.Abs(setPitch - currentPitch) >= EPSILON || Math.Abs(setYaw - currentYaw) >= EPSILON || Math.Abs(setRoll - currentRoll) >= EPSILON)
            {
                currentSpeed = new Vector3(pitchMod, yawMod, rollMod).Length();
                currentPitch = setPitch;
                currentYaw = setYaw;
                currentRoll = setRoll;

                rotatedMatrix = MatrixD.CreateFromYawPitchRoll(0, 0, MathHelper.ToRadians(currentRoll)) * originalMatrix;
                rotatedMatrix = MatrixD.CreateFromYawPitchRoll(MathHelper.ToRadians(currentYaw), MathHelper.ToRadians(currentPitch), 0) * rotatedMatrix;
                rotatedMatrix.Translation = originalMatrix.Translation + positionOffset;
                Entity.SetLocalMatrix(rotatedMatrix);

                if(soundRotateEmitter != null && !soundRotateEmitter.IsPlaying)
                    soundRotateEmitter.PlaySound(SOUND_ROTATE_PAIR, stopPrevious: true, skipIntro: true, alwaysHearOnRealistic: true, force2D: true);

                return true;
            }

            return false;
        }

        private MyCubeBlockDefinition.MountPoint GetDefaultMountPoint(MyCubeBlock block)
        {
            var mountPoints = block.BlockDefinition.MountPoints;

            for(int i = 0; i < mountPoints.Length; i++)
            {
                var mount = mountPoints[i];

                if(mount.Enabled && mount.Default)
                    return mount;
            }

            if(mountPoints.Length > 0) // if none have the Default property set, the first one is assumed the default one
                return mountPoints[0];

            return default(MyCubeBlockDefinition.MountPoint);
        }

        private void TryFixCameraPosition()
        {
            var block = (MyCubeBlock)Entity;
            var mount = GetDefaultMountPoint(block);

            if(block.BlockDefinition.ModelOffset.LengthSquared() <= EPSILON) // ignore mods that have model offset because that also moves camera position
            {
                positionOffset = Vector3.TransformNormal((Vector3)mount.Normal, originalMatrix) * ((block.CubeGrid.GridSize / 2f) - 0.05f);
                rotatedMatrix.Translation = originalMatrix.Translation + positionOffset;
            }
        }

        private static string GetControlAssignedName(IMyControl control)
        {
            var assign = control.GetControlButtonName(MyGuiInputDeviceEnum.Mouse);

            if(!string.IsNullOrWhiteSpace(assign))
                return assign;

            assign = control.GetControlButtonName(MyGuiInputDeviceEnum.Keyboard);

            if(!string.IsNullOrWhiteSpace(assign))
                return assign;

            assign = control.GetControlButtonName(MyGuiInputDeviceEnum.KeyboardSecond);

            if(!string.IsNullOrWhiteSpace(assign))
                return assign;

            return "(unassigned)";
        }
    }
}
