using PirateRoguelike.Data;

public class StunEffect : IEffect
{
    private float _duration;

    public StunEffect(float duration)
    {
        _duration = duration;
    }

    public void Apply(CombatContext ctx)
    {
        // Stun is a boolean debuff
        // For now, we'll just log it. Actual stun logic needs to be implemented in ShipState/CombatController
        ctx.Target.ApplyStun(_duration);
        UnityEngine.Debug.Log($"Applying Stun to {ctx.Target.Def.displayName} for {_duration}s.");
    }
}