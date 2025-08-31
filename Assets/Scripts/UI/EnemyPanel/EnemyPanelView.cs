using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class EnemyPanelView
    {
        private VisualElement _root;
        private VisualTreeAsset _slotTemplate;
        private PlayerUIThemeSO _theme;

        // Queried Elements
        private ShipPanelView _shipPanelView;
        private VisualElement _equipmentBar;

        public EnemyPanelView(VisualElement root, VisualTreeAsset slotTemplate, PlayerUIThemeSO theme)
        {
            _root = root;
            _slotTemplate = slotTemplate;
            _theme = theme;

            QueryElements();
        }

        private void QueryElements()
        {
            _shipPanelView = new ShipPanelView(_root.Q("ship-panel-instance"));
            _equipmentBar = _root.Q("equipment-bar");
        }

        public void UpdateShipData(IShipViewData data)
        {
            _shipPanelView.SetShipName(data.ShipName);
            _shipPanelView.SetShipSprite(data.ShipSprite);
            _shipPanelView.UpdateHealth(data.CurrentHp, data.MaxHp);
        }

        public void UpdateEquipment(System.Collections.Generic.List<ISlotViewData> slots)
        {
            PopulateSlots(_equipmentBar, slots);
        }

        private void PopulateSlots(VisualElement container, System.Collections.Generic.List<ISlotViewData> slots)
        {
            container.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                var slotInstance = _slotTemplate.Instantiate();
                var slotElement = slotInstance.Q<VisualElement>("slot");
                slotElement.userData = slots[i]; // Store the entire ISlotViewData
                container.Add(slotElement);

                // Register PointerEnter and PointerLeave events for tooltip
                if (!slots[i].IsEmpty && slots[i].ItemData != null)
                {
                    var currentSlotData = slots[i]; // Capture the current slot data
                    slotElement.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        TooltipController.Instance.Show(currentSlotData.ItemData, slotElement);
                    });
                    slotElement.RegisterCallback<PointerLeaveEvent>(evt =>
                    {
                        TooltipController.Instance.Hide();
                    });
                }

                BindSlot(slotElement, slots[i]);
            }
        }

        private void BindSlot(VisualElement slotElement, ISlotViewData slotData)
        {
            var icon = slotElement.Q<Image>("icon");
            icon.sprite = slotData.IsEmpty ? _theme.emptySlotBackground : slotData.Icon;
            icon.style.visibility = slotData.IsEmpty ? Visibility.Visible : Visibility.Visible;

            // Assign rarity frame from theme
            var rarityFrame = slotElement.Q<Image>("rarity-frame");
            if (!string.IsNullOrEmpty(slotData.Rarity))
            {
                switch (slotData.Rarity.ToLower())
                {
                    case "bronze": rarityFrame.sprite = _theme.bronzeFrame; break;
                    case "silver": rarityFrame.sprite = _theme.silverFrame; break;
                    case "gold": rarityFrame.sprite = _theme.goldFrame; break;
                    case "diamond": rarityFrame.sprite = _theme.diamondFrame; break;
                }
            }
            else
            {
                rarityFrame.sprite = null; // No frame for empty/unassigned rarity
            }

            slotElement.Q<VisualElement>("cooldown-overlay").style.scale = new Scale(new Vector2(1, slotData.CooldownPercent));

            slotElement.ClearClassList();
            slotElement.AddToClassList("slot");

            if (slotData.IsEmpty) slotElement.AddToClassList("slot--empty");
            if (slotData.IsDisabled) slotElement.AddToClassList("slot--disabled");
            if (slotData.IsPotentialMergeTarget) slotElement.AddToClassList("slot--merge");

            if (!string.IsNullOrEmpty(slotData.Rarity))
            {
                slotElement.AddToClassList($"rarity--{slotData.Rarity.ToLower()}");
            }
        }
    }
}
