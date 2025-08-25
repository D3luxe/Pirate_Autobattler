using UnityEngine;
using PirateRoguelike.Data.Actions;

namespace PirateRoguelike.Data.Effects
{
    [CreateAssetMenu(fileName = "NewEffect", menuName = "Pirate/Effects/Effect")]
    public class EffectSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Behavior")]
        public float duration; // in seconds
        public float tickInterval; // in seconds, 0 for no tick
        public ActionSO tickAction; // Action to perform on each tick

        [Header("Stacking")]
        public bool isStackable;
        public int maxStacks;

        public string Id => id;
        public string DisplayName => displayName;
        public float Duration => duration;
        public float TickInterval => tickInterval;
        public ActionSO TickAction => tickAction;
        public bool IsStackable => isStackable;
        public int MaxStacks => maxStacks;
    }
}
