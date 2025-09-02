using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PirateRoguelike.Data;
using System;

namespace PirateRoguelike.UI
{
    public class RewardItemSlot : MonoBehaviour
{
    [SerializeField] public Image itemIcon;
    [SerializeField] public TextMeshProUGUI itemName;
    [SerializeField] public Button selectButton;

    private int _itemIndex;
    private RewardUIController _controller;

    public void SetItem(ItemSO item, int index)
    {
        itemIcon.sprite = item.icon;
        itemName.text = item.displayName;
        _itemIndex = index;
        // Ensure the button's onClick listener is correctly set up
        // This might need to be done in the editor or dynamically
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => _controller.OnItemChosen(_itemIndex));
    }

    // This method is needed to pass the controller reference
    public void SetController(RewardUIController controller)
    {
        _controller = controller;
    }
}
}