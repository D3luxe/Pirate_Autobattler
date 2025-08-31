using PirateRoguelike.Data.Actions;
using PirateRoguelike.Combat; // For IRuntimeContext

namespace PirateRoguelike.Runtime
{
    public class RuntimeHealAction : RuntimeAction
    {
        public int CurrentHealAmount { get; set; }

        public RuntimeHealAction(HealActionSO baseActionSO) : base(baseActionSO)
        {
            CurrentHealAmount = baseActionSO.healAmount; // Initialize from SO
        }

        public override string BuildDescription(IRuntimeContext context)
        {
            // Example: "Heals for {CurrentHealAmount} health."
            return $"Heals for {CurrentHealAmount} health.";
        }
    }
}
