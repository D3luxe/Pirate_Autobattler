
using System.Collections.Generic;
using UnityEngine;

namespace PirateRoguelike.UI
{
    // Represents the visual state of a single slot
    public interface ISlotViewData
    {
        int SlotId { get; }
        Sprite Icon { get; }
        string Rarity { get; } // e.g., "bronze", "silver"
        bool IsEmpty { get; }
        bool IsDisabled { get; }
        float CooldownPercent { get; } // 0.0 to 1.0
        bool IsPotentialMergeTarget { get; }
        ItemSO ItemData { get; } // New: Reference to the actual ItemSO
    }

    // Represents the visual state of the ship panel
    public interface IShipViewData
    {
        string ShipName { get; }
        Sprite ShipSprite { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
    }

    // Represents the visual state of the HUD counters
    public interface IHudViewData
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
        List<ISlotViewData> EquipmentSlots { get; }
        List<ISlotViewData> InventorySlots { get; }
    }
}
