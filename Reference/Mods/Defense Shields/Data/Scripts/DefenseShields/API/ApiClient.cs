namespace DefenseShields
{
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    public class ApiClient
    {
        private const long Channel = 12345;

        /// <summary>
        /// Is the API ready to be used
        /// </summary>
        public static bool IsReady { get; private set; }

        private static void HandleMessage(object o)
        {
            var dict = o as IReadOnlyDictionary<string, Delegate>;
            if (dict == null)
                return;

            Delegate entry;
            IsReady = true;
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
            if (!IsReady)
                MyAPIGateway.Utilities.SendModMessage(Channel, "ApiEndpointRequest");
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
        }

    }
}
