using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;
using PirateRoguelike.Data.Abilities;
using PirateRoguelike.Combat; // Required for CombatContext

namespace PirateRoguelike.Core
{
    public static class AbilityManager
{
    private static bool _isInitialized = false;
    // A dictionary mapping a trigger type to all abilities that use that trigger.
    private static readonly Dictionary<TriggerType, List<AbilitySO>> _activeAbilities = new Dictionary<TriggerType, List<AbilitySO>>();
    // OPTIMIZATION: Lists to hold only items with active abilities, to avoid polling all items every tick.
    private static readonly List<ItemInstance> _playerActiveItems = new List<ItemInstance>();
    private static readonly List<ItemInstance> _enemyActiveItems = new List<ItemInstance>();

    public static void Initialize()
    {
        if (_isInitialized) return;

        SubscribeToEvents();
        _isInitialized = true;
        Debug.Log("AbilityManager initialized.");
    }

    public static void Shutdown()
    {
        if (!_isInitialized) return;

        UnsubscribeFromEvents();
        _activeAbilities.Clear();
        _playerActiveItems.Clear();
        _enemyActiveItems.Clear();
        _isInitialized = false;
        Debug.Log("AbilityManager shut down.");
    }

    private static void RegisterAbilities(IEnumerable<AbilitySO> abilities)
    {
        foreach (var ability in abilities)
        {
            if (!_activeAbilities.ContainsKey(ability.Trigger))
            {
                _activeAbilities[ability.Trigger] = new List<AbilitySO>();
            }
            _activeAbilities[ability.Trigger].Add(ability);
        }
    }

    #region Event Subscription

    private static void SubscribeToEvents()
    {
        EventBus.OnBattleStart += HandleBattleStart;
        EventBus.OnDamageDealt += HandleDamageDealt;
        EventBus.OnDamageReceived += HandleDamageReceived;
        EventBus.OnHeal += HandleHeal;
        EventBus.OnTick += HandleTick;
        // Add other event subscriptions here
    }

    private static void UnsubscribeFromEvents()
    {
        EventBus.OnBattleStart -= HandleBattleStart;
        EventBus.OnDamageDealt -= HandleDamageDealt;
        EventBus.OnDamageReceived -= HandleDamageReceived;
        EventBus.OnHeal -= HandleHeal;
        EventBus.OnTick -= HandleTick;
        // Add other event unsubscriptions here
    }

    #endregion

    #region Event Handlers

    private static void HandleTick(ShipState playerShip, ShipState enemyShip, float deltaTime)
    {
        // OPTIMIZATION: Process active items for the player ship from the pre-filtered list
        foreach (var item in _playerActiveItems)
        {
            if (item.CooldownRemaining <= 0)
            {
                // Item is ready to activate
                Debug.Log($"Item {item.Def.displayName} (Player) is ready to activate.");
                foreach (var ability in item.Def.abilities)
                {
                    // Only execute abilities with OnItemReady trigger for now
                    if (ability.Trigger == TriggerType.OnItemReady)
                    {
                        Debug.Log($"Executing OnItemReady ability: {ability.displayName} (Player)");
                        var ctx = new CombatContext { Caster = playerShip, Target = enemyShip };
                        foreach (var action in ability.Actions)
                        {
                            action.Execute(ctx);
                        }
                    }
                }
                item.CooldownRemaining = item.Def.cooldownSec; // Reset cooldown
            }
            else
            {
                item.CooldownRemaining -= deltaTime; // Reduce cooldown
            }
        }

        // OPTIMIZATION: Process active items for the enemy ship from the pre-filtered list
        foreach (var item in _enemyActiveItems)
        {
            if (item.CooldownRemaining <= 0)
            {
                // Item is ready to activate
                Debug.Log($"Item {item.Def.displayName} (Enemy) is ready to activate.");
                foreach (var ability in item.Def.abilities)
                {
                    // Only execute abilities with OnItemReady trigger for now
                    if (ability.Trigger == TriggerType.OnItemReady)
                    {
                        Debug.Log($"Executing OnItemReady ability: {ability.displayName} (Enemy)");
                        var ctx = new CombatContext { Caster = enemyShip, Target = playerShip };
                        foreach (var action in ability.Actions)
                        {
                            action.Execute(ctx);
                        }
                    }
                }
                item.CooldownRemaining = item.Def.cooldownSec; // Reset cooldown
            }
            else
            {
                item.CooldownRemaining -= deltaTime; // Reduce cooldown
            }
        }
    }

    private static void HandleBattleStart(CombatContext ctx)
    {
        _activeAbilities.Clear();
        _playerActiveItems.Clear();
        _enemyActiveItems.Clear();
        Debug.Log("AbilityManager: Cleared state and registering abilities for new battle.");

        // Register abilities and populate active item lists
        if (ctx.Caster != null) // Player
        {
            foreach (var item in ctx.Caster.Equipped)
            {
                if (item != null)
                { 
                    if(item.Def != null && item.Def.abilities != null) RegisterAbilities(item.Def.abilities);
                    if(item.Def != null && item.Def.isActive) _playerActiveItems.Add(item);
                }
            }
        }

        if (ctx.Target != null) // Enemy
        {
            foreach (var item in ctx.Target.Equipped)
            {
                if (item != null)
                {
                    if(item.Def != null && item.Def.abilities != null) RegisterAbilities(item.Def.abilities);
                    if(item.Def != null && item.Def.isActive) _enemyActiveItems.Add(item);
                }
            }
        }
        
        // Execute OnBattleStart abilities
        CheckAndExecuteAbilities(TriggerType.OnBattleStart, ctx);
    }

    private static void HandleDamageDealt(ShipState caster, ShipState target, float amount)
    {
        var ctx = new CombatContext { Caster = caster, Target = target, DamageAmount = amount };
        CheckAndExecuteAbilities(TriggerType.OnDamageDealt, ctx);
    }

    private static void HandleDamageReceived(ShipState target, float amount)
    {
        // Note: Caster is unknown in this context.
        var ctx = new CombatContext { Target = target, DamageAmount = amount };
        CheckAndExecuteAbilities(TriggerType.OnDamageReceived, ctx);
    }

    private static void HandleHeal(ShipState target, float amount)
    {
        // Note: Caster is the same as Target in a healing context.
        var ctx = new CombatContext { Caster = target, Target = target, HealAmount = amount };
        CheckAndExecuteAbilities(TriggerType.OnHeal, ctx);
    }

    #endregion

    private static void CheckAndExecuteAbilities(TriggerType trigger, CombatContext ctx)
    {
        if (_activeAbilities.TryGetValue(trigger, out var abilitiesToExecute))
        {
            Debug.Log($"Found {abilitiesToExecute.Count} abilities for trigger {trigger}");
            foreach (var ability in abilitiesToExecute)
            {
                Debug.Log($"Executing ability: {ability.name}");
                foreach (var action in ability.Actions)
                {
                    action.Execute(ctx);
                }
            }
        }
    }
}
}