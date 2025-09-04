using UnityEngine;
using System.ComponentModel;
using PirateRoguelike.Data;
using PirateRoguelike.Services;

namespace PirateRoguelike.UI
{
    // This ViewModel is specifically for items displayed in the shop.
    // It implements the standard ISlotViewData for compatibility with SlotElement
    // but adds shop-specific properties like Price.
    public class ShopSlotViewModel : SlotDataViewModel, ISlotViewData
    {
        public float Price { get; private set; }
        public bool IsPurchasable { get; private set; }
        public new SlotContainerType ContainerType => SlotContainerType.Shop;

        public ShopSlotViewModel(ItemInstance itemInstance, float price, bool isPurchasable, int slotId)
            : base(itemInstance, slotId, global::PirateRoguelike.Services.SlotContainerType.Shop)
        {
            Price = price;
            IsPurchasable = isPurchasable;
        }
    }
}