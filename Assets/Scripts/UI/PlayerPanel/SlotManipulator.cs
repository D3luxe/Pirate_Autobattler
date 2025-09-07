using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using PirateRoguelike.Services;
using PirateRoguelike.UI.Components;
using PirateRoguelike.Data;
using PirateRoguelike.Commands; // New using statement

namespace PirateRoguelike.UI
{
    public class SlotManipulator : PointerManipulator
    {
        private VisualElement _ghostIcon;
        private Vector2 _startPosition;
        public bool IsDragging { get; private set; }
        private ISlotViewData _sourceSlotData;
        private global::PirateRoguelike.Services.SlotContainerType _fromContainer;
        private VisualElement _lastHoveredSlot;
        private float _dragThreshold = 5f;
        private Vector2 _pointerDownPosition;
        private bool _isPointerDown = false;
        private PointerDownEvent _initialPointerDownEvent;

        public SlotManipulator(VisualElement targetElement, ISlotViewData sourceSlotData)
        {
            target = targetElement;
            _sourceSlotData = sourceSlotData;
            IsDragging = false;
            Debug.Log($"SlotManipulator: Constructor called for {targetElement.name} ({targetElement.GetType().Name}).");
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
            Debug.Log($"SlotManipulator: UnregisterCallbacksFromTarget called for {target.name} ({target.GetType().Name}).");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _fromContainer = _sourceSlotData.ContainerType;

            if (UIStateService.IsConsoleOpen) { return; }

            if (!UIInteractionService.CanManipulateItem(_fromContainer))
            {
                return;
            }

            if (_sourceSlotData == null || _sourceSlotData.IsEmpty)
            {
                return;
            }

            TooltipController.Instance.Hide();

            _pointerDownPosition = evt.position;
            _isPointerDown = true;
            target.CapturePointer(evt.pointerId);
            _initialPointerDownEvent = evt;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isPointerDown) return; // Only proceed if mouse button is down

            float distance = Vector2.Distance(_pointerDownPosition, evt.position); // Declare distance once here

            if (!IsDragging) // If dragging has not yet started
            {
                if (distance > _dragThreshold)
                {
                    IsDragging = true;
                    target.style.visibility = Visibility.Hidden;

                    PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_sourceSlotData.SlotId, _initialPointerDownEvent);

                    _ghostIcon = new Image();
                    float ghostWidth = target.resolvedStyle.width * 0.8f;
                    float ghostHeight = target.resolvedStyle.height * 0.8f;
                    _ghostIcon.style.width = ghostWidth;
                    _ghostIcon.style.height = ghostHeight;
                    ((Image)_ghostIcon).sprite = _sourceSlotData.Icon;
                    _ghostIcon.style.position = Position.Absolute;
                    _ghostIcon.pickingMode = PickingMode.Ignore;
                    _ghostIcon.style.opacity = 0.8f;
                    target.panel.visualTree.Add(_ghostIcon);

                    _ghostIcon.style.left = evt.position.x - (ghostWidth / 2);
                    _ghostIcon.style.top = evt.position.y - (ghostHeight / 2);

                    UpdateGhostPosition(evt.position); // Initial position update for the ghost icon
                }
            }

            if (IsDragging) // If dragging is active (either just initiated or ongoing)
            {
                UpdateGhostPosition(evt.position);

                (VisualElement newHoveredSlotElement, ISlotViewData newHoveredSlotData, global::PirateRoguelike.Services.SlotContainerType newHoveredContainerType) = FindHoveredSlot(evt.position);

                if (newHoveredSlotElement != _lastHoveredSlot)
                {
                    _lastHoveredSlot?.RemoveFromClassList("slot--hover");
                    newHoveredSlotElement?.AddToClassList("slot--hover");
                    _lastHoveredSlot = newHoveredSlotElement;
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _isPointerDown = false;

            if (!target.HasPointerCapture(evt.pointerId)) return;

            _lastHoveredSlot?.RemoveFromClassList("slot--hover");

            (VisualElement dropTargetElement, ISlotViewData dropSlotData, global::PirateRoguelike.Services.SlotContainerType toContainer) = FindHoveredSlot(evt.position);

            Debug.Log($"OnPointerUp: Drop Target Element: {dropTargetElement?.name ?? "NULL"} (Type: {dropTargetElement?.GetType().Name ?? "NULL"})");
            Debug.Log($"OnPointerUp: Drop Slot Data: {dropSlotData?.SlotId.ToString() ?? "NULL"} (IsEmpty: {dropSlotData?.IsEmpty.ToString() ?? "NULL"})");
            Debug.Log($"OnPointerUp: To Container: {toContainer}");

            target.style.visibility = Visibility.Visible;

            global::PirateRoguelike.Services.SlotId fromSlotId = new global::PirateRoguelike.Services.SlotId(_sourceSlotData.SlotId, _fromContainer);
            global::PirateRoguelike.Services.SlotId toSlotId = (dropSlotData != null) ? new global::PirateRoguelike.Services.SlotId(dropSlotData.SlotId, toContainer) : new global::PirateRoguelike.Services.SlotId(-1, global::PirateRoguelike.Services.SlotContainerType.Inventory); // Default to inventory if no specific target

            ICommand command = null;

            if (!IsDragging && _fromContainer == global::PirateRoguelike.Services.SlotContainerType.Shop)
            {
                // Click on shop item
                ItemSO itemToPurchase = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                command = new PurchaseItemCommand(fromSlotId, toSlotId, itemToPurchase);
            }
            else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Shop)
            {
                // Drag from shop
                ItemSO itemToPurchase = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                command = new PurchaseItemCommand(fromSlotId, toSlotId, itemToPurchase);
            }
            else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Inventory || _fromContainer == global::PirateRoguelike.Services.SlotContainerType.Equipment)
            {
                // Drag from inventory/equipment
                if (dropTargetElement != null && dropSlotData != null)
                {
                    command = new SwapItemCommand(fromSlotId, toSlotId);
                }
            }
            else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Reward)
            {
                ItemSO itemToClaim = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                if (dropTargetElement != null && dropSlotData != null && (toContainer == global::PirateRoguelike.Services.SlotContainerType.Inventory || toContainer == global::PirateRoguelike.Services.SlotContainerType.Equipment))
                {
                    // Drag from reward to inventory/equipment
                    command = new ClaimRewardItemCommand(fromSlotId, toSlotId, itemToClaim);
                }
                else if (!IsDragging) // Click to claim
                {
                    // Click on reward item
                    command = new ClaimRewardItemCommand(fromSlotId, toSlotId, itemToClaim);
                }
                else
                {
                    Debug.LogWarning("Reward item dropped outside valid target or not a click.");
                }
            }

            if (command != null)
            {
                UICommandProcessor.Instance.ProcessCommand(command);
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

        private (VisualElement, ISlotViewData, global::PirateRoguelike.Services.SlotContainerType) FindHoveredSlot(Vector2 pointerPosition)
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
                    return (current, slotData, slotData.ContainerType);
                }
            }
            return (null, null, default);
        }

        private void CleanUp()
        {
            IsDragging = false;
            _isPointerDown = false;
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