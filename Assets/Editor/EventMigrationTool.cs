using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Data.Actions;
using UnityEngine.UIElements;

public class EventMigrationTool : EditorWindow
{
    [MenuItem("Pirate Autobattler/Tools/Migrate Event Choices")]
    public static void ShowWindow()
    {
        GetWindow<EventMigrationTool>().titleContent = new GUIContent("Event Migration Tool");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        Button migrateButton = new Button(OnMigrateClicked);
        migrateButton.text = "Migrate Event Choices";
        root.Add(migrateButton);

        Label infoLabel = new Label("This tool will migrate old EventChoice fields to new EventChoiceAction ScriptableObjects.");
        root.Add(infoLabel);
    }

    private void OnMigrateClicked()
    {
        if (!EditorUtility.DisplayDialog("Confirm Migration",
            "This will modify existing EncounterSO assets and create new EventChoiceAction assets. " +
            "Please ensure you have a backup of your project before proceeding.",
            "Migrate", "Cancel"))
        {
            return;
        }

        List<EncounterSO> allEncounters = LoadAllEncounterSOs();
        int migratedCount = 0;

        foreach (EncounterSO encounter in allEncounters)
        {
            bool encounterModified = false;
            SerializedObject serializedEncounter = new SerializedObject(encounter);
            SerializedProperty eventChoicesProperty = serializedEncounter.FindProperty("eventChoices");

            if (eventChoicesProperty != null && eventChoicesProperty.isArray)
            {
                for (int i = 0; i < eventChoicesProperty.arraySize; i++)
                {
                    SerializedProperty choiceProperty = eventChoicesProperty.GetArrayElementAtIndex(i);

                    // Get old field values
                    SerializedProperty goldCostProp = choiceProperty.FindPropertyRelative("goldCost");
                    SerializedProperty lifeCostProp = choiceProperty.FindPropertyRelative("lifeCost");
                    SerializedProperty itemRewardIdProp = choiceProperty.FindPropertyRelative("itemRewardId");
                    SerializedProperty shipRewardIdProp = choiceProperty.FindPropertyRelative("shipRewardId");
                    SerializedProperty nextEncounterIdProp = choiceProperty.FindPropertyRelative("nextEncounterId");

                    // Get the new actions list property
                    SerializedProperty actionsProp = choiceProperty.FindPropertyRelative("actions");
                    if (actionsProp == null) // Should not happen if DataTypes.cs was updated correctly
                    {
                        Debug.LogError($"Actions property not found on EventChoice in {encounter.name}. Skipping.");
                        continue;
                    }

                    // Clear existing actions to prevent duplicates on re-run
                    actionsProp.ClearArray();

                    // Create and add new actions based on old data
                    if (goldCostProp != null && goldCostProp.intValue != 0)
                    {
                        GainResourceAction action = ScriptableObject.CreateInstance<GainResourceAction>();
                        action.name = $"GainGold_{goldCostProp.intValue}";
                        SerializedObject so = new SerializedObject(action);
                        so.FindProperty("resourceType").enumValueIndex = (int)GainResourceAction.ResourceType.Gold;
                        so.FindProperty("amount").intValue = goldCostProp.intValue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.AddObjectToAsset(action, encounter);
                        actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                        actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                        encounterModified = true;
                    }

                    if (lifeCostProp != null && lifeCostProp.intValue != 0)
                    {
                        // Assuming negative lifeCost means losing lives, positive means gaining
                        // For simplicity, if lifeCost is negative, create a ModifyStatAction (damage)
                        // If positive, create a GainResourceAction (lives)
                        if (lifeCostProp.intValue < 0)
                        {
                            ModifyStatAction action = ScriptableObject.CreateInstance<ModifyStatAction>();
                            action.name = $"LoseLife_{Mathf.Abs(lifeCostProp.intValue)}";
                            SerializedObject so = new SerializedObject(action);
                            so.FindProperty("statType").enumValueIndex = (int)ModifyStatAction.StatType.Health; // Assuming lives map to health for now
                            so.FindProperty("amount").intValue = lifeCostProp.intValue; // Negative for damage
                            so.ApplyModifiedPropertiesWithoutUndo();
                            AssetDatabase.AddObjectToAsset(action, encounter);
                            actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                            actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                            encounterModified = true;
                        }
                        else // Positive lifeCost means gaining lives
                        {
                            GainResourceAction action = ScriptableObject.CreateInstance<GainResourceAction>();
                            action.name = $"GainLife_{lifeCostProp.intValue}";
                            SerializedObject so = new SerializedObject(action);
                            so.FindProperty("resourceType").enumValueIndex = (int)GainResourceAction.ResourceType.Lives;
                            so.FindProperty("amount").intValue = lifeCostProp.intValue;
                            so.ApplyModifiedPropertiesWithoutUndo();
                            AssetDatabase.AddObjectToAsset(action, encounter);
                            actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                            actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                            encounterModified = true;
                        }
                    }

                    if (itemRewardIdProp != null && !string.IsNullOrEmpty(itemRewardIdProp.stringValue))
                    {
                        GiveItemAction action = ScriptableObject.CreateInstance<GiveItemAction>();
                        action.name = $"GiveItem_{itemRewardIdProp.stringValue}";
                        SerializedObject so = new SerializedObject(action);
                        so.FindProperty("itemId").stringValue = itemRewardIdProp.stringValue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.AddObjectToAsset(action, encounter);
                        actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                        actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                        encounterModified = true;
                    }

                    if (shipRewardIdProp != null && !string.IsNullOrEmpty(shipRewardIdProp.stringValue))
                    {
                        GiveShipAction action = ScriptableObject.CreateInstance<GiveShipAction>();
                        action.name = $"GiveShip_{shipRewardIdProp.stringValue}";
                        SerializedObject so = new SerializedObject(action);
                        so.FindProperty("shipId").stringValue = shipRewardIdProp.stringValue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.AddObjectToAsset(action, encounter);
                        actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                        actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                        encounterModified = true;
                    }

                    if (nextEncounterIdProp != null && !string.IsNullOrEmpty(nextEncounterIdProp.stringValue))
                    {
                        LoadEncounterAction action = ScriptableObject.CreateInstance<LoadEncounterAction>();
                        action.name = $"LoadEncounter_{nextEncounterIdProp.stringValue}";
                        SerializedObject so = new SerializedObject(action);
                        so.FindProperty("encounterId").stringValue = nextEncounterIdProp.stringValue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.AddObjectToAsset(action, encounter);
                        actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
                        actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1).objectReferenceValue = action;
                        encounterModified = true;
                    }

                    // After migration, remove the old properties from the SerializedObject
                    // This is tricky with SerializedProperty.FindPropertyRelative as it doesn't remove the actual field.
                    // The fields are already removed from the C# class (DataTypes.cs), so they won't be serialized anymore.
                    // We just need to ensure they are not used in the migration logic after this point.
                }
            }

            if (encounterModified)
            {
                serializedEncounter.ApplyModifiedProperties();
                EditorUtility.SetDirty(encounter);
                AssetDatabase.SaveAssets();
                migratedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Migration complete. Migrated {migratedCount} EncounterSO assets.");
        EditorUtility.DisplayDialog("Migration Complete",
            $"Successfully migrated {migratedCount} EncounterSO assets. " +
            "Please check your assets and save the project.", "OK");
    }

    private List<EncounterSO> LoadAllEncounterSOs()
    {
        List<EncounterSO> encounters = new List<EncounterSO>();
        string[] guids = AssetDatabase.FindAssets("t:EncounterSO", new[] { "Assets/Resources/GameData/Encounters" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EncounterSO encounter = AssetDatabase.LoadAssetAtPath<EncounterSO>(path);
            if (encounter != null)
            {
                encounters.Add(encounter);
            }
        }
        return encounters;
    }
}
