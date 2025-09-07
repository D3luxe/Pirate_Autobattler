using UnityEngine;
using PirateRoguelike.Services;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using PirateRoguelike.UI;

namespace PirateRoguelike.Commands
{
    public class ClaimRewardItemCommand : ICommand
    {
        private readonly SlotId _sourceSlot;
        private readonly SlotId _destinationSlot; // Can be -1 for auto-find
        private readonly ItemSO _itemToClaim;
        private SlotId _finalDestinationSlot; // Determined during CanExecute

        public ClaimRewardItemCommand(SlotId sourceSlot, SlotId destinationSlot, ItemSO itemToClaim)
        {
            _sourceSlot = sourceSlot;
            _destinationSlot = destinationSlot;
            _itemToClaim = itemToClaim;
        }

        public bool CanExecute()
        {
            if (UIStateService.IsConsoleOpen) return false;
            if (!UIInteractionService.CanManipulateItem(SlotContainerType.Reward)) return false;
            if (_itemToClaim == null)
            {
                Debug.LogError("Item to claim is null.");
                return false;
            }

            // Slot finding logic (moved from ItemManipulationService.RequestClaimReward)
            _finalDestinationSlot = _destinationSlot;
            if (_destinationSlot.Index == -1 || (_destinationSlot.ContainerType == SlotContainerType.Inventory && GameSession.Inventory.IsSlotOccupied(_destinationSlot.Index)) || (_destinationSlot.ContainerType == SlotContainerType.Equipment && GameSession.PlayerShip.IsEquipmentSlotOccupied(_destinationSlot.Index)))
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
                        Debug.LogWarning($"No available slots for {_itemToClaim.displayName}.");
                        return false;
                    }
                }
            }
            return true;
        }

        public void Execute()
        {
            bool claimSuccessful = ItemManipulationService.Instance.PerformClaimReward(_itemToClaim, _finalDestinationSlot, _sourceSlot);

            if (claimSuccessful)
            {
                Debug.Log($"Successfully claimed {_itemToClaim.displayName} and placed in {_finalDestinationSlot.ContainerType} slot {_finalDestinationSlot.Index}.");
            }
            else
            {
                Debug.LogError($"Failed to add {_itemToClaim.displayName} to slot {_finalDestinationSlot.Index}.");
            }
        }
    }
}