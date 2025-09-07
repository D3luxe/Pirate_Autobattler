using System.ComponentModel;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using PirateRoguelike.Services;
using UnityEngine;

namespace PirateRoguelike.UI
{
    public class RewardItemSlotViewData : ISlotViewData
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ItemInstance CurrentItemInstance { get; private set; }
        public Sprite Icon => CurrentItemInstance?.Def.icon; // Implement Icon
        public int SlotId { get; private set; } // Implement SlotId
        public bool IsEmpty => CurrentItemInstance == null; // Implement IsEmpty
        public SlotContainerType ContainerType => SlotContainerType.Reward;
        public int Index { get; private set; }

        public RewardItemSlotViewData(ItemInstance itemInstance)
        {
            CurrentItemInstance = itemInstance;
            Index = -1; // Reward slots don't have a fixed index like inventory
        }

        // This method is required by ISlotViewData but not directly used for reward items
        public void SetItem(ItemInstance itemInstance)
        {
            CurrentItemInstance = itemInstance;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentItemInstance)));
        }
    }
}