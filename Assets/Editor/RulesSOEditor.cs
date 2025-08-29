using UnityEditor;
using UnityEngine;
using Pirate.MapGen;
using System.Linq;

[CustomEditor(typeof(RulesSO))]
public class RulesSOEditor : Editor
{
    private SerializedProperty _counts;
    private SerializedProperty _spacing;
    private SerializedProperty _windows;
    private SerializedProperty _unknownWeights;
    private SerializedProperty _flags;

    private void OnEnable()
    {
        _counts = serializedObject.FindProperty("Counts");
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
        EditorGUILayout.LabelField("Counts", EditorStyles.boldLabel);
        DrawCountsProperties(_counts);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spacing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_spacing, true);

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

    private void DrawCountsProperties(SerializedProperty countsProp)
    {
        RulesSO rulesSO = (RulesSO)target;
        Counts counts = rulesSO.Counts;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Min", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Max", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        foreach (NodeType nodeType in System.Enum.GetValues(typeof(NodeType)))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(nodeType.ToString(), GUILayout.Width(80));

            // Min
            int currentMin = counts.Min.ContainsKey(nodeType) ? counts.Min[nodeType] : 0;
            int newMin = EditorGUILayout.IntField(currentMin, GUILayout.Width(50));
            if (newMin != currentMin)
            {
                counts.Min[nodeType] = newMin;
                EditorUtility.SetDirty(rulesSO);
            }

            // Max
            int currentMax = counts.Max.ContainsKey(nodeType) ? counts.Max[nodeType] : 0;
            int newMax = EditorGUILayout.IntField(currentMax, GUILayout.Width(50));
            if (newMax != currentMax)
            {
                counts.Max[nodeType] = newMax;
                EditorUtility.SetDirty(rulesSO);
            }

            // Target
            int currentTarget = counts.Targets.ContainsKey(nodeType) ? counts.Targets[nodeType] : 0;
            int newTarget = EditorGUILayout.IntField(currentTarget, GUILayout.Width(60));
            if (newTarget != currentTarget)
            {
                counts.Targets[nodeType] = newTarget;
                EditorUtility.SetDirty(rulesSO);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawUnknownWeightsProperties(SerializedProperty unknownWeightsProp)
    {
        RulesSO rulesSO = (RulesSO)target;
        UnknownWeights unknownWeights = rulesSO.UnknownWeights;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Start", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Pity", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Caps", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        foreach (NodeType nodeType in System.Enum.GetValues(typeof(NodeType)))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(nodeType.ToString(), GUILayout.Width(80));

            // Start
            int currentStart = unknownWeights.Start.ContainsKey(nodeType) ? unknownWeights.Start[nodeType] : 0;
            int newStart = EditorGUILayout.IntField(currentStart, GUILayout.Width(50));
            if (newStart != currentStart)
            {
                unknownWeights.Start[nodeType] = newStart;
                EditorUtility.SetDirty(rulesSO);
            }

            // Pity
            int currentPity = unknownWeights.Pity.ContainsKey(nodeType) ? unknownWeights.Pity[nodeType] : 0;
            int newPity = EditorGUILayout.IntField(currentPity, GUILayout.Width(50));
            if (newPity != currentPity)
            {
                unknownWeights.Pity[nodeType] = newPity;
                EditorUtility.SetDirty(rulesSO);
            }

            // Caps
            int currentCaps = unknownWeights.Caps.ContainsKey(nodeType) ? unknownWeights.Caps[nodeType] : 0;
            int newCaps = EditorGUILayout.IntField(currentCaps, GUILayout.Width(60));
            if (newCaps != currentCaps)
            {
                unknownWeights.Caps[nodeType] = newCaps;
                EditorUtility.SetDirty(rulesSO);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
}