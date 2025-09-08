using UnityEngine;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "GainResourceAction", menuName = "Event Actions/Gain Resource")]
    public class GainResourceAction : EventChoiceAction
    {
        public enum ResourceType
        {
            Gold,
            Lives
        }

        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int amount;

        public override void Execute(Core.PlayerContext context)
        {
            if (context.Economy == null)
            {
                Debug.LogError("EconomyService not available in PlayerContext for GainResourceAction.");
                return;
            }

            switch (resourceType)
            {
                case ResourceType.Gold:
                    context.Economy.AddGold(amount);
                    Debug.Log($"Gained {amount} gold.");
                    break;
                case ResourceType.Lives:
                    context.Economy.AddLives(amount);
                    Debug.Log($"Gained {amount} lives.");
                    break;
            }
        }
    }
}
