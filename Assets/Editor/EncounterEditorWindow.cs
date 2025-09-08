using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Assuming EncounterSO is in this namespace
using System; // Added for Func and Action
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class EncounterEditorWindow : EditorWindow
{
    private EncounterSO _selectedEncounter; // Field to hold the selected EncounterSO
    private VisualElement _rightColumn; // Reference to the right column
    private SerializedObject _serializedObject; // SerializedObject for the selected encounter

    // References to conditional UI containers
    private VisualElement _battlePropertiesContainer;
    private VisualElement _shopPropertiesContainer;
    private VisualElement _portPropertiesContainer;
    private VisualElement _eventPropertiesContainer;

    // Custom VisualElement for EventChoice row
    public class EventChoiceRow : VisualElement
    {
        public TextField ChoiceTextField { get; private set; }
        public TextField OutcomeTextField { get; private set; }
        private ListView _actionsListView;
        private SerializedProperty _actionsSerializedProperty;

        public EventChoiceRow()
        {
            style.flexDirection = FlexDirection.Column;
            style.marginBottom = 10;
            style.borderBottomWidth = 1;
            style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            ChoiceTextField = new TextField("Choice Text");
            Add(ChoiceTextField);

            OutcomeTextField = new TextField("Outcome Text");
            OutcomeTextField.multiline = true;
            Add(OutcomeTextField);

            _actionsListView = new ListView();
            _actionsListView.headerTitle = "Actions";
            _actionsListView.showAddRemoveFooter = true;
            _actionsListView.reorderable = true;
            _actionsListView.makeItem = () =>
            {
                // Container for the ObjectField and the specific action fields
                VisualElement itemContainer = new VisualElement();
                itemContainer.style.flexDirection = FlexDirection.Column;

                // ObjectField to select the EventChoiceAction asset
                ObjectField actionObjectField = new ObjectField();
                actionObjectField.objectType = typeof(PirateRoguelike.Data.EventChoiceAction);
                actionObjectField.allowSceneObjects = false;
                actionObjectField.label = "Action";
                itemContainer.Add(actionObjectField);

                // Container for the specific fields of the action
                VisualElement specificFieldsContainer = new VisualElement();
                specificFieldsContainer.name = "specific-fields-container"; // Give it a name for easy lookup
                itemContainer.Add(specificFieldsContainer);

                return itemContainer;
            };
                        _actionsListView = new ListView();
            _actionsListView.headerTitle = "Actions";
            _actionsListView.showAddRemoveFooter = true;
            _actionsListView.reorderable = true;
            _actionsListView.makeItem = () =>
            {
                // Container for the ObjectField and the specific action fields
                VisualElement itemContainer = new VisualElement();
                itemContainer.style.flexDirection = FlexDirection.Column;

                // ObjectField to select the EventChoiceAction asset
                ObjectField actionObjectField = new ObjectField();
                actionObjectField.objectType = typeof(PirateRoguelike.Data.EventChoiceAction);
                actionObjectField.allowSceneObjects = false;
                actionObjectField.label = "Action";
                itemContainer.Add(actionObjectField);

                // Container for the specific fields of the action
                VisualElement specificFieldsContainer = new VisualElement();
                specificFieldsContainer.name = "specific-fields-container"; // Give it a name for easy lookup
                itemContainer.Add(specificFieldsContainer);

                return itemContainer;
            };
              _actionsListView.bindItem = (element, index) =>
              {
                  ObjectField actionObjectField = element.Q<ObjectField>();
                  VisualElement specificFieldsContainer = element.Q<VisualElement>("specific-fields-container");

                  // Ensure _actionsSerializedProperty is valid
                  if (_actionsSerializedProperty == null || !_actionsSerializedProperty.isArray || index < 0 || index >= _actionsSerializedProperty.arraySize)
                  {
                      Debug.LogError($"Invalid _actionsSerializedProperty or index out of bounds. Index: {index}, ArraySize: {_actionsSerializedProperty?.arraySize ?? -1}");
                      actionObjectField.value = null;
                      specificFieldsContainer.Clear();
                      return;
                  }

                  SerializedProperty actionProperty = _actionsSerializedProperty.GetArrayElementAtIndex(index);

                  // Manually set the value of the ObjectField
                  actionObjectField.value = actionProperty.objectReferenceValue;

                  // Register callback for when the ObjectField value changes
                  actionObjectField.RegisterValueChangedCallback(evt =>
                  {
                      // Update the SerializedProperty with the new value
                      actionProperty.objectReferenceValue = evt.newValue;

                      // Apply changes to the parent SerializedObject (EncounterSO's SerializedObject)
                      // This is crucial for persisting the reference to the new EventChoiceAction
                      _actionsSerializedProperty.serializedObject.ApplyModifiedProperties();

                      // Re-bind the item to refresh the specific fields based on the new action type
                      _actionsListView.RefreshItem(index);
                  });

                  // Clear previous specific fields
                  specificFieldsContainer.Clear();

                  // Dynamically create and bind fields based on the action's type
                  PirateRoguelike.Data.EventChoiceAction actionInstance = actionProperty.objectReferenceValue as PirateRoguelike.Data.EventChoiceAction;
                  if (actionInstance != null)
                  {
                      // NEW: Create a SerializedObject for the actionInstance
                      SerializedObject actionSerializedObject = new SerializedObject(actionInstance);
                      actionSerializedObject.Update(); // Ensure it's up-to-date

                      if (actionInstance is PirateRoguelike.Data.Actions.GainResourceAction)
                      {
                          specificFieldsContainer.style.flexDirection = FlexDirection.Row;
                          specificFieldsContainer.style.justifyContent = Justify.SpaceBetween;

                          EnumField resourceTypeField = new EnumField("Resource Type");
                          SerializedProperty resourceTypeProp = actionSerializedObject.FindProperty("resourceType");
                          if (resourceTypeProp != null)
                          {
                              resourceTypeField.BindProperty(resourceTypeProp);
                              specificFieldsContainer.Add(resourceTypeField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'resourceType' not found in GainResourceAction for index {index}.");
                          }

                          IntegerField amountField = new IntegerField("Amount");
                          SerializedProperty amountProp = actionSerializedObject.FindProperty("amount");
                          if (amountProp != null)
                          {
                              amountField.BindProperty(amountProp);
                              specificFieldsContainer.Add(amountField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'amount' not found in GainResourceAction for index {index}.");
                          }
                      }
                      else if (actionInstance is PirateRoguelike.Data.Actions.ModifyStatAction)
                      {
                          specificFieldsContainer.style.flexDirection = FlexDirection.Row;
                          specificFieldsContainer.style.justifyContent = Justify.SpaceBetween;

                          EnumField statTypeField = new EnumField("Stat Type");
                          SerializedProperty statTypeProp = actionSerializedObject.FindProperty("statType");
                          if (statTypeProp != null)
                          {
                              statTypeField.BindProperty(statTypeProp);
                              specificFieldsContainer.Add(statTypeField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'statType' not found in ModifyStatAction for index {index}.");
                          }

                          IntegerField amountField = new IntegerField("Amount");
                          SerializedProperty amountProp = actionSerializedObject.FindProperty("amount");
                          if (amountProp != null)
                          {
                              amountField.BindProperty(amountProp);
                              specificFieldsContainer.Add(amountField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'amount' not found in ModifyStatAction for index {index}.");
                          }
                      }
                      else if (actionInstance is PirateRoguelike.Data.Actions.GiveItemAction)
                      {
                          TextField itemIdField = new TextField("Item ID");
                          SerializedProperty itemIdProp = actionSerializedObject.FindProperty("itemId");
                          if (itemIdProp != null)
                          {
                              itemIdField.BindProperty(itemIdProp);
                              specificFieldsContainer.Add(itemIdField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'itemId' not found in GiveItemAction for index {index}.");
                          }
                      }
                      else if (actionInstance is PirateRoguelike.Data.Actions.GiveShipAction)
                      {
                          TextField shipIdField = new TextField("Ship ID");
                          SerializedProperty shipIdProp = actionSerializedObject.FindProperty("shipId");
                          if (shipIdProp != null)
                          {
                              shipIdField.BindProperty(shipIdProp);
                              specificFieldsContainer.Add(shipIdField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'shipId' not found in GiveShipAction for index {index}.");
                          }
                      }
                      else if (actionInstance is PirateRoguelike.Data.Actions.LoadEncounterAction)
                      {
                          TextField encounterIdField = new TextField("Encounter ID");
                          SerializedProperty encounterIdProp = actionSerializedObject.FindProperty("encounterId");
                          if (encounterIdProp != null)
                          {
                              encounterIdField.BindProperty(encounterIdProp);
                              specificFieldsContainer.Add(encounterIdField);
                          }
                          else
                          {
                              Debug.LogError($"Property 'encounterId' not found in LoadEncounterAction for index {index}.");
                          }
                      }
                      // Add more else if blocks for other action types as needed

                      // Apply modified properties to the action's SerializedObject
                      actionSerializedObject.ApplyModifiedProperties();
                  }
              };

            // Custom add logic for actions list
            _actionsListView.onAdd = (lv) =>
            {
                int newIndex = _actionsSerializedProperty.arraySize;
                _actionsSerializedProperty.InsertArrayElementAtIndex(newIndex);
                SerializedProperty newElementProperty = _actionsSerializedProperty.GetArrayElementAtIndex(newIndex);

                // Create a new instance of a default EventChoiceAction (e.g., GainResourceAction)
                // and make it a sub-asset of the current EncounterSO.
                PirateRoguelike.Data.Actions.GainResourceAction newAction = ScriptableObject.CreateInstance<PirateRoguelike.Data.Actions.GainResourceAction>();
                newAction.name = $"New Gain Resource Action {newIndex}"; // Give it a unique name

                // Add the new action as a sub-asset to the EncounterSO
                AssetDatabase.AddObjectToAsset(newAction, _actionsSerializedProperty.serializedObject.targetObject);
                AssetDatabase.SaveAssets(); // Save the asset database to ensure the sub-asset is written to disk

                newElementProperty.objectReferenceValue = newAction; // Assign the new sub-asset

                _actionsSerializedProperty.serializedObject.ApplyModifiedProperties();
                lv.RefreshItems();
                lv.ScrollToItem(newIndex);
            };

            // Custom remove logic for actions list
            _actionsListView.onRemove = (lv) =>
            {
                int selectedIndex = lv.selectedIndex;
                if (selectedIndex < 0 || selectedIndex >= _actionsSerializedProperty.arraySize) return;

                SerializedProperty elementToRemoveProperty = _actionsSerializedProperty.GetArrayElementAtIndex(selectedIndex);
                PirateRoguelike.Data.EventChoiceAction actionToRemove = elementToRemoveProperty.objectReferenceValue as PirateRoguelike.Data.EventChoiceAction;

                // Remove the element from the SerializedProperty array
                _actionsSerializedProperty.DeleteArrayElementAtIndex(selectedIndex);

                // Destroy the sub-asset if it exists
                if (actionToRemove != null)
                {
                    // Check if it's a sub-asset of the current EncounterSO
                    if (AssetDatabase.IsSubAsset(actionToRemove))
                    {
                        AssetDatabase.RemoveObjectFromAsset(actionToRemove);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        DestroyImmediate(actionToRemove, true); // Destroy the actual object
                    }
                    else
                    {
                        // If it's not a sub-asset, it might be a top-level asset.
                        // We should not destroy top-level assets automatically.
                        Debug.LogWarning($"EventChoiceAction '{actionToRemove.name}' is not a sub-asset. Not destroying it automatically.");
                    }
                }

                _actionsSerializedProperty.serializedObject.ApplyModifiedProperties();
                lv.RefreshItems();
            };

            Add(_actionsListView);
            _actionsListView.fixedItemHeight = 80; // Adjust as needed to accommodate fields
            Add(_actionsListView);
        }

        public void Bind(SerializedProperty choiceElementProperty)
        {
            ChoiceTextField.BindProperty(choiceElementProperty.FindPropertyRelative("choiceText"));
            OutcomeTextField.BindProperty(choiceElementProperty.FindPropertyRelative("outcomeText"));
            _actionsSerializedProperty = choiceElementProperty.FindPropertyRelative("actions");
            _actionsListView.BindProperty(_actionsSerializedProperty);
        }
    }

    [MenuItem("Pirate Autobattler/Encounter Editor")]
    public static void ShowWindow()
    {
        EncounterEditorWindow wnd = GetWindow<EncounterEditorWindow>();
        wnd.titleContent = new GUIContent("Encounter Editor");
    }

    public void CreateGUI()
    {
        // Load UXML
        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UXML/EncounterEditorWindow.uxml");
        visualTree.CloneTree(rootVisualElement);

        // Load USS
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/USS/EncounterEditorWindow.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        // Get references to the two columns
        VisualElement leftColumn = rootVisualElement.Q<VisualElement>("left-column");
        _rightColumn = rootVisualElement.Q<VisualElement>("right-column"); // Assign to private field

        // Get references to conditional UI containers
        _battlePropertiesContainer = _rightColumn.Q<VisualElement>("battle-properties");
        _shopPropertiesContainer = _rightColumn.Q<VisualElement>("shop-properties");
        _portPropertiesContainer = _rightColumn.Q<VisualElement>("port-properties");
        _eventPropertiesContainer = _rightColumn.Q<VisualElement>("event-properties");

        // --- Step 2: Encounter Loading and Selection ---
        List<EncounterSO> encounters = LoadAllEncounterSOs();

        ListView encounterListView = new ListView(encounters, 20, () => new Label(), (element, index) =>
        {
            EncounterSO encounter = encounters[index]; // Retrieve item by index
            (element as Label).text = encounter?.id;
        });

        encounterListView.selectionType = SelectionType.Single;
        encounterListView.selectionChanged += (IEnumerable<object> selectedItems) =>
        {
            _selectedEncounter = selectedItems.FirstOrDefault() as EncounterSO;
            DisplayEncounterDetails(); // Call method to display details
        };

        if (leftColumn != null)
        {
            leftColumn.Clear(); // Clear existing label
            leftColumn.Add(encounterListView);
        }

        // Initial display of details (empty or first item if any)
        DisplayEncounterDetails();
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
    private void DisplayEncounterDetails()
    {
        _rightColumn.Clear(); // Clear previous details

        if (_selectedEncounter == null)
        {
            _rightColumn.Add(new Label("Select an Encounter from the list."));
            return;
        }

        // --- Step 3: Basic Property Binding ---
        _serializedObject = new SerializedObject(_selectedEncounter);
        _serializedObject.Update(); // NEW: Ensure SerializedObject is up-to-date

        // Common Properties
        TextField idField = new TextField("ID");
        idField.BindProperty(_serializedObject.FindProperty("id"));
        idField.SetEnabled(false);
        _rightColumn.Add(idField);

        TextField eventTitleField = new TextField("Event Title");
        eventTitleField.BindProperty(_serializedObject.FindProperty("eventTitle"));
        _rightColumn.Add(eventTitleField);

        TextField tooltipTextField = new TextField("Tooltip Text");
        tooltipTextField.BindProperty(_serializedObject.FindProperty("tooltipText"));
        tooltipTextField.multiline = true;
        _rightColumn.Add(tooltipTextField);

        EnumField typeField = new EnumField("Encounter Type");
        typeField.BindProperty(_serializedObject.FindProperty("type"));
        _rightColumn.Add(typeField);

        Toggle isEliteToggle = new Toggle("Is Elite");
        isEliteToggle.BindProperty(_serializedObject.FindProperty("isElite"));
        _rightColumn.Add(isEliteToggle);

        IntegerField minFloorField = new IntegerField("Min Floor");
        minFloorField.BindProperty(_serializedObject.FindProperty("minFloor"));
        _rightColumn.Add(minFloorField);

        IntegerField maxFloorField = new IntegerField("Max Floor");
        maxFloorField.BindProperty(_serializedObject.FindProperty("maxFloor"));
        _rightColumn.Add(maxFloorField);

        // Apply changes to the SerializedObject
        _serializedObject.ApplyModifiedProperties();

        // --- Step 4: Conditional UI by Encounter Type ---
        // Re-add conditional containers to the right column after clearing
        _rightColumn.Add(_battlePropertiesContainer);
        _rightColumn.Add(_shopPropertiesContainer);
        _rightColumn.Add(_portPropertiesContainer);
        _rightColumn.Add(_eventPropertiesContainer);

        // Hide all conditional containers first
        _battlePropertiesContainer.RemoveFromClassList("active");
        _shopPropertiesContainer.RemoveFromClassList("active");
        _portPropertiesContainer.RemoveFromClassList("active");
        _eventPropertiesContainer.RemoveFromClassList("active");

        // Show the relevant container based on EncounterType
        switch (_selectedEncounter.type)
        {
            case EncounterType.Port:
                _portPropertiesContainer.AddToClassList("active");
                _portPropertiesContainer.Clear(); // Clear contents to prevent stacking
                // Add Port-specific fields here
                break;
            case EncounterType.Event:
                _eventPropertiesContainer.AddToClassList("active");
                _eventPropertiesContainer.Clear(); // Clear contents to prevent stacking
                SerializedProperty eventChoicesProperty = _serializedObject.FindProperty("eventChoices");
                if (eventChoicesProperty != null)
                {
                    List<SerializedProperty> eventChoiceElements = new List<SerializedProperty>();
                    for (int i = 0; i < eventChoicesProperty.arraySize; i++)
                    {
                        eventChoiceElements.Add(eventChoicesProperty.GetArrayElementAtIndex(i));
                    }

                    ListView eventChoicesListView = new ListView(eventChoiceElements, 160, () => new EventChoiceRow(), (element, index) =>
                    {
                        // Bind item: get the SerializedProperty from the list of SerializedProperties
                        SerializedProperty choiceElementProperty = eventChoiceElements[index];
                        (element as EventChoiceRow).Bind(choiceElementProperty);
                    });

                    eventChoicesListView.headerTitle = "Event Choices";
                    eventChoicesListView.showAddRemoveFooter = true; // Show add/remove buttons
                    eventChoicesListView.reorderable = true; // Allow reordering

                    // Custom add logic for event choices list
                    eventChoicesListView.onAdd = (lv) =>
                    {
                        int newIndex = eventChoicesProperty.arraySize;
                        eventChoicesProperty.InsertArrayElementAtIndex(newIndex);
                        SerializedProperty newElementProperty = eventChoicesProperty.GetArrayElementAtIndex(newIndex);
                        // Unity will default-construct it.
                        // If EventChoice has fields that need default values, set them via FindPropertyRelative here.

                        _serializedObject.ApplyModifiedProperties();

                        // Re-populate the list of SerializedProperties to include the new element
                        eventChoiceElements.Clear(); // Clear existing elements
                        for (int i = 0; i < eventChoicesProperty.arraySize; i++)
                        {
                            eventChoiceElements.Add(eventChoicesProperty.GetArrayElementAtIndex(i));
                        }
                        
                        lv.itemsSource = eventChoiceElements; // Re-assign the updated list
                        lv.RefreshItems();
                        lv.ScrollToItem(newIndex);
                    };

                    // Custom remove logic for event choices list
                    eventChoicesListView.onRemove = (lv) =>
                    {
                        eventChoicesProperty.DeleteArrayElementAtIndex(lv.selectedIndex);
                        _serializedObject.ApplyModifiedProperties();

                        // Re-populate the list of SerializedProperties to reflect the removal
                        eventChoiceElements.Clear(); // Clear existing elements
                        for (int i = 0; i < eventChoicesProperty.arraySize; i++)
                        {
                            eventChoiceElements.Add(eventChoicesProperty.GetArrayElementAtIndex(i));
                        }
                        
                        lv.itemsSource = eventChoiceElements; // Re-assign the updated list
                        lv.RefreshItems();
                    };

                    _eventPropertiesContainer.Add(eventChoicesListView);
                }
                // Add Event-specific fields
                TextField eventDescriptionField = new TextField("Event Description");
                eventDescriptionField.BindProperty(_serializedObject.FindProperty("eventDescription"));
                eventDescriptionField.multiline = true;
                _eventPropertiesContainer.Add(eventDescriptionField);

                ObjectField eventUxmlField = new ObjectField("Event UXML");
                eventUxmlField.objectType = typeof(VisualTreeAsset);
                eventUxmlField.BindProperty(_serializedObject.FindProperty("eventUxml"));
                _eventPropertiesContainer.Add(eventUxmlField);

                ObjectField eventUssField = new ObjectField("Event USS");
                eventUssField.objectType = typeof(StyleSheet);
                eventUssField.BindProperty(_serializedObject.FindProperty("eventUss"));
                _eventPropertiesContainer.Add(eventUssField);
                break;
            case EncounterType.Shop:
                _shopPropertiesContainer.AddToClassList("active");
                _shopPropertiesContainer.Clear(); // Clear contents to prevent stacking
                // Add Shop-specific fields here
                break;
            case EncounterType.Battle:
                _battlePropertiesContainer.AddToClassList("active");
                _battlePropertiesContainer.Clear(); // Clear contents to prevent stacking
                SerializedProperty enemiesProperty = _serializedObject.FindProperty("enemies");
                if (enemiesProperty != null)
                {
                    ListView enemiesListView = new ListView();
                    enemiesListView.bindingPath = enemiesProperty.propertyPath; // Bind directly to SerializedProperty
                    enemiesListView.makeItem = () =>
                    {
                        // Make item: a PropertyField for EnemySO
                        PropertyField field = new PropertyField();
                        field.label = ""; // No label, as the list item itself is the label
                        // Temporary binding for type inference - will be properly bound in bindItem
                        field.BindProperty(enemiesProperty.GetArrayElementAtIndex(0));
                        return field;
                    };
                    enemiesListView.bindItem = (element, index) =>
                    {
                        // Bind item: get the SerializedProperty for the current enemy and bind it
                        SerializedProperty enemyElementProperty = enemiesProperty.GetArrayElementAtIndex(index);
                        (element as PropertyField).BindProperty(enemyElementProperty);
                    };
                    enemiesListView.fixedItemHeight = 20; // Set a fixed height
                    enemiesListView.headerTitle = "Enemies";
                    enemiesListView.showAddRemoveFooter = true; // Show add/remove buttons
                    enemiesListView.reorderable = true; // Allow reordering

                    // Custom add logic for enemies list
                    enemiesListView.onAdd = (lv) =>
                    {
                        int newIndex = enemiesProperty.arraySize;
                        enemiesProperty.InsertArrayElementAtIndex(newIndex);
                        SerializedProperty newElement = enemiesProperty.GetArrayElementAtIndex(newIndex);
                        // For ObjectField, set objectReferenceValue to null initially or a default EnemySO
                        newElement.objectReferenceValue = null; // Initialize to null

                        _serializedObject.ApplyModifiedProperties(); // Apply changes to the actual object
                        lv.RefreshItems(); // Refresh the ListView UI
                        lv.ScrollToItem(newIndex); // Scroll to the newly added item
                    };

                    // Custom remove logic for enemies list
                    enemiesListView.onRemove = (lv) =>
                    {
                        enemiesProperty.DeleteArrayElementAtIndex(lv.selectedIndex);
                        _serializedObject.ApplyModifiedProperties();
                        lv.RefreshItems();
                    };

                    _battlePropertiesContainer.Add(enemiesListView);
                }
                break;
            default:
                // Handle other types or do nothing
                break;
        }
    }
}