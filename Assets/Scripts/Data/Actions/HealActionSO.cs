using UnityEngine;
using PirateRoguelike.Combat;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "NewHealAction", menuName = "Pirate/Actions/Heal Action")]
    public class HealActionSO : ActionSO
    {
        public int healAmount;

        public override void Execute(CombatContext ctx)
        {
            if (ctx.Caster == null)
            {
                Debug.LogWarning("HealActionSO: Caster is null.");
                return;
            }

            // By default, a heal action targets the caster itself.
            ctx.Caster.Heal(healAmount);
            Debug.Log($"{ctx.Caster.Def.displayName} healed for {healAmount}.");
        }

        public override ActionType GetActionType()
        {
            return ActionType.Heal;
        }
    }
}
