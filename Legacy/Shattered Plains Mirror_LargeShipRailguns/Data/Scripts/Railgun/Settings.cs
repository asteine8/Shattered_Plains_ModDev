using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rexxar.Communication;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Rexxar
{
    public class Settings
    {
        public static readonly Guid EntityGuid = new Guid("bd77f000-6f37-40c4-a637-4d86a6439c20");

        [ProtoContract]
        public struct RailgunSettings
        {
            [ProtoMember]
            public bool Recharging;
        }

        private static Dictionary<long, RailgunSettings> _readCache = new Dictionary<long, RailgunSettings>();
        private static Dictionary<long, RailgunSettings> _writeCache = new Dictionary<long, RailgunSettings>();
        public static RailgunSettings GetSettings(IMyEntity entity)
        {
            RailgunSettings set;
            if (_readCache.TryGetValue(entity.EntityId, out set))
                return set;
            
            string s;
            if (entity.Storage != null && entity.Storage.TryGetValue(EntityGuid, out s))
            {
                try
                {
                    var d = Convert.FromBase64String(s);
                    set = MyAPIGateway.Utilities.SerializeFromBinary<RailgunSettings>(d);
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine("RailgunSettings failed to deserialize");
                    throw;
                }
                _readCache[entity.EntityId] = set;
                return set;
            }

            set = new RailgunSettings();
            set.Recharging = true;
            _readCache[entity.EntityId] = set;
            return set;
        }

        public static void SetSettings(IMyEntity entity, RailgunSettings settings)
        {
            _writeCache[entity.EntityId] = settings;
            _readCache[entity.EntityId] = settings;
        }

        public static void ConsumeSync(IEnumerable<KeyValuePair<long, RailgunSettings>> settingsCollection)
        {
            foreach (var kvp in settingsCollection)
            {
                _writeCache[kvp.Key] = kvp.Value;
                _readCache[kvp.Key] = kvp.Value;
            }
        }

        public static void SyncSettings()
        {
            Communication.Communication.SendMessageToServer(new SettingsMessage(_writeCache));
        }

        public static void CommitSettings()
        {
            if (!_writeCache.Any())
                return;

            foreach (var set in _writeCache)
            //MyAPIGateway.Parallel.ForEach(_writeCache, set =>
            {
                IMyEntity e;
                if (!MyAPIGateway.Entities.TryGetEntityById(set.Key, out e))
                    return;

                var d = MyAPIGateway.Utilities.SerializeToBinary(set.Value);
                var s = Convert.ToBase64String(d);
                if(e.Storage == null)
                    e.Storage = new MyModStorageComponent();
                e.Storage[EntityGuid] = s;
            }
            //);

            _writeCache.Clear();
        }

        public static void Unload()
        {
            _writeCache = null;
            _readCache = null;
        }
    }
}
