# Product Requirements Document

## Project: Pirate Autobattler (Working Title)
Type: 2D Roguelike Autobattler  
Engine: Unity3D

---

## 1. High-Level Vision
- **Target Audience:** Casual gamers and card game fans.  
- **Core Fantasy:** Build a pirate crew, enhance your ship, and defeat rival pirate lords.  
- **Key Differentiator:** Players can upgrade not only their crew and tools (items), but also their ship itself.  
- **Inspirations:** Slay the Spire, Super Auto Pets, The Bazaar.  

---

## 2. Game Structure
- **Run Length:** 20–30 minutes.  
- **Progression:** Player navigates a branching map with ~15 encounters before the final boss fight.  
- **Win Condition:** Defeat the Pirate Lord.  
- **Lose Condition:** Run out of lives (persistent across the run).  

---

## 3. Starter Ship System
- At the start of each run, players choose from 3 randomly offered starter ships.  
- Properties include base health, item slots, passive effects, and sometimes built-in items.  
- Ships may have unique passive mechanics tied to tags.  
- Rare ship events may offer sidegrade ships. Event-exclusive ships can only be obtained through their events.  
- Ship replacement is always direct (cannot be stored). Overflow items move to inventory.  
- **Cost:** Ships in shops cost ~2× average item price. Rare ships may be free, discounted, or require special conditions like sacrificing life or trading items.  

---

## 4. Encounters (Expanded)

### Map & Flow
- **Structure:** Each run spans ~15 encounters plus a final boss (Pirate Lord).  
- **Map Visibility:** Entire map is visible from the start. Players see only the encounter type (Battle, Event, Shop, Port, Boss), never detailed contents.  
- **Branching Rules:**  
  - Paths can branch and merge.  
  - Paths should not cross if avoidable.  
  - Every node has at least 1 inbound and 1 outbound connection (no dead ends).  
  - All paths ultimately flow toward the end boss node.  

### Encounter Types
1. **Battle**  
   - Default encounter type; majority of nodes.  
   - Enemy ship + item loadouts scale with encounter depth.  
   - Rewards: Gold (scaled) + random item choice (rarity scaled).  
   - Loss: Player loses 1 life.  

2. **Port (Healing Node)**  
   - Heals a percentage of total lives (suggested 30%).  
   - At least 1 Port should appear on each path.  
   - If player is at full lives, they gain a small gold bonus instead (suggested 10g).  
   - No items awarded.  

3. **Shop**  
   - Offers 3 items + 1 ship (see Shop System).  
   - Shops must appear at least once every 5 encounters on any given path.  
   - Encounter depth affects rarity odds.  

4. **Event**  
   - Narrative or interactive choice nodes.  
   - Event types:  
     - Simple deterministic (e.g., “Gain 15 Gold”).  
     - Complex interactive (e.g., gain ship, special NPC, risk/reward).  
   - Rules:  
     - Most events are deterministic unless stated otherwise.  
     - Each event has a minimum and maximum floor requirement, controlling when it can appear.  
     - Events may scale in effect with depth.  

5. **Final Boss (Pirate Lord)**  
   - Final encounter of every run.  
   - Boss Pool: Pirate Lord represents a pool of possible bosses with varied ships/item builds.  
   - Variants ensure replayability.  

### Scaling Rules
- **Encounter Depth (d):** 1–15. Governs enemy strength, item rarity, and rewards.  
- **Enemy Ship Health:** Early depth (~20 HP), Mid (~34 HP), Boss (~48 HP).  
- **Enemy Loadouts:** Pulled from same rarity scaling formula as Shops.  
- **Rewards:** Gold + item rarity scale with depth.  

### Run Length
- Default = 15 encounters (tunable).  

### UX & Feedback
- **Map Navigation:** Player selects next node; no backtracking.  
- **Transitions:** Sailing animation between nodes.  
- **Events:** Presented on parchment card UI with flavor text + choice buttons.  
- **Ports:** Dock scene with heal animation and confirmation feedback.  
- **Boss:** Distinct visual/audio buildup before final battle.  

### Edge Cases
- If player has 0 lives at a Port → cannot heal (run over).  
- If inventory is full at a Battle reward → must skip or merge (same rules as Shop).  
- Events with rare ships respect “no duplicate of current ship” rule.  
- Branching map always guarantees at least one valid forward path.  


## 5. Items & Progression
- Items encompass all cards including crew and tools.  
- **Active Items:** Cooldowns, trigger effects.  
- **Passive Items:** Trigger based on synergies.  
- **Acquisition:** Shops, events, battle victories.  
- **Tags:** Active, Passive, Tool, Crew Member (always displayed).  
- **Rarity:** Bronze → Silver → Gold → Diamond. Upgrades by merging duplicates (works in slots and inventory).  
- **Inventory:** 5 slots, storage only, no battle effects. Drag & drop equip/unequip. Auto-merging enabled.  
- **Selling:** Drag items to sell area for 50% gold refund.  

---



## 5a. Item Object Model & Tooltip System

### Item Object Model
All items share a single, consistent schema across every context (shop, rewards, inventory, battle). This ensures deterministic behavior and avoids duplicate implementations.

**Core Identity**
- `id`: Unique identifier  
- `displayName`: Name shown in UI  
- `rarity`: Bronze | Silver | Gold | Diamond  
- `tags`: Expandable system (e.g., Crew Member, Tool)  
- `size`: How many inventory slots this item occupies  

**Economy**
- `baseCost`: Gold cost when offered in shop or rewards  
- `sellValue`: Standardized to 50% of baseCost  
- `rarityScaling`: Tunable multipliers for stats based on rarity  

**Abilities**
- Each item has one or more **Abilities**, defined as:
  - `trigger`: Cooldown (e.g., 5s) or event (e.g., OnAllyActivate, OnBattleStart)  
  - `actions`: List of effects (Damage, Heal, Shield, ApplyStatus, StatChange, etc.)  
  - `priority`: Overrides resolution order when needed  
  - `type`: Active, Passive, or Aura  
  - `conditions`: Optional filters (e.g., must target Crew Members)  

**State**
- `cooldownRemaining`: Seconds until next activation (battle only)  
- `stacks`: Dictionary of stackable effects (e.g., Burn:3, Poison:2)  
- `disabled`: Boolean for stuns/silences (passives still trigger)  
- `vars`: Per-battle custom values (e.g., currentDamage)  

**Enchantments**
- Optional extensions that add new abilities (trigger + actions)


**Tier-Based Parameters**
- Abilities and actions may define parameters that vary by rarity.  
- Each rarity (Bronze, Silver, Gold, Diamond) can have distinct values for attributes such as damage, healing, cooldown, shield strength, etc.  
- Example:
  ```json
  "actions": [
    {
      "class": "Damage",
      "paramsByRarity": {
        "Bronze": { "amount": 3 },
        "Silver": { "amount": 5 },
        "Gold": { "amount": 8 },
        "Diamond": { "amount": 12 }
      },
      "target": "EnemyShip"
    }
  ]
  ```
- When an item is upgraded via merging, its abilities must automatically use the parameters corresponding to the new rarity.
  

---

### Tooltip System
The tooltip is a **view layer** that dynamically renders data from the Item Object Model. It must be consistent across all contexts.

**Header**
- Item name (`displayName`)  
- Rarity (colored border or frame)  
- Tags (displayed as chips/icons)  

**Context Row**
- Shop/Reward: Show **cost** (`baseCost`)  
- Inventory: Show **sell value**  
- Battle: Show **cooldownRemaining**, “Disabled” badge if applicable  

**Abilities**
- Render each ability line using its `trigger → actions` definition  
- Active: “Every 5s: Deal 2 damage.”  
- Passive: “When ally activates: +1 damage.”  
- Aura: “While equipped: Allies gain +1 shield.”  
- Resolve templated numbers live from state (e.g., currentDamage, stacks)  

**Enchantments**
- Listed after core abilities  

**Live State**
- Show stackable effect counts and tick intervals (e.g., “Burn ×3 (triggers every 5 ticks)”)  
- Boolean effects show only remaining duration (e.g., “Stunned: 3s”)  

**Merge Preview**
- If an item is eligible to merge with an owned item:  
  > “Bronze + Bronze ⇒ Silver”  

---

### Context Consistency
- Items are always the **same object**.  
- Shops/Rewards display economy values.  
- Inventory shows sell value.  
- Battles show cooldowns, stacks, and state.  
- Tooltips always pull directly from the object’s data, ensuring consistency after patches or balance changes.  


## 6. Battle System

### Combat Resolution Flow
- **Tick Rate**: 100ms base.
- **Cooldowns**: Items wait a full cycle before first activation (e.g., 5s cannon fires first at 5s).
- **Effect Priority**: Buff → Damage → Heal → Shield → Debuff.
- **Tie-breaking**:
  - Player-side effects before enemy-side.
  - Within side, resolve left→right by slot index.
  - Within same item, local action order can override.
- **Damage Pipeline**: Damage reduction → % modifiers → shield absorption → HP loss.
- **Healing vs Death**: Healing nullified if HP ≤ 0 in the same tick.
- **Sudden Death**:
  - Starts after 30s (configurable).
  - Deals 1 damage per tick for 5 ticks, doubling every 5 ticks.
  - FX: visual/audio + damage numbers displayed.
- **Tie**: Both ships ≤ 0 HP after resolution = tie.
- **Buff/Debuff Durations**: Additive; reapplying extends the timer.
- **Disabled Behavior**:
  - Passive abilities always trigger.
  - Active abilities blocked and cooldown paused.
  - Auras always active.
- **RNG**: Seeded at run start and persisted for reproducibility.

### Ability System

### Effects & Stacking

**Sudden Death**
- Sudden Death damage follows the standard damage pipeline (Flat Reduction → % Modifiers → Shield → HP).

**Effect Categories**
- Stackable (quantitative): Have a stack count, scale outcome by stacks, reapplication adds stacks and extends duration. May change stack count on activation.
- Boolean (qualitative): Binary state (e.g., stun). Reapplication only extends duration, never magnitude.

**Stackable Effect Parameters**
- Effect outcome (what happens each activation).
- Tick interval (frequency in ticks).
- Stack scaling (e.g., ×1 dmg per stack).
- Stack change on activation (e.g., -1 for Burn, 0 for Poison).
- Duration behavior (stacks expire when duration ends).

**Examples**
- Burn: 1 dmg/stack every 5 ticks, reduces stack by 1 each activation.
- Poison: 1 dmg/stack every 10 ticks, stacks persist, only duration expires.

- **Ability = Trigger + one or more Actions.**
- **Types**:
  - Active: cooldown-driven.
  - Passive: event-driven.
  - Aura: continuous, recalculated every tick.
- **Triggers**: Define activation (OnItemReady, OnAllyActivate, OnBattleStart, etc.).
- **Actions**: Define outcome (Buff, Damage, Heal, Shield, Debuff, StatChange, Meta).
- **Ordering**:
  - Global priority (Buff > Damage > Heal > Shield > Debuff).
  - Local `order` can override within an ability.
- **Event Bus**:
  - Dispatches item-specific and system-wide events (BattleStart, SuddenDeathStarted, EncounterEnd).
  - Passive resolution order = Player → Enemy → Left→Right → Local action order.


## 7. Progression Systems

### Gold Economy
- **Battle Rewards:**  
  - Win = `5 + (depth // 2)` gold + choose 1 of 3 item rewards (rarity scales with depth).  
  - Loss = 50% of win gold (no item choice).  
  - If player **skips the item reward**, they gain an additional 50% of the win gold.  
- **Ports:** If at full lives, instead of healing, gain a small gold bonus (suggested 10g).  
- **Shops:** Purchases and rerolls.  
  - Reroll cost: **2 gold** (refreshes items only).  
- **Gold Cap:** 999 maximum.  

### Item & Ship Pricing
- **Item Prices (by rarity):**  
  - Bronze = 5 gold  
  - Silver = 10 gold  
  - Gold = 15 gold  
  - Diamond = 25 gold  
- **Ship Prices:**  
  - Always 2× corresponding item rarity.  
  - Bronze ship = 10g → Diamond ship = 50g.  
- **Shop Rarity Matching Rule:**  
  - If a player owns an item already, and the shop generates that item, it will appear at the **highest rarity the player owns**.  
  - Ensures item upgrades function correctly.  

### Lives
- **Starting Lives:** 3–5 (TBD for balancing).  
- **Loss Condition:** -1 life on battle defeat. Run ends at 0 lives.  
- **Healing:** Ports heal 30% of total lives.  

### Encounter Depth Scaling
- **Definition:** Depth = encounter index (1–15).  
- **Scaling:**  
  - Enemy ship HP and loadouts increase with depth.  
  - Item rarity rolls (shops, battle rewards) increase with depth.  
  - Gold rewards scale with depth (see formula above).  

### Elites
- **Elite Encounters:** Harder battles that reward players with better loot.  
  - Item reward rarity is rolled as if the encounter were **+3 floors deeper** (tunable).  
  - Gold reward is the same as a standard battle of that depth.  
- **Player Choice:** Elites are optional but marked on the map for risk/reward.  

### Difficulty Curve
- **Early (1–5):** Bronze/Silver items, basic enemies.  
- **Mid (6–10):** Silver/Gold items, stronger synergies.  
- **Late (11–15):** Gold/Diamond items, advanced enemies.  
- **Boss:** Drawn from the Boss Pool (Pirate Lord variants).  

### Run End Rewards
- **Framework for Unlockables:** Defeating the final boss may award new ships, items, or meta-currency in future versions.  
- **MVP:** Unlockables not required for first release.  




### Reward Clarifications
- **Battle Rewards:** 3 random items, all unique; duplicates allowed across runs, not within the same set.  
- **Shop Rewards:** Same uniqueness rule (3 unique items per shelf).  
- **Inventory Full:** Rewards follow same blocking rules as shops — must sell first if no merges possible.  
- **Boss Rewards:** None for MVP; meta-progression can be layered later.  


### RNG & Persistence
- Persist **run seed + RNG cursor** in save state to ensure reproducibility and prevent reroll scumming.  
## 8. Health Scaling
- Player starting health: 30 (ship-dependent).  
- Enemy health progression: Early ~20 HP, Mid ~34 HP, Boss ~48 HP.  

---

## 9. Meta Systems
- **Player Profile:** Tracks runs, win rate (future).  
- **Unlockable Progression:** Framework ready, not MVP.  
- **Run Modifiers (Framework):** Mutators like harder enemies, gold cost changes.  
- **Save System:** Saves at encounter start using Unity PlayerPrefs or JSON serialization.  

---

## 10. Technical Implementation
- **Engine:** Unity3D.  
- **Rendering:** Unity 2D/URP with advanced particle systems, shaders, lighting.  
- **Physics:** Unity 2D Physics for polish and optional interactions.  
- **Data:** JSON and ScriptableObjects for items, ships, encounters.  
- **Save:** PlayerPrefs or JSON file saves.  
- **Platform:** WebGL, PC, mobile possible.  

---

## 11. Art & Audio
- **Style:** Dark, gritty, cartoony pirates.  
- **Assets:** Sprite renderer + particle effects, parallax backgrounds, shaders.  
- **Audio:** Unity Audio Mixer for layered background music, ambient sounds, SFX for combat, upgrades, victories.  

---

## 12. UX & Player Comfort
- Starting deck: Each run begins with the chosen ship plus 2 basic starter items.  
- Pause menu with ESC key (timescale pause).  
- Feedback cues: Visuals, particles, audio for damage, healing, cooldowns, stuns, outcomes.  
- Battle speed toggle via timescale adjustment (1×, 2×).  
- Drag-and-drop inventory and slots via Unity Canvas UI.  

---

## 13. User Interface & Presentation
- **Painterly cartoon pirate deck framing** unifies UI across all encounters.  
- **Top Panel:** Dynamic (Enemy ship, Shop, or Event board).  
- **Center Panel:** Ocean background for combat animations and event visuals.  
- **Bottom Panel:** Persistent — player ship, slots, inventory, HUD.  

**Battle Layout**  
- Top: Enemy ship, slots, health.  
- Center: ocean combat.  
- Bottom: player ship, slots, inventory, HUD with gold/lives/progress.  

**Shop Layout**  
- Top: Merchant scene with framed slots.  
- Center: faint ocean.  
- Bottom: player deck/inventory/HUD. Drag purchases into slots or inventory.  

**Event Layout**  
- Top: Parchment board/cards with outcomes.  
- Center: ocean with overlays (storms, wreckage).  
- Bottom: player deck/inventory/HUD.  

**Style Notes**  
- Wooden carved frames, ropes, lanterns, barrels unify visuals. Decorative but functional. Slots have painterly shading and depth.  

---

## 14. Shop System (Expanded)
- Each shop visit contains **3 items** + **1 ship**.  
- **Reroll: Flat 2g. One-time free if all items bought.
- **Ship Refresh:** Each shop visit generates a new ship. Ship remains fixed during rerolls. Event-exclusive ships appear only through events.  
- **Pricing: Items cost 5/10/15/25g by rarity. Ships cost 10/20/30/50g (always 2× item cost).
- **Inventory Rules:**  
  - Block purchase if all slots are full.  
  - Exception: buying duplicate item of same rarity → merges & upgrades rarity.  
  - No overflow storage.  
- **Free Reroll: Flat 2g. One-time free if all items bought.
- **Exit:** Player may leave shop at any time.  

### Rarity Scaling Formula
Rarity odds scale with encounter depth (`d`, 1–15).  

- **Bronze:** 80% → 10%  
- **Silver:** 20% → 30%  
- **Gold:** 0% → 40%  
- **Diamond:** 0% → 20%  

Formula:  
```
P(R, d) = StartR + (EndR - StartR) * ((d - 1) / (15 - 1))
FinalP(R, d) = P(R, d) / Σ(all rarities)
```

Examples:  
- Encounter 1 → Bronze 80%, Silver 20%, Gold 0%, Diamond 0%  
- Encounter 8 → Bronze ~35%, Silver ~45%, Gold ~15%, Diamond ~5%  
- Encounter 15 → Bronze 10%, Silver 30%, Gold 40%, Diamond 20%  

**Ship Pool Rules:**  
- Duplicate ships allowed across runs and shops.  
- Never show the ship the player is currently using.  

**UX Feedback:**  
- Successful purchase → gold deducted, item animates into inventory/slots, confirmation SFX.  
- Failed purchase (insufficient gold, no slot) → cost greyed out + shared failure indicator (visual + SFX).  
- Reroll → items animate out/in, ship remains static.  

---

## 15. Open Questions
- Target frequency of rare ship events per run?  
- Exact % healing at Ports, gold values, and shop balancing require tuning.  
- Should some event-exclusive ships require additional unique tradeoffs (life, crew)?  



### Enemy Design & Progression Rules

**Fixed Templates**
- Enemies are authored as fixed templates. Their HP, ship, and abilities are defined in design and do not change at runtime.
- Difficulty is not applied through runtime stat scaling.

**Depth-Based Pools**
- Encounter depth progression controls which enemies can appear. Deeper floors unlock access to tougher enemies.
- Scaling is achieved by introducing higher-tier enemies rather than buffing existing ones.

**Repetition Rules**
- Same enemy cannot appear in two consecutive battles (back-to-back ban).
- Designers can mark certain enemies as *one-per-run*, preventing multiple appearances in a single run.
- Otherwise, repetition is allowed, controlled by pool composition and ban rules.

**Balancing Focus**
- Designers balance difficulty by controlling pools and authored stats, not runtime multipliers.
- QA and telemetry must verify correct pool composition, enforcement of the back-to-back ban, and one-per-run rules.

**Net Effect**
- The system is designer-driven, predictable, and progression is clear to players.
- Difficulty comes from *which enemies appear*, not how much their stats scale.
