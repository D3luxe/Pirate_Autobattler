
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    // Mock data implementations for testing
    public class MockSlotViewData : ISlotViewData
    {
        public int SlotId { get; set; }
        public Sprite Icon { get; set; }
        public string Rarity { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsDisabled { get; set; }
        public float CooldownPercent { get; set; }
        public bool IsPotentialMergeTarget { get; set; }
    }

    public class MockShipViewData : IShipViewData
    {
        public string ShipName { get; set; }
        public Sprite ShipSprite { get; set; }
        public float CurrentHp { get; set; }
        public float MaxHp { get; set; }
    }

    public class MockHudViewData : IHudViewData
    {
        public int Gold { get; set; }
        public int Lives { get; set; }
        public int Depth { get; set; }
    }

    public class MockPlayerPanelData : IPlayerPanelData
    {
        public IShipViewData ShipData { get; set; }
        public IHudViewData HudData { get; set; }
        public List<ISlotViewData> EquipmentSlots { get; set; }
    }

    [RequireComponent(typeof(UIDocument))]
    public class DevDriver : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme;
        [SerializeField] private VisualTreeAsset _slotTemplate;

        private PlayerPanelView _panelView;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panelView = new PlayerPanelView(root, _slotTemplate, _theme);

            // Create Mock Data
            var mockData = new MockPlayerPanelData
            {
                ShipData = new MockShipViewData
                {
                    ShipName = "The Sea Serpent",
                    ShipSprite = _theme.pauseIcon, // Placeholder
                    CurrentHp = 75,
                    MaxHp = 100
                },
                HudData = new MockHudViewData
                {
                    Gold = 123,
                    Lives = 3,
                    Depth = 5
                },
                EquipmentSlots = new List<ISlotViewData>()
            };

            for (int i = 0; i < 10; i++)
            {
                mockData.EquipmentSlots.Add(new MockSlotViewData { 
                    SlotId = i, 
                    Icon = _theme.settingsIcon, // Placeholder
                    Rarity = new string[]{"bronze", "silver", "gold", "diamond"}[i % 4]
                });
            }

            // Bind data to the view
            _panelView.BindInitialData(mockData);

            // Register for events
            PlayerPanelEvents.OnPauseClicked += () => Debug.Log("Pause Clicked!");
            PlayerPanelEvents.OnSettingsClicked += () => Debug.Log("Settings Clicked!");
            PlayerPanelEvents.OnBattleSpeedChanged += (speed) => Debug.Log($"Battle Speed set to {speed}x");
        }
    }
}
