namespace DefenseShields.API
{
    namespace DefenseShields
    {
        using System;
        using System.Collections.Generic;
        using Sandbox.ModAPI;
        using VRageMath;

        public class ApiServer
        {
            private const long Channel = 12345;
            private static readonly Dictionary<string, Delegate> _endpoints;

            static ApiServer()
            {
                _endpoints = new Dictionary<string, Delegate>
                {
                    {"IntersectShield1", (Action)Session.Instance.IntersectShield1},
                    {"IntersectShield2", (Func<Vector3D?>)Session.Instance.IntersectShield2},
                    {"IntersectShield3", (Action<float>)Session.Instance.IntersectShield3},
                    {"IntersectShield4", (Func<float, Vector3D?>)Session.Instance.IntersectShield4},
                };
            }

            /// <summary>
            /// Is the API ready to be serve
            /// </summary>
            public static bool IsReady { get; private set; }

            private static void HandleMessage(object o)
            {
                if ((o as string) == "ApiEndpointRequest")
                    MyAPIGateway.Utilities.SendModMessage(Channel, _endpoints);
            }

            private static bool _isRegistered;

            /// <summary>
            /// Prepares the client to receive API endpoints and requests an update.
            /// </summary>
            public static void Load()
            {
                if (!_isRegistered)
                {
                    _isRegistered = true;
                    MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);
                }
                IsReady = true;
                MyAPIGateway.Utilities.SendModMessage(Channel, _endpoints);
            }


            /// <summary>
            /// Unloads all API endpoints and detaches events.
            /// </summary>
            public static void Unload()
            {
                if (_isRegistered)
                {
                    _isRegistered = false;
                    MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);
                }
                IsReady = false;
                MyAPIGateway.Utilities.SendModMessage(Channel, new Dictionary<string, Delegate>());
            }
        }
    }
}
