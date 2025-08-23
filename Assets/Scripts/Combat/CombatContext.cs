using System.Collections.Generic;
using PirateRoguelike.Data;

public class CombatContext
{
    public ShipState Caster { get; set; }
    public ShipState Target { get; set; }
    public List<(IEffect Effect, ActionType Type)> EffectsToApply { get; private set; } = new List<(IEffect, ActionType)>();

    public void AddEffectToQueue(IEffect effect, ActionType type)
    {
        EffectsToApply.Add((effect, type));
    }
}
