using System;
using System.Collections.Generic;
using UnityEngine; // Added for ScriptableObject
using PirateRoguelike.Data; // Added for EncounterType

namespace Pirate.MapGen
{
    [CreateAssetMenu(fileName = "NewRules", menuName = "Data/Rules")]
    public class RulesSO : ScriptableObject
    {
        public int testValue; // For testing serialization

        public Counts Counts;
        public Spacing Spacing;
        public Windows Windows;
        public UnknownWeights UnknownWeights;
        public RuleFlags Flags;

        public RulesSO()
        {
            Counts = new Counts();
            Spacing = new Spacing();
            Windows = new Windows();
            UnknownWeights = new UnknownWeights();
            Flags = new RuleFlags();
        }
    }

    [Serializable]
    public class Counts
    {
        public SerializableDictionary<NodeType, int> Min;
        public SerializableDictionary<NodeType, int> Max;
        public SerializableDictionary<NodeType, int> Targets;

        public Counts()
        {
            Min = new SerializableDictionary<NodeType, int>();
            Max = new SerializableDictionary<NodeType, int>();
            Targets = new SerializableDictionary<NodeType, int>();
        }
    }

    [Serializable]
    public class Spacing
    {
        public int EliteMinGap;
        public int ShopMinGap;
        public int PortMinGap;
        public int EliteEarlyRowsCap;
    }

    [Serializable]
    public class Windows
    {
        public string PreBossPortRow;
        public List<int> MidTreasureRows;

        public Windows()
        {
            MidTreasureRows = new List<int>();
        }
    }

    [Serializable]
    public class UnknownWeights
    {
        public SerializableDictionary<NodeType, int> Start;
        public SerializableDictionary<NodeType, int> Pity;
        public SerializableDictionary<NodeType, int> Caps;

        public UnknownWeights()
        {
            Start = new SerializableDictionary<NodeType, int>();
            Pity = new SerializableDictionary<NodeType, int>();
            Caps = new SerializableDictionary<NodeType, int>();
        }
    }

    [Serializable]
    public class RuleFlags
    {
        public bool EnableMetaKeys;
        public bool EnableBurningElites;
    }
}
