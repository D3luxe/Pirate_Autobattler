using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;

[CreateAssetMenu(fileName = "NewEncounter", menuName = "Pirate/Data/Encounter")]
public class EncounterSO : ScriptableObject
{
    public string id;
    public EncounterType type;
    public float weight = 1.0f; // For weighted random selection
    public bool isElite; // Is this an elite encounter?
    public int eliteRewardDepthBonus; // How many floors deeper to roll rewards for elites

    [Header("UI")]
    public string iconPath; // Path to the icon sprite for this encounter
    [TextArea(3, 5)]
    public string tooltipText; // Text for the tooltip when hovering over this encounter

    [Header("Battle")]
    public List<EnemySO> enemies;

    [Header("Shop")]
    public int shopItemCount = 3;
    // public ShopInventoryRules shopInventoryRules; // TODO

    [Header("Port")]
    public float portHealPercent = 0.3f;

    [Header("Event")]
    public string eventTitle;
    public string eventDescription;
    public int minFloor; // Minimum floor index for this event to appear
    public int maxFloor; // Maximum floor index for this event to appear
    public List<EventChoice> eventChoices;
}
