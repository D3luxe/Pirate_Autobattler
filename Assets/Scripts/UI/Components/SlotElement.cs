using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI.Components
{
    public partial class SlotElement : VisualElement
    {
        public SlotManipulator Manipulator { get; set; }

        private ISlotViewData _viewModel;

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
            else
            {
                // Clear the ItemElement if it exists
                // This will be handled by PlayerPanelView now
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Log($"SlotElement.OnViewModelPropertyChanged: Property changed: {e.PropertyName}");
            // ItemElement handles its own property changes
            // This will be handled by PlayerPanelView now
        }

        }
}
