using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using Pirate.MapGen; // For NodeType
using PirateRoguelike.Utility;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryPropertyDrawer : PropertyDrawer
{
    private const float ButtonWidth = 24f;
    private const float Padding = 2f;
    private const float LineHeight = 18f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        Rect foldoutRect = new Rect(position.x, position.y, position.width, LineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            SerializedProperty keysProperty = property.FindPropertyRelative("keys");
            SerializedProperty valuesProperty = property.FindPropertyRelative("values");

            // Draw each key-value pair
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                Rect lineRect = new Rect(position.x, position.y + LineHeight * (i + 1), position.width, LineHeight);

                Rect keyRect = new Rect(lineRect.x, lineRect.y, lineRect.width * 0.4f, lineRect.height);
                Rect valueRect = new Rect(lineRect.x + lineRect.width * 0.4f + Padding, lineRect.y, lineRect.width * 0.4f - ButtonWidth - Padding, lineRect.height);
                Rect removeButtonRect = new Rect(lineRect.x + lineRect.width - ButtonWidth, lineRect.y, ButtonWidth, lineRect.height);

                // Draw key (assuming NodeType for now, will need to be generic for other types)
                // This part needs to be generic for TKey
                // For NodeType, we can use EnumPopup
                if (keysProperty.GetArrayElementAtIndex(i).propertyType == SerializedPropertyType.Enum)
                {
                    EditorGUI.PropertyField(keyRect, keysProperty.GetArrayElementAtIndex(i), GUIContent.none);
                }
                else
                {
                    EditorGUI.PropertyField(keyRect, keysProperty.GetArrayElementAtIndex(i), GUIContent.none);
                }

                // Draw value
                EditorGUI.PropertyField(valueRect, valuesProperty.GetArrayElementAtIndex(i), GUIContent.none);

                // Remove button
                if (GUI.Button(removeButtonRect, "-"))
                {
                    keysProperty.DeleteArrayElementAtIndex(i);
                    valuesProperty.DeleteArrayElementAtIndex(i);
                    break; // Break to avoid issues with changed array size
                }
            }

            // Add new entry button
            Rect addButtonRect = new Rect(position.x + position.width - ButtonWidth, position.y + LineHeight * (keysProperty.arraySize + 1), ButtonWidth, LineHeight);
            if (GUI.Button(addButtonRect, "+"))
            {
                // Find the next available NodeType that is not already in the dictionary
                NodeType newKey = NodeType.Battle; // Default to Battle
                bool foundUniqueKey = false;

                // Get the current dictionary keys
                List<NodeType> currentKeys = new List<NodeType>();
                for (int i = 0; i < keysProperty.arraySize; i++)
                {
                    currentKeys.Add((NodeType)keysProperty.GetArrayElementAtIndex(i).enumValueIndex);
                }

                foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
                {
                    if (!currentKeys.Contains(type))
                    {
                        newKey = type;
                        foundUniqueKey = true;
                        break;
                    }
                }

                if (foundUniqueKey)
                {
                    keysProperty.arraySize++;
                    valuesProperty.arraySize++;

                    // Assign the new unique key
                    keysProperty.GetArrayElementAtIndex(keysProperty.arraySize - 1).enumValueIndex = (int)newKey;
                    // Assign a default value (e.g., 0 for int, or default for other types)
                    // For int values, 0 is a reasonable default.
                    valuesProperty.GetArrayElementAtIndex(valuesProperty.arraySize - 1).intValue = 0;
                }
                else
                {
                    Debug.LogWarning("All NodeTypes are already present in the dictionary. Cannot add more unique entries.");
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = LineHeight; // For foldout
        if (property.isExpanded)
        {
            SerializedProperty keysProperty = property.FindPropertyRelative("keys");
            height += LineHeight * (keysProperty.arraySize + 1); // +1 for add button
        }
        return height;
    }
}