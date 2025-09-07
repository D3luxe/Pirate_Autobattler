using UnityEngine;
using PirateRoguelike.Services;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using PirateRoguelike.UI;
using PirateRoguelike.Encounters; // For ShopManager

namespace PirateRoguelike.Commands
{
    public class PurchaseItemCommand : ICommand
    {
        private readonly SlotId _shopSlot;
        private readonly SlotId _playerTargetSlot; // Can be -1 for auto-find
        private readonly ItemSO _itemToPurchase;
        private SlotId _finalDestinationSlot; // Determined during CanExecute

        public PurchaseItemCommand(SlotId shopSlot, SlotId playerTargetSlot, ItemSO itemToPurchase)
        {
            _shopSlot = shopSlot;
            _playerTargetSlot = playerTargetSlot;
            _itemToPurchase = itemToPurchase;
        }

        public bool CanExecute()
        {
            if (UIStateService.IsConsoleOpen) return false;
            if (!UIInteractionService.CanManipulateItem(SlotContainerType.Shop)) return false;
            if (_itemToPurchase == null)
            {
                Debug.LogError("Item to purchase is null.");
                ShopManager.Instance?.DisplayMessage("Item not available!");
                return false;
            }
            if (!GameSession.Economy.CanAfford(_itemToPurchase.Cost))
            {
                Debug.LogWarning($"Not enough gold to purchase {_itemToPurchase.displayName}. Cost: {_itemToPurchase.Cost}, Gold: {GameSession.Economy.Gold}");
                ShopManager.Instance?.DisplayMessage("Not enough gold!");
                return false;
            }

            // Slot finding logic (moved from ItemManipulationService.RequestPurchase)
            _finalDestinationSlot = _playerTargetSlot;
            if (_playerTargetSlot.Index == -1 || (_playerTargetSlot.ContainerType == SlotContainerType.Inventory && GameSession.Inventory.IsSlotOccupied(_playerTargetSlot.Index)) || (_playerTargetSlot.ContainerType == SlotContainerType.Equipment && GameSession.PlayerShip.IsEquipmentSlotOccupied(_playerTargetSlot.Index)))
            {
                int availableInventorySlot = GameSession.Inventory.GetFirstEmptySlot();
                if (availableInventorySlot != -1)
                {
                    _finalDestinationSlot = new SlotId(availableInventorySlot, SlotContainerType.Inventory);
                }
                else
                {
                    int availableEquipmentSlot = GameSession.PlayerShip.GetFirstEmptyEquipmentSlot();
                    if (availableEquipmentSlot != -1)
                    {
                        _finalDestinationSlot = new SlotId(availableEquipmentSlot, SlotContainerType.Equipment);
                    }
                    else
                    {
                        Debug.LogWarning($"No available slots for {_itemToPurchase.displayName}.");
                        ShopManager.Instance?.DisplayMessage("Inventory full!");
                        return false;
                    }
                }
            }
            return true;
        }

        public void Execute()
        {
            GameSession.Economy.SpendGold(_itemToPurchase.Cost);

            bool purchaseSuccessful = ItemManipulationService.Instance.PerformPurchase(_itemToPurchase, _finalDestinationSlot, _shopSlot);

            if (purchaseSuccessful)
            {
                Debug.Log($"Successfully purchased {_itemToPurchase.displayName} for {_itemToPurchase.Cost} gold and placed in {_finalDestinationSlot.ContainerType} slot {_finalDestinationSlot.Index}.");
                ShopManager.Instance?.DisplayMessage($"Purchased {_itemToPurchase.displayName}!");
            }
            else
            {
                GameSession.Economy.AddGold(_itemToPurchase.Cost); // Refund gold
                Debug.LogError($"Failed to add {_itemToPurchase.displayName} to slot {_finalDestinationSlot.Index}. Gold refunded.");
                ShopManager.Instance?.DisplayMessage("Purchase failed!");
            }
        }
    }
}