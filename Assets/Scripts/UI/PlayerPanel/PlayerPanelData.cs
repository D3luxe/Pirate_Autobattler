
using System.Collections.Generic;
using UnityEngine;
using PirateRoguelike.Runtime; // Added for RuntimeItem
using System.ComponentModel;
using PirateRoguelike.Shared; // Added for ObservableList
using PirateRoguelike.Data; // NEW

namespace PirateRoguelike.UI
{
    // Represents the visual state of a single slot
    public interface ISlotViewData : INotifyPropertyChanged
    {
        int SlotId { get; }
        Sprite Icon { get; }
        string Rarity { get; }
        bool IsEmpty { get; }
        bool IsDisabled { get; }
        float CooldownPercent { get; }
        bool IsPotentialMergeTarget { get; }
        RuntimeItem ItemData { get; }
        string ItemInstanceId { get; }
        ItemInstance CurrentItemInstance { get; } // NEW
    }

    // Represents the visual state of the ship panel
    public interface IShipViewData : INotifyPropertyChanged
    {
        string ShipName { get; }
        Sprite ShipSprite { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
    }

    // Represents the visual state of the HUD counters
    public interface IHudViewData : INotifyPropertyChanged
    {
        int Gold { get; }
        int Lives { get; }
        int Depth { get; }
    }

    // Composition of all data needed to render the entire player panel
    public interface IPlayerPanelData
    {
        IShipViewData ShipData { get; }
        IHudViewData HudData { get; }
        ObservableList<ISlotViewData> EquipmentSlots { get; }
        ObservableList<ISlotViewData> InventorySlots { get; }
    }
}
