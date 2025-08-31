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
            var currentEquipped = GameSession.PlayerShip.Equipped;
            // Ensure _equipmentSlots has the correct count
            while (_equipmentSlots.Count < currentEquipped.Count())
            {
                _equipmentSlots.Add(null); // Add placeholders if new items are more than current slots
            }
            while (_equipmentSlots.Count > currentEquipped.Count())
            {
                _equipmentSlots.RemoveAt(_equipmentSlots.Count - 1); // Remove excess slots
            }

            for (int i = 0; i < currentEquipped.Count(); i++)
            {
                ISlotViewData newSlotData = new SlotDataViewModel(currentEquipped[i], i);
                // Check if the item has changed or if the slot is empty and now has an item
                if (_equipmentSlots[i] == null || 
                    _equipmentSlots[i].SlotId != newSlotData.SlotId || 
                    _equipmentSlots[i].IsEmpty != newSlotData.IsEmpty ||
                    (_equipmentSlots[i].ItemData == null && newSlotData.ItemData != null) || // Empty to filled
                    (_equipmentSlots[i].ItemData != null && newSlotData.ItemData == null) || // Filled to empty
                    (_equipmentSlots[i].ItemData != null && newSlotData.ItemData != null && ((SlotDataViewModel)_equipmentSlots[i]).ItemInstanceId != ((SlotDataViewModel)newSlotData).ItemInstanceId))
                {
                    _equipmentSlots[i] = newSlotData;
                }
            }
        }

        private void UpdateInventorySlots()
        {
            var currentInventory = GameSession.Inventory.Items;
            // Ensure _inventorySlots has the correct count
            while (_inventorySlots.Count < currentInventory.Count())
            {
                _inventorySlots.Add(null); // Add placeholders if new items are more than current slots
            }
            while (_inventorySlots.Count > currentInventory.Count())
            {
                _inventorySlots.RemoveAt(_inventorySlots.Count - 1); // Remove excess slots
            }

            for (int i = 0; i < currentInventory.Count(); i++)
            {
                ISlotViewData newSlotData = new SlotDataViewModel(currentInventory[i], i);
                // Check if the item has changed or if the slot is empty and now has an item
                if (_inventorySlots[i] == null || 
                    _inventorySlots[i].SlotId != newSlotData.SlotId || 
                    _inventorySlots[i].IsEmpty != newSlotData.IsEmpty ||
                    (_inventorySlots[i].ItemData == null && newSlotData.ItemData != null) || // Empty to filled
                    (_inventorySlots[i].ItemData != null && newSlotData.ItemData == null) || // Filled to empty
                    (_inventorySlots[i].ItemData != null && newSlotData.ItemData != null && ((SlotDataViewModel)_inventorySlots[i]).ItemInstanceId != ((SlotDataViewModel)newSlotData).ItemInstanceId))
                {
                    _inventorySlots[i] = newSlotData;
                }
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
        public string ItemInstanceId => _item?.Def.id; 
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
            if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Inventory)
            {
                GameSession.Inventory.SwapItems(fromSlotId, toSlotId);
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Equipment)
            {
                GameSession.PlayerShip.SwapEquipment(fromSlotId, toSlotId); // Use SwapEquipment
            }
            else if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Equipment)
            {
                ItemInstance itemToEquip = GameSession.Inventory.GetItemAt(fromSlotId);
                if (itemToEquip != null)
                {
                    ItemInstance equippedItem = GameSession.PlayerShip.GetEquippedItem(toSlotId);
                    if (equippedItem != null)
                    {
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.SetItemAt(fromSlotId, equippedItem);
                    }
                    else
                    {
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.RemoveItemAt(fromSlotId);
                    }
                }
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Inventory)
            {
                ItemInstance itemToUnequip = GameSession.PlayerShip.GetEquippedItem(fromSlotId);
                if (itemToUnequip != null)
                {
                    ItemInstance inventoryItem = GameSession.Inventory.GetItemAt(toSlotId);
                    if (inventoryItem != null)
                    {
                        GameSession.Inventory.SetItemAt(toSlotId, itemToUnequip);
                        GameSession.PlayerShip.SetEquippedAt(fromSlotId, inventoryItem);
                    }
                    else
                    {
                        GameSession.Inventory.AddItemAt(itemToUnequip, toSlotId);
                        GameSession.PlayerShip.RemoveEquippedAt(fromSlotId);
                    }
                }
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