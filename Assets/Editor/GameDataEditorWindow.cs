using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Combat;

public class GameDataEditorWindow : EditorWindow
{
    private List<ItemSO> _items;
    private List<ShipSO> _ships;
    private List<EncounterSO> _encounters;
    private List<EnemySO> _enemies; // New: List for EnemySO

    private ItemSO _selectedItem;
    private ShipSO _selectedShip;
    private EncounterSO _selectedEncounter;
    private EnemySO _selectedEnemy; // New: Selected EnemySO

    private Vector2 _scrollPosition;
    private int _selectedTab = 0;

    // Filtering and Sorting for Enemies
    private string _enemySearchString = "";
    private bool _sortEnemiesByNameAsc = true;

    [MenuItem("Game/Game Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<GameDataEditorWindow>("Game Data Editor");
    }

    private void OnEnable()
    {
        LoadAllGameData();
    }

    private void LoadAllGameData()
    {
        _items = AssetDatabase.FindAssets("t:ItemSO").Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        _ships = AssetDatabase.FindAssets("t:ShipSO").Select(guid => AssetDatabase.LoadAssetAtPath<ShipSO>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        _encounters = AssetDatabase.FindAssets("t:EncounterSO").Select(guid => AssetDatabase.LoadAssetAtPath<EncounterSO>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        _enemies = AssetDatabase.FindAssets("t:EnemySO").Select(guid => AssetDatabase.LoadAssetAtPath<EnemySO>(AssetDatabase.GUIDToAssetPath(guid))).ToList(); // New: Load EnemySO
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Data Editor", EditorStyles.boldLabel);

        _selectedTab = GUILayout.Toolbar(_selectedTab, new string[] { "Items", "Ships", "Encounters", "Enemies" }); // New: Add "Enemies" tab

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        switch (_selectedTab)
        {
            case 0: // Items Tab
                DrawItemsTab();
                break;
            case 1: // Ships Tab
                DrawShipsTab();
                break;
            case 2: // Encounters Tab
                DrawEncountersTab();
                break;
            case 3: // Enemies Tab (New)
                DrawEnemiesTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(this); // Mark the window as dirty to save changes
        }
    }

    private void DrawItemsTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.2f)); // Changed from 0.3f to 0.2f
        GUILayout.Label("Items", EditorStyles.boldLabel);

        foreach (var item in _items)
        {
            if (GUILayout.Button(item.displayName))
            {
                _selectedItem = item;
                _selectedShip = null;
                _selectedEncounter = null;
                _selectedEnemy = null; // New: Deselect enemy
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (_selectedItem != null)
        {
            GUILayout.Label($"Editing Item: {_selectedItem.displayName}", EditorStyles.boldLabel);
            DrawItemProperties(_selectedItem);
        }
        else
        {
            GUILayout.Label("Select an Item to edit.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawShipsTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.2f)); // Changed from 0.3f to 0.2f
        GUILayout.Label("Ships", EditorStyles.boldLabel);

        foreach (var ship in _ships)
        {
            if (GUILayout.Button(ship.displayName))
            {
                _selectedShip = ship;
                _selectedItem = null;
                _selectedEncounter = null;
                _selectedEnemy = null; // New: Deselect enemy
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (_selectedShip != null)
        {
            GUILayout.Label($"Editing Ship: {_selectedShip.displayName}", EditorStyles.boldLabel);
            DrawShipProperties(_selectedShip);
        }
        else
        {
            GUILayout.Label("Select a Ship to edit.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEncountersTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.2f)); // Changed from 0.3f to 0.2f
        GUILayout.Label("Encounters", EditorStyles.boldLabel);

        foreach (var encounter in _encounters)
        {
            if (GUILayout.Button(encounter.eventTitle))
            {
                _selectedEncounter = encounter;
                _selectedItem = null;
                _selectedShip = null;
                _selectedEnemy = null; // New: Deselect enemy
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (_selectedEncounter != null)
        {
            GUILayout.Label($"Editing Encounter: {_selectedEncounter.eventTitle}", EditorStyles.boldLabel);
            DrawEncounterProperties(_selectedEncounter);
        }
        else
        {
            GUILayout.Label("Select an Encounter to edit.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    // New: Draw Enemies Tab
    private void DrawEnemiesTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.2f)); // Changed from 0.3f to 0.2f
        GUILayout.Label("Enemies", EditorStyles.boldLabel);

        // Filtering
        EditorGUILayout.BeginHorizontal();
        _enemySearchString = EditorGUILayout.TextField("Search:", _enemySearchString);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            _enemySearchString = "";
        }
        EditorGUILayout.EndHorizontal();

        // Sorting
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Sort by Name:");
        if (GUILayout.Button(_sortEnemiesByNameAsc ? "Asc" : "Desc", GUILayout.Width(50)))
        {
            _sortEnemiesByNameAsc = !_sortEnemiesByNameAsc;
        }
        EditorGUILayout.EndHorizontal();

        var filteredEnemies = _enemies.Where(e => string.IsNullOrEmpty(_enemySearchString) || e.displayName.ToLower().Contains(_enemySearchString.ToLower()));
        var sortedEnemies = _sortEnemiesByNameAsc ? filteredEnemies.OrderBy(e => e.displayName) : filteredEnemies.OrderByDescending(e => e.displayName);

        foreach (var enemy in sortedEnemies)
        {
            if (GUILayout.Button(enemy.displayName))
            {
                _selectedEnemy = enemy;
                _selectedItem = null;
                _selectedShip = null;
                _selectedEncounter = null;
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (_selectedEnemy != null)
        {
            GUILayout.Label($"Editing Enemy: {_selectedEnemy.displayName}", EditorStyles.boldLabel);
            DrawEnemyProperties(_selectedEnemy);
        }
        else
        {
            GUILayout.Label("Select an Enemy to edit.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemProperties(ItemSO item)
    {
        SerializedObject serializedObject = new SerializedObject(item);
        SerializedProperty idProp = serializedObject.FindProperty("id");
        SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
        SerializedProperty descriptionProp = serializedObject.FindProperty("description");
        SerializedProperty iconProp = serializedObject.FindProperty("icon");
        SerializedProperty rarityProp = serializedObject.FindProperty("rarity");
        SerializedProperty isActiveProp = serializedObject.FindProperty("isActive");
        SerializedProperty cooldownSecProp = serializedObject.FindProperty("cooldownSec");
        SerializedProperty abilitiesProp = serializedObject.FindProperty("abilities");

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(descriptionProp, GUILayout.Height(50));
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(rarityProp);
        EditorGUILayout.PropertyField(isActiveProp);
        EditorGUILayout.PropertyField(cooldownSecProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(abilitiesProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawShipProperties(ShipSO ship)
    {
        SerializedObject serializedObject = new SerializedObject(ship);
        SerializedProperty idProp = serializedObject.FindProperty("id");
        SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
        SerializedProperty baseMaxHealthProp = serializedObject.FindProperty("baseMaxHealth");
        SerializedProperty baseItemSlotsProp = serializedObject.FindProperty("baseItemSlots");
        SerializedProperty artProp = serializedObject.FindProperty("art");
        SerializedProperty rarityProp = serializedObject.FindProperty("rarity");
        SerializedProperty costProp = serializedObject.FindProperty("Cost");
        SerializedProperty builtInItemsProp = serializedObject.FindProperty("builtInItems");
        SerializedProperty itemLoadoutProp = serializedObject.FindProperty("itemLoadout");

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(baseMaxHealthProp);
        EditorGUILayout.PropertyField(baseItemSlotsProp);
        EditorGUILayout.PropertyField(artProp);
        EditorGUILayout.PropertyField(rarityProp);
        EditorGUILayout.PropertyField(costProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Built-in Items", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(builtInItemsProp, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Item Loadout", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(itemLoadoutProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEncounterProperties(EncounterSO encounter)
    {
        SerializedObject serializedObject = new SerializedObject(encounter);
        SerializedProperty idProp = serializedObject.FindProperty("id");
        SerializedProperty eventTitleProp = serializedObject.FindProperty("eventTitle");
        SerializedProperty eventDescriptionProp = serializedObject.FindProperty("eventDescription");
        SerializedProperty typeProp = serializedObject.FindProperty("type");
        SerializedProperty isEliteProp = serializedObject.FindProperty("isElite");
        SerializedProperty minFloorProp = serializedObject.FindProperty("minFloor");
        SerializedProperty maxFloorProp = serializedObject.FindProperty("maxFloor");
        SerializedProperty enemiesProp = serializedObject.FindProperty("enemies");
        SerializedProperty eventChoicesProp = serializedObject.FindProperty("eventChoices");

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(eventTitleProp);
        EditorGUILayout.PropertyField(eventDescriptionProp, GUILayout.Height(50));
        EditorGUILayout.PropertyField(typeProp);
        EditorGUILayout.PropertyField(isEliteProp);
        EditorGUILayout.PropertyField(minFloorProp);
        EditorGUILayout.PropertyField(maxFloorProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemiesProp, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Event Choices", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(eventChoicesProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    // New: Draw Enemy Properties
    private void DrawEnemyProperties(EnemySO enemy)
    {
        SerializedObject serializedObject = new SerializedObject(enemy);
        SerializedProperty idProp = serializedObject.FindProperty("id");
        SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
        SerializedProperty shipIdProp = serializedObject.FindProperty("shipId");
        SerializedProperty itemLoadoutProp = serializedObject.FindProperty("itemLoadout");
        SerializedProperty targetingStrategyProp = serializedObject.FindProperty("targetingStrategy");

        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(shipIdProp);
        EditorGUILayout.PropertyField(itemLoadoutProp, true); // true for expandable list
        EditorGUILayout.PropertyField(targetingStrategyProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRarityTieredValueList(string label, List<RarityTieredValue> list)
    {
        // This method is no longer needed as EditorGUILayout.PropertyField handles lists automatically.
        // Keeping it as a placeholder for now, but it will be removed.
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(label, EditorStyles.miniBoldLabel);

        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i].rarity = (Rarity)EditorGUILayout.EnumPopup(list[i].rarity);
                list[i].value = EditorGUILayout.FloatField(list[i].value);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (GUILayout.Button("Add Value"))
        {
            if (list == null) list = new List<RarityTieredValue>();
            list.Add(new RarityTieredValue());
        }
        EditorGUILayout.EndVertical();
    }
}

    
