using UnityEngine;
using PirateRoguelike.Data.Effects;
using PirateRoguelike.Combat;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "NewApplyEffectAction", menuName = "Pirate/Actions/Apply Effect Action")]
    public class ApplyEffectActionSO : ActionSO
    {
        public EffectSO effectToApply;

        public override void Execute(CombatContext ctx)
        {
            if (effectToApply == null)
            {
                Debug.LogWarning("ApplyEffectActionSO: effectToApply is null.");
                return;
            }

            if (ctx.Target == null)
            {
                Debug.LogWarning("ApplyEffectActionSO: Target is null.");
                return;
            }

            // The logic for applying the effect will be on the ShipState class.
            // We just need to pass the EffectSO data to it.
            ctx.Target.ApplyEffect(effectToApply);
            Debug.Log($"{ctx.Caster.Def.displayName} applied effect '{effectToApply.DisplayName}' to {ctx.Target.Def.displayName}.");
        }

        public override ActionType GetActionType()
        {
            return ActionType.Meta; // This action applies an effect, which then might have its own action type.
        }
    }
}
