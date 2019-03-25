using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace Phoenix.Stargate
{
    public enum GateType : int
    {
        Invalid,
        Stargate,
        Supergate,
        SuperSupergate,
        Microgate,
    }

    public enum GateEdition : byte
    {
        None = 0,
        /// <summary>
        /// Universe Gate
        /// </summary>
        First = 1,

        /// <summary>
        /// Milky Way Gate
        /// </summary>
        Second,

        /// <summary>
        /// Pegasus Gate
        /// </summary>
        Third,
    }

    public struct EntityExpire
    {
        public IMyEntity Entity;
        public DateTime Expires;
    }

    [Flags]
    public enum Chevron : ushort
    {
        None = 0,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Four = 1 << 3,
        Five = 1 << 4,
        Six = 1 << 5,
        Seven = 1 << 6,
        Eight = 1 << 7,
        Nine = 1 << 8,
    }

    public enum GateState
    {
        Idle = 0,
        Dialing,
        Active,
        Incoming,
    }

    [Serializable]
    public struct MyTuple
    {
        public static Tuple2<T1, T2, T3> Create<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            return new Tuple2<T1, T2, T3>(arg1, arg2, arg3);
        }
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 4)]
    [Serializable]
    public struct Tuple2<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public Tuple2(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }
}
