using System;
using System.Collections.Generic;
using PirateRoguelike.Data;

namespace PirateRoguelike.Combat
{
    [Serializable]
    public class Ability
    {
        public TriggerType triggerType;
        public List<AbilityAction> actions; // To be defined
    }

    [Serializable]
    public class AbilityAction
    {
        public ActionType actionType;
        public List<RarityTieredValue> values; // Generic value for the action (e.g., damage amount, heal amount)
        public List<RarityTieredValue> durations; // Duration of the effect in seconds
        public List<RarityTieredValue> tickIntervals; // How often the effect ticks (0 for instant/non-ticking)
        public List<RarityTieredValue> stacks; // Initial stacks for stackable effects
        public StatType statType; // For stat-changing effects
        // Add more fields as needed for specific action types (e.g., buff duration, debuff type)
    }
}