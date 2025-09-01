using System;
using PirateRoguelike.Data;
using PirateRoguelike.Services; // For SlotId

namespace PirateRoguelike.Events
{
    public static class ItemManipulationEvents
    {
        public static event Action<ItemInstance, SlotId, SlotId> OnItemMoved;
        public static event Action<ItemInstance, SlotId, SlotId> OnItemEquipped;
        public static event Action<ItemInstance, SlotId, SlotId> OnItemUnequipped;
        public static event Action<ItemInstance, SlotId> OnItemAdded;
        public static event Action<ItemInstance, SlotId> OnItemRemoved;

        public static void DispatchItemMoved(ItemInstance item, SlotId from, SlotId to)
        {
            OnItemMoved?.Invoke(item, from, to);
        }

        public static void DispatchItemEquipped(ItemInstance item, SlotId from, SlotId to)
        {
            OnItemEquipped?.Invoke(item, from, to);
        }

        public static void DispatchItemUnequipped(ItemInstance item, SlotId from, SlotId to)
        {
            OnItemUnequipped?.Invoke(item, from, to);
        }

        public static void DispatchItemAdded(ItemInstance item, SlotId to)
        {
            OnItemAdded?.Invoke(item, to);
        }

        public static void DispatchItemRemoved(ItemInstance item, SlotId from)
        {
            OnItemRemoved?.Invoke(item, from);
        }
    }
}