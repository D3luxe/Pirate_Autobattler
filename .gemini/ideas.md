### To-do
#   Save/load RuntimeItem update
1. Runtime Data vs. Static Data is Confirmed:
   2. Trace of the SAVE Process Shows Data Loss:
  `csharp
          public SerializableItemInstance ToSerializable()
          {
              return new SerializableItemInstance(Def.id, Def.rarity, CooldownRemaining, StunDuration);
          }
          `
         Verification: This trace proves that the serialization process only reads the item's definition ID, its rarity, and the cooldown/stun values from the `ItemInstance` itself. It does not* access the RuntimeItem or any of its child RuntimeAction objects.      
  Therefore, a modified value like CurrentDamageAmount is never read during the save process.


   3. Trace of the LOAD Process Shows State is Not Restored:
  Conclusion: The documentation is correct. The system is incapable of persisting any runtime modifications to an item's abilities, which severely limits the design potential of permanent, in-run upgrades. The proposed solution in save-load.md to extend
  SerializableItemInstance to store these dynamic properties appears to be the correct path forward.

# OnItemReady contradiction
### Step 1: Remove Dead Code from EventBus.cs


   * Objective: To eliminate the source of the naming confusion and remove code that is not used anywhere in the project.
  `csharp
          // This line will be deleted:
          public static event Action<ItemInstance, CombatContext> OnItemReady;
          `
      2.  I will then locate the corresponding dispatcher method for that event and delete it as well.     

  `csharp
          // This line will be deleted:
          public static void DispatchItemReady(ItemInstance item, CombatContext ctx) => OnItemReady?.Invoke(item, ctx);
          `
     Outcome:* The EventBus will no longer contain any reference to OnItemReady. This makes it impossible for a developer to mistakenly subscribe to this non-functional event and clarifies that the only "OnItemReady" concept is the TriggerType enum used by the
  AbilityManager.

  ---

  ### Step 2: Optimize AbilityManager Cooldown Loop


   * Objective: To refactor the inefficient polling loop in HandleTick() into a performant, targeted update loop that only processes items with active abilities.
  `csharp
          // These lines will be added:
          private static readonly List<ItemInstance> _playerActiveItems = new List<ItemInstance>();
          private static readonly List<ItemInstance> _enemyActiveItems = new List<ItemInstance>();
          `
      2.  Modify `HandleBattleStart()`: I will update this method to prepare our new lists. After it clears all old state, it will perform a one-time loop through the player's and enemy's equipped items, adding only the ones with the isActive flag to our new lists.

  `csharp
          // Logic to be added to HandleBattleStart()
          _playerActiveItems.Clear();
          _enemyActiveItems.Clear();
          // ...
          foreach (var item in ctx.Caster.Equipped) // The player
          {
              if (item != null && item.Def.isActive) _playerActiveItems.Add(item);
          }
          // ... (repeat for enemy)
          `
      3.  Optimize `HandleTick()`: This is the core of the optimization. I will change the foreach loops in this method. Instead of looping over playerShip.Equipped (the full list), it will now loop over our new, pre-filtered _playerActiveItems list. The same       
  change will be made for the enemy.


           * Before: foreach (var item in playerShip.Equipped)
   * Outcome: The amount of work done inside the high-frequency HandleTick() method will be drastically reduced. Instead of checking 10-20 item slots every 100ms, it might only check the 2 or 3 items that actually have active abilities, leading to a more performant 
     and scalable combat system.
   * Objective: To ensure the technical documentation accurately reflects the code changes.
   * Outcome: The documentation will be accurate, consistent with the refactored code, and clearer for any developer trying to understand the combat and ability systems.






### Reward UI overview
RewardUI functions should be designed so that I can determine their rewards (e.g. ShowRewards(gold=10, items=3)). Whenever a reward is claimed or missing, the relevant buttons or shelves should automatically hide.

For example,
Battle Rewards: 
-   Gold amount should be determined by RewardGoldPerWin on RunConfigSO. Lets try using RewardGoldPerWin*(1+(depth*0.1)) so that the goldvalue is multiplied by 10% of the current depth.
    -   E.g. RewardGoldPerWin is 10, the player wins a battle at depth 5, they should be offered 15 gold.
-   If the player won:
    -   Call up the reward UI, offering the calculated gold amount and 3 randomly generated items (e.g. ShowRewards(gold=x, items=true))
-   If the player lost:
    -   Call up the reward UI, offering 25% less gold (rounded up) and no items (e.g. ShowRewards(gold=x, items=false))
Non-Battle Rewards: 
-   When called up from a random event or other situations, offer 3 randomly generated items AND/OR gold depending on function call.
    -   E.g. ShowRewards(gold=20, items=false)

### UI Elements
-   A button that displays the gold amount rewarded. Clicking this button hides it and gives the player the gold
-   A shelf area that holds 3 SlotElements, which is where the items will be displayed. After the player selects an item, this shelf should be hidden.
-   All Reward UIs that contain items should have a button at the botton. This button should be hidden if an item reward is selected.
    -   If the Player has not selected an item reward:
        -   Button says "Skip (+x gold)", which gives the player additional gold (50% of the initial gold reward to the total gold received)

### Edge cases
-   If the player collects all available rewards, the UI should close and the game continues.
-   If the player presses the skip button but has not collected their gold, automatically collect any gold rewards (including the bonus gold from skipping if applicable) and continue

### Outstanding questions
-   Can we use the ItemManipulationService in a similar way as the Shops? When the player tries to select an item reward, either via clicking or dragging, the same functionality as buying items in the shop in regards to ensuring an available inventory or equipment slot. If the player's inventory and equipment slots are full, then they should not be allowed to select an item reward (unless they are merging items, which I don't think we have implemented yet.)