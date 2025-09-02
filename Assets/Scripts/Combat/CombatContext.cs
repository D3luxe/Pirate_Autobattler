using PirateRoguelike.Core;

namespace PirateRoguelike.Combat
{
    /// <summary>
    /// Holds the context for a single combat event or action.
    /// </summary>
    public class CombatContext
    {
        public ShipState Caster { get; set; }
        public ShipState Target { get; set; }
        
        // Optional data that may be relevant to specific triggers
        public float DamageAmount { get; set; }
        public float HealAmount { get; set; }
    }
}
