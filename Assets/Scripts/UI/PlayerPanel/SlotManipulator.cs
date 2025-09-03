using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq; // Added for ToList()
using PirateRoguelike.Services; // Added for SlotId and SlotContainerType
using PirateRoguelike.UI.Components; // Added for SlotElement
using PirateRoguelike.Data; // For ItemInstance

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        public bool IsDragging { get; private set; } // Make IsDragging public for ShopItemElement
        private ISlotViewData _sourceSlotData; // Changed from ItemElement to ISlotViewData
        private PirateRoguelike.Services.SlotContainerType _fromContainer;
        private VisualElement _lastHoveredSlot;

        // Change constructor to accept VisualElement
        public SlotManipulator(VisualElement targetElement, ISlotViewData sourceSlotData)
        {
            target = targetElement; // Assign target here
            _sourceSlotData = sourceSlotData; // Initialize _sourceSlotData directly
            IsDragging = false;
            Debug.Log($"SlotManipulator: Constructor called for {targetElement.name} ({targetElement.GetType().Name}).");
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            // Determine the container type of the target slot when the manipulator is registered
            _fromContainer = GetSlotContainerType(target.parent);
            
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Debug.Log($"SlotManipulator: UnregisterCallbacksFromTarget called for {target.name} ({target.GetType().Name}).");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!UIInteractionService.CanManipulateItem(_fromContainer))
            {
                return;
            }

            // Use _sourceSlotData for SlotId and IsEmpty
            Debug.Log($"SlotManipulator: OnPointerDown called for Slot ID: {_sourceSlotData?.SlotId}");
            if (IsDragging) return;

            if (_sourceSlotData == null || _sourceSlotData.IsEmpty)
            {
                return;
            }

            // Hide the tooltip when a drag operation begins
            TooltipController.Instance.Hide();

            _startPosition = evt.position;
            target.CapturePointer(evt.pointerId);
            IsDragging = true;

            target.style.visibility = Visibility.Hidden; // Hide the original element

            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_sourceSlotData.SlotId, evt);

            _ghostIcon = new Image();
            float ghostWidth = target.resolvedStyle.width * 0.8f;
            float ghostHeight = target.resolvedStyle.height * 0.8f;
            _ghostIcon.style.width = ghostWidth;
            _ghostIcon.style.height = ghostHeight;
            // Access Icon from ISlotViewData
            ((Image)_ghostIcon).sprite = _sourceSlotData.Icon;
            _ghostIcon.style.position = Position.Absolute;
            _ghostIcon.pickingMode = PickingMode.Ignore;
            _ghostIcon.style.opacity = 0.8f;
            target.panel.visualTree.Add(_ghostIcon);

            _ghostIcon.style.left = evt.position.x - (ghostWidth / 2);
            _ghostIcon.style.top = evt.position.y - (ghostHeight / 2);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!IsDragging || !target.HasPointerCapture(evt.pointerId)) return;

            UpdateGhostPosition(evt.position);

            (VisualElement newHoveredSlotElement, ISlotViewData newHoveredSlotData, PirateRoguelike.Services.SlotContainerType newHoveredContainerType) = FindHoveredSlot(evt.position);

            if (newHoveredSlotElement != _lastHoveredSlot)
            {
                _lastHoveredSlot?.RemoveFromClassList("slot--hover");
                newHoveredSlotElement?.AddToClassList("slot--hover");
                _lastHoveredSlot = newHoveredSlotElement;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsDragging || !target.HasPointerCapture(evt.pointerId)) return;

            _lastHoveredSlot?.RemoveFromClassList("slot--hover");

            (VisualElement dropTargetElement, ISlotViewData dropSlotData, PirateRoguelike.Services.SlotContainerType toContainer) = FindHoveredSlot(evt.position);

            Debug.Log($"OnPointerUp: Drop Target Element: {dropTargetElement?.name ?? "NULL"} (Type: {dropTargetElement?.GetType().Name ?? "NULL"})");
            Debug.Log($"OnPointerUp: Drop Slot Data: {dropSlotData?.SlotId.ToString() ?? "NULL"} (IsEmpty: {dropSlotData?.IsEmpty.ToString() ?? "NULL"})");
            Debug.Log($"OnPointerUp: To Container: {toContainer}");

            target.style.visibility = Visibility.Visible; // Make original element visible again

            SlotId fromSlotId = new SlotId(_sourceSlotData.SlotId, _fromContainer);

            if (_fromContainer == SlotContainerType.Shop)
            {
                // Dragging from Shop
                if (dropTargetElement != null && dropSlotData != null)
                {
                    // Dropped onto a valid slot (inventory or equipment)
                    SlotId toSlotId = new SlotId(dropSlotData.SlotId, toContainer);

                    // If target slot is occupied, treat as click-to-buy (find first available)
                    if (!dropSlotData.IsEmpty)
                    {
                        ItemManipulationService.Instance.RequestPurchase(fromSlotId, new SlotId(-1, SlotContainerType.Inventory)); // -1 indicates find first available
                    }
                    else // Target slot is empty, place directly
                    {
                        ItemManipulationService.Instance.RequestPurchase(fromSlotId, toSlotId);
                    }
                }
                else
                {
                    // Dropped outside a valid slot, treat as click-to-buy (find first available)
                    ItemManipulationService.Instance.RequestPurchase(fromSlotId, new SlotId(-1, SlotContainerType.Inventory)); // -1 indicates find first available
                }
            }
            else if (_fromContainer == SlotContainerType.Inventory || _fromContainer == SlotContainerType.Equipment)
            {
                // Existing swap logic for inventory/equipment
                if (dropTargetElement != null && dropSlotData != null)
                {
                    SlotId toSlotId = new SlotId(dropSlotData.SlotId, toContainer);
                    ItemManipulationService.Instance.RequestSwap(fromSlotId, toSlotId);
                }
            }

            CleanUp();
            target.ReleasePointer(evt.pointerId);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!IsDragging) return;
            CleanUp();
        }

        private void UpdateGhostPosition(Vector2 pointerPosition)
        {
            _ghostIcon.style.left = pointerPosition.x - (_ghostIcon.resolvedStyle.width / 2);
            _ghostIcon.style.top = pointerPosition.y - (_ghostIcon.resolvedStyle.height / 2);
        }

        // Modified to return ISlotViewData and SlotContainerType
        private (VisualElement, ISlotViewData, PirateRoguelike.Services.SlotContainerType) FindHoveredSlot(Vector2 pointerPosition)
        {
            PickingMode originalPickingMode = target.pickingMode;
            target.pickingMode = PickingMode.Ignore;

            VisualElement picked = target.panel.Pick(pointerPosition);

            target.pickingMode = originalPickingMode;

            if (picked == null)
            {
                return (null, null, default);
            }

            VisualElement current = picked;
            while (current != null && !current.ClassListContains("slot"))
            {
                current = current.parent;
            }

            if (current != null)
            {
                SlotElement slotElement = current as SlotElement;
                if (slotElement != null && slotElement.userData is ISlotViewData slotData)
                {
                    return (current, slotData, GetSlotContainerType(current));
                }
            }
            return (null, null, default);
        }

        // New helper method to determine the slot's container type
        private PirateRoguelike.Services.SlotContainerType GetSlotContainerType(VisualElement slotElement)
        {
            VisualElement current = slotElement;
            while (current != null)
            {
                if (current.ClassListContains("equipment-bar"))
                {
                    return PirateRoguelike.Services.SlotContainerType.Equipment;
                }
                if (current.name == "inventory-container") // Inventory container has a name, not a class
                {
                    return PirateRoguelike.Services.SlotContainerType.Inventory;
                }
                // NEW: Check for shop item container
                if (current.name == "ShopItemContainer") // Assuming ShopItemContainer is the parent of shop items
                {
                    return PirateRoguelike.Services.SlotContainerType.Shop;
                }
                current = current.parent;
            }
            return PirateRoguelike.Services.SlotContainerType.Inventory; // Default to Inventory if not found
        }

        private void CleanUp()
        {
            IsDragging = false;
            _ghostIcon?.RemoveFromHierarchy();
            _ghostIcon = null;
            _lastHoveredSlot?.RemoveFromClassList("slot--hover");
            _lastHoveredSlot = null;
        }

        public void Dispose()
        {
            UnregisterCallbacksFromTarget();
        }
    }
}