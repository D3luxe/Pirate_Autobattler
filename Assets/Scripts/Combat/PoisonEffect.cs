using PirateRoguelike.Data;

public class PoisonEffect : IEffect
{
    private float _value;
    private float _duration;
    private float _tickInterval;
    private int _stacks;
    private StatType _statType;

    public PoisonEffect(float value, float duration, float tickInterval, int stacks, StatType statType)
    {
        _value = value;
        _duration = duration;
        _tickInterval = tickInterval;
        _stacks = stacks;
        _statType = statType;
    }

    public void Apply(CombatContext ctx)
    {
        StatModifier modifier = new StatModifier(_statType, StatModifierType.Flat, -_value); // Poison reduces stats
        ActiveCombatEffect newEffect = new ActiveCombatEffect(ActionType.Poison, modifier, _stacks, _duration, _tickInterval);
        ctx.Target.ActiveEffects.Add(newEffect);
        UnityEngine.Debug.Log($"Applying Poison to {ctx.Target.Def.displayName}. Value: {_value}, Stacks: {_stacks}, Duration: {_duration}s, TickInterval: {_tickInterval}s.");
    }
}