using UnityEngine;

namespace PirateRoguelike.UI
{
    public class ShopController : MonoBehaviour
    {
        // This class will be responsible for managing the Shop UI.

        // 1. It will need a reference to the UIDocument for the shop panel.
        // 2. It will query for the item slots within the shop panel.
        // 3. For each slot, it will need to register a PointerClickEvent.
        // 4. The callback for the click event will call the ItemManipulationService:
        //    ItemManipulationService.Instance.RequestPurchase(shopSlotId, null);
        //    (The null indicates a click-to-buy, where the service will find the first available slot).
        // 5. The SlotManipulator will also be used on shop items, and its OnPointerUp
        //    will call ItemManipulationService.Instance.RequestPurchase(shopSlotId, playerSlotId).
    }
}
