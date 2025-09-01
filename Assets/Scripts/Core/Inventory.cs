using System;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using UnityEngine;
using PirateRoguelike.Services; // Added for SlotId
using PirateRoguelike.Events; // Added for ItemManipulationEvents

public class Inventory
    {
        public List<ItemSlot> Slots { get; private set; }
        public int MaxSize { get; private set; }

    public Inventory(int maxSize)
    {
        MaxSize = maxSize;
        Slots = new List<ItemSlot>(maxSize);
        for (int i = 0; i < maxSize; i++)
        {
            Slots.Add(new ItemSlot());
        }
    }

    public bool AddItem(ItemInstance newItem)
    {
        // Try to merge with an existing item
        for (int i = 0; i < MaxSize; i++)
        {
            ItemInstance existingItem = Slots[i].Item;
            if (existingItem != null && existingItem.Def.id == newItem.Def.id && existingItem.Def.rarity == newItem.Def.rarity)
            {
                // Found a duplicate of the same rarity, attempt to merge
                Rarity nextRarity = GetNextRarity(existingItem.Def.rarity);
                if (nextRarity != existingItem.Def.rarity) // If there's a next rarity
                {
                    ItemSO upgradedItemSO = GameDataRegistry.GetItem(existingItem.Def.id, nextRarity); // Assuming GetItem can take rarity
                    if (upgradedItemSO != null)
                    {
                        Slots[i].Item = new ItemInstance(upgradedItemSO); // Replace with upgraded item
                        ItemManipulationEvents.DispatchItemAdded(new ItemInstance(upgradedItemSO), new SlotId(i, SlotContainerType.Inventory));
                        return true;
                    }
                }
            }
        }

        // If no merge, add to first empty slot
        for (int i = 0; i < MaxSize; i++)
        {
            if (Slots[i].Item == null)
            {
                Slots[i].Item = newItem;
                ItemManipulationEvents.DispatchItemAdded(newItem, new SlotId(i, SlotContainerType.Inventory));
                return true;
            }
        }
        return false; // Inventory full and no merge possible
    }

    public bool CanAddItem(ItemInstance newItem)
    {
        // Check if merge is possible
        for (int i = 0; i < MaxSize; i++)
        {
            ItemInstance existingItem = Slots[i].Item;
            if (existingItem != null && existingItem.Def.id == newItem.Def.id && existingItem.Def.rarity == newItem.Def.rarity)
            {
                Rarity nextRarity = GetNextRarity(existingItem.Def.rarity);
                if (nextRarity != existingItem.Def.rarity) // If there's a next rarity
                {
                    ItemSO upgradedItemSO = GameDataRegistry.GetItem(existingItem.Def.id, nextRarity); // Assuming GetItem can take rarity
                    if (upgradedItemSO != null)
                    {
                        return true; // Merge is possible
                    }
                }
            }
        }

        // Check for empty slot
        for (int i = 0; i < MaxSize; i++)
        {
            if (Slots[i].Item == null)
            {
                return true; // Empty slot available
            }
        }
        return false; // Inventory full and no merge possible
    }

    private Rarity GetNextRarity(Rarity currentRarity)
    {
        switch (currentRarity)
        {
            case Rarity.Bronze: return Rarity.Silver;
            case Rarity.Silver: return Rarity.Gold;
            case Rarity.Gold: return Rarity.Diamond;
            case Rarity.Diamond: return Rarity.Diamond; // Max rarity
            default: return currentRarity;
        }
    }

    public void AddItemAt(ItemInstance item, int index)
    {
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.AddItemAt: Invalid index {index}");
            return;
        }
        Slots[index].Item = item;
        ItemManipulationEvents.DispatchItemAdded(item, new SlotId(index, SlotContainerType.Inventory));
    }

    public ItemInstance RemoveItemAt(int index)
    {
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.RemoveItemAt: Invalid index {index}");
            return null;
        }
        ItemInstance _item = Slots[index].Item;
        Slots[index].Item = null;
        ItemManipulationEvents.DispatchItemRemoved(_item, new SlotId(index, SlotContainerType.Inventory));
        return _item;
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= MaxSize || indexB < 0 || indexB >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.SwapItems: Invalid indices. indexA: {indexA}, indexB: {indexB}");
            return;
        }
        ItemInstance itemA_before = Slots[indexA].Item;
        ItemInstance itemB_before = Slots[indexB].Item;

        // Swap the ItemInstances within the ItemSlot objects
        (Slots[indexA].Item, Slots[indexB].Item) = (Slots[indexB].Item, Slots[indexA].Item);
        
        ItemInstance itemA_after = Slots[indexA].Item;
        ItemInstance itemB_after = Slots[indexB].Item;
        ItemManipulationEvents.DispatchItemMoved(Slots[indexA].Item, new SlotId(indexA, SlotContainerType.Inventory), new SlotId(indexB, SlotContainerType.Inventory));
    }

    public ItemInstance GetItemAt(int index)
    {
        if (index < 0 || index >= MaxSize) return null;
        return Slots[index].Item;
    }

    public void SetItemAt(int index, ItemInstance item)
    {
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.SetItemAt: Invalid index {index}");
            return;
        }
        Slots[index].Item = item;
        ItemManipulationEvents.DispatchItemAdded(item, new SlotId(index, SlotContainerType.Inventory));
    }
}