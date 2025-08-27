using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        private bool _isDragging;
        private ISlotViewData _slotData;
        private SlotContainerType _fromContainer; // New field to store the source container type
        private VisualElement _lastHoveredSlot;

        public SlotManipulator(ISlotViewData slotData)
        {
            _slotData = slotData;
            _isDragging = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            // Determine the container type of the target slot when the manipulator is registered
            _fromContainer = GetSlotContainerType(target);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_isDragging) return;

            if (_slotData.IsEmpty)
            {
                return;
            }

            _startPosition = evt.position;
            target.CapturePointer(evt.pointerId);
            _isDragging = true;

            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_slotData.SlotId, evt);

            _ghostIcon = new Image();
            float ghostWidth = target.resolvedStyle.width * 0.8f;
            float ghostHeight = target.resolvedStyle.height * 0.8f;
            _ghostIcon.style.width = ghostWidth;
            _ghostIcon.style.height = ghostHeight;
            ((Image)_ghostIcon).sprite = target.Q<Image>("icon").sprite;
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

            if (dropTargetElement != null && dropSlotData != null) // Ensure dropSlotData is not null
            {
                PlayerPanelEvents.OnSlotDropped?.Invoke(_slotData.SlotId, _fromContainer, dropSlotData.SlotId, toContainer);
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

//            Debug.Log($"FindHoveredSlot: Picked element: {picked?.name ?? "NULL"}");

            if (picked == null) return (null, null, default); // Return default for SlotContainerType

            VisualElement current = picked;
            while (current != null && !current.ClassListContains("slot"))
            {
                current = current.parent;
            }

//            Debug.Log($"FindHoveredSlot: Found slot element: {current?.name ?? "NULL"}");

            if (current != null && current.userData is ISlotViewData slotData)
            {
//                Debug.Log($"FindHoveredSlot: Slot userData ID: {slotData.SlotId}");
                SlotContainerType containerType = GetSlotContainerType(current);
                return (current, slotData, containerType);
            }
            else
            {
//                Debug.Log($"FindHoveredSlot: Slot userData is not ISlotViewData or is null.");
                return (null, null, default); // Return default for SlotContainerType
            }
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
            // Default or error case, should ideally not happen if all slots are in a defined container
 //           Debug.LogWarning($"Slot element {slotElement.name} not found within a known container (equipment-bar or inventory-container). Defaulting to Inventory.");
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
    }
}