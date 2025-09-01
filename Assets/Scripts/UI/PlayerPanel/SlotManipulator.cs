using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq; // Added for ToList()
using PirateRoguelike.Services; // Added for SlotId and SlotContainerType
using PirateRoguelike.UI.Components; // Added for SlotElement

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        private bool _isDragging;
        private ItemElement _itemElement; // New field
        private PirateRoguelike.Services.SlotContainerType _fromContainer; // Re-added field
        private VisualElement _lastHoveredSlot; // Re-added field

        public SlotManipulator(ItemElement itemElement)
        {
            _itemElement = itemElement;
            _isDragging = false;
            Debug.Log($"SlotManipulator: Constructor called for ItemElement.");
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
            Debug.Log($"SlotManipulator: UnregisterCallbacksFromTarget called.");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!UIInteractionService.CanManipulateItem(_fromContainer))
            {
                return;
            }

            Debug.Log($"SlotManipulator: OnPointerDown called for Slot ID: {_itemElement.SlotViewData.SlotId}"); // ADD THIS LINE
            if (_isDragging) return;

            if (_itemElement.SlotViewData.IsEmpty)
            {
                return;
            }

            // Hide the tooltip when a drag operation begins
            TooltipController.Instance.Hide();

            _startPosition = evt.position;
            target.CapturePointer(evt.pointerId);
            _isDragging = true;

            _itemElement.style.visibility = Visibility.Hidden; // ADD THIS LINE

            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_itemElement.SlotViewData.SlotId, evt);

            _ghostIcon = new Image();
            float ghostWidth = target.resolvedStyle.width * 0.8f;
            float ghostHeight = target.resolvedStyle.height * 0.8f;
            _ghostIcon.style.width = ghostWidth;
            _ghostIcon.style.height = ghostHeight;
            ((Image)_ghostIcon).sprite = _itemElement.IconElement.sprite;
            _ghostIcon.style.position = Position.Absolute;
            _ghostIcon.pickingMode = PickingMode.Ignore;
            _ghostIcon.style.opacity = 0.8f;
            target.panel.visualTree.Add(_ghostIcon);

            _ghostIcon.style.left = evt.position.x - (ghostWidth / 2);
            _ghostIcon.style.top = evt.position.y - (ghostHeight / 2);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

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
            if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            _lastHoveredSlot?.RemoveFromClassList("slot--hover");

            (VisualElement dropTargetElement, ISlotViewData dropSlotData, PirateRoguelike.Services.SlotContainerType toContainer) = FindHoveredSlot(evt.position);

            Debug.Log($"OnPointerUp: Drop Target Element: {dropTargetElement?.name ?? "NULL"} (Type: {dropTargetElement?.GetType().Name ?? "NULL"})");
            Debug.Log($"OnPointerUp: Drop Slot Data: {dropSlotData?.SlotId.ToString() ?? "NULL"} (IsEmpty: {dropSlotData?.IsEmpty.ToString() ?? "NULL"})");
            Debug.Log($"OnPointerUp: To Container: {toContainer}");

            _itemElement.style.visibility = Visibility.Visible; // Make ItemElement visible again
 
            if (dropTargetElement != null && dropSlotData != null) // Ensure dropSlotData is not null
            {
                SlotId fromSlotId = new SlotId(_itemElement.SlotViewData.SlotId, _fromContainer);
                SlotId toSlotId = new SlotId(dropSlotData.SlotId, toContainer);

                Debug.Log($"OnPointerUp: dropSlotData.CurrentItemInstance: {dropSlotData.CurrentItemInstance?.Def.displayName ?? "NULL"}");

                if (_fromContainer == SlotContainerType.Inventory || _fromContainer == SlotContainerType.Equipment)
                {
                    ItemManipulationService.Instance.RequestSwap(fromSlotId, toSlotId);
                }
                // else if (_fromContainer == SlotContainerType.Shop) { ... }
            }

            CleanUp();
            target.ReleasePointer(evt.pointerId);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!_isDragging) return;
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
                current = current.parent;
            }
            return PirateRoguelike.Services.SlotContainerType.Inventory;
        }

        private void CleanUp()
        {
            _isDragging = false;
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
