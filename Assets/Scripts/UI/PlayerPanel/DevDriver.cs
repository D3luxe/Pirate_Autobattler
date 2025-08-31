
using UnityEngine;
using System.Collections.Generic; // Added
using PirateRoguelike.Data; // Changed from PirateRoguelike.Data.Items
using PirateRoguelike.Runtime; // Added for RuntimeItem
using UnityEngine.UIElements; // Added for UIDocument

namespace PirateRoguelike.UI
{
    public class MockSlotViewData : ISlotViewData
    {
        public int SlotId { get; private set; }
        public Sprite Icon { get; private set; }
        public string Rarity { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsDisabled { get; private set; }
        public float CooldownPercent { get; private set; }
        public bool IsPotentialMergeTarget { get; private set; }
        public RuntimeItem ItemData { get; private set; }

        public MockSlotViewData(int slotId, Sprite icon, string rarity, bool isEmpty, bool isDisabled, float cooldownPercent, bool isPotentialMergeTarget, RuntimeItem itemData)
        {
            SlotId = slotId;
            Icon = icon;
            Rarity = rarity;
            IsEmpty = isEmpty;
            IsDisabled = isDisabled;
            CooldownPercent = cooldownPercent;
            IsPotentialMergeTarget = isPotentialMergeTarget;
            ItemData = itemData;
        }
    }

    public class DevDriver : MonoBehaviour
    {
        [SerializeField] private PlayerPanelView _playerPanelView;
        [SerializeField] private PlayerUIThemeSO _theme;
        [SerializeField] private ItemSO _mockItem;
        [SerializeField] private VisualTreeAsset _slotTemplate; // Added
        [SerializeField] private Sprite _playerShipSprite; // Added

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var playerPanelRoot = root.Q("player-panel");

            _playerPanelView = new PlayerPanelView(playerPanelRoot, _slotTemplate, _theme, gameObject);

            var mockShipData = new MockShipViewData("The Sea Serpent", _playerShipSprite, 80, 100);
            var mockHudData = new MockHudViewData(150, 3, 5);

            var mockEquipmentSlots = new List<ISlotViewData>
            {
                new MockSlotViewData(0, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(1, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(2, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(3, _theme.emptySlotBackground, "", true, false, 0, false, null)
            };

            var mockInventorySlots = new List<ISlotViewData>
            {
                new MockSlotViewData(0, _mockItem.icon, _mockItem.rarity.ToString(), false, false, 0.5f, true, new RuntimeItem(_mockItem)),
                new MockSlotViewData(1, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(2, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(3, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(4, _theme.emptySlotBackground, "", true, false, 0, false, null),
                new MockSlotViewData(5, _theme.emptySlotBackground, "", true, false, 0, false, null)
            };

            var mockPlayerPanelData = new MockPlayerPanelData(mockShipData, mockHudData, mockEquipmentSlots, mockInventorySlots);

            _playerPanelView.BindInitialData(mockPlayerPanelData);
        }

        // Mock implementations for interfaces
        public class MockShipViewData : IShipViewData
        {
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
            public List<ISlotViewData> EquipmentSlots { get; private set; }
            public List<ISlotViewData> InventorySlots { get; private set; }

            public MockPlayerPanelData(IShipViewData shipData, IHudViewData hudData, List<ISlotViewData> equipmentSlots, List<ISlotViewData> inventorySlots)
            {
                ShipData = shipData;
                HudData = hudData;
                EquipmentSlots = equipmentSlots;
                InventorySlots = inventorySlots;
            }
        }
    }
}
