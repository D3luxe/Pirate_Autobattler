using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI
{
    [UxmlElement]
    public partial class ShopItemElement : VisualElement
    {

        private VisualElement _rarityOverlay;
        private Image _itemIcon;
        private Label _itemNameLabel;
        private Label _itemDescriptionLabel;
        private Label _itemCostLabel;
        private Button _buyButton;

        private ShopItemViewData _viewModel;

        public ShopItemElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Get references to elements once attached to the panel
            _rarityOverlay = this.Q<VisualElement>("rarity-overlay");
            _itemIcon = this.Q<Image>("item-icon");
            _itemNameLabel = this.Q<Label>("item-name-label");
            _itemDescriptionLabel = this.Q<Label>("item-description-label");
            _itemCostLabel = this.Q<Label>("item-cost-label");
            _buyButton = this.Q<Button>("buy-button");

            // Update UI immediately after elements are queried
            UpdateUI();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Clean up references if needed
            _rarityOverlay = null;
            _itemIcon = null;
            _itemNameLabel = null;
            _itemDescriptionLabel = null;
            _itemCostLabel = null;
            _buyButton = null;

            // Unsubscribe from view model property changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        public void Bind(ShopItemViewData viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUI(e.PropertyName);
        }

        private void UpdateUI(string propertyName = null)
        {
            if (_viewModel == null) return;

            if (propertyName == null || propertyName == nameof(ShopItemViewData.Icon))
            {
                _itemIcon.sprite = _viewModel.Icon;
            }
            if (propertyName == null || propertyName == nameof(ShopItemViewData.ItemName))
            {
                _itemNameLabel.text = _viewModel.ItemName;
            }
            if (propertyName == null || propertyName == nameof(ShopItemViewData.ItemDescription))
            {
                _itemDescriptionLabel.text = _viewModel.ItemDescription;
            }
            if (propertyName == null || propertyName == nameof(ShopItemViewData.ItemCost))
            {
                _itemCostLabel.text = _viewModel.ItemCost;
            }
            if (propertyName == null || propertyName == nameof(ShopItemViewData.RarityColor))
            {
                _rarityOverlay.style.backgroundColor = _viewModel.RarityColor;
            }
            if (propertyName == null || propertyName == nameof(ShopItemViewData.IsPurchasable))
            {
                _buyButton.SetEnabled(_viewModel.IsPurchasable);
            }
        }

        public void RegisterBuyButtonCallback(System.Action callback)
        {
            _buyButton.clicked += callback;
        }

        public void UnregisterBuyButtonCallback(System.Action callback)
        {
            _buyButton.clicked -= callback;
        }
    }
}
