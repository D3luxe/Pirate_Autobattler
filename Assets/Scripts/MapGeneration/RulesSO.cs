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

        public Spacing Spacing;
        public Windows Windows;
        public UnknownWeights UnknownWeights;
        public RuleFlags Flags;

        public RulesSO()
        {
            Spacing = new Spacing();
            Windows = new Windows();
            UnknownWeights = new UnknownWeights();
            Flags = new RuleFlags();
        }
    }

    public enum RowBand
    {
        Default,
        Early,
        Mid,
        Late,
        EliteUnlock, // For rows where elites just unlock
        PreBoss // For rows just before the boss row
    }

    [Serializable]
    public class RowBandOdds
    {
        public RowBand Band;
        public int MinRow;
        public int MaxRow;
        public SerializableDictionary<NodeType, int> Odds;

        public RowBandOdds()
        {
            Odds = new SerializableDictionary<NodeType, int>();
        }
    }

    [Serializable]
    public class Spacing
    {
        [Tooltip("Minimum number of rows between Elite nodes.")]
        public int EliteMinGap = 3;
        [Tooltip("Minimum number of rows between Shop nodes.")]
        public int ShopMinGap = 2;
        [Tooltip("Minimum number of rows between Port nodes.")]
        public int PortMinGap = 2;
        [Tooltip("Elites cannot appear before this row index.")]
        public int EliteEarlyRowsCap = 5;

        [Tooltip("Maximum number of re-roll attempts for a node type before falling back to a default.")]
        public int MaxRerollAttempts = 5;

        [Tooltip("Default node type to fall back to if re-rolls fail.")]
        public NodeType FallbackNodeType = NodeType.Battle;

        [Tooltip("Weighted odds for each node type during generation, defined per row band.")]
        public List<RowBandOdds> RowBandGenerationOdds;

        public Spacing()
        {
            RowBandGenerationOdds = new List<RowBandOdds>();
        }
    }

    [Serializable]
    public class Windows
    {
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

        [Header("Pity System Settings")]
        public float BattlePityBase = 0.10f;
        public float BattlePityIncrement = 0.10f;
        public float TreasurePityBase = 0.02f;
        public float TreasurePityIncrement = 0.02f;
        public float ShopPityBase = 0.03f;
        public float ShopPityIncrement = 0.03f;
        [Tooltip("If no other type procs, fallback to Event.")]
        public bool FallbackToEvent = true;
        [Tooltip("If true, pity counts are reset per act.")]
        public bool PityPerAct = true;

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

        [Header("Structural Bans")]
        [Tooltip("If true, Port nodes are banned on the row immediately before the pre-boss Port row.")]
        public bool BanPortOnPrePreBossRow = true;

        [Header("Adjacency Rules")]
        [Tooltip("If true, prevents consecutive Elite nodes along a path.")]
        public bool NoEliteToElite = true;
        [Tooltip("If true, prevents consecutive Shop nodes along a path.")]
        public bool NoShopToShop = true;
        [Tooltip("If true, prevents consecutive Port nodes along a path.")]
        public bool NoPortToPort = true;
        [Tooltip("If true, children of a split must be different types, unless the next row is uniform by design.")]
        public bool ChildrenMustBeDifferentTypes = true;

        [Header("Unknown Node Resolution Rules")]
        [Tooltip("If true, Unknown nodes resolve independently of adjacency rules.")]
        public bool IgnoreAdjacencyRulesOnResolve = true;
        [Tooltip("If true, Unknown nodes still respect structural bans on resolve.")]
        public bool RespectStructuralBansOnResolve = true;
    }
}
