using System;

namespace PirateRoguelike.Core
{
    public interface ITickService
{
    event Action OnTick;
    float IntervalSec { get; }
    void StartTicking();
    void Stop();
}
}