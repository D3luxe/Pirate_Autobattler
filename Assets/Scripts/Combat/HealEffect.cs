using UnityEngine;

public class HealEffect : IEffect
{
    private float _amount;

    public HealEffect(float amount)
    {
        _amount = amount;
    }

    public void Apply(CombatContext ctx)
    {
        if (ctx.Caster != null) // Healing usually applies to the caster's ship
        {
            ctx.Caster.Heal((int)_amount);
            Debug.Log($"{ctx.Caster.Def.displayName} healed for {_amount}");
        }
    }
}
