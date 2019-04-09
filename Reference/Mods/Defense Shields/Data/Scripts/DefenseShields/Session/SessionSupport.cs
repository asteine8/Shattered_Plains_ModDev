
namespace DefenseShields
{
    using Support;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using System;
    using VRage.Game.Entity;
    using VRage.Game;
    using Sandbox.Game.Entities;

    public partial class Session
    {
        public string ModPath()
        {
            var modPath = ModContext.ModPath;
            return modPath;
        }

        public MyEntity3DSoundEmitter AudioReady(MyEntity entity)
        {
            if (Tick - SoundTick < 600 && Tick > 600) return null;
            SoundTick = Tick;

            SoundEmitter.StopSound(false);
            SoundEmitter.Entity = entity;
            SoundEmitter.CustomVolume = MyAPIGateway.Session.Config.GameVolume * 0.75f;
            return SoundEmitter;
        }

        private void PlayerConnected(long id)
        {
            try
            {
                if (Players.ContainsKey(id))
                {
                    if (Enforced.Debug >= 3) Log.Line($"Player id({id}) already exists");
                    return;
                }
                MyAPIGateway.Multiplayer.Players.GetPlayers(null, myPlayer => FindPlayer(myPlayer, id));
            }
            catch (Exception ex) { Log.Line($"Exception in PlayerConnected: {ex}"); }
        }

        private void PlayerDisconnected(long l)
        {
            try
            {
                IMyPlayer removedPlayer;
                Players.TryRemove(l, out removedPlayer);
                PlayerEventId++;
                if (Enforced.Debug >= 3) Log.Line($"Removed player, new playerCount:{Players.Count}");
            }
            catch (Exception ex) { Log.Line($"Exception in PlayerDisconnected: {ex}"); }
        }

        private bool FindPlayer(IMyPlayer player, long id)
        {
            if (player.IdentityId == id)
            {
                Players[id] = player;
                PlayerEventId++;
                if (Enforced.Debug >= 3) Log.Line($"Added player: {player.DisplayName}, new playerCount:{Players.Count}");
            }
            return false;
        }

        #region Events
        private void OnEntityRemove(MyEntity myEntity)
        {
            var warhead = myEntity as IMyWarhead;
            if (warhead != null)
            {
                if (!warhead.IsFunctional && (warhead.IsArmed || (warhead.DetonationTime <= 0 && warhead.IsCountingDown)) && warhead.CustomData.Length != 0)
                {
                    var blastRatio = warhead.CubeGrid.GridSizeEnum == MyCubeSize.Small ? 1 : 5;
                    var epicCenter = warhead.PositionComp.WorldAABB.Center;
                    if (Enforced.Debug >= 2 && EmpStore.Count == 0) Log.Line($"====================================================================== [WarHead EventStart]");
                    EmpStore.Enqueue(new WarHeadBlast(blastRatio, epicCenter, warhead.CustomData));
                }
            }
        }
        #endregion
    }
}
