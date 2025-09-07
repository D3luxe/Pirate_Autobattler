using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PirateRoguelike.Core;

namespace PirateRoguelike.UI
{
    public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private PlayerPanelController _playerPanelController;
    private MapView _mapView;
    private TooltipController _tooltipController;
    private UIDocument _globalUIOverlayDocument;
    private DebugConsoleController _debugConsoleController;
    private RewardUIController _rewardUIController;

    public RewardUIController RewardUIController => _rewardUIController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(PlayerPanelController playerPanelController, MapView mapView, TooltipController tooltipController, UIDocument globalUIOverlayDocument, DebugConsoleController debugConsoleController, RewardUIController rewardUIController)
    {
        _playerPanelController = playerPanelController;
        _mapView = mapView;
        _tooltipController = tooltipController;
        _globalUIOverlayDocument = globalUIOverlayDocument;
        _debugConsoleController = debugConsoleController;
        _rewardUIController = rewardUIController;

        // Initial hookups
        if (_playerPanelController != null && _mapView != null)
        {
            _playerPanelController.SetMapPanel(_mapView);
        }

        if (_tooltipController != null && _globalUIOverlayDocument != null)
        {
            _tooltipController.Initialize(_globalUIOverlayDocument.rootVisualElement);
        }
        else
        {
            Debug.LogError("TooltipController or Global UI Overlay Document is null. Cannot initialize tooltip.");
        }

        if (_debugConsoleController != null && _globalUIOverlayDocument != null)
        {
            _debugConsoleController.Initialize(_globalUIOverlayDocument.rootVisualElement);
        }
        else
        {
            Debug.LogError("DebugConsoleController or GlobalUIRoot is null. Cannot initialize debug console.");
        }

        if (_rewardUIController != null && _globalUIOverlayDocument != null)
        {
            _globalUIOverlayDocument.rootVisualElement.Add(_rewardUIController.GetComponent<UIDocument>().rootVisualElement);
        }
        else
        {
            Debug.LogError("RewardUIController or GlobalUIRoot is null. Cannot initialize reward UI.");
        }
    }

    public void InitializeRunUI()
    {
        // Initialize the Player Panel with the current game session
        if (_playerPanelController != null)
        {
            _playerPanelController.Initialize(new GameSessionWrapper());
        }

        // Show the map view
        if (_mapView != null)
        {
            _mapView.Show();
        }
        else
        {
            Debug.LogError("MapView instance is null in OnRunSceneLoaded!");
                }
    }
}}