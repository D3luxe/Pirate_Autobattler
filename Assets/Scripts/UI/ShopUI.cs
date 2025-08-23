using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PirateRoguelike.Data;
using TMPro; // Add this
using System.Collections;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shopItemContainer; // Parent for shop item views
    [SerializeField] private GameObject shopItemViewPrefab; // Prefab for a single item in shop
    [SerializeField] private TextMeshProUGUI goldText; // Change this
    [SerializeField] private TextMeshProUGUI rerollCostText; // Change this
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button leaveShopButton;
    [SerializeField] private TextMeshProUGUI messageText; // For displaying feedback messages

    [Header("Ship Shop References")]
    [SerializeField] private Transform shopShipContainer;
    [SerializeField] private GameObject shopShipViewPrefab;
    [SerializeField] private Button buyShipButton;
    [SerializeField] private TextMeshProUGUI shipPriceText;

    private ShopManager _shopManager;
    private ShipSO _displayedShip;

    void Awake()
    {
        rerollButton.onClick.AddListener(OnRerollClicked);
        leaveShopButton.onClick.AddListener(OnLeaveShopClicked);
        buyShipButton.onClick.AddListener(OnBuyShipClicked);
        messageText.text = ""; // Clear message on start
    }

    public void SetShopManager(ShopManager manager)
    {
        _shopManager = manager;
    }

    public void DisplayShopItems(List<ItemSO> items)
    {
        // Clear existing items
        foreach (Transform child in shopItemContainer)
        {
            Destroy(child.gameObject);
        }

        // Display new items
        foreach (ItemSO itemSO in items)
        {
            GameObject itemViewGO = Instantiate(shopItemViewPrefab, shopItemContainer);
            ShopItemView itemView = itemViewGO.GetComponent<ShopItemView>();
            // Create ItemInstance here
            ItemInstance itemInstance = new ItemInstance(itemSO);
            itemView.SetItem(itemInstance);
        }
    }

    public void DisplayShopShip(ShipSO ship, float cost)
    {
        _displayedShip = ship;

        // Clear existing ship
        foreach (Transform child in shopShipContainer)
        {
            Destroy(child.gameObject);
        }

        if (ship != null)
        {
            GameObject shipViewGO = Instantiate(shopShipViewPrefab, shopShipContainer);
            ShipView shipView = shipViewGO.GetComponent<ShipView>();
            // Assuming ShipView can be initialized with a ShipSO for display purposes
            // You might need to create a dummy ShipState or modify ShipView.Initialize to accept ShipSO directly
            shipView.Initialize(new ShipState(ship)); 

            shipPriceText.text = $"{cost}g";
            buyShipButton.interactable = true;
        }
        else
        {
            shipPriceText.text = "";
            buyShipButton.interactable = false;
        }
    }

    public void UpdatePlayerGold(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    public void UpdateRerollCost(int cost)
    {
        rerollCostText.text = $"Reroll ({cost}g)";
    }

    public void DisplayMessage(string message, float duration = 2f)
    {
        if (messageText != null)
        {
            messageText.text = message;
            StopAllCoroutines(); // Stop any previous message coroutines
            StartCoroutine(ClearMessageAfterDelay(duration));
        }
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
        {
            messageText.text = "";
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
