using UnityEngine;

public class EconomyService
{
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
            // _freeRerollAvailable is not saved, it's transient per shop visit
        }
        else
        {
            Gold = config.startingGold;
            Lives = config.startingLives;
            _rerollsThisShop = 0;
        }
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
    }
    public bool TrySpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            return true;
        }
        return false;
    }

    public void AddLives(int amount) => Lives += amount;
    public void LoseLife() => Lives--;

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
