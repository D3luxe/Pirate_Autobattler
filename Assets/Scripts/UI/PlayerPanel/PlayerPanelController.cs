using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Added for ItemInstance
using PirateRoguelike.Runtime; // Added for RuntimeItem
using PirateRoguelike.Shared; // Added for ObservableList

namespace PirateRoguelike.UI
{
    // This is the adapter class that converts game data into view data.
    public class PlayerPanelDataViewModel : IPlayerPanelData, IShipViewData, IHudViewData, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        // IShipViewData
        public string ShipName => GameSession.PlayerShip.Def.displayName;
        public Sprite ShipSprite => GameSession.PlayerShip.Def.art; // Corrected
        private float _currentHp;
        public float CurrentHp
        {
            get => _currentHp;
            set
            {
                if (_currentHp != value)
                {
                    _currentHp = value;
                    OnPropertyChanged(nameof(CurrentHp));
                }
            }
        }
        public float MaxHp => GameSession.PlayerShip.Def.baseMaxHealth;

        // IHudViewData
        private int _gold;
        public int Gold
        {
            get => _gold;
            set
            {
                if (_gold != value)
                {
                    _gold = value;
                    OnPropertyChanged(nameof(Gold));
                }
            }
        }
        private int _lives;
        public int Lives
        {
            get => _lives;
            set
            {
                if (_lives != value)
                {
                    _lives = value;
                    OnPropertyChanged(nameof(Lives));
                }
            }
        }
        private int _depth;
        public int Depth
        {
            get => _depth;
            set
            {
                if (_depth != value)
                {
                    _depth = value;
                    OnPropertyChanged(nameof(Depth));
                }
            }
        }

        // IPlayerPanelData
        public IShipViewData ShipData => this;
        public IHudViewData HudData => this;
        private ObservableList<ISlotViewData> _equipmentSlots;
        private ObservableList<ISlotViewData> _inventorySlots;

        public ObservableList<ISlotViewData> EquipmentSlots => _equipmentSlots;
        public ObservableList<ISlotViewData> InventorySlots => _inventorySlots;

        public PlayerPanelDataViewModel()
        {
            _equipmentSlots = new ObservableList<ISlotViewData>();
            _inventorySlots = new ObservableList<ISlotViewData>();
        }

        public void Initialize()
        {
            // Initial population
            UpdateEquipmentSlots();
            UpdateInventorySlots();

            // Subscribe to GameSession events
            GameSession.PlayerShip.OnEquipmentChanged += UpdateEquipmentSlots;
            GameSession.Inventory.OnInventoryChanged += UpdateInventorySlots;
        }

        private void UpdateEquipmentSlots()
        {
            _equipmentSlots.Clear();
            foreach (var slotVm in GameSession.PlayerShip.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>()) 
            {
                _equipmentSlots.Add(slotVm);
            }
        }

        private void UpdateInventorySlots()
        {
            _inventorySlots.Clear();
            foreach (var slotVm in GameSession.Inventory.Items.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>()) 
            {
                _inventorySlots.Add(slotVm);
            }
        }
    }

    public class SlotDataViewModel : ISlotViewData, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        private readonly ItemInstance _item;
        private readonly int _index;

        public SlotDataViewModel(ItemInstance item, int index)
        {
            _item = item;
            _index = index;
        }

        public int SlotId => _index;
        public Sprite Icon => _item?.Def.icon;
        public string Rarity => _item?.Def.rarity.ToString();
        public bool IsEmpty => _item == null;
        public bool IsDisabled => false; // TODO: Hook up stun/disable logic

        private float _cooldownPercent;
        public float CooldownPercent
        {
            get => _cooldownPercent;
            private set
            {
                if (_cooldownPercent != value)
                {
                    _cooldownPercent = value;
                    OnPropertyChanged(nameof(CooldownPercent));
                }
            }
        }
        public bool IsPotentialMergeTarget => false; // TODO: Hook up merge logic
        public RuntimeItem ItemData => _item?.RuntimeItem;
    }


    [RequireComponent(typeof(UIDocument))]
    public class PlayerPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme;
        private MapView _mapView;

        private PlayerPanelView _panelView;
        private PlayerPanelDataViewModel _viewModel = new PlayerPanelDataViewModel();
        private Button _mapToggleButton;

        public void SetMapPanel(MapView mapView)
        {
            _mapView = mapView;
        }

        public void Initialize()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panelView = new PlayerPanelView(root, _theme, this.gameObject);

            // Get reference to the MapToggle button
            _mapToggleButton = root.Q<Button>("MapToggle");
            if (_mapToggleButton == null) Debug.LogError("Button 'MapToggle' not found in UXML.");

            // Register click event
            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked += OnMapToggleButtonClicked;
            }

            // Subscribe to game events
            
            
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged += HandleInventoryChanged;
            if (GameSession.PlayerShip != null) 
            {
                
                GameSession.PlayerShip.OnEquipmentChanged += HandleEquipmentChanged;
            }

            // Subscribe to UI events
            PlayerPanelEvents.OnSlotDropped += HandleSlotDropped;
            PlayerPanelEvents.OnMapToggleClicked += HandleMapToggleClicked;

            _viewModel.Initialize(); // Initialize the view model after GameSession is ready
            // Initial data bind
            _panelView.BindInitialData(_viewModel);
        }

        void OnDestroy()
        {
            // Unsubscribe from all events
            
            
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged -= HandleInventoryChanged;
            if (GameSession.PlayerShip != null) 
            {
                
                GameSession.PlayerShip.OnEquipmentChanged -= HandleEquipmentChanged;
            }

            PlayerPanelEvents.OnSlotDropped -= HandleSlotDropped;
            PlayerPanelEvents.OnMapToggleClicked -= HandleMapToggleClicked;

            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked -= OnMapToggleButtonClicked;
            }
        }

        // --- Event Handlers ---

        
        
        
        private void HandleEquipmentChanged()
        {
            // The ObservableList in PlayerPanelDataViewModel now handles UI updates
        }
        private void HandleInventoryChanged()
        {
            // The ObservableList in PlayerPanelDataViewModel now handles UI updates
        }
        private void HandleSlotDropped(int fromSlotId, SlotContainerType fromContainer, int toSlotId, SlotContainerType toContainer)
        {
            Debug.Log($"HandleSlotDropped: From {fromContainer} slot {fromSlotId} to {toContainer} slot {toSlotId}");

            if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Inventory)
            {
                Debug.Log($"HandleSlotDropped: Swapping items within inventory: {fromSlotId} and {toSlotId}");
                GameSession.Inventory.SwapItems(fromSlotId, toSlotId);
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Equipment)
            {
                Debug.Log($"HandleSlotDropped: Swapping items within equipment: {fromSlotId} and {toSlotId}");
                GameSession.PlayerShip.SwapEquipment(fromSlotId, toSlotId); // Use SwapEquipment
            }
            else if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Equipment)
            {
                Debug.Log($"HandleSlotDropped: Equipping item from inventory slot {fromSlotId} to equipment slot {toSlotId}");
                ItemInstance itemToEquip = GameSession.Inventory.GetItemAt(fromSlotId);
                Debug.Log($"HandleSlotDropped: Item to equip: {itemToEquip?.Def.displayName ?? "NULL"}");
                if (itemToEquip != null)
                {
                    ItemInstance equippedItem = GameSession.PlayerShip.GetEquippedItem(toSlotId);
                    Debug.Log($"HandleSlotDropped: Currently equipped item at {toSlotId}: {equippedItem?.Def.displayName ?? "NULL"}");
                    if (equippedItem != null)
                    {
                        Debug.Log($"HandleSlotDropped: Equipment slot {toSlotId} is occupied. Swapping {itemToEquip.Def.displayName} with {equippedItem.Def.displayName}.");
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.SetItemAt(fromSlotId, equippedItem);
                    }
                    else
                    {
                        Debug.Log($"HandleSlotDropped: Equipment slot {toSlotId} is empty. Moving {itemToEquip.Def.displayName}.");
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.RemoveItemAt(fromSlotId);
                    }

                    Debug.Log($"HandleSlotDropped: After equip - Equipped slot {toSlotId} has {GameSession.PlayerShip.GetEquippedItem(toSlotId)?.Def.displayName ?? "NULL"}");
                    Debug.Log($"HandleSlotDropped: After equip - Inventory slot {fromSlotId} has {GameSession.Inventory.GetItemAt(fromSlotId)?.Def.displayName ?? "NULL"}");
                }
                else
                {
                    Debug.LogWarning($"HandleSlotDropped: Item to equip from inventory slot {fromSlotId} was NULL.");
                }
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Inventory)
            {
                Debug.Log($"HandleSlotDropped: Unequipping from equipment slot {fromSlotId} to inventory slot {toSlotId}");
                ItemInstance itemToUnequip = GameSession.PlayerShip.GetEquippedItem(fromSlotId);
                Debug.Log($"HandleSlotDropped: Item to unequip: {itemToUnequip?.Def.displayName ?? "NULL"}");

                if (itemToUnequip != null)
                {
                    ItemInstance inventoryItem = GameSession.Inventory.GetItemAt(toSlotId);
                    Debug.Log($"HandleSlotDropped: Currently in inventory slot {toSlotId}: {inventoryItem?.Def.displayName ?? "NULL"}");

                    if (inventoryItem != null)
                    {
                        Debug.Log($"HandleSlotDropped: Inventory slot {toSlotId} is occupied. Swapping {itemToUnequip.Def.displayName} with {inventoryItem.Def.displayName}.");
                        GameSession.Inventory.SetItemAt(toSlotId, itemToUnequip);
                        GameSession.PlayerShip.SetEquippedAt(fromSlotId, inventoryItem);
                    }
                    else
                    {
                        Debug.Log($"HandleSlotDropped: Inventory slot {toSlotId} is empty. Moving {itemToUnequip.Def.displayName}.");
                        GameSession.Inventory.AddItemAt(itemToUnequip, toSlotId);
                        GameSession.PlayerShip.RemoveEquippedAt(fromSlotId);
                    }

                    Debug.Log($"HandleSlotDropped: After unequip - Equipped slot {fromSlotId} has {GameSession.PlayerShip.GetEquippedItem(fromSlotId)?.Def.displayName ?? "NULL"}");
                    Debug.Log($"HandleSlotDropped: After unequip - Inventory slot {toSlotId} has {GameSession.Inventory.GetItemAt(toSlotId)?.Def.displayName ?? "NULL"}");
                }
                else
                {
                    Debug.LogWarning($"HandleSlotDropped: Item to unequip from equipment slot {fromSlotId} was NULL.");
                }
            }
            else
            {
                Debug.LogWarning($"Unhandled slot drop: From {fromContainer} to {toContainer}");
            }
        }

        private void HandleMapToggleClicked()
        {
            if (_mapView.IsVisible())
            {
                _mapView.Hide();
            }
            else
            {
                _mapView.Show();
            }
        }

        // Implement OnMapToggleButtonClicked() Method
        private void OnMapToggleButtonClicked()
        {
            // Publish the event to toggle map visibility
            GameEvents.RequestMapToggle();
        }
    }
}