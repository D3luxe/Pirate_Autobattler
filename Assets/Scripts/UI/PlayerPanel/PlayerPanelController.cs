using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Added for ItemInstance
using PirateRoguelike.Runtime; // Added for RuntimeItem
using PirateRoguelike.Shared; // Added for ObservableList
using PirateRoguelike.Core; // Added for IGameSession
using PirateRoguelike.Services; // Added for SlotId
using PirateRoguelike.Events; // Added for ItemManipulationEvents

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

        

        public void Initialize()
        {
            // Subscribe to GameSession initialization events
            _gameSession.OnPlayerShipInitialized += UpdateShipData; // NEW
            _gameSession.OnEconomyInitialized += UpdateHudData; // NEW

            // Subscribe to ItemManipulationEvents
            PirateRoguelike.Events.ItemManipulationEvents.OnItemMoved += HandleItemMoved;
            PirateRoguelike.Events.ItemManipulationEvents.OnItemAdded += HandleItemAdded;
            PirateRoguelike.Events.ItemManipulationEvents.OnItemRemoved += HandleItemRemoved;

            // Subscriptions are now handled by ItemManipulationEvents

            // Manually trigger initial updates for equipment and inventory slots
            // This ensures the UI is populated even if the GameSession events fired before subscription.
            PopulateEquipmentSlots(); // ADD THIS
            PopulateInventorySlots(); // ADD THIS
            UpdateShipData(); // Also update ship data initially
            UpdateHudData(); // Also update HUD data initially
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
                _equipmentSlots.Add(new SlotDataViewModel(currentEquipped[i], i, global::PirateRoguelike.Services.SlotContainerType.Equipment));
            }
        }

        private void PopulateInventorySlots()
        {
            var currentInventory = _gameSession.Inventory.Slots;
            _inventorySlots.Clear();
            for (int i = 0; i < currentInventory.Count; i++)
            {
                ((IList<ISlotViewData>)_inventorySlots).Add(new SlotDataViewModel(currentInventory[i].Item, i, global::PirateRoguelike.Services.SlotContainerType.Inventory)); // MODIFIED LINE
            }
        }

        // --- Item Manipulation Event Handlers ---
        private void HandleItemMoved(ItemInstance item, SlotId from, SlotId to)
        {
            // Clear the old slot
            if (from.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Inventory)
            {
                (_inventorySlots[from.Index] as SlotDataViewModel).CurrentItemInstance = null;
            }
            else if (from.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Equipment)
            {
                (_equipmentSlots[from.Index] as SlotDataViewModel).CurrentItemInstance = null;
            }

            // Populate the new slot
            if (to.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Inventory)
            {
                (_inventorySlots[to.Index] as SlotDataViewModel).CurrentItemInstance = item;
            }
            else if (to.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Equipment)
            {
                (_equipmentSlots[to.Index] as SlotDataViewModel).CurrentItemInstance = item;
            }
        }

        private void HandleItemAdded(ItemInstance item, SlotId to)
        {
            if (to.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Inventory)
            {
                (_inventorySlots[to.Index] as SlotDataViewModel).CurrentItemInstance = item;
            }
            else if (to.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Equipment)
            {
                (_equipmentSlots[to.Index] as SlotDataViewModel).CurrentItemInstance = item;
            }
        }

        private void HandleItemRemoved(ItemInstance item, SlotId from)
        {
            if (from.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Inventory)
            {
                (_inventorySlots[from.Index] as SlotDataViewModel).CurrentItemInstance = null;
            }
            else if (from.ContainerType == global::PirateRoguelike.Services.SlotContainerType.Equipment)
            {
                (_equipmentSlots[from.Index] as SlotDataViewModel).CurrentItemInstance = null;
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
        public global::PirateRoguelike.Services.SlotContainerType ContainerType { get; private set; } // NEW

        public SlotDataViewModel(ItemInstance item, int index, global::PirateRoguelike.Services.SlotContainerType containerType) // MODIFIED CONSTRUCTOR
        {
            _item = item;
            _index = index;
            ContainerType = containerType; // NEW
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

        public void Initialize(IGameSession gameSession)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _viewModel = new PlayerPanelDataViewModel(gameSession); // Instantiate ViewModel here
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
            PlayerPanelEvents.OnMapToggleClicked += HandleMapToggleClicked;

            _viewModel.Initialize(); // Initialize the view model after GameSession is ready
            // Initial data bind
            _panelView.BindInitialData(_viewModel);
        }

        void OnDestroy()
        {
            // Unsubscribe from all events
            
            
            // Unsubscriptions are now handled by PlayerPanelDataViewModel

            PlayerPanelEvents.OnMapToggleClicked -= HandleMapToggleClicked;

            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked -= OnMapToggleButtonClicked;
            }
        }

        // --- Event Handlers ---
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