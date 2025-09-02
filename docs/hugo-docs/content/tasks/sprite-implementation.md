---
title: "Sprite Implementation Task"
weight: 10
system: ["ui", "map"]
types: ["task", "plan", "implementation", "visual"]
tags: ["MapView", "Sprite", "UI Toolkit", "USS", "UXML", "EncounterType", "MapNode"]
---

# Map Node Sprite Implementation Plan (Final)

This document outlines the optimal strategy for transitioning the map view from using CSS-styled circles to a sprite-based system. This approach centralizes the icon definitions for easy management.

## 1. `MapView.cs` Modifications

The core of this strategy involves creating a centralized, editable mapping from an encounter's `Type` to its icon `Sprite` directly within the `MapView` component. This avoids repetitive data entry in `EncounterSO` assets and reliance on string-based paths.

### Proposed Steps:

1.  **Create an Icon Mapping Structure:**
    *   Add a small, serializable class inside `MapView.cs` to define the relationship between an `EncounterType` and its `Sprite`.

    ```csharp
    // Add this class inside the MapView.cs file, or in its own file.
    [System.Serializable]
    public class EncounterIconMapping
    {
        public EncounterType type;
        public Sprite icon;
    }
    ```

2.  **Expose the Mapping in the Inspector:**
    *   Add a public list of these mappings to `MapView.cs`. This will allow you to assign the sprites for each encounter type directly in the Unity Inspector, which is fast and intuitive.

    ```csharp
    // Add this field to the MapView class
    public List<EncounterIconMapping> encounterIcons;
    ```

3.  **Create an Efficient Lookup Dictionary:**
    *   To avoid slow lookups in the list during rendering, convert the list into a `Dictionary` once when the map is initialized.

    ```csharp
    // Add this dictionary to the MapView class
    private Dictionary<EncounterType, Sprite> _iconLookup;

    // In the Awake() method of MapView.cs:
    void Awake()
    {
        // ... existing Awake() code ...

        // Initialize the lookup dictionary for fast access
        _iconLookup = encounterIcons.ToDictionary(mapping => mapping.type, mapping => mapping.icon);
    }
    ```

4.  **Modify `RenderNodesAndEdges()`:**
    *   Update the node rendering logic to use the new `_iconLookup` dictionary. This is cleaner and more performant than loading from a `Resources` path on the fly.

    *   **New Code (`RenderNodesAndEdges`):**
        ```csharp
        var ve = new VisualElement();
        ve.name = n.id;
        ve.AddToClassList("map-node"); // Keep this for base styles

        // Assumes a GameDataRegistry is available to get the EncounterSO
        EncounterSO encounterSO = GameDataRegistry.GetEncounterSO(n.type);

        if (encounterSO != null && _iconLookup.TryGetValue(encounterSO.type, out Sprite iconSprite))
        {
            ve.style.backgroundImage = new StyleBackground(iconSprite);
        }
        else
        {
            // Fallback to the old class-based color system if no icon is found
            ve.AddToClassList($"type-{n.type.ToLower()}");
        }

        // ... (rest of the function remains the same)
        canvas.Add(ve);
        ```

## 2. `MapView.uss` Modifications

The stylesheet changes remain as previously planned. The goal is to remove color-based styling in favor of effects that work on sprites.

### Proposed Steps:

1.  **Remove Encounter Type Styles:**
    *   Delete all `background-color` rules for encounter types (e.g., `.map-node.type-battle`, `.map-node.type-elite`).

2.  **Modify Base and State Styles:**
    *   Update the `.map-node` class to remove `background-color` and `border-radius`.
    *   Change state styles (`.node-available`, `.node-locked`, etc.) to use `opacity` and `-unity-background-image-tint-color` to visually alter the sprites.

    *   **New Styles:**
        ```css
        .map-node {
            position: absolute;
            width: 56px;
            height: 56px;
            /* REMOVE: background-color, border-radius */

            background-size: cover;
            -unity-background-scale-mode: scale-to-fit;
            -unity-background-image-tint-color: white;
            transition: opacity 0.2s ease-in-out,
                        -unity-background-image-tint-color 0.2s ease-in-out,
                        transform 0.2s ease-out;
        }

        /* State Styles using Tint and Opacity */
        .map-node.node-visited {
            -unity-background-image-tint-color: rgb(150, 150, 150); /* Grey tint */
        }
        .map-node.node-locked {
            opacity: 0.5; /* Dimmed */
        }
        .map-node.node-hover-highlight {
            -unity-background-image-tint-color: yellow;
            transform: scale(1.2);
        }
        ```

## Summary of Changes

| File | Changes |
| :--- | :--- |
| **`EncounterSO.cs`** | No changes needed. The `iconPath` field will be ignored. |
| **`MapView.cs`** | - Add `EncounterIconMapping` class.<br>- Add `public List<EncounterIconMapping> encounterIcons` field.<br>- Create a `Dictionary` from the list in `Awake()` for performance.<br>- Modify `RenderNodesAndEdges()` to use the dictionary to set the node's background sprite. |
| **`MapView.uss`** | - Remove all `.type-*` classes that set `background-color`.<br>- Modify state-based classes to use `opacity` and `-unity-background-image-tint-color`. |