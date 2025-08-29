using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this

public class ShipStateView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image shipSprite;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TextMeshProUGUI healthBarText; // Change this

    public ShipState ShipState { get; private set; }

    public void Initialize(ShipState state)
    {
        ShipState = state;
        ShipState.OnHealthChanged += OnHealthChanged; // Subscribe to health changes
        EventBus.OnDamageReceived += HandleDamageReceived;
        EventBus.OnHeal += HandleHeal;
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (ShipState != null)
        {
            ShipState.OnHealthChanged -= OnHealthChanged; // Unsubscribe to prevent memory leaks
        }
        EventBus.OnDamageReceived -= HandleDamageReceived;
        EventBus.OnHeal -= HandleHeal;
    }

    private void UpdateVisuals()
    {
        if (ShipState == null) return;

        // Update sprite (if art is available on ShipSO)
        if (shipSprite != null && ShipState.Def.art != null)
        {
            shipSprite.sprite = ShipState.Def.art;
        }

        // Update health bar
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = ShipState.Def.baseMaxHealth;
            healthBarSlider.value = ShipState.CurrentHealth;
        }
        if (healthBarText != null)
        {
            healthBarText.text = $"{ShipState.CurrentHealth}/{ShipState.Def.baseMaxHealth}";
        }
    }

    // Call this when ShipState's health changes
    public void OnHealthChanged()
    {
        UpdateVisuals();
    }

    private void HandleDamageReceived(ShipState target, float amount)
    {
        if (target == ShipState)
        {
            ShowDamageText(amount);
        }
    }

    private void HandleHeal(ShipState target, float amount)
    {
        if (target == ShipState)
        {
            ShowHealText(amount);
        }
    }

    public void ShowDamageText(float amount)
    {
        Debug.Log($"Displaying -{amount} damage text on {ShipState.Def.displayName}");
        // TODO: Implement actual floating text UI
    }

    public void ShowHealText(float amount)
    {
        Debug.Log($"Displaying +{amount} heal text on {ShipState.Def.displayName}");
        // TODO: Implement actual floating text UI
    }
}
