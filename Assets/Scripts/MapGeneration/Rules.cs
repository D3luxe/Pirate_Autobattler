using System;
using System.Collections.Generic;
using PirateRoguelike.Data; // Added for EncounterType

namespace Pirate.MapGen
{
    [Serializable]
    public class Rules
    {
        public Counts Counts { get; set; } = new Counts();
        public Spacing Spacing { get; set; } = new Spacing();
        public Windows Windows { get; set; } = new Windows();
        public UnknownWeights UnknownWeights { get; set; } = new UnknownWeights();
        public RuleFlags Flags { get; set; } = new RuleFlags();
    }

    [Serializable]
    public class Counts
    {
        public Dictionary<NodeType, int> Min { get; set; } = new Dictionary<NodeType, int>();
        public Dictionary<NodeType, int> Max { get; set; } = new Dictionary<NodeType, int>();
        public Dictionary<NodeType, int> Targets { get; set; } = new Dictionary<NodeType, int>();
    }

    [Serializable]
    public class Spacing
    {
        public int EliteMinGap { get; set; }
        public int ShopMinGap { get; set; }
        public int PortMinGap { get; set; }
        public int EliteEarlyRowsCap { get; set; }
    }

    [Serializable]
    public class Windows
    {
        public string PreBossPortRow { get; set; }
        public List<int> MidTreasureRows { get; set; } = new List<int>();
    }

    [Serializable]
    public class UnknownWeights
    {
        public Dictionary<NodeType, int> Start { get; set; } = new Dictionary<NodeType, int>();
        public Dictionary<NodeType, int> Pity { get; set; } = new Dictionary<NodeType, int>();
        public Dictionary<NodeType, int> Caps { get; set; } = new Dictionary<NodeType, int>();
    }

    [Serializable]
    public class RuleFlags
    {
        public bool EnableMetaKeys { get; set; }
        public bool EnableBurningElites { get; set; }
    }
}
