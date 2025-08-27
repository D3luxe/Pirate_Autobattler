
using System;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public static class PlayerPanelEvents
    {
        // Control Events
        public static Action OnPauseClicked;
        public static Action OnSettingsClicked;
        public static Action<int> OnBattleSpeedChanged;
        public static Action OnMapToggleClicked;

        // Slot Interaction Events
        public static Action<int> OnSlotClicked;
        public static Action<int, PointerDownEvent> OnSlotBeginDrag;
        public static Action<int, SlotContainerType, int, SlotContainerType> OnSlotDropped; // fromSlotId, fromContainer, toSlotId, toContainer

        // Tooltip Events
        public static Action<int> OnTooltipRequested;
        public static Action OnTooltipHidden;
    }
}
