using System;
using System.Linq;
using PirateRoguelike.Data;
using UnityEngine;

public class Inventory
{
    public ItemInstance[] Items { get; private set; }
    public int MaxSize { get; private set; }

    public event Action OnInventoryChanged;

    public Inventory(int maxSize)
    {
        MaxSize = maxSize;
        Items = new ItemInstance[maxSize];
    }

    public bool AddItem(ItemInstance newItem)
    {
        // Try to merge with an existing item
        for (int i = 0; i < MaxSize; i++)
        {
            ItemInstance existingItem = Items[i];
            if (existingItem != null && existingItem.Def.id == newItem.Def.id && existingItem.Def.rarity == newItem.Def.rarity)
            {
                // Found a duplicate of the same rarity, attempt to merge
                Rarity nextRarity = GetNextRarity(existingItem.Def.rarity);
                if (nextRarity != existingItem.Def.rarity) // If there's a next rarity
                {
                    ItemSO upgradedItemSO = GameDataRegistry.GetItem(existingItem.Def.id, nextRarity); // Assuming GetItem can take rarity
                    if (upgradedItemSO != null)
                    {
                        Items[i] = new ItemInstance(upgradedItemSO); // Replace with upgraded item
                        OnInventoryChanged?.Invoke();
//                        Debug.Log("Inventory: OnInventoryChanged invoked from AddItem (merge).");
                        return true;
                    }
                }
            }
        }

        // If no merge, add to first empty slot
        for (int i = 0; i < MaxSize; i++)
        {
            if (Items[i] == null)
            {
                Items[i] = newItem;
                OnInventoryChanged?.Invoke();
//                Debug.Log("Inventory: OnInventoryChanged invoked from AddItem (new slot).");
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
            ItemInstance existingItem = Items[i];
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
            if (Items[i] == null)
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
        if (index < 0 || index >= MaxSize) return;
        Items[index] = item;
        OnInventoryChanged?.Invoke();
//        Debug.Log("Inventory: OnInventoryChanged invoked from AddItemAt.");
    }

    public ItemInstance RemoveItemAt(int index)
    {
        if (index < 0 || index >= MaxSize) return null;
        ItemInstance item = Items[index];
        Items[index] = null;
        OnInventoryChanged?.Invoke();
 //       Debug.Log("Inventory: OnInventoryChanged invoked from RemoveItemAt.");
        return item;
    }

    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= MaxSize || indexB < 0 || indexB >= MaxSize) return;
        (Items[indexA], Items[indexB]) = (Items[indexB], Items[indexA]);
        OnInventoryChanged?.Invoke();
//        Debug.Log("Inventory: OnInventoryChanged invoked from SwapItems.");
    }

    public ItemInstance GetItemAt(int index)
    {
        if (index < 0 || index >= MaxSize) return null;
        return Items[index];
    }

    public void SetItemAt(int index, ItemInstance item)
    {
        if (index < 0 || index >= MaxSize) return;
        Items[index] = item;
        OnInventoryChanged?.Invoke();
//        Debug.Log("Inventory: OnInventoryChanged invoked from SetItemAt.");
    }
}