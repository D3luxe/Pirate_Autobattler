using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Core;
using Pirate.MapGen;
using UnityEngine;

namespace PirateRoguelike.Services
{
    public static class ItemGenerationService
    {
        public static List<ItemSO> GenerateRandomItems(int count, int floorIndex, bool isElite)
        {
            int mapLength = MapManager.Instance != null ? MapManager.Instance.mapLength : 1;
            List<RarityWeight> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(floorIndex, mapLength, isElite);

            List<ItemSO> availableItems = GameDataRegistry.GetAllItems();
            List<ItemSO> generatedItems = new List<ItemSO>();

            for (int i = 0; i < count; i++)
            {
                Rarity selectedRarity = GetRandomRarity(rarityProbabilities);
                ItemSO selectedItem = null;
                int attempts = 0;
                const int maxAttempts = 50;

                while (selectedItem == null && attempts < maxAttempts)
                {
                    List<ItemSO> itemsOfSelectedRarity = availableItems.Where(item => item.rarity == selectedRarity).ToList();
                    if (itemsOfSelectedRarity.Any())
                    {
                        ItemSO candidateItem = itemsOfSelectedRarity[Random.Range(0, itemsOfSelectedRarity.Count)];

                        if (!generatedItems.Any(gi => gi.id == candidateItem.id))
                        {
                            ItemInstance ownedItem = GameSession.Inventory.Slots.FirstOrDefault(invSlot => invSlot.Item != null && invSlot.Item.Def.id == candidateItem.id)?.Item;
                            if (ownedItem != null && ownedItem.Def.rarity > candidateItem.rarity)
                            {
                                ItemSO higherRarityVersion = GameDataRegistry.GetItem(candidateItem.id, ownedItem.Def.rarity);
                                if (higherRarityVersion != null)
                                {
                                    selectedItem = higherRarityVersion;
                                }
                                else
                                {
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
                    if (availableItems.Any())
                    {
                        generatedItems.Add(availableItems[Random.Range(0, availableItems.Count)]);
                    }
                }
            }
            return generatedItems;
        }

        private static Rarity GetRandomRarity(List<RarityWeight> probabilities)
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
    }
}
