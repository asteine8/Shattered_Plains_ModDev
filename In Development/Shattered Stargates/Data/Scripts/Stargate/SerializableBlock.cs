using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Phoenix.Stargate
{
    public abstract class SerializableBlock : MyGameLogicComponent
    {
        protected bool m_hasBeenDeserialized = false;

        public abstract void SerializeData();

        public virtual void DeserializeData()
        {
            m_hasBeenDeserialized = true;
        }
    }
}
