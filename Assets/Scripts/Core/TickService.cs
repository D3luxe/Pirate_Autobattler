using System;
using UnityEngine;

public class TickService : MonoBehaviour, ITickService
{
    public event Action OnTick;
    public float IntervalSec => 0.1f; // 100ms

    private Coroutine _tickCoroutine;

    public void Start()
    {
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
        }
        _tickCoroutine = StartCoroutine(TickCoroutine());
    }

    public void Stop()
    {
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }
    }

    private System.Collections.IEnumerator TickCoroutine()
    {
        var wait = new WaitForSeconds(IntervalSec);
        while (true)
        {
            yield return wait;
            OnTick?.Invoke();
        }
    }

    // Example of controlling speed
    public void SetSpeed(float multiplier)
    {
        Time.timeScale = multiplier;
    }
}
