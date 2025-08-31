Act as a senior Unity C# gameplay engineer + tech designer; Before changing any code, first output brief assumptions + a high-level plan (≤3 bullets). Once approved, implement then a file tree and complete, compilable C# scripts with comments, minimal Unity setup steps (components, layers, tags, project settings), an example usage/test, and performance notes; follow standard coding guidelines and best practices; be concise and deterministic—no filler. Ask questions if any of the intended functionality is unclear. Follow [FILE HERE];


Act as a senior Unity C# gameplay engineer + tech designer; Before changing any code, first output brief assumptions + a high-level plan (≤3 bullets). *Wait* for approval before continuing.


 Analyze the effort required to implement mouseover tooltip windows to our items. I have created the UXML to set the visual elements at @Assets/UI/TooltipPanel/TooltipPanel.uxml ; The #HeaderRow, #Center, and #Footer sections *must* retain their existing        │
│   sprites and sizing. I have included a #TimerBadge to display the item cooldown and  a #TitleBlock to display the item name in the #HeaderRow. #Center includes several #ActiveEffect elements currently, with the goal being to add one #ActiveEffect element        │
│   per active effect on the item, giving a description of what the effect does. Finally, in #Footer, there is a #PassiveEffect element which should function the same way as the ActiveEffect elements, but for if an item has any PassiveEffects.   


summary of Changes:

   1. `TooltipImplementationPlan.md`: Detailed plan documented.
   2. `TooltipController.cs`: New script for tooltip management (display, content, positioning, animation, singleton). Dynamically populates ItemSO data, clones UXML
      templates, and handles CSS-based fading.
   3. `EffectDisplay.cs`: New script for managing individual active/passive effects.
   4. `ShopItemViewUI.cs`: Modified to trigger tooltip on PointerEnterEvent and PointerLeaveEvent.
   5. `TooltipPanelStyle.uss`: Updated with tooltip--visible, tooltip--hidden classes, and transition property for fading.

  Verification Instructions:

   1. Create Tooltip Prefab:
       * In Unity Editor, create an empty GameObject named TooltipManager in your scene.
       * Add a UIDocument component, assign Assets/UI/TooltipPanel/TooltipPanel.uxml to its "Visual Tree Asset".
       * Attach TooltipController.cs to TooltipManager.
       * Create a prefab from TooltipManager (e.g., in Assets/Prefabs/UI).
   2. Run the Game: Start the game in the Unity Editor and go to a scene with ShopItemViewUI elements.
   3. Observe Tooltip Behavior:
       * Confirm tooltip appears right of shop items.
       * Verify correct item name, cooldown, and effects are displayed.
       * Check for smooth fade-in/out.
       * Ensure tooltip disappears on mouse leave.