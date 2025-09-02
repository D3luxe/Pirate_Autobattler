GameDataRegistry initialized. Loaded 7 items, 5 ships, 6 encounters.
UnityEngine.Debug:Log (object)
GameDataRegistry:Initialize () (at Assets/Scripts/Core/GameDataRegistry.cs:21)

DEBUG: 'enc_battle' loaded. Enemies count: 2
UnityEngine.Debug:Log (object)
GameDataRegistry:Initialize () (at Assets/Scripts/Core/GameDataRegistry.cs:26)

Start Game Clicked
UnityEngine.Debug:Log (object)
MainMenuController:OnStartGameClicked () (at Assets/Scripts/UI/MainMenuController.cs:70)
UnityEngine.UIElements.UIElementsRuntimeUtilityNative:UpdatePanels ()

AbilityManager initialized.
UnityEngine.Debug:Log (object)
AbilityManager:Initialize () (at Assets/Scripts/Core/AbilityManager.cs:19)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:17)

Starting a new run. RNG seed: 638924061013491250
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:70)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: starterItem1 (cannon_wood) is null: False
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:84)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: AddItem(cannon_wood) success: True
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:85)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: starterItem2 (deckhand) is null: False
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:87)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: AddItem(deckhand) success: True
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:88)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: Inventory.Slots.Count after adding: 10
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:89)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: Inventory item ID: item_cannon_wood
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:92)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: Inventory item ID: item_deckhand
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:92)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

GameSession: PlayerShip.Equipped.Length: 4
UnityEngine.Debug:Log (object)
GameSession:StartNewRun (RunConfigSO,PirateRoguelike.Data.ShipSO) (at Assets/Scripts/Core/GameSession.cs:96)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:56)

DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.
UnityEngine.Object:DontDestroyOnLoad (UnityEngine.Object)
MapManager:Awake () (at Assets/Scripts/Map/MapManager.cs:50)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Transform)
RunManager:Initialize () (at Assets/Scripts/Core/RunManager.cs:39)
GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:63)

Map generated successfully!
UnityEngine.Debug:Log (object)
MapManager:GenerateMapData (ulong) (at Assets/Scripts/Map/MapManager.cs:84)
MapManager:GenerateMapIfNeeded (ulong) (at Assets/Scripts/Map/MapManager.cs:61)
RunManager:OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:165)
RunManager:OnSceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode) (at Assets/Scripts/Core/RunManager.cs:150)
UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode)

Run scene initialized. Gold: 10, Lives: 4
UnityEngine.Debug:Log (object)
RunManager:OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:179)
RunManager:OnSceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode) (at Assets/Scripts/Core/RunManager.cs:150)
UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode)

MapView instance is null in OnRunSceneLoaded!
UnityEngine.Debug:LogError (object)
UIManager:InitializeRunUI () (at Assets/Scripts/UI/UIManager.cs:127)
RunManager:OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:189)
RunManager:OnSceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode) (at Assets/Scripts/Core/RunManager.cs:150)
UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode)

