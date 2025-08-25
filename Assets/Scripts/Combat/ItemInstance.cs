using PirateRoguelike.Data;

public class ItemInstance
{
    public ItemSO Def { get; }
    public float CooldownRemaining { get; set; } // Cooldown can now be modified by abilities

    public ItemInstance(ItemSO definition)
    {
        Def = definition;
        CooldownRemaining = Def.cooldownSec; // Start on cooldown
    }

    // Constructor for loading from save data
    public ItemInstance(SerializableItemInstance data)
    {
        Def = GameDataRegistry.GetItem(data.itemId);
        CooldownRemaining = data.cooldownRemaining;
    }

    public SerializableItemInstance ToSerializable()
    {
        // The stun duration is now an effect, so it's not saved here.
        return new SerializableItemInstance(Def.id, CooldownRemaining, 0); 
    }
}
