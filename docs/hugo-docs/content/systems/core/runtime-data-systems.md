---
title: "Runtime Data Systems: Items, Abilities, and Actions"
weight: 10
system: ["core"]
types: ["system-overview"]
status: "approved"
---

## 1. Problem Statement

Previously, game items, abilities, and actions were primarily defined by `ScriptableObject` assets. While excellent for defining static, immutable data templates, this approach proved rigid when dealing with dynamic gameplay scenarios such as:

*   Applying temporary buffs or debuffs to item stats (e.g., an item's damage increasing during combat).
*   Modifying item properties permanently within a game run (e.g., an event granting a permanent damage bonus to an item).
*   Displaying real-time, updated values in UI elements like tooltips, as the `ScriptableObject`s only held base values.

## 2. Solution: Runtime Representations

To overcome these limitations, a new layer of runtime C# classes has been introduced. These classes act as wrappers around their corresponding `ScriptableObject` blueprints, allowing for dynamic state management during gameplay. The core of this system is the `ItemInstance` class, which represents a unique item within the game world.

### 2.1. Core Components

*   **`ItemInstance` (`Assets/Scripts/Combat/ItemInstance.cs`):**
    *   The primary class representing a unique instance of an item in the game (e.g., in an inventory or equipped on a ship).
    *   Holds a reference to its `ItemSO` blueprint for base data.
    *   Contains mutable fields for instance-specific state like `CooldownRemaining` and `StunDuration`.
    *   Crucially, it holds a reference to a `RuntimeItem` instance.

*   **`RuntimeItem` (`Assets/Scripts/Runtime/RuntimeItem.cs`):**
    *   Represents the dynamic abilities and actions of an `ItemInstance`.
    *   Itself created from an `ItemSO` blueprint.
    *   Manages a collection of `RuntimeAbility` instances. This is where dynamic modifications to an item's *behavior* would be applied.

*   **`RuntimeAbility` (`Assets/Scripts/Runtime/RuntimeAbility.cs`):**
    *   Represents a unique instance of an ability associated with a `RuntimeItem`.
    *   Holds a reference to its `AbilitySO` blueprint.
    *   Manages a collection of `RuntimeAction` instances.

*   **`RuntimeAction` (`Assets/Scripts/Runtime/RuntimeAction.cs`):**
    *   An abstract base class for all unique action instances.
    *   Holds a reference to its `ActionSO` blueprint.
    *   Defines the `BuildDescription(IRuntimeContext context)` method for dynamic tooltip text generation.

*   **Concrete `RuntimeAction` Implementations (`Assets/Scripts/Runtime/`):**
    *   `RuntimeDamageAction`, `RuntimeHealAction`, `RuntimeApplyEffectAction`, etc. These wrap their corresponding `ActionSO` and hold mutable, runtime-specific values (e.g., `CurrentDamageAmount`).

### 2.2. How it Works

1.  When an item is needed in the game (e.g., from a shop, reward, or being equipped), an **`ItemInstance`** is created from an `ItemSO`.
2.  The `ItemInstance`'s constructor immediately creates a corresponding **`RuntimeItem`** instance, also from the `ItemSO`.
3.  The `RuntimeItem`'s constructor then recursively instantiates `RuntimeAbility` and `RuntimeAction` objects based on the `ItemSO`'s configuration.
4.  These `RuntimeAction` instances initialize their mutable fields (e.g., `CurrentDamageAmount`) from the base values in their `ActionSO` blueprints.
5.  The `ItemInstance` is what gets stored in the `Inventory` or `ShipState`. Any buffs, debuffs, or other runtime modifications are applied to the mutable fields within the `ItemInstance` or its nested `Runtime...` objects.

## 3. Benefits of the New System

*   **Flexibility:** Enables dynamic modification of item, ability, and action values at runtime.
*   **Accuracy:** Tooltips now display real-time, accurate information.
*   **Modularity:** Clearly separates static data (`ScriptableObject`s) from instance data (`ItemInstance`) and dynamic behavior data (`RuntimeItem`), improving code organization.
*   **Testability:** Runtime objects are easier to test in isolation.
*   **Consistency:** The same underlying system is used for both player and enemy item management and UI display.

## 4. Key Files Involved

*   `Assets/Scripts/Combat/ItemInstance.cs` (Primary runtime object)
*   `Assets/Scripts/Runtime/RuntimeItem.cs` (Handles abilities/actions)
*   `Assets/Scripts/Runtime/RuntimeAbility.cs`
*   `Assets/Scripts/Runtime/RuntimeAction.cs`
*   `Assets/Scripts/Combat/IRuntimeContext.cs`
*   `Assets/Scripts/Runtime/RuntimeDamageAction.cs` (Example concrete action)
*   `Assets/Scripts/Runtime/RuntimeHealAction.cs` (Example concrete action)
*   `Assets/Scripts/Runtime/RuntimeApplyEffectAction.cs` (Example concrete action)
