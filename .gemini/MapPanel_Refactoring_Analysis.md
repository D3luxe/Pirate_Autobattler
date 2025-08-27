# Map Panel Refactoring Analysis

## Objective
To align the current Map Panel implementation (`MapPanel.uxml` and `MapPanel.cs`) with the specifications outlined in `Map_Panel_Spec_PRD_v12.md`.

## Current State Analysis

### `Map_Panel_Spec_PRD_v12.md` (Key Requirements)
The specification details the desired functionality and appearance of the map panel, which typically includes:
*   **Visual Representation:** A clear, navigable map displaying various nodes (encounters, shops, boss, etc.).
*   **Node States:** Visual differentiation for visited, current, available, and locked nodes.
*   **Pathing:** Clear visual connections between nodes indicating possible routes.
*   **Player Position:** A distinct indicator for the player's current location.
*   **Navigation:** Interactive nodes allowing players to select and move to available next encounters.
*   **Information Display:** Potential for displaying details about selected nodes (e.g., encounter type, rewards).
*   **Dynamic Generation:** The map should be dynamically generated based on game progression or a predefined map structure.

### `MapPanel.uxml` (UI Structure)
The UXML defines the visual layout of the map panel. Based on typical Unity UI Toolkit practices for such a feature, it likely contains:
*   A root `VisualElement` acting as the container for the map.
*   Placeholder `VisualElement`s or `Button`s for individual map nodes.
*   Elements for drawing lines or connections between nodes.
*   Potentially, a `VisualElement` to represent the player's current position.
*   The structure needs to support dynamic instantiation and positioning of nodes and lines.

### `MapPanel.cs` (Logic)
This C# script is the controller for the `MapPanel.uxml`. Its responsibilities likely include:
*   Referencing `VisualElement`s defined in the UXML.
*   Loading map data from a game state manager (e.g., `GameSession` or a dedicated `MapManager`).
*   Dynamically creating and positioning map node UI elements based on the loaded map data.
*   Handling user input (e.g., clicks on nodes) to facilitate navigation.
*   Updating the visual state of nodes (e.g., changing color/icon for visited, current, available nodes).
*   Managing the visual representation of paths between nodes.
*   Interacting with other game systems (e.g., `SceneLoader` for transitioning to encounters, `EventBus` for map-related events).

### Deeper Analysis of Current Map Generation System

**`MapGenerator.cs` (Core Generation Logic):**
*   **Phased Generation:** Employs a 3-phase process for map creation:
    *   **Phase A (Skeleton Generation - `GenerateSkeleton`):** Constructs a layered Directed Acyclic Graph (DAG) of nodes and edges. It determines the number of nodes per row (1 for start/end, 2 to `Columns-1` for mid-rows) and establishes connections, ensuring each node has at least one incoming and outgoing connection. Nodes are initially set to `NodeType.Unknown`, except for the final row's node, which is designated `NodeType.Boss`.
    *   **Phase B (Typing Under Constraints - `ApplyTypingConstraints`):** Assigns specific `NodeType`s (Boss, Port, Treasure, Elite, Shop, Battle, Unknown) to nodes. This phase adheres to `ActSpec` and `Rules`, prioritizing guaranteed nodes (Boss, pre-boss Port, mid-act Treasure) and then distributing Elites, Shops, and Ports while respecting spacing rules. Remaining nodes are filled with `Unknown` or `Battle` types.
    *   **Phase C (Validation & Repair):** Utilizes `MapValidator` to verify the generated map against defined `Rules` (e.g., ensuring a valid path from start to boss). If validation fails, the system attempts to repair the map by regenerating sections or re-typing specific nodes.
*   **Deterministic Generation:** Leverages `IRandomNumberGenerator` and `SeedUtility` to ensure reproducible map generation.
*   **Internal Data Structures:** Uses `Node` and `Edge` classes (defined within `MapGraph.cs`) to represent the map structure during the generation process.

**`MapGraph.cs` (Map Data Structure during Generation):**
*   **`MapGraph`:** The primary container for the generated map, holding `Rows`, a list of `Node` objects, and a list of `Edge` objects.
*   **`Node`:** Represents an individual map node, including `Id`, `Row`, `Col`, `Type` (`NodeType` enum), `Tags` (for additional metadata), and `Meta` (a flexible dictionary for key-value pairs).
*   **`Edge`:** Defines a connection between two nodes using `FromId` and `ToId`.
*   **`NodeType` Enum:** Enumerates various encounter types such as Battle, Elite, Port, Shop, Treasure, Event, Unknown, and Boss.

**`MapManager.cs` (Runtime Map Management and Conversion):**
*   **Singleton:** Implemented as a `MonoBehaviour` singleton (`Instance`).
*   **Map Generation Trigger:** `GenerateMapIfNeeded()` invokes `GenerateMapData()` if the map has not yet been created.
*   **`GenerateMapData()`:**
    *   Initializes `ActSpec` and `Rules` (currently using a hardcoded seed).
    *   Instantiates `MapGenerator` and executes `GenerateMap()`.
    *   Stores the resulting `MapGraph` in `_currentMapGraph`.
    *   Initiates the conversion of `MapGraph` to `MapGraphData` via `ConvertMapGraphToMapGraphData()`.
*   **`ConvertMapGraphToMapGraphData()`:**
    *   Transforms the `MapGraph` (output from `MapGenerator`) into `MapGraphData`, which is intended for use by the UI and gameplay systems.
    *   Populates `MapGraphData.nodes` and `MapGraphData.edges` by mapping properties from the `Node` and `Edge` objects.
    *   Converts the `NodeType` enum to a string for `MapGraphData.Node.type`.
    *   Includes placeholder `constants` (e.g., `rowHeight`, `laneWidth`) within `MapGraphData` for UI layout purposes.



**`MapNodeData.cs` (Data for a Single Map Node - *Intended Intermediate Data Structure*):**
*   Contains properties suchs as `nodeType` (`NodeType` enum), `encounter` (`EncounterSO`), `nextNodeIndices`, `columnIndex`, `rowIndex`, `isElite`, `iconPath`, `tooltipText`, and `reachableNodeIndices`.
*   `GetUniqueNodeId()`: Generates an ID based on `columnIndex` and `rowIndex`.
*   **Observation:** This data structure is distinct from the `Node` class used in `MapGraph.cs`. However, `MapSystemIntegration.md` explicitly states that `MapManager` should convert `MapGraph` into the `List<List<MapNodeData>>` format expected by `MapPanel`. This indicates `MapNodeData` is the *intended* intermediate data structure for the UI, not a remnant to be removed. The challenge lies in ensuring this conversion is complete and accurate, populating all necessary fields for the UI.

**`MapView.cs` (PRD-aligned UI Implementation):**
*   A `MonoBehaviour` that largely implements the C# gist provided in `Map_Panel_Spec_PRD_v12.md`.
*   Consumes `MapGraphData` directly from `MapManager.Instance.GetMapGraphData()`.
*   Implements the PRD's layout rules, including deterministic jitter, barycentric ordering, and minimum horizontal separation.
*   Draws edges using cubic Bezier curves as specified in the PRD.
*   Dynamically creates `VisualElement`s for nodes and positions them based on calculated coordinates.
*   **Observation:** This implementation aligns very closely with the technical specifications of the PRD, but it uses `MapGraphData` directly, contrasting with `MapPanel.cs`'s reliance on `MapNodeData`.

## Gaps and Required Refactoring

The core map generation (`MapGenerator.cs`, `MapGraph.cs`) is robust and successfully produces a graph structure. The primary discrepancy lies in how this generated `MapGraph` is subsequently consumed and rendered by the UI.

**Key Areas for Clarification and Expansion:**

1.  **Conflicting Map UI Implementations and Data Models:**
    *   **Issue:** The project currently has two distinct UI implementations for the map panel: `MapPanel.cs` and `MapView.cs`.
        *   `MapPanel.cs` is the currently active UI controller, instantiated by `RunManager.cs` and controlled by `PlayerPanelController.cs`. It relies on `MapNodeData.cs` for its map representation, and `Saving/RunState.cs` also stores map data as `List<List<MapNodeData>>`.
        *   `MapView.cs` implements the layout and rendering logic as specified in `Map_Panel_Spec_PRD_v12.md`'s C# gist. It directly consumes `MapGraphData.Node` and `MapGraphData.Edge` (from `Pirate.MapGen`), which are the direct outputs of the `MapGenerator`.
    *   **Implication:** This creates a conflict in data models and UI implementation. The complex layout logic (jitter, barycentric ordering, Bezier curves) is already implemented in `MapView.cs` according to the PRD, but the active UI (`MapPanel.cs`) uses a different data structure (`MapNodeData`) and does not yet fully implement these advanced layout features.
    *   **Options for Resolution:**
        *   **Option A: Replace `MapPanel.cs` with `MapView.cs`:** This involves updating `RunManager.cs` and `PlayerPanelController.cs` to use `MapView.cs`. It also requires refactoring `Saving/RunState.cs` and `MapManager.cs` to consistently use `MapGraphData` instead of `MapNodeData`. This option leverages the existing PRD-aligned implementation in `MapView.cs` and provides a cleaner data flow.
        *   **Option B: Migrate `MapView.cs` logic into `MapPanel.cs`:** This involves porting the layout and rendering logic from `MapView.cs` into `MapPanel.cs`. `MapPanel.cs` would then need to be updated to consume `MapGraphData` directly, or `MapManager` would need to ensure a robust conversion from `MapGraphData` to `MapNodeData` that includes all necessary layout information for `MapPanel.cs` to implement the PRD's visual requirements.
    *   **Recommendation:** Option A is generally recommended as it leverages existing, PRD-aligned work and provides a cleaner data flow. However, it requires more significant refactoring of data structures across the project.

2.  **UI Toolkit Integration and Dynamic Generation:**
    *   **Issue:** The integration between the generated `MapGraphData` (or `MapNodeData`, depending on the chosen implementation) and the UI Toolkit display needs to be robust. The chosen UI implementation must dynamically create and manage `VisualElement`s for map nodes and connections.
    *   **Clarification Needed:** How are nodes currently displayed within `MapPanel.uxml`? The chosen UI implementation must assume responsibility for dynamically creating `VisualElement`s for each node object.
    *   **Expansion:** The chosen map UI implementation must assume responsibility for dynamically creating `VisualElement`s for each node object. This entails:
        *   Developing a UI Toolkit `VisualElement` representation for a single map node (e.g., a custom `VisualElement` or a `Button` with appropriate styling).
        *   Precisely positioning these `VisualElement`s using the `row`, `col` from the node data and the layout `constants` (e.g., `rowHeight`, `laneWidth`) from `MapGraphData` (which should be passed through the conversion or accessed via `MapManager`).
        *   Implementing the drawing of connections (lines) between these `VisualElement`s based on the node's `nextNodeIndices` (or equivalent edge data).

3.  **Navigation Logic Adaptation:**
    *   **Issue:** Any legacy navigation logic relying on a `columnIndex - 1` check implies a strict linear progression. The chosen map UI implementation must fully leverage and respect the generated graph structure.
    *   **Clarification:** The navigation logic within the chosen map UI implementation must be updated to fully leverage and respect the generated graph structure, ensuring movement is always forward (row r to row r+1) but allowing valid branching.
    *   **Expansion:** Upon a node click, the chosen map UI implementation should:
        *   Accurately identify the clicked node object.
        *   Verify if the clicked node is directly reachable from the player's current node by consulting the appropriate edge data (e.g., `nextNodeIndices` or `MapGraph.Edges`).
        *   If valid, update the player's current node within the game state.
        *   Visually refresh the map to reflect the change (e.g., highlight the new current node, mark the previous node as visited, and enable/disable interaction for reachable/unreachable nodes).

4.  **Map UI Layout and Positioning:**
    *   **Issue:** `MapGraphData.constants` contains critical layout parameters such as `rowHeight`, `laneWidth`, `mapPaddingX`, `mapPaddingY`, `minHorizontalSeparation`, and `jitter`. The chosen map UI implementation must correctly utilize these constants for positioning nodes within `MapPanel.uxml`.
    *   **Clarification:** `Map_Panel_Spec_PRD_v12.md` explicitly defines the "Data Contract" including these `constants` and provides "Layout Rules" with formulas for `x` and `y` coordinates.
    *   **Expansion:** The chosen map UI implementation will be responsible for employing these constants (retrieved from `MapManager` or passed through the node data) to precisely calculate the `style.left` and `style.top` (or `transform.position`) for each node `VisualElement` within the `MapPanel.uxml` container. This involves converting the `row` and `col` from the node data into appropriate screen coordinates using the formulas provided in the PRD, and also implementing barycentric ordering, minimum horizontal separation, and *deterministic jitter seeded from `subSeeds.decorations`*.

5.  **Visual Guarantees and QA Overlays:**
    *   **Issue:** The UI must visually surface specific guarantees provided by the map generation and offer optional QA overlays as per the PRD. These include:
        *   Pre-Boss Port appearing on the penultimate row (visually accent the row).
        *   Mid-act Treasure appearing within its configured window (optionally show a QA overlay “window” tag).
        *   Badge/halo for Burning Elite and Meta Keys.
        *   The UI should *not* "fix" maps or fabricate nodes/edges if generation guarantees connectivity; it should just surface/audit.
    *   **Clarification:** The UI needs to interpret and display these specific visual cues based on the node data (e.g., `tags`, `NodeType`).
    *   **Expansion:** The chosen map UI implementation must:
        *   Implement distinct visual treatments (e.g., styling, icons, overlays) for nodes based on their `NodeType` and `tags` (e.g., `boss-preview`, `burning-elite`, `meta-key`).
        *   Provide visual accentuation for the pre-boss Port row.
        *   Implement optional QA overlays for features like the mid-act Treasure window.

6.  **Unknown Node Interactivity:**
    *   **Issue:** Unknown nodes require specific tooltip behavior and must *not* be resolved on the UI side.
    *   **Clarification:** The UI should only display a forecast of outcome weights from the pity system when hovering over an Unknown node. The actual resolution of the Unknown node must happen in gameplay code, not within the UI.
    *   **Expansion:** The chosen map UI implementation must:
        *   Implement a tooltip mechanism for Unknown nodes that displays the forecast of outcome weights from the pity system.
        *   Ensure that no logic within the UI attempts to resolve the outcome of an Unknown node.

7.  **Scrolling and Boss Preview Behavior:**
    *   **Issue:** The map panel has specific scrolling and boss preview requirements.
    *   **Clarification:** The `ScrollView` must handle vertical drag/scroll. The `MapCanvas.height` needs to be calculated using the formula: `mapPaddingY*2 + rowHeight*(rows-1) + extraBottomPadding`. The Boss preview must always be visible once scrolled to the top, potentially by anchoring an overlay preview.
    *   **Expansion:** The chosen map UI implementation must:
        *   Correctly set the `MapCanvas.height` based on the provided formula to ensure proper scrolling.
        *   Implement the logic to ensure the Boss preview remains visible when the map is scrolled to the top.

8.  **Data Contract Fidelity:**
    *   **Issue:** Regardless of the chosen UI implementation (`MapPanel.cs` or `MapView.cs`), the data consumed by the UI must faithfully represent all necessary fields from the PRD's data contract.
    *   **Clarification:** This includes `id`, `row`, `col`, `type`, `tags` (for Burning Elite, Meta Keys, boss preview, etc.), `edges`, `constants`, and `subSeeds.decorations`. If `MapNodeData` is used, it must be ensured that all these fields are correctly populated during conversion from `MapGraphData`.
    *   **Expansion:** The data flow from `MapManager` to the chosen UI implementation must ensure that all relevant data points from the `MapGraphData` (or `MapNodeData` if used) are accessible and correctly mapped for UI rendering and logic.

9.  **Reachability UX Specifics:**
    *   **Issue:** The interactivity for node selection needs to precisely follow the PRD's specification for highlighting reachable nodes.
    *   **Clarification:** On hover/selection, the UI must highlight *only* nodes in the *next row* that are reachable from the current selection. This nuance is critical for guiding player navigation.
    *   **Expansion:** The chosen map UI implementation must:
        *   Implement hover and selection states for nodes.
        *   When a node is hovered or selected, query the gameplay/graph helper to identify reachable nodes in the *immediately subsequent row*.
        *   Visually highlight *only* these reachable nodes, providing clear feedback to the player.

## Steps to Move Forward

Here's a refined phased approach to refactor the Map Panel, incorporating the deeper understanding of the map generation system and the role of `MapNodeData`:

1.  **Data Model Alignment (Choose one based on resolution option):**
    *   **If Option A (Replace `MapPanel.cs` with `MapView.cs`) is chosen:**
        *   **Action:** Ensure `MapManager` correctly provides `MapGraphData` (which it largely does already). Refactor `Saving/RunState.cs` to store `MapGraphData` directly.
        *   **Details:** Verify `MapManager`'s `ConvertMapGraphToMapGraphData()` populates all necessary fields in `MapGraphData.Node` (e.g., `tags`). Update `Saving/RunState.cs` to use `MapGraphData` instead of `List<List<MapNodeData>>`.
    *   **If Option B (Migrate `MapView.cs` logic into `MapPanel.cs`) is chosen:**
        *   **Action:** Ensure `MapManager`'s `ConvertMapGraphToMapGraphData()` (or a similar method) accurately and completely transforms the `MapGraph` into `List<List<MapNodeData>>` (if `MapPanel.cs` continues to use `MapNodeData`). Alternatively, update `MapPanel.cs` to consume `MapGraphData` directly.
        *   **Details:** Populate `MapNodeData.nextNodeIndices` for each `MapNodeData` object based on the `MapGraph.Edges`, ensuring it reflects the `row r -> row r+1` connections. Ensure `iconPath` and `tooltipText` are correctly derived and assigned to `MapNodeData`. Consider how `MapGraphData.constants` (layout parameters) will be made available to `MapPanel.cs` (either directly from `MapManager` or by including them in the `MapNodeData` structure if appropriate).

2.  **UI Toolkit Dynamic Generation (for the chosen implementation):**
    *   **Action:** The chosen map UI implementation (`MapPanel.cs` or `MapView.cs`) must dynamically create and manage `VisualElement`s, drawing directly from the map data received from `MapManager`.
    *   **Details:**
        *   **Node Template:** Create a reusable UI Toolkit `VisualElement` template (either a custom UXML file or a C# class inheriting from `VisualElement`) specifically for a single map node. This template should be capable of displaying the `NodeType`, any relevant tags, and visually responding to various state changes (e.g., visited, current, available).
        *   **Display Method:** Implement a method within the chosen UI implementation (e.g., `DisplayMap(MapGraphData mapData)` or `DisplayMap(List<List<MapNodeData>> mapData)`) that:
            *   Clears any previously generated map UI elements.
            *   Iterates through the map data.
            *   Instantiates and configures a node `VisualElement` for each node object.
            *   Calculates the precise `style.left` and `style.top` (or `transform.position`) for each node `VisualElement`. This calculation *must* utilize the layout `constants` (e.g., `rowHeight`, `laneWidth`) and the node's row and column properties, applying the formulas and rules (barycentric ordering, min horizontal separation, and *deterministic jitter seeded from `subSeeds.decorations`*) from `Map_Panel_Spec_PRD_v12.md`. Avoid `UnityEngine.Random`'s global state for UI randomness.
            *   Adds these newly created node `VisualElement`s to the `MapPanel.uxml`'s visual tree.
            *   Nodes must be absolutely positioned within the `MapCanvas`. Avoid using Grid or Flexbox for node placement.
        *   Attaches appropriate click event handlers to each node `VisualElement`.

3.  **Implement Graph-Based Navigation Logic in `MapPanel.cs`:**
    *   **Action:** Update the node click handling within `MapPanel.cs` to leverage `MapNodeData.nextNodeIndices` for accurate and valid path checking, ensuring forward-only movement (row r to row r+1) while allowing branching.
    *   **Details:**
        *   **Node Identification:** When a node `VisualElement` is clicked, retrieve its corresponding `MapNodeData` object.
        *   **Current Player Node:** Obtain the player's current node information from `GameSession` or `MapManager`.
        *   **Path Validation:** Check if the clicked node's `rowIndex` is exactly one greater than the player's current node's `rowIndex`, and if the clicked node's ID is present in the `nextNodeIndices` of the player's current node.
        *   **State Update:** If the path is valid, update the player's current node within the `GameSession`.
        *   **Visual Feedback:** Visually update the map to reflect the change: highlight the new current node, mark the previous node as visited, and dynamically enable/disable interaction for other nodes based on their reachability from the new current position.

4.  **Implement Path Visualization (Lines):**
    *   **Action:** Draw clear visual lines or connectors between all connected nodes, based on the appropriate edge data (e.g., `MapNodeData.nextNodeIndices` or `MapGraph.Edges`).
    *   **Details:**
        *   For each node object, draw lines from its position to the positions of the nodes indicated by its connected edges.
        *   This *must* be achieved using a single backmost `EdgeLayer` with `generateVisualContent` and `painter2D` to draw cubic Bezier curves, following the specified vertical-biased control points (~40% of `rowHeight`) from `Map_Panel_Spec_PRD_v12.md`. Edges must render behind nodes with uniform width.

5.  **Visual State Management for Nodes:**
    *   **Action:** Implement robust visual feedback mechanisms to clearly indicate the state of each node (visited, current, available, locked).
    *   **Details:**
        *   Define distinct USS (Unity Style Sheet) classes for each node state (e.g., `.node-visited`, `.node-current`, `.node-available`, `.node-locked`).
        *   In the chosen map UI implementation, dynamically apply or remove these USS classes to the respective node `VisualElement`s based on their current state within the game logic.

6.  **Player Position Indicator:**
    *   **Action:** Introduce a dedicated `VisualElement` to visually represent the player's current position on the map and ensure its position is dynamically updated.
    *   **Details:**
        *   In `MapPanel.uxml`, add a `VisualElement` (e.g., an `Image` for a player sprite, or a simple `VisualElement` with a distinct background color) to serve as the player indicator.
        *   In the chosen map UI implementation, continuously update the `style.left` and `style.top` properties of this player indicator `VisualElement` to precisely match the calculated position of the current player node.

7.  **Refine `MapManager` Integration:**
    *   **Action:** Ensure seamless and efficient communication between `MapManager` and the chosen map UI implementation, allowing `MapManager` to provide map data and the UI to react to map-related events.
    *   **Details:**
        *   The chosen map UI implementation should subscribe to a relevant event exposed by `MapManager` (e.g., `OnMapDataReady`) to receive the map data (e.g., `MapGraphData` or `List<List<MapNodeData>>`) and trigger its display method.
        *   The chosen map UI implementation should also listen for player movement events (e.g., from `GameSession` or `MapManager`) to ensure the map's visual state, including node highlights and player indicator position, is always up-to-date.

This refined analysis provides a comprehensive roadmap for refactoring the Map Panel to fully leverage the generated graph structure and align with the specified requirements.
