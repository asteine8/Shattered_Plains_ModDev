using VRage.Game.ModAPI;
using VRageMath;

namespace Vicizlat.MultifloorElevator
{
    public partial class MultifloorElevator
    {
        private Color BLACK = Color.Black;
        private Color WHITE = Color.White;
        private Color ORANGE = Color.Orange;

        void SetDigit(IMyCubeBlock block, string prefix, int digit, Color color, float emissivity = 0.2f)
        {
            switch (digit)
            {
                case 0:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", color, emissivity);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 1:
                    block.SetEmissiveParts($"{prefix}1", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}4", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}5", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 2:
                    block.SetEmissiveParts($"{prefix}1", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}2", color, emissivity);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", BLACK, 0f);
                    break;
                case 3:
                    block.SetEmissiveParts($"{prefix}1", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 4:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 5:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 6:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", color, emissivity);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 7:
                    block.SetEmissiveParts($"{prefix}1", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}5", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 8:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", color, emissivity);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                case 9:
                    block.SetEmissiveParts($"{prefix}1", color, emissivity);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", color, emissivity);
                    block.SetEmissiveParts($"{prefix}4", color, emissivity);
                    block.SetEmissiveParts($"{prefix}5", color, emissivity);
                    block.SetEmissiveParts($"{prefix}6", color, emissivity);
                    block.SetEmissiveParts($"{prefix}7", color, emissivity);
                    break;
                default:
                    block.SetEmissiveParts($"{prefix}1", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}2", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}3", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}4", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}5", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}6", BLACK, 0f);
                    block.SetEmissiveParts($"{prefix}7", BLACK, 0f);
                    break;
            }
        }

        void SetDigitSubpart(IMyCubeBlock block, string prefix, int digit, Color color, float emissivity = 0.2f)
        {
            switch (digit)
            {
                case 0:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 1:
                    block.SetEmissivePartsForSubparts($"{prefix}1", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}4", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}5", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 2:
                    block.SetEmissivePartsForSubparts($"{prefix}1", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}2", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", BLACK, 0f);
                    break;
                case 3:
                    block.SetEmissivePartsForSubparts($"{prefix}1", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 4:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 5:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 6:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 7:
                    block.SetEmissivePartsForSubparts($"{prefix}1", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}5", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 8:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                case 9:
                    block.SetEmissivePartsForSubparts($"{prefix}1", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}4", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}5", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}6", color, emissivity);
                    block.SetEmissivePartsForSubparts($"{prefix}7", color, emissivity);
                    break;
                default:
                    block.SetEmissivePartsForSubparts($"{prefix}1", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}2", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}3", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}4", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}5", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}6", BLACK, 0f);
                    block.SetEmissivePartsForSubparts($"{prefix}7", BLACK, 0f);
                    break;
            }
        }
    }
}