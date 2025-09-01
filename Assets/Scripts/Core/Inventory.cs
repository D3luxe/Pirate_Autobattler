using System;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using UnityEngine;

public class Inventory
{
    public List<ItemSlot> Slots { get; private set; }
    public int MaxSize { get; private set; }

    public event Action<int, int> OnItemsSwapped;
    public event Action<int, ItemInstance> OnItemAddedAt;
    public event Action<int, ItemInstance> OnItemRemovedAt;

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
                        OnItemAddedAt?.Invoke(i, new ItemInstance(upgradedItemSO));
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
                OnItemAddedAt?.Invoke(i, newItem);
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
        Debug.Log($"Inventory.AddItemAt: Adding item {item?.Def.id ?? "NULL"} at index {index}.");
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.AddItemAt: Invalid index {index}");
            return;
        }
        Slots[index].Item = item;
        OnItemAddedAt?.Invoke(index, item);
    }

    public ItemInstance RemoveItemAt(int index)
    {
        Debug.Log($"Inventory.RemoveItemAt: Removing item at index {index}.");
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.RemoveItemAt: Invalid index {index}");
            return null;
        }
        ItemInstance item = Slots[index].Item;
        Debug.Log($"Inventory.RemoveItemAt: Item removed: {item?.Def.id ?? "NULL"}");
        Slots[index].Item = null;
        OnItemRemovedAt?.Invoke(index, item);
        return item;
    }

    public void SwapItems(int indexA, int indexB)
    {
        Debug.Log($"Inventory.SwapItems: Attempting to swap items at index {indexA} and {indexB}.");
        if (indexA < 0 || indexA >= MaxSize || indexB < 0 || indexB >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.SwapItems: Invalid indices. indexA: {indexA}, indexB: {indexB}");
            return;
        }
        ItemInstance itemA_before = Slots[indexA].Item;
        ItemInstance itemB_before = Slots[indexB].Item;
        Debug.Log($"Inventory.SwapItems: Before swap - Item at {indexA}: {itemA_before?.Def.id ?? "NULL"}, Item at {indexB}: {itemB_before?.Def.id ?? "NULL"}");

        // Swap the ItemInstances within the ItemSlot objects
        (Slots[indexA].Item, Slots[indexB].Item) = (Slots[indexB].Item, Slots[indexA].Item);
        
        ItemInstance itemA_after = Slots[indexA].Item;
        ItemInstance itemB_after = Slots[indexB].Item;
        Debug.Log($"Inventory.SwapItems: After swap - Item at {indexA}: {itemA_after?.Def.id ?? "NULL"}, Item at {indexB}: {itemB_after?.Def.id ?? "NULL"}");

        OnItemsSwapped?.Invoke(indexA, indexB);
    }

    public ItemInstance GetItemAt(int index)
    {
        if (index < 0 || index >= MaxSize) return null;
        return Slots[index].Item;
    }

    public void SetItemAt(int index, ItemInstance item)
    {
        Debug.Log($"Inventory.SetItemAt: Setting item {item?.Def.id ?? "NULL"} at index {index}.");
        if (index < 0 || index >= MaxSize) 
        {
            Debug.LogWarning($"Inventory.SetItemAt: Invalid index {index}");
            return;
        }
        Slots[index].Item = item;
        OnItemAddedAt?.Invoke(index, item);
    }
}