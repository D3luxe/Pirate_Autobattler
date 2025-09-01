using UnityEngine.UIElements;
using PirateRoguelike.Runtime;
using PirateRoguelike.UI.Components;
using UnityEngine;

namespace PirateRoguelike.UI.Utilities
{
    public static class TooltipUtility
    {
        public static void RegisterTooltipCallbacks(VisualElement element, ISlotViewData slotData, VisualElement panelRoot)
        {
            element.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (!slotData.IsEmpty && slotData.ItemData != null)
                {
                    TooltipController.Instance.Show(slotData.ItemData, element, panelRoot);
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
