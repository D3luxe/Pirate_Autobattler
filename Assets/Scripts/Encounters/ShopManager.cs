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

namespace PirateRoguelike.Encounters
{
    public class ShopManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument _shopUIDocument;

        private VisualElement _shopItemsContainer;
        private VisualElement _shopShipContainer;
        private Label _playerGoldLabel;
        private Label _rerollCostLabel;
        private Button _rerollButton;
        private Button _leaveShopButton;
        

        private List<ItemSO> _currentShopItems = new List<ItemSO>();
        private ShipSO _currentShopShip;
        private RunConfigSO _runConfig;
        private int _rerollsThisShop = 0;

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

            var root = _shopUIDocument.rootVisualElement;
            _shopItemsContainer = root.Q<VisualElement>("shop-items-container");
            _shopShipContainer = root.Q<VisualElement>("shop-ship-container");
            _playerGoldLabel = root.Q<Label>("player-gold-label");
            _rerollCostLabel = root.Q<Label>("reroll-cost-label");
            _rerollButton = root.Q<Button>("reroll-button");
            _leaveShopButton = root.Q<Button>("leave-shop-button");

            _rerollButton.clicked += RerollShop;
            _leaveShopButton.clicked += LeaveShop;

            GenerateShopItems();
            GenerateShopShip();
            UpdateShopUI();
        }

        private void GenerateShopItems()
        {
            _currentShopItems.Clear();
            int currentFloorIndex = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : 0;
            int mapLength = MapManager.Instance != null ? MapManager.Instance.mapLength : 1; // Default to 1 to avoid division by zero if MapManager not found
            List<RarityWeight> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(currentFloorIndex, mapLength, false);

            List<ItemSO> availableItems = GameDataRegistry.GetAllItems();
            List<ItemSO> generatedItems = new List<ItemSO>();

            for (int i = 0; i < 3; i++) // Generate 3 unique items
            {
                Rarity selectedRarity = GetRandomRarity(rarityProbabilities);
                ItemSO selectedItem = null;
                int attempts = 0;
                const int maxAttempts = 50; // Prevent infinite loops

                while (selectedItem == null && attempts < maxAttempts)
                {
                    List<ItemSO> itemsOfSelectedRarity = availableItems.Where(item => item.rarity == selectedRarity).ToList();
                    if (itemsOfSelectedRarity.Any())
                    {
                        ItemSO candidateItem = itemsOfSelectedRarity[Random.Range(0, itemsOfSelectedRarity.Count)];

                        // Check for duplicates in current shop offerings
                        if (!generatedItems.Any(gi => gi.id == candidateItem.id))
                        {
                            // Check for highest rarity owned rule
                            ItemInstance ownedItem = GameSession.Inventory.Slots.FirstOrDefault(invSlot => invSlot.Item != null && invSlot.Item.Def.id == candidateItem.id)?.Item;
                            if (ownedItem != null && ownedItem.Def.rarity > candidateItem.rarity)
                            {
                                // Player owns a higher rarity version, try to get that one or next rarity
                                ItemSO higherRarityVersion = GameDataRegistry.GetItem(candidateItem.id, ownedItem.Def.rarity);
                                if (higherRarityVersion != null)
                                {
                                    selectedItem = higherRarityVersion;
                                } else {
                                    // Fallback if specific higher rarity version not found
                                    selectedItem = candidateItem;
                                }
                            }
                            else
                            {
                                selectedItem = candidateItem;
                            }
                        }
                    }
                    attempts++;
                }

                if (selectedItem != null)
                {
                    generatedItems.Add(selectedItem);
                }
                else
                {
                    Debug.LogWarning($"Could not generate a unique item of rarity {selectedRarity} after {maxAttempts} attempts. Adding a fallback item.");
                    // Fallback: add any available item if generation fails
                    if (availableItems.Any())
                    {
                        generatedItems.Add(availableItems[Random.Range(0, availableItems.Count)]);
                    }
                }
            }
            _currentShopItems = generatedItems;
        }

        private Rarity GetRandomRarity(List<RarityWeight> probabilities)
        {
            int totalWeight = probabilities.Sum(p => p.weight);
            int randomNumber = Random.Range(0, totalWeight);

            foreach (var prob in probabilities)
            {
                if (randomNumber < prob.weight)
                {
                    return prob.rarity;
                }
                randomNumber -= prob.weight;
            }
            return Rarity.Bronze; // Fallback
        }

        private void UpdateShopUI()
        {
            _shopItemsContainer.Clear();
            foreach (var itemSO in _currentShopItems)
            {
                var shopItemViewData = new ShopItemViewData(itemSO, GetRarityColor(itemSO.rarity), GameSession.Economy.Gold >= itemSO.Cost);
                var shopItemElement = new ShopItemElement();
                shopItemElement.Bind(shopItemViewData);
                shopItemElement.RegisterBuyButtonCallback(() => BuyItem(itemSO));
                _shopItemsContainer.Add(shopItemElement);
            }

            _shopShipContainer.Clear();
            if (_currentShopShip != null)
            {
                var shipViewUI = new ShipViewUI();
                // Create a dummy ShipState for the shop ship to bind to ShipViewUI
                // This is a temporary solution, ideally ShipViewUI should bind to ShipSO directly or a dedicated ViewModel
                var shopShipState = new ShipState(_currentShopShip);
                var enemyShipViewData = new PirateRoguelike.UI.EnemyShipViewData(shopShipState); // Reusing EnemyShipViewData for now
                shipViewUI.Bind(enemyShipViewData);
                _shopShipContainer.Add(shipViewUI);
                // Add a buy button for the ship
                var buyShipButton = new Button(() => BuyShip(_currentShopShip)) { text = $"Buy Ship ({_currentShopShip.Cost} Gold)" };
                _shopShipContainer.Add(buyShipButton);
            }

            _playerGoldLabel.text = $"Gold: {GameSession.Economy.Gold}";
            _rerollCostLabel.text = $"Reroll: {GameSession.Economy.GetCurrentRerollCost()} Gold";
        }

        private Color GetRarityColor(Rarity rarity)
        {
            // This needs to be consistent with PlayerUIThemeSO.rarityColors
            // For now, hardcode or get from a central place
            switch (rarity)
            {
                case Rarity.Bronze: return new Color(0.8f, 0.5f, 0.2f); // Example Bronze
                case Rarity.Silver: return new Color(0.7f, 0.7f, 0.7f); // Example Silver
                case Rarity.Gold: return new Color(1.0f, 0.8f, 0.0f); // Example Gold
                case Rarity.Diamond: return new Color(0.0f, 0.8f, 1.0f); // Example Diamond
                default: return Color.white;
            }
        }

        public void BuyItem(ItemSO item)
        {
            int cost = item.Cost; // Use item.cost
            if (GameSession.Economy.TrySpendGold(cost))
            {
                // Add item to player inventory
                if (GameSession.Inventory.AddItem(new ItemInstance(item))) // Create a new ItemInstance
                {
                    Debug.Log($"Bought {item.displayName} for {cost} gold and added to inventory.");
                    _currentShopItems.Remove(item); // Remove from shop display
                    if (_currentShopItems.Count == 0)
                    {
                        GameSession.Economy.MarkFreeRerollAvailable();
                    }
                    UpdateShopUI();
                    // Update the persistent InventoryUI to reflect the new item
                    if (InventoryUI.Instance != null)
                    {
                        InventoryUI.Instance.RefreshAll();
                    }
                }
                else
                {
                    // Display message directly on UI
                    Debug.LogWarning("Inventory is full!");
                    // Refund gold if inventory is full
                    GameSession.Economy.AddGold(cost);
                    UpdateShopUI(); // Update gold display
                }
            }
            else
            {
                // Display message directly on UI
                Debug.LogWarning("Not enough gold!");
            }
        }

        public void RerollShop()
        {
            int rerollCost = GameSession.Economy.GetCurrentRerollCost();
            if (GameSession.Economy.TrySpendGold(rerollCost))
            {
                GameSession.Economy.IncrementRerollCount();
                GenerateShopItems();
                UpdateShopUI();
                Debug.Log($"Rerolled shop for {rerollCost} gold.");
            }
            else
            {
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
                int randomNumber = Random.Range(0, totalWeight);

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
                _currentShopShip = availableShipsOfRarity[Random.Range(0, availableShipsOfRarity.Count)];
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
                    _currentShopShip = allAvailableShips[Random.Range(0, allAvailableShips.Count)];
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
                GameSession.PlayerShip = new ShipState(ship); // Replace player's current ship
                Debug.Log($"Bought {ship.displayName} for {cost} gold. Current ship is now {ship.displayName}.");
                _currentShopShip = null; // Ship bought, remove from shop
                UpdateShopUI();
            }
            else
            {
                Debug.LogWarning("Not enough gold to buy this ship!");
            }
        }

        public void LeaveShop()
        {
            GameSession.Economy.ResetRerollCount(); // Reset reroll cost for next shop
            UnityEngine.SceneManagement.SceneManager.LoadScene("Run"); // Return to map

            // Hide the shop UI
            _shopUIDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}