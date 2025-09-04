using PirateRoguelike.Services;

namespace PirateRoguelike.UI
{
    public static class UIInteractionService
    {
        public static bool IsInCombat { get; set; } = false;

        public static bool CanManipulateItem(SlotContainerType containerType)
        {
            if (IsInCombat) return false;

            return containerType == SlotContainerType.Inventory || containerType == SlotContainerType.Equipment || containerType == SlotContainerType.Shop;
        }
    }
}
