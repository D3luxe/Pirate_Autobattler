using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    /// <summary>
    /// A view class that encapsulates the logic for a Ship Panel UI.
    /// It queries for the necessary elements and provides methods to update them.
    /// </summary>
    public class ShipPanelView
    {
        private Label _shipNameLabel;
        private Image _shipSpriteElement;
        private VisualElement _hpBarForeground;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipPanelView"/> class.
        /// </summary>
        /// <param name="rootElement">The root VisualElement of the ship panel (e.g., the 'ship-panel-instance').</param>
        public ShipPanelView(VisualElement rootElement)
        {
            if (rootElement == null)
            {
                Debug.LogError("ShipPanelView rootElement is null. Cannot query elements.");
                return;
            }
            
            _shipNameLabel = rootElement.Q<Label>("ship-name");
            _shipSpriteElement = rootElement.Q<Image>("ship-sprite");
            _hpBarForeground = rootElement.Q<VisualElement>("hp-bar__foreground");
        }

        /// <summary>
        /// Sets the display name of the ship.
        /// </summary>
        public void SetShipName(string name)
        {
            if (_shipNameLabel != null)
            {
                _shipNameLabel.text = name;
            }
        }

        /// <summary>
        /// Sets the sprite image of the ship.
        /// </summary>
        public void SetShipSprite(Sprite sprite)
        {
            if (_shipSpriteElement != null)
            {
                _shipSpriteElement.sprite = sprite;
            }
        }

        /// <summary>
        /// Updates the health bar based on current and maximum health.
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            if (_hpBarForeground != null)
            {
                float percentage = (max > 0) ? (current / max) * 100f : 0f;
                _hpBarForeground.style.width = new Length(percentage, LengthUnit.Percent);
            }
        }
    }
}
