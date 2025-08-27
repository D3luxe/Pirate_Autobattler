using System;
using UnityEngine;

public class EconomyService
{
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnLivesChanged;

    public int Gold { get; private set; }
    public int Lives { get; private set; }

    private readonly RunConfigSO _config;
    private int _rerollsThisShop;
    private bool _freeRerollAvailable = false;

    public EconomyService(RunConfigSO config, RunState runState = null)
    {
        _config = config;
        if (runState != null)
        {
            Gold = runState.gold;
            Lives = runState.playerLives;
            _rerollsThisShop = runState.rerollsThisShop;
        }
        else
        {
            Gold = config.startingGold;
            Lives = config.startingLives;
        }
        // Initial values are not broadcast with events, UI should read them on load.
    }

    public void SaveToRunState(RunState runState)
    {
        runState.gold = Gold;
        runState.playerLives = Lives;
        runState.rerollsThisShop = _rerollsThisShop;
    }

    public void MarkFreeRerollAvailable()
    {
        _freeRerollAvailable = true;
    }

    public void AddGold(int amount)
    {
        Gold = Mathf.Min(Gold + amount, 999);
        OnGoldChanged?.Invoke(Gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }
        return false;
    }

    public void AddLives(int amount)
    {
        Lives += amount;
        OnLivesChanged?.Invoke(Lives);
    }

    public void LoseLife()
    {
        Lives--;
        OnLivesChanged?.Invoke(Lives);
    }

    public int GetCurrentRerollCost()
    {
        if (_freeRerollAvailable && _rerollsThisShop == 0)
        {
            return 0;
        }
        return (int)Mathf.Round(_config.rerollBaseCost * Mathf.Pow(_config.rerollGrowth, _rerollsThisShop));
    }

    public void IncrementRerollCount()
    {
        _rerollsThisShop++;
        _freeRerollAvailable = false; // Free reroll used
    }

    public void ResetRerollCount()
    {
        _rerollsThisShop = 0;
        _freeRerollAvailable = false; // Reset for next shop
    }
}
