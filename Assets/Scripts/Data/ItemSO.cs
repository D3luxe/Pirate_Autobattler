using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Combat;

[CreateAssetMenu(fileName = "NewItem", menuName = "Pirate/Data/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stats")]
    public Rarity rarity;
    public bool isActive;
    public float cooldownSec;
    public float baseValue; // Damage, healing, etc.
    public List<Ability> abilities;

    [Header("Gameplay")]
    public List<Tag> tags;
    public List<SynergyRule> synergyRules;

    [Header("Shop")]
    public int Cost
    {
        get
        {
            switch (rarity)
            {
                case Rarity.Bronze: return 5;
                case Rarity.Silver: return 10;
                case Rarity.Gold: return 15;
                case Rarity.Diamond: return 25;
                default: return 0;
            }
        }
    }

    public float GetValueForRarity(List<RarityTieredValue> tieredValues, Rarity itemRarity)
    {
        // Find the value for the exact rarity
        RarityTieredValue foundValue = tieredValues.FirstOrDefault(rtv => rtv.rarity == itemRarity);
        if (foundValue != null) return foundValue.value;

        // If not found, try to find the next lower rarity
        for (int i = (int)itemRarity - 1; i >= 0; i--)
        {
            foundValue = tieredValues.FirstOrDefault(rtv => (int)rtv.rarity == i);
            if (foundValue != null) return foundValue.value;
        }

        // Fallback to the lowest rarity value if nothing else is found
        foundValue = tieredValues.OrderBy(rtv => rtv.rarity).FirstOrDefault();
        if (foundValue != null) return foundValue.value;

        return 0f; // Default if no values are defined
    }
}
