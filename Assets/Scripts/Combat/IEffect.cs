using UnityEngine;
using PirateRoguelike.Data;
using System.Collections.Generic;

public interface IEffect
{
    void Apply(CombatContext ctx);
}
