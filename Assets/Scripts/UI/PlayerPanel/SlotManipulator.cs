
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        private bool _isDragging;
        private int _slotId;
        private VisualElement _lastHoveredSlot;

        public SlotManipulator(int slotId)
        {
            _slotId = slotId;
            _isDragging = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
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

            _startPosition = evt.position;
            target.CapturePointer(evt.pointerId);
            _isDragging = true;

            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_slotId, evt);

            // Create and position the ghost icon
            _ghostIcon = new VisualElement();
            _ghostIcon.AddToClassList("slot__icon"); // Reuse icon style
            _ghostIcon.style.width = target.resolvedStyle.width * 0.8f;
            _ghostIcon.style.height = target.resolvedStyle.height * 0.8f;
            _ghostIcon.style.backgroundImage = target.Q<Image>("icon").style.backgroundImage;
            _ghostIcon.style.position = Position.Absolute;
            _ghostIcon.pickingMode = PickingMode.Ignore;
            _ghostIcon.style.opacity = 0.8f;
            target.panel.visualTree.Add(_ghostIcon);
            UpdateGhostPosition(evt.position);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            UpdateGhostPosition(evt.position);

            VisualElement newHoveredSlot = FindHoveredSlot(evt.position);

            if (newHoveredSlot != _lastHoveredSlot)
            {
                _lastHoveredSlot?.RemoveFromClassList("slot--hover");
                newHoveredSlot?.AddToClassList("slot--hover");
                _lastHoveredSlot = newHoveredSlot;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            _lastHoveredSlot?.RemoveFromClassList("slot--hover");

            VisualElement dropTarget = FindHoveredSlot(evt.position);
            if (dropTarget != null && dropTarget.userData is int dropSlotId)
            {
                PlayerPanelEvents.OnSlotDropped?.Invoke(_slotId, dropSlotId);
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

        private VisualElement FindHoveredSlot(Vector2 pointerPosition)
        {
            VisualElement picked = target.panel.Pick(pointerPosition);
            if (picked == null) return null;

            // Traverse up to find the parent with the 'slot' class
            VisualElement current = picked;
            while (current != null && !current.ClassListContains("slot"))
            {
                current = current.parent;
            }
            return current;
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
