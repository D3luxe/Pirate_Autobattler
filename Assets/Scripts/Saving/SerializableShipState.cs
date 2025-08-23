using System;
using System.Collections.Generic;

[Serializable]
public class SerializableShipState
{
    public string shipId;
    public int currentHealth;
    public float currentShield;
    public float stunDuration;
    public List<SerializableItemInstance> equippedItems;
    public List<ActiveCombatEffect> activeEffects;
    public List<StatModifier> activeStatModifiers;

    public SerializableShipState(string id, int health, float shield, float stun, List<SerializableItemInstance> equipped, List<ActiveCombatEffect> activeFx, List<StatModifier> activeStatMods)
    {
        shipId = id;
        currentHealth = health;
        currentShield = shield;
        stunDuration = stun;
        equippedItems = equipped;
        activeEffects = activeFx;
        activeStatModifiers = activeStatMods;
    }
}