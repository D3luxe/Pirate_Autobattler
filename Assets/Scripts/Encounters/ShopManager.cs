using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopUI shopUI;

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

        // Show InventoryUI when entering shop (full display)
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetInventoryVisibility(true, true);
            InventoryUI.Instance.RefreshAll();
        }

        GenerateShopItems();
        GenerateShopShip();
        UpdateShopUI();
    }

    private void GenerateShopItems()
    {
        _currentShopItems.Clear();
        int currentFloorIndex = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : 0;
        List<RarityProbability> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(currentFloorIndex);

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
                        ItemInstance ownedItem = GameSession.Inventory.Items.FirstOrDefault(invItem => invItem != null && invItem.Def.id == candidateItem.id);
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

    private Rarity GetRandomRarity(List<RarityProbability> probabilities)
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
        if (shopUI != null)
        {
            shopUI.SetShopManager(this);
            shopUI.DisplayShopItems(_currentShopItems);
            shopUI.DisplayShopShip(_currentShopShip, _currentShopShip != null ? _currentShopShip.Cost : 0);
            shopUI.UpdateRerollCost(GameSession.Economy.GetCurrentRerollCost());
            shopUI.UpdatePlayerGold(GameSession.Economy.Gold);
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
                shopUI.DisplayMessage("Inventory is full!");
                // Refund gold if inventory is full
                GameSession.Economy.AddGold(cost);
                UpdateShopUI(); // Update gold display
            }
        }
        else
        {
            shopUI.DisplayMessage("Not enough gold!");
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
            shopUI.DisplayMessage("Not enough gold to reroll!");
        }
    }

    private void GenerateShopShip()
    {
        int currentFloorIndex = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : 0;
        List<RarityProbability> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(currentFloorIndex);

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
            shopUI.DisplayMessage("Not enough gold to buy this ship!");
        }
    }

    public void LeaveShop()
    {
        GameSession.Economy.ResetRerollCount(); // Reset reroll cost for next shop
        UnityEngine.SceneManagement.SceneManager.LoadScene("Run"); // Return to map

        // Show InventoryUI when leaving shop
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetInventoryVisibility(true, true);
            InventoryUI.Instance.RefreshAll();
        }
    }
}