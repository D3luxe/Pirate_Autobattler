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
    public List<RarityMilestone> rarityMilestones; // NEW
    public int eliteModifier; // NEW

    public RulesSO rules; // Map generation rules
}


