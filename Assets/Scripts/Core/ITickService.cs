using System;

public interface ITickService
{
    event Action OnTick;
    float IntervalSec { get; }
    void Start();
    void Stop();
}