using UnityEngine;
using PirateRoguelike.Combat;

namespace PirateRoguelike.Data.Actions
{
    /// <summary>
    /// Base class for all actions that can be executed by an ability.
    /// </summary>
    public abstract class ActionSO : ScriptableObject
    {
        [TextArea]
        [SerializeField] private string description;

        /// <summary>
        /// Executes the action's logic.
        /// </summary>
        /// <param name="ctx">The context of the current combat tick.</param>
        public abstract void Execute(CombatContext ctx);

        /// <summary>
        /// Returns the ActionType of this action.
        /// </summary>
        public abstract ActionType GetActionType();
    }
}
