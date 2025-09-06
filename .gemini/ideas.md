Battle screen should use different player panel ui to highlight the ships and equipment.



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