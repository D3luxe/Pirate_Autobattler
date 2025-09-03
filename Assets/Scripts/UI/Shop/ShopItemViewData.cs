using UnityEngine;
using System.ComponentModel;
using PirateRoguelike.Data;

namespace PirateRoguelike.UI
{
    public class ShopItemViewData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Sprite _icon;
        public Sprite Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }

        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChanged(nameof(ItemName));
                }
            }
        }

        private string _itemDescription;
        public string ItemDescription
        {
            get => _itemDescription;
            set
            {
                if (_itemDescription != value)
                {
                    _itemDescription = value;
                    OnPropertyChanged(nameof(ItemDescription));
                }
            }
        }

        private string _itemCost;
        public string ItemCost
        {
            get => _itemCost;
            set
            {
                if (_itemCost != value)
                {
                    _itemCost = value;
                    OnPropertyChanged(nameof(ItemCost));
                }
            }
        }

        private Color _rarityColor;
        public Color RarityColor
        {
            get => _rarityColor;
            set
            {
                if (_rarityColor != value)
                {
                    _rarityColor = value;
                    OnPropertyChanged(nameof(RarityColor));
                }
            }
        }

        private bool _isPurchasable;
        public bool IsPurchasable
        {
            get => _isPurchasable;
            set
            {
                if (_isPurchasable != value)
                {
                    _isPurchasable = value;
                    OnPropertyChanged(nameof(IsPurchasable));
                }
            }
        }

        // NEW: Rarity property
        private string _rarity;
        public string Rarity
        {
            get => _rarity;
            set
            {
                if (_rarity != value)
                {
                    _rarity = value;
                    OnPropertyChanged(nameof(Rarity));
                }
            }
        }

        private ItemSO _itemSO; // Reference to the original ItemSO for purchase logic
        public ItemSO ItemSO => _itemSO;

        // NEW: ShopSlotId to identify the item's position in the shop
        public int ShopSlotId { get; private set; }

        // NEW: CurrentItemInstance to satisfy ISlotViewData
        private ItemInstance _currentItemInstance;
        public ItemInstance CurrentItemInstance
        {
            get => _currentItemInstance;
            private set
            {
                if (_currentItemInstance != value)
                {
                    _currentItemInstance = value;
                    OnPropertyChanged(nameof(CurrentItemInstance));
                }
            }
        }

        public ShopItemViewData(ItemSO itemSO, Color rarityColor, bool isPurchasable, int shopSlotId) // Modified constructor
        {
            _itemSO = itemSO;
            Icon = itemSO.icon;
            ItemName = itemSO.displayName;
            ItemDescription = itemSO.description;
            ItemCost = itemSO.Cost.ToString() + " Gold";
            RarityColor = rarityColor;
            IsPurchasable = isPurchasable;
            ShopSlotId = shopSlotId; // Set the new property
            CurrentItemInstance = new ItemInstance(itemSO); // Initialize CurrentItemInstance
            Rarity = itemSO.rarity.ToString(); // Populate Rarity
        }
    }
}
