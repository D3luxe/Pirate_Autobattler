using System;
using UnityEngine;
using PirateRoguelike.Data; // Added for Rarity

namespace PirateRoguelike.Saving
{
    [Serializable]
    public class SerializableItemInstance
    {
        public string itemId;
        public Rarity rarity; // Added
        public float cooldownRemaining;
        public float stunDuration;

        public SerializableItemInstance(string id, Rarity itemRarity, float cooldown, float stun)
        {
            itemId = id;
            rarity = itemRarity;
            cooldownRemaining = cooldown;
            stunDuration = stun;
        }
    }
}