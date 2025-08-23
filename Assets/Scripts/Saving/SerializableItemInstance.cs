using System;
using UnityEngine;

[Serializable]
public class SerializableItemInstance
{
    public string itemId;
    public float cooldownRemaining;
    public float stunDuration;

    public SerializableItemInstance(string id, float cooldown, float stun)
    {
        itemId = id;
        cooldownRemaining = cooldown;
        stunDuration = stun;
    }
}