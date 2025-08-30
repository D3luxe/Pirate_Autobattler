using System;
using System.Collections.Generic;
using Pirate.MapGen; // Added for NodeType
using PirateRoguelike.Data; // Added for EncounterSO

[Serializable]
public class MapNodeData
{
    public NodeType nodeType; // The type of map node (Battle, Elite, Unknown, etc.)
    public EncounterSO encounter; // The specific encounter data for this node (resolved from nodeType)
    public List<int> nextNodeIndices = new List<int>();
    public int columnIndex;
    public int rowIndex;
    public bool isElite; // Is this an elite node?
    public string iconPath; // Path to the icon sprite for this node
    public string tooltipText; // Text for the tooltip when hovering over this node
    public List<int> reachableNodeIndices; // Indices of all nodes reachable from this node

    // Assuming a max of 10 nodes per column for unique ID generation
    public int GetUniqueNodeId()
    {
        return columnIndex * 10 + rowIndex;
    }
}
