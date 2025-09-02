using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using System; // Added for Enum
using Pirate.MapGen;

[CustomEditor(typeof(RunConfigSO))]
public class RunConfigSOEditor : Editor
{
    private SerializedProperty _startingLives;
    private SerializedProperty _startingGold;
    private SerializedProperty _inventorySize;
    private SerializedProperty _rewardGoldPerWin;
    private SerializedProperty _rerollBaseCost;
    private SerializedProperty _rerollGrowth;
    private SerializedProperty _rarityMilestones;
    private SerializedProperty _eliteModifier;
    private SerializedProperty _rules;

    private void OnEnable()
    {
        _startingLives = serializedObject.FindProperty("startingLives");
        _startingGold = serializedObject.FindProperty("startingGold");
        _inventorySize = serializedObject.FindProperty("inventorySize");
        _rewardGoldPerWin = serializedObject.FindProperty("rewardGoldPerWin");
        _rerollBaseCost = serializedObject.FindProperty("rerollBaseCost");
        _rerollGrowth = serializedObject.FindProperty("rerollGrowth");
        _rarityMilestones = serializedObject.FindProperty("rarityMilestones");
        _eliteModifier = serializedObject.FindProperty("eliteModifier");
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

        EditorGUILayout.PropertyField(_eliteModifier);
        DrawRarityMilestoneTable(_rarityMilestones);

        DrawRarityInterpolationChart();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_rules);

        serializedObject.ApplyModifiedProperties();
    }

    

    private void DrawRarityMilestoneTable(SerializedProperty rarityMilestonesProp)
    {
        EditorGUILayout.LabelField("Rarity Milestones", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Floor", EditorStyles.boldLabel, GUILayout.Width(50));
        foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
        {
            EditorGUILayout.LabelField(rarity.ToString(), EditorStyles.boldLabel, GUILayout.Width(60));
        }
        EditorGUILayout.LabelField("", GUILayout.Width(40)); // For move buttons
        EditorGUILayout.LabelField("", GUILayout.Width(20)); // For delete button
        EditorGUILayout.EndHorizontal();

        // Ensure all rarities are present in each milestone's weights list
        for (int i = 0; i < rarityMilestonesProp.arraySize; i++)
        {
            SerializedProperty milestoneProp = rarityMilestonesProp.GetArrayElementAtIndex(i);
            SerializedProperty weightsProp = milestoneProp.FindPropertyRelative("weights");
            InitializeRarityWeightsForMilestone(weightsProp);
        }

        // Milestone rows
        for (int i = 0; i < rarityMilestonesProp.arraySize; i++)
        {
            SerializedProperty milestoneProp = rarityMilestonesProp.GetArrayElementAtIndex(i);
            SerializedProperty floorProp = milestoneProp.FindPropertyRelative("floor");
            SerializedProperty weightsProp = milestoneProp.FindPropertyRelative("weights");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(floorProp, GUIContent.none, GUILayout.Width(50));

            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                // Find the RarityWeight for this rarity
                SerializedProperty rarityWeightProp = null;
                for (int j = 0; j < weightsProp.arraySize; j++)
                {
                    SerializedProperty currentWeightProp = weightsProp.GetArrayElementAtIndex(j);
                    if ((Rarity)currentWeightProp.FindPropertyRelative("rarity").enumValueIndex == rarity)
                    {
                        rarityWeightProp = currentWeightProp;
                        break;
                    }
                }

                if (rarityWeightProp != null)
                {
                    EditorGUILayout.PropertyField(rarityWeightProp.FindPropertyRelative("weight"), GUIContent.none, GUILayout.Width(60));
                }
                else
                {
                    EditorGUILayout.LabelField("N/A", GUILayout.Width(60)); // Should not happen if InitializeRarityWeightsForMilestone works
                }
            }

            // Move Up/Down buttons
            if (GUILayout.Button("▲", GUILayout.Width(20)))
            {
                if (i > 0)
                {
                    rarityMilestonesProp.MoveArrayElement(i, i - 1);
                }
            }
            if (GUILayout.Button("▼", GUILayout.Width(20)))
            {
                if (i < rarityMilestonesProp.arraySize - 1)
                {
                    rarityMilestonesProp.MoveArrayElement(i, i + 1);
                }
            }

            // Delete button
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                rarityMilestonesProp.DeleteArrayElementAtIndex(i);
                break; // Exit loop after deleting to avoid issues with changed array size
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Add Milestone"))
        {
            rarityMilestonesProp.arraySize++;
            SerializedProperty newMilestone = rarityMilestonesProp.GetArrayElementAtIndex(rarityMilestonesProp.arraySize - 1);
            newMilestone.FindPropertyRelative("floor").intValue = 0; // Default floor
            newMilestone.FindPropertyRelative("weights").ClearArray(); // Clear existing weights
            InitializeRarityWeightsForMilestone(newMilestone.FindPropertyRelative("weights")); // Initialize with all rarities
        }

        EditorGUILayout.EndVertical();
    }

    // Helper to ensure all rarities have a RarityWeight entry in a milestone
    private void InitializeRarityWeightsForMilestone(SerializedProperty weightsProp)
    {
        foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
        {
            bool found = false;
            for (int i = 0; i < weightsProp.arraySize; i++)
            {
                SerializedProperty entry = weightsProp.GetArrayElementAtIndex(i);
                if ((Rarity)entry.FindPropertyRelative("rarity").enumValueIndex == rarity)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                weightsProp.arraySize++;
                SerializedProperty newEntry = weightsProp.GetArrayElementAtIndex(weightsProp.arraySize - 1);
                newEntry.FindPropertyRelative("rarity").enumValueIndex = (int)rarity;
                newEntry.FindPropertyRelative("weight").intValue = 0; // Default weight
            }
        }
    }

    private void DrawRarityInterpolationChart()
    {
        RunConfigSO runConfig = (RunConfigSO)target;

        // Get mapLength from MapManager.Instance.mapLength
        // In editor, MapManager.Instance might be null. Provide a default or a way to input.
        int mapLength = 15; // Default for visualization
        if (MapManager.Instance != null)
        {
            mapLength = MapManager.Instance.mapLength;
        }
        else
        {
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

        List<RarityMilestone> rarityMilestones = runConfig.rarityMilestones;
        int eliteModifier = runConfig.eliteModifier;

        // Get all unique rarities
        var allRarities = System.Enum.GetValues(typeof(Rarity)).Cast<Rarity>().ToList();
        allRarities.Sort(); // Ensure consistent order

        float barWidth = chartRect.width / mapLength;

        for (int floor = 0; floor < mapLength; floor++)
        {
            // Calculate interpolated weights for this floor using the GameDataRegistry logic
            // We'll simulate the call to GetRarityProbabilitiesForFloor here for visualization
            List<RarityWeight> normalRarityWeights = GetInterpolatedRarityWeights(rarityMilestones, floor, 0); // 0 elite modifier for normal
            List<RarityWeight> eliteRarityWeights = GetInterpolatedRarityWeights(rarityMilestones, floor, eliteModifier); // With elite modifier

            // For visualization, we'll show both normal and elite side-by-side or stacked
            // For simplicity, let's just show the normal weights for now, or average them.
            // A better visualization might involve two charts or a toggle.
            // For this task, I'll just show the normal weights.

            Dictionary<Rarity, int> currentFloorWeights = new Dictionary<Rarity, int>();
            foreach (Rarity rarity in allRarities)
            {
                currentFloorWeights[rarity] = normalRarityWeights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;
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

    // Helper method to get interpolated rarity weights for visualization
    private List<RarityWeight> GetInterpolatedRarityWeights(List<RarityMilestone> rarityMilestones, int floorIndex, int modifier)
    {
        if (rarityMilestones == null || !rarityMilestones.Any())
        {
            return new List<RarityWeight> { new RarityWeight { rarity = Rarity.Bronze, weight = 1 } };
        }

        int effectiveFloor = floorIndex;
        if (modifier != 0)
        {
            int firstMilestoneFloor = rarityMilestones.Min(m => m.floor);
            int lastMilestoneFloor = rarityMilestones.Max(m => m.floor);
            effectiveFloor = Mathf.Clamp(floorIndex + modifier, firstMilestoneFloor, lastMilestoneFloor);
        }

        RarityMilestone milestoneFk = rarityMilestones
            .Where(m => m.floor <= effectiveFloor)
            .OrderByDescending(m => m.floor)
            .FirstOrDefault();

        RarityMilestone milestoneFkPlus1 = rarityMilestones
            .Where(m => m.floor >= effectiveFloor)
            .OrderBy(m => m.floor)
            .FirstOrDefault();

        if (milestoneFk == null) 
        {
            milestoneFk = rarityMilestones.OrderBy(m => m.floor).First();
            milestoneFkPlus1 = milestoneFk; 
        }
        else if (milestoneFkPlus1 == null) 
        {
            milestoneFkPlus1 = rarityMilestones.OrderByDescending(m => m.floor).First();
            milestoneFk = milestoneFkPlus1; 
        }

        List<RarityWeight> interpolatedWeights = new List<RarityWeight>();
        var allRarities = System.Enum.GetValues(typeof(Rarity)).Cast<Rarity>().ToList();

        if (milestoneFk.floor == milestoneFkPlus1.floor)
        {
            foreach (Rarity rarity in allRarities)
            {
                int weight = milestoneFk.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;
                interpolatedWeights.Add(new RarityWeight { rarity = rarity, weight = weight });
            }
        }
        else
        {
            float t = (float)(effectiveFloor - milestoneFk.floor) / (milestoneFkPlus1.floor - milestoneFk.floor);

            foreach (Rarity rarity in allRarities)
            {
                int weightFk = milestoneFk.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;
                int weightFkPlus1 = milestoneFkPlus1.weights.FirstOrDefault(rw => rw.rarity == rarity)?.weight ?? 0;

                int interpolatedWeight = Mathf.RoundToInt(Mathf.Lerp(weightFk, weightFkPlus1, t));
                interpolatedWeights.Add(new RarityWeight { rarity = rarity, weight = interpolatedWeight });
            }
        }
        return interpolatedWeights;
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