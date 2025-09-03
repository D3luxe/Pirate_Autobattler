using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;
using PirateRoguelike.Data;
using PirateRoguelike.Encounters; // For ShopManager

namespace PirateRoguelike.UI
{
    [UxmlElement]
    public partial class ShopItemViewUI : VisualElement
    {
        private Image _itemIcon;
        private Label _itemNameLabel;
        private Label _itemCostLabel;
        private Button _buyButton;

        private ItemSO _itemInstance; // Changed to ItemSO
        private ShopManager _shopManager; // Reference to ShopManager

        public ShopItemViewUI()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _itemIcon = this.Q<Image>("item-icon");
            _itemNameLabel = this.Q<Label>("item-name-label");
            _itemCostLabel = this.Q<Label>("item-cost-label");
            _buyButton = this.Q<Button>("buy-button");

            // Get ShopManager instance
            _shopManager = ShopManager.Instance;

            UpdateUI();

            _buyButton.clicked += OnBuyButtonClicked;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _buyButton.clicked -= OnBuyButtonClicked;
            _itemIcon = null;
            _itemNameLabel = null;
            _itemCostLabel = null;
            _buyButton = null;
            _shopManager = null;
        }

        public void Bind(ItemSO item) // Changed to ItemSO
        {
            _itemInstance = item;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_itemInstance == null) return;

            _itemIcon.sprite = _itemInstance.icon;
            _itemNameLabel.text = _itemInstance.displayName;
            _itemCostLabel.text = _itemInstance.Cost.ToString() + " Gold";
        }

        private void OnBuyButtonClicked()
        {
            if (_shopManager == null)
            {
                Debug.LogError("ShopManager not found!");
                return;
            }
            // _shopManager.BuyItem(_itemInstance.Def); // COMMENTED OUT THIS LINE
        }
    }
}
