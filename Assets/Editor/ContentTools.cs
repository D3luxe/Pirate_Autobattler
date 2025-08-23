using UnityEngine;
using UnityEditor;
using System.IO;
using PirateRoguelike.Data;
using PirateRoguelike.Combat;

public class ContentTools : EditorWindow
{
    [MenuItem("Game/Content Editor")]
    public static void ShowWindow()
    {
        GetWindow<ContentTools>("Game Content");
    }

    void OnGUI()
    {
        GUILayout.Label("Game Content Management", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Starter Content"))
        {
            CreateStarterContent();
        }
        if (GUILayout.Button("Create Scene Structure"))
        {
            CreateSceneStructure();
        }
        if (GUILayout.Button("Create Core Prefabs"))
        {
            CreateCorePrefabs();
        }
    }

    [MenuItem("Game/Tools/Create Starter Content")]
    public static void CreateStarterContent()
    {
        Debug.Log("Creating starter content...");

        // Ensure all directories exist inside the Resources folder
        Directory.CreateDirectory("Assets/Resources/GameData");
        Directory.CreateDirectory("Assets/Resources/GameData/Items");
        Directory.CreateDirectory("Assets/Resources/GameData/Ships");
        Directory.CreateDirectory("Assets/Resources/GameData/Encounters");

        CreateRunConfig();
        CreateItems();
        CreateShips();
        CreateEvents();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Starter content created successfully!");
    }

    private static void CreateRunConfig()
    {
        RunConfigSO config = ScriptableObject.CreateInstance<RunConfigSO>();
        config.startingGold = 10; // Default starting gold
        AssetDatabase.CreateAsset(config, "Assets/Resources/GameData/RunConfiguration.asset");
    }

    private static void CreateItems()
    {
        // Cannon (Damage)
        ItemSO cannon = ScriptableObject.CreateInstance<ItemSO>();
        cannon.id = "item_cannon_wood";
        cannon.displayName = "Wooden Cannon";
        cannon.rarity = Rarity.Bronze;
        cannon.isActive = true;
        cannon.cooldownSec = 5f;
        cannon.baseValue = 5; // Base damage
        cannon.abilities = new System.Collections.Generic.List<Ability>
        {
            new Ability
            {
                triggerType = TriggerType.OnItemReady,
                actions = new System.Collections.Generic.List<AbilityAction>
                {
                    new AbilityAction { actionType = ActionType.Damage, values = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Bronze, value = cannon.baseValue } } }
                }
            }
        };
        AssetDatabase.CreateAsset(cannon, "Assets/Resources/GameData/Items/item_cannon_wood.asset");

        // Deckhand (Heal)
        ItemSO deckhand = ScriptableObject.CreateInstance<ItemSO>();
        deckhand.id = "item_deckhand";
        deckhand.displayName = "Deckhand";
        deckhand.rarity = Rarity.Bronze;
        deckhand.isActive = true;
        deckhand.cooldownSec = 2f;
        deckhand.baseValue = 1; // Base heal
        deckhand.abilities = new System.Collections.Generic.List<Ability>
        {
            new Ability
            {
                triggerType = TriggerType.OnItemReady,
                actions = new System.Collections.Generic.List<AbilityAction>
                {
                    new AbilityAction { actionType = ActionType.Heal, values = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Bronze, value = deckhand.baseValue } } }
                }
            }
        };
        AssetDatabase.CreateAsset(deckhand, "Assets/Resources/GameData/Items/item_deckhand.asset");

        // Flintlock (No active ability, passive or instant)
        ItemSO flintlock = ScriptableObject.CreateInstance<ItemSO>();
        flintlock.id = "item_flintlock";
        flintlock.displayName = "Flintlock";
        flintlock.rarity = Rarity.Bronze;
        flintlock.isActive = false;
        AssetDatabase.CreateAsset(flintlock, "Assets/Resources/GameData/Items/item_flintlock.asset");

        // Powder Monkey (Example for a buff/debuff, currently no specific ability)
        ItemSO powderMonkey = ScriptableObject.CreateInstance<ItemSO>();
        powderMonkey.id = "item_powder_monkey";
        powderMonkey.displayName = "Powder Monkey";
        powderMonkey.rarity = Rarity.Bronze;
        powderMonkey.isActive = true;
        powderMonkey.cooldownSec = 10f;
        powderMonkey.baseValue = 1; // Example: could be a buff value
        AssetDatabase.CreateAsset(powderMonkey, "Assets/Resources/GameData/Items/item_powder_monkey.asset");

        // New: Fire Bomb (Burn Debuff)
        ItemSO fireBomb = ScriptableObject.CreateInstance<ItemSO>();
        fireBomb.id = "item_fire_bomb";
        fireBomb.displayName = "Fire Bomb";
        fireBomb.rarity = Rarity.Silver;
        fireBomb.isActive = true;
        fireBomb.cooldownSec = 8f;
        fireBomb.baseValue = 1; // Damage per stack per tick
        fireBomb.abilities = new System.Collections.Generic.List<Ability>
        {
            new Ability
            {
                triggerType = TriggerType.OnItemReady,
                actions = new System.Collections.Generic.List<AbilityAction>
                {
                    new AbilityAction
                    {
                        actionType = ActionType.Burn,
                        values = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = fireBomb.baseValue } },
                        durations = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 5f } },
                        tickIntervals = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 1f } },
                        stacks = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 3 } }
                    }
                }
            }
        };
        AssetDatabase.CreateAsset(fireBomb, "Assets/Resources/GameData/Items/item_fire_bomb.asset");

        // New: Poison Vial (Poison Debuff)
        ItemSO poisonVial = ScriptableObject.CreateInstance<ItemSO>();
        poisonVial.id = "item_poison_vial";
        poisonVial.displayName = "Poison Vial";
        poisonVial.rarity = Rarity.Silver;
        poisonVial.isActive = true;
        poisonVial.cooldownSec = 7f;
        poisonVial.baseValue = 1; // Damage per stack per tick
        poisonVial.abilities = new System.Collections.Generic.List<Ability>
        {
            new Ability
            {
                triggerType = TriggerType.OnItemReady,
                actions = new System.Collections.Generic.List<AbilityAction>
                {
                    new AbilityAction
                    {
                        actionType = ActionType.Poison,
                        values = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = poisonVial.baseValue } },
                        durations = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 10f } },
                        tickIntervals = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 2f } },
                        stacks = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Silver, value = 2 } }
                    }
                }
            }
        };
        AssetDatabase.CreateAsset(poisonVial, "Assets/Resources/GameData/Items/item_poison_vial.asset");

        // New: Stun Grenade (Stun Debuff)
        ItemSO stunGrenade = ScriptableObject.CreateInstance<ItemSO>();
        stunGrenade.id = "item_stun_grenade";
        stunGrenade.displayName = "Stun Grenade";
        stunGrenade.rarity = Rarity.Gold;
        stunGrenade.isActive = true;
        stunGrenade.cooldownSec = 12f;
        stunGrenade.abilities = new System.Collections.Generic.List<Ability>
        {
            new Ability
            {
                triggerType = TriggerType.OnItemReady,
                actions = new System.Collections.Generic.List<AbilityAction>
                {
                    new AbilityAction
                    {
                        actionType = ActionType.Stun,
                        durations = new System.Collections.Generic.List<RarityTieredValue> { new RarityTieredValue { rarity = Rarity.Gold, value = 3f } }
                    } // Stun has no value or stacks
                }
            }
        };
        AssetDatabase.CreateAsset(stunGrenade, "Assets/Resources/GameData/Items/item_stun_grenade.asset");
    }

    private static void CreateShips()
    {
        ShipSO brigantine = ScriptableObject.CreateInstance<ShipSO>();
        brigantine.id = "ship_brigantine";
        brigantine.displayName = "Brigantine";
        brigantine.rarity = Rarity.Bronze;
        brigantine.baseMaxHealth = 100;
        brigantine.baseItemSlots = 4;
        AssetDatabase.CreateAsset(brigantine, "Assets/Resources/GameData/Ships/ship_brigantine.asset");

        ShipSO ironclad = ScriptableObject.CreateInstance<ShipSO>();
        ironclad.id = "ship_ironclad";
        ironclad.displayName = "Ironclad";
        ironclad.rarity = Rarity.Silver;
        ironclad.baseMaxHealth = 150;
        ironclad.baseItemSlots = 3;
        AssetDatabase.CreateAsset(ironclad, "Assets/Resources/GameData/Ships/ship_ironclad.asset");

        ShipSO corsair = ScriptableObject.CreateInstance<ShipSO>();
        corsair.id = "ship_corsair";
        corsair.displayName = "Corsair";
        corsair.rarity = Rarity.Silver;
        corsair.baseMaxHealth = 80;
        corsair.baseItemSlots = 5;
        AssetDatabase.CreateAsset(corsair, "Assets/Resources/GameData/Ships/ship_corsair.asset");
        
        ShipSO ghostShip = ScriptableObject.CreateInstance<ShipSO>();
        ghostShip.id = "ship_ghostly_galleon";
        ghostShip.displayName = "Ghostly Galleon";
        ghostShip.rarity = Rarity.Gold;
        ghostShip.baseMaxHealth = 99;
        ghostShip.baseItemSlots = 4;
        AssetDatabase.CreateAsset(ghostShip, "Assets/Resources/GameData/Ships/ship_ghostly_galleon.asset");
    }

    private static void CreateEvents()
    {
        EncounterSO ghostEvent = ScriptableObject.CreateInstance<EncounterSO>();
        ghostEvent.id = "event_ghost_ship";
        ghostEvent.type = EncounterType.Event;
        ghostEvent.eventTitle = "A Ghostly Encounter";
        ghostEvent.eventDescription = "A spectral galleon offers to trade ships. Its power is immense, but it seems... unstable.";
        AssetDatabase.CreateAsset(ghostEvent, "Assets/Resources/GameData/Encounters/event_ghost_ship.asset");

        EncounterSO battle = ScriptableObject.CreateInstance<EncounterSO>();
        battle.id = "enc_battle";
        battle.type = EncounterType.Battle;
        battle.eventTitle = "A Skirmish"; // Using event title for node name for now
        AssetDatabase.CreateAsset(battle, "Assets/Resources/GameData/Encounters/enc_battle.asset");

        EncounterSO shop = ScriptableObject.CreateInstance<EncounterSO>();
        shop.id = "enc_shop";
        shop.type = EncounterType.Shop;
        shop.eventTitle = "A Shady Port";
        AssetDatabase.CreateAsset(shop, "Assets/Resources/GameData/Encounters/enc_shop.asset");

        EncounterSO port = ScriptableObject.CreateInstance<EncounterSO>();
        port.id = "enc_port";
        port.type = EncounterType.Port;
        port.eventTitle = "A Safe Haven";
        AssetDatabase.CreateAsset(port, "Assets/Resources/GameData/Encounters/enc_port.asset");

        EncounterSO boss = ScriptableObject.CreateInstance<EncounterSO>();
        boss.id = "enc_boss";
        boss.type = EncounterType.Boss;
        boss.eventTitle = "The Pirate Lord";
        AssetDatabase.CreateAsset(boss, "Assets/Resources/GameData/Encounters/enc_boss.asset");
    }

    [MenuItem("Game/Tools/Create Scene Structure")]
    public static void CreateSceneStructure()
    {
        string[] sceneNames = { "MainMenu", "Run", "Battle", "Shop", "Event", "Port", "Boss" };
        foreach (string name in sceneNames)
        {
            string path = $"Assets/Scenes/{name}.unity";
            if (!File.Exists(path))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Game/Tools/Create Core Prefabs")]
    public static void CreateCorePrefabs()
    {
        // This is a placeholder for creating prefabs.
        Debug.Log("TODO: Implement prefab creation for ItemCard, ShipView, etc.");
    }
}
