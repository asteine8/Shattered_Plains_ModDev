using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using VRage.Game.Components;

namespace DefenseShields
{
    public class ModulatorGridComponent : MyEntityComponentBase
    {
        private static List<ModulatorGridComponent> gridModulator = new List<ModulatorGridComponent>();
        public Modulators Modulator;
        public string Password;

        public ModulatorGridComponent(Modulators modulator)
        {
            Modulator = modulator;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            if (Container.Entity.InScene)
            {
                gridModulator.Add(this);
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {

            if (Container.Entity.InScene)
            {
                gridModulator.Remove(this);
            }

            base.OnBeforeRemovedFromContainer();
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            gridModulator.Add(this);
        }

        public override void OnRemovedFromScene()
        {
            gridModulator.Remove(this);

            base.OnRemovedFromScene();
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public HashSet<MyCubeGrid> SubGrids { get; set; } = new HashSet<MyCubeGrid>();

        public string ModulationPassword
        {
            get { return Password; }
            set { Password = value; }
        }

        public override string ComponentTypeDebugString
        {
            get { return "Shield"; }
        }
    }
}
