using System;
using System.Collections.Generic;
using Pirate.MapGen;

namespace PirateRoguelike.Saving
{
    // A plain C# object for serializing the state of a run to JSON.
    [Serializable]
    public class RunState
    {
        public int playerLives;
        public int gold;
        public int currentColumnIndex = -1; // -1 means before the first column
        public int currentEncounterNode;
        public string currentEncounterId;
        public SerializableShipState playerShipState;
        public SerializableShipState enemyShipState; // For saving battle state
        public List<SerializableItemInstance> inventoryItems;
        public MapGraphData mapGraphData; // Full map data
        public ulong randomSeed;
        public SubSeeds subSeeds; // Persist sub-seeds for deterministic replays
        public PityState pityState; // Persist pity state for Unknown node resolution
        public int rerollsThisShop; // Save reroll count for current shop
        public List<RunModifier> activeRunModifiers;
        public List<SerializableItemInstance> battleRewards; // Items offered as battle rewards
        public UnknownContext unknownContext; // Context for unknown node resolution

        public RunState()
        {
            pityState = new PityState();
            unknownContext = new UnknownContext();
        }
    }
}