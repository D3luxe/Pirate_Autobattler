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

    public static List<RarityProbability> GetRarityProbabilitiesForFloor(int floorIndex)
    {
        if (_runConfig == null)
        {
            Debug.LogError("RunConfigSO is null in GameDataRegistry. Cannot get rarity probabilities.");
            return new List<RarityProbability>();
        }

        foreach (var settings in _runConfig.floorRaritySettings)
        {
            if (floorIndex >= settings.minFloorIndex && floorIndex <= settings.maxFloorIndex)
            {
                return settings.rarityProbabilities;
            }
        }

        Debug.LogWarning($"No rarity settings found for floor index {floorIndex}. Returning empty list.");
        return new List<RarityProbability>();
    }
}
