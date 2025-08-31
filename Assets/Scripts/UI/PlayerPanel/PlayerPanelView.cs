using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class PlayerPanelView
    {
        private VisualElement _root;
        private VisualTreeAsset _slotTemplate;
        private PlayerUIThemeSO _theme;
        private int _battleSpeed = 1;

        // --- Queried Elements ---
        private ShipPanelView _shipPanelView;
        private VisualElement _equipmentBar;
        private VisualElement _inventoryContainer; // New
        private VisualElement _mainContainer; // The actual panel background/container
        private List<VisualElement> _equipmentSlotElements = new List<VisualElement>();
        private List<VisualElement> _inventorySlotElements = new List<VisualElement>();
        private Label _goldLabel, _livesLabel, _depthLabel;
        private Button _pauseButton, _settingsButton, _battleSpeedButton, _mapToggleButton;
        private Image _battleSpeedIcon;

        private GameObject _ownerGameObject; // New field

        public PlayerPanelView(VisualElement root, VisualTreeAsset slotTemplate, PlayerUIThemeSO theme, GameObject ownerGameObject)
        {
            _root = root;
            //_root.BringToFront();
            _slotTemplate = slotTemplate;
            _theme = theme;
            _ownerGameObject = ownerGameObject; // Assign owner GameObject
            _root.pickingMode = PickingMode.Ignore; // Let clicks pass through the root container
            QueryElements();
            RegisterCallbacks();
        }

        private void QueryElements()
        {
            _mainContainer = _root.Q("main-container");
            if (_mainContainer != null) _mainContainer.pickingMode = PickingMode.Ignore;

            _shipPanelView = new ShipPanelView(_root.Q("ship-panel-instance"));

            var topBar = _root.Q("top-bar");
            _equipmentBar = topBar.Q("equipment-bar");
            _inventoryContainer = _root.Q("inventory-container");

            _goldLabel = _root.Q<Label>("gold-label");
            _livesLabel = _root.Q<Label>("lives-label");
            _depthLabel = _root.Q<Label>("depth-label");
            _pauseButton = _root.Q<Button>("pause-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _battleSpeedButton = _root.Q<Button>("battle-speed-button");
            _battleSpeedIcon = _battleSpeedButton.Q<Image>();
            _mapToggleButton = _root.Q<Button>("map-toggle-button");
        }

        private void RegisterCallbacks()
        {
            _pauseButton.clicked += () => PlayerPanelEvents.OnPauseClicked?.Invoke();
            _settingsButton.clicked += () => PlayerPanelEvents.OnSettingsClicked?.Invoke();
            _battleSpeedButton.clicked += ToggleBattleSpeed;
            if (_mapToggleButton != null) // Add null check
            {
                _mapToggleButton.clicked += () => PlayerPanelEvents.OnMapToggleClicked?.Invoke();
            }
        }

        public void BindInitialData(IPlayerPanelData data)
        {
            // Bind Ship Data
            _shipPanelView.SetShipName(data.ShipData.ShipName);
            _shipPanelView.SetShipSprite(data.ShipData.ShipSprite);
            _shipPanelView.UpdateHealth(data.ShipData.CurrentHp, data.ShipData.MaxHp);

            // Bind HUD Data
            UpdateGold(data.HudData.Gold);
            UpdateLives(data.HudData.Lives);
            UpdateDepth(data.HudData.Depth);

            // Set initial icons from theme
            _root.Q<Image>("gold-icon").sprite = _theme.goldIcon;
            _root.Q<Image>("lives-icon").sprite = _theme.livesIcon;
            _root.Q<Image>("depth-icon").sprite = _theme.depthIcon;
            _pauseButton.Q<Image>().sprite = _theme.pauseIcon;
            _settingsButton.Q<Image>().sprite = _theme.settingsIcon;
            UpdateBattleSpeedIcon();

            // Create and Bind Slots
            UpdateEquipment(data.EquipmentSlots);
            UpdatePlayerInventory(data.InventorySlots);
        }

        public void UpdateEquipment(List<ISlotViewData> slots)
        {
            //Debug.Log("PlayerPanelView: UpdateEquipment called.");
            PopulateSlots(_equipmentBar, _equipmentSlotElements, slots);
        }

        public void UpdatePlayerInventory(List<ISlotViewData> slots)
        {
            //Debug.Log("PlayerPanelView: UpdatePlayerInventory called.");
            PopulateSlots(_inventoryContainer, _inventorySlotElements, slots);
        }

        private void PopulateSlots(VisualElement container, List<VisualElement> slotElementCache, List<ISlotViewData> slots)
        {
            container.Clear();
            slotElementCache.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                var slotInstance = _slotTemplate.Instantiate();
                var slotElement = slotInstance.Q<VisualElement>("slot");
                slotElement.userData = slots[i]; // Store the entire ISlotViewData
                container.Add(slotElement);
                slotElementCache.Add(slotElement);

                slotElement.AddManipulator(new SlotManipulator(slots[i])); // Pass ISlotViewData to manipulator

                BindSlot(slotElement, slots[i]);

                // Register PointerEnter and PointerLeave events for tooltip for ALL slots
                var currentSlotData = slots[i]; // Capture the current slot data
                slotElement.RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (!currentSlotData.IsEmpty && currentSlotData.ItemData != null)
                    {
                        TooltipController.Instance.Show(currentSlotData.ItemData, slotElement);
                    }
                    else
                    {
                        // If entering an empty slot, ensure tooltip is hidden
                        TooltipController.Instance.Hide();
                    }
                });
                slotElement.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    if (TooltipController.Instance.IsTooltipVisible) // NEW: Check if tooltip is visible
                    {
                        TooltipController.Instance.Hide();
                    }
                });
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

        public void UpdateHp(float current, float max)
        {
            _shipPanelView.UpdateHealth(current, max);
        }

        public void UpdateGold(int amount) => _goldLabel.text = amount.ToString();
        public void UpdateLives(int amount) => _livesLabel.text = amount.ToString();
        public void UpdateDepth(int amount) => _depthLabel.text = amount.ToString();

        private void ToggleBattleSpeed()
        {
            _battleSpeed = _battleSpeed == 1 ? 2 : 1;
            UpdateBattleSpeedIcon();
            PlayerPanelEvents.OnBattleSpeedChanged?.Invoke(_battleSpeed);
        }

        private void UpdateBattleSpeedIcon()
        {
            _battleSpeedIcon.sprite = _battleSpeed == 1 ? _theme.battleSpeed1xIcon : _theme.battleSpeed2xIcon;
        }
    }
}