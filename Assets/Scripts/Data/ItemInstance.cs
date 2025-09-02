using UnityEngine;
using PirateRoguelike.Runtime; // Added for RuntimeItem
using PirateRoguelike.Saving; // Added for SerializableItemInstance

namespace PirateRoguelike.Data
{
    /// <summary>
    /// Represents a runtime instance of an item, referencing its ItemSO definition.
    /// </summary>
    [System.Serializable]
    public class ItemInstance
    {
        public ItemSO Def; // Reference to the ItemSO definition
        public RuntimeItem RuntimeItem { get; private set; }
        public float CooldownRemaining { get; set; } // New: For tracking cooldowns
        public float StunDuration { get; set; } // New: For tracking stun duration

        public ItemInstance(ItemSO def)
        {
            Def = def;
            RuntimeItem = new RuntimeItem(def);
            CooldownRemaining = 0; // Initialize cooldown
            StunDuration = 0; // Initialize stun duration
        }

        public SerializableItemInstance ToSerializable()
        {
            return new SerializableItemInstance(Def.id, Def.rarity, CooldownRemaining, StunDuration);
        }

        // Add any runtime mutable properties here, e.g., current durability, charges, etc.
        // For now, it just holds a reference to the ItemSO.
    }
}
