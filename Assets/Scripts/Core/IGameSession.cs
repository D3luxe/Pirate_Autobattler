using System;
using System.Collections.Generic;
using UnityEngine;
using PirateRoguelike.Data; // For ItemInstance
using PirateRoguelike.Runtime; // For RuntimeItem
using PirateRoguelike.Services; // For IEconomyService

namespace PirateRoguelike.Core
{
    // Interface for PlayerShip properties and events used by ViewModel
    public interface IPlayerShip
    {
        ShipSO Def { get; }
        event Action OnEquipmentChanged;
        ItemInstance[] Equipped { get; }
        ItemInstance GetEquippedItem(int index);
        void SwapEquipment(int fromIndex, int toIndex);
        void SetEquippedAt(int index, ItemInstance item);
        void RemoveEquippedAt(int index);
        // NEW: For ItemManipulationService
        bool IsEquipmentSlotOccupied(int index);
        int GetFirstEmptyEquipmentSlot();
        bool SetEquipment(int index, ItemInstance item); // Changed return type to bool
    }

    // Interface for Inventory properties and events used by ViewModel
    public interface IInventory
    {
        event Action OnInventoryChanged;
        ItemInstance[] Items { get; }
        ItemInstance GetItemAt(int index);
        void SwapItems(int fromIndex, int toIndex);
        void SetItemAt(int index, ItemInstance item);
        void RemoveItemAt(int index);
        bool AddItemAt(ItemInstance item, int index); // Changed return type to bool
        // NEW: For ItemManipulationService
        bool IsSlotOccupied(int index);
        int GetFirstEmptySlot();
    }

    // Interface for GameSession properties and events used by ViewModel
    public interface IGameSession
    {
        ShipState PlayerShip { get; } // Changed from IPlayerShip
        Inventory Inventory { get; } // Changed from IInventory
        int Gold { get; }
        int Lives { get; }
        int CurrentDepth { get; }

        // NEW: For ItemManipulationService
        EconomyService Economy { get; }

        event Action OnPlayerShipInitialized; // NEW
        event Action OnInventoryInitialized; // NEW
        event Action OnEconomyInitialized; // NEW
    }
}
