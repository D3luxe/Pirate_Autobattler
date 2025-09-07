# EncounterSO Editor: Analysis and Implementation Plan

## 1. Introduction

This document provides a comprehensive analysis and a detailed implementation plan for developing a new, more intuitive editor for `EncounterSO` (Encounter ScriptableObject) assets within the Unity Editor. The goal is to transition from the existing IMGUI-based editor to a modern UIToolkit-based solution, significantly enhancing the content creation workflow.

## 2. Analysis of Current State and Proposed Solution

The current `GameDataEditorWindow.cs` utilizes Unity's Immediate Mode GUI (IMGUI) system for editing `ScriptableObject`s. While functional, this approach presents inherent limitations for complex data structures like `EncounterSO`, which features conditional fields and nested lists. The generic property drawing of IMGUI results in a less-than-optimal user experience, lacking visual adaptability and clarity.

A transition to Unity's UIToolkit for the `EncounterSO` editor is highly advantageous, offering substantial improvements:

*   **Dynamic UI Adaptation:** UIToolkit enables the creation of highly responsive interfaces that can dynamically display or hide fields based on the selected `EncounterType`. This significantly declutters the editor, presenting only relevant properties and enhancing user focus.
*   **Enhanced List Management:** UIToolkit provides robust components for managing collections of data, allowing for more intuitive and interactive controls for nested lists such as `enemies` and `eventChoices`. This moves beyond basic expandable lists towards more integrated editing experiences.
*   **Modern Asset Integration:** The new editor will offer direct `ObjectField` controls for assigning `VisualTreeAsset` (`eventUxml`) and `StyleSheet` (`eventUss`) assets, crucial for event-based encounters.

In this context, "WYSIWYG" (What You See Is What You Get) primarily refers to a more visually intuitive and interactive property editor within the Unity Editor. This includes conditional UI, improved list editing, and better asset selection, rather than a full, live visual rendering of UXML/USS content directly embedded within the `EncounterSO` editor window.

## 3. Assumptions

The following assumptions underpin this analysis and implementation plan:

*   **Unity Version and UIToolkit Maturity:** The project's Unity version (6000.2.0f1) provides robust and stable support for UIToolkit Editor Extensions, including features like `ListView`, `ObjectField`, and `SerializedObject` binding.
*   **`ScriptableObject` Data Structure Stability:** The core data structures of `EncounterSO`, `EnemySO`, `EventChoice`, `VisualTreeAsset`, and `StyleSheet` will remain largely stable throughout the implementation of this editor. Significant changes to these definitions would necessitate adjustments to the editor's UI and binding logic.
*   **Asset Management Strategy:** `EnemySO` assets are assumed to be pre-existing and managed independently. The editor will facilitate linking to these existing assets rather than providing functionality to create new `EnemySO`s directly within the `EncounterSO` editor.
*   **"WYSIWYG" Scope Interpretation:** As defined in Section 2, the primary focus is on conditional and interactive property editing, not a full visual UXML/USS rendering.
*   **Performance Considerations:** For the expected number of `EncounterSO`s and their nested data, standard UIToolkit components and binding mechanisms are assumed to offer acceptable performance.
*   **Basic Error Handling:** The plan includes basic validation and error display. Comprehensive, production-grade error handling for all conceivable edge cases will be an iterative process during development.
*   **Coexistence with `GameDataEditorWindow.cs`:** The existing `GameDataEditorWindow.cs` can be safely refactored to remove its `EncounterSO` editing capabilities and integrate a simple mechanism to open the new dedicated `EncounterEditorWindow` without introducing regressions.

## 4. Implementation Plan

The following steps outline the development process for the new UIToolkit-based `EncounterSO` editor:

**Step 1: Initial Setup**

*   Create the C# script for the new `EditorWindow` (`Assets/Editor/EncounterEditorWindow.cs`).
*   Define the UXML layout (`Assets/Editor/UXML/EncounterEditorWindow.uxml`) and USS styling (`Assets/Editor/USS/EncounterEditorWindow.uss`).
*   Set up the basic `EditorWindow` to load these assets and establish a two-column layout.

**Step 2: Encounter Loading and Selection**

*   Implement logic to load all `EncounterSO` assets from the project.
*   Populate a `ListView` in the left column with the loaded `EncounterSO`s.
*   Configure the `ListView` to update the right-hand detail panel when an `EncounterSO` is selected.

**Step 3: Basic Property Binding**

*   In the detail panel, create UI elements (e.g., `TextField`, `EnumField`, `Toggle`, `IntegerField`) for the core `EncounterSO` properties (ID, display name, description, type, elite status, floor range).
*   Bind these UI elements to the `EncounterSO`'s properties using `SerializedObject` to ensure data persistence.

**Step 4: Conditional UI by Encounter Type**

*   In the UXML, create distinct `VisualElement` containers for properties unique to each `EncounterType` (Battle, Shop, Port, Event).
*   Implement C# logic to dynamically show or hide these containers based on the value of the `EncounterType` property.

**Step 5: Enhanced List Editing**

*   For the `enemies` and `eventChoices` lists, utilize `ListView` components.
*   Implement custom item creation and binding functions for these `ListView`s to display and allow editing of individual list entries.
*   Add buttons for adding new items and removing existing items from these lists, including an `ObjectField` for selecting existing `EnemySO` assets.

**Step 6: Asset Pickers for Event Encounters**

*   Add `ObjectField` elements for `eventUxml` (type `VisualTreeAsset`) and `eventUss` (type `StyleSheet`) within the event-specific UI container.

**Step 7: Refactor Existing Editor**

*   Remove the `EncounterSO` editing functionality from `GameDataEditorWindow.cs`.
*   Add a menu item or button within `GameDataEditorWindow.cs` to conveniently open the new `EncounterEditorWindow`.
