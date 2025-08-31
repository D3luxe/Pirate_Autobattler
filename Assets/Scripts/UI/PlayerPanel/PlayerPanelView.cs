using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq; // Added
using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.UI.Components; // Added for ShipDisplayElement and SlotElement
using PirateRoguelike.UI.Utilities; // Added for BindingUtility
using PirateRoguelike.Shared; // Added for ObservableList

namespace PirateRoguelike.UI
{
    public class PlayerPanelView
    {
        private VisualElement _root;
        
        private PlayerUIThemeSO _theme;
        private int _battleSpeed = 1;

        // --- Queried Elements ---
        private ShipDisplayElement _shipDisplayElement; // Replaced ShipPanelView
        private VisualElement _equipmentBar;
        private VisualElement _inventoryContainer;
        private VisualElement _mainContainer;
        
        private Label _goldLabel, _livesLabel, _depthLabel;
        private Button _pauseButton, _settingsButton, _battleSpeedButton, _mapToggleButton;
        private Image _battleSpeedIcon;

        private GameObject _ownerGameObject;

        public PlayerPanelView(VisualElement root, PlayerUIThemeSO theme, GameObject ownerGameObject)
        {
            _root = root;
            _theme = theme;
            _ownerGameObject = ownerGameObject;
            // Removed: _root.pickingMode = PickingMode.Ignore;
            QueryElements();
            RegisterCallbacks();
        }

        private void QueryElements()
        {
            _mainContainer = _root.Q("main-container");
            if (_mainContainer != null) _mainContainer.pickingMode = PickingMode.Ignore;

            // Instantiate ShipDisplayElement directly as it's no longer a UxmlElement
            _shipDisplayElement = new ShipDisplayElement();
            _root.Q("left-column").Add(_shipDisplayElement); // Assuming "left-column" is the correct parent

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
            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked += () => PlayerPanelEvents.OnMapToggleClicked?.Invoke();
            }
        }

        public void BindInitialData(IPlayerPanelData data)
        {
            // Bind Ship Data
            _shipDisplayElement.Bind(data.ShipData);

            // Bind HUD Data using BindingUtility
            BindingUtility.BindLabelText(_goldLabel, data.HudData, nameof(data.HudData.Gold));
            BindingUtility.BindLabelText(_livesLabel, data.HudData, nameof(data.HudData.Lives));
            BindingUtility.BindLabelText(_depthLabel, data.HudData, nameof(data.HudData.Depth));

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

        public void UpdateEquipment(ObservableList<ISlotViewData> slots)
        {
            BindSlots(_equipmentBar, slots);
        }

        public void UpdatePlayerInventory(ObservableList<ISlotViewData> slots)
        {
            BindSlots(_inventoryContainer, slots);
        }

        private void BindSlots(VisualElement container, ObservableList<ISlotViewData> slots)
        {
            // Clear existing elements and populate initially
            container.Clear();
            foreach (var slotData in slots)
            {
                SlotElement newSlotElement = CreateSlotElement(slotData);
                container.Add(newSlotElement);
                SlotManipulator newManipulator = new SlotManipulator(slotData);
                newSlotElement.AddManipulator(newManipulator);
                newSlotElement.Manipulator = newManipulator; // Assign to new property
            }

            // Subscribe to collection changes
            slots.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (ISlotViewData newItem in args.NewItems)
                        {
                            container.Insert(args.NewStartingIndex, CreateSlotElement(newItem));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (ISlotViewData oldItem in args.OldItems)
                        {
                            // Find and remove the corresponding VisualElement
                            var elementToRemove = container.Children().FirstOrDefault(e => e.userData == oldItem);
                            if (elementToRemove != null)
                            {
                                // Dispose manipulator before removing element
                                if (elementToRemove is SlotElement oldSlotElement && oldSlotElement.Manipulator != null)
                                {
                                    oldSlotElement.Manipulator.Dispose(); // Call Dispose
                                }
                                container.Remove(elementToRemove);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // For replace, remove old and add new at the same index
                        foreach (ISlotViewData oldItem in args.OldItems)
                        {
                            var elementToRemove = container.Children().FirstOrDefault(e => e.userData == oldItem);
                            if (elementToRemove != null)
                            {
                                // Dispose manipulator before removing element
                                if (elementToRemove is SlotElement oldSlotElement && oldSlotElement.Manipulator != null)
                                {
                                    oldSlotElement.Manipulator.Dispose(); // Call Dispose
                                }
                                container.Remove(elementToRemove);
                            }
                        }
                        foreach (ISlotViewData newItem in args.NewItems)
                        {
                            SlotElement newSlotElement = CreateSlotElement(newItem);
                            container.Insert(args.NewStartingIndex, newSlotElement);
                            SlotManipulator newManipulator = new SlotManipulator(newItem);
                            newSlotElement.AddManipulator(newManipulator);
                            newSlotElement.Manipulator = newManipulator; // Assign to new property
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        // Remove and re-insert for move
                        var elementToMove = container.Children().FirstOrDefault(e => e.userData == args.OldItems[0]);
                        if (elementToMove != null)
                        {
                            container.Remove(elementToMove);
                            container.Insert(args.NewStartingIndex, elementToMove);
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        // Clear all and re-add
                        // Dispose manipulators from all existing elements before clearing
                        foreach (var child in container.Children().ToList()) // ToList to avoid modifying collection while iterating
                        {
                            if (child is SlotElement oldSlotElement && oldSlotElement.Manipulator != null)
                            {
                                oldSlotElement.Manipulator.Dispose(); // Call Dispose
                            }
                        }
                        container.Clear();
                        foreach (var slotData in slots)
                        {
                            SlotElement newSlotElement = CreateSlotElement(slotData);
                            container.Add(newSlotElement);
                            SlotManipulator newManipulator = new SlotManipulator(slotData);
                            newSlotElement.AddManipulator(newManipulator);
                            newSlotElement.Manipulator = newManipulator; // Assign to new property
                        }
                        break;
                }
            };
        }

        private SlotElement CreateSlotElement(ISlotViewData slotData)
        {
            SlotElement slotElement = new SlotElement();
            slotElement.userData = slotData; // Store view model in userData for easy lookup
            //Debug.Log($"CreateSlotElement: Setting userData for slot {slotData.SlotId}. IsEmpty: {slotData.IsEmpty}. UserData type: {slotElement.userData.GetType().Name}");
            // Manipulator is now added in BindSlots after element is added to container
            slotElement.Bind(slotData);

            // Register PointerEnter and PointerLeave events for tooltip
            TooltipUtility.RegisterTooltipCallbacks(slotElement, slotData);
            return slotElement;
        }

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
