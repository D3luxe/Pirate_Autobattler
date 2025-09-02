using System;
using UnityEngine;

namespace PirateRoguelike.Core
{
    public class TickService : MonoBehaviour, ITickService
{
    public event Action OnTick;
    public float IntervalSec => 0.1f; // 100ms

    private Coroutine _tickCoroutine;

    public void StartTicking()
    {
        Debug.Log("TickService: StartTicking called.");
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
        }
        _tickCoroutine = StartCoroutine(TickCoroutine());
    }

    public void Stop()
    {
        Debug.Log("TickService: Stop called.");
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }
    }

    private System.Collections.IEnumerator TickCoroutine()
    {
        Debug.Log("TickService: TickCoroutine started.");
        var wait = new WaitForSeconds(IntervalSec);
        while (true)
        {
            yield return wait;
            OnTick?.Invoke();
            //Debug.Log("TickService: Tick!");
        }
    }

    // Example of controlling speed
    public void SetSpeed(float multiplier)
    {
        Time.timeScale = multiplier;
    }
}
}