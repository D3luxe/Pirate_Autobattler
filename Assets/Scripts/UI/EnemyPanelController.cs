using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.UI; // Make sure this namespace is correct for ShipPanelView

/// <summary>
/// Manages the enemy's UI panel in the battle scene.
/// It listens to an enemy's ShipState and updates the UI to reflect changes in health and equipment.
/// </summary>
public class EnemyPanelController : MonoBehaviour
{
    [Tooltip("The UIDocument component for the enemy panel.")]
    [SerializeField] private UIDocument _enemyPanelDocument;

    private ShipState _enemyShipState;

    // UI Views
    private ShipPanelView _shipPanelView;
    private VisualElement _equipmentBar;

    /// <summary>
    /// Initializes the controller with the enemy's ship state and sets up UI bindings.
    /// This method should be called by the CombatController when a battle starts.
    /// </summary>
    /// <param name="enemyShipState">The ShipState object for the enemy ship.</param>
    public void Initialize(ShipState enemyShipState)
    {
        _enemyShipState = enemyShipState;

        var root = _enemyPanelDocument.rootVisualElement;
        
        // Query for the instantiated ShipPanel and the equipment bar
        _shipPanelView = new ShipPanelView(root.Q<VisualElement>("ship-panel-instance"));
        _equipmentBar = root.Q<VisualElement>("equipment-bar");

        if (_enemyShipState != null)
        {
            // Register event handlers
            _enemyShipState.OnHealthChanged += HandleHealthChanged;
            _enemyShipState.OnEquipmentChanged += HandleEquipmentChanged;

            // Initial UI setup
            UpdateAllUI();
        }
        else
        {
            Debug.LogError("EnemyPanelController initialized with a null ShipState.");
        }
    }

    private void OnDestroy()
    {
        // Unregister event handlers to prevent memory leaks
        if (_enemyShipState != null)
        {
            _enemyShipState.OnHealthChanged -= HandleHealthChanged;
            _enemyShipState.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    /// <summary>
    /// Updates all UI elements to reflect the current state of the enemy ship.
    /// </summary>
    private void UpdateAllUI()
    {
        HandleHealthChanged();
        HandleEquipmentChanged();
        _shipPanelView.SetShipName(_enemyShipState.Def.displayName);
        // The sprite is often set from the ShipSO in a more complex system,
        // but we can set it here if the SO has a direct reference.
        // _shipPanelView.SetShipSprite(_enemyShipState.Def.shipSprite);
    }

    /// <summary>
    /// Handles changes to the enemy's health and updates the health bar.
    /// </summary>
    private void HandleHealthChanged()
    {
        if (_enemyShipState != null && _shipPanelView != null)
        {
            _shipPanelView.UpdateHealth(_enemyShipState.CurrentHealth, _enemyShipState.Def.baseMaxHealth);
        }
    }

    /// <summary>
    /// Handles changes to the enemy's equipment and updates the equipment bar UI.
    /// </summary>
    private void HandleEquipmentChanged()
    {
        if (_enemyShipState != null && _equipmentBar != null)
        {
            // Clear existing equipment icons
            _equipmentBar.Clear();

            // Populate with current equipment
            foreach (var itemInstance in _enemyShipState.Equipped)
            {
                var slot = new VisualElement();
                slot.AddToClassList("slot"); 

                if (itemInstance != null && itemInstance.Def != null)
                {
                    var icon = new Image { sprite = itemInstance.Def.icon };
                    icon.AddToClassList("slot__icon");
                    slot.Add(icon);
                }
                _equipmentBar.Add(slot);
            }
        }
    }
}