# Enforcement Checklist

- Item prices must be Bronze 5g, Silver 10g, Gold 15g, Diamond 25g.  
- Ship prices must be 2× item cost (10/20/30/50g).  
- Reroll cost must be flat 2g. One free reroll only if all items are bought.  
- Selling an item must return 50% of its rarity list price.  
- Gold must be capped at 999.  
- Shops must offer 3 unique items + 1 ship.  
- Rerolling must replace only items, not the ship.  
- Shop shelves must only refill on reroll or visiting a new shop.  
- Duplicate item offers must match the highest rarity owned (to enable merges).  
- Shops/rewards must not contain duplicate items in the same set.  
- Player inventory must be limited to 5 slots.  
- Player must own only 1 ship at a time. New ship replaces current ship.  
- Each ship type must have a fixed rarity.  
- Ship replacement must block purchase if inventory overflow would occur.  
- Item merging must be enforced: identical rarity items combine into the next rarity.  
- Shop UI must show a merge preview when hovering an item that would merge.  
- Tick rate must be 100ms.  
- Items must wait one full cooldown cycle before first activation.  
- Effect resolution order must be Buff > Damage > Heal > Shield > Debuff.  
- Within same tier: resolve Player → Enemy → Left-to-Right slots → Local action order.  
- Damage resolution must follow: Flat Reduction → % Modifiers → Shield → HP.  
- Buff/debuff durations must stack additively; reapplying extends timer.  
- Passives must always trigger even if stunned.  
- Actives must not trigger while stunned (cooldowns paused).  
- Auras must always remain active.  
- Sudden Death must start after 30s: 1 dmg/tick for 5 ticks, doubling every 5 ticks.  
- Healing must not revive a ship reduced to ≤ 0 HP that tick.  
- Battle must end in a tie if both ships ≤ 0 HP after a tick.  
- RNG must be seeded at run start and persist through the run.  
- Items must support multiple abilities.  
- Each ability must be defined as Trigger + one or more Actions.  
- Ability types must be Active, Passive, or Aura.  
- Event bus must dispatch item-specific and system-wide events.  
- Passive resolution order must be Player → Enemy → Left-to-Right → Local order.  
- Auras must be recalculated every tick.  
- Each run must be 15 encounters + final boss.  
- Shops must appear at least once every 5 floors.  
- Ports must appear at least once per path.  
- Elites must appear at least once every 6–7 floors and not be adjacent to another elite.  
- Elite rewards must roll as if 3 floors deeper.  
- Ports must heal ceil(30% of max lives), capped at max lives.  
- Events must have min/max floor requirements and must not duplicate unless marked repeatable with cooldown.  
- Onboarding/tutorial must teach merging, synergies, and encounter types.  
- UI must highlight tag synergies.  
- Sudden Death must include visual/audio FX and show damage numbers.  
- Run summary screen must display encounters visited, gold earned, final loadout, and boss faced.  

---

## 🔧 New: Item Object Model & Tooltip Enforcement
- All item effects must support rarity-based definitions. Ability/action parameters (damage, heal, cooldown, shield, etc.) must be tunable per tier (Bronze/Silver/Gold/Diamond). Merging into a higher rarity must always replace effect values with the new tier’s parameters.

- All items must conform to a unified schema: Identity (id, name, rarity, tags, size), Economy (baseCost, sellValue), Abilities (trigger + actions + priority + type), State (cooldownRemaining, stacks, disabled, vars), and Enchantments.  
- Items must always be the same object across contexts (shop, reward, inventory, battle).  
- Tooltips must always render from the item schema, never hardcoded text.  
- Tooltip sections must include: Header (name, rarity, tags), Context Row (cost/sell/cooldown), Abilities (resolved from triggers/actions), Enchantments, Live State (cooldowns, stacks, disabled status), and Merge Preview if applicable.  
- Tooltip numbers must resolve live values (e.g., cooldown remaining, currentDamage, stack counts) from the item state.  
- Stackable effects must display both count and tick interval (e.g., Burn ×3, triggers every 5 ticks).  
- Boolean effects must display remaining duration only (e.g., Stunned: 3s).  
- Passives must display their trigger conditions (e.g., “When ally activates…”).  
- Merge preview must always show the resulting upgrade path (e.g., “Bronze + Bronze ⇒ Silver”).  


## 🛡 Enemy Design Enforcement
- Enemies must be authored as fixed templates (no runtime stat scaling).  
- Depth progression must control which enemies appear, not their stats.  
- Same enemy cannot appear in consecutive battles.  
- One-per-run enemies must not reappear within the same run.  
- Telemetry/QA must validate enemy pool composition, back-to-back ban, and one-per-run logic.  
