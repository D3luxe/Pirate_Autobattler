# Gemini Prompt — Visual Row Centering & Compaction (Render-Time Only)

Implement a **render-time** layout pass that centers and evenly spaces nodes **per row** while keeping the generator’s lanes and connectivity unchanged. This is purely visual; gameplay still uses the original lanes.

## Inputs
- `MapGraph { nodes[id,row,lane], edges[from,to] }` with lanes in `[0..4]`
- Rendering constants: `contentWidth`, `gutters`, `maxRowSpan`, `rowHeight`, `padY`, `minSep`, `jitterAmp`
- Determinism: `subSeeds.decorations` (seed for visual jitter)

## Constraints
- **Do not** change rows, lanes, or edges. Only compute `visualX`/`visualY` used for drawing.
- Vertical movement is strict by rows; edges always connect `row r → r+1`.

## Algorithm
1. **Collect & order per row**
   - For each row `r`, build `A = nodes where node.row == r`.
   - Stable **barycentric sort** `A` by the average parent `visualX` (fallback to lane order).

2. **Row center & spacing**
   ```
   c = contentWidth * 0.5
   k = |A|
   span = min(maxRowSpan, contentWidth - 2*gutters)
   if k <= 1:
       left = c; s = 0
   else:
       s = span / (k - 1)            // even spacing for this row only
       left = c - 0.5 * s * (k - 1)  // center the row
   ```

3. **Place nodes, then jitter (deterministic)**
   ```
   prevX = -INF
   for j in 0..k-1:
       x = (k == 1) ? left : left + j * s
       x += jitter(nodeId, seed=subSeeds.decorations) * jitterAmp   // apply AFTER spacing
       x = clamp(x, gutters, contentWidth - gutters)
       x = max(x, prevX + minSep)                                   // enforce separation
       node.visualX = x
       prevX = x
   ```

4. **Y position (strict rows)**
   ```
   node.visualY = padY + row * rowHeight
   ```

5. **Edges (render-time)**
   - Draw cubic **Bezier** curves from parent bottom to child top using `visualX/visualY`.
   - Control points vertical offset ≈ `0.4 * rowHeight` for smooth S-curves.

## UI Toolkit Notes
- Use an absolutely positioned **MapCanvas** inside a vertical-only `ScrollView`; **no grid/flex slots** for nodes.
- Store `visualX/visualY` separately from lane indices to preserve gameplay logic.
- Seed all randomness from `subSeeds.decorations` for reproducibility.

## Result
A row like `x | x | battle | battle | shop` (lanes 2–4 active) renders as **three nodes centered and evenly spaced** across the content width, with light deterministic jitter for an organic Slay‑the‑Spire look—while the underlying graph and pathing stay intact.
