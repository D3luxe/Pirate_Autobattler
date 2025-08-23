using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;

[CreateAssetMenu(fileName = "RunConfigSO", menuName = "Data/RunConfigSO")]
public class RunConfigSO : ScriptableObject
{
    public int mapLength;
    public int enemyHealthMin;
    public int enemyHealthMax;
    public int startingLives;
    public int startingGold;
    public int rerollBaseCost;
    public float rerollGrowth;
    public int inventorySize;
    public int rewardGoldPerWin;
    public int shopEveryN;
    public int portEveryN;
    public int eliteEveryN;
    public List<FloorRaritySettings> floorRaritySettings;
}

[System.Serializable]
public class FloorRaritySettings
{
    public int minFloorIndex;
    public int maxFloorIndex;
    public List<RarityProbability> rarityProbabilities;
}
