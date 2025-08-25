namespace PirateRoguelike.Data
{
    public static class ActionTypeExtensions
    {
        public static int GetPriority(this ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Buff: return 0;
                case ActionType.Damage: return 1;
                case ActionType.Heal: return 2;
                case ActionType.Shield: return 3;
                case ActionType.Debuff: return 4;
                case ActionType.StatChange: return 5; // Placeholder priority
                case ActionType.Meta: return 6; // Placeholder priority
                case ActionType.Burn: return 7; // Burn and Poison are damage over time, so they should apply after direct damage
                case ActionType.Poison: return 8;
                case ActionType.Stun: return 9; // Stun should apply last, as it affects subsequent actions
                default: return 100; // Fallback for unhandled types
            }
        }
    }
}
