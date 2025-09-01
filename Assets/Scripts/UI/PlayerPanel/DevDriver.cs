
using UnityEngine;
using System.Collections.Generic; // Added
using PirateRoguelike.Data; // Changed from PirateRoguelike.Data.Items
using PirateRoguelike.Runtime; // Added for RuntimeItem
using UnityEngine.UIElements; // Added for UIDocument
using PirateRoguelike.Shared; // Added for ObservableList

namespace PirateRoguelike.UI
{
    public class MockSlotViewData : ISlotViewData
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public int SlotId { get; private set; }
        public Sprite Icon { get; private set; }
        public string Rarity { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsDisabled { get; private set; }
        public float CooldownPercent { get; private set; }
        public bool IsPotentialMergeTarget { get; private set; }
        public RuntimeItem ItemData { get; private set; }
        public string ItemInstanceId { get; private set; } // NEW
        public ItemInstance CurrentItemInstance { get; private set; } // NEW

        public MockSlotViewData(int slotId, Sprite icon, string rarity, bool isEmpty, bool isDisabled, float cooldownPercent, bool isPotentialMergeTarget, RuntimeItem itemData, string itemInstanceId, ItemInstance currentItemInstance)
        {
            SlotId = slotId;
            Icon = icon;
            Rarity = rarity;
            IsEmpty = isEmpty;
            IsDisabled = isDisabled;
            CooldownPercent = cooldownPercent;
            IsPotentialMergeTarget = isPotentialMergeTarget;
            ItemData = itemData;
            ItemInstanceId = itemInstanceId;
            CurrentItemInstance = currentItemInstance;
        }
    }

    public class DevDriver : MonoBehaviour
    {
        private PlayerPanelView _playerPanelView;
                [SerializeField] private PlayerUIThemeSO _theme;
        [SerializeField] private ItemSO _mockItem;
        [SerializeField] private Sprite _playerShipSprite; // Added

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var playerPanelRoot = root.Q("player-panel");

                        _playerPanelView = new PlayerPanelView(playerPanelRoot, _theme, gameObject);

            var mockShipData = new MockShipViewData("The Sea Serpent", _playerShipSprite, 80, 100);
            var mockHudData = new MockHudViewData(150, 3, 5);

            var mockEquipmentSlots = new ObservableList<ISlotViewData>
            {
                new MockSlotViewData(0, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(1, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(2, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(3, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null)
            };

            var mockInventorySlots = new ObservableList<ISlotViewData>
            {
                new MockSlotViewData(0, _mockItem.icon, _mockItem.rarity.ToString(), false, false, 0.5f, true, new RuntimeItem(_mockItem), _mockItem.id, new ItemInstance(_mockItem)),
                new MockSlotViewData(1, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(2, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(3, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(4, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null),
                new MockSlotViewData(5, _theme.emptySlotBackground, "", true, false, 0, false, null, null, null)
            };

            var mockPlayerPanelData = new MockPlayerPanelData(mockShipData, mockHudData, mockEquipmentSlots, mockInventorySlots);

            _playerPanelView.BindInitialData(mockPlayerPanelData);
        }

        // Mock implementations for interfaces
        public class MockShipViewData : IShipViewData
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            public string ShipName { get; private set; }
            public Sprite ShipSprite { get; private set; }
            public float CurrentHp { get; private set; }
            public float MaxHp { get; private set; }

            public MockShipViewData(string shipName, Sprite shipSprite, float currentHp, float maxHp)
            {
                ShipName = shipName;
                ShipSprite = shipSprite;
                CurrentHp = currentHp;
                MaxHp = maxHp;
            }
        }

        public class MockHudViewData : IHudViewData
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            public int Gold { get; private set; }
            public int Lives { get; private set; }
            public int Depth { get; private set; }

            public MockHudViewData(int gold, int lives, int depth)
            {
                Gold = gold;
                Lives = lives;
                Depth = depth;
            }
        }

        public class MockPlayerPanelData : IPlayerPanelData
        {
            public IShipViewData ShipData { get; private set; }
            public IHudViewData HudData { get; private set; }
            public ObservableList<ISlotViewData> EquipmentSlots { get; private set; }
            public ObservableList<ISlotViewData> InventorySlots { get; private set; }

            public MockPlayerPanelData(IShipViewData shipData, IHudViewData hudData, ObservableList<ISlotViewData> equipmentSlots, ObservableList<ISlotViewData> inventorySlots)
            {
                ShipData = shipData;
                HudData = hudData;
                EquipmentSlots = equipmentSlots;
                InventorySlots = inventorySlots;
            }
        }
    }
}
