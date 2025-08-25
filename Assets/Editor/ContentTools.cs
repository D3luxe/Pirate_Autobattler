using UnityEngine;
using UnityEditor;
using System.IO;
using PirateRoguelike.Data;
using PirateRoguelike.Data.Actions; // New using
using PirateRoguelike.Data.Effects; // New using
using PirateRoguelike.Data.Abilities; // New using
using System.Collections.Generic; // Added for List

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
        Directory.CreateDirectory("Assets/Resources/GameData/Actions"); // New
        Directory.CreateDirectory("Assets/Resources/GameData/Effects"); // New
        Directory.CreateDirectory("Assets/Resources/GameData/Abilities"); // New

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
        Debug.Log("Creating item assets...");

        // --- Create Actions ---
        // Damage Actions
        DamageActionSO cannonDamage = CreateActionAsset<DamageActionSO>("action_damage_cannon", "Cannon Damage", 5);
        DamageActionSO burnTickDamage = CreateActionAsset<DamageActionSO>("action_damage_burntick", "Burn Tick Damage", 1);
        DamageActionSO poisonTickDamage = CreateActionAsset<DamageActionSO>("action_damage_poisontick", "Poison Tick Damage", 1);

        // Heal Actions
        HealActionSO deckhandHeal = CreateActionAsset<HealActionSO>("action_heal_deckhand", "Deckhand Heal", 1);

        // Apply Effect Actions
        // Need to create EffectSOs first for these

        // --- Create Effects ---
        // Burn Effect
        EffectSO burnEffect = CreateEffectAsset("effect_burn", "Burn", 5f, 1f, burnTickDamage, true, 3);
        ApplyEffectActionSO applyBurn = CreateActionAsset<ApplyEffectActionSO>("action_apply_burn", "Apply Burn", burnEffect);

        // Poison Effect
        EffectSO poisonEffect = CreateEffectAsset("effect_poison", "Poison", 10f, 2f, poisonTickDamage, true, 2);
        ApplyEffectActionSO applyPoison = CreateActionAsset<ApplyEffectActionSO>("action_apply_poison", "Apply Poison", poisonEffect);

        // Stun Effect (no tick action)
        EffectSO stunEffect = CreateEffectAsset("effect_stun", "Stun", 3f, 0f, null, false, 1);
        ApplyEffectActionSO applyStun = CreateActionAsset<ApplyEffectActionSO>("action_apply_stun", "Apply Stun", stunEffect);


        // --- Create Abilities ---
        // Cannon Ability
        AbilitySO cannonAbility = CreateAbilityAsset("ability_cannon_onready", "Cannon Shot", TriggerType.OnItemReady, new List<ActionSO> { cannonDamage });

        // Deckhand Ability
        AbilitySO deckhandAbility = CreateAbilityAsset("ability_deckhand_onready", "Deckhand Heal", TriggerType.OnItemReady, new List<ActionSO> { deckhandHeal });

        // Fire Bomb Ability
        AbilitySO fireBombAbility = CreateAbilityAsset("ability_firebomb_onready", "Fire Bomb", TriggerType.OnItemReady, new List<ActionSO> { applyBurn });

        // Poison Vial Ability
        AbilitySO poisonVialAbility = CreateAbilityAsset("ability_poisonvial_onready", "Poison Vial", TriggerType.OnItemReady, new List<ActionSO> { applyPoison });

        // Stun Grenade Ability
        AbilitySO stunGrenadeAbility = CreateAbilityAsset("ability_stungrenade_onready", "Stun Grenade", TriggerType.OnItemReady, new List<ActionSO> { applyStun });


        // --- Create ItemSOs ---
        // Cannon
        ItemSO cannon = ScriptableObject.CreateInstance<ItemSO>();
        cannon.id = "item_cannon_wood";
        cannon.displayName = "Wooden Cannon";
        cannon.rarity = Rarity.Bronze;
        cannon.isActive = true;
        cannon.cooldownSec = 5f;
        cannon.abilities = new List<AbilitySO> { cannonAbility };
        AssetDatabase.CreateAsset(cannon, "Assets/Resources/GameData/Items/item_cannon_wood.asset");

        // Deckhand
        ItemSO deckhand = ScriptableObject.CreateInstance<ItemSO>();
        deckhand.id = "item_deckhand";
        deckhand.displayName = "Deckhand";
        deckhand.rarity = Rarity.Bronze;
        deckhand.isActive = true;
        deckhand.cooldownSec = 2f;
        deckhand.abilities = new List<AbilitySO> { deckhandAbility };
        AssetDatabase.CreateAsset(deckhand, "Assets/Resources/GameData/Items/item_deckhand.asset");

        // Flintlock (No active ability, passive or instant)
        ItemSO flintlock = ScriptableObject.CreateInstance<ItemSO>();
        flintlock.id = "item_flintlock";
        flintlock.displayName = "Flintlock";
        flintlock.rarity = Rarity.Bronze;
        flintlock.isActive = false;
        AssetDatabase.CreateAsset(flintlock, "Assets/Resources/GameData/Items/item_flintlock.asset");

        // Powder Monkey (No active ability, passive or instant)
        ItemSO powderMonkey = ScriptableObject.CreateInstance<ItemSO>();
        powderMonkey.id = "item_powder_monkey";
        powderMonkey.displayName = "Powder Monkey";
        powderMonkey.rarity = Rarity.Bronze;
        powderMonkey.isActive = false; // Assuming it's passive
        powderMonkey.cooldownSec = 10f; // Still has cooldown for consistency, but no active ability
        AssetDatabase.CreateAsset(powderMonkey, "Assets/Resources/GameData/Items/item_powder_monkey.asset");

        // Fire Bomb
        ItemSO fireBomb = ScriptableObject.CreateInstance<ItemSO>();
        fireBomb.id = "item_fire_bomb";
        fireBomb.displayName = "Fire Bomb";
        fireBomb.rarity = Rarity.Silver;
        fireBomb.isActive = true;
        fireBomb.cooldownSec = 8f;
        fireBomb.abilities = new List<AbilitySO> { fireBombAbility };
        AssetDatabase.CreateAsset(fireBomb, "Assets/Resources/GameData/Items/item_fire_bomb.asset");

        // Poison Vial
        ItemSO poisonVial = ScriptableObject.CreateInstance<ItemSO>();
        poisonVial.id = "item_poison_vial";
        poisonVial.displayName = "Poison Vial";
        poisonVial.rarity = Rarity.Silver;
        poisonVial.isActive = true;
        poisonVial.cooldownSec = 7f;
        poisonVial.abilities = new List<AbilitySO> { poisonVialAbility };
        AssetDatabase.CreateAsset(poisonVial, "Assets/Resources/GameData/Items/item_poison_vial.asset");

        // Stun Grenade
        ItemSO stunGrenade = ScriptableObject.CreateInstance<ItemSO>();
        stunGrenade.id = "item_stun_grenade";
        stunGrenade.displayName = "Stun Grenade";
        stunGrenade.rarity = Rarity.Gold;
        stunGrenade.isActive = true;
        stunGrenade.cooldownSec = 12f;
        stunGrenade.abilities = new List<AbilitySO> { stunGrenadeAbility };
        AssetDatabase.CreateAsset(stunGrenade, "Assets/Resources/GameData/Items/item_stun_grenade.asset");
    }

    // --- Helper Methods --- //

    private static T CreateActionAsset<T>(string id, string displayName, int value) where T : ActionSO
    {
        T action = ScriptableObject.CreateInstance<T>();
        action.name = id; // Set the asset name
        // Assuming all actions have a 'value' field for simplicity in this helper
        // This will need to be refined for actions without a simple 'value'
        if (action is DamageActionSO damageAction) damageAction.damageAmount = value;
        else if (action is HealActionSO healAction) healAction.healAmount = value;
        // Add other action types as needed

        AssetDatabase.CreateAsset(action, $"Assets/Resources/GameData/Actions/{id}.asset");
        return action;
    }

    private static T CreateActionAsset<T>(string id, string displayName, EffectSO effect) where T : ApplyEffectActionSO
    {
        T action = ScriptableObject.CreateInstance<T>();
        action.name = id; // Set the asset name
        action.effectToApply = effect;
        AssetDatabase.CreateAsset(action, $"Assets/Resources/GameData/Actions/{id}.asset");
        return action;
    }

    private static EffectSO CreateEffectAsset(string id, string displayName, float duration, float tickInterval, ActionSO tickAction, bool isStackable, int maxStacks)
    {
        EffectSO effect = ScriptableObject.CreateInstance<EffectSO>();
        effect.name = id; // Set the asset name
        // Assign properties directly (assuming public setters or internal access)
        // For now, using reflection or direct field assignment if properties are private set
        // Or, better, make properties public set for editor tools
        // For this example, I'll assume direct field assignment is possible or properties are public set.
        // In a real scenario, you'd use a constructor or public properties.
        effect.id = id; // Assuming id field is public or has a setter
        effect.displayName = displayName; // Assuming displayName field is public or has a setter
        effect.duration = duration;
        effect.tickInterval = tickInterval;
        effect.tickAction = tickAction;
        effect.isStackable = isStackable;
        effect.maxStacks = maxStacks;

        AssetDatabase.CreateAsset(effect, $"Assets/Resources/GameData/Effects/{id}.asset");
        return effect;
    }

    private static AbilitySO CreateAbilityAsset(string id, string displayName, TriggerType trigger, List<ActionSO> actions)
    {
        AbilitySO ability = ScriptableObject.CreateInstance<AbilitySO>();
        ability.name = id; // Set the asset name
        // Assign properties directly
        ability.id = id; // Assuming id field is public or has a setter
        ability.displayName = displayName; // Assuming displayName field is public or has a setter
        ability.trigger = trigger;
        ability.actions = actions;

        AssetDatabase.CreateAsset(ability, $"Assets/Resources/GameData/Abilities/{id}.asset");
        return ability;
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
