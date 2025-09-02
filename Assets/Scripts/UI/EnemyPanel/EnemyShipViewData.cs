using UnityEngine;
using System.ComponentModel;
using PirateRoguelike.Data;
using PirateRoguelike.Runtime;
using PirateRoguelike.Core;

namespace PirateRoguelike.UI
{
    public class EnemyShipViewData : IShipViewData, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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