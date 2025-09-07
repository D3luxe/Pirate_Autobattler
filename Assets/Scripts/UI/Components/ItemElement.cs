using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Data;
using System.ComponentModel;
using PirateRoguelike.Core;

namespace PirateRoguelike.UI.Components
{
    public partial class ItemElement : VisualElement
    {
        public ISlotViewData SlotViewData { get; set; }

        // UXML elements
        private VisualElement _itemIcon;
        private Label _priceLabel;

        public ItemElement()
        {
            Debug.Log("ItemElement: Constructor called.");
            // Load UXML and add to hierarchy
            var visualTree = ServiceLocator.Resolve<UIAssetRegistry>().ItemElementUXML;
            visualTree.CloneTree(this);

            // Query elements
            _itemIcon = this.Q<VisualElement>("item-icon");
            _priceLabel = this.Q<Label>("price-label");
        }

        public void Bind()
        {
            if (SlotViewData == null || SlotViewData.CurrentItemInstance == null)
            {
                // Hide or clear elements if no item
                _itemIcon.style.backgroundImage = null;
                _priceLabel.text = string.Empty;
                this.style.visibility = Visibility.Hidden;
                return;
            }

            this.style.visibility = Visibility.Visible;

            // Set item icon
            _itemIcon.style.backgroundImage = new StyleBackground(SlotViewData.Icon);

            // Set price label visibility and text based on SlotViewData type
            if (SlotViewData is ShopSlotViewModel shopSlotViewModel)
            {
                _priceLabel.text = $"{shopSlotViewModel.Price}g";
                _priceLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _priceLabel.text = string.Empty;
                _priceLabel.style.display = DisplayStyle.None;
            }
        }
    }
}