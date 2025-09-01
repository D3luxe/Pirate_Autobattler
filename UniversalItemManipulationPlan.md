# Universal Item Manipulation Library Refactoring Plan

**Goal:** To create a universal item manipulation system that centralizes logic, improves reusability, and decouples UI elements from direct game logic, enabling item interaction across various UI elements (PlayerPanel, Shop, Crafting, etc.).

**Core Principles:**
*   **Single Source of Truth:** `GameSession` remains the authoritative source for game state.
*   **Event-Driven:** UI reacts to events dispatched by the `ItemManipulationService`.
*   **Separation of Concerns:** UI elements focus on presentation; services handle logic.
*   **ItemElement as Movable Entity:** `ItemElement`s are created only for existing items and are moved between `SlotElement`s.

---

**Phase 1: Core Services & Events (Foundation)**

1.  **Create `ItemManipulationService.cs`:**
    *   **Location:** `Assets/Scripts/Core/ItemManipulationService.cs`
    *   **Content:**
        *   Singleton pattern (`Instance` property, private constructor).
        *   `Initialize(IGameSession gameSession)` method to inject `GameSession` dependency.
        *   Public methods: `MoveItem(SlotId from, SlotId to)`, `EquipItem(SlotId from, SlotId to)`, `UnequipItem(SlotId from, SlotId to)`. These will contain core logic, validate requests, call `GameSession` methods, and dispatch `ItemManipulationEvents`.
        *   Define `SlotId` struct (int Index, SlotContainerType ContainerType) and `SlotContainerType` enum (Inventory, Equipment, Shop, Crafting, etc.) within its namespace.
    *   **Actionable Steps:**
        *   Create `Assets/Scripts/Core/ItemManipulationService.cs` with the specified content.
        *   Add `SlotId` struct and `SlotContainerType` enum.

2.  **Create `ItemManipulationEvents.cs`:**
    *   **Location:** `Assets/Scripts/Core/ItemManipulationEvents.cs`
    *   **Content:**
        *   Static class.
        *   Public static events: `OnItemMoved`, `OnItemEquipped`, `OnItemUnequipped`, `OnItemAdded`, `OnItemRemoved`.
        *   Public static `Dispatch` methods for each event (e.g., `DispatchItemMoved(ItemInstance item, SlotId from, SlotId to)`).
    *   **Actionable Steps:**
        *   Create `Assets/Scripts/Core/ItemManipulationEvents.cs` with the specified content.

3.  **Initialize `ItemManipulationService`:**
    *   **Location:** `GameInitializer.cs` (or a suitable central initialization point).
    *   **Action:** In `GameInitializer.Start()`, call `ItemManipulationService.Instance.Initialize(GameSession.Instance);` (assuming `GameSession` is also a singleton or accessible).
    *   **Actionable Steps:**
        *   Modify `GameInitializer.cs` to initialize `ItemManipulationService`.

---

**Phase 2: Refactor `SlotManipulator` (Input Layer)**

1.  **Modify `SlotManipulator.cs`:**
    *   **Action:**
        *   **Constructor:** Change to `public SlotManipulator(ItemElement itemElement)`. The `SlotManipulator` will get the `ISlotViewData` from `itemElement.SlotViewData`.
        *   **Fields:** Remove `_slotData` field. `_itemElement.SlotViewData` will provide the context.
        *   **`OnPointerDown`:**
            *   Ensure `_itemElement.SlotViewData.IsEmpty` check is correct.
            *   Make the original `ItemElement` invisible (`_itemElement.style.visibility = Visibility.Hidden;`).
        *   **`OnPointerUp`:**
            *   Remove direct calls to `PlayerPanelEvents.OnSlotDropped`.
            *   Call `ItemManipulationService.Instance.MoveItem(...)`, `EquipItem(...)`, or `UnequipItem(...)` based on the `_fromContainer` and `toContainer`.
            *   Make the `ItemElement` visible again (`_itemElement.style.visibility = Visibility.Visible;`).
            *   **Crucial:** The actual re-parenting of the `ItemElement` will be handled by `PlayerPanelView` reacting to `ItemManipulationEvents`.
        *   **`FindHoveredSlot`:** Ensure it correctly identifies the `SlotElement` and retrieves its `ISlotViewData`.
        *   **`GetSlotContainerType`:** Ensure it correctly identifies the container type of a `VisualElement`.
    *   **Actionable Steps:**
        *   Modify `SlotManipulator.cs` constructor and fields.
        *   Update `OnPointerDown` to hide `_itemElement`.
        *   Update `OnPointerUp` to call `ItemManipulationService` and make `_itemElement` visible.
        *   Review `FindHoveredSlot` and `GetSlotContainerType` for compatibility.

---

**Phase 3: Refactor `PlayerPanelController` & `PlayerPanelDataViewModel` (Reaction Layer)**

1.  **Modify `PlayerPanelController.cs`:**
    *   **Action:**
        *   Remove `PlayerPanelEvents.OnSlotDropped += HandleSlotDropped;` subscription.
        *   Remove the entire `HandleSlotDropped` method.
    *   **Actionable Steps:**
        *   Remove subscription and method from `PlayerPanelController.cs`.

2.  **Modify `PlayerPanelDataViewModel.cs`:**
    *   **Action:**
        *   Remove subscriptions to `_gameSession.PlayerShip.OnEquipmentSwapped`, `OnEquipmentAddedAt`, `OnEquipmentRemovedAt`.
        *   Remove subscriptions to `_gameSession.Inventory.OnItemsSwapped`, `OnItemAddedAt`, `OnItemRemovedAt`.
        *   Remove `HandleEquipmentSwapped`, `HandleEquipmentAddedAt`, `HandleEquipmentRemovedAt`.
        *   Remove `HandleInventorySwapped`, `HandleInventoryAddedAt`, `HandleInventoryRemovedAt`.
        *   Add subscriptions to `ItemManipulationEvents.OnItemMoved`, `OnItemEquipped`, `OnItemUnequipped`, `OnItemAdded`, `OnItemRemoved`.
        *   Implement new handlers for `ItemManipulationEvents` to update `_equipmentSlots` and `_inventorySlots` (the `ObservableList`s). These handlers will update the `CurrentItemInstance` of the `SlotDataViewModel`s in the `ObservableList`s.
    *   **Actionable Steps:**
        *   Remove old subscriptions and handlers from `PlayerPanelDataViewModel.cs`.
        *   Add new subscriptions to `ItemManipulationEvents`.
        *   Implement new event handlers to update `SlotDataViewModel.CurrentItemInstance`.

---

**Phase 4: Refactor `GameSession` Event Dispatch (Decoupling `GameSession`)**

1.  **Modify `Inventory.cs`:**
    *   **Action:**
        *   Change `OnItemsSwapped?.Invoke(indexA, indexB);` to `ItemManipulationEvents.DispatchItemMoved(Slots[indexA].Item, new SlotId(indexA, SlotContainerType.Inventory), new SlotId(indexB, SlotContainerType.Inventory));` (and similar for other events).
        *   Remove `OnItemsSwapped`, `OnItemAddedAt`, `OnItemRemovedAt` event definitions.
    *   **Actionable Steps:**
        *   Update event dispatches in `Inventory.cs`.
        *   Remove old event definitions.

2.  **Modify `ShipState.cs`:**
    *   **Action:**
        *   Change `OnEquipmentSwapped?.Invoke(indexA, indexB);` to `ItemManipulationEvents.DispatchItemMoved(Equipped[indexA], new SlotId(indexA, SlotContainerType.Equipment), new SlotId(indexB, SlotContainerType.Equipment));` (and similar for other events).
        *   Remove `OnEquipmentSwapped`, `OnEquipmentAddedAt`, `OnEquipmentRemovedAt` event definitions.
    *   **Actionable Steps:**
        *   Update event dispatches in `ShipState.cs`.
        *   Remove old event definitions.

---

**Phase 5: UI Element Management (Visual Update Orchestration)**

1.  **Modify `PlayerPanelView.BindSlots` (Initial Population):**
    *   **Action:**
        *   Only create `SlotElement`s.
        *   If `slotData.CurrentItemInstance` is *not* `null`, create an `ItemElement`, bind it, add it to the corresponding `SlotElement`, and attach the `SlotManipulator`.
    *   **Actionable Steps:**
        *   Update initial population loop in `PlayerPanelView.BindSlots`.

2.  **Modify `PlayerPanelView.BindSlots` (`Add` case):**
    *   **Action:** If `newItem.CurrentItemInstance` is *not* `null`, create an `ItemElement`, bind it, add it to the `SlotElement`, and attach the `SlotManipulator`.
    *   **Actionable Steps:**
        *   Update `Add` case in `PlayerPanelView.BindSlots`.

3.  **Modify `PlayerPanelView.BindSlots` (`Remove` case):**
    *   **Action:** When `NotifyCollectionChangedAction.Remove` occurs, find the `ItemElement` in the `SlotElement` and remove it.
    *   **Actionable Steps:**
        *   Update `Remove` case in `PlayerPanelView.BindSlots`.

4.  **Modify `PlayerPanelView.BindSlots` (`Replace` case):**
    *   **Action:** Implement logic to move the `ItemElement` from the `oldSlotElement` to the `newSlotElement`. This is the core of the visual drag-and-drop.
    *   **Actionable Steps:**
        *   Update `Replace` case in `PlayerPanelView.BindSlots`.

5.  **Modify `PlayerPanelView.BindSlots` (`Move` case):**
    *   **Action:** Ensure the `ItemElement` moves with the `SlotElement`. (This should happen automatically as `ItemElement` is a child).
    *   **Actionable Steps:**
        *   Review `Move` case in `PlayerPanelView.BindSlots`.

---

**Phase 6: Final Cleanup & Verification**

1.  **Remove Obsolete Code:**
    *   **Action:** Remove any remaining unused fields, methods, or debug logs from all modified scripts.
    *   **Rationale:** Maintain a clean codebase.

2.  **Testing:**
    *   **Action:** Thoroughly test all item manipulation scenarios (drag-and-drop within inventory, equip, unequip, shop interactions, etc.).
    *   **Rationale:** Ensure the new system is robust and bug-free.

---
