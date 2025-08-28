using System;

public static class GameEvents
{
    public static event Action OnMapToggleRequested;

    public static void RequestMapToggle()
    {
        OnMapToggleRequested?.Invoke();
    }
}