﻿namespace DefenseShields
{
    using System;
    using Support;
    using Sandbox.ModAPI;
    using VRageMath;
    using System.ComponentModel;

    public partial class Session
    {
        [DefaultValue("API:IntersectShield1")]
        public void IntersectShield1()
        {
        }

        [DefaultValue("API:IntersectShield2")]
        public Vector3D? IntersectShield2()
        {
            return Vector3D.Zero;
        }

        [DefaultValue("API:IntersectShield3")]
        public void IntersectShield3(float damage)
        {
        }

        [DefaultValue("API:IntersectShield4")]
        public Vector3D? IntersectShield4(float damage)
        {
            return Vector3D.Zero;
        }
        #region Network sync
        internal void RequestEnforcement(ulong requestorId)
        {
            try
            {
                Enforced.SenderId = requestorId;
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataEnforce(0, Enforced));
                MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_ID, bytes, true);
            }
            catch (Exception ex) { Log.Line($"Exception in PacketizeEnforcementToServer: {ex}"); }
        }

        internal void ClaimDisplay(ulong requestorId, long playerId, uint mId, long displayId, bool abandon)
        {
            try
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(new DataDisplayClaim(displayId, new DisplayClaimValues { PlayerId = playerId, Abandon = abandon}));
                MyAPIGateway.Multiplayer.SendMessageToServer(PACKET_ID, bytes, true);
            }
            catch (Exception ex) { Log.Line($"Exception in ClaimDisplayToServer: {ex}"); }
        }

        internal void PacketizeToClientsInRange(IMyFunctionalBlock block, PacketBase packet)
        {
            try
            {
                var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);
                var localSteamId = MyAPIGateway.Multiplayer.MyId;
                foreach (var p in Players.Values)
                {
                    var id = p.SteamUserId;
                    if (id != localSteamId && id != packet.SenderId && Vector3D.DistanceSquared(p.GetPosition(), block.PositionComp.WorldAABB.Center) <= SyncBufferedDistSqr)
                        MyAPIGateway.Multiplayer.SendMessageTo(PACKET_ID, bytes, p.SteamUserId);
                }
            }
            catch (Exception ex) { Log.Line($"Exception in PacketizeToClientsInRange: {ex}"); }
        }

        private void ReceivedPacket(byte[] rawData)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);
                if (packet.Received(IsServer) && packet.Entity != null)
                {
                    var localSteamId = MyAPIGateway.Multiplayer.MyId;
                    foreach (var p in Players.Values)
                    {
                        var id = p.SteamUserId;
                        if (id != localSteamId && id != packet.SenderId && Vector3D.DistanceSquared(p.GetPosition(), packet.Entity.PositionComp.WorldAABB.Center) <= SyncBufferedDistSqr)
                            MyAPIGateway.Multiplayer.SendMessageTo(PACKET_ID, rawData, p.SteamUserId);
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in ReceivedPacket: {ex}"); }
        }
        #endregion
    }
}
