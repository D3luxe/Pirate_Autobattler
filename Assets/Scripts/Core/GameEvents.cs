using System;

namespace PirateRoguelike.Events
{
    public static class GameEvents
{
    public static event Action OnMapToggleRequested;

    public static void RequestMapToggle()
    {
        OnMapToggleRequested?.Invoke();
    }
}
}