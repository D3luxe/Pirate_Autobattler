using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Added for ItemInstance
using PirateRoguelike.Runtime; // Added for RuntimeItem
using PirateRoguelike.Shared; // Added for ObservableList
using PirateRoguelike.Core; // Added for IGameSession

namespace PirateRoguelike.UI
{
    // This is the adapter class that converts game data into view data.
    public class PlayerPanelDataViewModel : IPlayerPanelData, IShipViewData, IHudViewData, System.ComponentModel.INotifyPropertyChanged
    {
        private readonly IGameSession _gameSession;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public PlayerPanelDataViewModel(IGameSession gameSession)
        {
            _gameSession = gameSession;
            _equipmentSlots = new ObservableList<ISlotViewData>(); // ADDED
            _inventorySlots = new ObservableList<ISlotViewData>(); // ADDED
        }

        // IShipViewData
        private string _shipName;
        public string ShipName
        {
            get => _shipName;
            private set
            {
                if (_shipName != value)
                {
                    _shipName = value;
                    OnPropertyChanged(nameof(ShipName));
                }
            }
        }
        private Sprite _shipSprite;
        public Sprite ShipSprite
        {
            get => _shipSprite;
            private set
            {
                if (_shipSprite != value)
                {
                    _shipSprite = value;
                    OnPropertyChanged(nameof(ShipSprite));
                }
            }
        }
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
        private float _maxHp;
        public float MaxHp
        {
            get => _maxHp;
            private set
            {
                if (_maxHp != value)
                {
                    _maxHp = value;
                    OnPropertyChanged(nameof(MaxHp));
                }
            }
        }

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
            // Subscribe to GameSession initialization events
            _gameSession.OnPlayerShipInitialized += UpdateShipData; // NEW
            _gameSession.OnEconomyInitialized += UpdateHudData; // NEW

            // Subscribe to GameSession change events (for ongoing updates)
            _gameSession.PlayerShip.OnEquipmentSwapped += HandleEquipmentSwapped;
            _gameSession.PlayerShip.OnEquipmentAddedAt += HandleEquipmentAddedAt;
            _gameSession.PlayerShip.OnEquipmentRemovedAt += HandleEquipmentRemovedAt;

            _gameSession.Inventory.OnItemsSwapped += HandleInventorySwapped;
            _gameSession.Inventory.OnItemAddedAt += HandleInventoryAddedAt;
            _gameSession.Inventory.OnItemRemovedAt += HandleInventoryRemovedAt;

            // Manually trigger initial updates for equipment and inventory slots
            // This ensures the UI is populated even if the GameSession events fired before subscription.
            PopulateEquipmentSlots(); // ADD THIS
            PopulateInventorySlots(); // ADD THIS
            UpdateShipData(); // Also update ship data initially
            UpdateHudData(); // Also update HUD data initially
        }

        public void HandleEquipmentSwapped(int indexA, int indexB)
        {
            // Get the ItemSlot objects from the ObservableList
            var slotA = _equipmentSlots[indexA] as SlotDataViewModel;
            var slotB = _equipmentSlots[indexB] as SlotDataViewModel;

            // Get the ItemInstances from the GameSession (which has the updated data)
            var itemA = _gameSession.PlayerShip.GetEquippedItem(indexA);
            var itemB = _gameSession.PlayerShip.GetEquippedItem(indexB);

            // Update the Item property of the existing SlotDataViewModel objects
            // This will trigger PropertyChanged events on the SlotDataViewModel,
            // which SlotElement listens to.
            slotA.CurrentItemInstance = itemB;
            slotB.CurrentItemInstance = itemA;
        }

        public void HandleEquipmentAddedAt(int index, ItemInstance item)
        {
            (_equipmentSlots[index] as SlotDataViewModel).CurrentItemInstance = item;
        }

        public void HandleEquipmentRemovedAt(int index, ItemInstance item)
        {
            (_equipmentSlots[index] as SlotDataViewModel).CurrentItemInstance = null; // Set to empty slot
        }

        public void HandleInventorySwapped(int indexA, int indexB)
        {
            var slotA = _inventorySlots[indexA] as SlotDataViewModel;
            var slotB = _inventorySlots[indexB] as SlotDataViewModel;

            var itemA = _gameSession.Inventory.GetItemAt(indexA);
            var itemB = _gameSession.Inventory.GetItemAt(indexB);

            slotA.CurrentItemInstance = itemB;
            slotB.CurrentItemInstance = itemA;
        }

        public void HandleInventoryAddedAt(int index, ItemInstance item)
        {
            (_inventorySlots[index] as SlotDataViewModel).CurrentItemInstance = item;
        }

        public void HandleInventoryRemovedAt(int index, ItemInstance item)
        {
            (_inventorySlots[index] as SlotDataViewModel).CurrentItemInstance = null; // Set to empty slot
        }

        private void UpdateHudData()
        {
            Gold = _gameSession.Gold;
            Lives = _gameSession.Lives;
            Depth = _gameSession.CurrentDepth;
        }

        private void UpdateShipData()
        {
            ShipName = _gameSession.PlayerShip.Def.displayName;
            ShipSprite = _gameSession.PlayerShip.Def.art;
            CurrentHp = _gameSession.PlayerShip.CurrentHealth;
            MaxHp = _gameSession.PlayerShip.Def.baseMaxHealth;
        }

        private void PopulateEquipmentSlots()
        {
            var currentEquipped = _gameSession.PlayerShip.Equipped;
            _equipmentSlots.Clear();
            for (int i = 0; i < currentEquipped.Length; i++)
            {
                _equipmentSlots.Add(new SlotDataViewModel(currentEquipped[i], i));
            }
        }

        private void PopulateInventorySlots()
        {
            var currentInventory = _gameSession.Inventory.Slots;
            _inventorySlots.Clear();
            for (int i = 0; i < currentInventory.Count; i++)
            {
                ((IList<ISlotViewData>)_inventorySlots).Add(new SlotDataViewModel(currentInventory[i].Item, i)); // MODIFIED LINE
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

        private ItemInstance _item;
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

        public ItemInstance CurrentItemInstance
        {
            get => _item;
            set
            {
                if (_item != value)
                {
                    _item = value;
                    Debug.Log($"SlotDataViewModel.CurrentItemInstance setter: Item changed to: {value?.Def.id ?? "NULL"}"); // ADD THIS LINE
                    OnPropertyChanged(nameof(CurrentItemInstance));
                    OnPropertyChanged(nameof(Icon)); // Icon might change
                    OnPropertyChanged(nameof(Rarity)); // Rarity might change
                    OnPropertyChanged(nameof(IsEmpty)); // IsEmpty might change
                }
            }
        } // NEW PROPERTY 
    }


    [RequireComponent(typeof(UIDocument))]
    public class PlayerPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme;
        private MapView _mapView;

        private PlayerPanelView _panelView;
        private PlayerPanelDataViewModel _viewModel;
        private Button _mapToggleButton;

        public void SetMapPanel(MapView mapView)
        {
            _mapView = mapView;
        }

        public void Initialize()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _viewModel = new PlayerPanelDataViewModel(new PirateRoguelike.Core.GameSessionWrapper()); // Instantiate ViewModel here
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
            
            
            // Subscriptions are now handled by PlayerPanelDataViewModel

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
            
            
            // Unsubscriptions are now handled by PlayerPanelDataViewModel

            PlayerPanelEvents.OnSlotDropped -= HandleSlotDropped;
            PlayerPanelEvents.OnMapToggleClicked -= HandleMapToggleClicked;

            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked -= OnMapToggleButtonClicked;
            }
        }

        // --- Event Handlers ---
        private void HandleSlotDropped(int fromSlotId, SlotContainerType fromContainer, int toSlotId, SlotContainerType toContainer)
        {
            Debug.Log($"HandleSlotDropped: fromSlotId={fromSlotId}, fromContainer={fromContainer}, toSlotId={toSlotId}, toContainer={toContainer}");
            if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Inventory)
            {
                Debug.Log($"HandleSlotDropped: Calling GameSession.Inventory.SwapItems({fromSlotId}, {toSlotId})");
                GameSession.Inventory.SwapItems(fromSlotId, toSlotId);
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Equipment)
            {
                Debug.Log($"HandleSlotDropped: Calling GameSession.PlayerShip.SwapEquipment({fromSlotId}, {toSlotId})");
                GameSession.PlayerShip.SwapEquipment(fromSlotId, toSlotId); // Use SwapEquipment
            }
            else if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Equipment)
            {
                Debug.Log($"HandleSlotDropped: Attempting to equip item from Inventory to Equipment.");
                ItemInstance itemToEquip = GameSession.Inventory.GetItemAt(fromSlotId);
                if (itemToEquip != null)
                {
                    ItemInstance equippedItem = GameSession.PlayerShip.GetEquippedItem(toSlotId);
                    if (equippedItem != null)
                    {
                        Debug.Log($"HandleSlotDropped: Swapping item {itemToEquip.Def.id} with {equippedItem.Def.id}");
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.SetItemAt(fromSlotId, equippedItem);
                    }
                    else
                    {
                        Debug.Log($"HandleSlotDropped: Equipping item {itemToEquip.Def.id}");
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip);
                        GameSession.Inventory.RemoveItemAt(fromSlotId);
                    }
                }
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Inventory)
            {
                Debug.Log($"HandleSlotDropped: Attempting to unequip item from Equipment to Inventory.");
                ItemInstance itemToUnequip = GameSession.PlayerShip.GetEquippedItem(fromSlotId);
                if (itemToUnequip != null)
                {
                    ItemInstance inventoryItem = GameSession.Inventory.GetItemAt(toSlotId);
                    if (inventoryItem != null)
                    {
                        Debug.Log($"HandleSlotDropped: Swapping item {itemToUnequip.Def.id} with {inventoryItem.Def.id}");
                        GameSession.Inventory.SetItemAt(toSlotId, itemToUnequip);
                        GameSession.PlayerShip.SetEquippedAt(fromSlotId, inventoryItem);
                    }
                    else
                    {
                        Debug.Log($"HandleSlotDropped: Unequipping item {itemToUnequip.Def.id}");
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