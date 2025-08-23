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
}
