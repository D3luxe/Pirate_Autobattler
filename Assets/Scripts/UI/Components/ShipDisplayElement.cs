using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;

namespace PirateRoguelike.UI.Components
{
    public partial class ShipDisplayElement : VisualElement
    { 

        private Label _shipNameLabel;
        private VisualElement _shipHpBar;
        private VisualElement _hpBarForeground;
        private Label _shipHpLabel;
        private Image _shipSprite;

        private IShipViewData _viewModel;

        

        public ShipDisplayElement()
        {
            // Load UXML and add to hierarchy
            var visualTree = UIManager.Instance.ShipDisplayElementUXML;
            visualTree.CloneTree(this);

            // Query elements here, after the UXML has been cloned
            _shipNameLabel = this.Q<Label>("ship-name");
            _shipHpBar = this.Q<VisualElement>("hp-bar");
            _hpBarForeground = _shipHpBar.Q<VisualElement>("hp-bar__foreground");
            _shipHpLabel = this.Q<Label>("hp-label");
            _shipSprite = this.Q<Image>("ship-sprite");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Initial UI update after elements are queried
            UpdateUI();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Clean up references if needed
            _shipNameLabel = null;
            _shipHpBar = null;
            _shipHpLabel = null;
            _shipSprite = null;

            // Unsubscribe from view model property changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        }

        public void Bind(IShipViewData viewModel)
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

            UpdateUI(); // Immediately update UI with the new view model state
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateUI(e.PropertyName);
        }

        private void UpdateUI(string propertyName = null)
        {
            if (_viewModel == null) return;

            if (propertyName == null || propertyName == nameof(IShipViewData.ShipName))
            {
                _shipNameLabel.text = _viewModel.ShipName;
            }
            if (propertyName == null || propertyName == nameof(IShipViewData.CurrentHp) || propertyName == nameof(IShipViewData.MaxHp))
            {
                float hpPercentage = _viewModel.MaxHp > 0 ? (_viewModel.CurrentHp / _viewModel.MaxHp) : 0;
                _hpBarForeground.style.width = new Length(hpPercentage * 100, LengthUnit.Percent);
                _shipHpLabel.text = $"{_viewModel.CurrentHp}/{_viewModel.MaxHp}";
            }
            if (propertyName == null || propertyName == nameof(IShipViewData.ShipSprite))
            {
                _shipSprite.sprite = _viewModel.ShipSprite;
            }
        }
    }
}
