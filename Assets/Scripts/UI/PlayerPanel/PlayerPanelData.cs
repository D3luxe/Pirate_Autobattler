using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using PirateRoguelike.Shared;
using PirateRoguelike.Data;
using PirateRoguelike.Services;

namespace PirateRoguelike.UI
{
    // Represents the visual state of a single slot
    public interface ISlotViewData : System.ComponentModel.INotifyPropertyChanged
{
    ItemInstance CurrentItemInstance { get; }
    Sprite Icon { get; }
    int SlotId { get; }
    bool IsEmpty { get; }
    SlotContainerType ContainerType { get; }
}

    // Represents the visual state of the ship panel
    public interface IShipViewData : System.ComponentModel.INotifyPropertyChanged
    {
        string ShipName { get; }
        Sprite ShipSprite { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
    }

    // Represents the visual state of the HUD counters
    public interface IHudViewData : System.ComponentModel.INotifyPropertyChanged
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