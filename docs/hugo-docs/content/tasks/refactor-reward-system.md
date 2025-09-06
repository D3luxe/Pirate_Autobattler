---
title: "Refactor Reward System Task"
weight: 10
system: ["ui", "core", "data"]
types: ["task", "plan", "refactoring"]
tags: ["RewardUIController", "ItemGenerationService", "ItemManipulationService", "UI Toolkit", "Refactoring", "Legacy UI"]
stage: "Planned"
---

### Objective

To refactor the legacy uGUI-based `RewardUIController` to a modern UI Toolkit implementation. This involves creating a new, shared `ItemGenerationService` to handle the creation of random item lists for both the Shop and the new Reward UI. The new Reward UI must support both click-to-claim and drag-to-claim interactions, reusing the existing `SlotManipulator` pattern.

### Assumptions

- The `SlotElement` and `SlotManipulator` components are robust and can be reused for the Reward UI context.
- The `GameSession` provides all necessary data (e.g., `Inventory`, `Economy`) for processing a reward claim.
- A `RewardService` will be created to manage the business logic of generating and claiming rewards, acting as an intermediary between the `RunManager` and the `RewardUIController`.

### Analysis

- The trace of `RewardUIController.cs` confirms it is a legacy uGUI component that needs to be deprecated.
- The trace of `ShopManager.cs` confirms its item generation logic is complex, will be duplicated by a reward system, and is a prime candidate for refactoring into a shared service.
- The trace of `SlotManipulator.cs` handles both click and drag gestures by checking an `IsDragging` flag and the source `SlotContainerType`. This pattern can be cleanly extended to support a new `Reward` container type.
- The trace of `ItemManipulationService.cs` confirms it is the central hub for item movement logic and can be extended with a `RequestClaimReward` method.

### Planned steps

**Part 1: Modular Item Generation**

1.  **Create `ItemGenerationService.cs`**
    *   **Action:** Create a new static class `ItemGenerationService`.
    *   **Logic:** Create a public static method `GenerateRandomItems(int count, int floorIndex, bool isElite)`. Move the item generation loop (including rarity selection and duplicate prevention) from `ShopManager.GenerateShopItems` into this new method.

2.  **Refactor `ShopManager.cs`**
    *   **Action:** Modify `ShopManager.GenerateShopItems` to delete the old loop and replace it with a single call to `ItemGenerationService.GenerateRandomItems(...)`.

**Part 2: New Reward System Architecture**

1.  **Create `RewardService.cs`**
    *   **Action:** Create a new service to manage the state and logic of rewards.
    *   **Logic:** It will have methods like `GenerateBattleReward(int floor, bool isElite)` which will use the `ItemGenerationService` to get item choices. It will hold the list of choices and the gold amount, and expose them to the UI layer via an event (e.g., `OnRewardsAvailable`).

2.  **Create `RewardUIController.cs` and UXML/USS**
    *   **Action:** Create a new `RewardUI.uxml` file for the layout and a `RewardUI.uss` for styling.
    *   **Action:** Create a new `MonoBehaviour` script, `RewardUIController.cs`, to manage the new UXML.
    *   **Logic:** This controller will subscribe to the `RewardService.OnRewardsAvailable` event. When triggered, it will populate the UI with the reward data. It will be responsible for attaching the interaction logic (click/drag) to the reward slots.

**Part 3: Implement Click/Drag-to-Claim**

1.  **Extend `SlotContainerType`**
    *   **File:** `Assets/Scripts/Core/ItemManipulationService.cs`
    *   **Action:** Add `Reward` to the `SlotContainerType` enum.

2.  **Extend `ItemManipulationService.cs`**
    *   **Action:** Add a new public method `RequestClaimReward(SlotId sourceSlot, SlotId destinationSlot)`.
    *   **Logic:** This method will verify the source is a `Reward` slot and the destination is valid (`Inventory` or `Equipment`). It will then call `GameSession.Inventory.AddItem` (checking for space first) without any gold cost. It will notify the `RewardService` that the reward has been claimed.

3.  **Extend `SlotManipulator.cs`**
    *   **File:** `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
    *   **Method:** `OnPointerUp`
    *   **Action:** Add `else if` conditions to handle the `SlotContainerType.Reward`. For both click (`!IsDragging`) and drag (`IsDragging`) gestures, the new logic will call `ItemManipulationService.Instance.RequestClaimReward(...)`.
