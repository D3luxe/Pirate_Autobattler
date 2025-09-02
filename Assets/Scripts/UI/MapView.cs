using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Pirate.MapGen;
using PirateRoguelike.Core;
using PirateRoguelike.Events;

namespace PirateRoguelike.UI
{
    public sealed class MapView : MonoBehaviour
    {
        public VisualTreeAsset uxml;
        public StyleSheet uss;

        private VisualElement _root;
        private Button _closeButton;

        [System.Serializable]
        public class EncounterIconMapping
        {
            public PirateRoguelike.Data.EncounterType type;
            public Sprite icon;
        }

        public List<EncounterIconMapping> encounterIcons;
        private Dictionary<PirateRoguelike.Data.EncounterType, Sprite> _iconLookup;

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

        [Header("Path Debug Colors")]
        public bool enablePathColorDebug = false;
        public Color[] pathColors = new Color[6] 
        {
            new Color(1, 0, 0, 0.5f), // Red
            new Color(0, 1, 0, 0.5f), // Green
            new Color(0, 0, 1, 0.5f), // Blue
            new Color(1, 1, 0, 0.5f), // Yellow
            new Color(1, 0, 1, 0.5f), // Magenta
            new Color(0, 1, 1, 0.5f)  // Cyan
        };
        public Color mergeColor = new Color(1, 1, 1, 0.7f); // White

        public float centeringOffset = 0f; // New serialized field for fine-tuning centering
        private float _previousCenteringOffset = 0f; // Store previous value for OnValidate

        private const float nodeHalfHeight = 28f; // From map-node height in USS
        private const float DragThreshold = 20f;

        ScrollView scroll;
        VisualElement canvas, edgeLayer, playerIndicator, scrollCenter;

        Dictionary<string, MapGraphData.Node> nodeById = new();
        private Dictionary<string, Vector2> visualNodePositions = new Dictionary<string, Vector2>();
        List<MapGraphData.Node> nodes = new();
        List<MapGraphData.Edge> edges = new();
        int rows = 0;

        System.Random decoRng;
        private Dictionary<string, VisualElement> nodeVisualElements = new Dictionary<string, VisualElement>();
        
        private Vector3 _startMousePosition;
        private Vector2 _startScrollOffset;
        private bool _dragOccurredThisCycle;

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

            _iconLookup = encounterIcons.ToDictionary(mapping => mapping.type, mapping => mapping.icon);

            _root.Clear();
            uxml.CloneTree(_root);
            _root.styleSheets.Add(uss);

            // Get reference to the new close button
            _closeButton = _root.Q<Button>("CloseButton");
            if (_closeButton == null) Debug.LogError("Button 'CloseButton' not found in UXML.");

            // Register close button click event
            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseButtonClicked;
                _closeButton.BringToFront(); // Add this line
            }

            // Subscribe to the map toggle event
            GameEvents.OnMapToggleRequested += ToggleMapVisibility;

            scroll = _root.Q<ScrollView>("MapScroll");
            if (scroll == null) Debug.LogError("ScrollView 'MapScroll' not found in UXML.");

            scrollCenter = _root.Q<VisualElement>("ScrollCenter");
            if (scrollCenter == null) Debug.LogError("VisualElement 'ScrollCenter' not found in UXML.");

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

                // --- Path Color Debugging ---
                if (enablePathColorDebug && n.PathIndices != null && n.PathIndices.Count > 0)
                {
                    if (n.PathIndices.Count > 1)
                    {
                        ve.style.backgroundColor = mergeColor;
                    }
                    else
                    {
                        int pathIndex = n.PathIndices.First();
                        if (pathIndex < pathColors.Length)
                        {
                            ve.style.backgroundColor = pathColors[pathIndex];
                        }
                    }
                }
                // --- End Path Color Debugging ---

                if (System.Enum.TryParse<PirateRoguelike.Data.EncounterType>(n.type, true, out var encounterType) && _iconLookup.TryGetValue(encounterType, out Sprite iconSprite))
                {
                    ve.style.backgroundImage = new StyleBackground(iconSprite);
                }
                else
                {
                    // Fallback to the old class-based color system if no icon is found
                    ve.AddToClassList($"type-{n.type.ToLower()}");
                }

                foreach (var tag in n.tags)
                {
                    ve.AddToClassList($"tag-{tag.ToLower()}");
                }

                if (rows > 1 && n.row == rows - 2)
                {
                    ve.AddToClassList("penultimate-row-node");
                }

                if (n.type == "Treasure" && n.row >= rows / 3 && n.row <= (2 * rows / 3))
                {
                    ve.AddToClassList("qa-treasure-window");
                }

                if (n.type == "Unknown")
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
            scrollCenter.style.height = canvas.style.height;
            edgeLayer.MarkDirtyRepaint();
        }

        void OnDestroy()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnMapDataUpdated -= HandleMapDataUpdated;
            }
            GameSession.OnPlayerNodeChanged -= UpdateNodeVisualStates;

            // Unregister close button event
            if (_closeButton != null)
            {
                _closeButton.clicked -= OnCloseButtonClicked;
            }

            // Unsubscribe from the map toggle event
            GameEvents.OnMapToggleRequested -= ToggleMapVisibility;
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
            PerformAutoScroll();
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

            float patternLength = dashLength + gapLength;

            foreach (var e in edges)
            {
                // --- Path Color Debugging ---
                if (enablePathColorDebug && e.PathIndices != null && e.PathIndices.Count > 0)
                {
                    if (e.PathIndices.Count > 1)
                    {
                        p2d.strokeColor = mergeColor;
                    }
                    else
                    {
                        int pathIndex = e.PathIndices.First();
                        if (pathIndex < pathColors.Length)
                        {
                            p2d.strokeColor = pathColors[pathIndex];
                        }
                    }
                }
                else
                {
                    p2d.strokeColor = Color.white; // Default color
                }
                // --- End Path Color Debugging ---

                if (!visualNodePositions.TryGetValue(e.fromId, out Vector2 fromPos) || !visualNodePositions.TryGetValue(e.toId, out Vector2 toPos))
                {
                    Debug.LogWarning($"Missing position for edge {e.fromId} -> {e.toId}");
                    continue;
                }

                Vector2 offset = new Vector2(0f, nodeHalfHeight + curvePadding);
                Vector2 a = (Vector2)fromPos - offset;
                Vector2 b = (Vector2)toPos + offset;

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
            _root.schedule.Execute(() => PerformAutoScroll()).Every(10).Until(() => scroll.contentContainer.resolvedStyle.height > 0);
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        public bool IsVisible()
        {
            return _root.style.display == DisplayStyle.Flex;
        }

        private void PerformAutoScroll()
        {
            //Debug.Log($"PerformAutoScroll() called. GameSession.CurrentRunState: {(GameSession.CurrentRunState != null ? "NOT NULL" : "NULL")}, mapGraphData: {(GameSession.CurrentRunState?.mapGraphData != null ? "NOT NULL" : "NULL")}");

            // Auto-scroll to player's current position or to the bottom
            if (GameSession.CurrentRunState != null && GameSession.CurrentRunState.mapGraphData != null)
            {
                

                // Calculate max scroll Y based on content height
                float maxScrollY = scroll.contentContainer.resolvedStyle.height - scroll.resolvedStyle.height;
                maxScrollY = Mathf.Max(0, maxScrollY); // Ensure it's not negative
                ////Debug.Log($"Max Scroll Y: {maxScrollY}");

                string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
                //Debug.Log($"Current Encounter ID: {currentEncounterId}");

                if (!string.IsNullOrEmpty(currentEncounterId) && nodeById.ContainsKey(currentEncounterId))
                {
                    // Player is at an encounter, scroll to that node
                    Vector2 playerNodePos = visualNodePositions[currentEncounterId];
                    float scrollViewHeight = scroll.resolvedStyle.height;
                    float targetScrollY = playerNodePos.y - (scrollViewHeight / 2f) + centeringOffset;

                    // Clamp the targetScrollY to ensure it's within valid scroll limits
                    targetScrollY = Mathf.Clamp(targetScrollY, 0, maxScrollY);

                    scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, targetScrollY);
                    //Debug.Log($"Scrolling to player node. Player Node Pos Y: {playerNodePos.y}, Target Scroll Y: {targetScrollY}, Centering Offset: {centeringOffset}");
                }
                else
                {
                    // No current encounter, scroll to the very bottom
                    scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, maxScrollY);
                    //Debug.Log($"No current encounter. Scrolling to bottom. Target Scroll Y: {maxScrollY}");
                }
            }
            else
            {
                //Debug.Log("Auto-scroll skipped: GameSession.CurrentRunState or mapGraphData is null.");
            }
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
                //Debug.Log($"PlayerIndicator: Visible at {playerPos.x}, {playerPos.y}");
            }
            else
            {
                playerIndicator.style.display = DisplayStyle.None;
                //Debug.Log("PlayerIndicator: Hidden (no current node).");
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
            // If a drag occurred during this click cycle, do not process as a click.
            if (_dragOccurredThisCycle)
            {
                _dragOccurredThisCycle = false; // Reset for next cycle
                return;
            }

            if (GameSession.CurrentRunState == null || GameSession.CurrentRunState.mapGraphData == null)
            {
                Debug.LogWarning("GameSession or MapGraphData is not initialized.");
                return;
            }

            string currentEncounterId = GameSession.CurrentRunState.currentEncounterId;
            MapGraphData.Node clickedNode = nodeById[nodeId];
            MapGraphData.Node currentPlayerNode = null;

            if (currentEncounterId != null && nodeById.ContainsKey(currentEncounterId))
            {
                currentPlayerNode = nodeById[currentEncounterId];
            }
            else
            {
                if (clickedNode.row == 0)
                {
                    //Debug.Log($"First move to node: {clickedNode.id}");
                    GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
                    GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;
                    UpdateNodeVisualStates();
                    GameSession.InvokeOnPlayerNodeChanged(); // Invoke for the first node as well
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
                Debug.Log($"Invalid move: Node {clickedNode.id} is not in the next row. Current row: {currentPlayerNode.row}, Clicked row: {clickedNode.id}");
                return;
            }

            bool hasValidEdge = GameSession.CurrentRunState.mapGraphData.edges.Any(e => e.fromId == currentPlayerNode.id && e.toId == clickedNode.id);
            if (!hasValidEdge)
            {
                Debug.Log($"Invalid move: No direct path from {currentPlayerNode.id} to {clickedNode.id}.");
                return;
            }

            // --- NEW: Handle Unknown Node Resolution ---
            if (clickedNode.type == Pirate.MapGen.NodeType.Unknown.ToString())
            {
                // Get necessary data for resolution
                ulong currentSeed = GameSession.CurrentRunState.randomSeed;
                IRandomNumberGenerator resolutionRng = new Xoshiro256ss(SeedUtility.CreateSubSeed(currentSeed, "unknown_resolution_" + clickedNode.id)); // Use node ID for unique sub-seed
                UnknownNodeResolver resolver = new UnknownNodeResolver();
                RulesSO rules = MapManager.Instance.RunConfig.rules; // Get rules from MapManager

                // Resolve the Unknown node
                Pirate.MapGen.NodeType resolvedNodeType = resolver.ResolveUnknownNode(
                    MapManager.Instance.CurrentMapGraph.Nodes.FirstOrDefault(n => n.Id == clickedNode.id), // Pass the original MapGraph.Node
                    new Pirate.MapGen.ActSpec { Rows = MapManager.Instance.mapLength }, // Pass ActSpec for row info
                    rules, // Pass rules for UnknownResolution settings
                    resolutionRng,
                    GameSession.CurrentRunState.pityState,
                    GameSession.CurrentRunState.unknownContext
                );

                // Update the node's type in all relevant data structures
                clickedNode.type = resolvedNodeType.ToString(); // Update the clickedNode (MapGraphData.Node)
                // Update the corresponding node in _convertedMapNodes
                // var convertedNode = _convertedMapNodes[clickedNode.row].FirstOrDefault(n => n.rowIndex == clickedNode.col);
                // if (convertedNode != null)
                // {
                //     convertedNode.nodeType = resolvedNodeType;
                //     // Also update encounter, iconPath, tooltipText, isElite based on resolvedNodeType
                //     // This logic is currently in MapManager.ConvertMapGraphToMapGraphData, need to extract or re-implement
                //     UpdateMapNodeDataEncounterInfo(convertedNode, resolvedNodeType, MapManager.Instance.RunConfig.rules); // Corrected line
                // }

                // Update the visual element to reflect the new type
                VisualElement ve = nodeVisualElements[clickedNode.id];
                ve.RemoveFromClassList("type-unknown"); // Remove old class
                ve.AddToClassList($"type-{resolvedNodeType.ToString().ToLower()}"); // Add new class
                // Update icon if applicable
                if (System.Enum.TryParse<PirateRoguelike.Data.EncounterType>(resolvedNodeType.ToString(), true, out var encounterType) && _iconLookup.TryGetValue(encounterType, out Sprite iconSprite))
                {
                    ve.style.backgroundImage = new StyleBackground(iconSprite);
                }
                else
                {
                    ve.style.backgroundImage = null; // Clear old icon if no new one
                }
                // Update tooltip
                ve.tooltip = $"Resolved to: {resolvedNodeType.ToString()}"; // Simple tooltip for now

                Debug.Log($"Node {clickedNode.id} resolved from Unknown to {resolvedNodeType}.");
            }
            // --- END NEW ---

            Debug.Log($"Valid move! Moving from {currentPlayerNode.id} to {clickedNode.id}.");
            GameSession.CurrentRunState.currentEncounterId = clickedNode.id;
            GameSession.CurrentRunState.currentColumnIndex = clickedNode.row;

            GameSession.InvokeOnPlayerNodeChanged();
        }

        // Helper method to update MapNodeData with encounter info (extracted from MapManager)
        private void UpdateMapNodeDataEncounterInfo(MapNodeData mapNode, Pirate.MapGen.NodeType resolvedNodeType, RulesSO rules)
        {
            PirateRoguelike.Data.EncounterSO encounter = null;
            switch (resolvedNodeType)
            {
                case Pirate.MapGen.NodeType.Battle:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == PirateRoguelike.Data.EncounterType.Battle && !e.isElite);
                    break;
                case Pirate.MapGen.NodeType.Elite:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == PirateRoguelike.Data.EncounterType.Elite);
                    break;
                case Pirate.MapGen.NodeType.Boss:
                    encounter = GameDataRegistry.GetEncounter("enc_boss");
                    break;
                case Pirate.MapGen.NodeType.Shop:
                    encounter = GameDataRegistry.GetEncounter("enc_shop");
                    break;
                case Pirate.MapGen.NodeType.Event:
                    encounter = GameDataRegistry.GetEncounter("enc_event");
                    break;
                case Pirate.MapGen.NodeType.Treasure:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == PirateRoguelike.Data.EncounterType.Treasure);
                    break;
                case Pirate.MapGen.NodeType.Port: // Added Port case
                    encounter = GameDataRegistry.GetEncounter("enc_port"); // Assuming a specific port encounter
                    break;
                default:
                    encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == PirateRoguelike.Data.EncounterType.Battle && !e.isElite);
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
                Debug.LogWarning($"Could not find a suitable encounter for resolved node type {resolvedNodeType}.");
            }
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
            // Perform a hit test to find the VisualElement at the pointer's position
            VisualElement clickedElement = scroll.panel.Pick(evt.position);

            bool clickedOnNode = false;
            if (clickedElement != null)
            {
                // Check if the clickedElement is one of our map nodes
                foreach (var entry in nodeVisualElements)
                {
                    if (entry.Value == clickedElement)
                    {
                        clickedOnNode = true;
                        break;
                    }
                }
            }

            if (clickedOnNode)
            {
                // If clicked on a node, do NOT capture pointer on ScrollView.
                // Let the event propagate so the node's ClickEvent can fire.
            }
            else
            {
                // If not clicked on a node, capture pointer for ScrollView dragging.
                _startMousePosition = evt.localPosition;
                _startScrollOffset = scroll.scrollOffset;
                _dragOccurredThisCycle = false; // Reset for new cycle
                scroll.CapturePointer(evt.pointerId);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!scroll.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            // Check if the mouse has moved beyond the drag threshold
            // and if the left mouse button is still pressed (for robustness)
            if ((evt.pressedButtons & 1) == 1 && Vector2.Distance((Vector2)evt.localPosition, (Vector2)_startMousePosition) > DragThreshold)
            {
                _dragOccurredThisCycle = true; // A drag has started

                // Update scroll offset based on initial position and current delta
                Vector2 delta = (Vector2)evt.localPosition - (Vector2)_startMousePosition;
                scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, _startScrollOffset.y - delta.y);

                // Do NOT stop propagation here. Let the ClickEvent be generated.
                // The OnNodeClicked method will check _dragOccurredThisCycle.
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            // Release pointer capture
            if (scroll.HasPointerCapture(evt.pointerId))
            {
                scroll.ReleasePointer(evt.pointerId);
            }
        }

        // Implement OnCloseButtonClicked() Method
        private void OnCloseButtonClicked()
        {
            Hide();
        }

        // Implement ToggleMapVisibility() Method
        private void ToggleMapVisibility()
        {
            if (IsVisible())
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

    #if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying && _root != null)
            {
                // If only centeringOffset changed, don't trigger re-layout
                if (centeringOffset != _previousCenteringOffset)
                {
                    _previousCenteringOffset = centeringOffset;
                    return;
                }

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && Application.isPlaying)
                    {
                        LayoutAndRender();
                        UpdateNodeVisualStates();
                    }
                };
            }
            _previousCenteringOffset = centeringOffset;
        }
    #endif
    }
}
