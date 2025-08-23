using UnityEngine;

public class DamageEffect : IEffect
{
    private float _amount;
    private ShipState _caster;

    public DamageEffect(float amount, ShipState caster)
    {
        _amount = amount;
        _caster = caster;
    }

    public void Apply(CombatContext ctx)
    {
        // Damage always targets ships
        if (ctx.Target != null)
        {
            ctx.Target.TakeDamage((int)_amount);
            EventBus.DispatchDamageDealt(_caster, ctx.Target, _amount);
            // TODO: Hook into visual/audio feedback system
            Debug.Log($"{_caster.Def.displayName} dealt {_amount} damage to {ctx.Target.Def.displayName}");
        }
    }
}
