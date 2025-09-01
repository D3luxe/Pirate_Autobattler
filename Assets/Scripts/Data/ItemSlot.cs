using System.ComponentModel;
using PirateRoguelike.Data;

namespace PirateRoguelike.Data
{
    public class ItemSlot : INotifyPropertyChanged
    {
        private ItemInstance _item;
        public ItemInstance Item
        {
            get => _item;
            set
            {
                if (_item != value)
                {
                    _item = value;
                    OnPropertyChanged(nameof(Item));
                    OnPropertyChanged(nameof(IsEmpty)); // Notify that IsEmpty might have changed
                }
            }
        }

        public bool IsEmpty => _item == null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ItemSlot(ItemInstance item = null)
        {
            Item = item;
        }
    }
}