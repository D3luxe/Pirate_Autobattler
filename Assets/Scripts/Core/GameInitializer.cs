using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject runManagerPrefab;

    void Start()
    {
        // Initialize core systems
        AbilityManager.Initialize();

        // Instantiate persistent UI and managers
        if (runManagerPrefab != null && RunManager.Instance == null)
        {
            Instantiate(runManagerPrefab);
        }

        // Load the main menu or the first scene of your game
        // For now, we'll load the Run scene directly for testing.
        SceneManager.LoadScene("Run");
    }

    private void OnApplicationQuit()
    {
        // Ensure systems are shut down properly
        AbilityManager.Shutdown();
    }
}
