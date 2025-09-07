using System;
using UnityEngine;
using PirateRoguelike.Data;
using PirateRoguelike.Core; // Assuming GameSession is in Core
using System.Linq; // For LINQ operations like FirstOrDefault, Any
using PirateRoguelike.Events; // Added

namespace PirateRoguelike.Services
{
    public class ItemManipulationService
    {
        private static ItemManipulationService _instance;
        public static ItemManipulationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ItemManipulationService();
                }
                return _instance;
            }
        }

        private IGameSession _gameSession; // Dependency

        private ItemManipulationService() { } // Private constructor for singleton

        public void Initialize(IGameSession gameSession)
        {
            _gameSession = gameSession;
        }

        public void RequestSwap(SlotId fromSlot, SlotId toSlot)
        {
            // Check rules before executing the swap
            if (!PirateRoguelike.UI.UIInteractionService.CanManipulateItem(fromSlot.ContainerType) || !PirateRoguelike.UI.UIInteractionService.CanManipulateItem(toSlot.ContainerType))
            {
                return;
            }
            ExecuteSwap(fromSlot, toSlot);
        }

        public void RequestPurchase(SlotId shopSlot, SlotId playerSlot)
        {
            // Ensure it's a shop item being purchased
            if (shopSlot.ContainerType != SlotContainerType.Shop)
            {
                Debug.LogWarning($"RequestPurchase called with non-shop slot type: {shopSlot.ContainerType}");
                return;
            }

            // Get the ItemSO from the shop (assuming ShopManager exposes it)
            // This requires ShopManager to expose its current shop items by index or ID
            // For now, let's assume we can get the ItemSO directly from the shopSlot.Index
            // This is a temporary coupling, ideally ShopManager would provide the item.
            // Let's assume ShopManager.Instance.GetShopItem(shopSlot.Index) exists.
            ItemSO itemToPurchase = null;
            if (PirateRoguelike.Encounters.ShopManager.Instance != null)
            {
                itemToPurchase = PirateRoguelike.Encounters.ShopManager.Instance.GetShopItem(shopSlot.Index);
            }

            if (itemToPurchase == null)
            {
                Debug.LogError($"Could not find item in shop at index {shopSlot.Index} for purchase.");
                PirateRoguelike.Encounters.ShopManager.Instance?.DisplayMessage("Item not available!");
                return;
            }

            // Check if player has enough gold
            if (!_gameSession.Economy.TrySpendGold(itemToPurchase.Cost))
            {
                Debug.LogWarning($"Not enough gold to purchase {itemToPurchase.displayName}. Cost: {itemToPurchase.Cost}, Gold: {_gameSession.Economy.Gold}");
                PirateRoguelike.Encounters.ShopManager.Instance?.DisplayMessage("Not enough gold!");
                return;
            }

            // Determine target slot for the item
            SlotId targetFinalSlot = playerSlot;

            // If playerSlot is -1 (find first available) or target slot is occupied (for drag-to-occupied)
            if (playerSlot.Index == -1 || (playerSlot.ContainerType == SlotContainerType.Inventory && _gameSession.Inventory.IsSlotOccupied(playerSlot.Index)) || (playerSlot.ContainerType == SlotContainerType.Equipment && _gameSession.PlayerShip.IsEquipmentSlotOccupied(playerSlot.Index)))
            {
                // Try to find first available inventory slot
                int availableInventorySlot = _gameSession.Inventory.GetFirstEmptySlot();
                if (availableInventorySlot != -1)
                {
                    targetFinalSlot = new SlotId(availableInventorySlot, SlotContainerType.Inventory);
                }
                else
                {
                    // If no inventory slot, try to find first available equipment slot
                    int availableEquipmentSlot = _gameSession.PlayerShip.GetFirstEmptyEquipmentSlot();
                    if (availableEquipmentSlot != -1)
                    {
                        targetFinalSlot = new SlotId(availableEquipmentSlot, SlotContainerType.Equipment);
                    }
                    else
                    {
                        // No available slots, refund gold and display message
                        _gameSession.Economy.AddGold(itemToPurchase.Cost);
                        Debug.LogWarning($"No available slots for {itemToPurchase.displayName}. Gold refunded.");
                        PirateRoguelike.Encounters.ShopManager.Instance?.DisplayMessage("Inventory full!");
                        return;
                    }
                }
            }

            // Add item to the determined target slot
            bool purchaseSuccessful = false;
            if (targetFinalSlot.ContainerType == SlotContainerType.Inventory)
            {
                purchaseSuccessful = _gameSession.Inventory.AddItemAt(new ItemInstance(itemToPurchase), targetFinalSlot.Index);
            }
            else if (targetFinalSlot.ContainerType == SlotContainerType.Equipment)
            {
                purchaseSuccessful = _gameSession.PlayerShip.SetEquipment(targetFinalSlot.Index, new ItemInstance(itemToPurchase));
            }

            if (purchaseSuccessful)
            {
                // Remove item from shop (ShopManager needs to handle this)
                PirateRoguelike.Encounters.ShopManager.Instance?.RemoveShopItem(shopSlot.Index);
                Debug.Log($"Successfully purchased {itemToPurchase.displayName} for {itemToPurchase.Cost} gold and placed in {targetFinalSlot.ContainerType} slot {targetFinalSlot.Index}.");
                PirateRoguelike.Encounters.ShopManager.Instance?.DisplayMessage($"Purchased {itemToPurchase.displayName}!");
            }
            else
            {
                // Should not happen if slot finding logic is correct, but as a fallback
                _gameSession.Economy.AddGold(itemToPurchase.Cost);
                Debug.LogError($"Failed to add {itemToPurchase.displayName} to slot {targetFinalSlot.Index}. Gold refunded.");
                PirateRoguelike.Encounters.ShopManager.Instance?.DisplayMessage("Purchase failed!");
            }
        }

        public void RequestClaimReward(SlotId sourceSlot, SlotId destinationSlot, ItemSO itemToClaimFromSource)
        {
            if (sourceSlot.ContainerType != SlotContainerType.Reward)
            {
                Debug.LogWarning($"RequestClaimReward called with non-reward slot type: {sourceSlot.ContainerType}");
                return;
            }

            // Get the item from the RewardService (now passed directly)
            ItemSO itemToClaim = itemToClaimFromSource;

            if (itemToClaim == null)
            {
                Debug.LogError($"Item to claim is null.");
                return;
            }

            // Determine target slot for the item
            SlotId targetFinalSlot = destinationSlot;

            // If destinationSlot is -1 (find first available) or target slot is occupied (for drag-to-occupied)
            if (destinationSlot.Index == -1 || (destinationSlot.ContainerType == SlotContainerType.Inventory && _gameSession.Inventory.IsSlotOccupied(destinationSlot.Index)) || (destinationSlot.ContainerType == SlotContainerType.Equipment && _gameSession.PlayerShip.IsEquipmentSlotOccupied(destinationSlot.Index)))
            {
                // Try to find first available inventory slot
                int availableInventorySlot = _gameSession.Inventory.GetFirstEmptySlot();
                if (availableInventorySlot != -1)
                {
                    targetFinalSlot = new SlotId(availableInventorySlot, SlotContainerType.Inventory);
                }
                else
                {
                    // If no inventory slot, try to find first available equipment slot
                    int availableEquipmentSlot = _gameSession.PlayerShip.GetFirstEmptyEquipmentSlot();
                    if (availableEquipmentSlot != -1)
                    {
                        targetFinalSlot = new SlotId(availableEquipmentSlot, SlotContainerType.Equipment);
                    }
                    else
                    {
                        Debug.LogWarning($"No available slots for {itemToClaim.displayName}.");
                        return;
                    }
                }
            }

            // Add item to the determined target slot
            bool claimSuccessful = false;
            if (targetFinalSlot.ContainerType == SlotContainerType.Inventory)
            {
                claimSuccessful = _gameSession.Inventory.AddItemAt(new ItemInstance(itemToClaim), targetFinalSlot.Index);
            }
            else if (targetFinalSlot.ContainerType == SlotContainerType.Equipment)
            {
                claimSuccessful = _gameSession.PlayerShip.SetEquipment(targetFinalSlot.Index, new ItemInstance(itemToClaim));
            }

            if (claimSuccessful)
            {
                Debug.Log($"Successfully claimed {itemToClaim.displayName} and placed in {targetFinalSlot.ContainerType} slot {targetFinalSlot.Index}.");
                RewardService.RemoveClaimedItem(itemToClaim); // Notify RewardService that item has been claimed
                ItemManipulationEvents.DispatchRewardItemClaimed(sourceSlot.Index); // Invoke the event
            }
            else
            {
                Debug.LogError($"Failed to add {itemToClaim.displayName} to slot {targetFinalSlot.Index}.");
            }
        }

        private void ExecuteSwap(SlotId slotA, SlotId slotB)
        {
            ItemInstance itemA = null;
            ItemInstance itemB = null;

            // Get itemA from slotA and remove it
            if (slotA.ContainerType == SlotContainerType.Inventory)
            {
                itemA = _gameSession.Inventory.GetItemAt(slotA.Index);
                _gameSession.Inventory.RemoveItemAt(slotA.Index);
            }
            else if (slotA.ContainerType == SlotContainerType.Equipment)
            {
                itemA = _gameSession.PlayerShip.GetEquippedItem(slotA.Index);
                _gameSession.PlayerShip.RemoveEquippedAt(slotA.Index);
            }

            // Get itemB from slotB and remove it
            if (slotB.ContainerType == SlotContainerType.Inventory)
            {
                itemB = _gameSession.Inventory.GetItemAt(slotB.Index);
                _gameSession.Inventory.RemoveItemAt(slotB.Index);
            }
            else if (slotB.ContainerType == SlotContainerType.Equipment)
            {
                itemB = _gameSession.PlayerShip.GetEquippedItem(slotB.Index);
                _gameSession.PlayerShip.RemoveEquippedAt(slotB.Index);
            }

            // Place itemB into slotA
            if (itemB != null)
            {
                if (slotA.ContainerType == SlotContainerType.Inventory)
                {
                    _gameSession.Inventory.AddItemAt(itemB, slotA.Index);
                }
                else if (slotA.ContainerType == SlotContainerType.Equipment)
                {
                    _gameSession.PlayerShip.SetEquipment(slotA.Index, itemB);
                }
            }

            // Place itemA into slotB
            if (itemA != null)
            {
                if (slotB.ContainerType == SlotContainerType.Inventory)
                {
                    _gameSession.Inventory.AddItemAt(itemA, slotB.Index);
                }
                else if (slotB.ContainerType == SlotContainerType.Equipment)
                {
                    _gameSession.PlayerShip.SetEquipment(slotB.Index, itemA);
                }
            }
        }

    }

    // Define SlotId struct
    public struct SlotId
    {
        public int Index;
        public SlotContainerType ContainerType;

        public SlotId(int index, SlotContainerType containerType)
        {
            Index = index;
            ContainerType = containerType;
        }
    }

    public enum SlotContainerType
    {
        Inventory,
        Equipment,
        Shop,
        Crafting,
        Reward
    }
}