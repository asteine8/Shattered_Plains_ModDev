using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Vicizlat.MultifloorElevator
{
    public class Communication
    {
        public static ushort NETWORK_ID = 8843;
        private static bool FloorRequestSent;

        public static void RegisterHandlers()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(NETWORK_ID, MessageHandler);
            Logging.Instance.WriteLine("Register Message Handler");
        }

        public static void UnregisterHandlers()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(NETWORK_ID, MessageHandler);
            Logging.Instance.WriteLine("Unregister Message Handler");
        }

        public static void RequestFloor(int selectedFloor, long elevatorId)
        {
            if (!FloorRequestSent)
            {
                Logging.Instance.WriteLine($"Sending to server: Request floor {selectedFloor}");
                byte[] data = new byte[sizeof(long) + 1];
                data[0] = (byte)selectedFloor;
                BitConverter.GetBytes(elevatorId).CopyTo(data, 1);
                MyAPIGateway.Utilities.InvokeOnGameThread(() => { MyAPIGateway.Multiplayer.SendMessageToServer(NETWORK_ID, data); });
                FloorRequestSent = true;
            }
        }

        private static void MessageHandler(byte[] data)
        {
            try
            {
                IMyEntity Elevator;
                if (!MyAPIGateway.Entities.TryGetEntityById(BitConverter.ToInt64(data, 1), out Elevator)) return;
                if (Elevator.GameLogic.GetAs<MultifloorElevator>() == null) return;
                Elevator.GameLogic.GetAs<MultifloorElevator>().SetTargetFloor(data[0]);
                FloorRequestSent = false;

                if (MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Utilities.IsDedicated)
                {
                    SendToClients(data, MyAPIGateway.Multiplayer.ServerId);
                }
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine($"Error during message handle! {e.Message}");
            }
        }

        public static void SendToClients(byte[] data, ulong sender)
        {
            List<IMyPlayer> PlayersList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(PlayersList);
            foreach (IMyPlayer player in PlayersList)
            {
                if (player.SteamUserId != MyAPIGateway.Multiplayer.MyId && player.SteamUserId != sender && !player.Character.IsDead)
                {
                    Logging.Instance.WriteLine($"Sending to client {player.SteamUserId}: Floor {data[0]}");
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => { MyAPIGateway.Multiplayer.SendMessageTo(NETWORK_ID, data, player.SteamUserId); });
                }
            }
            PlayersList.Clear();
        }
    }
}