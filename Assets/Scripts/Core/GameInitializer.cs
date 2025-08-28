using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;

public class GameInitializer : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject runManagerPrefab;
    [SerializeField] private RunConfigSO runConfig;
    [SerializeField] private ShipSO debugStartingShip; // Add this field

    void Start()
    {
        // Initialize core systems
        AbilityManager.Initialize();

        // Instantiate persistent UI and managers
        if (runManagerPrefab != null && RunManager.Instance == null)
        {
            Instantiate(runManagerPrefab);
        }

        // Determine whether to load a saved game or start a new one
        if (GameSession.ShouldLoadSavedGame)
        {
            if (GameSession.SavedRunStateToLoad != null)
            {
                if (runConfig == null)
                {
                    Debug.LogError("GameInitializer: RunConfigSO is not assigned. Cannot load saved game.");
                    return;
                }
                GameSession.LoadRun(GameSession.SavedRunStateToLoad, runConfig);
                GameSession.ShouldLoadSavedGame = false; // Reset flag
                GameSession.SavedRunStateToLoad = null; // Clear saved state
                SceneManager.LoadScene("Run");
            }
            else
            {
                Debug.LogError("GameInitializer: ShouldLoadSavedGame is true but SavedRunStateToLoad is null.");
                // Fallback to starting a new game if saved state is missing
                SceneManager.LoadScene("Run");
            }
        }
        else
        {
            // Start a new game
            if (runConfig == null || debugStartingShip == null)
            {
                Debug.LogError("GameInitializer: RunConfig or DebugStartingShip is not assigned for new game!");
                return;
            }
            GameSession.StartNewRun(runConfig, debugStartingShip);
            SceneManager.LoadScene("Run");
        }
    }

    private void OnApplicationQuit()
    {
        // Ensure systems are shut down properly
        AbilityManager.Shutdown();
    }
}
