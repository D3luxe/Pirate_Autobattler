using System;
using System.Collections.Generic;
using PirateRoguelike.Data;

namespace Pirate.MapGen
{
    [Serializable]
    public class UnknownContext
    {
        // Modifiers from relics/flags/player state (multiplicative weight scalars)
        // For now, let's assume a simple dictionary of EncounterType to float for modifiers.
        public Dictionary<NodeType, float> Modifiers { get; set; } = new Dictionary<NodeType, float>
        {
            { NodeType.Event, 1.0f },
            { NodeType.Battle, 1.0f },
            { NodeType.Shop, 1.0f },
            { NodeType.Treasure, 1.0f }
        };
    }
}
