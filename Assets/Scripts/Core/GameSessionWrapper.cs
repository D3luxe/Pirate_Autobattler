using System;
using System.Collections.Generic;
using PirateRoguelike.Data;
using PirateRoguelike.Runtime;
using PirateRoguelike.Services;

namespace PirateRoguelike.Core
{
    public class GameSessionWrapper : IGameSession
    {
        public ShipState PlayerShip => GameSession.PlayerShip;
        public Inventory Inventory => GameSession.Inventory;

        public int Gold => GameSession.Economy.Gold;
        public int Lives => GameSession.Economy.Lives;
        public int CurrentDepth => GameSession.CurrentRunState.currentColumnIndex; // Assuming CurrentDepth maps to currentColumnIndex

        // NEW: Implement Economy property
        public EconomyService Economy => GameSession.Economy;

        public event Action OnPlayerShipInitialized
        {
            add => GameSession.OnPlayerShipInitialized += value;
            remove => GameSession.OnPlayerShipInitialized -= value;
        }

        public event Action OnInventoryInitialized
        {
            add => GameSession.OnInventoryInitialized += value;
            remove => GameSession.OnInventoryInitialized -= value;
        }

        public event Action OnEconomyInitialized
        {
            add => GameSession.OnEconomyInitialized += value;
            remove => GameSession.OnEconomyInitialized -= value;
        }
    }
}
