using System.Collections.Generic;
using PirateRoguelike.Data;
using System.Linq;
using UnityEngine;

public static class RewardService
{
    public static List<ItemSO> GenerateBattleRewards(int currentDepth, RunConfigSO config)
    {
        List<ItemSO> rewards = new List<ItemSO>();
        List<ItemSO> allAvailableItems = GameDataRegistry.GetAllItems();

        if (allAvailableItems == null || allAvailableItems.Count == 0)
        {
            Debug.LogWarning("No items found in GameDataRegistry to generate rewards.");
            return rewards; // Return empty list if no items are available
        }

        // Determine rarity probabilities for the current depth
        FloorRaritySettings currentFloorRaritySettings = config.floorRaritySettings
            .FirstOrDefault(s => currentDepth >= s.minFloorIndex && currentDepth <= s.maxFloorIndex);

        if (currentFloorRaritySettings == null)
        {
            Debug.LogWarning($"No FloorRaritySettings found for depth {currentDepth}. Using default probabilities.");
            // Fallback to a default if no specific settings are found
            currentFloorRaritySettings = new FloorRaritySettings
            {
                rarityProbabilities = new List<RarityProbability>
                {
                    new RarityProbability { rarity = Rarity.Bronze, weight = 80 },
                    new RarityProbability { rarity = Rarity.Silver, weight = 20 }
                }
            };
        }

        // Generate 3 unique item rewards
        for (int i = 0; i < 3; i++)
        {
            Rarity selectedRarity = GetRandomRarity(currentFloorRaritySettings.rarityProbabilities);
            List<ItemSO> itemsOfSelectedRarity = allAvailableItems.Where(item => item.rarity == selectedRarity && !rewards.Contains(item)).ToList();

            if (itemsOfSelectedRarity.Any())
            {
                ItemSO selectedItem = itemsOfSelectedRarity[Random.Range(0, itemsOfSelectedRarity.Count)];
                rewards.Add(selectedItem);
            }
            else
            {
                Debug.LogWarning($"Could not find a unique item of rarity {selectedRarity} for reward. Trying another rarity.");
                // Fallback: try to find any unique item if specific rarity is exhausted
                List<ItemSO> anyUniqueItem = allAvailableItems.Where(item => !rewards.Contains(item)).ToList();
                if (anyUniqueItem.Any())
                {
                    rewards.Add(anyUniqueItem[Random.Range(0, anyUniqueItem.Count)]);
                }
                else
                {
                    Debug.LogWarning("No unique items left to generate rewards.");
                    break; // No more unique items to add
                }
            }
        }

        return rewards;
    }

    private static Rarity GetRandomRarity(List<RarityProbability> probabilities)
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
        return probabilities.Last().rarity; // Fallback
    }
}
