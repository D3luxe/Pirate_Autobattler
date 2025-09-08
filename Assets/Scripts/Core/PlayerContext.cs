using PirateRoguelike.Services;
using PirateRoguelike.Core;
using Pirate.MapGen;
using PirateRoguelike.Data;

namespace PirateRoguelike.Core
{
    // Interfaces for services that EventChoiceActions will interact with
    public interface IEconomyService
    {
        void AddGold(int amount);
        bool TrySpendGold(int amount);
        void AddLives(int amount);
        void LoseLife();
    }

    public interface IInventoryService
    {
        bool AddItem(string itemId);
        bool RemoveItem(string itemId);
        ItemInstance GetItem(string itemId);
    }

    public interface IGameSessionService
    {
        ShipState PlayerShip { get; }
        void SetPlayerShip(ShipState newShipState);
        void SetNextEncounter(string encounterId);
        void LoadRun(Saving.RunState runState); // For loading saved runs, if needed by an action
        void StartNewRun(); // For starting new runs, if needed by an action
    }

    public interface IRunManagerService
    {
        void ReturnToMap();
        void LoadEncounter(string encounterId);
    }

    public class PlayerContext
    {
        public IEconomyService Economy { get; }
        public IInventoryService Inventory { get; }
        public IGameSessionService GameSession { get; }
        public IRunManagerService RunManager { get; }

        public PlayerContext(IEconomyService economy, IInventoryService inventory, IGameSessionService gameSession, IRunManagerService runManager)
        {
            Economy = economy;
            Inventory = inventory;
            GameSession = gameSession;
            RunManager = runManager;
        }
    }
}
