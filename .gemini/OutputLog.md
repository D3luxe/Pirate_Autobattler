SlotManipulator: OnPointerDown called for Slot ID: 1
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerDown (UnityEngine.UIElements.PointerDownEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:49)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: Drop Target Element:  (Type: SlotElement)
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:105)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: Drop Slot Data: 6 (IsEmpty: True)
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: To Container: Inventory
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:107)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnSlotDropped Event: From Slot ID: 1, From Container: Inventory, To Slot ID: 6, To Container: Inventory
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:111)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotManipulator: OnPointerDown called for Slot ID: 1
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerDown (UnityEngine.UIElements.PointerDownEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:49)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: Drop Target Element:  (Type: SlotElement)
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:105)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: Drop Slot Data: 2 (IsEmpty: True)
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:106)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnPointerUp: To Container: Inventory
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:107)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

OnSlotDropped Event: From Slot ID: 1, From Container: Inventory, To Slot ID: 2, To Container: Inventory
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:111)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotDataViewModel.CurrentItemInstance setter: Item changed to: NULL
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotDataViewModel:set_CurrentItemInstance (PirateRoguelike.Data.ItemInstance) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:298)
PirateRoguelike.UI.PlayerPanelDataViewModel:HandleInventorySwapped (int,int) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:199)
Inventory:SwapItems (int,int) (at Assets/Scripts/Core/Inventory.cs:126)
PirateRoguelike.UI.PlayerPanelController:HandleSlotDropped (int,PirateRoguelike.UI.SlotContainerType,int,PirateRoguelike.UI.SlotContainerType) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:375)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:112)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotElement.OnViewModelPropertyChanged: Property changed: CurrentItemInstance
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.Components.SlotElement:OnViewModelPropertyChanged (object,System.ComponentModel.PropertyChangedEventArgs) (at Assets/Scripts/UI/Components/SlotElement.cs:65)
PirateRoguelike.UI.SlotDataViewModel:OnPropertyChanged (string) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:255)
PirateRoguelike.UI.SlotDataViewModel:set_CurrentItemInstance (PirateRoguelike.Data.ItemInstance) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:299)
PirateRoguelike.UI.PlayerPanelDataViewModel:HandleInventorySwapped (int,int) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:199)
Inventory:SwapItems (int,int) (at Assets/Scripts/Core/Inventory.cs:126)
PirateRoguelike.UI.PlayerPanelController:HandleSlotDropped (int,PirateRoguelike.UI.SlotContainerType,int,PirateRoguelike.UI.SlotContainerType) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:375)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:112)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotElement.OnViewModelPropertyChanged: Property changed: Icon
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.Components.SlotElement:OnViewModelPropertyChanged (object,System.ComponentModel.PropertyChangedEventArgs) (at Assets/Scripts/UI/Components/SlotElement.cs:65)
PirateRoguelike.UI.SlotDataViewModel:OnPropertyChanged (string) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:255)
PirateRoguelike.UI.SlotDataViewModel:set_CurrentItemInstance (PirateRoguelike.Data.ItemInstance) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:300)
PirateRoguelike.UI.PlayerPanelDataViewModel:HandleInventorySwapped (int,int) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:199)
Inventory:SwapItems (int,int) (at Assets/Scripts/Core/Inventory.cs:126)
PirateRoguelike.UI.PlayerPanelController:HandleSlotDropped (int,PirateRoguelike.UI.SlotContainerType,int,PirateRoguelike.UI.SlotContainerType) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:375)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:112)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotElement.OnViewModelPropertyChanged: Property changed: Rarity
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.Components.SlotElement:OnViewModelPropertyChanged (object,System.ComponentModel.PropertyChangedEventArgs) (at Assets/Scripts/UI/Components/SlotElement.cs:65)
PirateRoguelike.UI.SlotDataViewModel:OnPropertyChanged (string) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:255)
PirateRoguelike.UI.SlotDataViewModel:set_CurrentItemInstance (PirateRoguelike.Data.ItemInstance) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:301)
PirateRoguelike.UI.PlayerPanelDataViewModel:HandleInventorySwapped (int,int) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:199)
Inventory:SwapItems (int,int) (at Assets/Scripts/Core/Inventory.cs:126)
PirateRoguelike.UI.PlayerPanelController:HandleSlotDropped (int,PirateRoguelike.UI.SlotContainerType,int,PirateRoguelike.UI.SlotContainerType) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:375)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:112)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotElement.OnViewModelPropertyChanged: Property changed: IsEmpty
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.Components.SlotElement:OnViewModelPropertyChanged (object,System.ComponentModel.PropertyChangedEventArgs) (at Assets/Scripts/UI/Components/SlotElement.cs:65)
PirateRoguelike.UI.SlotDataViewModel:OnPropertyChanged (string) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:255)
PirateRoguelike.UI.SlotDataViewModel:set_CurrentItemInstance (PirateRoguelike.Data.ItemInstance) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:302)
PirateRoguelike.UI.PlayerPanelDataViewModel:HandleInventorySwapped (int,int) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:199)
Inventory:SwapItems (int,int) (at Assets/Scripts/Core/Inventory.cs:126)
PirateRoguelike.UI.PlayerPanelController:HandleSlotDropped (int,PirateRoguelike.UI.SlotContainerType,int,PirateRoguelike.UI.SlotContainerType) (at Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs:375)
PirateRoguelike.UI.SlotManipulator:OnPointerUp (UnityEngine.UIElements.PointerUpEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:112)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotManipulator: OnPointerDown called for Slot ID: 1
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerDown (UnityEngine.UIElements.PointerDownEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:49)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

SlotManipulator: OnPointerDown called for Slot ID: 1
UnityEngine.Debug:Log (object)
PirateRoguelike.UI.SlotManipulator:OnPointerDown (UnityEngine.UIElements.PointerDownEvent) (at Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs:49)
UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@423bc642aff1/Runtime/UGUI/EventSystem/EventSystem.cs:514)

