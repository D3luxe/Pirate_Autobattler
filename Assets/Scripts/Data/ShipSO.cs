using UnityEngine;
using PirateRoguelike.Data;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShipSO", menuName = "Data/ShipSO")]
public class ShipSO : ScriptableObject
{
    public string id;
    public string displayName;
    public int baseMaxHealth;
    public int baseItemSlots;
    public List<ItemRef> builtInItems;
    public List<ItemRef> itemLoadout;
    public Sprite art;
    public Rarity rarity;
    public int Cost;
}
