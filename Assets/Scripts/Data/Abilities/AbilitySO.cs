using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data.Actions;

namespace PirateRoguelike.Data.Abilities
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Pirate/Abilities/Ability")]
    public class AbilitySO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Behavior")]
        public TriggerType trigger;
        public List<ActionSO> actions;

        public TriggerType Trigger => trigger;
        public IReadOnlyList<ActionSO> Actions => actions;
    }
}
