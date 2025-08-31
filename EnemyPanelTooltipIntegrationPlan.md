# Plan: Integrate Tooltip Functionality into EnemyPanelController

This plan outlines the steps to integrate the item tooltip functionality into the `EnemyPanelController.cs`, ensuring that tooltips appear for enemy-equipped items.

## 1. Add Required Fields to `EnemyPanelController.cs`

*   Add a `[SerializeField] private VisualTreeAsset _slotTemplate;` field to allow assigning the UXML template for item slots in the Unity Editor.
*   Add a `[SerializeField] private PlayerUIThemeSO _theme;` field to allow assigning the UI theme (for slot visuals like rarity frames) in the Unity Editor.

## 2. Modify `HandleEquipmentChanged` Method in `EnemyPanelController.cs`

*   Iterate through each `RuntimeItem` in `_enemyShipState.Equipped`.
*   For each `itemInstance`:
    *   Instantiate the `_slotTemplate` to create a new `VisualElement` for the slot.
    *   Query for the specific `slotElement` within the instantiated template (e.g., `slotInstance.Q<VisualElement>("slot")`).
    *   Create an `ISlotViewData` instance (e.g., using `SlotDataViewModel`) from the `itemInstance`.
    *   Call the new `BindSlot` method (see step 3) to populate the `slotElement` with the `ISlotViewData`.
    *   Register `PointerEnterEvent` and `PointerLeaveEvent` callbacks on the `slotElement`.
    *   Inside the `PointerEnterEvent` callback, call `TooltipController.Instance.Show(itemInstance, slotElement)`.
    *   Inside the `PointerLeaveEvent` callback, call `TooltipController.Instance.Hide()`.
    *   Add the `slotElement` to the `_equipmentBar` `VisualElement`.

## 3. Add `BindSlot` Method to `EnemyPanelController.cs`

*   Implement a `private void BindSlot(VisualElement slotElement, ISlotViewData slotData)` method.
*   This method will be responsible for setting the visual properties of the `slotElement` (e.g., icon, rarity frame, cooldown overlay) based on the provided `ISlotViewData`. This logic will be adapted from the `BindSlot` method previously found in `EnemyPanelView.cs`.

## 4. Verify `SlotDataViewModel` Accessibility

*   Ensure that `SlotDataViewModel` (or an equivalent class that implements `ISlotViewData` and correctly wraps `RuntimeItem`) is accessible within `EnemyPanelController.cs`. If not, ensure it's in a public namespace or move its definition.
