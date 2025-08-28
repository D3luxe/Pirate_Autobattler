# Map Panel Close Button and Toggle Functionality Plan

This document outlines the steps to add a fixed "X" button to the upper-right corner of the map panel to hide it, and to implement toggle functionality for the map panel via the existing `#MapToggle` button in the Player Panel.

## 1. Proposed `MapPanel.uxml` Changes (Close Button)

To ensure the close button is anchored to the right side of the *visible* map panel area (the `ScrollView`'s viewport) and *does not scroll*, the `ui:Button` element should be placed as a direct child of the `ui:ScrollView`, making it a sibling to the `MapContentContainer`.

```xml
<UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/UI/MapView.uss?fileID=7433441132597879392&amp;guid=5d1ce1f7234762a4982274686ccb2b0d&amp;type=3#MapView" />

    <ui:ScrollView name="MapScroll" class="map-scroll">
        <!-- New Close Button -->
        <ui:Button name="CloseButton" text="X" class="close-button" />

        <ui:VisualElement name="MapContentContainer" class="map-content-container">
            <ui:VisualElement name="ScrollTopRoll" class="scroll-top-roll" />
            <ui:VisualElement name="ScrollCenter" class="scroll-center">
                <ui:VisualElement name="MapCanvas" class="map-canvas" style="left: auto; right: auto; width: 100%; align-self: center; margin-left: 0%;">
                    <ui:VisualElement name="EdgeLayer" class="edge-layer" style="left: 0; right: 0;" />
                    <ui:VisualElement name="PlayerIndicator" class="player-indicator" />
                </ui:VisualElement>
            </ui:VisualElement>
            <ui:VisualElement name="ScrollBottomRoll" class="scroll-bottom-roll" />
        </ui:VisualElement>
    </ui:ScrollView>
</UXML>
```

## 2. Proposed `MapView.uss` Changes (Close Button)

Add `position: relative;` to `.map-scroll` to establish a positioning context for its children. Then, adjust the `.close-button` styling to position it absolutely within the `map-scroll` viewport.

```css
/* In MapView.uss */
.map-scroll {
    flex-grow: 1;
    position: relative; /* Establish positioning context for absolute children */
}

.close-button {
    position: absolute;
    top: 10px; /* Distance from the top edge of the scroll view */
    right: 10px; /* Distance from the right edge of the scroll view */
    width: 40px; /* Button width */
    height: 40px; /* Button height */
    font-size: 24px; /* Size of the 'X' text */
    -unity-text-align: middle-center; /* Center the 'X' */
    background-color: rgba(0, 0, 0, 0.5); /* Semi-transparent background */
    border-radius: 5px; /* Slightly rounded corners */
    border-width: 1px;
    border-color: #FFFFFF;
    color: #FFFFFF; /* White 'X' text */
    z-index: 100; /* Ensure it's on top of other UI elements */
}

.close-button:hover {
    background-color: rgba(0, 0, 0, 0.7); /* Darker on hover */
}
```

## 3. Proposed `MapView.cs` Changes (Close Button and Toggle Functionality)

Modify the `MapView.cs` script to get a reference to the new close button and attach a click event handler. Additionally, `MapView.cs` will subscribe to an event from the Player Panel to toggle its visibility.

```csharp
// In MapView.cs

// Declare a Button field
private Button _closeButton;

void Awake()
{
    // ... existing Awake() code ...

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
    }

    // Subscribe to the map toggle event
    GameEvents.OnMapToggleRequested += ToggleMapVisibility;

    // ... rest of Awake() code ...
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

void OnDestroy()
{
    // ... existing OnDestroy() code ...

    // Unregister close button event
    if (_closeButton != null)
    {
        _closeButton.clicked -= OnCloseButtonClicked;
    }

    // Unsubscribe from the map toggle event
    GameEvents.OnMapToggleRequested -= ToggleMapVisibility;
}
```

## 4. Proposed `PlayerPanelController.cs` Changes (Map Toggle Button)

This script (or whichever script manages `PlayerPanel.uxml`) will be responsible for getting a reference to the `#MapToggle` button and publishing an event when it's clicked.

```csharp
// In PlayerPanelController.cs

// Declare a Button field
private Button _mapToggleButton;

void Awake()
{
    // Assuming _root is the root VisualElement of PlayerPanel.uxml
    _mapToggleButton = _root.Q<Button>("MapToggle");
    if (_mapToggleButton == null) Debug.LogError("Button 'MapToggle' not found in UXML.");

    // Register click event
    if (_mapToggleButton != null)
    {
        _mapToggleButton.clicked += OnMapToggleButtonClicked;
    }
}

// Implement OnMapToggleButtonClicked() Method
private void OnMapToggleButtonClicked()
{
    // Publish the event to toggle map visibility
    GameEvents.OnMapToggleRequested?.Invoke();
}

void OnDestroy()
{
    if (_mapToggleButton != null)
    {
        _mapToggleButton.clicked -= OnMapToggleButtonClicked;
    }
}
```

## 5. Proposed `GameEvents.cs` Changes (New Event)

Create a new static class `GameEvents` (if it doesn't exist) or add to an existing one, to define the event for toggling map visibility.

```csharp
// In GameEvents.cs (or similar central event class)
using System;

public static class GameEvents
{
    public static event Action OnMapToggleRequested;
}
```

## 6. Summary of Changes

| File | Changes |
| :--- | :--- |
| **`MapPanel.uxml`** | Add a `ui:Button` element (Close Button) inside `ui:ScrollView`. |
| **`MapView.uss`** | Add `position: relative;` to `.map-scroll`; adjust `.close-button` positioning. |
| **`MapView.cs`** | - Declare `_closeButton` field.<br>- Get close button reference and register click event in `Awake()`.<br>- Implement `OnCloseButtonClicked()` to call `Hide()`.<br>- Subscribe to `GameEvents.OnMapToggleRequested` in `Awake()`.<br>- Implement `ToggleMapVisibility()` to show/hide map.<br>- Unregister events in `OnDestroy()`. |
| **`PlayerPanelController.cs`** | - Declare `_mapToggleButton` field.<br>- Get map toggle button reference and register click event in `Awake()`/`OnEnable()`.<br>- Implement `OnMapToggleButtonClicked()` to publish `GameEvents.OnMapToggleRequested`.<br>- Unregister event in `OnDestroy()`/`OnDisable()`. |
| **`GameEvents.cs`** | Create/add `public static event Action OnMapToggleRequested;`. |