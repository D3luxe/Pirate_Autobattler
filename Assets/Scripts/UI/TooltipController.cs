
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    [SerializeField] private VisualTreeAsset _tooltipUxml; // Re-added serialized field
    private VisualElement _tooltip;
    private Label _titleLabel;
    private Label _timerText;
    private VisualElement _center;
    private VisualElement _footer;

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
        _tooltip = _tooltipUxml.Instantiate();
        _tooltip.style.position = Position.Absolute; // Force absolute positioning
        mainUIRoot.Add(_tooltip); // Add to the main UI root

        // Query for the main elements from the instantiated tooltip
        _titleLabel = _tooltip.Q<Label>("TitleLabel");
        _timerText = _tooltip.Q<Label>("TimerText");
        _center = _tooltip.Q<VisualElement>("Center");
        _footer = _tooltip.Q<VisualElement>("Footer");

        // Explicitly set initial position and hide
        _tooltip.style.left = 0;
        _tooltip.style.top = 0;
        _tooltip.style.display = DisplayStyle.None;
        _tooltip.RemoveFromClassList("tooltip--visible");
    }

    public void Show(ItemSO item, VisualElement targetElement)
    {
        if (_tooltip == null) return; // Null check
        Debug.Log($"TooltipController.Show() called for item: {item.displayName}");
        _titleLabel.text = item.displayName;
        _timerText.text = item.cooldownSec > 0 ? $"{item.cooldownSec}s" : "";

        _center.Clear();
        _footer.Clear();

        foreach (var ability in item.abilities)
        {
            // Assuming 'isActive' on ItemSO determines if it's an active or passive item.
            // And 'trigger' on AbilitySO determines if it's an active or passive ability.
            // This logic might need refinement based on actual game design.
            if (item.isActive)
            {
                var newEffect = _activeEffectUxml.Instantiate();
                var effectDisplay = new EffectDisplay(newEffect);
                effectDisplay.SetData(ability);
                _center.Add(newEffect);
            }
            else
            {
                var newEffect = _passiveEffectUxml.Instantiate();
                var effectDisplay = new EffectDisplay(newEffect);
                effectDisplay.SetData(ability);
                _footer.Add(newEffect);
            }
        }

        _tooltip.style.display = DisplayStyle.Flex;
        _tooltip.style.backgroundColor = new StyleColor(Color.red); // TEMPORARY: For debugging visibility
        Debug.Log("Preparing to remove and readd tooltip");
        // Remove and re-add to ensure it's the last child (highest z-order)
        // We need a reference to the root VisualElement of the main UI Document
        // This should be passed in during initialization.
        // For now, let's assume _tooltip.parent is the correct root if it exists.
        // If _tooltip.parent is null, it means it hasn't been added to any hierarchy yet.
        VisualElement currentRoot = _tooltip.parent;
        if (currentRoot != null)
        {
            _tooltip.RemoveFromHierarchy();
            currentRoot.Add(_tooltip);
        }

        // Positioning and animation
        _tooltip.style.left = targetElement.worldBound.xMax;
        _tooltip.style.top = targetElement.worldBound.y;
        Debug.Log($"Tooltip position set to: X={_tooltip.style.left.value.ToString()}, Y={_tooltip.style.top.value.ToString()}");
        _tooltip.AddToClassList("tooltip--visible");
    }

    public void Hide()
    {
        if (_tooltip == null) return; // Null check
        Debug.Log("TooltipController.Hide() called.");
        _tooltip.RemoveFromClassList("tooltip--visible");
        _tooltip.style.display = DisplayStyle.None;
    }
}
