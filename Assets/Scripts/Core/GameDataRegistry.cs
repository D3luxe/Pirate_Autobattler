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

    public static List<RarityProbability> GetRarityProbabilitiesForFloor(int floorIndex, int mapLength)
    {
        if (_runConfig == null)
        {
            Debug.LogError("RunConfigSO is null in GameDataRegistry. Cannot get rarity probabilities.");
            return new List<RarityProbability>();
        }

        List<RarityProbability> interpolatedProbabilities = new List<RarityProbability>();

        // Get all unique rarities from both start and end lists
        var allRarities = _runConfig.startRarityProbabilities
                                    .Select(rp => rp.rarity)
                                    .Union(_runConfig.endRarityProbabilities.Select(rp => rp.rarity))
                                    .Distinct();

        foreach (Rarity rarity in allRarities)
        {
            // Get start and end RarityProbability for this rarity
            RarityProbability startRP = _runConfig.startRarityProbabilities.FirstOrDefault(rp => rp.rarity == rarity);
            RarityProbability endRP = _runConfig.endRarityProbabilities.FirstOrDefault(rp => rp.rarity == rarity);

            int startWeight = startRP != null ? startRP.weight : 0;
            int endWeight = endRP != null ? endRP.weight : 0;
            int floorAvailable = startRP != null ? startRP.floorAvailable : 0;
            int floorUnavailable = startRP != null ? startRP.floorUnavailable : int.MaxValue; // Use floorUnavailable from startRP

            // If current floor is before rarity is available OR at/after it becomes unavailable, weight is 0
            if (floorIndex < floorAvailable || floorIndex >= floorUnavailable)
            {
                interpolatedProbabilities.Add(new RarityProbability { rarity = rarity, weight = 0, floorAvailable = floorAvailable, floorUnavailable = floorUnavailable });
                continue;
            }

            // Calculate effective range for interpolation
            int effectiveStartFloor = floorAvailable;
            int effectiveEndFloor = floorUnavailable - 1; // Interpolate up to the floor *before* it becomes unavailable

            // If the effective range is invalid or a single point, handle accordingly
            if (effectiveEndFloor < effectiveStartFloor)
            {
                // This means floorUnavailable is <= floorAvailable, so the rarity is never available for interpolation
                interpolatedProbabilities.Add(new RarityProbability { rarity = rarity, weight = 0, floorAvailable = floorAvailable, floorUnavailable = floorUnavailable });
                continue;
            }

            float normalizedFloor = 0;
            if (effectiveEndFloor > effectiveStartFloor) // If there's a range to interpolate over
            {
                normalizedFloor = (float)(floorIndex - effectiveStartFloor) / (effectiveEndFloor - effectiveStartFloor);
            }
            else // If effectiveStartFloor == effectiveEndFloor (single floor availability)
            {
                normalizedFloor = 1; // It's the "end" of its very short effective life
            }

            float interpolationFactor = _runConfig.rarityInterpolationCurve.Evaluate(normalizedFloor);

            int interpolatedWeight = Mathf.RoundToInt(Mathf.Lerp(startWeight, endWeight, interpolationFactor));
            interpolatedProbabilities.Add(new RarityProbability { rarity = rarity, weight = interpolatedWeight, floorAvailable = floorAvailable, floorUnavailable = floorUnavailable });
        }

        // Ensure total weight is not zero to avoid division by zero errors later
        if (interpolatedProbabilities.Sum(rp => rp.weight) == 0 && interpolatedProbabilities.Any())
        {
            Debug.LogWarning($"Interpolated rarity probabilities for floor {floorIndex} resulted in zero total weight. This might lead to issues.");
        }

        return interpolatedProbabilities;
    }
}
