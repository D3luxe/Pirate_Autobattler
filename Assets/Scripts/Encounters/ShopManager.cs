using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using UnityEngine.UIElements; // Added
using PirateRoguelike.UI.Components; // Added
using PirateRoguelike.UI.Utilities; // Added
using PirateRoguelike.UI; // Added for ShopItemViewData and ShopItemElement
using PirateRoguelike.Core;
using Pirate.MapGen;
using System; // For Action
using PirateRoguelike.Services;

namespace PirateRoguelike.Encounters
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; } // Singleton instance

        [Header("References")]
        // [SerializeField] private UIDocument _shopUIDocument; // Removed: UI handled by ShopController

        // Removed UI elements:
        // private VisualElement _shopItemsContainer;
        // private VisualElement _shopShipContainer;
        // private Label _playerGoldLabel;
        // private Label _rerollCostLabel;
        // private Button _rerollButton;
        // private Button _leaveShopButton;

        private List<ItemSO> _currentShopItems = new List<ItemSO>();
        private ShipSO _currentShopShip;
        private RunConfigSO _runConfig;
        private int _rerollsThisShop = 0;

        // Expose data for ShopController
        public List<ItemSO> CurrentShopItems => _currentShopItems;
        public ShipSO CurrentShopShip => _currentShopShip;

        // Events for ShopController
        public event Action OnShopDataUpdated;
        public event Action<string> OnMessageDisplayed;

        // NEW: Public method to dispatch messages
        public void DisplayMessage(string message)
        {
            OnMessageDisplayed?.Invoke(message);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (GameSession.CurrentRunState == null)
            {
                Debug.LogError("GameSession not active. Cannot open shop.");
                // TODO: Handle debug mode for shop
                return;
            }

            _runConfig = GameDataRegistry.GetRunConfig();
            if (_runConfig == null)
            {
                Debug.LogError("RunConfigSO not found in GameDataRegistry!");
                return;
            }

            // Removed UI element querying and button subscriptions
            // var root = _shopUIDocument.rootVisualElement;
            // _shopItemsContainer = root.Q<VisualElement>("shop-items-container");
            // ...

            GenerateShopItems(GameSession.CurrentRunState.NextShopItemCount);
            GenerateShopShip();
            Debug.Log($"ShopManager.Start: Firing OnShopDataUpdated. Subscribers: {OnShopDataUpdated?.GetInvocationList()?.Length ?? 0}");
            OnShopDataUpdated?.Invoke(); // Initial UI update
        }

        public ItemSO GetShopItem(int index)
        {
            if (index >= 0 && index < _currentShopItems.Count)
            {
                return _currentShopItems[index];
            }
            return null;
        }

        public void RemoveShopItem(int index)
        {
            if (index >= 0 && index < _currentShopItems.Count)
            {
                _currentShopItems.RemoveAt(index);
                OnShopDataUpdated?.Invoke(); // Notify UI
            }
        }


        private void GenerateShopItems(int itemCount)
        {
            int currentFloorIndex = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : 0;
            _currentShopItems = ItemGenerationService.GenerateRandomItems(itemCount, currentFloorIndex, false);
            OnShopDataUpdated?.Invoke(); // Notify UI
        }

        

        // Removed UpdateShopUI() - UI is now handled by ShopController

        public void RerollShop()
        {
            int rerollCost = GameSession.Economy.GetCurrentRerollCost();
            if (GameSession.Economy.TrySpendGold(rerollCost))
            {
                GameSession.Economy.IncrementRerollCount();
                GenerateShopItems(GameSession.CurrentRunState.NextShopItemCount);
                Debug.Log($"Rerolled shop for {rerollCost} gold.");
                OnShopDataUpdated?.Invoke(); // Notify UI
            }
            else
            {
                OnMessageDisplayed?.Invoke("Not enough gold to reroll!");
                Debug.LogWarning("Not enough gold to reroll!");
            }
        }

        private void GenerateShopShip()
        {
            int currentFloorIndex = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : 0;
            int mapLength = MapManager.Instance != null ? MapManager.Instance.mapLength : 1; // Default to 1 to avoid division by zero if MapManager not found
            List<RarityWeight> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(currentFloorIndex, mapLength, false);

            Rarity selectedRarity = Rarity.Bronze; // Default to Bronze
            if (rarityProbabilities.Any())
            {
                int totalWeight = rarityProbabilities.Sum(p => p.weight);
                int randomNumber = UnityEngine.Random.Range(0, totalWeight);

                foreach (var prob in rarityProbabilities)
                {
                    if (randomNumber < prob.weight)
                    {
                        selectedRarity = prob.rarity;
                        break;
                    }
                    randomNumber -= prob.weight;
                }
            }

            List<ShipSO> availableShipsOfRarity = GameDataRegistry.GetAllShips()
                .Where(ship => ship.rarity == selectedRarity && (GameSession.PlayerShip == null || ship.id != GameSession.PlayerShip.Def.id))
                .ToList();

            if (availableShipsOfRarity.Count > 0)
            {
                _currentShopShip = availableShipsOfRarity[UnityEngine.Random.Range(0, availableShipsOfRarity.Count)];
            }
            else
            {
                Debug.LogWarning($"No ships of {selectedRarity} rarity found (excluding current ship) for floor {currentFloorIndex}. Trying other rarities.");
                // Fallback: if no ships of the selected rarity, try other rarities, still excluding current ship
                List<ShipSO> allAvailableShips = GameDataRegistry.GetAllShips()
                    .Where(ship => GameSession.PlayerShip == null || ship.id != GameSession.PlayerShip.Def.id)
                    .ToList();
                if (allAvailableShips.Count > 0)
                {
                    _currentShopShip = allAvailableShips[UnityEngine.Random.Range(0, allAvailableShips.Count)];
                }
                else
                {
                    Debug.LogWarning("No ships found in GameDataRegistry to sell in shop (excluding current ship).");
                    _currentShopShip = null;
                }
            }
        }

        public void BuyShip(ShipSO ship)
        {
            int cost = ship.Cost; // Assuming ShipSO has a cost field
            if (GameSession.Economy.TrySpendGold(cost))
            {
                GameSession.PlayerShip = new Core.ShipState(ship); // Replace player's current ship
                Debug.Log($"Bought {ship.displayName} for {cost} gold. Current ship is now {ship.displayName}.");
                _currentShopShip = null; // Ship bought, remove from shop
                OnShopDataUpdated?.Invoke(); // Notify UI
                OnMessageDisplayed?.Invoke($"Purchased {ship.displayName}!");
            }
            else
            {
                OnMessageDisplayed?.Invoke("Not enough gold to buy this ship!");
                Debug.LogWarning("Not enough gold to buy this ship!");
            }
        }

        public void LeaveShop()
        {
            GameSession.Economy.ResetRerollCount(); // Reset reroll cost for next shop
            UnityEngine.SceneManagement.SceneManager.LoadScene("Run"); // Return to map

            // Hide the shop UI (ShopController will handle this, but keep for now if ShopManager directly controls scene)
            // _shopUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
