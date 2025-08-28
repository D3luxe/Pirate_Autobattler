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

    [Header("Visual Compaction Settings")]
    public float contentWidth = 1000f;
    public float gutters = 50f;
    public float maxRowSpan = 800f;
    public float rowHeight = 160f;
    public float padY = 80f;
    public float minSep = 70f;
    public float jitterAmp = 10f;

    [Header("Edge Rendering Settings")]
    public float edgeWidth = 4f;
    public float dashLength = 10f;
    public float gapLength = 5f;
    public float curvePadding = 20f;

    private const float nodeHalfHeight = 28f; // From map-node height in USS
    private const float DragThreshold = 5f;

    ScrollView scroll;
    VisualElement canvas, edgeLayer, playerIndicator;

    Dictionary<string, MapGraphData.Node> nodeById = new();
    private Dictionary<string, Vector2> visualNodePositions = new Dictionary<string, Vector2>();
    List<MapGraphData.Node> nodes = new();
    List<MapGraphData.Edge> edges = new();
    int rows = 0;

    System.Random decoRng;
    private Dictionary<string, VisualElement> nodeVisualElements = new Dictionary<string, VisualElement>();
    
    private Vector2 _startMousePosition;
    private Vector2 _startScrollOffset;
    private bool _isDragging = false;

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

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnMapDataUpdated += HandleMapDataUpdated;
            if (MapManager.Instance.GetMapGraphData() != null)
            {
                HandleMapDataUpdated();
            }
        }
        else
        {
            Debug.LogError("MapManager Instance is null in MapView.Awake()! Cannot subscribe to map data updates.");
        }

        GameSession.OnPlayerNodeChanged += UpdateNodeVisualStates;
        
        scroll.RegisterCallback<PointerDownEvent>(OnPointerDown);
        scroll.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        scroll.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void LayoutAndRender()
    {
        if (canvas == null) return;

        nodeById = nodes.ToDictionary(n => n.id, n => n);
        visualNodePositions.Clear();
        nodeVisualElements.Clear();

        foreach (var ve in canvas.Children().ToList())
        {
            if (ve.name != "EdgeLayer" && ve.name != "PlayerIndicator")
            {
                ve.RemoveFromHierarchy();
            }
        }

        CalculateVisualLayout();
        RenderNodesAndEdges();
    }

    void CalculateVisualLayout()
    {
        var nodesByRow = new Dictionary<int, List<MapGraphData.Node>>();
        foreach (var node in nodes)
        {
            if (!nodesByRow.ContainsKey(node.row))
            {
                nodesByRow[node.row] = new List<MapGraphData.Node>();
            }
            nodesByRow[node.row].Add(node);
        }

        for (int r = 0; r < rows; r++)
        {
            if (!nodesByRow.ContainsKey(r)) continue;

            var rowNodes = nodesByRow[r];

            if (r > 0)
            {
                rowNodes.Sort((a, b) =>
                {
                    float ax = AvgParentX(a), bx = AvgParentX(b);
                    int cmp = ax.CompareTo(bx);
                    return (cmp != 0) ? cmp : a.col.CompareTo(b.col);
                });
            }
            else
            {
                rowNodes.Sort((a, b) => a.col.CompareTo(b.col));
            }

            int k = rowNodes.Count;
            float span = Mathf.Min(maxRowSpan, (k - 1) * minSep);
            float spacing = (k > 1) ? (span / (k - 1)) : 0;
            float left = gutters + (contentWidth - span) / 2f;

            float lastX = float.NegativeInfinity;
            for (int j = 0; j < k; j++)
            {
                var node = rowNodes[j];
                float visualX = left + (j * spacing);
                visualX += Jitter(decoRng, -jitterAmp, jitterAmp);

                if (j > 0 && visualX - lastX < minSep)
                {
                    visualX = lastX + minSep;
                }
                lastX = visualX;

                float visualY = padY + (rows - 1 - node.row) * rowHeight + Jitter(decoRng, -jitterAmp, jitterAmp);
                visualNodePositions[node.id] = new Vector2(visualX, visualY);
            }
        }
    }

    void RenderNodesAndEdges()
    {
        foreach (var n in nodes)
        {
            var ve = new VisualElement();
            ve.name = n.id;
            ve.AddToClassList("map-node");
            ve.AddToClassList($"type-{n.type.ToLower()}");
            foreach (var tag in n.tags)
            {
                ve.AddToClassList($"tag-{tag.ToLower()}");
            }

            if (rows > 1 && n.row == rows - 2)
            {
                ve.AddToClassList("penultimate-row-node");
            }

            if (n.type == Pirate.MapGen.NodeType.Treasure.ToString().ToLower() && n.row >= rows / 3 && n.row <= (2 * rows / 3))
            {
                ve.AddToClassList("qa-treasure-window");
            }

            if (n.type == Pirate.MapGen.NodeType.Unknown.ToString().ToLower())
            {
                ve.tooltip = "Unknown Node - Tooltip Placeholder (Pity System Forecast Here)";
            }

            var p = visualNodePositions[n.id];
            ve.style.left = p.x - (56f / 2f);
            ve.style.top = p.y - (56f / 2f);

            canvas.Add(ve);
            nodeVisualElements[n.id] = ve;
            ve.RegisterCallback<ClickEvent>(evt => OnNodeClicked(n.id));
            ve.RegisterCallback<PointerEnterEvent>(evt => OnNodePointerEnter(n));
            ve.RegisterCallback<PointerLeaveEvent>(evt => OnNodePointerLeave(n));
        }

        playerIndicator = canvas.Q<VisualElement>("PlayerIndicator");
        if (playerIndicator == null) Debug.LogError("VisualElement 'PlayerIndicator' not found in UXML.");
        else playerIndicator.style.display = DisplayStyle.None;

        float maxY = nodes.Any() ? nodes.Max(n => visualNodePositions[n.id].y) : 0;
        canvas.style.height = padY * 2 + maxY + 300;
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
            return n.col * 100f; 
        }

        var px = parentEdges
            .Select(e => visualNodePositions[e.fromId].x)
            .Average();
        return (float)px;
    }

    static float Jitter(System.Random rng, float min, float max)
        => (float)(min + rng.NextDouble() * (max - min));

    private System.Random GetEdgeRNG(string fromId, string toId)
    {
        int seed = fromId.GetHashCode() ^ toId.GetHashCode();
        return new System.Random(seed);
    }

    void DrawEdges(MeshGenerationContext mgc)
    {
        var p2d = mgc.painter2D;
        p2d.lineWidth = edgeWidth;
        p2d.strokeColor = Color.white;

        float patternLength = dashLength + gapLength;

        foreach (var e in edges)
        {
            if (!visualNodePositions.TryGetValue(e.fromId, out Vector2 fromPos) || !visualNodePositions.TryGetValue(e.toId, out Vector2 toPos))
            {
                Debug.LogWarning($"Missing position for edge {e.fromId} -> {e.toId}");
                continue;
            }

            Vector2 a = fromPos - new Vector2(0, nodeHalfHeight + curvePadding);
            Vector2 b = toPos + new Vector2(0, nodeHalfHeight + curvePadding);

            float lineLength = Vector2.Distance(a, b);

            int numSegments = 100;
            float wavesPerUnitLength = 0.005f;
            float minAmplitude = 2f;
            float maxAmplitude = 5f;
            float amplitudeScale = 0.05f;

            float amplitude = Mathf.Clamp(lineLength * amplitudeScale, minAmplitude, maxAmplitude);
            float frequency = lineLength * wavesPerUnitLength;
            float damping = 0.9f;

            System.Random edgeRng = GetEdgeRNG(e.fromId, e.toId);

            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i <= numSegments; i++)
            {
                float t = (float)i / numSegments;
                Vector2 basePoint = Vector2.Lerp(a, b, t);

                Vector2 tangent = (b - a).normalized;
                Vector2 perpendicular = new Vector2(-tangent.y, tangent.x);

                float sinusoidalOffset = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude * (1f - t * damping);
                Vector2 perturbedPoint = basePoint + perpendicular * sinusoidalOffset;
                points.Add(perturbedPoint);
            }

            p2d.BeginPath();
            float distanceAlongCurve = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i+1];
                float segmentLength = Vector2.Distance(p1, p2);

                float currentPos = 0;
                while (currentPos < segmentLength)
                {
                    float patternPos = (distanceAlongCurve + currentPos) % patternLength;
                    
                    if (patternPos < dashLength)
                    {
                        float dashRemaining = dashLength - patternPos;
                        float drawLength = Mathf.Min(segmentLength - currentPos, dashRemaining);
                        
                        Vector2 start = Vector2.Lerp(p1, p2, currentPos / segmentLength);
                        Vector2 end = Vector2.Lerp(p1, p2, (currentPos + drawLength) / segmentLength);

                        p2d.MoveTo(start);
                        p2d.LineTo(end);

                        currentPos += drawLength;
                    }
                    else
                    {
                        float gapRemaining = patternLength - patternPos;
                        float skipLength = Mathf.Min(segmentLength - currentPos, gapRemaining);
                        currentPos += skipLength;
                    }
                }
                distanceAlongCurve += segmentLength;
            }
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
            return;
        }

        string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
        MapGraphData.Node currentPlayerNode = null;
        if (currentEncounterId != null && nodeById.ContainsKey(currentEncounterId))
        {
            currentPlayerNode = nodeById[currentEncounterId];
        }

        if (playerIndicator == null) Debug.LogError("PlayerIndicator is null!");

        if (currentPlayerNode != null)
        {
            Vector2 playerPos = visualNodePositions[currentPlayerNode.id];
            playerIndicator.style.left = playerPos.x - (40f / 2f);
            playerIndicator.style.top = playerPos.y - (40f / 2f);
            playerIndicator.style.display = DisplayStyle.Flex;
            playerIndicator.BringToFront();
            Debug.Log($"PlayerIndicator: Visible at {playerPos.x}, {playerPos.y}");
        }
        else
        {
            playerIndicator.style.display = DisplayStyle.None;
            Debug.Log("PlayerIndicator: Hidden (no current node).");
        }

        foreach (var node in nodes)
        {
            VisualElement ve = nodeVisualElements[node.id];
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
                ve.AddToClassList("node-visited");
            }
            else if (currentPlayerNode != null && node.row == currentPlayerNode.row + 1)
            {
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
                ve.AddToClassList("node-locked");
            }

            if (node.type == Pirate.MapGen.NodeType.Boss.ToString().ToLower())
            {
                float bossNodeY = visualNodePositions[node.id].y;
                float scrollY = scroll.scrollOffset.y;
                float previewThreshold = 200f;

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
        if (_isDragging) return;

        if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
        {
            Debug.LogWarning("GameSession or MapGraphData is not initialized.");
            return;
        }

        MapGraphData.Node clickedNode = nodeById[nodeId];
        MapGraphData.Node currentPlayerNode = null;

        if (GameSession.CurrentRunState.currentEncounterId != null && nodeById.ContainsKey(GameSession.CurrentRunState.currentEncounterId))
        {
            currentPlayerNode = nodeById[GameSession.CurrentRunState.currentEncounterId];
        }
        else
        {
            if (clickedNode.row == 0)
            {
                Debug.Log($"First move to node: {clickedNode.id}");
                GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
                GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;
                UpdateNodeVisualStates();
                return;
            }
            else
            {
                Debug.LogWarning($"Cannot move to {clickedNode.id}. Player's current position is not defined for non-start node.");
                return;
            }
        }

        if (clickedNode.row != currentPlayerNode.row + 1)
        {
            Debug.Log($"Invalid move: Node {clickedNode.id} is not in the next row. Current row: {currentPlayerNode.row}, Clicked row: {clickedNode.row}");
            return;
        }

        bool hasValidEdge = GameSession.CurrentRunState.mapGraphData.edges.Any(e => e.fromId == currentPlayerNode.id && e.toId == clickedNode.id);
        if (!hasValidEdge)
        {
            Debug.Log($"Invalid move: No direct path from {currentPlayerNode.id} to {clickedNode.id}.");
            return;
        }

        Debug.Log($"Valid move! Moving from {currentPlayerNode.id} to {clickedNode.id}.");
        GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
        GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;

        GameSession.InvokeOnPlayerNodeChanged();
    }

    private void OnNodePointerEnter(MapGraphData.Node hoveredNode)
    {
        if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
        {
            return;
        }

        string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
        MapGraphData.Node currentPlayerNode = null;
        if (currentEncounterId != null && nodeById.ContainsKey(currentEncounterId))
        {
            currentPlayerNode = nodeById[currentEncounterId];
        }

        if (currentPlayerNode == null) return;

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
        foreach (var node in nodes)
        {
            VisualElement ve = nodeVisualElements[node.id];
            ve.RemoveFromClassList("node-hover-highlight");
        }
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        _startMousePosition = evt.localPosition;
        _startScrollOffset = scroll.scrollOffset;
        _isDragging = false;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging && (evt.pressedButtons & 1) == 1)
        {
            Vector2 localPosition = evt.localPosition;
            if (Vector2.Distance(localPosition, _startMousePosition) > DragThreshold)
            {
                _isDragging = true;
                scroll.CapturePointer(evt.pointerId);
            }
        }

        if (_isDragging)
        {
            Vector2 localPosition = evt.localPosition;
            Vector2 delta = localPosition - _startMousePosition;
            scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, _startScrollOffset.y - delta.y);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (_isDragging)
        {
            scroll.ReleasePointer(evt.pointerId);
        }
        _isDragging = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying && _root != null)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && Application.isPlaying)
                {
                    LayoutAndRender();
                    UpdateNodeVisualStates();
                }
            };
        }
    }
#endif
}
