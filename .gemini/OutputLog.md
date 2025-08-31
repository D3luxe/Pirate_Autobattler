TooltipController.Show() called for item: Deckhand
UnityEngine.Debug:Log (object)
TooltipController:Show (ItemSO,UnityEngine.UIElements.VisualElement) (at Assets/Scripts/UI/TooltipController.cs:56)
PirateRoguelike.UI.PlayerPanelView/<>c__DisplayClass25_0:<PopulateSlots>b__0 (UnityEngine.UIElements.PointerEnterEvent) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelView.cs:130)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

NullReferenceException: Object reference not set to an instance of an object
TooltipController.Show (ItemSO item, UnityEngine.UIElements.VisualElement targetElement) (at Assets/Scripts/UI/TooltipController.cs:88)
PirateRoguelike.UI.PlayerPanelView+<>c__DisplayClass25_0.<PopulateSlots>b__0 (UnityEngine.UIElements.PointerEnterEvent evt) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelView.cs:130)
UnityEngine.UIElements.EventCallbackFunctor`1[TEventType].Invoke (UnityEngine.UIElements.EventBase evt) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventCallbackRegistry+DynamicCallbackList.Invoke (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToAssignedTarget (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.PointerEnterEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEventQueue () (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcher.OpenGate () (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcherGate.Dispose () (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <aae110a54f4e40afadedeea84895fc4b>:0)
UnityEngine.UIElements.PanelEventHandler.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.EventSystems.BaseEventData sourceEventData) (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/UIElements/PanelEventHandler.cs:283)
UnityEngine.UIElements.PanelEventHandler.OnPointerMove (UnityEngine.EventSystems.PointerEventData eventData) (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/UIElements/PanelEventHandler.cs:130)
UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerMoveHandler handler, UnityEngine.EventSystems.BaseEventData eventData) (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/ExecuteEvents.cs:22)
UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/ExecuteEvents.cs:272)
UnityEngine.EventSystems.EventSystem:Update() (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

TooltipController.Hide() called.
UnityEngine.Debug:Log (object)
TooltipController:Hide () (at Assets/Scripts/UI/TooltipController.cs:105)
PirateRoguelike.UI.PlayerPanelView/<>c:<PopulateSlots>b__25_1 (UnityEngine.UIElements.PointerLeaveEvent) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelView.cs:134)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

