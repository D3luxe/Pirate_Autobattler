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

        private ItemSO _itemSO; // Reference to the original ItemSO for purchase logic
        public ItemSO ItemSO => _itemSO;

        public ShopItemViewData(ItemSO itemSO, Color rarityColor, bool isPurchasable)
        {
            _itemSO = itemSO;
            Icon = itemSO.icon;
            ItemName = itemSO.displayName;
            ItemDescription = itemSO.description;
            ItemCost = itemSO.Cost.ToString() + " Gold";
            RarityColor = rarityColor;
            IsPurchasable = isPurchasable;
        }
    }
}
