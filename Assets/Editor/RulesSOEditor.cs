using UnityEditor;
using UnityEngine;
using Pirate.MapGen;
using System.Linq;

[CustomEditor(typeof(RulesSO))]
public class RulesSOEditor : Editor
{
    private SerializedProperty _spacing;
    private SerializedProperty _windows;
    private SerializedProperty _unknownWeights;
    private SerializedProperty _flags;

    private void OnEnable()
    {
        _spacing = serializedObject.FindProperty("Spacing");
        _windows = serializedObject.FindProperty("Windows");
        _unknownWeights = serializedObject.FindProperty("UnknownWeights");
        _flags = serializedObject.FindProperty("Flags");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("testValue")); // Draw testValue

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spacing", EditorStyles.boldLabel);
        // Explicitly draw Spacing properties, excluding RowBandGenerationOdds
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("EliteMinGap"));
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("ShopMinGap"));
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("PortMinGap"));
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("EliteEarlyRowsCap"));
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("MaxRerollAttempts"));
        EditorGUILayout.PropertyField(_spacing.FindPropertyRelative("FallbackNodeType"));
        DrawRowBandGenerationOddsProperties(_spacing); // This will draw the RowBandGenerationOdds

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Windows", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_windows, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unknown Weights", EditorStyles.boldLabel);
        DrawUnknownWeightsProperties(_unknownWeights);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_flags, true);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawUnknownWeightsProperties(SerializedProperty unknownWeightsProp)
    {
        RulesSO rulesSO = (RulesSO)target;
        UnknownWeights unknownWeights = rulesSO.UnknownWeights;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Draw the dictionaries
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("Start"), true);
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("Pity"), true);
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("Caps"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pity System Settings", EditorStyles.boldLabel);

        // Draw the float properties
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("BattlePityBase"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("BattlePityIncrement"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("TreasurePityBase"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("TreasurePityIncrement"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("ShopPityBase"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("ShopPityIncrement"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("FallbackToEvent"));
        EditorGUILayout.PropertyField(unknownWeightsProp.FindPropertyRelative("PityPerAct"));

        EditorGUILayout.EndVertical();
    }

    private void DrawRowBandGenerationOddsProperties(SerializedProperty spacingProp)
    {
        SerializedProperty rowBandGenerationOddsProp = spacingProp.FindPropertyRelative("RowBandGenerationOdds");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Row Band Generation Odds", EditorStyles.boldLabel);

        // Add button
        if (GUILayout.Button("Add New Row Band Odds"))
        {
            rowBandGenerationOddsProp.arraySize++;
            SerializedProperty newElement = rowBandGenerationOddsProp.GetArrayElementAtIndex(rowBandGenerationOddsProp.arraySize - 1);
            // Initialize new element with default values if necessary
            newElement.FindPropertyRelative("Band").enumValueIndex = (int)RowBand.Default;
            newElement.FindPropertyRelative("MinRow").intValue = 0;
            newElement.FindPropertyRelative("MaxRow").intValue = 0;
            newElement.FindPropertyRelative("Odds").isExpanded = true; // Explicitly initialize the Odds dictionary by expanding it
        }

        for (int i = 0; i < rowBandGenerationOddsProp.arraySize; i++)
        {
            SerializedProperty elementProp = rowBandGenerationOddsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            // Draw the RowBandOdds element (Band, MinRow, MaxRow)
            EditorGUILayout.PropertyField(elementProp, new GUIContent($"Element {i}"), true); // Pass true to expand children

            // Remove button
            if (GUILayout.Button("Remove", GUILayout.Width(60))) 
            {
                rowBandGenerationOddsProp.DeleteArrayElementAtIndex(i);
                break; // Exit loop after removing an element to avoid issues with changed array size
            }
            EditorGUILayout.EndHorizontal();

            // The Odds property is already drawn by elementProp because it's a public field of RowBandOdds
            // No need to explicitly draw it again here.
        }
        EditorGUILayout.EndVertical();
    }
}