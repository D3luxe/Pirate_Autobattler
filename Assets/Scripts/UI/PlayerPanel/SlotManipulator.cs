using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq; // Added for ToList()
using PirateRoguelike.UI.Components; // Added for SlotElement

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        private bool _isDragging;
        private ISlotViewData _slotData; // Keep this for now, might be needed for drop logic
        private ItemElement _itemElement; // New field
        private SlotContainerType _fromContainer; // Re-added field
        private VisualElement _lastHoveredSlot; // Re-added field

        public SlotManipulator(ItemElement itemElement)
        {
            _itemElement = itemElement;
            _slotData = itemElement.SlotViewData; // Get SlotData from ItemElement
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

            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_slotData.SlotId, evt);

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

            (VisualElement newHoveredSlotElement, ISlotViewData newHoveredSlotData, SlotContainerType newHoveredContainerType) = FindHoveredSlot(evt.position);

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

            (VisualElement dropTargetElement, ISlotViewData dropSlotData, SlotContainerType toContainer) = FindHoveredSlot(evt.position);

            Debug.Log($"OnPointerUp: Drop Target Element: {dropTargetElement?.name ?? "NULL"} (Type: {dropTargetElement?.GetType().Name ?? "NULL"})");
            Debug.Log($"OnPointerUp: Drop Slot Data: {dropSlotData?.SlotId.ToString() ?? "NULL"} (IsEmpty: {dropSlotData?.IsEmpty.ToString() ?? "NULL"})");
            Debug.Log($"OnPointerUp: To Container: {toContainer}");

            if (dropTargetElement != null && dropSlotData != null) // Ensure dropSlotData is not null
            {
                Debug.Log($"OnSlotDropped Event: From Slot ID: {_itemElement.SlotViewData.SlotId}, From Container: {_fromContainer}, To Slot ID: {dropSlotData.SlotId}, To Container: {toContainer}");
                PlayerPanelEvents.OnSlotDropped?.Invoke(_itemElement.SlotViewData.SlotId, _fromContainer, dropSlotData.SlotId, toContainer);
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
        private (VisualElement, ISlotViewData, SlotContainerType) FindHoveredSlot(Vector2 pointerPosition)
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
        private SlotContainerType GetSlotContainerType(VisualElement slotElement)
        {
            VisualElement current = slotElement;
            while (current != null)
            {
                if (current.ClassListContains("equipment-bar"))
                {
                    return SlotContainerType.Equipment;
                }
                if (current.name == "inventory-container") // Inventory container has a name, not a class
                {
                    return SlotContainerType.Inventory;
                }
                current = current.parent;
            }
            return SlotContainerType.Inventory;
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