---
title: "Debug Console"
weight: 10
system: ["crosscutting"]
types: ["system-overview", "reference"]
status: "approved"
discipline: ["engineering"]
stage: ["Completed"]
---

## Overview

The Debug Console is an in-game tool designed to assist developers and testers by providing real-time access to game state and the ability to execute commands. It functions as a command-line interface overlay, allowing for quick manipulation of game variables, scene loading, and other diagnostic actions without requiring a Unity Editor restart.

## Design

The Debug Console is implemented using Unity's UI Toolkit, ensuring it integrates seamlessly with the existing UI framework. Its design prioritizes functionality and ease of use during development.

*   **Visibility:** Toggled by a hotkey (backquote `).
*   **Input Field:** A `TextField` for command entry.
*   **Output Area:** A `ScrollView` to display command output and game logs.
*   **Global State Management:** Utilizes `UIStateService.IsConsoleOpen` to prevent conflicts with other UI elements (e.g., `SlotManipulator`) when the console is active.

## Implementation Details

### Core Components

*   **`DebugConsoleController` (`Assets/Scripts/UI/DebugConsoleController.cs`):**
    *   A `MonoBehaviour` responsible for managing the console's UI elements, handling input, processing commands, and logging output.
    *   It instantiates the console's UXML and applies its USS.
    *   It subscribes to `RunManager.OnToggleConsole` to manage its visibility.
    *   It registers a `KeyDownEvent` on its `TextField` with `TrickleDown.TrickleDown` phase to ensure immediate processing of the Enter key, preventing default `TextField` behavior and focus loss.
    *   It sets the `UIStateService.IsConsoleOpen` flag when its visibility changes.

*   **`RunManager` (`Assets/Scripts/Core/RunManager.cs`):**
    *   The persistent singleton responsible for managing the lifecycle of the debug console's input action.
    *   It holds a reference to the `InputSystem_Actions` asset.
    *   It finds the "ToggleConsole" action within the "Debug" action map.
    *   It exposes a static `OnToggleConsole` event, which `DebugConsoleController` subscribes to. This ensures the input action persists across scene loads.

*   **`InputSystem_Actions.inputactions` (`Assets/InputSystem_Actions.inputactions`):**
    *   Defines the "Debug" action map, which includes the "ToggleConsole" action bound to the backquote (`) key.
    *   The "ToggleConsole" action's binding has no specific interaction (empty string), ensuring it triggers immediately on key down.

*   **`UIStateService` (`Assets/Scripts/Services/UIStateService.cs`):**
    *   A static class providing a global `IsConsoleOpen` flag.
    *   Other UI components (e.g., `SlotManipulator`) check this flag to disable their functionality when the console is active, preventing input conflicts.

### Command Processing

The `ProcessCommand` method in `DebugConsoleController.cs` parses user input and executes corresponding actions.

**Available Commands:**

*   `help`: Displays a list of available commands.
*   `addgold <amount>`: Adds a specified amount of gold to the player's economy.
*   `addlives <amount>`: Adds a specified amount of lives to the player's economy.
*   `loadscene <sceneName>`: Loads a specified scene (e.g., `MainMenu`, `Boot`, `Run`, `Battle`, `Summary`). **Note:** Using `loadscene shop` will automatically set a default item count for the shop to ensure it is populated correctly for testing.
*   `skipnode`: Advances the player to the next node on the map.
*   `giveitem <itemId>`: Gives the player a specified item.
*   `generatereward [goldAmount] [showItems] [isElite] [floorIndex]`: Generates a reward window for testing. Defaults: 10 gold, show 3 items, non-elite, floor 5. `showItems` (boolean) controls if items are generated (default 3 items if true, 0 if false).

## Usage

1.  **Toggle Console:** Press the backquote (`) key to open or close the console.
2.  **Enter Commands:** Type commands into the input field at the bottom of the console.
3.  **Submit:** Press Enter to execute the command.
4.  **View Output:** Command output and game logs will appear in the scrollable area above the input field.

## Related Documents

*   [Core Systems Overview]({{< myrelref "../core/_index.md" >}})
*   [UI Systems Overview]({{< myrelref "../ui/ui-systems.md" >}})
*   [Input System Documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html) (External)
