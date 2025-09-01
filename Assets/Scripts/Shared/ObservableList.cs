using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PirateRoguelike.Shared
{
    public class ObservableList<T> : ObservableCollection<T>
    {
        public ObservableList()
        {
        }

        public ObservableList(System.Collections.Generic.IEnumerable<T> collection) : base(collection)
        {
        }

        // Override SetItem to raise PropertyChanged for the indexer
        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        // Override InsertItem to raise PropertyChanged for Count and the indexer
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        // Override RemoveItem to raise PropertyChanged for Count and the indexer
        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        // Override ClearItems to raise PropertyChanged for Count and the indexer
        protected override void ClearItems()
        {
            base.ClearItems();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
