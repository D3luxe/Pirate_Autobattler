using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;
using UnityEngine.UIElements;
using System.Collections;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet shopUss; // Assign this in the Inspector
    [SerializeField] private VisualTreeAsset shopItemViewUxml; // Assign this in the Inspector
    [SerializeField] private StyleSheet shopItemViewUss; // Assign this in the Inspector
    [SerializeField] private VisualTreeAsset shipViewUxml; // Assign this in the Inspector
    [SerializeField] private StyleSheet shipViewUss; // Assign this in the Inspector
    private VisualElement _root;
    private VisualElement _shopItemContainer;
    private VisualElement _shopShipContainer;
    private Label _goldLabel;
    private Label _rerollCostLabel;
    private Button _rerollButton;
    private Button _leaveShopButton;
    private Label _messageLabel;

    [Header("Ship Shop References")]
    private Label _shipPriceLabel;
    private Button _buyShipButton;

    private ShopManager _shopManager;
    private ShipSO _displayedShip;

    void Awake()
    {
        _root = uiDocument.rootVisualElement;
        _root.styleSheets.Add(shopUss);
    }

    public void SetShopManager(ShopManager manager)
    {
        _shopManager = manager;
    }

    public void DisplayShopItems(List<ItemSO> items)
    {
        // Clear existing items
        _shopItemContainer.Clear();

        // Display new items
        foreach (ItemSO itemSO in items)
        {
            var itemView = new ShopItemViewUI();
            itemView.Setup(shopItemViewUxml, shopItemViewUss);
            ItemInstance itemInstance = new ItemInstance(itemSO);
            itemView.SetItem(itemInstance, _shopManager);
            _shopItemContainer.Add(itemView);
        }
    }

    public void DisplayShopShip(ShipSO ship, float cost)
    {
        _displayedShip = ship;

        // Clear existing ship
        _shopShipContainer.Clear();

        if (ship != null)
        {
            var shipView = new ShipViewUI();
            shipView.Setup(shipViewUxml, shipViewUss);
            shipView.SetShip(ship);
            _shopShipContainer.Add(shipView);

            _shipPriceLabel.text = $"{cost}g";
            _buyShipButton.SetEnabled(true);
        }
        else
        {
            _shipPriceLabel.text = "";
            _buyShipButton.SetEnabled(false);
        }
    }

    public void UpdatePlayerGold(int gold)
    {
        _goldLabel.text = $"Gold: {gold}";
    }

    public void UpdateRerollCost(int cost)
    {
        _rerollCostLabel.text = $"({cost}g)";
    }

    public void DisplayMessage(string message, float duration = 2f)
    {
        if (_messageLabel != null)
        {
            _messageLabel.text = message;
            StopAllCoroutines(); // Stop any previous message coroutines
            StartCoroutine(ClearMessageAfterDelay(duration));
        }
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_messageLabel != null)
        {
            _messageLabel.text = "";
        }
    }

    private void OnRerollClicked()
    {
        _shopManager.RerollShop();
    }

    private void OnLeaveShopClicked()
    {
        _shopManager.LeaveShop();
    }

    private void OnBuyShipClicked()
    {
        if (_displayedShip != null)
        {
            _shopManager.BuyShip(_displayedShip);
        }
    }
}
