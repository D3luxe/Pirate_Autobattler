# Runtime Item, Ability, and Action System Implementation Plan

This document outlines a concise and actionable plan to introduce runtime representations for items, abilities, and actions. This system will enable dynamic modification of values (e.g., buffs, debuffs) during gameplay and ensure that the UI (specifically tooltips) accurately reflects these changes. This plan also integrates the "Dedicated Description Builder" (Option B) to operate on these new runtime objects.

## Analysis and Reasoning:

The current system relies heavily on `ScriptableObject` assets, which are static data templates. While excellent for defining base properties, they are not designed to hold mutable, per-instance runtime values. To address this, we will introduce a new layer of C# classes that will be instantiated at runtime, encapsulating the `ScriptableObject` blueprints and holding the dynamic state.

## Plan:

### Step 1: Define `IRuntimeContext` (Optional but Recommended)

*   **Purpose:** To provide a flexible way to pass necessary runtime data (e.g., current combat state, player stats) to `BuildDescription` methods without tightly coupling runtime actions to specific game systems.
*   **Action:** Create a new C# interface `IRuntimeContext.cs` in `Assets/Scripts/Combat/`.
*   **Details:** For now, it can be an empty interface or include basic properties if universally needed for descriptions (e.g., `ShipState Caster { get; }`, `ShipState Target { get; }`).

### Step 2: Create Base `RuntimeAction` Class

*   **Purpose:** To serve as the abstract base for all runtime action instances. It will hold a reference to its `ActionSO` blueprint and define the `BuildDescription` method.
*   **Action:** Create `RuntimeAction.cs` in `Assets/Scripts/Runtime/`.
*   **Details:**
    ```csharp
    using PirateRoguelike.Data.Actions;

    namespace PirateRoguelike.Runtime
    {
        public abstract class RuntimeAction
        {
            protected readonly ActionSO BaseActionSO;

            public RuntimeAction(ActionSO baseActionSO)
            {
                BaseActionSO = baseActionSO;
            }

            public abstract string BuildDescription(IRuntimeContext context);
        }
    }
    ```

### Step 3: Create Concrete `RuntimeAction` Implementations

*   **Purpose:** These classes will wrap their respective `ActionSO`s, hold mutable fields for dynamic values, and implement the `BuildDescription` method.
*   **Action:** Create `RuntimeDamageAction.cs`, `RuntimeHealAction.cs`, and `RuntimeApplyEffectAction.cs` in `Assets/Scripts/Runtime/`.
*   **Details (Example for `RuntimeDamageAction`):
    ```csharp
    using PirateRoguelike.Data.Actions;

    namespace PirateRoguelike.Runtime
    {
        public class RuntimeDamageAction : RuntimeAction
        {
            public int CurrentDamageAmount { get; set; }

            public RuntimeDamageAction(DamageActionSO baseActionSO) : base(baseActionSO)
            {
                CurrentDamageAmount = baseActionSO.damageAmount; // Initialize from SO
            }

            public override string BuildDescription(IRuntimeContext context)
            {
                // Example: "Deals {CurrentDamageAmount} damage."
                return $"Deals {CurrentDamageAmount} damage.";
            }
        }
    }
    ```
    *   Similar implementations for `RuntimeHealAction` (with `CurrentHealAmount`) and `RuntimeApplyEffectAction` (which might just reference `BaseActionSO.effectToApply` and its `DisplayName`).

### Step 4: Create Base `RuntimeAbility` Class

*   **Purpose:** To wrap an `AbilitySO` and manage its `RuntimeAction` instances.
*   **Action:** Create `RuntimeAbility.cs` in `Assets/Scripts/Runtime/`.
*   **Details:**
    ```csharp
    using System.Collections.Generic;
    using PirateRoguelike.Data.Abilities;
    using PirateRoguelike.Data.Actions;

    namespace PirateRoguelike.Runtime
    {
        public class RuntimeAbility
        {
            protected readonly AbilitySO BaseAbilitySO;
            public IReadOnlyList<RuntimeAction> Actions { get; private set; }

            public RuntimeAbility(AbilitySO baseAbilitySO)
            {
                BaseAbilitySO = baseAbilitySO;
                var runtimeActions = new List<RuntimeAction>();
                foreach (var actionSO in baseAbilitySO.actions)
                {
                    // This will require a factory or switch statement to create the correct RuntimeAction type
                    if (actionSO is DamageActionSO damageActionSO)
                    {
                        runtimeActions.Add(new RuntimeDamageAction(damageActionSO));
                    }
                    else if (actionSO is HealActionSO healActionSO)
                    {
                        runtimeActions.Add(new new RuntimeHealAction(healActionSO));
                    }
                    else if (actionSO is ApplyEffectActionSO applyEffectActionSO)
                    {
                        runtimeActions.Add(new RuntimeApplyEffectAction(applyEffectActionSO));
                    }
                    // Add more cases for other ActionSO types
                }
                Actions = runtimeActions;
            }

            public string DisplayName => BaseAbilitySO.displayName;
        }
    }
    ```

### Step 5: Create `RuntimeItem` Class

*   **Purpose:** To wrap an `ItemSO` and manage its `RuntimeAbility` instances. This will be the primary object passed to the UI.
*   **Action:** Create `RuntimeItem.cs` in `Assets/Scripts/Runtime/`.
*   **Details:**
    ```csharp
    using System.Collections.Generic;
    using PirateRoguelike.Data.Items;

    namespace PirateRoguelike.Runtime
    {
        public class RuntimeItem
        {
            protected readonly ItemSO BaseItemSO;
            public IReadOnlyList<RuntimeAbility> Abilities { get; private set; }

            public RuntimeItem(ItemSO baseItemSO)
            {
                BaseItemSO = baseItemSO;
                var runtimeAbilities = new List<RuntimeAbility>();
                foreach (var abilitySO in baseItemSO.abilities)
                {
                    runtimeAbilities.Add(new RuntimeAbility(abilitySO));
                }
                Abilities = runtimeAbilities;
            }

            public string DisplayName => BaseItemSO.displayName;
            public int CooldownSec => BaseItemSO.cooldownSec; // Example of passing through static data
            public bool IsActive => BaseItemSO.isActive;
        }
    }
    ```

### Step 6: Modify `TooltipController` and `EffectDisplay`

*   **Purpose:** Update the UI to consume `RuntimeItem` and `RuntimeAbility`/`RuntimeAction` objects instead of `ScriptableObject`s.
*   **Action:** Modify `TooltipController.cs` in `Assets/Scripts/UI/` and `EffectDisplay.cs` in `Assets/Scripts/UI/`.
*   **Details:**
    *   **`TooltipController.cs`:**
        *   Change `Show(ItemSO item, VisualElement targetElement)` to `Show(RuntimeItem runtimeItem, VisualElement targetElement)`.
        *   Update internal logic to use `runtimeItem.DisplayName`, `runtimeItem.CooldownSec`, `runtimeItem.IsActive`.
        *   Pass `RuntimeAbility` to `EffectDisplay.SetData()`.
    *   **`EffectDisplay.cs`:**
        *   Change `SetData(AbilitySO ability)` to `SetData(RuntimeAbility runtimeAbility, IRuntimeContext context)`.
        *   Update logic to iterate `runtimeAbility.Actions` and call `action.BuildDescription(context)`.

### Step 7: Update Item Generation/Management

*   **Purpose:** Ensure that `RuntimeItem` instances are created and managed when items are generated in the game.
*   **Action:** Identify and modify existing code that currently uses `ItemSO` to represent items in the game (e.g., inventory, shop, player equipment). This will involve changes in files like `PlayerPanelView.cs`, `ShopItemViewUI.cs`, and any other systems that manage items.
    *   Wherever an `ItemSO` is currently used to represent an item instance, it should be replaced with a `RuntimeItem` instance.
    *   When an `ItemSO` is loaded or created, a corresponding `RuntimeItem` should be instantiated from it.

### Step 8: Verification

*   **Action:** Create a simple test scene or modify an existing one to instantiate a `RuntimeItem`.
*   **Action:** Apply a temporary buff to one of its `RuntimeAction`'s dynamic values (e.g., `CurrentDamageAmount` in a `RuntimeDamageAction`).
*   **Action:** Trigger the tooltip for this `RuntimeItem`.
*   **Expected Outcome:** The tooltip should display the buffed value, confirming that dynamic values are correctly reflected.

## Assessment:

This plan outlines a significant architectural refactor that introduces a new layer of runtime objects. While it requires a fair amount of code changes across several files, it is a standard and robust pattern for handling dynamic game data in Unity. It clearly separates static data (ScriptableObjects) from mutable runtime state, which will greatly improve the flexibility, testability, and maintainability of the ability and item systems. The "Dedicated Description Builder" will then operate on these runtime objects, allowing for dynamic and accurate tooltip descriptions that reflect the current game state.
