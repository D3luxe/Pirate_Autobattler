using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using PirateRoguelike.Services;
using PirateRoguelike.UI.Components;
using PirateRoguelike.Core;
using PirateRoguelike.Events;
using PirateRoguelike.Saving; // Added

namespace PirateRoguelike.UI
{
    public class RewardUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _root;
        private VisualElement _rewardItemsContainer;
        private Label _goldAmountLabel;
        private Button _claimAllButton; // Removed from UXML, but kept for now
        private Button _leaveButton;

        private Button _goldButton;
        private Label _goldButtonLabel;
        private Button _skipButton;

        private List<ItemSO> _currentRewardItems;
        private int _currentRewardGold;
        private int _initialGoldReward; // New field

        void Awake()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("UIDocument is not assigned to RewardUIController.");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            _root.style.display = DisplayStyle.None; // Hide initially

            _rewardItemsContainer = _root.Q<VisualElement>("reward-items-container");
            _goldButton = _root.Q<Button>("gold-button");
            _goldButtonLabel = _goldButton.Q<Label>("gold-button-label");
            _skipButton = _root.Q<Button>("skip-button");
            _leaveButton = _root.Q<Button>("leave-button");

            _goldButton.clicked += OnGoldButtonClicked;
            _skipButton.clicked += OnSkipButtonClicked;
            _leaveButton.clicked += OnLeaveClicked;
        }

        void OnEnable()
        {
            ItemManipulationEvents.OnRewardItemClaimed += HandleRewardItemClaimed; // Subscribe to new event
        }

        void OnDisable()
        {
            ItemManipulationEvents.OnRewardItemClaimed -= HandleRewardItemClaimed; // Unsubscribe from new event
        }

        public void ShowRewards(List<SerializableItemInstance> items, int gold)
        {
            _currentRewardItems = new List<ItemSO>();
            foreach (var serializableItem in items)
            {
                ItemSO itemSO = GameDataRegistry.GetItem(serializableItem.itemId, serializableItem.rarity);
                if (itemSO != null)
                {
                    _currentRewardItems.Add(itemSO);
                }
                else
                {
                    Debug.LogWarning($"RewardUIController: Could not find ItemSO for serializable item ID: {serializableItem.itemId}");
                }
            }

            _initialGoldReward = gold; // Store initial gold reward

            _rewardItemsContainer.Clear();
            if (_currentRewardItems != null && _currentRewardItems.Any())
            {
                _rewardItemsContainer.style.display = DisplayStyle.Flex;
                foreach (var item in _currentRewardItems)
                {
                    var slotElement = new SlotElement();
                    slotElement.pickingMode = PickingMode.Position; // Explicitly set picking mode
                    slotElement.AddToClassList("slot");
                    var viewModel = new RewardItemSlotViewData(new ItemInstance(item));
                    slotElement.Bind(viewModel);
                    _rewardItemsContainer.Add(slotElement);
                    PirateRoguelike.UI.Utilities.TooltipUtility.RegisterTooltipCallbacks(slotElement, viewModel, UIManager.Instance.GlobalUIRoot);
                }
                _skipButton.text = $"Skip (+{Mathf.RoundToInt(_initialGoldReward * 0.5f)} Gold)"; // Update skip button text
                _skipButton.style.display = DisplayStyle.Flex; // Show skip button if items are present
            }
            else
            {
                _rewardItemsContainer.style.display = DisplayStyle.None; // Hide item container if no items
                _skipButton.style.display = DisplayStyle.None; // Hide skip button if no items
            }

            if (gold > 0)
            {
                _goldButton.style.display = DisplayStyle.Flex;
                _goldButtonLabel.text = $"{gold} Gold";
            }
            else
            {
                _goldButton.style.display = DisplayStyle.None;
            }

            _root.style.display = DisplayStyle.Flex; // Show the UI
        }

        private void OnClaimAllClicked()
        {
            // Implement logic to claim all items and gold
            // This will likely involve calling ItemManipulationService for each item
            // and GameSession.Economy for gold.
            Debug.Log("Claim All button clicked.");
            // For now, just hide and clear
            GameSession.Economy.AddGold(_currentRewardGold);
            RewardService.ClearRewards();
            _root.style.display = DisplayStyle.None;
            // TODO: Add actual item claiming logic
        }

        private void OnGoldButtonClicked()
        {
            GameSession.Economy.AddGold(_initialGoldReward);
            _initialGoldReward = 0; // Reset gold reward
            _goldButton.style.display = DisplayStyle.None; // Hide gold button
            CheckAndCloseUI(); // Check if UI can be closed
        }

        private void OnSkipButtonClicked()
        {
            int bonusGold = Mathf.RoundToInt(_initialGoldReward * 0.5f);
            GameSession.Economy.AddGold(_initialGoldReward + bonusGold);
            _initialGoldReward = 0; // Reset gold reward
            _goldButton.style.display = DisplayStyle.None; // Hide gold button
            _rewardItemsContainer.style.display = DisplayStyle.None; // Hide item container
            _skipButton.style.display = DisplayStyle.None; // Hide skip button
            CheckAndCloseUI();
        }

        public void HandleRewardItemClaimed(int itemIndex)
        {
            Debug.Log($"Item at index {itemIndex} claimed from rewards.");

            _rewardItemsContainer.style.display = DisplayStyle.None; // Hide item shelf
            _skipButton.style.display = DisplayStyle.None; // Hide skip button

            // Automatically collect gold if offered
            if (_goldButton.style.display == DisplayStyle.Flex)
            {
                OnGoldButtonClicked();
            }
            else
            {
                CheckAndCloseUI(); // Check if UI can be closed if no gold to collect
            }
        }

        private void OnLeaveClicked()
        {
            Debug.Log("Leave button clicked.");
            RewardService.ClearRewards();
            _root.style.display = DisplayStyle.None;
            // TODO: Transition back to map or next scene
        }

        private void CheckAndCloseUI()
        {
            bool goldCollected = _goldButton.style.display == DisplayStyle.None;
            bool itemsCollected = _rewardItemsContainer.style.display == DisplayStyle.None;

            if (goldCollected && itemsCollected)
            {
                _root.style.display = DisplayStyle.None; // Hide the UI
                RewardService.ClearRewards(); // Clear internal reward state
            }
        }
    }
}
