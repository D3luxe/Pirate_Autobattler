# Map Panel (Slay‑the‑Spire‑style) — UI Spec  
_Aligned with PRD v12 and the “Map System” prompt; for Unity UI Toolkit_

## Goals & Scope
Render a vertically scrollable, node‑and‑edge map (a directed acyclic graph) similar to **Slay the Spire**. The player starts at the **bottom** and advances **one row at a time** to the **Boss** at the top. The layout should be **organic (non‑grid)**, **deterministic** from seeds, and **fully data‑driven** (no hardcoded UXML per row).

---

## Core Model
- The map is a **DAG** with discrete **rows** (a.k.a. “floors”).  
- Each node belongs to exactly **one row**.  
- Edges only connect **row r → row r+1** (no skipping rows, no cycles).  
- Node types (icons/affordances only): `Battle, Elite, Port, Shop, Treasure, Event, Unknown (?), Boss`.

---

## Visual Guarantees (from PRD v12)
Ensure the renderer surfaces these guarantees supplied by generation:
- **Pre‑Boss Port** appears on the **penultimate row** (visually accent the row).
- **Mid‑act Treasure** appears within its configured window (optionally show a QA overlay “window” tag).
- At least **one valid path** exists from start to boss.
- Optional feature flags/tags:
  - **Burning Elite** (at most one, until claimed).
  - **Meta Keys** can attach to Port/Treasure/Burning Elite for progression systems.
- **Unknown (?) Pity System**: UI shows a **forecast** (weights) for Unknown outcomes; **resolution** happens in gameplay code (UI does not roll).

> Note: The UI **renders what the generator outputs** (counts, density, spacing). It must not invent nodes/edges.

---

## Data Contract (what the UI consumes)

```json
{
  "rows": 16,
  "nodes": [
    { "id": "n0", "row": 0, "col": 2, "type": "Start", "tags": [] },
    { "id": "n1", "row": 1, "col": 1, "type": "Battle", "tags": [] },
    { "id": "n2", "row": 1, "col": 3, "type": "Unknown", "tags": [] },
    { "id": "n3", "row": 2, "col": 2, "type": "Rest", "tags": [] },
    { "id": "nBoss", "row": 15, "col": 2, "type": "Boss", "tags": ["boss-preview"] }
  ],
  "edges": [
    { "from": "n0", "to": "n1" },
    { "from": "n0", "to": "n2" },
    { "from": "n1", "to": "n3" },
    { "from": "n2", "to": "n3" },
    { "from": "n3", "to": "nBoss" }
  ],
  "subSeeds": {
    "decorations": 123456789
  },
  "constants": {
    "rowHeight": 160,
    "laneWidth": 140,
    "mapPaddingX": 80,
    "mapPaddingY": 80,
    "minHorizontalSeparation": 70,
    "jitter": 18
  }
}
```

- `row` and `col` are discrete indices from generation.  
- `edges` will always be `row → row+1`.  
- `subSeeds.decorations` is used by the UI to seed **deterministic jitter/decoration**.  
- `constants` are rendering values (can come from config).

---

## Layout Rules (organic, never a grid)

### Coordinate System
- The map lives inside a **vertical `ScrollView`** (no horizontal scroll).
- Within it, a single **`MapCanvas`** is **absolutely positioned**; all child nodes are **absolute**.
- There is a single backmost **`EdgeLayer`** used to draw **all** edges with Beziers.

### Node Positions
- `x` comes from **column lanes** plus a small **deterministic jitter** (seeded using `subSeeds.decorations`):
  ```
  x = mapPaddingX + col * laneWidth + jitter(col,row, seed=subSeeds.decorations)
  ```
- `y` is strictly row‑based:
  ```
  y = mapPaddingY + row * rowHeight
  ```
- Enforce **minimum horizontal separation** between nodes in the same row (`minHorizontalSeparation`). If two nodes would overlap, nudge right (still deterministic).
- Apply a simple **barycentric ordering** within each row to lower edge crossings:
  - Sort nodes in row *r* by the **average x of their parents** in row *r‑1* (stable sort).

### Edges
- Draw each connection as a **cubic Bezier** from the **bottom anchor** of the parent to the **top anchor** of the child.
- Control points bias **vertically** to keep smooth S‑curves:
  ```
  c1 = a + (0, 0.4 * rowHeight)
  c2 = b - (0, 0.4 * rowHeight)
  ```
- Uniform line width; edges render **behind** nodes.

---

## Scrolling & Camera
- The `ScrollView` handles vertical drag/scroll.  
- `MapCanvas.height` = `mapPaddingY*2 + rowHeight*(rows-1) + extraBottomPadding`.  
- The **Boss preview** is always visible once scrolled to top; you may anchor an overlay preview.

---

## Interactivity
- Hover/selection highlights **reachable nodes** in the **next row** given the current selection (query from gameplay/graph helper).  
- Unknown nodes show a **tooltip forecast** of outcome weights from the pity system.  
- Optional overlays for QA:
  - Penultimate row highlight (pre‑boss Port).
  - Mid‑act Treasure “window” band.
  - Badge/halo for **Burning Elite** and **Meta Keys**.

---

## Determinism
- Use a dedicated, seeded RNG (not `UnityEngine.Random`’s global state) for all visual jitter/placements:
  - Seed = `subSeeds.decorations` (or equivalent provided by generation).
- With the same input graph + seeds, the layout must be **bit‑for‑bit identical** across machines.

---

## Unity UI Toolkit — Implementation Skeleton

### UXML
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ScrollView name="MapScroll" horizontal-scroller-visibility="Hidden">
    <VisualElement name="MapCanvas" class="map-canvas">
      <VisualElement name="EdgeLayer" class="edge-layer"/>
      <!-- Node elements are created at runtime -->
    </VisualElement>
  </ScrollView>
</ui:UXML>
```

### USS
```css
.map-canvas { position: relative; width: 100%; height: 6000px; }
.edge-layer { position: absolute; inset: 0; picking-mode: ignore; }
.map-node   { position: absolute; width: 56px; height: 56px; border-radius: 50%; }
.map-node.type-port { /* icon/color via class */ }
```

### C# (gist)
```csharp
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MapView : MonoBehaviour
{
    [Serializable] public class Node { public string id; public int row, col; public string type; public string[] tags; }
    [Serializable] public class Edge { public string fromId, toId; }

    public VisualTreeAsset uxml;
    public StyleSheet uss;

    // Rendering constants (could be loaded from JSON)
    public float rowHeight = 160, laneWidth = 140, jitter = 18, padX = 80, padY = 80;
    public float minSep = 70;

    ScrollView scroll;
    VisualElement canvas, edgeLayer;

    Dictionary<string, Node> nodeById = new();
    Dictionary<string, Vector2> pos = new();
    List<Node> nodes = new();
    List<Edge> edges = new();
    int rows = 16;

    System.Random decoRng;

    void Awake()
    {
        var root = uxml.CloneTree();
        root.styleSheets.Add(uss);
        GetComponent<UIDocument>().rootVisualElement.Add(root);

        scroll = root.Q<ScrollView>("MapScroll");
        canvas = root.Q<VisualElement>("MapCanvas");
        edgeLayer = root.Q<VisualElement>("EdgeLayer");
        edgeLayer.generateVisualContent += DrawEdges;

        // Load data (graph, seeds) then:
        // decoRng = new System.Random(unchecked((int)subSeeds.decorations));
        // nodes, edges, rows = from graph
        // For demo, assume already populated.
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
            rowNodes.Sort((a,b) =>
            {
                float ax = AvgParentX(a), bx = AvgParentX(b);
                int cmp = ax.CompareTo(bx);
                return (cmp != 0) ? cmp : a.col.CompareTo(b.col);
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
            ve.AddToClassList("map-node");
            ve.AddToClassList($"type-{n.type.ToLower()}");
            var p = pos[n.id];
            ve.style.left = p.x - 28;
            ve.style.top  = p.y - 28;
            canvas.Add(ve);
        }

        // 4) Resize canvas
        canvas.style.height = padY * 2 + rowHeight * (rows - 1) + 300;
        edgeLayer.MarkDirtyRepaint();
    }

    float AvgParentX(Node n)
    {
        var px = edges.Where(e => e.toId == n.id)
                      .Select(e => pos[e.fromId].x)
                      .DefaultIfEmpty(pos[n.id].x)
                      .Average();
        return (float)px;
    }

    static float Jitter(System.Random rng, float min, float max)
        => (float)(min + rng.NextDouble() * (max - min));

    void DrawEdges(MeshGenerationContext mgc)
    {
        var p2d = mgc.painter2D;
        p2d.lineWidth = 2f;

        foreach (var e in edges)
        {
            var a = pos[e.fromId] + new Vector2(0, 28);
            var b = pos[e.toId]   - new Vector2(0, 28);
            Vector2 c1 = a + new Vector2(0, 0.4f * rowHeight);
            Vector2 c2 = b - new Vector2(0, 0.4f * rowHeight);

            p2d.BeginPath();
            p2d.MoveTo(a);
            p2d.CubicTo(c1, c2, b);
            p2d.Stroke();
        }
    }
}
```

---

## “Copy to Gemini” — instruction block

> **Render a layered, one‑way DAG map from `MapGraph` (edges only `row→row+1`).** Use a vertical `ScrollView`; put nodes on an **absolutely positioned** `MapCanvas`; draw edges with a single **`EdgeLayer`** using **Bezier** curves.  
> **Node x** = `padX + col*laneWidth + deterministic jitter from subSeed 'decorations'`; **y** = `padY + row*rowHeight`.  
> Apply **barycentric ordering** within each row and enforce **minimum horizontal separation** to reduce crossings **without grids**.  
> **Do not** use Grid/Flex slots for node placement; create one VisualElement per node at runtime; UXML is static and minimal.  
> Show **Boss preview** at the top; visually highlight the **pre‑boss Port row** and the **mid‑act Treasure window** (from audits).  
> Hook tooltips for **Unknown** nodes to the pity‑system forecast; **do not** resolve Unknowns in the UI.  
> Everything must be **deterministic** from `GenerationResult.seed` and `subSeeds`.

---

## Common Pitfalls & Fixes
- **Looks like a grid:** Ensure **absolute positioning** + **deterministic jitter**; never place nodes in Flex/Grid slots.  
- **Messy lines:** Use **cubic Beziers** with **vertical control points** (~40% of `rowHeight`).  
- **Isolated nodes:** Generation should guarantee connectivity; if a row looks isolated, highlight via QA overlay rather than fabricating edges.  
- **Non‑reproducible layout:** Use a **local RNG seeded** from `subSeeds.decorations` for all jitter/ordering.

---

### Notes
- Replace placeholder constants (`rowHeight`, `laneWidth`, padding) with your tuned values.  
- Hook your actual `MapGraph`/`GenerationResult` types where the gist uses sample classes.
