using PirateRoguelike.Data;
using UnityEngine;
using System;

[Serializable]
public class ActiveCombatEffect
{
    public ActionType Type { get; private set; }
    public StatModifier StatModifier { get; private set; } // For stat-changing effects
    public float Value => StatModifier != null ? StatModifier.Value : 0; // Generic value (e.g., damage per tick, stat modifier)
    public StatType StatType => StatModifier != null ? StatModifier.StatType : StatType.Attack; // Default or handle null
    public int Stacks { get; private set; }
    public float Duration { get; private set; } // Remaining duration in seconds
    public float TickInterval { get; private set; } // How often the effect ticks
    public float TimeSinceLastTick { get; private set; } // Time elapsed since last tick

    public ActiveCombatEffect(ActionType type, float value, int stacks, float duration, float tickInterval, StatType statType)
    {
        Type = type;
        Stacks = stacks;
        Duration = duration;
        TickInterval = tickInterval;
        TimeSinceLastTick = 0f;
        // For non-stat-modifying effects, StatModifier will be null
        StatModifier = (type == ActionType.Buff || type == ActionType.Debuff || type == ActionType.StatChange) 
            ? new StatModifier(statType, StatModifierType.Flat, value) // Assuming flat for now
            : null;
    }

    // Overload for effects that directly provide a StatModifier
    public ActiveCombatEffect(ActionType type, StatModifier modifier, int stacks, float duration, float tickInterval)
    {
        Type = type;
        StatModifier = modifier;
        Stacks = stacks;
        Duration = duration;
        TickInterval = tickInterval;
        TimeSinceLastTick = 0f;
    }

    public void AddStacks(int amount) => Stacks += amount;
    public void ExtendDuration(float amount) => Duration += amount;
    public void ReduceStacks(int amount) => Stacks = Mathf.Max(0, Stacks - amount);

    public bool Tick(float deltaTime)
    {
        TimeSinceLastTick += deltaTime;
        Duration -= deltaTime;

        bool triggered = false;
        if (TickInterval > 0 && TimeSinceLastTick >= TickInterval)
        {
            triggered = true;
            TimeSinceLastTick -= TickInterval; // Reset for next tick
        }
        return triggered;
    }

    public bool IsExpired => Duration <= 0;
}