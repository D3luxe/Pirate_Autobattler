Gaps / corrections (most important first)

Determinism + seeded jitter are mandatory (UI layer too).
The PRD requires a local, seeded RNG for all visual jitter/placements (seed = subSeeds.decorations) so the same inputs yield identical layouts across machines. Gemini mentions “jitter” but not the seeded determinism requirement for the UI. Ensure you derive all layout randomness from subSeeds.decorations and avoid UnityEngine.Random’s global state. 
 

Edges must be drawn on a single backmost EdgeLayer using cubic Beziers.
Gemini suggests “borders” as an option for lines; that contradicts the spec. Use one EdgeLayer with generateVisualContent/painter2D and the specified vertical-biased control points c1/c2 (~40% of rowHeight). Keep edges behind nodes with uniform width. 
 

Absolute positioning—never Grid/Flex for placement.
Nodes live on an absolutely positioned MapCanvas inside a vertical ScrollView. The PRD explicitly warns against using Grid/Flex slots for node placement; compute style.left/top from row/col + constants + deterministic jitter. 
 

UI must surface PRD “Visual Guarantees” & QA overlays.
Gemini doesn’t call out these required callouts: pre-boss Port row highlight, mid-act Treasure window band, and at least one valid path (UI should not “fix” maps; just surface/audit). Add QA overlays and do not fabricate nodes/edges. 
 

Unknown (?) pity system → tooltip forecast only (no UI-side resolution).
Replace any generic tooltip text with the specified forecast of outcome weights for Unknown nodes; resolution happens in gameplay code, not the UI. 

Scrolling & boss preview behavior.
Implement the height formula for the canvas and ensure the boss preview behavior when scrolled to the top. Gemini doesn’t mention these camera rules. 

Data contract fidelity (tags, constants, subSeeds).
If you choose to convert to List<List<MapNodeData>>, keep everything the renderer needs from the PRD’s data contract: id, row, col, type, tags (for Burning Elite, Meta Keys, boss preview, etc.), edges, constants, and subSeeds.decorations. Gemini lists some fields but doesn’t ensure tags/subSeeds make it through. 
 
 

Reachability UX specifics.
On hover/selection, highlight only nodes in the next row that are reachable from the current selection—this nuance is part of the spec.