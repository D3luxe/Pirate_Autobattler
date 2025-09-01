This plan has been successfully implemented. The final architecture is described in the **UI Interaction System** documentation (`docs/ui-interaction-system.md`).

# Universal UI Interaction System: Detailed Implementation Plan

## 1. Analysis and Evidence

This plan is based on a completed Trace and Verify investigation of the codebase. The findings are as follows:

*   **Tooltip System:** The trace (`EnemyPanelController.cs:141` -> `TooltipUtility.cs:13` -> `TooltipController.cs:Show`) confirms that the `TooltipController` is initialized with a single root UI element (`TooltipController.cs:51`) and adds its tooltip UXML directly to that root (`TooltipController.cs:60`). This is the verifiable cause of the bug preventing tooltips from appearing on other UI documents (e.g., the battle scene UI).

*   **Item Manipulation System:** The trace (`SlotManipulator.cs:111` -> `ItemManipulationService.cs:31`) confirms that the UI directly calls `ItemManipulationService.SwapItems`, which contains no validation logic. This confirms the need for a rule-based gatekeeper to prevent illegal actions.

*   **Combat State:** The trace of `CombatController.cs` confirms that `Init` (line 30) and `CheckBattleEndConditions` (line 160) are the definitive start and end points of combat, providing clear hooks for managing a global `IsInCombat` state.

## 2. Detailed Implementation Steps

### Part 1: Universal Tooltip System

**Goal:** Refactor the `TooltipController` to be context-aware and render on any specified UI panel.

*   **Step 1.1: Modify `TooltipController.cs`**
    *   **File:** `Assets/Scripts/UI/TooltipController.cs`
    *   **Action:** Change the `Show` method signature.
    *   **From:** `public void Show(RuntimeItem runtimeItem, VisualElement targetElement)`
    *   **To:** `public void Show(RuntimeItem runtimeItem, VisualElement targetElement, VisualElement panelRoot)`
    *   **Action:** Update the `ShowCoroutine` to re-parent the tooltip container.
    *   **Code (in `ShowCoroutine`):**
        ```csharp
        // At the top of the coroutine, before positioning is calculated:
        if (_tooltipContainer.parent != panelRoot)
        {
            _tooltipContainer.RemoveFromHierarchy();
            panelRoot.Add(_tooltipContainer);
        }
        ```

*   **Step 1.2: Modify `TooltipUtility.cs`**
    *   **File:** `Assets/Scripts/UI/Utilities/TooltipUtility.cs`
    *   **Action:** Change the `RegisterTooltipCallbacks` method signature to accept the panel root.
    *   **From:** `public static void RegisterTooltipCallbacks(VisualElement element, ISlotViewData slotData)`
    *   **To:** `public static void RegisterTooltipCallbacks(VisualElement element, ISlotViewData slotData, VisualElement panelRoot)`
    *   **Action:** Update the call to `TooltipController.Show` inside the `PointerEnterEvent` callback.
    *   **From:** `TooltipController.Instance.Show(slotData.ItemData, element);`
    *   **To:** `TooltipController.Instance.Show(slotData.ItemData, element, panelRoot);`

*   **Step 1.3: Update `EnemyPanelController.cs`**
    *   **File:** `Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs`
    *   **Action:** Update the call to `RegisterTooltipCallbacks` inside the `CreateSlotElement` method.
    *   **From:** `TooltipUtility.RegisterTooltipCallbacks(slotElement, slotData);`
    *   **To:** `TooltipUtility.RegisterTooltipCallbacks(slotElement, slotData, this.GetComponent<UIDocument>().rootVisualElement);`

*   **Step 1.4: Update Player Panel**
    *   **File:** `Assets/Scripts/UI/PlayerPanel/PlayerPanelView.cs` (or wherever the player's slots are created and callbacks registered).
    *   **Action:** Similar to the enemy panel, update the call to `TooltipUtility.RegisterTooltipCallbacks` to pass its own root visual element.

### Part 2: Context-Aware Item Manipulation

**Goal:** Implement a rule-based system to govern item manipulation, fulfilling the shop and combat-lockdown requirements.

*   **Step 2.1: Create `UIInteractionService.cs`**
    *   **Action:** Create a new file at `Assets/Scripts/UI/UIInteractionService.cs`.
    *   **Content:**
        ```csharp
        using PirateRoguelike.Services;

        public static class UIInteractionService
        {
            public static bool IsInCombat { get; set; } = false;

            public static bool CanManipulateItem(SlotContainerType containerType)
            {
                if (IsInCombat) return false;

                return containerType == SlotContainerType.Inventory || containerType == SlotContainerType.Equipment;
            }
        }
        ```

*   **Step 2.2: Integrate with `CombatController.cs`**
    *   **File:** `Assets/Scripts/Combat/CombatController.cs`
    *   **Action:** Add `using PirateRoguelike.UI;` to the top.
    *   **Action:** In the `Init` method, add the line: `UIInteractionService.IsInCombat = true;`
    *   **Action:** In the `CheckBattleEndConditions` method, inside the `if (playerDefeated || enemyDefeated)` block, add the line: `UIInteractionService.IsInCombat = false;`

*   **Step 2.3: Refactor `ItemManipulationService.cs`**
    *   **File:** `Assets/Scripts/Core/ItemManipulationService.cs`
    *   **Action:** Add `using PirateRoguelike.UI;` to the top.
    *   **Action:** Rename the existing `SwapItems` method to `private void ExecuteSwap(SlotId slotA, SlotId slotB)`.
    *   **Action:** Create a new public `RequestSwap` method.
        ```csharp
        public void RequestSwap(SlotId fromSlot, SlotId toSlot)
        {
            if (!UIInteractionService.CanManipulateItem(fromSlot.ContainerType) || !UIInteractionService.CanManipulateItem(toSlot.ContainerType))
            {
                return; // Rule check failed
            }
            ExecuteSwap(fromSlot, toSlot);
        }
        ```
    *   **Action:** Create the `RequestPurchase` stub.
        ```csharp
        public void RequestPurchase(SlotId shopSlot, SlotId playerSlot)
        {
            // TODO: Implement Shop Logic
            // 1. Check GameSession.IsInShop state via UIInteractionService
            // 2. Check player gold
            // 3. Check for valid empty slot if playerSlot is null
            // 4. Execute move and deduct gold
            UnityEngine.Debug.Log($"Purchase requested for item in shop slot {shopSlot.Index}.");
        }
        ```

*   **Step 2.4: Update `SlotManipulator.cs`**
    *   **File:** `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
    *   **Action:** In `OnPointerDown`, add the gatekeeper check at the beginning.
        ```csharp
        // At top of OnPointerDown
        if (!UIInteractionService.CanManipulateItem(_fromContainer))
        {
            return;
        }
        ```
    *   **Action:** In `OnPointerUp`, replace the existing call to `SwapItems` with the new request-based logic.
        ```csharp
        // Replace the existing if/else chain for manipulation
        if (_fromContainer == SlotContainerType.Inventory || _fromContainer == SlotContainerType.Equipment)
        {
             ItemManipulationService.Instance.RequestSwap(fromSlotId, toSlotId);
        }
        // else if (_fromContainer == SlotContainerType.Shop) { ... }
        ```

*   **Step 2.5: Create Placeholder `ShopController.cs`**
    *   **Action:** Create a new file at `Assets/Scripts/UI/ShopController.cs`.
    *   **Content:** Add basic MonoBehaviour structure and a comment block outlining where to add the click-to-purchase logic as described in the plan.
