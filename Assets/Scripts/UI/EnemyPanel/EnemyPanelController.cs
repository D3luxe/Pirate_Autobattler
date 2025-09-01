using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized; // Added
using PirateRoguelike.Data;
using PirateRoguelike.Runtime;
using PirateRoguelike.UI.Components; // Added for ShipDisplayElement and SlotElement
using PirateRoguelike.Shared; // Added for ObservableList
using PirateRoguelike.UI.Utilities; // Added for TooltipUtility

namespace PirateRoguelike.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class EnemyPanelController : MonoBehaviour
    {
        [SerializeField] private PlayerUIThemeSO _theme; // Reusing player theme for now

        private ShipState _enemyShipState; // Reference to the enemy's ShipState
        private EnemyShipViewData _viewModel; // Add this line
        private ShipDisplayElement _shipDisplayElement; // Added for ShipDisplayElement
        private VisualElement _equipmentBar; // Added to hold equipment slots
        private ObservableList<ISlotViewData> _enemyEquipmentSlots; // Added for observable enemy equipment

        public void Initialize(ShipState enemyShipState)
        {
            _enemyShipState = enemyShipState;

            var root = GetComponent<UIDocument>().rootVisualElement;
            // Instantiate ShipDisplayElement directly as it's no longer a UxmlElement
            _shipDisplayElement = new ShipDisplayElement();
            root.Add(_shipDisplayElement); // Assuming 'root' is the correct parent for the enemy ship display

            _equipmentBar = root.Q<VisualElement>("equipment-bar"); // Query equipment bar

            _viewModel = new EnemyShipViewData(_enemyShipState); // Instantiate the viewmodel

            // Subscribe to enemy ship events (only equipment changed remains)
            _enemyShipState.OnEquipmentAddedAt += HandleEquipmentAddedAt;
            _enemyShipState.OnEquipmentRemovedAt += HandleEquipmentRemovedAt;
            _enemyShipState.OnEquipmentSwapped += HandleEquipmentSwapped;

            // Initial data bind
            _shipDisplayElement.Bind(_viewModel); // Bind ShipDisplayElement

            _enemyEquipmentSlots = new ObservableList<ISlotViewData>();
            UpdateEnemyEquipmentSlots(); // Initial population
            BindEquipmentSlots(_equipmentBar, _enemyEquipmentSlots); // Bind to the observable list
        }

        void OnDestroy()
        {
            if (_enemyShipState != null)
            {
                _enemyShipState.OnEquipmentAddedAt -= HandleEquipmentAddedAt;
                _enemyShipState.OnEquipmentRemovedAt -= HandleEquipmentRemovedAt;
                _enemyShipState.OnEquipmentSwapped -= HandleEquipmentSwapped;
            }
        }

        // Event Handlers
        private void UpdateEnemyEquipmentSlots()
        {
            _enemyEquipmentSlots.Clear();
            foreach (var slotVm in _enemyShipState.Equipped.Select((item, index) => new SlotDataViewModel(item, index)).Cast<ISlotViewData>()) 
            {
                _enemyEquipmentSlots.Add(slotVm);
            }
        }

        private void HandleEquipmentSwapped(int indexA, int indexB)
        {
            UpdateEnemyEquipmentSlots();
        }

        private void HandleEquipmentAddedAt(int index, ItemInstance item)
        {
            UpdateEnemyEquipmentSlots();
        }

        private void HandleEquipmentRemovedAt(int index, ItemInstance item)
        {
            UpdateEnemyEquipmentSlots();
        }

        private void BindEquipmentSlots(VisualElement container, ObservableList<ISlotViewData> slots)
        {
            // Clear existing elements and populate initially
            container.Clear();
            foreach (var slotData in slots)
            {
                container.Add(CreateSlotElement(slotData));
            }

            // Subscribe to collection changes
            slots.CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (ISlotViewData newItem in args.NewItems)
                        {
                            container.Insert(args.NewStartingIndex, CreateSlotElement(newItem));
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (ISlotViewData oldItem in args.OldItems)
                        {
                            var elementToRemove = container.Children().FirstOrDefault(e => e.userData == oldItem);
                            if (elementToRemove != null)
                            {
                                container.Remove(elementToRemove);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        foreach (ISlotViewData oldItem in args.OldItems)
                        {
                            var elementToRemove = container.Children().FirstOrDefault(e => e.userData == oldItem);
                            if (elementToRemove != null)
                            {
                                container.Remove(elementToRemove);
                            }
                        }
                        foreach (ISlotViewData newItem in args.NewItems)
                        {
                            container.Insert(args.NewStartingIndex, CreateSlotElement(newItem));
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        var elementToMove = container.Children().FirstOrDefault(e => e.userData == args.OldItems[0]);
                        if (elementToMove != null)
                        {
                            container.Remove(elementToMove);
                            container.Insert(args.NewStartingIndex, elementToMove);
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        container.Clear();
                        foreach (var slotData in slots)
                        {
                            container.Add(CreateSlotElement(slotData));
                        }
                        break;
                }
            };
        }

        private SlotElement CreateSlotElement(ISlotViewData slotData)
        {
            SlotElement slotElement = new SlotElement();
            slotElement.userData = slotData;
            slotElement.Bind(slotData);

            // Register PointerEnter and PointerLeave events for tooltip
            TooltipUtility.RegisterTooltipCallbacks(slotElement, slotData);
            return slotElement;
        }

        
    }
}