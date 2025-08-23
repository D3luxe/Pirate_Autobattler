using PirateRoguelike.Data;

public class DebuffEffect : IEffect
{
    private float _value;
    private float _duration;
    private float _tickInterval;
    private int _stacks;
    private StatType _statType;

    public DebuffEffect(float value, float duration, float tickInterval, int stacks, StatType statType)
    {
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _stacks = stacks;
        _statType = statType;
    }

    public void Apply(CombatContext ctx)
    {
        StatModifier modifier = new StatModifier(_statType, StatModifierType.Flat, -_value); // Debuffs reduce stats
        ActiveCombatEffect newEffect = new ActiveCombatEffect(ActionType.Debuff, modifier, _stacks, _duration, _tickInterval);
        ctx.Target.ActiveEffects.Add(newEffect);
        ctx.Target.AddStatModifier(modifier);
        EventBus.DispatchDebuffApplied(ctx.Target, newEffect);
        UnityEngine.Debug.Log($"Applying a debuff of value {_value} with {_stacks} stacks for {_duration}s (ticks every {_tickInterval}s) to {ctx.Target.Def.displayName}.");
    }
}