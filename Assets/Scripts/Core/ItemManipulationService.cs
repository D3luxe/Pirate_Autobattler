using System;
using UnityEngine;
using PirateRoguelike.Data;
using PirateRoguelike.Core; // Assuming GameSession is in Core

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
            // TODO: Implement Shop Logic
            UnityEngine.Debug.Log($"Purchase requested for item in shop slot {shopSlot.Index}.");
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
        Crafting
    }
}
