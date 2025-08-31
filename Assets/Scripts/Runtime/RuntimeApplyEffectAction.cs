using PirateRoguelike.Data.Actions;
using PirateRoguelike.Combat; // For IRuntimeContext
using PirateRoguelike.Data.Effects; // For EffectSO

namespace PirateRoguelike.Runtime
{
    public class RuntimeApplyEffectAction : RuntimeAction
    {
        public EffectSO EffectToApply { get; private set; } // Reference to the base EffectSO

        public RuntimeApplyEffectAction(ApplyEffectActionSO baseActionSO) : base(baseActionSO)
        {
            EffectToApply = baseActionSO.effectToApply; // Initialize from SO
        }

        public override string BuildDescription(IRuntimeContext context)
        {
            // Example: "Applies {EffectToApply.DisplayName}."
            // If EffectSO had dynamic properties, they would be accessed here.
            return $"Applies {EffectToApply.DisplayName}.";
        }
    }
}
