### **Analysis of `PlayerPanel.uxml`**

1.  **File Structure**: The file `Assets/UI/PlayerPanel/PlayerPanel.uxml` defines the layout for the player's main interface panel.
2.  **Key Components**:
    *   It uses a `<ui:Template>` to instantiate a reusable `ShipPanel.uxml`, which displays the ship's stats (health, shields, etc.). This instance is named `ship-panel-instance`.
    *   An `<ui:VisualElement>` named `equipment-bar` is used to display the ship's equipment.
    *   Other elements like `inventory-container`, `counters-container` (Gold, Lives, Depth), and `controls-container` are for player-specific information and are not needed for the enemy panel.
3.  **Styling**: The panel is styled using `player-panel.uss`. It is positioned at the bottom of the screen (`bottom: 0%`).

### **Implementation Plan: Enemy Panel**

The goal is to create a simplified version of the `PlayerPanel` for the enemy ship, positioned at the top of the screen during combat.

**Milestone 1: Create the UXML Asset for the Enemy Panel**

1.  **Create New Directory**: Create a new folder named `EnemyPanel` inside `Assets/UI/`.
2.  **Create UXML File**: Create a new UXML file named `EnemyPanel.uxml` inside `Assets/UI/EnemyPanel/`.
3.  **Copy and Modify**: Copy the content from `Assets/UI/PlayerPanel/PlayerPanel.uxml` and paste it into the new `EnemyPanel.uxml`. Make the following modifications:
    *   **Root Element**: Change the name of the root `VisualElement` from `player-panel` to `enemy-panel`.
    *   **Positioning**: In the `enemy-panel` element's inline style, change `bottom: 0%` to `top: 0%` to position it at the top of the screen.
    *   **Remove Unnecessary Elements**: Delete the `VisualElement`s with the following names, as they are not needed for the enemy display:
        *   `middle-column` (the player's inventory)
        *   `right-column` (the player's gold, lives, depth, and game controls)
        *   `MapToggle` (the button to open the map)
    *   **Adjust Background**: The background image might need to be flipped vertically or replaced to fit the top of the screen. For now, we will keep the existing background and adjust later if needed.

**Milestone 2: Create the C# Controller Script**

1.  **Create Script**: Create a new C# script named `EnemyPanelController.cs` in `Assets/Scripts/UI/`.
2.  **Script Logic**: This script will be responsible for populating the `EnemyPanel` with the enemy's data.
    *   It will need a public field to reference the `UIDocument` for the enemy panel.
    *   It will query for the `ship-panel-instance` and `equipment-bar` elements within its `rootVisualElement`.
    *   It will require a method, `Initialize(ShipState enemyShipState)`, which will be called by the combat system. This method will store the enemy's `ShipState` and subscribe to its events (e.g., `OnHealthChanged`, `OnEquipmentChanged`).
    *   It will include handler methods to update the UI (ship stats and equipment icons) when it receives events from the enemy's `ShipState`.

**Milestone 3: Create the Prefab and Integrate into the Battle Scene**

1.  **Create Prefab**: Create a new prefab named `EnemyPanel.prefab` in the `Assets/Prefabs/UI/` directory.
2.  **Configure Prefab**:
    *   Add a `UIDocument` component to the prefab. Assign the `EnemyPanel.uxml` asset to its `Source Asset` field.
    *   Add the `EnemyPanelController.cs` script to the prefab.
3.  **Scene Integration**:
    *   Open the `Battle` scene (`Assets/Scenes/Battle.unity`).
    *   Drag the `EnemyPanel.prefab` into the scene hierarchy.
    *   The `CombatController` (or equivalent manager script) will need to be modified to:
        *   Hold a reference to the `EnemyPanelController` in the scene.
        *   When a battle starts, it will call the `Initialize(enemyShipState)` method on the `EnemyPanelController`, passing the `ShipState` of the enemy ship for that encounter.
