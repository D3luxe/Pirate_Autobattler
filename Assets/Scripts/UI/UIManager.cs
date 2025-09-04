using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PirateRoguelike.Core;

namespace PirateRoguelike.UI
{
    public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Prefabs")]
    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private GameObject mapViewPrefab;
    [SerializeField] private GameObject tooltipManagerPrefab;
    [SerializeField] private GameObject globalUIOverlayPrefab;
    [SerializeField] private GameObject debugConsolePrefab; // Added for Debug Console

    [Header("UXML Assets")]
    [SerializeField] private VisualTreeAsset _shipDisplayElementUXML;
    [SerializeField] private VisualTreeAsset _slotElementUXML;
    [SerializeField] private VisualTreeAsset _itemElementUXML; // Added for ItemElement

    public VisualTreeAsset ShipDisplayElementUXML => _shipDisplayElementUXML;
    public VisualTreeAsset SlotElementUXML => _slotElementUXML;
    public VisualTreeAsset ItemElementUXML => _itemElementUXML; // Added for ItemElement


    private PlayerPanelController _playerPanelController;
    private MapView _mapView;
    private TooltipController _tooltipController;
    private UIDocument _globalUIOverlayDocument;

    public VisualElement GlobalUIRoot => _globalUIOverlayDocument?.rootVisualElement;

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

    public void Initialize()
    {
        InstantiateUI();
    }

    private void InstantiateUI()
    {
        // Instantiate Global UI Overlay
        if (globalUIOverlayPrefab != null)
        {
            GameObject globalUIInstance = Instantiate(globalUIOverlayPrefab, transform);
            _globalUIOverlayDocument = globalUIInstance.GetComponent<UIDocument>();
            if (_globalUIOverlayDocument == null)
            {
                Debug.LogError("GlobalUIOverlay Prefab does not have a UIDocument component!");
            }
        }
        else
        {
            Debug.LogError("GlobalUIOverlay Prefab is not assigned in UIManager!");
        }

        // Instantiate PlayerPanel UI
        if (playerPanelPrefab != null)
        {
            GameObject panelInstance = Instantiate(playerPanelPrefab, transform);
            _playerPanelController = panelInstance.GetComponent<PlayerPanelController>();
        }
        else
        {
            Debug.LogError("PlayerPanel Prefab is not assigned in UIManager!");
        }

        // Instantiate MapView UI from prefab
        if (mapViewPrefab != null)
        {
            GameObject mapViewInstance = Instantiate(mapViewPrefab, transform);
            _mapView = mapViewInstance.GetComponent<MapView>();
        }
        else
        {
            Debug.LogError("MapView Prefab is not assigned in UIManager!");
        }

        // Instantiate TooltipManager UI
        if (tooltipManagerPrefab != null)
        {
            GameObject tooltipInstance = Instantiate(tooltipManagerPrefab, transform);
            _tooltipController = tooltipInstance.GetComponent<TooltipController>();
        }
        else
        {
            Debug.LogError("TooltipManager Prefab is not assigned in UIManager!");
        }

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

        // Instantiate Debug Console
        if (debugConsolePrefab != null)
        {
            GameObject debugConsoleInstance = Instantiate(debugConsolePrefab, transform);
            DebugConsoleController debugConsoleController = debugConsoleInstance.GetComponent<DebugConsoleController>();
            if (debugConsoleController != null && GlobalUIRoot != null)
            {
                debugConsoleController.Initialize(GlobalUIRoot);
            }
            else
            {
                Debug.LogError("DebugConsoleController or GlobalUIRoot is null. Cannot initialize debug console.");
            }
        }
        else
        {
            Debug.LogError("DebugConsole Prefab is not assigned in UIManager!");
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