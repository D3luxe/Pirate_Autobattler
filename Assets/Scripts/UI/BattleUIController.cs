using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("Ship Views")]
    [SerializeField] private ShipView playerShipView;
    [SerializeField] private ShipView enemyShipView;

    [Header("Player HUD")]
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI playerLivesText;

    [Header("Enemy HUD")]
    [SerializeField] private TextMeshProUGUI enemyHealthText;

    [Header("Battle Log")]
    [SerializeField] private TextMeshProUGUI battleLogText;
    [SerializeField] private ScrollRect battleLogScrollRect;

    [Header("Controls")]
    [SerializeField] private Button speedToggleButton;

    private InventoryUI _inventoryUI;
    private float _currentSpeed = 1f; // Default speed

    public void Initialize(ShipView playerView, ShipView enemyView, InventoryUI inventoryUI)
    {
        playerShipView = playerView;
        enemyShipView = enemyView;
        _inventoryUI = inventoryUI;

        // Initial UI update
        UpdatePlayerHUD();
        UpdateEnemyHUD();
        ClearBattleLog();

        // Subscribe to events for real-time updates
        EventBus.OnSuddenDeathStarted += HandleSuddenDeathStarted;
        // Health update subscriptions are now managed by CombatController.
        // GameSession.Economy.OnGoldChanged += UpdatePlayerHUD; // Assuming EconomyService has OnGoldChanged
        // GameSession.Economy.OnLivesChanged += UpdatePlayerHUD; // Assuming EconomyService has OnLivesChanged

        // Speed toggle
        if (speedToggleButton != null)
        {
            speedToggleButton.onClick.AddListener(OnSpeedToggleClicked);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        EventBus.OnSuddenDeathStarted -= HandleSuddenDeathStarted;
        if (speedToggleButton != null)
        {
            speedToggleButton.onClick.RemoveListener(OnSpeedToggleClicked);
        }
    }

    private void HandleSuddenDeathStarted()
    {
        AppendToBattleLog("SUDDEN DEATH HAS BEGUN!");
        // TODO: Add visual/audio effects for sudden death
    }

    public void UpdatePlayerHUD()
    {
        if (playerHealthText != null && GameSession.PlayerShip != null) playerHealthText.text = $"HP: {GameSession.PlayerShip.CurrentHealth}";
        if (playerGoldText != null && GameSession.Economy != null) playerGoldText.text = $"Gold: {GameSession.Economy.Gold}";
        if (playerLivesText != null && GameSession.Economy != null) playerLivesText.text = $"Lives: {GameSession.Economy.Lives}";
    }

    public void UpdateEnemyHUD()
    {
        if (enemyHealthText != null && enemyShipView != null && enemyShipView.ShipState != null) enemyHealthText.text = $"HP: {enemyShipView.ShipState.CurrentHealth}";
    }

    public void AppendToBattleLog(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text += message + "\n";
            // Scroll to bottom
            if (battleLogScrollRect != null) battleLogScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ClearBattleLog()
    {
        if (battleLogText != null) battleLogText.text = "";
    }

    private void OnSpeedToggleClicked()
    {
        if (_currentSpeed == 1f)
        {
            _currentSpeed = 2f;
        }
        else
        {
            _currentSpeed = 1f;
        }
        Time.timeScale = _currentSpeed;
        Debug.Log($"Battle speed set to {_currentSpeed}x");
    }

    // TODO: Add methods to update item cooldowns, etc.
}

