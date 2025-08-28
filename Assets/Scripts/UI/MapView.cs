using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Pirate.MapGen;

public sealed class MapView : MonoBehaviour
{
    public VisualTreeAsset uxml;
    public StyleSheet uss;

    private VisualElement _root;

    // Rendering constants (could be loaded from JSON)
    public float rowHeight = 160, laneWidth = 140, jitter = 18, padX = 80, padY = 80;
    public float minSep = 70;

    ScrollView scroll;
    VisualElement canvas, edgeLayer, playerIndicator;

    Dictionary<string, MapGraphData.Node> nodeById = new();
    Dictionary<string, Vector2> pos = new();
    List<MapGraphData.Node> nodes = new();
    List<MapGraphData.Edge> edges = new();
    int rows = 0;

    System.Random decoRng;
    private Dictionary<string, VisualElement> nodeVisualElements = new Dictionary<string, VisualElement>();

        void Awake()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on MapView GameObject.");
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("Root VisualElement is null.");
            return;
        }

        _root.Clear();
        uxml.CloneTree(_root);
        _root.styleSheets.Add(uss);

        scroll = _root.Q<ScrollView>("MapScroll");
        if (scroll == null) Debug.LogError("ScrollView 'MapScroll' not found in UXML.");

        canvas = _root.Q<VisualElement>("MapCanvas");
        if (canvas == null) Debug.LogError("VisualElement 'MapCanvas' not found in UXML.");

        edgeLayer = _root.Q<VisualElement>("EdgeLayer");
        if (edgeLayer == null) Debug.LogError("VisualElement 'EdgeLayer' not found in UXML.");
        else edgeLayer.generateVisualContent += DrawEdges;

        // Subscribe to MapManager events
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnMapDataUpdated += HandleMapDataUpdated;
            // If map data is already generated, trigger update immediately
            if (MapManager.Instance.GetMapGraphData() != null)
            {
                HandleMapDataUpdated();
            }
        }
        else
        {
            Debug.LogError("MapManager Instance is null in MapView.Awake()! Cannot subscribe to map data updates.");
        }

        // Subscribe to GameSession events
        GameSession.OnPlayerNodeChanged += UpdateNodeVisualStates;

        // LayoutAndRender() and UpdateNodeVisualStates() are now called by HandleMapDataUpdated
    }

    void LayoutAndRender()
    {
        if (canvas == null) return; // Ensure canvas is not null before proceeding

        nodeById = nodes.ToDictionary(n => n.id, n => n);
        nodeVisualElements.Clear(); // Clear previous references

        // Remove old node VisualElements from canvas
        foreach (var ve in canvas.Children().ToList())
        {
            if (ve.name != "EdgeLayer" && ve.name != "PlayerIndicator") // Don't remove static elements
            {
                ve.RemoveFromHierarchy();
            }
        }

        // 1) Initial positions
        foreach (var n in nodes)
        {
            float x = padX + n.col * laneWidth + Jitter(decoRng, -jitter, jitter);
            float y = padY + (rows - 1 - n.row) * rowHeight;
            pos[n.id] = new Vector2(x, y);
        }

        // 2) Barycentric order per row (reduce crossings)
        for (int r = 1; r < rows; r++)
        {
            var rowNodes = nodes.Where(n => n.row == r).ToList();
            rowNodes.Sort((a, b) =>
            {
                float ax = AvgParentX(a), bx = AvgParentX(b);
                int cmp = ax.CompareTo(bx);
                return (cmp != 0) ? cmp : a.col.CompareTo(b.col); // Stable sort
            });

            // Repack to enforce min separation (left-to-right pass)
            float lastX = float.NegativeInfinity;
            foreach (var n in rowNodes)
            {
                var p = pos[n.id];
                if (p.x - lastX < minSep) p.x = lastX + minSep;
                pos[n.id] = p;
                lastX = p.x;
            }
        }

        // 3) Instantiate nodes
        foreach (var n in nodes)
        {
            var ve = new VisualElement();
            ve.name = n.id; // Assign name for easier debugging/lookup
            ve.AddToClassList("map-node");
            ve.AddToClassList($"type-{n.type.ToLower()}");
            foreach (var tag in n.tags)
            {
                ve.AddToClassList($"tag-{tag.ToLower()}");
            }

            // Add class for penultimate row accentuation
            if (rows > 1 && n.row == rows - 2)
            {
                ve.AddToClassList("penultimate-row-node");
            }

            // Add class for mid-act treasure QA overlay
            if (n.type == Pirate.MapGen.NodeType.Treasure.ToString().ToLower() && n.row >= rows / 3 && n.row <= (2 * rows / 3))
            {
                ve.AddToClassList("qa-treasure-window");
            }

            // Add tooltip for unknown nodes
            if (n.type == Pirate.MapGen.NodeType.Unknown.ToString().ToLower())
            {
                ve.tooltip = "Unknown Node - Tooltip Placeholder (Pity System Forecast Here)";
            }

            var p = pos[n.id];
            // Adjust position to center the node element around its calculated (x,y)
            ve.style.left = p.x - (56f / 2f);
            ve.style.top = p.y - (56f / 2f);

            canvas.Add(ve);
            nodeVisualElements[n.id] = ve; // Store reference to VisualElement
            ve.RegisterCallback<ClickEvent>(evt => OnNodeClicked(n.id));
            ve.RegisterCallback<PointerEnterEvent>(evt => OnNodePointerEnter(n));
            ve.RegisterCallback<PointerLeaveEvent>(evt => OnNodePointerLeave(n));
        }

        // Add player indicator to canvas after all nodes
        playerIndicator = canvas.Q<VisualElement>("PlayerIndicator");
        if (playerIndicator == null) Debug.LogError("VisualElement 'PlayerIndicator' not found in UXML.");
        else playerIndicator.style.display = DisplayStyle.None; // Ensure it's hidden initially

        // 4) Resize canvas
        // Calculate max Y to determine canvas height
        float maxY = nodes.Any() ? nodes.Max(n => pos[n.id].y) : 0;
        canvas.style.height = padY * 2 + maxY + 300; // Add extra padding for scrolling past the last node
        edgeLayer.MarkDirtyRepaint();
    }

    void OnDestroy()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnMapDataUpdated -= HandleMapDataUpdated;
        }
        GameSession.OnPlayerNodeChanged -= UpdateNodeVisualStates;
    }

    private void HandleMapDataUpdated()
    {
        MapGraphData mapGraphData = MapManager.Instance.GetMapGraphData();
        if (mapGraphData == null)
        {
            Debug.LogError("MapGraphData is null after update. Cannot render map.");
            return;
        }

        nodes = mapGraphData.nodes;
        edges = mapGraphData.edges;
        rows = mapGraphData.rows;

        // Use constants from MapGraphData if available, otherwise use defaults
        if (mapGraphData.constants != null)
        {
            rowHeight = mapGraphData.constants.rowHeight;
            laneWidth = mapGraphData.constants.laneWidth;
            padX = mapGraphData.constants.mapPaddingX;
            padY = mapGraphData.constants.mapPaddingY;
            minSep = mapGraphData.constants.minHorizontalSeparation;
            jitter = mapGraphData.constants.jitter;
        }

        if (mapGraphData.subSeeds != null)
        {
            decoRng = new System.Random(unchecked((int)mapGraphData.subSeeds.decorations));
        }
        else
        {
            Debug.LogWarning("subSeeds not found in MapGraphData. Using default random seed for decorations.");
            decoRng = new System.Random();
        }

        LayoutAndRender();
        UpdateNodeVisualStates();
    }

    

    float AvgParentX(MapGraphData.Node n)
    {
        var parentEdges = edges.Where(e => e.toId == n.id);
        if (!parentEdges.Any())
        {
            return pos[n.id].x; // If no parents, use its own x for sorting stability
        }

        var px = parentEdges
            .Select(e => pos[e.fromId].x)
            .Average();
        return (float)px;
    }

    static float Jitter(System.Random rng, float min, float max)
        => (float)(min + rng.NextDouble() * (max - min));

    void DrawEdges(MeshGenerationContext mgc)
    {
        var p2d = mgc.painter2D;
        p2d.lineWidth = 2f;
        p2d.strokeColor = Color.white; // Or a specific color for edges

        foreach (var e in edges)
        {
            // Get start and end node positions
            if (!pos.TryGetValue(e.fromId, out Vector2 fromPos) || !pos.TryGetValue(e.toId, out Vector2 toPos))
            {
                Debug.LogWarning($"Missing position for edge {e.fromId} -> {e.toId}");
                continue;
            }

            // Adjust start and end points to be at the bottom/top of the node visual element
            // Assuming node elements are 56x56 as per USS gist
            Vector2 a = fromPos + new Vector2(0, 28); // Bottom center of from node
            Vector2 b = toPos - new Vector2(0, 28);   // Top center of to node

            Vector2 c1 = a - new Vector2(0, 0.4f * rowHeight);
            Vector2 c2 = b + new Vector2(0, 0.4f * rowHeight);

            p2d.BeginPath();
            p2d.MoveTo(a);
            p2d.BezierCurveTo(c1, c2, b);
            p2d.Stroke();
        }
    }

    public void Show()
    {
        _root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _root.style.display = DisplayStyle.None;
    }

    public bool IsVisible()
    {
        return _root.style.display == DisplayStyle.Flex;
    }

    private void UpdateNodeVisualStates()
    {
        if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
        {
            return; // Not ready to update states
        }

        string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
        MapGraphData.Node currentPlayerNode = null;
        if (currentEncounterId != null && nodeById.ContainsKey(currentEncounterId))
        {
            currentPlayerNode = nodeById[currentEncounterId];
        }

        // Update player indicator position
        if (playerIndicator == null) Debug.LogError("PlayerIndicator is null!");

        if (currentPlayerNode != null)
        {
            Vector2 playerPos = pos[currentPlayerNode.id];
            playerIndicator.style.left = playerPos.x - (40f / 2f);
            playerIndicator.style.top = playerPos.y - (40f / 2f);
            playerIndicator.style.display = DisplayStyle.Flex; // Make sure it's visible
            playerIndicator.BringToFront(); // Ensure it's rendered on top
            Debug.Log($"PlayerIndicator: Visible at {playerPos.x}, {playerPos.y}");
        }
        else
        {
            playerIndicator.style.display = DisplayStyle.None; // Hide if no current node
            Debug.Log("PlayerIndicator: Hidden (no current node).");
        }

        foreach (var node in nodes)
        {
            VisualElement ve = nodeVisualElements[node.id];
            // Clear all state classes first
            ve.RemoveFromClassList("node-visited");
            ve.RemoveFromClassList("node-current");
            ve.RemoveFromClassList("node-available");
            ve.RemoveFromClassList("node-locked");

            if (node.id == currentEncounterId)
            {
                ve.AddToClassList("node-current");
            }
            else if (currentPlayerNode != null && node.row < currentPlayerNode.row)
            {
                // Simplistic: if node is in a row before current player, it's visited
                ve.AddToClassList("node-visited");
            }
            else if (currentPlayerNode != null && node.row == currentPlayerNode.row + 1)
            {
                // Check if there's a valid edge from current player node to this node
                bool isAvailable = GameSession.CurrentRunState.mapGraphData.edges.Any(e => e.fromId == currentPlayerNode.id && e.toId == node.id);
                if (isAvailable)
                {
                    ve.AddToClassList("node-available");
                }
                else
                {
                    ve.AddToClassList("node-locked");
                }
            }
            else
            {
                // All other nodes are locked (e.g., future rows beyond next, or same row but not current)
                ve.AddToClassList("node-locked");
            }

            // Boss preview active state
            if (node.type == Pirate.MapGen.NodeType.Boss.ToString().ToLower())
            {
                float bossNodeY = pos[node.id].y;
                float scrollY = scroll.scrollOffset.y;
                float previewThreshold = 200f; // Adjust this value as needed

                if (bossNodeY - scrollY < previewThreshold)
                {
                    ve.AddToClassList("boss-preview-active");
                }
                else
                {
                    ve.RemoveFromClassList("boss-preview-active");
                }
            }
        }
    }

    private void OnNodeClicked(string nodeId)
    {
        if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
        {
            Debug.LogWarning("GameSession or MapGraphData is not initialized.");
            return;
        }

        MapGraphData.Node clickedNode = nodeById[nodeId];
        MapGraphData.Node currentPlayerNode = null;

        // Find the current player node
        if (GameSession.CurrentRunState.currentEncounterId != null && nodeById.ContainsKey(GameSession.CurrentRunState.currentEncounterId))
        {
            currentPlayerNode = nodeById[GameSession.CurrentRunState.currentEncounterId];
        }
        else
        {
            // If currentEncounterId is not set (e.g., start of run), assume player is at row -1, allowing movement to row 0
            // This is a temporary handling for the very first move.
            // A more robust solution might involve a dedicated 'start' node or initial state.
            if (clickedNode.row == 0)
            {
                Debug.Log($"First move to node: {clickedNode.id}");
                GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
                GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;
                UpdateNodeVisualStates(); // Update states after first move
                // TODO: Trigger scene load or other game logic
                return;
            }
            else
            {
                Debug.LogWarning($"Cannot move to {clickedNode.id}. Player's current position is not defined for non-start node.");
                return;
            }
        }

        // 1. Validate Forward Movement (row r to row r+1)
        if (clickedNode.row != currentPlayerNode.row + 1)
        {
            Debug.Log($"Invalid move: Node {clickedNode.id} is not in the next row. Current row: {currentPlayerNode.row}, Clicked row: {clickedNode.row}");
            return;
        }

        // 2. Validate Valid Edge
        bool hasValidEdge = GameSession.CurrentRunState.mapGraphData.edges.Any(e => e.fromId == currentPlayerNode.id && e.toId == clickedNode.id);
        if (!hasValidEdge)
        {
            Debug.Log($"Invalid move: No direct path from {currentPlayerNode.id} to {clickedNode.id}.");
            return;
        }

        // If both validations pass, update game state
        Debug.Log($"Valid move! Moving from {currentPlayerNode.id} to {clickedNode.id}.");
        GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
        GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;

        GameSession.InvokeOnPlayerNodeChanged(); // Invoke event via public method

        // TODO: Trigger scene load or other game logic based on clickedNode.type
    }

    private void OnNodePointerEnter(MapGraphData.Node hoveredNode)
    {
        if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
        {
            return; // Not ready to highlight
        }

        string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
        MapGraphData.Node currentPlayerNode = null;
        if (currentEncounterId != null && nodeById.ContainsKey(currentEncounterId))
        {
            currentPlayerNode = nodeById[currentEncounterId];
        }

        if (currentPlayerNode == null) return; // No current player node to determine reachability from

        // Highlight only nodes in the next row that are reachable from the current player node
        foreach (var node in nodes)
        {
            VisualElement ve = nodeVisualElements[node.id];
            bool isReachable = (node.row == currentPlayerNode.row + 1) &&
                               GameSession.CurrentRunState.mapGraphData.edges.Any(e => e.fromId == currentPlayerNode.id && e.toId == node.id);

            if (isReachable)
            {
                ve.AddToClassList("node-hover-highlight");
            }
        }
    }

    private void OnNodePointerLeave(MapGraphData.Node hoveredNode)
    {
        // Remove highlight from all nodes
        foreach (var node in nodes)
        {
            VisualElement ve = nodeVisualElements[node.id];
            ve.RemoveFromClassList("node-hover-highlight");
        }
    }
}