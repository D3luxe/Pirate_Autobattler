Overall layout & positioning

Central vertical panel: The map lives in a single vertical-only ScrollView centered in the game view. It fills the safe-area width but leaves left/right gutters for fixed UI (e.g., Return on the left, Legend on the right).

Static HUD, scrolling content: Top run HUD and side overlays are anchored to screen edges and do not move. Only the map content scrolls.

Tall canvas: Inside the ScrollView is one absolutely positioned MapCanvas whose height = top/bottom padding + rowHeight × (rows−1). The player drags to scroll from start at the bottom to boss at the top. Horizontal scrolling is disabled.

Layering (back→front): parchment/background → EdgeLayer (all Bezier connections) → Node elements (icons, hitboxes) → overlays/tooltips (if any).

Coordinate system (for placement):

y = padY + row * rowHeight (strict vertical rows)

x = padX + col * laneWidth + small deterministic jitter (organic spacing; not a grid)

Edges always connect row r → r+1 as smooth cubic Beziers.

Responsiveness: The panel remains centered; gutters compress/expand with aspect ratio. Content scales uniformly; scroll behavior and absolute positions are unchanged.

Always-visible cues: When scrolled to the top, the boss preview/header is visible; pre-boss row and special windows (e.g., mid-act treasure) are highlighted within the canvas, not as separate screens.