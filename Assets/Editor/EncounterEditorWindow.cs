using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Assuming EncounterSO is in this namespace
using System; // Added for Func and Action

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
        public IntegerField GoldCostField { get; private set; }
        public IntegerField LifeCostField { get; private set; }
        public TextField ItemRewardIdField { get; private set; }
        public TextField ShipRewardIdField { get; private set; }
        public TextField NextEncounterIdField { get; private set; }
        public TextField OutcomeTextField { get; private set; }

        public EventChoiceRow()
        {
            style.flexDirection = FlexDirection.Column;

            ChoiceTextField = new TextField("Choice Text");
            Add(ChoiceTextField);

            GoldCostField = new IntegerField("Gold Cost");
            Add(GoldCostField);

            LifeCostField = new IntegerField("Life Cost");
            Add(LifeCostField);

            ItemRewardIdField = new TextField("Item Reward ID");
            Add(ItemRewardIdField);

            ShipRewardIdField = new TextField("Ship Reward ID");
            Add(ShipRewardIdField);

            NextEncounterIdField = new TextField("Next Encounter ID");
            Add(NextEncounterIdField);

            OutcomeTextField = new TextField("Outcome Text");
            OutcomeTextField.multiline = true;
            Add(OutcomeTextField);
        }

        public void Bind(SerializedProperty choiceElementProperty)
        {
            ChoiceTextField.BindProperty(choiceElementProperty.FindPropertyRelative("choiceText"));
            GoldCostField.BindProperty(choiceElementProperty.FindPropertyRelative("goldCost"));
            LifeCostField.BindProperty(choiceElementProperty.FindPropertyRelative("lifeCost"));
            ItemRewardIdField.BindProperty(choiceElementProperty.FindPropertyRelative("itemRewardId"));
            ShipRewardIdField.BindProperty(choiceElementProperty.FindPropertyRelative("shipRewardId"));
            NextEncounterIdField.BindProperty(choiceElementProperty.FindPropertyRelative("nextEncounterId"));
            OutcomeTextField.BindProperty(choiceElementProperty.FindPropertyRelative("outcomeText"));
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