using System;
using PirateRoguelike.Data;
using PirateRoguelike.Combat;

namespace PirateRoguelike.Core
{
    public static class EventBus
{
    // System-wide events
    public static event Action<CombatContext> OnBattleStart;
    public static event Action OnSuddenDeathStarted;
    public static event Action OnEncounterEnd;

    // Item-specific triggers (example, more will be added)
    public static event Action<ItemInstance, CombatContext> OnItemReady;
    public static event Action<ItemInstance, CombatContext> OnAllyActivate;

    // Combat-related events
    public static event Action<ShipState, ShipState, float> OnDamageDealt; // Caster, Target, Amount
    public static event Action<ShipState, float> OnDamageReceived; // Target, Amount
    public static event Action<ShipState, float> OnHeal; // Target, Amount
    public static event Action<ShipState, float> OnShieldGained; // Target, Amount
    public static event Action<ShipState, ActiveCombatEffect> OnDebuffApplied; // Target, Effect
    public static event Action<ShipState, ActiveCombatEffect> OnBuffApplied; // Target, Effect
    public static event Action<ShipState, ShipState, float> OnTick; // PlayerShip, EnemyShip, DeltaTime

    // Dispatchers
    public static void DispatchBattleStart(CombatContext ctx) => OnBattleStart?.Invoke(ctx);
    public static void DispatchSuddenDeathStarted() => OnSuddenDeathStarted?.Invoke();
    public static void DispatchEncounterEnd() => OnEncounterEnd?.Invoke();
    public static void DispatchItemReady(ItemInstance item, CombatContext ctx) => OnItemReady?.Invoke(item, ctx);
    public static void DispatchAllyActivate(ItemInstance item, CombatContext ctx) => OnAllyActivate?.Invoke(item, ctx);
    public static void DispatchDamageDealt(ShipState caster, ShipState target, float amount) => OnDamageDealt?.Invoke(caster, target, amount);
    public static void DispatchDamageReceived(ShipState target, float amount) => OnDamageReceived?.Invoke(target, amount);
    public static void DispatchHeal(ShipState target, float amount) => OnHeal?.Invoke(target, amount);
    public static void DispatchShieldGained(ShipState target, float amount) => OnShieldGained?.Invoke(target, amount);
    public static void DispatchDebuffApplied(ShipState target, ActiveCombatEffect effect) => OnDebuffApplied?.Invoke(target, effect);
    public static void DispatchBuffApplied(ShipState target, ActiveCombatEffect effect) => OnBuffApplied?.Invoke(target, effect);
    public static void DispatchTick(ShipState playerShip, ShipState enemyShip, float deltaTime) => OnTick?.Invoke(playerShip, enemyShip, deltaTime);

    // Generic dispatch for other triggers (will be expanded)
    public static void Dispatch(TriggerType triggerType, params object[] args)
    {
        switch (triggerType)
        {
            case TriggerType.OnBattleStart:
                if (args.Length > 0 && args[0] is CombatContext ctxBattleStart)
                {
                    DispatchBattleStart(ctxBattleStart);
                }
                break;
            case TriggerType.OnEncounterEnd:
                // This event is currently unused and has no specific dispatch logic here.
                // DispatchEncounterEnd(); // If it were used, this would be the call.
                break;
            case TriggerType.OnItemReady:
            case TriggerType.OnAllyActivate:
                if (args.Length > 1 && args[0] is ItemInstance itemAllyParam && args[1] is CombatContext ctxAllyParam)
                {
                    DispatchAllyActivate(itemAllyParam, ctxAllyParam);
                }
                break;
            case TriggerType.OnDamageDealt:
                if (args.Length > 2 && args[0] is ShipState casterParam && args[1] is ShipState targetParam && args[2] is float amountParam)
                {
                    DispatchDamageDealt(casterParam, targetParam, amountParam);
                }
                break;
            case TriggerType.OnDamageReceived:
                if (args.Length > 1 && args[0] is ShipState targetParam2 && args[1] is float amountParam2)
                {
                    DispatchDamageReceived(targetParam2, amountParam2);
                }
                break;
            case TriggerType.OnHeal:
                if (args.Length > 1 && args[0] is ShipState targetParam3 && args[1] is float amountParam3)
                {
                    DispatchHeal(targetParam3, amountParam3);
                }
                break;
            case TriggerType.OnShieldGained:
                if (args.Length > 1 && args[0] is ShipState targetParam4 && args[1] is float amountParam4)
                {
                    DispatchShieldGained(targetParam4, amountParam4);
                }
                break;
            case TriggerType.OnDebuffApplied:
                if (args.Length > 1 && args[0] is ShipState targetDebuffParam && args[1] is ActiveCombatEffect effectDebuffParam)
                {
                    DispatchDebuffApplied(targetDebuffParam, effectDebuffParam);
                }
                break;
            case TriggerType.OnBuffApplied:
                if (args.Length > 1 && args[0] is ShipState targetBuffParam && args[1] is ActiveCombatEffect effectBuffParam)
                {
                    DispatchBuffApplied(targetBuffParam, effectBuffParam);
                }
                break;
            case TriggerType.OnTick:
                if (args.Length > 2 && args[0] is ShipState playerShipParam && args[1] is ShipState enemyShipParam && args[2] is float deltaTimeParam)
                {
                    DispatchTick(playerShipParam, enemyShipParam, deltaTimeParam);
                }
                break;
            // Add more cases for other trigger types
            default:
                UnityEngine.Debug.LogWarning($"Unhandled trigger type dispatched: {triggerType}");
                break;
        }
    }
}
}