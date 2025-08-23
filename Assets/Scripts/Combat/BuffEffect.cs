using PirateRoguelike.Data;

public class BuffEffect : IEffect
{
    private float _value;
    private float _duration;
    private float _tickInterval;
    private int _stacks;
    private StatType _statType;

    public BuffEffect(float value, float duration, float tickInterval, int stacks, StatType statType)
    {
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _stacks = stacks;
        _statType = statType;
    }

    public void Apply(CombatContext ctx)
    {
        StatModifier modifier = new StatModifier(_statType, StatModifierType.Flat, _value); // Assuming flat for now
        ActiveCombatEffect newEffect = new ActiveCombatEffect(ActionType.Buff, modifier, _stacks, _duration, _tickInterval);
        ctx.Target.ActiveEffects.Add(newEffect);
        ctx.Target.AddStatModifier(modifier);
        EventBus.DispatchBuffApplied(ctx.Target, newEffect);
        UnityEngine.Debug.Log($"Applying a buff of value {_value} with {_stacks} stacks for {_duration}s (ticks every {_tickInterval}s) to {ctx.Target.Def.displayName}.");
    }
}