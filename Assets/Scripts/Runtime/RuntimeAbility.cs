using System.Collections.Generic;
using PirateRoguelike.Data.Abilities;
using PirateRoguelike.Data.Actions;

namespace PirateRoguelike.Runtime
{
    public class RuntimeAbility
    {
        protected readonly AbilitySO BaseAbilitySO;
        public IReadOnlyList<RuntimeAction> Actions { get; private set; }

        public RuntimeAbility(AbilitySO baseAbilitySO)
        {
            BaseAbilitySO = baseAbilitySO;
            var runtimeActions = new List<RuntimeAction>();
            foreach (var actionSO in baseAbilitySO.actions)
            {
                // This will require a factory or switch statement to create the correct RuntimeAction type
                if (actionSO is DamageActionSO damageActionSO)
                {
                    runtimeActions.Add(new RuntimeDamageAction(damageActionSO));
                }
                else if (actionSO is HealActionSO healActionSO)
                {
                    runtimeActions.Add(new RuntimeHealAction(healActionSO));
                }
                else if (actionSO is ApplyEffectActionSO applyEffectActionSO)
                {
                    runtimeActions.Add(new RuntimeApplyEffectAction(applyEffectActionSO));
                }
                // Add more cases for other ActionSO types as they are created
            }
            Actions = runtimeActions;
        }

        public string DisplayName => BaseAbilitySO.displayName;
    }
}
