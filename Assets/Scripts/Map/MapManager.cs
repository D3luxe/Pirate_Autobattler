using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using Pirate.MapGen; // Added for MapGraph, NodeType, ActSpec, Rules, MapGenerator, GenerationResult

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public event Action OnMapDataUpdated; // New event

    public int mapLength = 15;
    public Vector2Int nodesPerColumnRange = new Vector2Int(2, 4);

    private MapGraphData _mapGraphData; // Changed to MapGraphData
    private bool _isMapGenerated = false;
    private RunConfigSO _runConfig;
    private List<List<MapNodeData>> _convertedMapNodes;

    public MapGraphData GetMapGraphData() => _mapGraphData; // Changed return type
    public List<List<MapNodeData>> GetConvertedMapNodes() => _convertedMapNodes;

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

    public void GenerateMapIfNeeded(ulong seed)
    {
        if (_isMapGenerated) return;
        GenerateMapData(seed);
        _isMapGenerated = true;
        // PrecomputeReachabilityCache(); // Removed
    }

    private MapGraph _currentMapGraph; // Store the generated MapGraph

    void GenerateMapData(ulong seed)
    {
        ActSpec actSpec = new ActSpec
        {
            Rows = mapLength,
            Columns = nodesPerColumnRange.y,
            Branchiness = 0.5f
        };

        Rules rules = new Rules();

        MapGenerator mapGenerator = new MapGenerator();
        GenerationResult result = mapGenerator.GenerateMap(actSpec, rules, seed);

        if (result.Audits.IsValid)
        {
            _currentMapGraph = result.Graph;
            ConvertMapGraphToMapGraphData(result); // Pass result to get subSeeds
            Debug.Log("Map generated successfully!");
            OnMapDataUpdated?.Invoke(); // Invoke the event after map data is updated
        }
        else
        {
            Debug.LogError($"Map generation failed: {string.Join(", ", result.Audits.Violations)}");
            _currentMapGraph = null;
        }
    }

    private void ConvertMapGraphToMapGraphData(GenerationResult result)
    {
        _mapGraphData = new MapGraphData();
        _mapGraphData.rows = result.Graph.Nodes.Max(n => n.Row) + 1; // Assuming rows are 0-indexed
        _mapGraphData.nodes = new List<MapGraphData.Node>();
        _mapGraphData.edges = new List<MapGraphData.Edge>();

        // Populate MapGraphData.nodes
        foreach (var node in _currentMapGraph.Nodes)
        {
            _mapGraphData.nodes.Add(new MapGraphData.Node
            {
                id = node.Id,
                row = node.Row,
                col = node.Col,
                type = node.Type.ToString(), // Convert NodeType enum to string
                tags = node.Tags
            });
        }

        // Populate MapGraphData.edges
        foreach (var edge in _currentMapGraph.Edges)
        {
            _mapGraphData.edges.Add(new MapGraphData.Edge
            {
                fromId = edge.FromId,
                toId = edge.ToId
            });
        }

        // Populate subSeeds and constants
        _mapGraphData.subSeeds = new MapGraphData.SubSeeds
        {
            decorations = result.SubSeeds.Decorations // Assuming Decorations is available in SubSeeds
        };

        _mapGraphData.constants = new MapGraphData.Constants
        {
            rowHeight = 160f, // Placeholder, tune as needed
            laneWidth = 140f, // Placeholder, tune as needed
            mapPaddingX = 80f, // Placeholder, tune as needed
            mapPaddingY = 80f, // Placeholder, tune as needed
            minHorizontalSeparation = 70f, // Placeholder, tune as needed
            jitter = 18f // Placeholder, tune as needed
        };

        // Assign the generated map data to GameSession
        GameSession.CurrentRunState.mapGraphData = _mapGraphData;

        // Now, convert to List<List<MapNodeData>> for GameSession and MapPanel
        _convertedMapNodes = new List<List<MapNodeData>>();

        // Initialize columns
        for (int i = 0; i < _mapGraphData.rows; i++)
        {
            _convertedMapNodes.Add(new List<MapNodeData>());
        }

        foreach (var node in _currentMapGraph.Nodes)
        {
            MapNodeData mapNode = new MapNodeData
            {
                columnIndex = node.Row,
                rowIndex = node.Col,
                nodeType = (NodeType)Enum.Parse(typeof(NodeType), node.Type.ToString()), // Convert string to NodeType enum
                nextNodeIndices = new List<int>(),
                reachableNodeIndices = new List<int>()
            };

            // Determine EncounterSO based on NodeType
            EncounterSO encounter = null;
            switch (mapNode.nodeType)
            {
                case NodeType.Battle:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == EncounterType.Battle && !e.isElite);
                    break;
                case NodeType.Elite:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == EncounterType.Battle && e.isElite); // Placeholder for Elite
                    break;
                case NodeType.Boss:
                    encounter = GameDataRegistry.GetEncounter("enc_boss"); // Assuming a specific boss encounter
                    break;
                case NodeType.Shop:
                    encounter = GameDataRegistry.GetEncounter("enc_shop"); // Assuming a specific shop encounter
                    break;
                case NodeType.Event:
                    encounter = GameDataRegistry.GetEncounter("enc_event"); // Assuming a specific event encounter
                    break;
                case NodeType.Unknown:
                    encounter = GameDataRegistry.GetEncounter("enc_unknown"); // Assuming a specific unknown encounter
                    break;
                default:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == EncounterType.Battle && !e.isElite); // Default to Battle
                    break;
            }

            if (encounter != null)
            {
                mapNode.encounter = encounter;
                mapNode.iconPath = encounter.iconPath;
                mapNode.tooltipText = encounter.tooltipText;
                mapNode.isElite = encounter.isElite;
            }
            else
            {
                Debug.LogWarning($"Could not find a suitable encounter for node type {mapNode.nodeType}.");
            }

            _convertedMapNodes[node.Row].Add(mapNode);
        }

        // Now, populate nextNodeIndices for each node
        foreach (var edge in _currentMapGraph.Edges)
        {
            // Find the actual Node objects using FromId and ToId
            var fromNode = _currentMapGraph.Nodes.FirstOrDefault(n => n.Id == edge.FromId);
            var toNode = _currentMapGraph.Nodes.FirstOrDefault(n => n.Id == edge.ToId);

            if (fromNode != null && toNode != null)
            {
                // Find the source MapNodeData in _convertedMapNodes
                MapNodeData sourceMapNodeData = _convertedMapNodes[fromNode.Row].FirstOrDefault(n => n.rowIndex == fromNode.Col);
                if (sourceMapNodeData != null)
                {
                    // Add the rowIndex of the destination node to nextNodeIndices
                    sourceMapNodeData.nextNodeIndices.Add(toNode.Col);
                }
            }
        }
    }

    // Removed unused methods:
    // GetRandomEncounterType
    // EncounterProbability
    // PrecomputeReachabilityCache
    // CalculateReachableNodes
}