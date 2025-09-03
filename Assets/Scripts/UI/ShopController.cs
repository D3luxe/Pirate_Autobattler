using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Encounters; // To access ShopManager
using PirateRoguelike.Core; // For GameSession, Economy, etc.
using PirateRoguelike.Data; // For ItemSO, ShipSO
using PirateRoguelike.Services; // For ItemManipulationService
using PirateRoguelike.UI.Components; // For ShopItemElement, ShipViewUI
using System.Collections.Generic; // For List

namespace PirateRoguelike.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private ShopManager _shopManager; // Reference to the ShopManager

        // UI Elements (queried from UXML)
        private VisualElement _shopRoot;
        private Label _merchantNameLabel; // New: for MerchantName
        private Label _messageLabel; // For displaying messages like "Not enough gold!"
        private VisualElement _shopItemContainer;
        private VisualElement _shopShipContainer;
        private Label _rerollCostLabel;
        private Button _rerollButton;
        private Button _leaveShopButton;
        private Label _shipPriceLabel; // For the ship price
        private Button _buyShipButton; // For the buy ship button

        void Awake()
        {
            // Get the UIDocument component attached to this GameObject
            UIDocument shopUIDocument = GetComponent<UIDocument>();
            if (shopUIDocument == null)
            {
                Debug.LogError("ShopController requires a UIDocument component on the same GameObject.");
                return;
            }

            _shopRoot = shopUIDocument.rootVisualElement;

            // Query UI elements by name
            _merchantNameLabel = _shopRoot.Q<Label>("MerchantName");
            _messageLabel = _shopRoot.Q<Label>("MessageLabel");
            _shopItemContainer = _shopRoot.Q<VisualElement>("ShopItemContainer");
            _shopShipContainer = _shopRoot.Q<VisualElement>("ShopShipContainer");
            _rerollCostLabel = _shopRoot.Q<Label>("RerollCostLabel");
            _rerollButton = _shopRoot.Q<Button>("RerollButton");
            _leaveShopButton = _shopRoot.Q<Button>("LeaveShopButton");
            _shipPriceLabel = _shopRoot.Q<Label>("ShipPriceLabel");
            _buyShipButton = _shopRoot.Q<Button>("BuyShipButton");

            // Register button callbacks
            _rerollButton.clicked += OnRerollButtonClicked;
            _leaveShopButton.clicked += OnLeaveShopButtonClicked;
            _buyShipButton.clicked += OnBuyShipButtonClicked; // Assuming this button is always present in UXML
        }

        void OnEnable()
        {
            // Subscribe to ShopManager events to update UI
            if (_shopManager != null)
            {
                _shopManager.OnShopDataUpdated += UpdateShopUI;
                _shopManager.OnMessageDisplayed += DisplayMessage;
            }
        }

        void OnDisable()
        {
            // Unsubscribe from ShopManager events
            if (_shopManager != null)
            {
                _shopManager.OnShopDataUpdated -= UpdateShopUI;
                _shopManager.OnMessageDisplayed -= DisplayMessage;
            }
        }

        void Start()
        {
            // Initial UI update when the shop is first opened
            if (_shopManager != null)
            {
                // ShopManager now generates items in its Start/Awake, so just update UI
                UpdateShopUI();
            }
        }

        private void OnRerollButtonClicked()
        {
            _shopManager.RerollShop();
        }

        private void OnLeaveShopButtonClicked()
        {
            _shopManager.LeaveShop();
        }

        private void OnBuyShipButtonClicked()
        {
            // ShopManager needs to expose the current ship for sale
            if (_shopManager.CurrentShopShip != null)
            {
                _shopManager.BuyShip(_shopManager.CurrentShopShip);
            }
        }

        // This method will be called by ShopManager when shop data changes
        public void UpdateShopUI()
        {
            // Update Merchant Name (static for now as per user request)
            // _merchantNameLabel.text = "Ol' Barnacle Bill's Emporium"; // Already set in UXML

            // Clear existing items and populate with current shop items
            _shopItemContainer.Clear();
            for (int i = 0; i < _shopManager.CurrentShopItems.Count; i++)
            {
                var itemSO = _shopManager.CurrentShopItems[i];
                var shopItemViewData = new ShopItemViewData(itemSO, GetRarityColor(itemSO.rarity), GameSession.Economy.Gold >= itemSO.Cost, i); // Pass index as ShopSlotId
                var shopItemElement = new ShopItemElement();
                shopItemElement.Bind(shopItemViewData);
                // The click-to-buy and drag-to-buy are now handled within ShopItemElement
                _shopItemContainer.Add(shopItemElement);
            }

            // Update ship display
            _shopShipContainer.Clear();
            if (_shopManager.CurrentShopShip != null)
            {
                var shipViewUI = new ShipViewUI(); // Assuming ShipViewUI is a reusable component
                var shopShipState = new PirateRoguelike.Core.ShipState(_shopManager.CurrentShopShip); // Use Core.ShipState
                var enemyShipViewData = new PirateRoguelike.UI.EnemyShipViewData(shopShipState);
                shipViewUI.Bind(enemyShipViewData);
                _shopShipContainer.Add(shipViewUI);

                // Update ship price label and buy button state
                _shipPriceLabel.text = $"{_shopManager.CurrentShopShip.Cost} Gold";
                _buyShipButton.SetEnabled(GameSession.Economy.Gold >= _shopManager.CurrentShopShip.Cost);
            }
            else
            {
                _shipPriceLabel.text = "";
                _buyShipButton.SetEnabled(false);
            }

            // Update reroll cost
            _rerollCostLabel.text = $"({GameSession.Economy.GetCurrentRerollCost()}g)";

            // Update player gold (if needed, but user said it's on PlayerPanel)
            // If you want to show gold here, you'd need a label for it and update it.
            // _playerGoldLabel.text = $"Gold: {GameSession.Economy.Gold}"; // This label was removed from UXML
        }

        private void DisplayMessage(string message)
        {
            _messageLabel.text = message;
            // Optionally, add a timer to clear the message after a few seconds
        }

        // Helper to get rarity color (can be moved to a utility class or theme SO)
        private Color GetRarityColor(Rarity rarity)
        {
            // This needs to be consistent with PlayerUIThemeSO.rarityColors
            // For now, hardcode or get from a central place
            switch (rarity)
            {
                case Rarity.Bronze: return new Color(0.8f, 0.5f, 0.2f); // Example Bronze
                case Rarity.Silver: return new Color(0.7f, 0.7f, 0.7f); // Example Silver
                case Rarity.Gold: return new Color(1.0f, 0.8f, 0.0f); // Example Gold
                case Rarity.Diamond: return new Color(0.0f, 0.8f, 1.0f); // Example Diamond
                default: return Color.white;
            }
        }
    }
}