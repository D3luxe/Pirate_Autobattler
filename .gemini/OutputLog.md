DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.
UnityEngine.Object:DontDestroyOnLoad (UnityEngine.Object)
Pirate.MapGen.MapManager:Awake () (at Assets/Scripts/Map/MapManager.cs:52)
UnityEngine.Object:Instantiate<UnityEngine.GameObject> (UnityEngine.GameObject,UnityEngine.Transform)
PirateRoguelike.Core.RunManager:Initialize () (at Assets/Scripts/Core/RunManager.cs:63)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:77)

UIManager Prefab is not assigned in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:97)

GlobalUIOverlay Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:113)

GlobalUIOverlay Document is null or UIManager is null. Cannot initialize GlobalUIService!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:125)

PlayerPanel Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:137)

MapView Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:149)

TooltipManager Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:161)

DebugConsole Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:173)

RewardUI Prefab is not assigned or UIManager is null in GameInitializer!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:185)

UIManager instance is null. Cannot initialize UIManager!
UnityEngine.Debug:LogError (object)
PirateRoguelike.Core.GameInitializer:Start () (at Assets/Scripts/Core/GameInitializer.cs:204)

Map generated successfully!
UnityEngine.Debug:Log (object)
Pirate.MapGen.MapManager:GenerateMapData (ulong) (at Assets/Scripts/Map/MapManager.cs:86)
Pirate.MapGen.MapManager:GenerateMapIfNeeded (ulong) (at Assets/Scripts/Map/MapManager.cs:63)
PirateRoguelike.Core.RunManager:OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:220)
PirateRoguelike.Core.RunManager:OnSceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode) (at Assets/Scripts/Core/RunManager.cs:189)
UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode)

Run scene initialized. Gold: 10, Lives: 4
UnityEngine.Debug:Log (object)
PirateRoguelike.Core.RunManager:OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:234)
PirateRoguelike.Core.RunManager:OnSceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode) (at Assets/Scripts/Core/RunManager.cs:189)
UnityEngine.SceneManagement.SceneManager:Internal_SceneLoaded (UnityEngine.SceneManagement.Scene,UnityEngine.SceneManagement.LoadSceneMode)

InvalidOperationException: Service of type PlayerPanelController not registered.
PirateRoguelike.Core.ServiceLocator.Resolve[T] () (at Assets/Scripts/Core/ServiceLocator.cs:32)
PirateRoguelike.Core.RunManager.OnRunSceneLoaded () (at Assets/Scripts/Core/RunManager.cs:242)
PirateRoguelike.Core.RunManager.OnSceneLoaded (UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) (at Assets/Scripts/Core/RunManager.cs:189)
UnityEngine.SceneManagement.SceneManager.Internal_SceneLoaded (UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) (at <98fbc9de20ae47d9bb2559ab79ec6643>:0)

