using System;
using System.Collections.Generic;

[Serializable]
public class MapNodeData
{
    public EncounterSO encounter;
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
