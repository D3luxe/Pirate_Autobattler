using UnityEngine;
using PirateRoguelike.Core;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "GiveShipAction", menuName = "Event Actions/Give Ship")]
    public class GiveShipAction : EventChoiceAction
    {
        [SerializeField] private string shipId;

        public override void Execute(Core.PlayerContext context)
        {
            if (context.GameSession == null)
            {
                Debug.LogError("GameSessionService not available in PlayerContext for GiveShipAction.");
                return;
            }

            if (string.IsNullOrEmpty(shipId))
            {
                Debug.LogWarning("Ship ID is empty for GiveShipAction. No ship given.");
                return;
            }

            ShipSO shipSO = GameDataRegistry.GetShip(shipId);
            if (shipSO == null)
            {
                Debug.LogError($"ShipSO with ID '{shipId}' not found in GameDataRegistry.");
                return;
            }

            ShipState newShipState = new ShipState(shipSO);
            context.GameSession.SetPlayerShip(newShipState);
            Debug.Log($"Player ship changed to: {shipSO.displayName}");
        }
    }
}
