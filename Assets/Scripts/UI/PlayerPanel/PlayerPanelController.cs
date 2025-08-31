using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Added for ItemInstance
using PirateRoguelike.Runtime; // Added for RuntimeItem

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
            private set
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
        public List<ISlotViewData> EquipmentSlots => GameSession.PlayerShip.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList();
        public List<ISlotViewData> InventorySlots => GameSession.Inventory.Items.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList();
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
        [SerializeField] private VisualTreeAsset _slotTemplate;
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
            _panelView = new PlayerPanelView(root, _slotTemplate, _theme, this.gameObject);

            // Get reference to the MapToggle button
            _mapToggleButton = root.Q<Button>("MapToggle");
            if (_mapToggleButton == null) Debug.LogError("Button 'MapToggle' not found in UXML.");

            // Register click event
            if (_mapToggleButton != null)
            {
                _mapToggleButton.clicked += OnMapToggleButtonClicked;
            }

            // Subscribe to game events
            EconomyService.OnGoldChanged += HandleGoldChanged;
            EconomyService.OnLivesChanged += HandleLivesChanged;
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged += HandleInventoryChanged;
            if (GameSession.PlayerShip != null) 
            {
                GameSession.PlayerShip.OnHealthChanged += HandleHealthChanged;
                GameSession.PlayerShip.OnEquipmentChanged += HandleEquipmentChanged;
            }

            // Subscribe to UI events
            PlayerPanelEvents.OnSlotDropped += HandleSlotDropped;
            PlayerPanelEvents.OnMapToggleClicked += HandleMapToggleClicked;

            // Initial data bind
            _panelView.BindInitialData(_viewModel);
        }

        void OnDestroy()
        {
            // Unsubscribe from all events
            EconomyService.OnGoldChanged -= HandleGoldChanged;
            EconomyService.OnLivesChanged -= HandleLivesChanged;
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged -= HandleInventoryChanged;
            if (GameSession.PlayerShip != null) 
            {
                GameSession.PlayerShip.OnHealthChanged -= HandleHealthChanged;
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

        private void HandleGoldChanged(int newGold) => _viewModel.Gold = newGold;
        private void HandleLivesChanged(int newLives) => _viewModel.Lives = newLives;
        private void HandleHealthChanged() => _viewModel.CurrentHp = GameSession.PlayerShip.CurrentHealth;
        private void HandleEquipmentChanged()
        {
            //Debug.Log("PlayerPanelController: HandleEquipmentChanged called.");
            _panelView.UpdateEquipment(_viewModel.EquipmentSlots);
        }
        private void HandleInventoryChanged()
        {
            //Debug.Log("PlayerPanelController: HandleInventoryChanged called.");
            _panelView.UpdatePlayerInventory(_viewModel.InventorySlots);
        }
        private void HandleSlotDropped(int fromSlotId, SlotContainerType fromContainer, int toSlotId, SlotContainerType toContainer)
        {
            //Debug.Log($"HandleSlotDropped: From {fromContainer} slot {fromSlotId} to {toContainer} slot {toSlotId}");

            if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Inventory)
            {
                // Move item within inventory
                //Debug.Log($"Swapping items within inventory: {fromSlotId} and {toSlotId}");
                GameSession.Inventory.SwapItems(fromSlotId, toSlotId);
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Equipment)
            {
                // Move item within equipment
                //Debug.Log($"Swapping items within equipment: {fromSlotId} and {toSlotId}");
                GameSession.PlayerShip.SwapEquipment(fromSlotId, toSlotId); // Use SwapEquipment
            }
            else if (fromContainer == SlotContainerType.Inventory && toContainer == SlotContainerType.Equipment)
            {
                // Equip item from inventory
                //Debug.Log($"Equipping item from inventory slot {fromSlotId} to equipment slot {toSlotId}");
                ItemInstance itemToEquip = GameSession.Inventory.GetItemAt(fromSlotId); // Use GetItemAt
                //Debug.Log($"Item to equip: {itemToEquip?.Def.displayName ?? "NULL"}");
                if (itemToEquip != null)
                {
                    // Check if target equipment slot is occupied
                    ItemInstance equippedItem = GameSession.PlayerShip.GetEquippedItem(toSlotId); // Use GetEquippedItem
//                    //Debug.Log($"Currently equipped item at {toSlotId}: {equippedItem?.Def.displayName ?? "NULL"}");
                    if (equippedItem != null)
                    {
                        // If occupied, swap items
//                        //Debug.Log($"Equipment slot {toSlotId} is occupied. Swapping {itemToEquip.Def.displayName} with {equippedItem.Def.displayName}.");
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip); // Equip new item
                        GameSession.Inventory.SetItemAt(fromSlotId, equippedItem); // Put old equipped item into inventory
                    }
                    else
                    {
                        GameSession.PlayerShip.SetEquippedAt(toSlotId, itemToEquip); // Equip new item
                        GameSession.Inventory.RemoveItemAt(fromSlotId); // Remove new item from inventory
                    }

//                    //Debug.Log($"After equip: Equipped slot {toSlotId} has {GameSession.PlayerShip.GetEquippedItem(toSlotId)?.Def.displayName ?? "NULL"}");
//                    //Debug.Log($"After equip: Inventory slot {fromSlotId} has {GameSession.Inventory.GetItemAt(fromSlotId)?.Def.displayName ?? "NULL"}");
                }
            }
            else if (fromContainer == SlotContainerType.Equipment && toContainer == SlotContainerType.Inventory)
            {
                // Unequip item to inventory
//                //Debug.Log($"Unequipping item from equipment slot {fromSlotId} to inventory slot {toSlotId}");
                ItemInstance itemToUnequip = GameSession.PlayerShip.GetEquippedItem(fromSlotId); // Use GetEquippedItem
//                //Debug.Log($"Item to unequip: {itemToUnequip?.Def.displayName ?? "NULL"}");
                if (itemToUnequip != null)
                {
                    // Check if target inventory slot is occupied
                    ItemInstance inventoryItem = GameSession.Inventory.GetItemAt(toSlotId); // Use GetItemAt
//                    //Debug.Log($"Currently in inventory slot {toSlotId}: {inventoryItem?.Def.displayName ?? "NULL"}");
                    if (inventoryItem != null)
                    {
                        // If occupied, swap items
//                        //Debug.Log($"Inventory slot {toSlotId} is occupied. Swapping {itemToUnequip.Def.displayName} with {inventoryItem.Def.displayName}.");
                        GameSession.Inventory.SetItemAt(toSlotId, itemToUnequip); // Use SetItemAt
                        GameSession.PlayerShip.SetEquippedAt(fromSlotId, inventoryItem); // Use SetEquippedAt
                    }
                    else
                    {
                        GameSession.Inventory.AddItemAt(itemToUnequip, toSlotId);
                        GameSession.PlayerShip.RemoveEquippedAt(fromSlotId); // Use RemoveEquippedAt
                    }

//                    //Debug.Log($"After unequip: Equipped slot {fromSlotId} has {GameSession.PlayerShip.GetEquippedItem(fromSlotId)?.Def.displayName ?? "NULL"}");
//                    //Debug.Log($"After unequip: Inventory slot {toSlotId} has {GameSession.Inventory.GetItemAt(toSlotId)?.Def.displayName ?? "NULL"}");
                }
            }
            else
            {
                //Debug.LogWarning($"Unhandled slot drop: From {fromContainer} to {toContainer}");
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