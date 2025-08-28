using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public VisualTreeAsset mainMenuUXML; // Assign your MainMenu.uxml here in the Inspector

    private VisualElement _root;

    void OnEnable()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("MainMenuController: UIDocument component not found on GameObject.");
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("MainMenuController: Root VisualElement is null. Check UXML assignment in UIDocument.");
            return;
        }

        // Assign button callbacks
        Button startGameButton = _root.Q<Button>("StartGame");
        if (startGameButton != null) startGameButton.clicked += OnStartGameClicked;
        else Debug.LogError("MainMenuController: StartGame button not found.");

        Button continueGameButton = _root.Q<Button>("ContinueGame");
        if (continueGameButton != null) continueGameButton.clicked += OnContinueGameClicked;
        else Debug.LogError("MainMenuController: ContinueGame button not found.");

        Button quitGameButton = _root.Q<Button>("QuitGame");
        if (quitGameButton != null) quitGameButton.clicked += OnQuitGameClicked;
        else Debug.LogError("MainMenuController: QuitGame button not found.");

        // Initial state for Continue button
        UpdateButtonStates();
    }

    void OnDisable()
    {
        // Unassign button callbacks to prevent memory leaks
        Button startGameButton = _root.Q<Button>("StartGame");
        if (startGameButton != null) startGameButton.clicked -= OnStartGameClicked;

        Button continueGameButton = _root.Q<Button>("ContinueGame");
        if (continueGameButton != null) continueGameButton.clicked -= OnContinueGameClicked;

        Button quitGameButton = _root.Q<Button>("QuitGame");
        if (quitGameButton != null) quitGameButton.clicked -= OnQuitGameClicked;
    }

    private void UpdateButtonStates()
    {
        Button continueButton = _root.Q<Button>("ContinueGame");
        if (SaveManager.SaveFileExists())
        {
            continueButton.SetEnabled(true);
        } else {
            continueButton.SetEnabled(false);
        }
    }

    private void OnStartGameClicked()
    {
        Debug.Log("Start Game Clicked");
        GameSession.EndRun(); // Reset any previous game state
        SceneManager.LoadScene("Boot"); // Load the scene that initializes RunManager and starts a new game
    }

    private void OnContinueGameClicked()
    {
        Debug.Log("Continue Game Clicked");
        if (SaveManager.SaveFileExists())
        {
            RunState loadedState = SaveManager.LoadRun();
            GameSession.ShouldLoadSavedGame = true;
            GameSession.SavedRunStateToLoad = loadedState;
            SceneManager.LoadScene("Boot"); // Load Boot scene, which will then load the saved game
        }
        else
        {
            Debug.LogWarning("No saved game found to continue.");
        }
    }

    private void OnQuitGameClicked()
    {
        Debug.Log("Quit Game Clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
