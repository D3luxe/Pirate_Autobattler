using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PirateRoguelike.Data;

public class RewardUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private RewardItemSlot[] itemSlots; // Custom class/prefab for displaying item choices
    [SerializeField] private Button skipRewardButton;

    private List<SerializableItemInstance> _currentRewards;
    private int _currentGoldReward;

    public static RewardUIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // Return early to avoid further initialization
        }
        Instance = this;

        // Ensure the UI has a Canvas to render on.
        // This makes the prefab self-sufficient.
        if (GetComponentInParent<Canvas>() == null)
        {
            Debug.LogWarning("RewardUIController is not under a Canvas. Adding one to ensure it renders.");
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Add essential components for UI interaction and scaling.
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        rewardPanel.SetActive(false); // Start hidden
    }

    public void ShowRewards(List<SerializableItemInstance> rewards, int goldReward)
    {
        _currentRewards = rewards;
        _currentGoldReward = goldReward;

        Debug.Log($"RewardUI: Showing rewards. Gold: {goldReward}, Items: {rewards.Count}");

        goldRewardText.text = $"Gold: {goldReward}";

        // Populate item slots
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < _currentRewards.Count)
            {
                ItemSO itemSO = GameDataRegistry.GetItem(_currentRewards[i].itemId);
                if (itemSO != null)
                {
                    Debug.Log($"RewardUI: Populating slot {i} with item '{itemSO.displayName}' (ID: {_currentRewards[i].itemId})");
                    itemSlots[i].SetItem(itemSO, i);
                    itemSlots[i].SetController(this); // Pass controller reference
                    itemSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogError($"ItemSO not found for ID: {_currentRewards[i].itemId}");
                    itemSlots[i].gameObject.SetActive(false);
                }
            }
            else
            {
                itemSlots[i].gameObject.SetActive(false); // Hide unused slots
            }
        }

        rewardPanel.SetActive(true);
        Debug.Log("RewardUI: Panel activated.");
    }

    public void OnItemChosen(int index)
    {
        if (index >= 0 && index < _currentRewards.Count)
        {
            ItemSO chosenItemSO = GameDataRegistry.GetItem(_currentRewards[index].itemId);
            if (chosenItemSO != null)
            {
                // Check inventory space before adding
                if (GameSession.Inventory.CanAddItem(new ItemInstance(chosenItemSO)))
                {
                    GameSession.Inventory.AddItem(new ItemInstance(chosenItemSO));
                    Debug.Log($"Player chose item: {chosenItemSO.displayName}");
                    EndRewardPhase();
                }
                else
                {
                    // TODO: Display "Inventory Full" message to player
                    Debug.LogWarning("Inventory is full! Cannot add item.");
                }
            }
        }
    }

    public void OnSkipRewardClicked()
    {
        // Player skips item reward, gets bonus gold
        int bonusGold = _currentGoldReward / 2; // As per GEMINI.md: 50% of win gold
        GameSession.Economy.AddGold(bonusGold);
        Debug.Log($"Player skipped item reward and gained {bonusGold} bonus gold.");
        EndRewardPhase();
    }

    private void EndRewardPhase()
    {
        Debug.Log("Reward phase ended. Hiding reward panel.");
        rewardPanel.SetActive(false);
        // The player is already in the Run scene, so we just need to hide this UI.
        // The MapUI and other elements should be revealed automatically.
    }
}
