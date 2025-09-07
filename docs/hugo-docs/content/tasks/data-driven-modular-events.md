---
title: "Modular Event System for Unity (UI Toolkit + ScriptableObjects)"
weight: 10
system: ["crosscutting"]
types: ["task", "plan", "integration"]
tags: ["EnemyPanelController", "TooltipController", "RuntimeItem", "UI Toolkit"]
stage: ["Planned"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

## 1. **EventSO** (The Encounter Itself)
Holds the “story card” information:

- **Title** (`string`)  
- **Description** (`string` or `LocalizedString` for localization)  
- **Art** (`Sprite`, `Texture2D`, or `VisualTreeAsset`)  
- **Choices** (`List<EventChoice>`)  



## 2. **EventChoice** (A Single Option in the UI)
Represents one button in the event panel.

- **Label** (`string`) → Button text  
- **Icon** (`Sprite`, optional)  
- **Conditions** (optional requirements, e.g., *“Needs 5 Gold”*)  
- **Actions** (`List<EventChoiceAction>`) → Defines what happens when clicked  



## 3. **EventChoiceAction** (Reusable Effect Templates)
Abstract base ScriptableObject:

```csharp
public abstract class EventChoiceAction : ScriptableObject {
    public abstract void Execute(PlayerContext context);
}
```

### Example subclasses:
- `GainResourceAction` → Gold, food, mana, etc.  
- `ModifyStatAction` → Health, damage, defense  
- `ModifyItemAction` → Buff/nerf an item in a slot  
- `TriggerCombatAction` → Launch combat sequence  
- `CompositeAction` → Runs multiple actions in sequence  



## 4. **UI Panel Workflow**
1. Panel receives an `EventSO`.  
2. Assigns **Title**, **Description**, and **Art**.  
3. Clears existing choice buttons.  
4. For each `EventChoice`:  
   - Instantiate a button from a **UXML template**  
   - Set **label** & **icon**  
   - Check **conditions** (disable if unmet, show tooltip)  
   - Register click handler → `button.clicked += () => ExecuteChoice(choice)`  
5. On click: `ExecuteChoice(choice)` runs all actions in `choice.Actions`.  



## 5. **Example Flow**

**Designer setup (no code required):**

- Create `EventSO` → *"Ambushed by Bandits"*  
  - **Title**: `"Ambushed by Bandits"`  
  - **Description**: `"They demand your gold or your life."`  
  - **Art**: Bandit sprite  
  - **Choices**:  
    - **Fight Back** → `TriggerCombatAction`  
    - **Pay Gold** → `GainResourceAction` (−20 Gold)  
    - **Run Away** → `ModifyStatAction` (−5 Health)  

At runtime, the panel builds automatically.



## 6. **Advantages**
- **Reusable templates** → Define once, reuse everywhere  
- **Data-driven** → Events authored in Inspector, not code  
- **Flexible** → Supports both simple and complex outcomes  
- **Scalable** → Easy to add new encounters  



## 7. **Refinement Ideas**
- **Condition System** → Choices require items, stats, or quest flags  
- **Disabled Buttons** → Show unavailable options with tooltips  
- **Composite Actions** → Chain multiple effects in one choice  
- **Validation Tools** → Editor script warns if event is incomplete  
- **Localization** → Use `LocalizedString` for all text  
- **Player Context Passing** → Ensure actions execute on the correct player state  



## **Workflow after setup**:  
1. Create a new `EventSO`  
2. Fill in **title/description/art**  
3. Add choices, assign existing **actions**  
4. Done — no extra coding needed  
