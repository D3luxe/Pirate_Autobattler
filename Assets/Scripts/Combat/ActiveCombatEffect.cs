using UnityEngine;
using PirateRoguelike.Data.Effects;

[System.Serializable]
public class ActiveCombatEffect
{
    public EffectSO Def { get; private set; }
    public int Stacks { get; private set; }
    public float RemainingDuration { get; private set; }

    private float _timeSinceLastTick;

    public ActiveCombatEffect(EffectSO definition)
    {
        Def = definition;
        Stacks = 1; // Start with 1 stack
        RemainingDuration = Def.Duration;
        _timeSinceLastTick = 0f;
    }

    /// <summary>
    /// Ticks the effect's duration and determines if its action should execute.
    /// </summary>
    /// <param name="deltaTime">Time since last game tick.</param>
    /// <returns>True if the effect's action should execute.</returns>
    public bool Tick(float deltaTime)
    {
        RemainingDuration -= deltaTime;
        
        if (Def.TickInterval <= 0)
        {
            return false; // This effect does not tick.
        }

        _timeSinceLastTick += deltaTime;
        if (_timeSinceLastTick >= Def.TickInterval)
        {
            _timeSinceLastTick -= Def.TickInterval;
            return true;
        }

        return false;
    }

    public void AddStack()
    {
        if (Def.IsStackable)
        {
            Stacks = Mathf.Min(Stacks + 1, Def.MaxStacks);
        }
        // Add duration on adding a stack, instead of resetting
        RemainingDuration += Def.Duration;
    }

    public bool IsExpired()
    {
        return RemainingDuration <= 0;
    }
}
