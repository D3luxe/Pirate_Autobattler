using UnityEngine.UIElements;
using PirateRoguelike.Runtime;
using PirateRoguelike.UI.Components;

namespace PirateRoguelike.UI.Utilities
{
    public static class TooltipUtility
    {
        public static void RegisterTooltipCallbacks(VisualElement element, ISlotViewData slotData)
        {
            element.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (!slotData.IsEmpty && slotData.ItemData != null)
                {
                    TooltipController.Instance.Show(slotData.ItemData, element);
                }
                else
                {
                    TooltipController.Instance.Hide();
                }
            });
            element.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                if (TooltipController.Instance.IsTooltipVisible)
                {
                    TooltipController.Instance.Hide();
                }
            });
        }
    }
}
