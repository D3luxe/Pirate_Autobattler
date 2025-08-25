using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Pirate/Data/Enemy")]
public class EnemySO : ScriptableObject
{
    public string id;
    public string displayName;
    public string shipId; // The ID of the ShipSO this enemy uses
    public List<ItemSO> itemLoadout;
    public TargetingStrategy targetingStrategy; // AI targeting strategy
}
