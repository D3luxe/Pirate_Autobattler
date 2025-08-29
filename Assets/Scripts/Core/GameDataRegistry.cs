using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;

// A central, load-once repository for all ScriptableObject game data.
public static class GameDataRegistry
{
    private static Dictionary<string, ItemSO> _items;
    private static Dictionary<string, ShipSO> _ships;
    private static Dictionary<string, EncounterSO> _encounters;
    private static RunConfigSO _runConfig; // Add this

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        _items = Resources.LoadAll<ItemSO>("GameData/Items").ToDictionary(x => x.id, x => x);
        _ships = Resources.LoadAll<ShipSO>("GameData/Ships").ToDictionary(x => x.id, x => x);
        _encounters = Resources.LoadAll<EncounterSO>("GameData/Encounters").ToDictionary(x => x.id, x => x);
        _runConfig = Resources.Load<RunConfigSO>("GameData/RunConfiguration"); // Load the RunConfigSO
        Debug.Log($"GameDataRegistry initialized. Loaded {_items.Count} items, {_ships.Count} ships, {_encounters.Count} encounters.");

        // --- DEBUGGING ADDITION ---
        if (_encounters.TryGetValue("enc_battle", out var battleEncounter))
        {
            Debug.Log($"DEBUG: 'enc_battle' loaded. Enemies count: {(battleEncounter.enemies != null ? battleEncounter.enemies.Count : 0)}");
        }
        // --- END DEBUGGING ADDITION ---
    }

    public static ItemSO GetItem(string id) => _items.TryGetValue(id, out var item) ? item : null;
    public static ItemSO GetItem(string id, Rarity rarity) => _items.Values.FirstOrDefault(item => item.id == id && item.rarity == rarity);
    public static ShipSO GetShip(string id) => _ships.TryGetValue(id, out var ship) ? ship : null;
    public static EncounterSO GetEncounter(string id) => _encounters.TryGetValue(id, out var encounter) ? encounter : null;
    public static RunConfigSO GetRunConfig() => _runConfig;
    public static List<ItemSO> GetAllItems() => _items.Values.ToList();
    public static List<ShipSO> GetAllShips() => _ships.Values.ToList();
    public static List<EncounterSO> GetAllEncounters() => _encounters.Values.ToList();

    // NEW: Milestone-based rarity probability calculation
    public static List<RarityWeight> GetRarityProbabilitiesForFloor(int floorIndex, int mapLength, bool isElite)
    {
        if (_runConfig == null)
        {
            Debug.LogError("RunConfigSO is null in GameDataRegistry. Cannot get rarity probabilities.");
            return new List<RarityWeight>();
        }
        if (_runConfig.rarityMilestones == null || !_runConfig.rarityMilestones.Any())
        {
            Debug.LogWarning("No rarity milestones defined in RunConfigSO. Returning default Bronze rarity.");
            return new List<RarityWeight> { new RarityWeight { rarity = Rarity.Bronze, weight = 1 } };
        }

        int effectiveFloor = floorIndex;
        if (isElite)
        {
            // Clamp effective floor to the range of milestones
            int firstMilestoneFloor = _runConfig.rarityMilestones.Min(m => m.floor);
            int lastMilestoneFloor = _runConfig.rarityMilestones.Max(m => m.floor);
            effectiveFloor = Mathf.Clamp(floorIndex + _runConfig.eliteModifier, firstMilestoneFloor, lastMilestoneFloor);
        }

        // Find the two milestones that bracket the effectiveFloor
        RarityMilestone milestoneFk = _runConfig.rarityMilestones
            .Where(m => m.floor <= effectiveFloor)
            .OrderByDescending(m => m.floor)
            .FirstOrDefault();

        RarityMilestone milestoneFkPlus1 = _runConfig.rarityMilestones
            .Where(m => m.floor >= effectiveFloor)
            .OrderBy(m => m.floor)
            .FirstOrDefault();

        // Handle clamping outside range: floors < first milestone use the first row; floors > last milestone use the last row.
        if (milestoneFk == null) // effectiveFloor is before the first milestone
        {
            milestoneFk = _runConfig.rarityMilestones.OrderBy(m => m.floor).First();
            milestoneFkPlus1 = milestoneFk; // Use the first milestone's weights
        }
        else if (milestoneFkPlus1 == null) // effectiveFloor is after the last milestone
        {
            milestoneFkPlus1 = _runConfig.rarityMilestones.OrderByDescending(m => m.floor).First();
            milestoneFk = milestoneFkPlus1; // Use the last milestone's weights
        }

        List<RarityWeight> interpolatedProbabilities = new List<RarityWeight>();
        var allRarities = System.Enum.GetValues(typeof(Rarity)).Cast<Rarity>().ToList();

        // If both milestones are the same (e.g., effectiveFloor is exactly a milestone, or clamped)
        if (milestoneFk.floor == milestoneFkPlus1.floor)
        {
            foreach (Rarity rarity in allRarities)
            {
                int weight = milestoneFk.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;
                interpolatedProbabilities.Add(new RarityWeight { rarity = rarity, weight = weight });
            }
        }
        else // Interpolate between two different milestones
        {
            float t = (float)(effectiveFloor - milestoneFk.floor) / (milestoneFkPlus1.floor - milestoneFk.floor);

            foreach (Rarity rarity in allRarities)
            {
                int weightFk = milestoneFk.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;
                int weightFkPlus1 = milestoneFkPlus1.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;

                int interpolatedWeight = Mathf.RoundToInt(Mathf.Lerp(weightFk, weightFkPlus1, t));
                interpolatedProbabilities.Add(new RarityWeight { rarity = rarity, weight = interpolatedWeight });
            }
        }

        // Ensure total weight is not zero to avoid division by zero errors later
        if (interpolatedProbabilities.Sum(rp => rp.weight) == 0 && interpolatedProbabilities.Any())
        {
            Debug.LogWarning($"Interpolated rarity probabilities for floor {floorIndex} resulted in zero total weight. This might lead to issues.");
            // Fallback: if all weights are zero, return a default Bronze rarity
            return new List<RarityWeight> { new RarityWeight { rarity = Rarity.Bronze, weight = 1 } };
        }

        return interpolatedProbabilities;
    }
}
