using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Runtime;

namespace PirateRoguelike.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class EnemyPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme; // Reusing player theme for now
        [SerializeField] private VisualTreeAsset _slotTemplate; // Slot UXML template

        private EnemyPanelView _panelView;
        private ShipState _enemyShipState; // Reference to the enemy's ShipState

        public void Initialize(ShipState enemyShipState)
        {
            _enemyShipState = enemyShipState;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _panelView = new EnemyPanelView(root, _slotTemplate, _theme);

            // Subscribe to enemy ship events
            _enemyShipState.OnHealthChanged += HandleEnemyHealthChanged;
            _enemyShipState.OnEquipmentChanged += HandleEnemyEquipmentChanged;

            // Initial data bind
            BindData();
        }

        void OnDestroy()
        {
            if (_enemyShipState != null)
            {
                _enemyShipState.OnHealthChanged -= HandleEnemyHealthChanged;
                _enemyShipState.OnEquipmentChanged -= HandleEnemyEquipmentChanged;
            }
        }

        private void BindData()
        {
            // Bind Ship Data
            _panelView.UpdateShipData(new EnemyShipViewData(_enemyShipState));

            // Bind Equipment Slots
            _panelView.UpdateEquipment(_enemyShipState.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList());
        }

        // Event Handlers
        private void HandleEnemyHealthChanged()
        {
            _panelView.UpdateShipData(new EnemyShipViewData(_enemyShipState));
        }

        private void HandleEnemyEquipmentChanged()
        {
            _panelView.UpdateEquipment(_enemyShipState.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList());
        }

        // Helper class for Enemy Ship View Data
        public class EnemyShipViewData : IShipViewData
        {
            private ShipState _shipState;

            public EnemyShipViewData(ShipState shipState)
            {
                _shipState = shipState;
            }

            public string ShipName => _shipState.Def.displayName;
            public Sprite ShipSprite => _shipState.Def.art;
            public float CurrentHp => _shipState.CurrentHealth;
            public float MaxHp => _shipState.Def.baseMaxHealth;
        }
    }
}
