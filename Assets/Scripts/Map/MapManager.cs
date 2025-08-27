using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public int mapLength = 15;
    public Vector2Int nodesPerColumnRange = new Vector2Int(2, 4);

    private List<List<MapNodeData>> _mapNodes = new List<List<MapNodeData>>();
    private bool _isMapGenerated = false;
    private RunConfigSO _runConfig;

    public List<List<MapNodeData>> GetMapNodes() => _mapNodes;

    public void SetMapNodes(List<List<MapNodeData>> nodes)
    {
        _mapNodes = nodes;
        _isMapGenerated = true;
    }

    void Start()
    {
        // Reward checking logic has been moved to RunManager.OnRunSceneLoaded()
        // for more reliable execution timing.
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _runConfig = GameDataRegistry.GetRunConfig();
        if (_runConfig == null)
        {
            Debug.LogError("RunConfigSO not found in GameDataRegistry!");
        }
    }

    public void GenerateMapIfNeeded()
    {
        if (_isMapGenerated) return;
        GenerateMapData();
        _isMapGenerated = true;
        PrecomputeReachabilityCache();
    }

    void GenerateMapData()
    {
        _mapNodes.Clear();

        // Define encounter probabilities (adjust as needed for balance)
        var encounterProbabilities = new List<EncounterProbability>
        {
            new EncounterProbability { type = EncounterType.Battle, weight = 60 },
            new EncounterProbability { type = EncounterType.Shop, weight = 10 },
            new EncounterProbability { type = EncounterType.Port, weight = 15 },
            new EncounterProbability { type = EncounterType.Event, weight = 15 }
        };

        // Pre-load all possible encounters
        Dictionary<EncounterType, List<EncounterSO>> availableEncounters = new Dictionary<EncounterType, List<EncounterSO>>();
        foreach (EncounterType type in System.Enum.GetValues(typeof(EncounterType)))
        {
            availableEncounters[type] = GameDataRegistry.GetAllEncounters().Where(e => e.type == type && !e.isElite).ToList();
        }
        List<EncounterSO> availableEliteEncounters = GameDataRegistry.GetAllEncounters().Where(e => e.isElite).ToList();

        EncounterSO bossEncounter = GameDataRegistry.GetEncounter("enc_boss");
        if (bossEncounter == null)
        {
            Debug.LogError("Boss encounter (enc_boss) not found in GameDataRegistry!");
            return;
        }

        int lastShopColumn = -_runConfig.shopEveryN; // Initialize to force shop early
        int lastPortColumn = -_runConfig.portEveryN; // Initialize to force port early
        int lastEliteColumn = -_runConfig.eliteEveryN; // Initialize to force elite early

        // Generate nodes data
        for (int i = 0; i < mapLength; i++)
        {
            int nodesInColumn = (i == 0 || i == mapLength - 1) ? 1 : Random.Range(nodesPerColumnRange.x, nodesPerColumnRange.y + 1);
            _mapNodes.Add(new List<MapNodeData>());

            bool forceShop = (i - lastShopColumn >= _runConfig.shopEveryN) && (i < mapLength - 1); // Don't force shop on boss column
            bool forcePort = (i - lastPortColumn >= _runConfig.portEveryN) && (i < mapLength - 1); // Don't force port on boss column
            bool forceElite = (i - lastEliteColumn >= _runConfig.eliteEveryN) && (i < mapLength - 2); // Don't force elite on boss column or column before boss

            for (int j = 0; j < nodesInColumn; j++)
            {
                EncounterType selectedType;
                bool isEliteNode = false;

                if (i == 0) // First node is always a battle (or a fixed start node type)
                {
                    selectedType = EncounterType.Battle;
                }
                else if (i == mapLength - 1) // Last node is always the boss
                {
                    selectedType = EncounterType.Boss;
                }
                else if (forceElite && availableEliteEncounters.Any())
                {
                    selectedType = EncounterType.Battle; // Elites are a type of battle
                    isEliteNode = true;
                    forceElite = false; // Only force one elite per column
                    lastEliteColumn = i;
                }
                else if (forceShop)
                {
                    selectedType = EncounterType.Shop;
                    forceShop = false; // Only force one shop per column
                    lastShopColumn = i;
                }
                else if (forcePort)
                {
                    selectedType = EncounterType.Port;
                    forcePort = false; // Only force one port per column
                    lastPortColumn = i;
                }
                else
                {
                    selectedType = GetRandomEncounterType(encounterProbabilities);
                }

                EncounterSO encounterSO = null;
                if (selectedType == EncounterType.Boss)
                {
                    encounterSO = bossEncounter;
                }
                else if (isEliteNode)
                {
                    encounterSO = availableEliteEncounters[Random.Range(0, availableEliteEncounters.Count)];
                }
                else if (availableEncounters.ContainsKey(selectedType) && availableEncounters[selectedType].Any())
                {
                    if (selectedType == EncounterType.Event)
                    {
                        // Filter events by min/max floor
                        List<EncounterSO> validEvents = availableEncounters[selectedType]
                            .Where(e => i >= e.minFloor && i <= e.maxFloor)
                            .ToList();
                        if (validEvents.Any())
                        {
                            encounterSO = validEvents[Random.Range(0, validEvents.Count)];
                        }
                        else
                        {
                            Debug.LogWarning($"No valid Event encounter found for floor {i}. Falling back to Battle.");
                            selectedType = EncounterType.Battle; // Fallback
                            encounterSO = availableEncounters[selectedType][Random.Range(0, availableEncounters[selectedType].Count)];
                        }
                    }
                    else
                    {
                        encounterSO = availableEncounters[selectedType][Random.Range(0, availableEncounters[selectedType].Count)];
                    }
                }

                if (encounterSO == null)
                {
                    Debug.LogWarning($"No {selectedType} encounter found. Falling back to Battle.");
                    encounterSO = availableEncounters[EncounterType.Battle].FirstOrDefault();
                    if (encounterSO == null)
                    {
                        Debug.LogError("No Battle encounter found. Cannot generate map.");
                        return;
                    }
                }

                _mapNodes[i].Add(new MapNodeData { encounter = encounterSO, columnIndex = i, rowIndex = j, isElite = isEliteNode, iconPath = encounterSO.iconPath, tooltipText = encounterSO.tooltipText });
            }
        }

        // Connect nodes data (Slay the Spire inspired)
        for (int i = 0; i < mapLength - 1; i++)
        {
            List<MapNodeData> currentColumn = _mapNodes[i];
            List<MapNodeData> nextColumn = _mapNodes[i + 1];

            // Ensure every node in the next column has at least one incoming connection
            foreach (MapNodeData nextNode in nextColumn)
            {
                bool isConnected = false;
                foreach (MapNodeData currentNode in currentColumn)
                {
                    if (currentNode.nextNodeIndices.Contains(nextColumn.IndexOf(nextNode)))
                    {
                        isConnected = true;
                        break;
                    }
                }

                if (!isConnected)
                {
                    // Connect from a random node in the current column
                    MapNodeData randomCurrentNode = currentColumn[Random.Range(0, currentColumn.Count)];
                    if (!randomCurrentNode.nextNodeIndices.Contains(nextColumn.IndexOf(nextNode)))
                    {
                        randomCurrentNode.nextNodeIndices.Add(nextColumn.IndexOf(nextNode));
                    }
                }
            }

            // Ensure every node in the current column has at least one outgoing connection
            foreach (MapNodeData currentNode in currentColumn)
            {
                if (!currentNode.nextNodeIndices.Any())
                {
                    // Connect to a random node in the next column
                    MapNodeData randomNextNode = nextColumn[Random.Range(0, nextColumn.Count)];
                    currentNode.nextNodeIndices.Add(nextColumn.IndexOf(randomNextNode));
                }

                // Add more random connections (branching)
                int additionalConnections = Random.Range(0, 2); // 0 or 1 additional connection
                for (int k = 0; k < additionalConnections; k++)
                {
                    MapNodeData randomCurrentNode = currentColumn[Random.Range(0, currentColumn.Count)];
                    int randomNextNodeIndex = Random.Range(0, nextColumn.Count);
                    if (!randomCurrentNode.nextNodeIndices.Contains(randomNextNodeIndex))
                    {
                        randomCurrentNode.nextNodeIndices.Add(randomNextNodeIndex);
                    }
                }
            }
        }
    }

    private EncounterType GetRandomEncounterType(List<EncounterProbability> probabilities)
    {
        int totalWeight = probabilities.Sum(p => p.weight);
        int randomNumber = Random.Range(0, totalWeight);

        foreach (var prob in probabilities)
        {
            if (randomNumber < prob.weight)
            {
                return prob.type;
            }
            randomNumber -= prob.weight;
        }
        return probabilities.Last().type; // Fallback
    }

    [System.Serializable]
    private class EncounterProbability
    {
        public EncounterType type;
        public int weight;
    }

    private void PrecomputeReachabilityCache()
    {
        foreach (var column in _mapNodes)
        {
            foreach (var node in column)
            {
                node.reachableNodeIndices = CalculateReachableNodes(node);
            }
        }
    }

    private List<int> CalculateReachableNodes(MapNodeData startNode)
    {
        HashSet<int> reachable = new HashSet<int>();
        Queue<MapNodeData> queue = new Queue<MapNodeData>();

        queue.Enqueue(startNode);
        reachable.Add(startNode.GetUniqueNodeId()); // Assuming a method to get a unique ID for the node

        while (queue.Any())
        {
            MapNodeData currentNode = queue.Dequeue();

            // Check if there's a next column
            if (currentNode.columnIndex + 1 < _mapNodes.Count)
            {
                List<MapNodeData> nextColumn = _mapNodes[currentNode.columnIndex + 1];
                foreach (int nextNodeIndex in currentNode.nextNodeIndices)
                {
                    if (nextNodeIndex >= 0 && nextNodeIndex < nextColumn.Count)
                    {
                        MapNodeData nextNode = nextColumn[nextNodeIndex];
                        if (reachable.Add(nextNode.GetUniqueNodeId()))
                        {
                            queue.Enqueue(nextNode);
                        }
                    }
                }
            }
        }
        return reachable.ToList();
    }
}
