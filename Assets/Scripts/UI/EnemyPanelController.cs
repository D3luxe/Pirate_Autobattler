using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.UI; // Make sure this namespace is correct for ShipPanelView
using PirateRoguelike.Runtime; // NEW: Added for RuntimeItem

/// <summary>
/// Manages the enemy's UI panel in the battle scene.
/// It listens to an enemy's ShipState and updates the UI to reflect changes in health and equipment.
/// </summary>
public class EnemyPanelController : MonoBehaviour
{
    [Tooltip("The UIDocument component for the enemy panel.")]
    [SerializeField] private UIDocument _enemyPanelDocument;
    [SerializeField] private VisualTreeAsset _slotTemplate; // NEW: Slot UXML template
    [SerializeField] private PlayerUIThemeSO _theme; // NEW: Player UI Theme for slot visuals

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
            _equipmentBar.Clear();

            // Populate with current equipment
            foreach (var itemInstance in _enemyShipState.Equipped)
            {
                var slotInstance = _slotTemplate.Instantiate();
                var slotElement = slotInstance.Q<VisualElement>("slot");
                
                // Create SlotDataViewModel from RuntimeItem
                var slotData = new SlotDataViewModel(itemInstance, 0); // Index might not be relevant here, using 0 for now

                BindSlot(slotElement, slotData); // Bind visual data

                // Register PointerEnter and PointerLeave events for tooltip
                if (!slotData.IsEmpty && slotData.ItemData != null)
                {
                    var currentItemData = slotData.ItemData; // Capture the current item data
                    slotElement.RegisterCallback<PointerEnterEvent>(evt =>
                    {
                        TooltipController.Instance.Show(currentItemData, slotElement);
                    });
                    slotElement.RegisterCallback<PointerLeaveEvent>(evt =>
                    {
                        TooltipController.Instance.Hide();
                    });
                }
                _equipmentBar.Add(slotElement);
            }
        }
    }

    private void BindSlot(VisualElement slotElement, ISlotViewData slotData)
    {
        var icon = slotElement.Q<Image>("icon");
        icon.sprite = slotData.IsEmpty ? _theme.emptySlotBackground : slotData.Icon;
        icon.style.visibility = slotData.IsEmpty ? Visibility.Visible : Visibility.Visible;

        // Assign rarity frame from theme
        var rarityFrame = slotElement.Q<Image>("rarity-frame");
        if (!string.IsNullOrEmpty(slotData.Rarity))
        {
            switch (slotData.Rarity.ToLower())
            {
                case "bronze": rarityFrame.sprite = _theme.bronzeFrame; break;
                case "silver": rarityFrame.sprite = _theme.silverFrame; break;
                case "gold": rarityFrame.sprite = _theme.goldFrame; break;
                case "diamond": rarityFrame.sprite = _theme.diamondFrame; break;
            }
        }
        else
        {
            rarityFrame.sprite = null; // No frame for empty/unassigned rarity
        }

        slotElement.Q<VisualElement>("cooldown-overlay").style.scale = new Scale(new Vector2(1, slotData.CooldownPercent));

        slotElement.ClearClassList();
        slotElement.AddToClassList("slot");

        if (slotData.IsEmpty) slotElement.AddToClassList("slot--empty");
        if (slotData.IsDisabled) slotElement.AddToClassList("slot--disabled");
        if (slotData.IsPotentialMergeTarget) slotElement.AddToClassList("slot--merge");

        if (!string.IsNullOrEmpty(slotData.Rarity))
        {
            slotElement.AddToClassList($"rarity--{slotData.Rarity.ToLower()}");
        }
    }
}