using UnityEngine;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "ModifyStatAction", menuName = "Event Actions/Modify Stat")]
    public class ModifyStatAction : EventChoiceAction
    {
        public enum StatType
        {
            Health,
            // Add other stats here as needed, e.g., MaxHealth, Shield, etc.
        }

        [SerializeField] private StatType statType;
        [SerializeField] private int amount;

        public override void Execute(Core.PlayerContext context)
        {
            if (context.GameSession?.PlayerShip == null)
            {
                Debug.LogError("PlayerShip or GameSession not available in PlayerContext for ModifyStatAction.");
                return;
            }

            switch (statType)
            {
                case StatType.Health:
                    // Assuming ShipState has methods like TakeDamage or Heal
                    if (amount < 0)
                    {
                        context.GameSession.PlayerShip.TakeDamage(Mathf.Abs(amount));
                        Debug.Log($"Took {Mathf.Abs(amount)} damage.");
                    }
                    else
                    {
                        context.GameSession.PlayerShip.Heal(amount);
                        Debug.Log($"Healed {amount} health.");
                    }
                    break;
                // Add cases for other stat types
            }
        }
    }
}
