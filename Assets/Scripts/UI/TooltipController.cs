using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using PirateRoguelike.Runtime; // Added for RuntimeItem and RuntimeAbility
using PirateRoguelike.Combat; // Added for IRuntimeContext
using System.Collections; // Added for IEnumerator

// Temporary dummy implementation for IRuntimeContext
public class DummyRuntimeContext : IRuntimeContext
{
    // Empty implementation
}

namespace PirateRoguelike.UI
{
    public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    [SerializeField] private VisualTreeAsset _tooltipUxml;
    [SerializeField] private StyleSheet _tooltipStyleSheet; // The USS file for the tooltip
    [SerializeField] private VisualTreeAsset _activeEffectUxml;
    [SerializeField] private VisualTreeAsset _passiveEffectUxml;

    // Dictionary to hold a tooltip instance for each panel root
    private Dictionary<VisualElement, VisualElement> _tooltipInstances = new Dictionary<VisualElement, VisualElement>();
    private VisualElement _activeTooltipContainer; // The currently visible tooltip

    private Coroutine _currentTooltipCoroutine;
    public bool IsTooltipVisible { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Initialize(VisualElement mainUIRoot)
    {
        GetOrCreateTooltipForPanel(mainUIRoot);
    }

    private VisualElement GetOrCreateTooltipForPanel(VisualElement panelRoot)
    {
        if (!_tooltipInstances.TryGetValue(panelRoot, out var tooltipContainer))
        {
            tooltipContainer = _tooltipUxml.Instantiate();
            tooltipContainer.style.position = Position.Absolute;
            
            // Add the stylesheet to the panel if it doesn't already have it
            if (!panelRoot.styleSheets.Contains(_tooltipStyleSheet))
            {
                panelRoot.styleSheets.Add(_tooltipStyleSheet);
            }

            panelRoot.Add(tooltipContainer);
            _tooltipInstances[panelRoot] = tooltipContainer;
        }
        return tooltipContainer;
    }

    public void Show(RuntimeItem runtimeItem, VisualElement targetElement, VisualElement panelRoot)
    {
        if (panelRoot == null) return;

        if (_currentTooltipCoroutine != null)
        {
            StopCoroutine(_currentTooltipCoroutine);
        }
        _currentTooltipCoroutine = StartCoroutine(ShowCoroutine(runtimeItem, targetElement, panelRoot));
        IsTooltipVisible = true;
    }

    private IEnumerator ShowCoroutine(RuntimeItem runtimeItem, VisualElement targetElement, VisualElement panelRoot)
    {
        _activeTooltipContainer = GetOrCreateTooltipForPanel(panelRoot);
        var tooltipPanelRoot = _activeTooltipContainer.Q<VisualElement>("Tooltip");

        tooltipPanelRoot.style.visibility = Visibility.Visible;
        
        // Query elements from the active tooltip instance
        var titleLabel = _activeTooltipContainer.Q<Label>("TitleLabel");
        var timerText = _activeTooltipContainer.Q<Label>("TimerText");
        var center = _activeTooltipContainer.Q<VisualElement>("Center");
        var footer = _activeTooltipContainer.Q<VisualElement>("Footer");

        titleLabel.text = runtimeItem.DisplayName;
        timerText.text = runtimeItem.CooldownSec > 0 ? $"{runtimeItem.CooldownSec}s" : "";

        center.Clear();
        footer.Clear();

        IRuntimeContext dummyContext = new DummyRuntimeContext();

        foreach (var runtimeAbility in runtimeItem.Abilities)
        {
            if (runtimeItem.IsActive)
            {
                var newEffect = _activeEffectUxml.Instantiate();
                new EffectDisplay(newEffect).SetData(runtimeAbility, dummyContext);
                center.Add(newEffect);
            }
            else
            {
                var newEffect = _passiveEffectUxml.Instantiate();
                new EffectDisplay(newEffect).SetData(runtimeAbility, dummyContext);
                footer.Add(newEffect);
            }
        }

        _activeTooltipContainer.style.left = targetElement.worldBound.xMax;
        _activeTooltipContainer.style.top = targetElement.worldBound.y;

        tooltipPanelRoot.RemoveFromClassList("tooltip--hidden");
        yield return null; // Wait for one frame
        tooltipPanelRoot.AddToClassList("tooltip--visible");

        _currentTooltipCoroutine = null;
    }

    public void Hide()
    {
        if (!IsTooltipVisible || _activeTooltipContainer == null) return;

        if (_currentTooltipCoroutine != null)
        {
            StopCoroutine(_currentTooltipCoroutine);
        }
        _currentTooltipCoroutine = StartCoroutine(HideCoroutine());
        IsTooltipVisible = false;
    }

    private IEnumerator HideCoroutine()
    {
        var tooltipPanelRoot = _activeTooltipContainer.Q<VisualElement>("Tooltip");
        tooltipPanelRoot.RemoveFromClassList("tooltip--visible");
        tooltipPanelRoot.AddToClassList("tooltip--hidden");

        yield return new WaitForSeconds(0.2f);

        tooltipPanelRoot.style.visibility = Visibility.Hidden;
        _activeTooltipContainer = null; // Clear the active tooltip reference
        _currentTooltipCoroutine = null;
    }
}
}
