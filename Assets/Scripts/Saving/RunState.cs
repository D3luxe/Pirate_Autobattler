using System;
using System.Collections.Generic;

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
    public List<List<MapNodeData>> mapNodes; // Full map data
    public uint randomSeed;
    public int rerollsThisShop; // Save reroll count for current shop
    public List<RunModifier> activeRunModifiers;
    public List<SerializableItemInstance> battleRewards; // Items offered as battle rewards
}