using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Data.Effects;
using PirateRoguelike.Services; // Added for SlotId
using PirateRoguelike.Events; // Added for ItemManipulationEvents
using PirateRoguelike.Combat;
using PirateRoguelike.Saving; // Added for SerializableItemInstance

namespace PirateRoguelike.Core
{
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
            var itemData = data.equippedItems[i];
            if (itemData != null)
            {
                ItemSO itemSO = GameDataRegistry.GetItem(itemData.itemId, itemData.rarity);
                if (itemSO != null)
                {
                    Equipped[i] = new ItemInstance(itemSO);
                }
                else
                {
                    Debug.LogWarning($"Could not find ItemSO with ID {itemData.itemId} and Rarity {itemData.rarity} in GameDataRegistry. Treating slot as empty.");
                    Equipped[i] = null;
                }
            }
            else
            {
                Equipped[i] = null;
            }
        }

        RecalculateStats(); // Recalculate stats after loading modifiers
    }

    public SerializableShipState ToSerializable()
    {
        List<SerializableItemInstance> equippedSerializable = new List<SerializableItemInstance>();
        foreach (var item in Equipped)
        {
            equippedSerializable.Add(item?.ToSerializable());
        }
        return new SerializableShipState(ShipId, CurrentHealth, CurrentShield, _stunDuration, equippedSerializable, ActiveEffects, _activeStatModifiers);
    }

    public void ApplyEffect(EffectSO effectToApply)
    {
        var existingEffect = ActiveEffects.FirstOrDefault(e => e.Def == effectToApply);
        if (existingEffect != null)
        {
            existingEffect.AddStack();
        }
        else
        {
            ActiveEffects.Add(new ActiveCombatEffect(effectToApply));
        }
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
        ItemManipulationEvents.DispatchItemAdded(item, new SlotId(index, SlotContainerType.Equipment));
    }

    public void SwapEquipment(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= Equipped.Length || indexB < 0 || indexB >= Equipped.Length) 
        {
            Debug.LogWarning($"ShipState.SwapEquipment: Invalid indices. indexA: {indexA}, indexB: {indexB}");
            return;
        }
        ItemInstance itemA_before = Equipped[indexA];
        ItemInstance itemB_before = Equipped[indexB];

        (Equipped[indexA], Equipped[indexB]) = (Equipped[indexB], Equipped[indexA]);

        ItemInstance itemA_after = Equipped[indexA];
        ItemInstance itemB_after = Equipped[indexB];
        ItemManipulationEvents.DispatchItemMoved(Equipped[indexA], new SlotId(indexA, SlotContainerType.Equipment), new SlotId(indexB, SlotContainerType.Equipment));
    }

    public void RemoveEquippedAt(int index)
    {
        if (index >= 0 && index < Equipped.Length)
        {
            ItemInstance item = Equipped[index];
            Equipped[index] = null;
            ItemManipulationEvents.DispatchItemRemoved(item, new SlotId(index, SlotContainerType.Equipment));
        }
        else
        {
            Debug.LogWarning($"ShipState.RemoveEquippedAt: Invalid index {index}");
        }
    }

    public void SetEquippedAt(int index, ItemInstance item)
    {
        if (index >= 0 && index < Equipped.Length)
        {
            Equipped[index] = item;
            ItemManipulationEvents.DispatchItemAdded(item, new SlotId(index, SlotContainerType.Equipment));
        }
        else
        {
            Debug.LogWarning($"ShipState.SetEquippedAt: Invalid index {index}");
        }
    }

    public ItemInstance GetItemById(string id)
    {
        return Equipped.FirstOrDefault(item => item != null && item.Def.id == id);
    }

    public void TakeDamage(int amount)
    {
        float incomingDamage = amount;

        // 1. Damage reduction (flat and percentage from modifiers)
        float flatDefense = _activeStatModifiers.Where(m => m.StatType == StatType.Defense && m.ModifierType == StatModifierType.Flat).Sum(m => m.Value);
        float percentDefense = _activeStatModifiers.Where(m => m.StatType == StatType.Defense && m.ModifierType == StatModifierType.Percentage).Sum(m => m.Value);

        incomingDamage -= flatDefense;
        incomingDamage *= (1 - percentDefense); // 1 - 0.1 = 0.9 for 10% reduction

        // Ensure damage doesn't go below zero after reduction
        incomingDamage = Mathf.Max(0, incomingDamage);

        // 2. Shield absorption
        float damageToShield = Mathf.Min(incomingDamage, CurrentShield);
        CurrentShield -= damageToShield;
        incomingDamage -= damageToShield;

        // 3. HP loss
        CurrentHealth -= Mathf.RoundToInt(incomingDamage);
        if (CurrentHealth < 0) CurrentHealth = 0;

        OnHealthChanged?.Invoke();
        EventBus.DispatchDamageReceived(this, amount); // Dispatch original amount for event
    }

    public void Heal(int amount)
    {
        // Healing nullified if HP <= 0 in the same tick
        if (CurrentHealth <= 0)
        {
            //Debug.Log($"{Def.displayName} is at 0 HP. Healing nullified.");
            return;
        }

        CurrentHealth += amount;
        if (CurrentHealth > Def.baseMaxHealth) CurrentHealth = Def.baseMaxHealth;
        OnHealthChanged?.Invoke();
        EventBus.DispatchHeal(this, amount);
    }
}
}