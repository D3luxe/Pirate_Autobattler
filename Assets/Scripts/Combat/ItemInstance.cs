// Recompile test
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;

public class ItemInstance
{
    public ItemSO Def { get; }
    public float CooldownRemaining { get; private set; }
    public bool IsActive => Def.isActive;
    public bool IsStunned => _stunDuration > 0;

    private float _stunDuration;

    public ItemInstance(ItemSO definition)
    {
        Def = definition;
        CooldownRemaining = Def.cooldownSec; // Start on cooldown

        // Subscribe to events based on abilities
        if (Def.abilities != null)
        {
            foreach (var ability in Def.abilities)
            {
                switch (ability.triggerType)
                {
                    case TriggerType.OnItemReady:
                        EventBus.OnItemReady += HandleOnItemReady;
                        break;
                    case TriggerType.OnDamageDealt:
                        EventBus.OnDamageDealt += HandleOnDamageDealt;
                        break;
                    case TriggerType.OnDamageReceived:
                        EventBus.OnDamageReceived += HandleOnDamageReceived;
                        break;
                    case TriggerType.OnHeal:
                        EventBus.OnHeal += HandleOnHeal;
                        break;
                    case TriggerType.OnBattleStart:
                        EventBus.OnBattleStart += HandleOnBattleStart;
                        break;
                    case TriggerType.OnAllyActivate:
                        EventBus.OnAllyActivate += HandleOnAllyActivate;
                        break;
                    case TriggerType.OnShieldGained:
                        EventBus.OnShieldGained += HandleOnShieldGained;
                        break;
                    case TriggerType.OnDebuffApplied:
                        EventBus.OnDebuffApplied += HandleOnDebuffApplied;
                        break;
                    case TriggerType.OnBuffApplied:
                        EventBus.OnBuffApplied += HandleOnBuffApplied;
                        break;
                    case TriggerType.OnTick:
                        EventBus.OnTick += HandleOnTick;
                        break;
                    // Add more subscriptions for other trigger types as needed
                }
            }
        }
    }

    // Constructor for loading from save data
    public ItemInstance(SerializableItemInstance data)
    {
        Def = GameDataRegistry.GetItem(data.itemId);
        CooldownRemaining = data.cooldownRemaining;
        _stunDuration = data.stunDuration;

        // Re-subscribe to events based on abilities
        if (Def.abilities != null)
        {
            foreach (var ability in Def.abilities)
            {
                switch (ability.triggerType)
                {
                    case TriggerType.OnItemReady:
                        EventBus.OnItemReady += HandleOnItemReady;
                        break;
                    case TriggerType.OnDamageDealt:
                        EventBus.OnDamageDealt += HandleOnDamageDealt;
                        break;
                    case TriggerType.OnDamageReceived:
                        EventBus.OnDamageReceived += HandleOnDamageReceived;
                        break;
                    case TriggerType.OnHeal:
                        EventBus.OnHeal += HandleOnHeal;
                        break;
                    case TriggerType.OnBattleStart:
                        EventBus.OnBattleStart += HandleOnBattleStart;
                        break;
                    case TriggerType.OnAllyActivate:
                        EventBus.OnAllyActivate += HandleOnAllyActivate;
                        break;
                    case TriggerType.OnShieldGained:
                        EventBus.OnShieldGained += HandleOnShieldGained;
                        break;
                    case TriggerType.OnDebuffApplied:
                        EventBus.OnDebuffApplied += HandleOnDebuffApplied;
                        break;
                    case TriggerType.OnBuffApplied:
                        EventBus.OnBuffApplied += HandleOnBuffApplied;
                        break;
                    case TriggerType.OnTick:
                        EventBus.OnTick += HandleOnTick;
                        break;
                }
            }
        }
    }

    public void Dispose()
    {
        if (Def.abilities != null)
        {
            foreach (var ability in Def.abilities)
            {
                switch (ability.triggerType)
                {
                    case TriggerType.OnItemReady:
                        EventBus.OnItemReady -= HandleOnItemReady;
                        break;
                    case TriggerType.OnDamageDealt:
                        EventBus.OnDamageDealt -= HandleOnDamageDealt;
                        break;
                    case TriggerType.OnDamageReceived:
                        EventBus.OnDamageReceived -= HandleOnDamageReceived;
                        break;
                    case TriggerType.OnHeal:
                        EventBus.OnHeal -= HandleOnHeal;
                        break;
                    case TriggerType.OnBattleStart:
                        EventBus.OnBattleStart -= HandleOnBattleStart;
                        break;
                    case TriggerType.OnAllyActivate:
                        EventBus.OnAllyActivate -= HandleOnAllyActivate;
                        break;
                    case TriggerType.OnShieldGained:
                        EventBus.OnShieldGained -= HandleOnShieldGained;
                        break;
                    case TriggerType.OnDebuffApplied:
                        EventBus.OnDebuffApplied -= HandleOnDebuffApplied;
                        break;
                    case TriggerType.OnBuffApplied:
                        EventBus.OnBuffApplied -= HandleOnBuffApplied;
                        break;
                    case TriggerType.OnTick:
                        EventBus.OnTick -= HandleOnTick;
                        break;
                    // Add more unsubscriptions
                }
            }
        }
    }

    public SerializableItemInstance ToSerializable()
    {
        return new SerializableItemInstance(Def.id, CooldownRemaining, _stunDuration);
    }

    private void HandleOnItemReady(ItemInstance item, CombatContext ctx)
    {
        if (item == this) // Only trigger if this item is the one ready
        {
            ExecuteAbilities(ctx, TriggerType.OnItemReady);
        }
    }

    private void HandleOnDamageDealt(ShipState caster, ShipState target, float amount)
    {
        // Need to determine the context for this ability. For now, assume caster is the item's owner.
        // This is a simplification; a proper context would be passed with the event.
        CombatContext ctx = new CombatContext { Caster = caster, Target = target };
        ExecuteAbilities(ctx, TriggerType.OnDamageDealt);
    }

    private void HandleOnDamageReceived(ShipState target, float amount)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target }; // Caster and Target are the same for self-received damage
        ExecuteAbilities(ctx, TriggerType.OnDamageReceived);
    }

    private void HandleOnHeal(ShipState target, float amount)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target }; // Caster and Target are the same for self-healed
        ExecuteAbilities(ctx, TriggerType.OnHeal);
    }

    private void HandleOnBattleStart(CombatContext ctx)
    {
        // For OnBattleStart, the context is already provided
        ExecuteAbilities(ctx, TriggerType.OnBattleStart);
    }

    private void HandleOnAllyActivate(ItemInstance activatedItem, CombatContext ctx)
    {
        if (activatedItem != this) // Only trigger if it's an ally's item activating
        {
            ExecuteAbilities(ctx, TriggerType.OnAllyActivate);
        }
    }

    private void HandleOnShieldGained(ShipState target, float amount)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target };
        ExecuteAbilities(ctx, TriggerType.OnShieldGained);
    }

    private void HandleOnDebuffApplied(ShipState target, ActiveCombatEffect effect)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target };
        ExecuteAbilities(ctx, TriggerType.OnDebuffApplied);
    }

    private void HandleOnBuffApplied(ShipState target, ActiveCombatEffect effect)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target };
        ExecuteAbilities(ctx, TriggerType.OnBuffApplied);
    }

    private void HandleOnTick(ShipState target, float deltaTime)
    {
        // Need to determine the context for this ability. For now, assume target is the item's owner.
        CombatContext ctx = new CombatContext { Caster = target, Target = target };
        ExecuteAbilities(ctx, TriggerType.OnTick);
    }

    public void OnTick(CombatContext ctx)
    {
        if (ctx.Caster.IsStunned) return; // Ship is stunned, item cannot tick

        if (IsStunned)
        {
            _stunDuration -= 0.1f; // Assuming 100ms tick
            return;
        }

        if (IsActive)
        {
            CooldownRemaining -= 0.1f;
            if (CooldownRemaining <= 0)
            {
                EventBus.DispatchItemReady(this, ctx);
                CooldownRemaining = Def.cooldownSec;
            }
        }
    }

    public void Stun(float duration)
    {
        _stunDuration = duration;
    }

    private void ExecuteAbilities(CombatContext ctx, TriggerType triggeredBy)
    {
        foreach (var ability in Def.abilities)
        {
            if (ability.triggerType == triggeredBy)
            {
                foreach (var action in ability.actions)
                {
                    IEffect effect = null;
                    float value = Def.GetValueForRarity(action.values, Def.rarity);
                    float duration = Def.GetValueForRarity(action.durations, Def.rarity);
                    float tickInterval = Def.GetValueForRarity(action.tickIntervals, Def.rarity);
                    int stacks = (int)Def.GetValueForRarity(action.stacks, Def.rarity);

                    switch (action.actionType)
                    {
                        case ActionType.Damage:
                            effect = new DamageEffect(value, ctx.Caster);
                            break;
                        case ActionType.Heal:
                            effect = new HealEffect(value);
                            break;
                        case ActionType.Shield:
                            effect = new ShieldEffect(value);
                            break;
                        case ActionType.Buff:
                            effect = new BuffEffect(value, duration, tickInterval, stacks, action.statType);
                            break;
                        case ActionType.Debuff:
                            effect = new DebuffEffect(value, duration, tickInterval, stacks, action.statType);
                            break;
                        case ActionType.StatChange:
                            effect = new StatChangeEffect(value, duration, tickInterval, stacks, action.statType);
                            break;
                        case ActionType.Burn:
                            effect = new BurnEffect(value, duration, tickInterval, stacks, action.statType);
                            break;
                        case ActionType.Poison:
                            effect = new PoisonEffect(value, duration, tickInterval, stacks, action.statType);
                            break;
                        case ActionType.Stun:
                            effect = new StunEffect(duration);
                            break;
                        // Add other ActionTypes here as they are implemented
                        default:
                            UnityEngine.Debug.LogWarning($"Item {Def.displayName} has unhandled action type: {action.actionType}");
                            break;
                    }

                    if (effect != null)
                    {
                        ctx.AddEffectToQueue(effect, action.actionType);
                    }
                }
            }
        }
    }
}
