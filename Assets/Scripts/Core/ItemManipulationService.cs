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
            Debug.Log("ItemManipulationService initialized.");
        }

        public void MoveItem(SlotId from, SlotId to, ItemInstance swapItem = null)
        {
            Debug.Log($"ItemManipulationService: MoveItem from {from.ContainerType} {from.Index} to {to.ContainerType} {to.Index}");

            ItemInstance itemToMove = null;

            if(swapItem != null)
            {
                Debug.Log($"ItemManipulationService: swapItem: {swapItem.Def.displayName}");
                if(to.ContainerType == SlotContainerType.Inventory)
                {
                   _gameSession.Inventory.RemoveItemAt(to.Index);
                   Debug.Log($"  Removed item {swapItem?.Def.displayName ?? "NULL"} from Inventory {to.Index} to prepare for swap");
                } else if(to.ContainerType == SlotContainerType.Equipment)
                {
                   _gameSession.PlayerShip.RemoveEquippedAt(to.Index);
                   Debug.Log($"  Removed item {swapItem?.Def.displayName ?? "NULL"} from Equipment {to.Index} to prepare for swap");
                }
            }

            // Get item from 'from' slot
            if (from.ContainerType == SlotContainerType.Inventory)
            {
                itemToMove = _gameSession.Inventory.GetItemAt(from.Index);
                _gameSession.Inventory.RemoveItemAt(from.Index);
                Debug.Log($"  Retrieved item {itemToMove?.Def.displayName ?? "NULL"} from Inventory {from.Index}");

                if(swapItem != null)
                {
                    _gameSession.Inventory.AddItemAt(swapItem, from.Index);
                    Debug.Log($"  Placed item {swapItem.Def.displayName} in Inventory {from.Index}");
                }
            }
            else if (from.ContainerType == SlotContainerType.Equipment)
            {
                itemToMove = _gameSession.PlayerShip.GetEquippedItem(from.Index);
                _gameSession.PlayerShip.RemoveEquippedAt(from.Index);
                Debug.Log($"  Retrieved item {itemToMove?.Def.displayName ?? "NULL"} from Equipment {from.Index}");

                if(swapItem != null)
                {
                    _gameSession.PlayerShip.SetEquipment(from.Index, swapItem);
                    Debug.Log($"  Placed item {swapItem.Def.displayName} in Equipment {from.Index}");
                }
            }

            if (itemToMove == null)
            {
                Debug.LogWarning($"ItemManipulationService: No item found at source slot {from.ContainerType} {from.Index}");
                return;
            }

            // Place item in 'to' slot
            if (to.ContainerType == SlotContainerType.Inventory)
            {
                _gameSession.Inventory.AddItemAt(itemToMove, to.Index);
                Debug.Log($"  Placed item {itemToMove.Def.displayName} in Inventory {to.Index}");
            }
            else if (to.ContainerType == SlotContainerType.Equipment)
            {
                _gameSession.PlayerShip.SetEquipment(to.Index, itemToMove);
                Debug.Log($"  Placed item {itemToMove.Def.displayName} in Equipment {to.Index}");
            }
        }

        public void EquipItem(SlotId from, SlotId to)
        {
            Debug.Log($"ItemManipulationService: EquipItem from {from.ContainerType} {from.Index} to {to.ContainerType} {to.Index}");

            if (from.ContainerType != SlotContainerType.Inventory || to.ContainerType != SlotContainerType.Equipment)
            {
                Debug.LogWarning($"EquipItem: Invalid container types. From: {from.ContainerType}, To: {to.ContainerType}");
                return;
            }

            ItemInstance itemToEquip = _gameSession.Inventory.GetItemAt(from.Index);
            _gameSession.Inventory.RemoveItemAt(from.Index);
            if (itemToEquip == null)
            {
                Debug.LogWarning($"EquipItem: No item found in inventory at index {from.Index}");
                return;
            }

            _gameSession.PlayerShip.SetEquipment(to.Index, itemToEquip);
            Debug.Log($"Equipped {itemToEquip.Def.displayName} from Inventory {from.Index} to Equipment {to.Index}");
        }

        public void UnequipItem(SlotId from, SlotId to)
        {
            Debug.Log($"ItemManipulationService: UnequipItem from {from.ContainerType} {from.Index} to {to.ContainerType} {to.Index}");

            if (from.ContainerType != SlotContainerType.Equipment || to.ContainerType != SlotContainerType.Inventory)
            {
                Debug.LogWarning($"UnequipItem: Invalid container types. From: {from.ContainerType}, To: {to.ContainerType}");
                return;
            }

            ItemInstance itemToUnequip = _gameSession.PlayerShip.GetEquippedItem(from.Index);
            _gameSession.PlayerShip.RemoveEquippedAt(from.Index);
            if (itemToUnequip == null)
            {
                Debug.LogWarning($"UnequipItem: No item found in equipment at index {from.Index}");
                return;
            }

            _gameSession.Inventory.AddItemAt(itemToUnequip, to.Index);
            Debug.Log($"Unequipped {itemToUnequip.Def.displayName} from Equipment {from.Index} to Inventory {to.Index}");
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

    // Define SlotContainerType enum (if not already defined elsewhere)
    public enum SlotContainerType
    {
        Inventory,
        Equipment,
        Shop,
        Crafting,
        // Add other container types as needed
    }
}