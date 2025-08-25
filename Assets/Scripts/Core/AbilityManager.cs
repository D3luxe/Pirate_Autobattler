using UnityEngine;
using System.Collections.Generic;
using PirateRoguelike.Data;
using PirateRoguelike.Data.Abilities;
using PirateRoguelike.Combat; // Required for CombatContext

public static class AbilityManager
{
    private static bool _isInitialized = false;
    // A dictionary mapping a trigger type to all abilities that use that trigger.
    private static readonly Dictionary<TriggerType, List<AbilitySO>> _activeAbilities = new Dictionary<TriggerType, List<AbilitySO>>();

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
        // Process active items for the player ship
        foreach (var item in playerShip.Equipped)
        {
            if (item != null && item.Def.isActive)
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
        }

        // Process active items for the enemy ship
        foreach (var item in enemyShip.Equipped)
        {
            if (item != null && item.Def.isActive)
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
    }

    private static void HandleBattleStart(CombatContext ctx)
    {
        _activeAbilities.Clear();
        Debug.Log("AbilityManager: Registering abilities for new battle.");

        // Register abilities from player's items
        if (ctx.Caster != null)
        {
            foreach (var item in ctx.Caster.Equipped)
            {
                if (item != null && item.Def != null && item.Def.abilities != null) 
                {
                    RegisterAbilities(item.Def.abilities);
                }
            }
        }

        // Register abilities from enemy's items
        if (ctx.Target != null)
        {
            foreach (var item in ctx.Target.Equipped)
            {
                if (item != null && item.Def != null && item.Def.abilities != null) 
                {
                    RegisterAbilities(item.Def.abilities);
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
