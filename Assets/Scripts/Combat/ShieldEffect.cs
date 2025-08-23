using PirateRoguelike.Data;

public class ShieldEffect : IEffect
{
    private float _amount;

    public ShieldEffect(float amount)
    {
        _amount = amount;
    }

    public void Apply(CombatContext ctx)
    {
        // Apply shield to the target ship
        ctx.Target.AddShield(_amount);
        EventBus.DispatchShieldGained(ctx.Target, _amount);
        UnityEngine.Debug.Log($"Shielded {ctx.Target.Def.displayName} for {_amount} shield.");
    }
}