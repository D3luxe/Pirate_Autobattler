using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemView : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemCostText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image rarityOverlayImage; // New: For rarity color overlay
    [SerializeField] private Color[] rarityColors; // New: Array of colors for rarities (Bronze, Silver, Gold, Diamond)

    private ItemInstance _itemInstance;

    public void SetItem(ItemInstance item)
    {
        _itemInstance = item;
        if (_itemInstance != null)
        {
            if (itemIcon != null) itemIcon.sprite = _itemInstance.Def.icon;
            if (itemNameText != null) itemNameText.text = _itemInstance.Def.displayName;
            if (itemDescriptionText != null) itemDescriptionText.text = _itemInstance.Def.description;
            if (itemCostText != null) itemCostText.text = _itemInstance.Def.Cost.ToString() + " Gold";
            
            // Set rarity color overlay
            if (rarityOverlayImage != null && rarityColors != null && (int)_itemInstance.Def.rarity < rarityColors.Length)
            {
                rarityOverlayImage.color = rarityColors[(int)_itemInstance.Def.rarity];
            }
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnPurchase);
                UpdatePurchaseButtonState();
            }
        }
    }

    private void UpdatePurchaseButtonState()
    {
        if (_itemInstance == null || buyButton == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }
        buyButton.interactable = GameSession.Economy.Gold >= _itemInstance.Def.Cost;
    }

    public void OnPurchase()
    {
        if (_itemInstance == null) return;

        // We need to find the ShopManager to handle the purchase logic
        ShopManager shopManager = FindAnyObjectByType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.BuyItem(_itemInstance.Def);
        }
        else
        {
            Debug.LogError("ShopManager not found in scene!");
        }
    }
}
