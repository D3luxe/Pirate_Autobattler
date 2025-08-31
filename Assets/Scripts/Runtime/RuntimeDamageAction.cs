using PirateRoguelike.Data.Actions;
using PirateRoguelike.Combat; // For IRuntimeContext

namespace PirateRoguelike.Runtime
{
    public class RuntimeDamageAction : RuntimeAction
    {
        public int CurrentDamageAmount { get; set; }

        public RuntimeDamageAction(DamageActionSO baseActionSO) : base(baseActionSO)
        {
            CurrentDamageAmount = baseActionSO.damageAmount; // Initialize from SO
        }

        public override string BuildDescription(IRuntimeContext context)
        {
            // Example: "Deals {CurrentDamageAmount} damage."
            return $"Deals {CurrentDamageAmount} damage.";
        }
    }
}
