using System;
using System.Collections.Generic;
using PirateRoguelike.Data;

namespace Pirate.MapGen
{
    [Serializable]
    public class PityState
    {
        public Dictionary<NodeType, int> PityAccumulated { get; set; } = new Dictionary<NodeType, int>
        {
            { NodeType.Event, 0 },
            { NodeType.Battle, 0 },
            { NodeType.Shop, 0 },
            { NodeType.Treasure, 0 }
        };
    }
}
