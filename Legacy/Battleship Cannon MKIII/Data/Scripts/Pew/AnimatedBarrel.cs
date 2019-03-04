using VRage.Game.Entity;
using VRageMath;

namespace MWI
{
    internal class AnimatedBarrel
    {
        public Vector3 initialTranslation;
        public bool restoring = false;
        public MyEntitySubpart subpart;
        public float travel = 0f;

        public AnimatedBarrel(MyEntitySubpart subpart)
        {
            this.subpart = subpart;
            initialTranslation = subpart.PositionComp.LocalMatrix.Translation;
        }
    }

}