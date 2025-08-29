using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;
using Pirate.MapGen; // Added for Rules

[CreateAssetMenu(fileName = "RunConfigSO", menuName = "Data/RunConfigSO")]
public class RunConfigSO : ScriptableObject
{
    public int startingLives;
    public int startingGold;
    public int inventorySize;
    public int rewardGoldPerWin;

    // Re-adding these fields
    public int rerollBaseCost;
    public float rerollGrowth;

    [Header("Rarity Settings")]
    public List<RarityProbability> startRarityProbabilities;
    public List<RarityProbability> endRarityProbabilities;
    public AnimationCurve rarityInterpolationCurve = AnimationCurve.Linear(0, 0, 1, 1); // Default to linear interpolation

    public RulesSO rules; // Map generation rules
}


