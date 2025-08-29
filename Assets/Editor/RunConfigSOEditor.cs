using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using System; // Added for Enum

[CustomEditor(typeof(RunConfigSO))]
public class RunConfigSOEditor : Editor
{
    private SerializedProperty _startingLives;
    private SerializedProperty _startingGold;
    private SerializedProperty _inventorySize;
    private SerializedProperty _rewardGoldPerWin;
    private SerializedProperty _rerollBaseCost;
    private SerializedProperty _rerollGrowth;
    private SerializedProperty _startRarityProbabilities;
    private SerializedProperty _endRarityProbabilities;
    private SerializedProperty _rarityInterpolationCurve;
    private SerializedProperty _rules;

    private void OnEnable()
    {
        _startingLives = serializedObject.FindProperty("startingLives");
        _startingGold = serializedObject.FindProperty("startingGold");
        _inventorySize = serializedObject.FindProperty("inventorySize");
        _rewardGoldPerWin = serializedObject.FindProperty("rewardGoldPerWin");
        _rerollBaseCost = serializedObject.FindProperty("rerollBaseCost");
        _rerollGrowth = serializedObject.FindProperty("rerollGrowth");
        _startRarityProbabilities = serializedObject.FindProperty("startRarityProbabilities");
        _endRarityProbabilities = serializedObject.FindProperty("endRarityProbabilities");
        _rarityInterpolationCurve = serializedObject.FindProperty("rarityInterpolationCurve");
        _rules = serializedObject.FindProperty("rules");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_startingLives);
        EditorGUILayout.PropertyField(_startingGold);
        EditorGUILayout.PropertyField(_inventorySize);
        EditorGUILayout.PropertyField(_rewardGoldPerWin);
        EditorGUILayout.PropertyField(_rerollBaseCost);
        EditorGUILayout.PropertyField(_rerollGrowth);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rarity Settings", EditorStyles.boldLabel);

        DrawCombinedRarityProbabilitiesTable(_startRarityProbabilities, _endRarityProbabilities);

        EditorGUILayout.PropertyField(_rarityInterpolationCurve);

        DrawRarityInterpolationChart(); // NEW: Draw the chart

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_rules);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCombinedRarityProbabilitiesTable(SerializedProperty startList, SerializedProperty endList)
    {
        EditorGUILayout.LabelField("Rarity Weights", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rarity", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Floor Available", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Floor Unavailable", EditorStyles.boldLabel, GUILayout.Width(120)); // NEW COLUMN
        EditorGUILayout.LabelField("Starting Weight", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Ending Weight", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        // Ensure lists are initialized with all rarities
        InitializeRarityList(startList);
        InitializeRarityList(endList);

        foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(rarity.ToString(), GUILayout.Width(80));

            // Get the RarityProbability entry for this rarity from the startList
            SerializedProperty startEntry = FindOrCreateRarityEntry(startList, rarity);
            SerializedProperty endEntry = FindOrCreateRarityEntry(endList, rarity);

            if (startEntry != null && endEntry != null)
            {
                // Floor Available
                EditorGUILayout.PropertyField(startEntry.FindPropertyRelative("floorAvailable"), GUIContent.none, GUILayout.Width(100));
                // Floor Unavailable
                EditorGUILayout.PropertyField(startEntry.FindPropertyRelative("floorUnavailable"), GUIContent.none, GUILayout.Width(120)); // NEW FIELD

                // Starting Weight
                EditorGUILayout.PropertyField(startEntry.FindPropertyRelative("weight"), GUIContent.none, GUILayout.Width(100));

                // Ending Weight
                EditorGUILayout.PropertyField(endEntry.FindPropertyRelative("weight"), GUIContent.none, GUILayout.Width(100));
            }
            else
            {
                EditorGUILayout.LabelField("Error", GUILayout.Width(300)); // Combined width for error
            }

            EditorGUILayout.EndHorizontal();
        }

        // Calculate and display sums
        int totalStartWeight = 0;
        for (int i = 0; i < startList.arraySize; i++)
        {
            totalStartWeight += startList.GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue;
        }

        int totalEndWeight = 0;
        for (int i = 0; i < endList.arraySize; i++)
        {
            totalEndWeight += endList.GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue;
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Weights:", EditorStyles.boldLabel, GUILayout.Width(200)); // Adjusted width for new column
        EditorGUILayout.LabelField(totalStartWeight.ToString(), EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField(totalEndWeight.ToString(), EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();


        EditorGUILayout.EndVertical();
    }

    private void InitializeRarityList(SerializedProperty listProperty)
    {
        // If the list is empty, add default entries for all rarities
        if (listProperty.arraySize == 0)
        {
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                listProperty.arraySize++;
                SerializedProperty newEntry = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                newEntry.FindPropertyRelative("rarity").enumValueIndex = (int)rarity;
                newEntry.FindPropertyRelative("weight").intValue = 0; // Default weight
                newEntry.FindPropertyRelative("floorAvailable").intValue = 0; // Default floorAvailable
                newEntry.FindPropertyRelative("floorUnavailable").intValue = int.MaxValue; // NEW DEFAULT
            }
        }
        // Ensure all rarities are present in the list
        else
        {
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                bool found = false;
                for (int i = 0; i < listProperty.arraySize; i++){
                    SerializedProperty entry = listProperty.GetArrayElementAtIndex(i);
                    if ((Rarity)entry.FindPropertyRelative("rarity").enumValueIndex == rarity)
                    {
                        found = true;
                        // Ensure floorUnavailable is set for existing entries if it wasn't before
                        if (entry.FindPropertyRelative("floorUnavailable") == null)
                        {
                            entry.FindPropertyRelative("floorUnavailable").intValue = int.MaxValue;
                        }
                        break;
                    }
                }
                if (!found)
                {
                    listProperty.arraySize++;
                    SerializedProperty newEntry = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                    newEntry.FindPropertyRelative("rarity").enumValueIndex = (int)rarity;
                    newEntry.FindPropertyRelative("weight").intValue = 0; // Default weight
                    newEntry.FindPropertyRelative("floorAvailable").intValue = 0; // Default floorAvailable
                    newEntry.FindPropertyRelative("floorUnavailable").intValue = int.MaxValue; // NEW DEFAULT
                }
            }
        }
    }

    private SerializedProperty FindOrCreateRarityEntry(SerializedProperty listProperty, Rarity rarity)
    {
        for (int i = 0; i < listProperty.arraySize; i++){
            SerializedProperty entry = listProperty.GetArrayElementAtIndex(i);
            if ((Rarity)entry.FindPropertyRelative("rarity").enumValueIndex == rarity)
            {
                return entry;
            }
        }
        // This should ideally not be reached if InitializeRarityList is called correctly
        Debug.LogError($"Rarity {rarity} not found in list. This should not happen.");
        return null;
    }

    private void DrawRarityInterpolationChart()
    {
        RunConfigSO runConfig = (RunConfigSO)target;

        // Get mapLength from MapManager.Instance.mapLength
        // In editor, MapManager.Instance might be null. Provide a default or a way to input.
        // For now, let's use a default map length for visualization if MapManager is not available.
        int mapLength = 15; // Default for visualization
        if (MapManager.Instance != null)
        {
            mapLength = MapManager.Instance.mapLength;
        }
        else
        {
            // Allow user to input a map length for visualization in editor
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Chart Visualization Settings", EditorStyles.boldLabel);
            mapLength = EditorGUILayout.IntField("Simulated Map Length", mapLength);
            mapLength = Mathf.Max(1, mapLength); // Ensure at least 1
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rarity Interpolation Chart", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        Rect chartRect = EditorGUILayout.GetControlRect(false, 200); // 200 pixels height for the chart
        EditorGUI.DrawRect(chartRect, new Color(0.1f, 0.1f, 0.1f, 1f)); // Background

        // Get the actual RarityProbability lists from the RunConfigSO instance
        List<RarityProbability> startProbabilities = runConfig.startRarityProbabilities;
        List<RarityProbability> endProbabilities = runConfig.endRarityProbabilities;
        AnimationCurve curve = runConfig.rarityInterpolationCurve;

        // Create dictionaries for easier lookup
        Dictionary<Rarity, RarityProbability> startRarityMap = startProbabilities.ToDictionary(rp => rp.rarity, rp => rp);
        Dictionary<Rarity, RarityProbability> endRarityMap = endProbabilities.ToDictionary(rp => rp.rarity, rp => rp);

        // Get all unique rarities
        var allRarities = System.Enum.GetValues(typeof(Rarity)).Cast<Rarity>().ToList();
        allRarities.Sort(); // Ensure consistent order

        float barWidth = chartRect.width / mapLength;

        for (int floor = 0; floor < mapLength; floor++)
        {
            // Calculate interpolated weights for this floor
            Dictionary<Rarity, int> currentFloorWeights = new Dictionary<Rarity, int>();
            foreach (Rarity rarity in allRarities)
            {
                startRarityMap.TryGetValue(rarity, out RarityProbability startRP);
                endRarityMap.TryGetValue(rarity, out RarityProbability endRP);

                int startWeight = startRP != null ? startRP.weight : 0;
                int endWeight = endRP != null ? endRP.weight : 0;
                int floorAvailable = startRP != null ? startRP.floorAvailable : 0;
                int floorUnavailable = startRP != null ? startRP.floorUnavailable : int.MaxValue; // Get floorUnavailable

                int interpolatedWeight = 0;
                // Check if rarity is available on this floor based on floorAvailable and floorUnavailable
                if (floor >= floorAvailable && floor < floorUnavailable)
                {
                    // Calculate effective range for interpolation
                    int effectiveStartFloor = floorAvailable;
                    int effectiveEndFloor = floorUnavailable - 1; // Interpolate up to the floor *before* it becomes unavailable

                    float normalizedFloor = 0;
                    if (effectiveEndFloor > effectiveStartFloor) // If there's a range to interpolate over
                    {
                        normalizedFloor = (float)(floor - effectiveStartFloor) / (effectiveEndFloor - effectiveStartFloor);
                    }
                    else // If effectiveStartFloor == effectiveEndFloor (single floor availability)
                    {
                        normalizedFloor = 1; // It's the "end" of its very short effective life
                    }

                    float interpolationFactor = curve.Evaluate(normalizedFloor);
                    interpolatedWeight = Mathf.RoundToInt(Mathf.Lerp(startWeight, endWeight, interpolationFactor));
                }
                currentFloorWeights[rarity] = interpolatedWeight;
            }

            int totalWeight = currentFloorWeights.Sum(w => w.Value);
            if (totalWeight == 0) totalWeight = 1; // Avoid division by zero

            float currentY = chartRect.yMax; // Start drawing from the bottom

            foreach (Rarity rarity in allRarities)
            {
                float rarityHeight = (float)currentFloorWeights[rarity] / totalWeight * chartRect.height;
                Rect rarityBarRect = new Rect(chartRect.x + floor * barWidth, currentY - rarityHeight, barWidth, rarityHeight);

                Color barColor = GetRarityColor(rarity); // Helper to get color for rarity
                EditorGUI.DrawRect(rarityBarRect, barColor);

                currentY -= rarityHeight;
            }
        }

        // Draw floor labels
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.UpperCenter;
        for (int floor = 0; floor < mapLength; floor += Mathf.Max(1, mapLength / 10)) // Draw labels every 10% or so
        {
            Rect labelRect = new Rect(chartRect.x + floor * barWidth, chartRect.yMax + 2, barWidth, 20);
            EditorGUI.LabelField(labelRect, floor.ToString(), labelStyle);
        }

        EditorGUILayout.EndVertical();
    }

    private Color GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Bronze: return new Color(0.8f, 0.5f, 0.2f); // Brownish
            case Rarity.Silver: return new Color(0.7f, 0.7f, 0.7f); // Greyish
            case Rarity.Gold: return new Color(0.9f, 0.8f, 0.1f); // Yellowish
            case Rarity.Diamond: return new Color(0.2f, 0.8f, 0.9f); // Light Blueish
            default: return Color.black;
        }
    }
}