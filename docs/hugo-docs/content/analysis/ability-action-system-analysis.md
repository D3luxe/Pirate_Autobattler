---
title: "Ability Action System"
weight: 10
system: ["combat"]
types: ["analysis","system-overview"]
status: "archived"
discipline: ["engineering"]
stage: ["production"]
---

# Ability and Action System Analysis

This document provides a thorough analysis of the `AbilitySO` and `ActionSO` system, including its base classes and concrete implementations, to assess its current design, flexibility, and potential areas of rigidity.

## Problem/Topic

Assessment of the current design, flexibility, and potential areas of rigidity within the `AbilitySO` and `ActionSO` system, including its base classes and concrete implementations.

## Analysis

### 1. AbilitySO.cs

**File Path:** `Assets/Scripts/Data/Abilities/AbilitySO.cs`

**Purpose:**
`AbilitySO` serves as the primary definition for an in-game ability. It's a `ScriptableObject`, allowing abilities to be created and configured as data assets within the Unity Editor, promoting a data-driven design.

**Key Fields:**
*   **`id` (string):** A unique identifier for the ability.
*   **`displayName` (string):** The human-readable name of the ability, primarily used for UI display.
*   **`trigger` (TriggerType):** Defines *when* the ability can be activated (e.g., OnReady, OnDamageDealt). This is an enum that dictates the conditions under which the ability's actions will be executed.
*   **`actions` (List<ActionSO>):** A list of `ActionSO` objects. This is the core of the ability, defining *what* the ability does. An ability can comprise one or many distinct actions.

**Relationship with ActionSO:**
`AbilitySO` acts as a container and orchestrator for `ActionSO`s. It defines the trigger condition and then delegates the actual execution logic to its contained `ActionSO` instances. This composition allows for complex abilities to be built from simpler, reusable action components.

**Flexibility/Rigidity:**
*   **Flexibility:** The `actions` list provides significant flexibility. New actions can be added or removed from an ability without modifying code, simply by adjusting the `AbilitySO` asset in the editor. This promotes reusability of `ActionSO`s across different abilities.
*   **Rigidity:** The `TriggerType` enum, while clear, can be rigid if new, complex trigger conditions are required that don't fit existing enum values. Extending this would require code changes to the enum and potentially the system that processes triggers.

### 2. ActionSO.cs (Base Class)

**File Path:** `Assets/Scripts/Data/Actions/ActionSO.cs`

**Purpose:**
`ActionSO` is an abstract base class for all specific actions that an ability can perform. It defines the common interface and properties that all actions must adhere to. Like `AbilitySO`, it's a `ScriptableObject`, enabling data-driven action definitions.

**Key Fields/Methods:**
*   **`description` (string, `[TextArea]`, `[SerializeField]`):** A string field intended to hold a human-readable description of the action. This field was recently exposed via a public `Description` property.
*   **`Execute(CombatContext ctx)` (abstract method):** This is the core method that concrete `ActionSO` implementations must override. It defines the specific logic for performing the action. The `CombatContext` provides necessary runtime information (caster, target, etc.).
*   **`GetActionType()` (abstract method):** Returns an `ActionType` enum value, categorizing the action (e.g., Damage, Heal, Meta).

**Flexibility/Rigidity:**
*   **Flexibility:**
    *   **Polymorphism:** Allows for diverse action types to be treated uniformly through the `ActionSO` interface.
    *   **Extensibility:** New action types can be easily added by inheriting from `ActionSO` and implementing the abstract methods.
    *   **Reusability:** Once defined, an `ActionSO` can be used in multiple `AbilitySO`s.
*   **Rigidity:**
    *   **`description` field:** While present, it's a static string. It lacks built-in support for dynamic values (e.g., displaying the actual damage amount for a `DamageActionSO`). This is a significant source of rigidity for generating dynamic tooltip text.
    *   **`CombatContext`:** While generally flexible for combat, if actions need to interact with systems outside of the immediate combat context (e.g., global game state, UI elements not directly tied to combat), the `CombatContext` might need to be expanded or a different mechanism for broader system interaction would be required.
    *   **`ActionType`:** The `ActionType` enum can be limiting if an action has multiple classifications or a more nuanced type.

### 3. Concrete ActionSO Implementations

### 3.1. ApplyEffectActionSO.cs

**File Path:** `Assets/Scripts/Data/Actions/ApplyEffectActionSO.cs`

**Purpose:**
This action applies a specified `EffectSO` to the target within the combat context.

**Key Fields:**
*   **`effectToApply` (EffectSO):** A reference to the `ScriptableObject` defining the effect to be applied.

**`Execute` Logic:**
*   Checks for null `effectToApply` or `ctx.Target`.
*   Calls `ctx.Target.ApplyEffect(effectToApply)`, delegating the actual effect application to the `ShipState` of the target.
*   Logs the action for debugging.

**`GetActionType()`:** Returns `ActionType.Meta`. This indicates that the action itself doesn't directly deal damage or heal, but rather applies an effect which might then have its own ongoing effects.

**Flexibility/Rigidity:**
*   **Flexibility:** Easily applies any defined `EffectSO`.
*   **Rigidity:** The `ActionType.Meta` might be too generic if more specific categorization of meta-actions is needed.

### 3.2. DamageActionSO.cs

**File Path:** `Assets/Scripts/Data/Actions/DamageActionSO.cs`

**Purpose:**
This action deals a specified amount of damage to the target.

**Key Fields:**
*   **`damageAmount` (int):** The integer value of damage to be dealt.

**`Execute` Logic:**
*   Checks for null `ctx.Target`.
*   Calls `ctx.Target.TakeDamage(damageAmount)`, delegating damage calculation and health reduction to the target's `ShipState`.
*   Logs the action for debugging.

**`GetActionType()`:** Returns `ActionType.Damage`.

**Flexibility/Rigidity:**
*   **Flexibility:** Simple and effective for dealing fixed damage.
*   **Rigidity:** The `damageAmount` is a fixed integer. If damage needs to be calculated dynamically (e.g., based on caster stats, target defenses, random range), this class would need to be extended or modified.

### 3.3. HealActionSO.cs

**File Path:** `Assets/Scripts/Data/Actions/HealActionSO.cs`

**Purpose:**
This action heals the caster by a specified amount.

**Key Fields:**
*   **`healAmount` (int):** The integer value of health to be restored.

**`Execute` Logic:**
*   Checks for null `ctx.Caster`.
*   Calls `ctx.Caster.Heal(healAmount)`, delegating healing logic to the caster's `ShipState`.
*   Logs the action for debugging.

**`GetActionType()`:** Returns `ActionType.Heal`. 

**Flexibility/Rigidity:**
*   **Flexibility:** Simple and effective for fixed healing.
*   **Rigidity:** Similar to `DamageActionSO`, `healAmount` is a fixed integer. Dynamic healing calculations would require extensions.

### 4. Overall System Design: Flexibility vs. Rigidity

### Strengths (Flexibility):

*   **Modularity and Reusability:** Both `AbilitySO` and `ActionSO` are `ScriptableObject`s, promoting a highly modular and reusable system. Actions can be combined in various ways to form different abilities, and individual actions can be reused across many abilities.
*   **Extensibility:** Adding new types of abilities (new `TriggerType`s) or new types of actions (new `ActionSO` subclasses) is straightforward and adheres to the Open/Closed Principle.
*   **Data-Driven Design:** The entire system is configured through data assets in the Unity Editor, reducing the need for code changes when designing new abilities or tweaking existing ones. This empowers designers.
*   **Clear Separation of Concerns:** `AbilitySO` defines *what* triggers and *what* actions are performed, while `ActionSO` defines *how* a specific action is executed. This separation makes the system easier to understand, maintain, and extend.

### Weaknesses (Rigidity Points):

*   **Static Action Descriptions:** This is the most significant point of rigidity identified. The `description` field in `ActionSO` is a simple string. It cannot dynamically incorporate runtime values (e.g., `damageAmount`, `healAmount`, `effectToApply.DisplayName`) into the tooltip text. This forces generic descriptions or requires external logic to format them.
    *   **Impact:** Tooltips cannot provide precise numerical information without a separate, more complex system to parse and format these strings.
*   **Fixed Action Parameters:** Concrete `ActionSO`s (e.g., `DamageActionSO`, `HealActionSO`) have fixed parameters (e.g., `damageAmount`, `healAmount`). If an action needs more complex or dynamic parameterization (e.g., damage based on a percentage of health, healing over time), the current structure requires creating new `ActionSO` subclasses for each variation or adding complex logic within the `Execute` method.
*   **`CombatContext` Scope:** While `CombatContext` is appropriate for combat-related actions, if abilities or actions need to interact with systems outside of the immediate combat context (e.g., global game state, UI elements not directly tied to combat), the `CombatContext` might become a "God object" or require a more generic service locator/dependency injection pattern.
*   **`ActionType` Granularity:** The `ActionType` enum (Damage, Heal, Meta) is functional but might lack the granularity for more complex filtering or categorization of actions, especially for `Meta` actions.
*   **No Built-in Description Templating:** There's no inherent mechanism to define a template for the `description` string (e.g., "Deals {0} damage") and then automatically fill in the placeholders from the `ActionSO`'s specific fields.

## Conclusion/Recommendations

To address the identified rigidities, particularly concerning dynamic descriptions and action parameterization, consider the following:

1.  **Introducing Runtime Representations (High Priority):**
    *   **Problem:** `ScriptableObject`s are static data templates. They are not designed to hold mutable, per-instance runtime values (e.g., an item's current damage after buffs).
    *   **Solution:** Implement runtime C# classes (e.g., `RuntimeItem`, `RuntimeAbility`, `RuntimeAction`) that act as wrappers around their corresponding `ScriptableObject` assets.
        *   These runtime objects would be instantiated when an item is generated in the game.
        *   They would hold references to their `ScriptableObject` blueprints for default values and static data.
        *   Crucially, they would contain **mutable fields** for dynamic values (e.g., `CurrentDamageAmount` in a `RuntimeDamageAction`).
        *   Buffs, debuffs, and other runtime modifications would apply to these mutable fields on the runtime instances, not the `ScriptableObject` assets.
    *   **Impact:** This foundational change enables true per-instance item/ability/action state, allowing for dynamic values to be tracked and modified during gameplay.

2.  **Dynamic Description System (High Priority - operates on Runtime Representations):**
    *   **Problem:** The `description` field in `ActionSO` is static and cannot display dynamic values.
    *   **Solution (Option B: Dedicated Description Builder):**
        *   Introduce an interface (e.g., `IDescriptionProvider`) that runtime action classes (e.g., `RuntimeAction`, `RuntimeDamageAction`) would implement.
        *   Each runtime action would have a method (e.g., `BuildDescription(IRuntimeContext context)`) that returns a formatted string based on its *current, dynamic* internal state (e.g., `CurrentDamageAmount`). The `IRuntimeContext` would provide any necessary external data for complex calculations.
        *   The `EffectDisplay` and `TooltipController` would be updated to receive and utilize these runtime action instances (or a context containing them) to call `BuildDescription()` and generate the tooltip text.
    *   **Impact:** Enables precise, dynamic numerical information in tooltips, reflecting current buffs/debuffs, by pulling data from the mutable fields of the runtime action instances.

3.  **Enhanced Action Parameterization (operates on Runtime Representations):**
    *   **Problem:** Fixed parameters in `ActionSO`s limit dynamic calculations.
    *   **Solution:** With runtime action classes, parameters can be more flexible. Instead of directly storing `int damageAmount` on the `DamageActionSO`, the `RuntimeDamageAction` could have a `CurrentDamageAmount` property. The `DamageActionSO` would define the *base* damage, and the `RuntimeDamageAction` would calculate its `CurrentDamageAmount` based on the base value plus any active buffs/debuffs.
    *   **Impact:** Allows for complex and dynamic calculations of action parameters based on runtime conditions, without requiring new `ActionSO` subclasses for every variation.

4.  **Broader Context/Service Access:**
    *   If actions need to interact with systems beyond combat, consider introducing a more generic `GameContext` or a service locator pattern that provides access to various game managers (e.g., `InventoryManager`, `EventManager`). This would prevent `CombatContext` from becoming overly bloated.

5.  **Refined Action Types:**
    *   If needed, expand the `ActionType` enum or introduce a tagging system to allow for more granular categorization of actions.
