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
        private EnemyShipViewData _viewModel; // Add this line

        public void Initialize(ShipState enemyShipState)
        {
            _enemyShipState = enemyShipState;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _panelView = new EnemyPanelView(root, _slotTemplate, _theme);

            _viewModel = new EnemyShipViewData(_enemyShipState); // Instantiate the viewmodel

            // Subscribe to enemy ship events
            _enemyShipState.OnHealthChanged += HandleEnemyHealthChanged;
            _enemyShipState.OnEquipmentChanged += HandleEnemyEquipmentChanged;

            // Initial data bind
            _panelView.UpdateShipData(_viewModel); // Pass the viewmodel

            // Bind Equipment Slots (remains as is for now)
            _panelView.UpdateEquipment(_enemyShipState.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList());
        }

        void OnDestroy()
        {
            if (_enemyShipState != null)
            {
                _enemyShipState.OnHealthChanged -= HandleEnemyHealthChanged;
                _enemyShipState.OnEquipmentChanged -= HandleEnemyEquipmentChanged;
            }
        }

        // Event Handlers
        private void HandleEnemyHealthChanged()
        {
            _viewModel.CurrentHp = _enemyShipState.CurrentHealth; // Update viewmodel property directly
        }

        private void HandleEnemyEquipmentChanged()
        {
            _panelView.UpdateEquipment(_enemyShipState.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>().ToList());
        }

        // Helper class for Enemy Ship View Data
        public class EnemyShipViewData : IShipViewData, System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            private ShipState _shipState;
            private float _currentHp;

            public EnemyShipViewData(ShipState shipState)
            {
                _shipState = shipState;
                _currentHp = _shipState.CurrentHealth; // Initialize backing field
            }

            public string ShipName => _shipState.Def.displayName;
            public Sprite ShipSprite => _shipState.Def.art;
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
            public float MaxHp => _shipState.Def.baseMaxHealth;
        }
    }
}
