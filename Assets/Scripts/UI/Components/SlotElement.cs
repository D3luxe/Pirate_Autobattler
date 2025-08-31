using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI.Components
{
    public partial class SlotElement : VisualElement
    {
        public SlotManipulator Manipulator { get; set; }

        private Image _slotIcon;
        private Label _slotRarityLabel;
        private VisualElement _cooldownOverlay;
        private ProgressBar _cooldownBar;
        private VisualElement _disabledOverlay;

        private ISlotViewData _viewModel;

        public SlotElement()
        {
            // Load UXML and add to hierarchy
            var visualTree = UIManager.Instance.SlotElementUXML;
            visualTree.CloneTree(this);

            this.AddToClassList("slot"); // Add the "slot" class to the SlotElement itself

            // Query elements here, after the UXML has been cloned
            _slotIcon = this.Q<Image>("icon");
            _slotRarityLabel = this.Q<Label>("slot-id-label");
            _cooldownOverlay = this.Q<VisualElement>("cooldown-overlay");
            //_cooldownBar = this.Q<ProgressBar>("cooldown-bar");
            //_disabledOverlay = this.Q<VisualElement>("disabled-overlay");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            //Debug.Log($"SlotElement.OnAttachToPanel: UserData is {this.userData?.GetType().Name ?? "NULL"}. SlotId: {(this.userData as ISlotViewData)?.SlotId ?? -1}");
            // Initial UI update after elements are queried
            UpdateUI();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Clean up references if needed
            _slotIcon = null;
            _slotRarityLabel = null;
            _cooldownOverlay = null;
            //_cooldownBar = null;
            _disabledOverlay = null;

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
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUI(e.PropertyName);
        }

        private void UpdateUI(string propertyName = null)
        {
            if (_viewModel == null) return;

            if (propertyName == null || propertyName == nameof(ISlotViewData.Icon))
            {
                _slotIcon.sprite = _viewModel.Icon;
                if (_viewModel.IsEmpty)
                {
                    this.AddToClassList("slot--empty");
                }
                else
                {
                    this.RemoveFromClassList("slot--empty");
                }
            }
            if (propertyName == null || propertyName == nameof(ISlotViewData.Rarity))
            {
                _slotRarityLabel.text = _viewModel.Rarity;
            }
            if (propertyName == null || propertyName == nameof(ISlotViewData.CooldownPercent))
            {
                //_cooldownBar.value = _viewModel.CooldownPercent;
                _cooldownOverlay.style.visibility = (_viewModel.CooldownPercent > 0 && _viewModel.CooldownPercent < 1) ? Visibility.Visible : Visibility.Hidden;
            }
            if (propertyName == null || propertyName == nameof(ISlotViewData.IsDisabled))
            {
                //_disabledOverlay.style.visibility = _viewModel.IsDisabled ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}
