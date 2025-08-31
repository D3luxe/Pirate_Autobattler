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

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    [SerializeField] private VisualTreeAsset _tooltipUxml;
    private VisualElement _tooltipContainer; // Reference to the TemplateContainer
    private VisualElement _tooltipPanelRoot; // Reference to the actual tooltip panel root
    private Label _titleLabel;
    private Label _timerText;
    private VisualElement _center;
    private VisualElement _footer;

    private Coroutine _currentTooltipCoroutine; // NEW: Field to hold the current coroutine
    public bool IsTooltipVisible { get; private set; } // NEW: Flag to track tooltip visibility


    [SerializeField] private VisualTreeAsset _activeEffectUxml;
    [SerializeField] private VisualTreeAsset _passiveEffectUxml;

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
        // Instantiate the tooltip from the UXML asset
        _tooltipContainer = _tooltipUxml.Instantiate();
        _tooltipPanelRoot = _tooltipContainer.Q<VisualElement>("Tooltip"); // Query for the actual tooltip panel root
        _tooltipContainer.style.position = Position.Absolute; // Force absolute positioning
        mainUIRoot.Add(_tooltipContainer); // Add to the main UI root

        // Query for the main elements from the instantiated tooltip
                _titleLabel = _tooltipPanelRoot.Q<Label>("TitleLabel");
        _timerText = _tooltipPanelRoot.Q<Label>("TimerText");
        _center = _tooltipPanelRoot.Q<VisualElement>("Center");
        _footer = _tooltipPanelRoot.Q<VisualElement>("Footer");

        // Explicitly set initial position and hide
        _tooltipContainer.style.left = 0;
        _tooltipContainer.style.top = 0;
        _tooltipPanelRoot.AddToClassList("tooltip--hidden");
    }

    public void Show(RuntimeItem runtimeItem, VisualElement targetElement)
    {
        if (_tooltipContainer == null || _tooltipPanelRoot == null) return; // Null check

        if (_currentTooltipCoroutine != null) // NEW: Stop existing coroutine
        {
            StopCoroutine(_currentTooltipCoroutine);
        }
        _currentTooltipCoroutine = StartCoroutine(ShowCoroutine(runtimeItem, targetElement)); // NEW: Assign coroutine
        IsTooltipVisible = true; // NEW: Set flag to true
    }

    private IEnumerator ShowCoroutine(RuntimeItem runtimeItem, VisualElement targetElement)
    {
        //Debug.Log($"TooltipController.Show() called for item: {runtimeItem.DisplayName}");
        _tooltipPanelRoot.style.visibility = Visibility.Visible;
        _titleLabel.text = runtimeItem.DisplayName;
        _timerText.text = runtimeItem.CooldownSec > 0 ? $"{runtimeItem.CooldownSec}s" : "";

        _center.Clear();
        _footer.Clear();

        // For now, we'll use a dummy context. This will need to be properly passed from the game state.
        IRuntimeContext dummyContext = new DummyRuntimeContext(); 

        foreach (var runtimeAbility in runtimeItem.Abilities)
        {
            // Assuming 'IsActive' on RuntimeItem determines if it's an active or passive item.
            // This logic might need refinement based on actual game design.
            if (runtimeItem.IsActive)
            {
                var newEffect = _activeEffectUxml.Instantiate();
                var effectDisplay = new EffectDisplay(newEffect);
                effectDisplay.SetData(runtimeAbility, dummyContext); 
                _center.Add(newEffect);
            }
            else
            {
                var newEffect = _passiveEffectUxml.Instantiate();
                var effectDisplay = new EffectDisplay(newEffect);
                effectDisplay.SetData(runtimeAbility, dummyContext); 
                _footer.Add(newEffect);
            }
        }

        // Remove and re-add to ensure it's the last child (highest z-order)
        // We need a reference to the root VisualElement of the main UI Document
        // This should be passed in during initialization.
        // For now, let's assume _tooltipContainer.parent is the correct root if it exists.
        // If _tooltipContainer.parent is null, it means it hasn't been added to any hierarchy yet.
        VisualElement currentRoot = _tooltipContainer.parent;
        if (currentRoot != null)
        {
            _tooltipContainer.RemoveFromHierarchy();
            currentRoot.Add(_tooltipContainer);
        }

        // Positioning and animation
        _tooltipContainer.style.left = targetElement.worldBound.xMax;
        _tooltipContainer.style.top = targetElement.worldBound.y;
        //Debug.Log($"Tooltip position set to: X={_tooltipContainer.style.left.value.ToString()}, Y={_tooltipContainer.style.top.value.ToString()}");
        
        _tooltipPanelRoot.RemoveFromClassList("tooltip--hidden");
        yield return null; // Wait for one frame
        _tooltipPanelRoot.AddToClassList("tooltip--visible");
        
        _currentTooltipCoroutine = null; // NEW: Clear coroutine reference
    }

    public void Hide()
    {
        if (_tooltipContainer == null || _tooltipPanelRoot == null) return; // Null check
        if (!IsTooltipVisible) return; // NEW: Only hide if currently visible

        if (_currentTooltipCoroutine != null) // NEW: Stop existing coroutine
        {
            StopCoroutine(_currentTooltipCoroutine);
        }
        _currentTooltipCoroutine = StartCoroutine(HideCoroutine()); // NEW: Assign coroutine
        IsTooltipVisible = false; // NEW: Set flag to false
    }

    private IEnumerator HideCoroutine()
    {
        //Debug.Log("TooltipController.Hide() called.");
        _tooltipPanelRoot.RemoveFromClassList("tooltip--visible");
        _tooltipPanelRoot.AddToClassList("tooltip--hidden");

        // Wait for the opacity transition to complete (0.2 seconds as per USS)
        yield return new WaitForSeconds(0.2f); 

        // Now set visibility to hidden
        _tooltipPanelRoot.style.visibility = Visibility.Hidden;
        
        _currentTooltipCoroutine = null; // NEW: Clear coroutine reference
    }
}