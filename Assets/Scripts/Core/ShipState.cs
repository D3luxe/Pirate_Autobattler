using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Combat;
using PirateRoguelike.Data;

public class ShipState
{
    public ShipSO Def { get; }
    public string ShipId { get; private set; }
    public int CurrentHealth { get; private set; }
    public float CurrentShield { get; private set; }
    public List<ActiveCombatEffect> ActiveEffects { get; private set; }
    public ItemInstance[] Equipped { get; set; }

    private float _stunDuration; // Stun duration for the ship
    public bool IsStunned => _stunDuration > 0;

    private List<StatModifier> _activeStatModifiers; // List of active stat modifiers

    public event Action OnHealthChanged;

    public ShipState(ShipSO definition)
    {
        Def = definition;
        ShipId = definition.id;
        CurrentHealth = Def.baseMaxHealth;
        CurrentShield = 0; // Initialize shield
        ActiveEffects = new List<ActiveCombatEffect>(); // Initialize active effects
        Equipped = new ItemInstance[Def.baseItemSlots];
        _stunDuration = 0; // Initialize stun duration
        _activeStatModifiers = new List<StatModifier>(); // Initialize stat modifiers

        foreach (var itemRef in Def.builtInItems)
        {
            ItemSO itemSO = GameDataRegistry.GetItem(itemRef.id);
            if (itemSO != null)
            {
                for (int i = 0; i < Equipped.Length; i++)
                {
                    if (Equipped[i] == null)
                    {
                        Equipped[i] = new ItemInstance(itemSO);
                        break;
                    }
                }
            }
        }
    }

    // Constructor for loading from save data
    public ShipState(SerializableShipState data)
    {
        Def = GameDataRegistry.GetShip(data.shipId);
        ShipId = data.shipId;
        CurrentHealth = data.currentHealth;
        CurrentShield = data.currentShield;
        _stunDuration = data.stunDuration;
        ActiveEffects = data.activeEffects; // Directly assign, assuming ActiveCombatEffect is serializable
        _activeStatModifiers = data.activeStatModifiers; // Directly assign, assuming StatModifier is serializable

        Equipped = new ItemInstance[Def.baseItemSlots];
        for (int i = 0; i < data.equippedItems.Count; i++)
        {
            Equipped[i] = new ItemInstance(data.equippedItems[i]);
        }

        RecalculateStats(); // Recalculate stats after loading modifiers
    }

    public SerializableShipState ToSerializable()
    {
        List<SerializableItemInstance> equippedSerializable = new List<SerializableItemInstance>();
        foreach (var item in Equipped)
        {
            if (item != null)
            {
                equippedSerializable.Add(item.ToSerializable());
            }
        }
        return new SerializableShipState(ShipId, CurrentHealth, CurrentShield, _stunDuration, equippedSerializable, ActiveEffects, _activeStatModifiers);
    }

    public void SetCurrentHealth(int amount)
    {
        CurrentHealth = amount;
        OnHealthChanged?.Invoke();
    }

    public void AddShield(float amount)
    {
        CurrentShield += amount;
    }

    public void ApplyStun(float duration)
    {
        _stunDuration = Mathf.Max(_stunDuration, duration); // Reapplying extends duration
    }

    public void ReduceStun(float deltaTime)
    {
        _stunDuration -= deltaTime;
        if (_stunDuration < 0) _stunDuration = 0;
    }

    public void AddStatModifier(StatModifier modifier)
    {
        _activeStatModifiers.Add(modifier);
        RecalculateStats();
    }

    public void RemoveStatModifier(StatModifier modifier)
    {
        _activeStatModifiers.Remove(modifier);
        RecalculateStats();
    }

    private void RecalculateStats()
    {
        // This method now just ensures the _activeStatModifiers list is up-to-date
        // The GetCurrentAttack/Defense methods will do the actual calculation
    }

    public float GetCurrentAttack()
    {
        float baseAttack = 10; // Placeholder base attack, should come from ShipSO or other source
        float totalAttackFlat = _activeStatModifiers.Where(m => m.StatType == StatType.Attack && m.ModifierType == StatModifierType.Flat).Sum(m => m.Value);
        float totalAttackPercent = _activeStatModifiers.Where(m => m.StatType == StatType.Attack && m.ModifierType == StatModifierType.Percentage).Sum(m => m.Value);
        return (baseAttack + totalAttackFlat) * (1 + totalAttackPercent);
    }

    public float GetCurrentDefense()
    {
        float baseDefense = 5; // Placeholder base defense, should come from ShipSO or other source
        float totalDefenseFlat = _activeStatModifiers.Where(m => m.StatType == StatType.Defense && m.ModifierType == StatModifierType.Flat).Sum(m => m.Value);
        float totalDefensePercent = _activeStatModifiers.Where(m => m.StatType == StatType.Defense && m.ModifierType == StatModifierType.Percentage).Sum(m => m.Value);
        return (baseDefense + totalDefenseFlat) * (1 + totalDefensePercent);
    }

    public ItemInstance GetEquippedItem(int index)
    {
        if (index < 0 || index >= Equipped.Length) return null;
        return Equipped[index];
    }

    public void SetEquipment(int index, ItemInstance item)
    {
        if (index < 0 || index >= Equipped.Length) return;
        Equipped[index] = item;
    }

    public void SwapEquipment(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= Equipped.Length || indexB < 0 || indexB >= Equipped.Length) return;
        (Equipped[indexA], Equipped[indexB]) = (Equipped[indexB], Equipped[indexA]);
    }

    public void RemoveEquippedAt(int index)
    {
        if (index >= 0 && index < Equipped.Length)
        {
            Equipped[index] = null;
        }
    }

    public void SetEquippedAt(int index, ItemInstance item)
    {
        if (index >= 0 && index < Equipped.Length)
        {
            Equipped[index] = item;
        }
    }

    public ItemInstance GetItemById(string id)
    {
        return Equipped.FirstOrDefault(item => item != null && item.Def.id == id);
    }

    public void TakeDamage(int amount)
    {
        // Apply defense modifier here
        float finalDamage = amount / (1 + GetCurrentDefense()); // Simple defense reduction
        CurrentHealth -= Mathf.RoundToInt(finalDamage);
        if (CurrentHealth < 0) CurrentHealth = 0;
        OnHealthChanged?.Invoke();
        EventBus.DispatchDamageReceived(this, amount);
    }

    public void Heal(int amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > Def.baseMaxHealth) CurrentHealth = Def.baseMaxHealth;
        OnHealthChanged?.Invoke();
        EventBus.DispatchHeal(this, amount);
    }
}