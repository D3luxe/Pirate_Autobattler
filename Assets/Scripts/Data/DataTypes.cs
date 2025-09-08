using System;
using System.Collections.Generic;
using PirateRoguelike.Combat;
using PirateRoguelike.Saving;

namespace PirateRoguelike.Data
{
    public enum Rarity { Bronze, Silver, Gold, Diamond }

    [Serializable]
    public class RarityMilestone
    {
        public int floor;
        public List<RarityWeight> weights;
    }

    [Serializable]
    public class RarityWeight
    {
        public Rarity rarity;
        public int weight;
    }

    public enum EncounterType { Battle, Elite, Shop, Port, Event, Treasure, Boss, Unknown }

    public enum TriggerType { OnItemReady, OnAllyActivate, OnBattleStart, OnEncounterEnd, OnDamageDealt, OnDamageReceived, OnHeal, OnShieldGained, OnDebuffApplied, OnBuffApplied, OnTick }
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
        public string outcomeText; // Text describing the outcome of the choice
        public List<EventChoiceAction> actions;
    }

    public enum TargetingStrategy { Player, LowestHealth, HighestDamage }

    
}
