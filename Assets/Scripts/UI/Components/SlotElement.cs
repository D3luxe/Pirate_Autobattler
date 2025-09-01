using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI.Components
{
    public partial class SlotElement : VisualElement
    {
        public SlotManipulator Manipulator { get; set; }

        private ISlotViewData _viewModel;
        private ItemElement _itemElement; // Added

        public SlotElement()
        {
            // Load UXML and add to hierarchy
            var visualTree = UIManager.Instance.SlotElementUXML;
            visualTree.CloneTree(this);

            this.AddToClassList("slot"); // Add the "slot" class to the SlotElement itself

            // ItemElement will be created and added by PlayerPanelView.CreateSlotElement

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // No need to query elements here anymore, they are part of ItemElement
            // Initial UI update after elements are queried
            // UpdateUI(); // No longer needed, ItemElement handles its own updates
            
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Unsubscribe from view model property changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            // Dispose manipulator if it exists
            if (Manipulator != null)
            {
                Manipulator.Dispose();
                Manipulator = null;
            }
        }

        public void Bind(ISlotViewData viewModel)
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
            
            UpdateItemElement(); // Call this to update the ItemElement based on the new view model
            
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Debug.Log($"SlotElement.OnViewModelPropertyChanged: Property changed: {e.PropertyName}");
            if (e.PropertyName == nameof(ISlotViewData.CurrentItemInstance))
            {
                UpdateItemElement();
            }
        }

        private void UpdateItemElement()
        {
            if (_viewModel.CurrentItemInstance != null)
            {
                // If there's an item, ensure ItemElement exists and is bound
                if (_itemElement == null)
                {
                    _itemElement = new ItemElement();
                    this.Add(_itemElement);
                    // Create and add the SlotManipulator to the ItemElement
                    Manipulator = new SlotManipulator(_itemElement); // Assign to SlotElement's Manipulator property
                    _itemElement.AddManipulator(Manipulator);
                }
                _itemElement.SlotViewData = _viewModel; // Assign slotData to ItemElement
                _itemElement.Bind(_viewModel.CurrentItemInstance); // Bind ItemElement to ItemInstance
            }
            else
            {
                // If no item, remove ItemElement if it exists
                if (_itemElement != null)
                {
                    _itemElement.RemoveFromHierarchy();
                    // Dispose manipulator
                    if (Manipulator != null)
                    {
                        Manipulator.Dispose();
                        Manipulator = null;
                    }
                    _itemElement = null;
                }
            }
        }
    }
}
