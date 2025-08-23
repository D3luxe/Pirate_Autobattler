using UnityEngine;
using PirateRoguelike.Data;

public class BattleManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private CombatController combatController;
    [SerializeField] private TickService tickService;
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private Transform playerShipSpawnPoint;
    [SerializeField] private Transform enemyShipSpawnPoint;
    [SerializeField] private GameObject shipViewPrefab;
    [SerializeField] private RectTransform uiParentForShips; // Add this: A RectTransform on the Canvas

    void Start()
    {
        if (GameSession.CurrentRunState == null)
        {
            Debug.LogError("Cannot start battle: GameSession is not active. You can start a debug battle from the Run scene.");
            return;
        }

        // Hide InventoryUI or show equipped only when entering battle
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.SetInventoryVisibility(false, true);
            InventoryUI.Instance.SetEquipmentInteractability(false);
        }

        SetupBattle();
    }

    private void SetupBattle()
    {
        string encounterId = GameSession.CurrentRunState.currentEncounterId;
        EncounterSO encounterData = GameDataRegistry.GetEncounter(encounterId);

        if (encounterData == null || (encounterData.type != EncounterType.Battle && encounterData.type != EncounterType.Boss))
        {
            Debug.LogError($"Invalid or non-battle encounter data found for this scene: {encounterId}");
            return;
        }

        ShipState playerState = GameSession.PlayerShip;
        

        // 3. Build the Enemy Ship state from the encounter data
        // Check if there are enemies defined for this encounter
        if (encounterData.enemies == null || encounterData.enemies.Count == 0)
        {
            Debug.LogError($"Battle encounter '{encounterId}' has no enemies defined! Please assign EnemySOs to its 'Enemies' list.");
            return; // Prevent crash
        }

        EnemySO enemyDefinition = encounterData.enemies[0];
        ShipSO enemyShipSO = GameDataRegistry.GetShip(enemyDefinition.shipId);
        if (enemyShipSO == null)
        {
            Debug.LogError($"Could not find ship with ID: {enemyDefinition.shipId} for enemy {enemyDefinition.name}");
            return;
        }
        ShipState enemyState = new ShipState(enemyShipSO);
        

        // Instantiate visual Ship prefabs for player and enemy as children of the UI Canvas
        GameObject playerShipGO = Instantiate(shipViewPrefab, uiParentForShips);
        playerShipGO.GetComponent<RectTransform>().anchoredPosition = playerShipSpawnPoint.localPosition;
        ShipView playerShipView = playerShipGO.GetComponent<ShipView>();
        playerShipView.Initialize(playerState);

        GameObject enemyShipGO = Instantiate(shipViewPrefab, uiParentForShips);
        enemyShipGO.GetComponent<RectTransform>().anchoredPosition = enemyShipSpawnPoint.localPosition;
        ShipView enemyShipView = enemyShipGO.GetComponent<ShipView>();
        enemyShipView.Initialize(enemyState);

        // Initialize the BattleUIController
        battleUIController.Initialize(playerShipView, enemyShipView, InventoryUI.Instance);

        combatController.Init(playerState, enemyDefinition, tickService, battleUIController, playerShipView, enemyShipView);

        Debug.Log($"Battle started: {playerState.Def.displayName} vs {enemyState.Def.displayName}");
    }
}
