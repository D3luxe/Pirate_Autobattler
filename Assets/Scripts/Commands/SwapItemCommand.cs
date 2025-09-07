using UnityEngine;
using PirateRoguelike.Services;
using PirateRoguelike.Core;
using PirateRoguelike.UI;

namespace PirateRoguelike.Commands
{
    public class SwapItemCommand : ICommand
    {
        private readonly SlotId _fromSlot;
        private readonly SlotId _toSlot;

        public SwapItemCommand(SlotId fromSlot, SlotId toSlot)
        {
            _fromSlot = fromSlot;
            _toSlot = toSlot;
        }

        public bool CanExecute()
        {
            if (UIStateService.IsConsoleOpen) return false;
            if (!UIInteractionService.CanManipulateItem(_fromSlot.ContainerType) || !UIInteractionService.CanManipulateItem(_toSlot.ContainerType))
            {
                Debug.LogWarning($"Cannot swap items. Manipulation not allowed for container types: {_fromSlot.ContainerType}, {_toSlot.ContainerType}");
                return false;
            }
            // Add any other specific swap validation here if needed
            return true;
        }

        public void Execute()
        {
            ItemManipulationService.Instance.PerformSwap(_fromSlot, _toSlot);
            Debug.Log($"Successfully swapped item from {_fromSlot.ContainerType} slot {_fromSlot.Index} to {_toSlot.ContainerType} slot {_toSlot.Index}.");
        }
    }
}