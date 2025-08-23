using PirateRoguelike.Data;

public class StatChangeEffect : IEffect
{
    private float _value;
    private float _duration;
    private float _tickInterval;
    private int _stacks;
    private StatType _statType;

    public StatChangeEffect(float value, float duration, float tickInterval, int stacks, StatType statType)
    {
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _stacks = stacks;
        _statType = statType;
    }

    public void Apply(CombatContext ctx)
    {
        StatModifier modifier = new StatModifier(_statType, StatModifierType.Flat, _value); // StatChange can be positive or negative
        ActiveCombatEffect newEffect = new ActiveCombatEffect(ActionType.StatChange, modifier, _stacks, _duration, _tickInterval);
        ctx.Target.ActiveEffects.Add(newEffect);
        ctx.Target.AddStatModifier(modifier);
        UnityEngine.Debug.Log($"Applying a stat change of value {_value} with {_stacks} stacks for {_duration}s (ticks every {_tickInterval}s) to {ctx.Target.Def.displayName}.");
    }
}