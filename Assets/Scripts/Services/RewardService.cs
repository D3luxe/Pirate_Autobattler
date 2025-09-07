using System;
using System.Collections.Generic;
using PirateRoguelike.Data;
using PirateRoguelike.Core;
using UnityEngine;

namespace PirateRoguelike.Services
{
    public static class RewardService
    {
        public static event Action<List<ItemSO>, int> OnRewardsAvailable;

        private static List<ItemSO> _currentRewardItems;
        private static int _currentRewardGold;

        public static void GenerateBattleReward(int floorIndex, bool isElite)
        {
            // For now, a fixed number of items. This can be made dynamic later.
            int itemCount = 3;
            _currentRewardItems = ItemGenerationService.GenerateRandomItems(itemCount, floorIndex, isElite);

            // Simple gold reward for now. Can be made more complex.
            _currentRewardGold = 50 + (floorIndex * 5);
        }

        public static void GenerateDebugReward(int floorIndex, bool isElite, int? goldAmount = null, int? itemCount = null)
        {
            // Default item generation
            int actualItemCount = itemCount ?? 3; // Use provided itemCount or default to 3
            if (actualItemCount > 0)
            {
                _currentRewardItems = ItemGenerationService.GenerateRandomItems(actualItemCount, floorIndex, isElite);
            }
            else
            {
                _currentRewardItems = new List<ItemSO>(); // No items if itemCount is 0
            }

            // Default gold generation
            int actualGoldAmount = goldAmount ?? (50 + (floorIndex * 5)); // Use provided goldAmount or default
            _currentRewardGold = actualGoldAmount;
        }

        public static List<ItemSO> GetCurrentRewardItems() => _currentRewardItems;
        public static int GetCurrentRewardGold() => _currentRewardGold;

        public static void ClearRewards()
        {
            _currentRewardItems = null;
            _currentRewardGold = 0;
        }

        public static void RemoveClaimedItem(ItemSO itemToRemove)
        {
            if (_currentRewardItems != null)
            {
                _currentRewardItems.Remove(itemToRemove);
            }
        }
    }
}
