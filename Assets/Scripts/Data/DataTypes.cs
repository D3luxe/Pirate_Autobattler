using System;
using System.Collections.Generic;

namespace PirateRoguelike.Data
{
    public enum Rarity { Bronze, Silver, Gold, Diamond }

    [Serializable]
    public class RarityProbability
    {
        public Rarity rarity;
        public int weight;
    }

    public enum EncounterType { Battle, Shop, Port, Event, Boss }

    public enum TriggerType { OnItemReady, OnAllyActivate, OnBattleStart, OnDamageDealt, OnDamageReceived, OnHeal, OnShieldGained, OnDebuffApplied, OnBuffApplied, OnTick }
    public enum ActionType { Buff, Damage, Heal, Shield, Debuff, StatChange, Meta, Burn, Poison, Stun }

    public enum StatType { Attack, Defense }

    public enum StatModifierType { Flat, Percentage }

    [Serializable]
    public class RarityTieredValue
    {
        public Rarity rarity;
        public float value;
    }

    // Used to reference an ItemSO without a direct link, allowing for JSON serialization
    [Serializable]
    public class ItemRef
    {
        public string id;
        // public int level; // For future use if items can be pre-leveled
    }

    // Base class for any kind of tag
    [Serializable]
    public class Tag
    {
        public string id;
        public string displayName;
    }

    // Example of a complex passive effect definition
    [Serializable]
    public class ShipPassive
    {
        public string description;
        // TODO: Implement a proper condition/effect system
    }

    [Serializable]
    public class SynergyRule
    {
        public string description;
        // TODO: Implement a rule engine for synergies
    }

    [Serializable]
    public class EventChoice
    {
        public string choiceText;
        public int goldCost; // Cost to make this choice
        public int lifeCost; // Life cost to make this choice
        public string itemRewardId; // ID of item to give as reward
        public string shipRewardId; // ID of ship to give as reward
        public string nextEncounterId; // For branching events, ID of next encounter
        public string outcomeText; // Text describing the outcome of the choice
    }

    public enum TargetingStrategy { Player, LowestHealth, HighestDamage }

    [Serializable]
    public class SerializableShipState
    {
        public string shipId;
        public int currentHealth;
        public float currentShield;
        public float stunDuration;
        public List<SerializableItemInstance> equippedItems;
        public List<ActiveCombatEffect> activeEffects;
        public List<StatModifier> activeStatModifiers;

        public SerializableShipState(string shipId, int currentHealth, float currentShield, float stunDuration, List<SerializableItemInstance> equippedItems, List<ActiveCombatEffect> activeEffects, List<StatModifier> activeStatModifiers)
        {
            this.shipId = shipId;
            this.currentHealth = currentHealth;
            this.currentShield = currentShield;
            this.stunDuration = stunDuration;
            this.equippedItems = equippedItems;
            this.activeEffects = activeEffects;
            this.activeStatModifiers = activeStatModifiers;
        }
    }
}
