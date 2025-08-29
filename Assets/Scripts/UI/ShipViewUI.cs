using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Data;

[UxmlElement]
public partial class ShipViewUI : VisualElement
{

    private VisualElement _shipIcon;
    private Label _shipName;
    private Label _healthStat;

    public ShipViewUI()
    {
        // Query elements
        _shipIcon = this.Q<VisualElement>("ShipIcon");
        _shipName = this.Q<Label>("ShipName");
        _healthStat = this.Q<Label>("HealthStat");
    }

    public void Setup(VisualTreeAsset uxml, StyleSheet uss)
    {
        uxml.CloneTree(this);
        styleSheets.Add(uss);
    }

    public void SetShip(ShipSO ship)
    {
        _shipName.text = ship.displayName;
        _healthStat.text = $"HP: {ship.baseMaxHealth}";

        // Set ship icon (assuming ShipSO has a shipSprite field)
        if (ship.art != null)
        {
            _shipIcon.style.backgroundImage = new StyleBackground(ship.art.texture);
        }
    }
}