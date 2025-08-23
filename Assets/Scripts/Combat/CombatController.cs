// Recompile test
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;

public class CombatController : MonoBehaviour
{
    public ShipState Player { get; private set; }
    public ShipState Enemy { get; private set; }

    private ITickService _tickService;
    private BattleUIController _battleUI; // Store a reference to the UI controller
    private float _battleDuration = 0f; // Track battle duration
    private bool _suddenDeathStarted = false;

    // Called to set up the battle
    public void Init(ShipState player, EnemySO enemyDef, ITickService tickService, BattleUIController battleUI, ShipView playerShipView, ShipView enemyShipView)
    {
        Player = player;
        Enemy = new ShipState(GameDataRegistry.GetShip(enemyDef.shipId)); // Create ShipState from EnemySO
        _tickService = tickService;
        _battleUI = battleUI; // Store the reference

        _tickService.OnTick += HandleTick;
        _tickService.Start();
        // Create a context for the battle start event
        var battleStartCtx = new CombatContext { Caster = Player, Target = Enemy };
        EventBus.DispatchBattleStart(battleStartCtx); // Dispatch battle start event

        // Scale enemy health based on floor index
        if (GameSession.CurrentRunState != null && GameDataRegistry.GetRunConfig() != null)
        {
            int currentFloorIndex = GameSession.CurrentRunState.currentColumnIndex;
            RunConfigSO runConfig = GameDataRegistry.GetRunConfig();
            float healthScaleFactor = (float)currentFloorIndex / (MapManager.Instance.mapLength - 1);
            int scaledHealth = Mathf.RoundToInt(Mathf.Lerp(runConfig.enemyHealthMin, runConfig.enemyHealthMax, healthScaleFactor));
            scaledHealth = Mathf.Max(1, scaledHealth); // Ensure health is at least 1 to prevent instant battles
            Enemy.SetCurrentHealth(scaledHealth);
            Debug.Log($"Enemy {Enemy.Def.displayName} health scaled to {scaledHealth} at floor {currentFloorIndex}.");

            // Scale enemy item loadout rarity
            List<RarityProbability> rarityProbabilities = GameDataRegistry.GetRarityProbabilitiesForFloor(currentFloorIndex);
            List<ItemInstance> scaledEnemyItems = new List<ItemInstance>();

            foreach (var itemRef in enemyDef.itemLoadout)
            {
                Rarity selectedRarity = GetRandomRarity(rarityProbabilities);
                ItemSO scaledItemSO = GameDataRegistry.GetItem(itemRef.id, selectedRarity); // Get item of scaled rarity
                if (scaledItemSO != null)
                {
                    scaledEnemyItems.Add(new ItemInstance(scaledItemSO));
                }
                else
                {
                    Debug.LogWarning($"Could not find scaled item for {itemRef.id} at rarity {selectedRarity}. Using default.");
                    scaledEnemyItems.Add(new ItemInstance(GameDataRegistry.GetItem(itemRef.id))); // Fallback to default
                }
            }
            Enemy.Equipped = scaledEnemyItems.ToArray(); // Replace enemy's equipped items
        }

        // Initialize Views
        playerShipView.Initialize(Player);
        enemyShipView.Initialize(Enemy);

        // Initialize BattleUI and subscribe to events
        _battleUI.Initialize(playerShipView, enemyShipView, InventoryUI.Instance);
        Player.OnHealthChanged += _battleUI.UpdatePlayerHUD; // Subscribe to player health changes
        Enemy.OnHealthChanged += _battleUI.UpdateEnemyHUD; // Subscribe to enemy health changes

        _battleDuration = 0f; // Reset battle duration
        _suddenDeathStarted = false; // Reset sudden death flag
    }

    private Rarity GetRandomRarity(List<RarityProbability> probabilities)
    {
        int totalWeight = probabilities.Sum(p => p.weight);
        int randomNumber = Random.Range(0, totalWeight);

        foreach (var prob in probabilities)
        {
            if (randomNumber < prob.weight)
            {
                return prob.rarity;
            }
            randomNumber -= prob.weight;
        }
        return Rarity.Bronze; // Fallback
    }

    private void OnDestroy()
    {
        if (_tickService != null)
        {
            _tickService.OnTick -= HandleTick;
            _tickService.Stop();
        }

        // Unsubscribe from health events to prevent memory leaks
        if (Player != null && _battleUI != null) Player.OnHealthChanged -= _battleUI.UpdatePlayerHUD;
        if (Enemy != null && _battleUI != null) Enemy.OnHealthChanged -= _battleUI.UpdateEnemyHUD;
    }

    void HandleTick()
    {
        _battleDuration += _tickService.IntervalSec;

        // Sudden Death logic
        if (!_suddenDeathStarted && _battleDuration >= 30f) // 30 seconds for sudden death
        {
            _suddenDeathStarted = true;
            EventBus.DispatchSuddenDeathStarted();
            Debug.Log("Sudden Death has begun!");
        }

        // Apply Sudden Death damage if active
        if (_suddenDeathStarted)
        {
            // For now, a simple damage application. PRD specifies scaling damage.
            Player.TakeDamage(1); 
            Enemy.TakeDamage(1);
        }

        var currentTickContext = new CombatContext { Caster = Player, Target = Enemy }; // Context for this tick

        // Process player items
        foreach (var item in Player.Equipped)
        {
            if (item == null) continue;
            item.OnTick(currentTickContext);
            // Dispatch OnAllyActivate for other items on the same ship
            foreach (var allyItem in Player.Equipped)
            {
                if (allyItem != null && allyItem != item) // Exclude self
                {
                    EventBus.DispatchAllyActivate(allyItem, currentTickContext);
                }
            }
        }

        // Process enemy items
        foreach (var item in Enemy.Equipped)
        {
            if (item == null) continue;
            // For enemy items, the caster is Enemy and target is Player
            var enemyItemContext = new CombatContext { Caster = Enemy, Target = Player };
            item.OnTick(enemyItemContext);
            // Dispatch OnAllyActivate for other items on the same ship
            foreach (var allyItem in Enemy.Equipped)
            {
                if (allyItem != null && allyItem != item) // Exclude self
                {
                    EventBus.DispatchAllyActivate(allyItem, enemyItemContext);
                }
            }
            // Merge enemy effects into the main context
            foreach (var effectTuple in enemyItemContext.EffectsToApply)
            {
                currentTickContext.AddEffectToQueue(effectTuple.Effect, effectTuple.Type);
            }
        }

        // Apply collected effects in priority order
        ApplyEffectsInOrder(currentTickContext);

        // Process active effects on ships
        ProcessActiveEffects(Player, _tickService.IntervalSec);
        ProcessActiveEffects(Enemy, _tickService.IntervalSec);

        // Reduce stun duration
        Player.ReduceStun(_tickService.IntervalSec);
        Enemy.ReduceStun(_tickService.IntervalSec);

        // Dispatch OnTick for general passive abilities
        EventBus.Dispatch(TriggerType.OnTick, Player, _tickService.IntervalSec);
        EventBus.Dispatch(TriggerType.OnTick, Enemy, _tickService.IntervalSec);

        // Check for win/loss conditions after all effects have been processed
        CheckBattleEndConditions();
    }

    private void ProcessActiveEffects(ShipState ship, float deltaTime)
    {
        // Create a temporary list to avoid modifying collection while iterating
        var effectsToRemove = new System.Collections.Generic.List<ActiveCombatEffect>();

        foreach (var activeEffect in ship.ActiveEffects)
        {
            if (activeEffect.Tick(deltaTime)) // If it's time for the effect to tick
            {
                // Apply the effect based on its type
                // This is a simplified application. More complex effects might need a CombatContext
                switch (activeEffect.Type)
                {
                    case ActionType.Damage:
                        ship.TakeDamage(Mathf.RoundToInt(activeEffect.Value * activeEffect.Stacks));
                        activeEffect.ReduceStacks(1); // Example for Burn-like effect
                        break;
                    case ActionType.Heal:
                        ship.Heal(Mathf.RoundToInt(activeEffect.Value * activeEffect.Stacks));
                        break;
                    case ActionType.Buff:
                        switch (activeEffect.StatType)
                        {
                            case StatType.Attack:
                                ship.AddStatModifier(new StatModifier(StatType.Attack, StatModifierType.Flat, activeEffect.Value * activeEffect.Stacks));
                                break;
                            case StatType.Defense:
                                ship.AddStatModifier(new StatModifier(StatType.Defense, StatModifierType.Flat, activeEffect.Value * activeEffect.Stacks));
                                break;
                        }
                        UnityEngine.Debug.Log($"Applying tick buff to {ship.Def.displayName}. Value: {activeEffect.Value}, Stacks: {activeEffect.Stacks}, Stat: {activeEffect.StatType}");
                        break;
                    case ActionType.Debuff:
                        switch (activeEffect.StatType)
                        {
                            case StatType.Attack:
                                ship.AddStatModifier(new StatModifier(StatType.Attack, StatModifierType.Flat, -activeEffect.Value * activeEffect.Stacks)); // Debuff reduces stat
                                break;
                            case StatType.Defense:
                                ship.AddStatModifier(new StatModifier(StatType.Defense, StatModifierType.Flat, -activeEffect.Value * activeEffect.Stacks)); // Debuff reduces stat
                                break;
                        }
                        UnityEngine.Debug.Log($"Applying tick debuff to {ship.Def.displayName}. Value: {activeEffect.Value}, Stacks: {activeEffect.Stacks}, Stat: {activeEffect.StatType}");
                        break;
                    case ActionType.StatChange:
                        switch (activeEffect.StatType)
                        {
                            case StatType.Attack:
                                ship.AddStatModifier(new StatModifier(StatType.Attack, StatModifierType.Flat, activeEffect.Value * activeEffect.Stacks));
                                break;
                            case StatType.Defense:
                                ship.AddStatModifier(new StatModifier(StatType.Defense, StatModifierType.Flat, activeEffect.Value * activeEffect.Stacks));
                                break;
                        }
                        UnityEngine.Debug.Log($"Applying tick stat change to {ship.Def.displayName}. Value: {activeEffect.Value}, Stacks: {activeEffect.Stacks}, Stat: {activeEffect.StatType}");
                        break;
                    case ActionType.Stun:
                        ship.ApplyStun(activeEffect.Value); // Value represents stun duration
                        UnityEngine.Debug.Log($"Applying stun to {ship.Def.displayName}. Duration: {activeEffect.Value}");
                        break;
                    // Shield effects are typically instant, not ticking
                }
            }

            if (activeEffect.IsExpired || activeEffect.Stacks <= 0)
            {
                effectsToRemove.Add(activeEffect);
                if (activeEffect.StatModifier != null)
                {
                    ship.RemoveStatModifier(activeEffect.StatModifier);
                }
            }
        }

        // Remove expired effects
        foreach (var effect in effectsToRemove)
        {
            ship.ActiveEffects.Remove(effect);
            UnityEngine.Debug.Log($"Removed expired effect: {effect.Type} from {ship.Def.displayName}");
        }
    }

    private void ApplyEffectsInOrder(CombatContext ctx)
    {
        // Sort effects based on ActionType priority: Buff -> Damage -> Heal -> Shield -> Debuff
        // Note: StatChange and Meta are not explicitly in the PRD's priority list, 
        // so we'll place them at the end for now.
        var sortedEffects = ctx.EffectsToApply.OrderBy(effectTuple =>
        {
            switch (effectTuple.Type)
            {
                case ActionType.Buff: return 0;
                case ActionType.Damage: return 1;
                case ActionType.Heal: return 2;
                case ActionType.Shield: return 3;
                case ActionType.Debuff: return 4;
                case ActionType.StatChange: return 5; // Placeholder priority
                case ActionType.Meta: return 6; // Placeholder priority
                default: return 100; // Fallback for unhandled types
            }
        }).ToList();

        foreach (var effectTuple in sortedEffects)
        {
            effectTuple.Effect.Apply(ctx); // Apply the effect
        }

        // Clear effects for the next tick
        ctx.EffectsToApply.Clear();
    }

    private void CheckBattleEndConditions()
    {
        bool playerDefeated = Player.CurrentHealth <= 0;
        bool enemyDefeated = Enemy.CurrentHealth <= 0;

        if (playerDefeated || enemyDefeated)
        {
            _tickService.Stop(); // Stop the combat tick

            bool playerWon = false;
            if (playerDefeated && enemyDefeated)
            {
                Debug.Log("Both ships defeated! Player wins due to tie-breaker.");
                playerWon = true; // Ties favor the player
            }
            else if (enemyDefeated)
            {
                Debug.Log("Enemy defeated! Player wins!");
                playerWon = true;
            }
            else if (playerDefeated)
            {
                Debug.Log("Player defeated! Player loses.");
                playerWon = false;
            }

            // Report outcome to GameSession
            if (GameSession.CurrentRunState != null)
            {
                GameSession.EndBattle(playerWon, GameDataRegistry.GetRunConfig(), Enemy);
            }
            else
            {
                Debug.LogWarning("GameSession not active. Battle ended, but no run state to update.");
            }
        }
    }
}
