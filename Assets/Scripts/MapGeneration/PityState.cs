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

        public void ResetPity(NodeType type)
        {
            if (PityAccumulated.ContainsKey(type))
            {
                PityAccumulated[type] = 0;
            }
        }

        public void IncrementOtherPities(NodeType chosenType)
        {
            foreach (var key in new List<NodeType>(PityAccumulated.Keys))
            {
                if (key != chosenType && key != NodeType.Event) // Event pity is incremented separately
                {
                    PityAccumulated[key]++;
                }
            }
        }

        public void IncrementAllPities()
        {
            foreach (var key in new List<NodeType>(PityAccumulated.Keys))
            {
                PityAccumulated[key]++;
            }
        }

        public void ResetAllPities()
        {
            foreach (var key in new List<NodeType>(PityAccumulated.Keys))
            {
                PityAccumulated[key] = 0;
            }
        }
    }
}
