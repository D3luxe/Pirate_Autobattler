using PirateRoguelike.Data;

namespace PirateRoguelike.Combat
{
    [System.Serializable]
    public class StatModifier
{
    public StatType StatType;
    public StatModifierType ModifierType;
    public float Value;

    public StatModifier(StatType statType, StatModifierType modifierType, float value)
    {
        StatType = statType;
        ModifierType = modifierType;
        Value = value;
    }
}
}