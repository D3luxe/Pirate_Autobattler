
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace PirateRoguelike.UI
{
    // This is the adapter class that converts game data into view data.
    public class PlayerPanelDataViewModel : IPlayerPanelData, IShipViewData, IHudViewData
    {
        // IShipViewData
        public string ShipName => GameSession.PlayerShip.Def.displayName;
        public Sprite ShipSprite => GameSession.PlayerShip.Def.art; // Corrected
        public float CurrentHp => GameSession.PlayerShip.CurrentHealth;
        public float MaxHp => GameSession.PlayerShip.Def.baseMaxHealth;

        // IHudViewData
        public int Gold => GameSession.Economy.Gold;
        public int Lives => GameSession.Economy.Lives;
        public int Depth => GameSession.CurrentRunState?.currentColumnIndex ?? 0;

        // IPlayerPanelData
        public IShipViewData ShipData => this;
        public IHudViewData HudData => this;
        public List<ISlotViewData> EquipmentSlots => GameSession.Inventory.Items.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList();
    }

    public class SlotDataViewModel : ISlotViewData
    {
        private readonly ItemInstance _item;
        private readonly int _index;

        public SlotDataViewModel(ItemInstance item, int index)
        {
            _item = item;
            _index = index;
        }

        public int SlotId => _index;
        public Sprite Icon => _item?.Def.icon; // Corrected: was itemSprite
        public string Rarity => _item?.Def.rarity.ToString();
        public bool IsEmpty => _item == null;
        public bool IsDisabled => false; // TODO: Hook up stun/disable logic
        public float CooldownPercent => (_item == null || _item.Def.cooldownSec <= 0) ? 0 : _item.CooldownRemaining / _item.Def.cooldownSec;
        public bool IsPotentialMergeTarget => false; // TODO: Hook up merge logic
    }


    [RequireComponent(typeof(UIDocument))]
    public class PlayerPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme;
        [SerializeField] private VisualTreeAsset _slotTemplate;

        private PlayerPanelView _panelView;
        private PlayerPanelDataViewModel _viewModel = new PlayerPanelDataViewModel();

        public void Initialize()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panelView = new PlayerPanelView(root, _slotTemplate, _theme);

            // Subscribe to game events
            EconomyService.OnGoldChanged += HandleGoldChanged;
            EconomyService.OnLivesChanged += HandleLivesChanged;
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged += HandleInventoryChanged;
            if (GameSession.PlayerShip != null) GameSession.PlayerShip.OnHealthChanged += HandleHealthChanged;

            // Subscribe to UI events
            PlayerPanelEvents.OnSlotDropped += HandleSlotDropped;

            // Initial data bind
            _panelView.BindInitialData(_viewModel);
        }

        void OnDestroy()
        {
            // Unsubscribe from all events
            EconomyService.OnGoldChanged -= HandleGoldChanged;
            EconomyService.OnLivesChanged -= HandleLivesChanged;
            if (GameSession.Inventory != null) GameSession.Inventory.OnInventoryChanged -= HandleInventoryChanged;
            if (GameSession.PlayerShip != null) GameSession.PlayerShip.OnHealthChanged -= HandleHealthChanged;

            PlayerPanelEvents.OnSlotDropped -= HandleSlotDropped;
        }

        // --- Event Handlers ---

        private void HandleGoldChanged(int newGold) => _panelView.UpdateGold(newGold);
        private void HandleLivesChanged(int newLives) => _panelView.UpdateLives(newLives);
        private void HandleHealthChanged() => _panelView.UpdateHp(GameSession.PlayerShip.CurrentHealth, GameSession.PlayerShip.Def.baseMaxHealth);
        private void HandleInventoryChanged() => _panelView.UpdateInventory(_viewModel.EquipmentSlots);
        private void HandleSlotDropped(int fromIndex, int toIndex) => GameSession.Inventory.SwapItems(fromIndex, toIndex);
    }
}
