using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace Digi.CameraPanning
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CameraPanningMod : MySessionComponentBase
    {
        public override void LoadData()
        {
            instance = this;
            Log.SetUp("Camera Panning", WORKSHOPID, "CameraPanning");
        }

        public static CameraPanningMod instance = null;
        private bool init = false;
        private bool thirdPersonEnabled = false;
        private float originalCameraFovSmall = 0;
        private float originalCameraFovLarge = 0;

        private const ulong WORKSHOPID = 806331071;
        public const float CAMERA_FOV = (float)(100 / 180d * Math.PI); // 100 degrees in radians
        public readonly MyDefinitionId CAMERA_SMALL_ID = new MyDefinitionId(typeof(MyObjectBuilder_CameraBlock), "SmallCameraBlock");
        public readonly MyDefinitionId CAMERA_LARGE_ID = new MyDefinitionId(typeof(MyObjectBuilder_CameraBlock), "LargeCameraBlock");

        public override void UpdateAfterSimulation()
        {
            if(init)
                return;

            try
            {
                if(MyAPIGateway.Session == null)
                    return;

                init = true;
                Log.Init();

                thirdPersonEnabled = MyAPIGateway.Session.SessionSettings.Enable3rdPersonView;

                var def = GetCameraDefinition(CAMERA_SMALL_ID);

                if(def != null)
                {
                    originalCameraFovSmall = def.MaxFov;
                    def.MaxFov = CAMERA_FOV;
                }

                def = GetCameraDefinition(CAMERA_LARGE_ID);

                if(def != null)
                {
                    originalCameraFovLarge = def.MaxFov;
                    def.MaxFov = CAMERA_FOV;
                }

                // SetUpdateOrder() throws an exception if called in the update method; this to overcomes that
                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        public override void HandleInput()
        {
            try
            {
                if(!init || thirdPersonEnabled || MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible)
                    return;

                if(MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.CAMERA_MODE))
                {
                    var camCtrl = MyAPIGateway.Session.CameraController;
                    var controller = MyAPIGateway.Session.ControlledObject as Sandbox.Game.Entities.IMyControllableEntity; // avoiding ambiguity

                    if(camCtrl == null || controller == null)
                        return;

                    if(controller is IMyShipController)
                    {
                        // HACK this is how MyCockpit.Rotate() does things so I kinda have to use these magic numbers.
                        var num = MyAPIGateway.Input.GetMouseSensitivity() * 0.13f;
                        camCtrl.Rotate(new Vector2(controller.HeadLocalXAngle / num, controller.HeadLocalYAngle / num), 0);
                    }
                    else
                    {
                        // HACK this is how MyCharacter.RotateHead() does things so I kinda have to use these magic numbers.
                        camCtrl.Rotate(new Vector2(controller.HeadLocalXAngle * 2, controller.HeadLocalYAngle * 2), 0);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }

        protected override void UnloadData()
        {
            try
            {
                // restore original FOV for camera definitions as they are not reloaded in between world loads which means removing the mod will not reset the FOV.
                var def = GetCameraDefinition(CAMERA_SMALL_ID);

                if(def != null)
                    def.MaxFov = originalCameraFovSmall;

                def = GetCameraDefinition(CAMERA_LARGE_ID);

                if(def != null)
                    def.MaxFov = originalCameraFovSmall;
            }
            catch(Exception e)
            {
                Log.Error(e);
            }

            instance = null;
            Log.Close();
        }

        private MyCameraBlockDefinition GetCameraDefinition(MyDefinitionId defId)
        {
            MyCubeBlockDefinition def;

            if(MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out def))
                return def as MyCameraBlockDefinition;

            return null;
        }
    }
}
