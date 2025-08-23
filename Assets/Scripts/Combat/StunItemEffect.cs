using UnityEngine;

public class StunItemEffect : IEffect
{
    private float _duration;

    public StunItemEffect(float duration)
    {
        _duration = duration;
    }

    public void Apply(CombatContext ctx)
    {
        if (ctx.Target == null) return;

        foreach (var item in ctx.Target.Equipped)
        {
            if (item != null)
            {
                item.Stun(_duration);
                Debug.Log($"{item.Def.displayName} on {ctx.Target.Def.displayName} stunned for {_duration} seconds.");
            }
        }
    }
}