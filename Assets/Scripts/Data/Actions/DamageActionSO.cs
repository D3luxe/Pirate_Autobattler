using UnityEngine;
using PirateRoguelike.Combat;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "NewDamageAction", menuName = "Pirate/Actions/Damage Action")]
    public class DamageActionSO : ActionSO
    {
        public int damageAmount;

        public override void Execute(CombatContext ctx)
        {
            if (ctx.Target == null)
            {
                Debug.LogWarning("DamageActionSO: Target is null.");
                return;
            }

            // By default, a damage action targets the opponent.
            ctx.Target.TakeDamage(damageAmount);
            Debug.Log($"{ctx.Caster.Def.displayName} dealt {damageAmount} damage to {ctx.Target.Def.displayName}.");
        }

        public override ActionType GetActionType()
        {
            return ActionType.Damage;
        }
    }
}
