using UnityEngine;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "GiveItemAction", menuName = "Event Actions/Give Item")]
    public class GiveItemAction : EventChoiceAction
    {
        [SerializeField] private string itemId;

        public override void Execute(Core.PlayerContext context)
        {
            if (context.Inventory == null)
            {
                Debug.LogError("InventoryService not available in PlayerContext for GiveItemAction.");
                return;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("Item ID is empty for GiveItemAction. No item given.");
                return;
            }

            if (context.Inventory.AddItem(itemId))
            {
                Debug.Log($"Successfully added item: {itemId}");
            }
            else
            {
                Debug.LogWarning($"Failed to add item: {itemId}");
            }
        }
    }
}
