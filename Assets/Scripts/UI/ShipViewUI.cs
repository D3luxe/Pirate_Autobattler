using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Data;
using System.ComponentModel;

namespace PirateRoguelike.UI
{
    [UxmlElement]
    public partial class ShipViewUI : VisualElement
{
    private VisualElement _shipIcon;
    private Label _shipName;
    private Label _healthStat;

    private IShipViewData _viewModel;

    public ShipViewUI()
    {
        // Query elements
        _shipIcon = this.Q<VisualElement>("ShipIcon");
        _shipName = this.Q<Label>("ShipName");
        _healthStat = this.Q<Label>("HealthStat");
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
            UpdateUI();
        }
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
            _shipName.text = _viewModel.ShipName;
        }
        if (propertyName == null || propertyName == nameof(IShipViewData.CurrentHp) || propertyName == nameof(IShipViewData.MaxHp))
        {
            _healthStat.text = $"HP: {_viewModel.CurrentHp}/{_viewModel.MaxHp}";
        }
        if (propertyName == null || propertyName == nameof(IShipViewData.ShipSprite))
        {
            if (_viewModel.ShipSprite != null)
            {
                _shipIcon.style.backgroundImage = new StyleBackground(_viewModel.ShipSprite.texture);
            }
            else
            {
                _shipIcon.style.backgroundImage = null;
            }
        }
    }
}
}
