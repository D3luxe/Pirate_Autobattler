---
title: "Event Encounter Creation Guide"
linkTitle: "Event Encounters"
weight: 10
type: "docs"
system: ["map", "crosscutting"] # e.g., "core", "ui", "map", "combat", "data", "crosscutting"
types: ["analysis", "guide"] # e.g., "analysis", "summary", "refactoring", "recommendation", "bug-fix", "troubleshooting", "architecture", "overview", "assessment", "plan"
---

### **Comprehensive `TRACE_AND_VERIFY` Analysis: EncounterSO and EventChoice Fields**

This report details the purpose, impact, and typical values for each field within the `EncounterSO` and `EventChoice` ScriptableObjects, based on a thorough codebase trace.

---

#### **Part 1: EncounterSO Fields**

The `EncounterSO` (Encounter ScriptableObject) defines the parameters for different types of encounters a player can face on the map.

1.  **`id`**
    *   **Purpose:** The **unique identifier** for this `EncounterSO`. It serves as a primary key for retrieving and referencing this specific encounter throughout the game.
    *   **Impact:**
        *   **Critical for Lookup:** Used by `GameDataRegistry` to load the `EncounterSO` by its ID.
        *   **Game State Tracking:** Stored in `GameSession.CurrentRunState.currentEncounterId` to track the player's current position on the map.
        *   **Map Node Identification:** Links map nodes (`MapNodeData`) to their corresponding `EncounterSO` definitions.
        *   **Debug Commands:** Used by the `startencounter <id>` debug command to load a specific encounter.
        *   **Branching Events:** Intended to be used by `EventChoice.nextEncounterId` for narrative branching (though `nextEncounterId` is not yet implemented).
    *   **Typical Value:** A short, descriptive, and unique string (e.g., `enc_my_first_event`, `enc_boss_final`, `enc_shop_basic`).
    *   **Recommendation:** Always ensure this is unique and consistent.

2.  **`type`**
    *   **Purpose:** A **critical discriminator** that defines the fundamental nature of the encounter. It dictates how the game will behave when this encounter is reached.
    *   **Impact:**
        *   **Game Flow:** Determines which scene to load (`Battle`, `Shop`, `Port`, `Event`) or if the encounter is handled directly (`Treasure`). The `RunManager` contains a central `switch` statement that uses this field.
        *   **UI Presentation:** The `MapView` uses this type to display the correct icon for the encounter node on the map.
        *   **Validation:** Ensures battle-specific logic is only applied to battle-type encounters.
        *   **Map Generation:** Influences the weighted random selection of encounter types and the resolution of "Unknown" node types.
    *   **Typical Value:** One of the `EncounterType` enum values: `Battle`, `Elite`, `Shop`, `Port`, `Event`, `Treasure`, `Boss`, `Unknown`. For a custom event, it **must** be `Event`.

3.  **`weight`**
    *   **Purpose:** Intended for weighted random selection of `EncounterSO` instances.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The game's current map generation and encounter selection logic primarily uses `EncounterType` and `isElite` for filtering, not this specific `weight` field. It appears to be a placeholder for future functionality where multiple `EncounterSO`s of the same `EncounterType` could be randomly selected based on their individual weights.
    *   **Typical Value:** `1.0f` (default). You can leave it as is.

4.  **`isElite`**
    *   **Purpose:** Indicates whether this is an "elite" version of an encounter, implying increased difficulty and rewards.
    *   **Impact:**
        *   **Rewards:** Elite encounters influence `ItemGenerationService` to provide better rewards (higher rarity items).
        *   **Map Generation/Validation:** The map generator uses this flag to ensure elite encounters are placed according to rules (e.g., not too early in the map).
        *   **Game Session Tracking:** The game session tracks if the current encounter is elite to correctly generate rewards.
    *   **Typical Value:** `true` for elite encounters, `false` for standard encounters. For a custom event, set this based on whether you intend it to be a "harder" event with better rewards.

5.  **`eliteRewardDepthBonus`**
    *   **Purpose:** Intended to provide a bonus to the "floor depth" used for calculating rewards in elite encounters, allowing for scaling reward quality independently of actual map progression.
    *   **Impact:** **Currently, setting this value has no direct impact on the game's reward generation.** It appears to be a planned feature that is not yet implemented.
    *   **Typical Value:** `0` (default).

6.  **`iconPath`**
    *   **Purpose:** Intended to store the path to a sprite asset that visually represents this encounter on the map.
    *   **Impact:** **Currently, setting this field has no direct visual impact in the game.** The `MapView` does not directly load sprites using this path. Instead, it uses a pre-configured list (`MapView.encounterIcons`) that maps `EncounterType` directly to `Sprite` assets. To display a custom icon for an `Event` type, you must configure it directly on the `MapView` component in the Unity Editor.
    *   **Typical Value:** A string representing the path to a sprite asset (e.g., `Assets/Sprites/EventIcon.png`).

7.  **`tooltipText`**
    *   **Purpose:** Intended to provide a descriptive text that appears when the player hovers over the encounter node on the map.
    *   **Impact:** **Currently, setting this field has no direct visual impact in the game.** The `MapView` does not use `MapNodeData.tooltipText` to set the `VisualElement.tooltip` property for encounter nodes. It is a missing feature.
    *   **Typical Value:** A short, informative string describing the encounter (e.g., "A dangerous pirate ambush awaits.", "A bustling port where you can repair your ship.").

8.  **`enemies`**
    *   **Purpose:** A `List` of `EnemySO` assets that defines the opponents for `Battle` encounters.
    *   **Impact:**
        *   **For `Battle` encounters:** This list *must* contain at least one `EnemySO` for the battle to function correctly. The `BattleManager` currently only uses the *first* `EnemySO` in the list.
        *   **For other encounter types (Event, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A list containing one or more `EnemySO` assets. For a custom event, this field is not relevant and can be left empty.

9.  **`shopItemCount`**
    *   **Purpose:** Defines the number of items that will be available for purchase in a `Shop` encounter.
    *   **Impact:**
        *   **For `Shop` encounters:** This value directly controls the quantity of items presented to the player by the `ShopManager`.
        *   **For other encounter types (Event, Battle, Port, etc.):** This field is ignored.
    *   **Typical Value:** An integer, typically 3 or more. For a custom event, this field is not relevant and can be left at its default.

10. **`portHealPercent`**
    *   **Purpose:** Defines the percentage of the player's maximum health that will be restored when interacting with a `Port` encounter.
    *   **Impact:**
        *   **For `Port` encounters:** This value directly determines the healing provided by the `PortController`.
        *   **For other encounter types (Event, Battle, Shop, etc.):** This field is ignored.
    *   **Typical Value:** A float between 0.0f and 1.0f (e.g., `0.3f` for 30% heal). For a custom event, this field is not relevant and can be left at its default.

11. **`eventTitle`**
    *   **Purpose:** Provides the main title or heading for an `Event` encounter.
    *   **Impact:**
        *   **For `Event` encounters:** This string is directly displayed to the player in the event UI by the `EventController`.
        *   **For other encounter types (Battle, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A descriptive string for the event (e.g., "Ancient Ruins", "Merchant's Dilemma").

12. **`eventDescription`**
    *   **Purpose:** Provides the main narrative text for an `Event` encounter, detailing the situation or story to the player.
    *   **Impact:**
        *   **For `Event` encounters:** This string is directly displayed to the player in the event UI by the `EventController`.
        *   **For other encounter types (Battle, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A longer, descriptive string that sets the scene for the event and provides context for the choices.

13. **`eventUxml`**
    *   **Purpose:** Allows you to specify a custom UXML file that defines the visual layout and hierarchy of elements for a specific `Event` encounter.
    *   **Impact:**
        *   **For `Event` encounters:** If this field is `null`, the event will not display correctly, and the game will return to the Run scene. If assigned, the UI defined in the UXML will be instantiated by the `EventController`. This is crucial for creating unique and visually distinct event interfaces.
        *   **For other encounter types (Battle, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A reference to a `VisualTreeAsset` (UXML file) created in your Unity project (e.g., `Assets/UI/Events/MyCustomEventUI.uxml`).

14. **`eventUss`**
    *   **Purpose:** Allows you to specify a custom USS file that provides styling rules for the UI elements of a specific `Event` encounter.
    *   **Impact:**
        *   **For `Event` encounters:** If assigned, its styles will be applied to the event's UI by the `EventController`. If `null`, the event UI will rely on default styles or styles inherited from parent elements.
        *   **For other encounter types (Battle, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A reference to a `StyleSheet` (USS file) created in your Unity project (e.g., `Assets/UI/Events/MyCustomEventStyle.uss`).

15. **`minFloor`**
    *   **Purpose:** Intended to define the minimum floor index at which this `EncounterSO` can appear in the game.
    *   **Impact:** **Currently, setting this value has no direct impact on the game's encounter generation.** It appears to be a planned feature that is not yet implemented.
    *   **Typical Value:** An integer representing the floor number (e.g., 1 for early game, 5 for mid-game).

16. **`maxFloor`**
    *   **Purpose:** Intended to define the maximum floor index at which this `EncounterSO` can appear in the game.
    *   **Impact:** **Currently, setting this value has no direct impact on the game's encounter generation.** It appears to be a planned feature that is not yet implemented.
    *   **Typical Value:** An integer representing the floor number (e.g., 5 for mid-game, 10 for late-game).

17. **`eventChoices`**
    *   **Purpose:** A `List` of `EventChoice` objects that defines the available options and their consequences for an `Event` encounter.
    *   **Impact:**
        *   **For `Event` encounters:** This list directly determines the interactive options presented to the player in the event UI by the `EventController`. Each `EventChoice` in the list will correspond to a selectable option.
        *   **For other encounter types (Battle, Shop, Port, etc.):** This field is ignored.
    *   **Typical Value:** A list containing one or more `EventChoice` objects.

---

#### **Part 2: EventChoice Fields**

The `EventChoice` class defines a single choice option within an `Event` encounter.

1.  **`choiceText`**
    *   **Purpose:** Provides the descriptive text for a player's choice within an `Event` encounter.
    *   **Impact:** This string is directly displayed in the event UI as a clickable option by the `EventController`.
    *   **Typical Value:** A concise phrase describing the action or decision (e.g., "Investigate the wreckage", "Attempt to flee").

2.  **`goldCost`**
    *   **Purpose:** Intended to represent the amount of gold the player must spend to select this `EventChoice`.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The logic to deduct gold when this choice is selected is not yet implemented.
    *   **Typical Value:** An integer representing the gold amount (e.g., 50, 100).

3.  **`lifeCost`**
    *   **Purpose:** Intended to represent the number of "lives" (or health points) the player must sacrifice to select this `EventChoice`.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The logic to deduct lives when this choice is selected is not yet implemented.
    *   **Typical Value:** An integer representing the life amount (e.g., 1, 2).

4.  **`itemRewardId`**
    *   **Purpose:** Intended to store the `id` of an `ItemSO` that will be granted to the player as a reward for selecting this `EventChoice`.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The logic to grant the item when this choice is selected is not yet implemented.
    *   **Typical Value:** The `id` of an existing `ItemSO` (e.g., `item_cannon`, `item_repair_kit`).

5.  **`shipRewardId`**
    *   **Purpose:** Intended to store the `id` of a `ShipSO` that will be granted to the player as a reward for selecting this `EventChoice`.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The logic to grant the ship when this choice is selected is not yet implemented.
    *   **Typical Value:** The `id` of an existing `ShipSO` (e.g., `ship_frigate`, `ship_galleon`).

6.  **`nextEncounterId`**
    *   **Purpose:** Intended to store the `id` of another `EncounterSO` that the player will transition to after selecting this `EventChoice`. This would enable branching narratives or multi-stage events.
    *   **Impact:** **Currently, setting this value has no direct impact on the game.** The logic to load a new encounter based on this ID is not yet implemented.
    *   **Typical Value:** The `id` of an existing `EncounterSO` (e.g., `enc_follow_up_event`, `enc_ambush`).

7.  **`outcomeText`**
    *   **Purpose:** Intended to provide a descriptive text that explains the immediate result of the player's chosen `EventChoice`.
    *   **Impact:** **Currently, setting this value has no direct visual impact in the game.** It is only logged to the console. The logic to display this text to the player in the UI is not yet implemented.
    *   **Typical Value:** A short sentence or paragraph describing what happens after the choice (e.g., "You successfully evaded the patrol.", "The merchant thanks you and offers a small token of appreciation.").

---

#### **Part 3: Detailed Instructions for Adding a New Event (Revised)**

To add a new event to the game and ensure it appears in-game, you will primarily work with `EncounterSO` assets and potentially custom UI elements.

**Step 1: Create the Event Data (`EncounterSO` and `EventChoice`s)**

1.  **Open the Encounter Editor Window:**
    *   In Unity, go to `Pirate Autobattler/Encounter Editor`.

2.  **Create a New `EncounterSO`:**
    *   In the left column of the Encounter Editor, click the `+` button at the bottom of the `ListView` to add a new `EncounterSO`.
    *   A new `EncounterSO` asset will be created in `Assets/Resources/GameData/Encounters`. Select it in the list.

3.  **Configure the `EncounterSO`'s Common Properties:**
    *   **ID:** Provide a **unique identifier** for your event (e.g., `MyNewEvent`). This is crucial for referencing it throughout the game.
    *   **Event Title:** The title that will be displayed for your event (e.g., "A Mysterious Encounter").
    *   **Tooltip Text:** A brief description that might appear when hovering over the event node on the map. **(Note: This is currently not displayed in-game.)**
    *   **Encounter Type:** **Set this to `Event`**. This is critical as it dictates how the game handles this encounter. Setting it to `Event` will reveal the Event-specific properties.
    *   **Is Elite:** (Optional) Set to `true` if this event should be considered an "elite" encounter, which currently influences reward generation (higher rarity items).
    *   **Min Floor / Max Floor:** Define the range of floors where this event can appear. **(Note: These fields are currently not used by the map generation logic.)**

4.  **Configure the `EncounterSO`'s Event-Specific Properties:**
    *   **Event Description:** The main text describing the event scenario to the player. This can be multiline.
    *   **Event UXML:** (Optional, but recommended for custom layouts) Assign a `VisualTreeAsset` (UXML file) if your event requires a custom UI layout beyond the default. This UXML will be instantiated by the `EventController`.
    *   **Event USS:** (Optional) Assign a `StyleSheet` (USS file) for custom styling of your event's UI. This USS will be applied to the UI instantiated from `eventUxml`.
    *   **Event Choices:** This is a list of `EventChoice` objects. Each `EventChoice` defines a player option and its consequences.
        *   Click the `+` button in the `Event Choices` section to add a new choice.
        *   For each `EventChoice`:
            *   **Choice Text:** The text displayed for the player's choice button (e.g., "Investigate the wreckage").
            *   **Gold Cost:** (Optional) The amount of gold intended to be deducted for this choice. **(Note: The gold deduction logic is not yet implemented.)**
            *   **Life Cost:** (Optional) The amount of "lives" intended to be deducted for this choice. **(Note: The life deduction logic is not yet implemented.)**
            *   **Item Reward ID:** (Optional) The `id` of an `ItemSO` intended to be given as a reward. **(Note: The item reward logic is not yet implemented.)**
            *   **Ship Reward ID:** (Optional) The `id` of a `ShipSO` intended to be given as a reward. **(Note: The ship reward logic is not yet implemented.)**
            *   **Next Encounter ID:** (Optional) The `id` of another `EncounterSO` intended for branching to a subsequent encounter. **(Note: The branching logic is not yet implemented.)**
            *   **Outcome Text:** The text describing the immediate outcome of the choice. **(Note: This is currently only logged to the console, not displayed in-game.)**

**Step 2: Create Custom UI Assets (If Needed)**

If your event requires a unique visual presentation (beyond just text and buttons), you'll need to create custom UXML and USS files.

1.  **Create UXML (`VisualTreeAsset`):**
    *   In your Unity project, right-click in the Project window (e.g., `Assets/UI/Events/`) and go to `Create/UI Toolkit/UI Document`. Name it appropriately (e.g., `MyNewEventUI.uxml`).
    *   Open the UXML file in UI Builder and design your event's layout. This will define the visual structure of your event.

2.  **Create USS (`StyleSheet`):**
    *   Right-click in the Project window (e.g., `Assets/UI/Events/`) and go to `Create/UI Toolkit/Style Sheet`. Name it appropriately (e.g., `MyNewEventStyle.uss`).
    *   Open the USS file and add CSS rules to style your UXML elements.

3.  **Assign UI Assets to `EncounterSO`:**
    *   Go back to your `EncounterSO` in the Encounter Editor.
    *   Drag and drop your `MyNewEventUI.uxml` into the `Event UXML` field.
    *   Drag and drop your `MyNewEventStyle.uss` into the `Event USS` field.

**Step 3: Ensure Event Appears In-Game**

There are two primary ways to make your event appear in the game:

1.  **Via Map Generation (Standard Gameplay):**
    *   The map generation system uses a weighted random selection process to place encounters on the map, respecting `EncounterType` and `isElite` properties.
    *   To increase the chance of your event appearing, you would typically adjust its `weight` property in the `EncounterSO`. **(Note: This `weight` field is currently not used for selecting specific `EncounterSO` instances.)**
    *   The primary way to influence its appearance via map generation is to ensure its `EncounterType` is `Event` and that the map generation rules (`RulesSO`) allow for `Event` nodes in the desired floor ranges.
    *   **To display a custom icon for your event on the map:** You must manually configure the `MapView` component in the Unity Editor. Find the `MapView` prefab or GameObject in your scene, locate its `encounterIcons` list, and add a new entry. Set the `Type` to `Event` and drag your desired `Sprite` asset into the `Icon` field.

2.  **Via Debug Console (For Testing):**
    *   This is the quickest way to test your event without playing through the game.
    *   **Open the Debug Console:** In-game, press the backquote (`) key.
    *   **Load a Scene:** If you are not already in the `Run` scene, type `loadscene Run` and press Enter.
    *   **Force an Encounter:** The debug console has a `startencounter <encounterId>` command.
        *   Type `startencounter <YourEventID>` (replace `<YourEventID>` with the `ID` you set in your `EncounterSO`, e.g., `startencounter MyNewEvent`).
        *   Press Enter. This will directly load your specified event.
