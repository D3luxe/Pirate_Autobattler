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

    // Rendering constants (could be loaded from JSON)
    public float rowHeight = 160, laneWidth = 140, jitter = 18, padX = 80, padY = 80;
    public float minSep = 70;

    ScrollView scroll;
    VisualElement canvas, edgeLayer;

    Dictionary<string, MapGraphData.Node> nodeById = new();
    Dictionary<string, Vector2> pos = new();
    List<MapGraphData.Node> nodes = new();
    List<MapGraphData.Edge> edges = new();
    int rows = 0;

    System.Random decoRng;

    void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.Clear();
        uxml.CloneTree(root);
        root.styleSheets.Add(uss);

        scroll = root.Q<ScrollView>("MapScroll");
        canvas = root.Q<VisualElement>("MapCanvas");
        edgeLayer = root.Q<VisualElement>("EdgeLayer");
        edgeLayer.generateVisualContent += DrawEdges;

        // Load data (graph, seeds)
        MapGraphData mapGraphData = MapManager.Instance.GetMapGraphData();
        if (mapGraphData == null)
        {
            Debug.LogError("MapGraphData is null. Cannot render map.");
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
    }

    void LayoutAndRender()
    {
        nodeById = nodes.ToDictionary(n => n.id, n => n);

        // 1) Initial positions
        foreach (var n in nodes)
        {
            float x = padX + n.col * laneWidth + Jitter(decoRng, -jitter, jitter);
            float y = padY + n.row * rowHeight;
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

            var p = pos[n.id];
            // Adjust position to center the node element around its calculated (x,y)
            ve.style.left = p.x - (ve.resolvedStyle.width / 2); // Assuming map-node has a fixed width/height
            ve.style.top = p.y - (ve.resolvedStyle.height / 2); // Assuming map-node has a fixed width/height

            canvas.Add(ve);
        }

        // 4) Resize canvas
        // Calculate max Y to determine canvas height
        float maxY = nodes.Any() ? nodes.Max(n => pos[n.id].y) : 0;
        canvas.style.height = padY * 2 + maxY + 300; // Add extra padding for scrolling past the last node
        edgeLayer.MarkDirtyRepaint();
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

            Vector2 c1 = a + new Vector2(0, 0.4f * rowHeight);
            Vector2 c2 = b - new Vector2(0, 0.4f * rowHeight);

            p2d.BeginPath();
            p2d.MoveTo(a);
            p2d.BezierCurveTo(c1, c2, b);
            p2d.Stroke();
        }
    }

    }
