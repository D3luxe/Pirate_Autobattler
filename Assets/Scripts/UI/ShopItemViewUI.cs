using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Data;

[UxmlElement]
public partial class ShopItemViewUI : VisualElement
{
    private VisualElement _itemIcon;
    private Label _itemName;
    private Label _itemPrice;
    private Button _buyItemButton;

    private ItemInstance _itemInstance;
    private ShopManager _shopManager; // Reference to the shop manager to handle buy logic

    public ShopItemViewUI()
    {
        // Query elements
        _itemIcon = this.Q<VisualElement>("ItemIcon");
        _itemName = this.Q<Label>("ItemName");
        _itemPrice = this.Q<Label>("ItemPrice");
        _buyItemButton = this.Q<Button>("BuyItemButton");

        _buyItemButton.clicked += OnBuyItemClicked;
    }

    public void Setup(VisualTreeAsset uxml, StyleSheet uss)
    {
        uxml.CloneTree(this);
        styleSheets.Add(uss);
    }

    public void SetItem(ItemInstance itemInstance, ShopManager shopManager)
    {
        _itemInstance = itemInstance;
        _shopManager = shopManager;

        _itemName.text = itemInstance.Def.displayName;
        _itemPrice.text = $"{itemInstance.Def.Cost}g"; // Assuming ItemSO has a baseCost

        // Set item icon (assuming ItemSO has a sprite field)
        if (itemInstance.Def.icon != null)
        {
            _itemIcon.style.backgroundImage = new StyleBackground(itemInstance.Def.icon.texture);
        }
    }

    private void OnBuyItemClicked()
    {
        if (_itemInstance != null && _shopManager != null)
        {
            _shopManager.BuyItem(_itemInstance.Def);
        }
    }
}