using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;
using PirateRoguelike.Data;
using PirateRoguelike.Runtime; // For RuntimeItem
using PirateRoguelike.Services; // For SlotId, SlotContainerType
using PirateRoguelike.Shared; // For INotifyPropertyChanged (from ISlotViewData)
using PirateRoguelike.UI.Components; // For SlotManipulator, ItemElement (if needed)

namespace PirateRoguelike.UI
{
    [UxmlElement]
    public partial class ShopItemElement : VisualElement, ISlotViewData // Implement ISlotViewData
    {
        // ... existing fields ...
        private VisualElement _rarityOverlay;
        private Image _itemIcon;
        private Label _itemNameLabel;
        private Label _itemDescriptionLabel;
        private Label _itemCostLabel;
        private Button _buyButton;

        private ShopItemViewData _viewModel;
        private SlotManipulator _manipulator; // Add SlotManipulator

        // ISlotViewData properties
        public int SlotId => _viewModel?.ShopSlotId ?? -1; // Assuming ShopItemViewData will have a ShopSlotId
        public Sprite Icon => _viewModel?.Icon;
        public string Rarity => _viewModel?.Rarity;
        public bool IsEmpty => _viewModel?.CurrentItemInstance == null; // Based on CurrentItemInstance
        public bool IsDisabled => !_viewModel?.IsPurchasable ?? true; // If not purchasable, consider disabled
        public float CooldownPercent => 0; // Shop items don't have cooldowns
        public bool IsPotentialMergeTarget => false; // Not applicable for shop items
        public RuntimeItem ItemData => _viewModel?.CurrentItemInstance?.RuntimeItem;
        public string ItemInstanceId => _viewModel?.CurrentItemInstance?.Def.id;
        public ItemInstance CurrentItemInstance => _viewModel?.CurrentItemInstance;

        // INotifyPropertyChanged implementation for ISlotViewData
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ShopItemElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // Initialize SlotManipulator here, passing 'this' (ShopItemElement)
            // Note: SlotManipulator expects an ItemElement, so we might need to adjust SlotManipulator
            // or create a common interface for draggable elements. For now, let's assume it can work with VisualElement.
            // Or, we can create a dummy ItemElement for the manipulator if ShopItemElement doesn't fully match.
            // Let's try passing 'this' and see if SlotManipulator needs adjustments.
            _manipulator = new SlotManipulator(this, this); // Pass this ShopItemElement as target and sourceSlotData
            this.AddManipulator(_manipulator); // Add the manipulator to this element
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // ... existing element querying ...
            _rarityOverlay = this.Q<VisualElement>("rarity-overlay");
            _itemIcon = this.Q<Image>("item-icon");
            _itemNameLabel = this.Q<Label>("item-name-label");
            _itemDescriptionLabel = this.Q<Label>("item-description-label");
            _itemCostLabel = this.Q<Label>("item-cost-label");
            _buyButton = this.Q<Button>("buy-button");

            // Update UI immediately after elements are queried
            UpdateUI();

            // Register click event for the entire element (for click-to-buy)
            this.RegisterCallback<PointerDownEvent>(OnElementPointerDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt) // Corrected event type
        {
            // ... existing cleanup ...
            _rarityOverlay = null;
            _itemIcon = null;
            _itemNameLabel = null;
            _itemDescriptionLabel = null;
            _itemCostLabel = null;
            _buyButton = null;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // Dispose manipulator
            _manipulator?.Dispose();
            this.UnregisterCallback<PointerDownEvent>(OnElementPointerDown);
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
            UpdateUI(); // Initial update after binding
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUI(e.PropertyName);
            // Also notify ISlotViewData properties changed
            OnPropertyChanged(e.PropertyName); // Forward property changes
        }

        private void UpdateUI(string propertyName = null)
        {
            if (_viewModel == null) return;

            // ... existing UI updates ...
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

        // Handle direct click on the item element
        private void OnElementPointerDown(PointerDownEvent evt)
        {
            // Only trigger click-to-buy if not dragging
            if (!_manipulator.IsDragging && _viewModel != null && _viewModel.IsPurchasable)
            {
                // Request purchase with null player slot (click-to-buy)
                ItemManipulationService.Instance.RequestPurchase(
                    new SlotId(_viewModel.ShopSlotId, SlotContainerType.Shop),
                    new SlotId(-1, SlotContainerType.Inventory) // Indicate click-to-buy, find first available
                );
                evt.StopPropagation(); // Prevent further propagation of the event
            }
        }

        // The RegisterBuyButtonCallback will now be used for the direct click on the button itself,
        // but the primary click-to-buy will be handled by OnElementPointerDown.
        // We can keep this for consistency if the UXML button is still used.
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