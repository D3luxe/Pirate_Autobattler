using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Combat;
using PirateRoguelike.UI;
using PirateRoguelike.Core;

namespace PirateRoguelike.Combat
{
    public class CombatController : MonoBehaviour
{
    public ShipState Player { get; private set; }
    public ShipState Enemy { get; private set; }

    private ITickService _tickService;
    private BattleUIController _battleUI; // Store a reference to the UI controller
    private EnemyPanelController _enemyPanelController; // Reference to the enemy panel controller
    private float _battleDuration = 0f; // Track battle duration
    private bool _suddenDeathStarted = false;
    private float _suddenDeathDamage = 1f; // Initial sudden death damage
    private int _suddenDeathTickCount = 0; // Count ticks for doubling damage

    // Called to set up the battle
    public void Init(ShipState player, EnemySO enemyDef, ITickService tickService, BattleUIController battleUI, ShipStateView playerShipStateView, ShipStateView enemyShipStateView, EnemyPanelController enemyPanelController)
    {
        Player = player;
        Enemy = new ShipState(GameDataRegistry.GetShip(enemyDef.shipId)); // Create ShipState from EnemySO
        Enemy.Equipped = enemyDef.itemLoadout.Select(itemSO => new ItemInstance(itemSO)).ToArray(); // Set enemy's equipped items from its definition
        _tickService = tickService;
        _battleUI = battleUI; // Store the reference
        _enemyPanelController = enemyPanelController; // Store the reference

        _tickService.OnTick += HandleTick;
        _tickService.StartTicking();

        // Initialize Views
        playerShipStateView.Initialize(Player);
        enemyShipStateView.Initialize(Enemy);

        // Initialize BattleUI and subscribe to events
        _battleUI.Initialize(playerShipStateView, enemyShipStateView, InventoryUI.Instance);
        _enemyPanelController.Initialize(Enemy); // Initialize the enemy panel
        Player.OnHealthChanged += _battleUI.UpdatePlayerHUD; // Subscribe to player health changes
        Enemy.OnHealthChanged += _battleUI.UpdateEnemyHUD; // Subscribe to enemy health changes

        _battleDuration = 0f; // Reset battle duration
        _suddenDeathStarted = false; // Reset sudden death flag
        _suddenDeathDamage = 1f; // Reset sudden death damage
        _suddenDeathTickCount = 0; // Reset sudden death tick count

        // Dispatch battle start event for the AbilityManager
        var battleStartCtx = new CombatContext { Caster = Player, Target = Enemy };
        EventBus.DispatchBattleStart(battleStartCtx);

        // Set global UI state
        UIInteractionService.IsInCombat = true;
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
            _suddenDeathDamage = 1f; // Reset damage on start
            _suddenDeathTickCount = 0; // Reset tick count on start
            EventBus.DispatchSuddenDeathStarted();
            Debug.Log("Sudden Death has begun!");
        }

        if (_suddenDeathStarted)
        {
            _suddenDeathTickCount++;
            if (_suddenDeathTickCount % 5 == 0)
            {
                _suddenDeathDamage *= 2; // Double damage every 5 ticks
                Debug.Log($"Sudden Death damage doubled to {_suddenDeathDamage}!");
            }
            Player.TakeDamage(Mathf.RoundToInt(_suddenDeathDamage)); 
            Enemy.TakeDamage(Mathf.RoundToInt(_suddenDeathDamage));
        }

        // Process active effects on ships
        ProcessActiveEffects(Player, Enemy, _tickService.IntervalSec);
        ProcessActiveEffects(Enemy, Player, _tickService.IntervalSec);

        // Reduce stun duration
        Player.ReduceStun(_tickService.IntervalSec);
        Enemy.ReduceStun(_tickService.IntervalSec);

        // Dispatch OnTick for general passive abilities
        EventBus.DispatchTick(Player, Enemy, _tickService.IntervalSec);

        CheckBattleEndConditions();
    }

    private void ProcessActiveEffects(ShipState ship, ShipState opponent, float deltaTime)
    {
        // Use a copy to allow modification during iteration
        var effectsToRemove = new List<ActiveCombatEffect>();

        // Sort active effects by their ActionType priority before processing
        // This ensures effects like Buffs are applied before Damage, etc.
        var sortedActiveEffects = ship.ActiveEffects
            .OrderBy(ae => ae.Def.TickAction != null ? ae.Def.TickAction.GetActionType().GetPriority() : 100) 
            .ToList();

        foreach (var activeEffect in sortedActiveEffects) // Iterate through sorted copy
        {
            if (activeEffect.Tick(deltaTime))
            {
                if (activeEffect.Def.TickAction != null)
                {
                    var ctx = new CombatContext { Caster = ship, Target = opponent };
                    activeEffect.Def.TickAction.Execute(ctx);
                }
            }

            if (activeEffect.IsExpired())
            {
                effectsToRemove.Add(activeEffect);
            }
        }

        foreach (var effect in effectsToRemove)
        {
            ship.ActiveEffects.Remove(effect);
        }
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
}