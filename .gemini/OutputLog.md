No current encounter. Scrolling to bottom. Target Scroll Y: 2492
UnityEngine.Debug:Log (object)
MapView:PerformAutoScroll () (at Assets/Scripts/UI/MapView.cs:473)
MapView:<Show>b__47_0 () (at Assets/Scripts/UI/MapView.cs:426)
UnityEngine.UIElements.UIElementsRuntimeUtilityNative:UpdatePanels ()

First move to node: node_0_0
UnityEngine.Debug:Log (object)
MapView:OnNodeClicked (string) (at Assets/Scripts/UI/MapView.cs:584)
MapView/<>c__DisplayClass40_0:<RenderNodesAndEdges>b__1 (UnityEngine.UIElements.ClickEvent) (at Assets/Scripts/UI/MapView.cs:252)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

PlayerIndicator: Visible at 572.6243, 2911.732
UnityEngine.Debug:Log (object)
MapView:UpdateNodeVisualStates () (at Assets/Scripts/UI/MapView.cs:505)
MapView:OnNodeClicked (string) (at Assets/Scripts/UI/MapView.cs:587)
MapView/<>c__DisplayClass40_0:<RenderNodesAndEdges>b__1 (UnityEngine.UIElements.ClickEvent) (at Assets/Scripts/UI/MapView.cs:252)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

Valid move! Moving from node_0_0 to node_1_2.
UnityEngine.Debug:Log (object)
MapView:OnNodeClicked (string) (at Assets/Scripts/UI/MapView.cs:610)
MapView/<>c__DisplayClass40_0:<RenderNodesAndEdges>b__1 (UnityEngine.UIElements.ClickEvent) (at Assets/Scripts/UI/MapView.cs:252)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

PlayerIndicator: Visible at 790.6027, 2674.218
UnityEngine.Debug:Log (object)
MapView:UpdateNodeVisualStates () (at Assets/Scripts/UI/MapView.cs:505)
GameSession:InvokeOnPlayerNodeChanged () (at Assets/Scripts/Core/GameSession.cs:22)
MapView:OnNodeClicked (string) (at Assets/Scripts/UI/MapView.cs:614)
MapView/<>c__DisplayClass40_0:<RenderNodesAndEdges>b__1 (UnityEngine.UIElements.ClickEvent) (at Assets/Scripts/UI/MapView.cs:252)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

Player moved to node node_1_2 of type Battle.
UnityEngine.Debug:Log (object)
RunManager:HandlePlayerNodeChanged () (at Assets/Scripts/Core/RunManager.cs:189)
GameSession:InvokeOnPlayerNodeChanged () (at Assets/Scripts/Core/GameSession.cs:22)
MapView:OnNodeClicked (string) (at Assets/Scripts/UI/MapView.cs:614)
MapView/<>c__DisplayClass40_0:<RenderNodesAndEdges>b__1 (UnityEngine.UIElements.ClickEvent) (at Assets/Scripts/UI/MapView.cs:252)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

TickService: StartTicking called.
UnityEngine.Debug:Log (object)
TickService:StartTicking () (at Assets/Scripts/Core/TickService.cs:13)
CombatController:Init (ShipState,EnemySO,ITickService,BattleUIController,ShipStateView,ShipStateView,EnemyPanelController) (at Assets/Scripts/Combat/CombatController.cs:32)
BattleManager:SetupBattle () (at Assets/Scripts/Combat/BattleManager.cs:89)
BattleManager:Start () (at Assets/Scripts/Combat/BattleManager.cs:32)

