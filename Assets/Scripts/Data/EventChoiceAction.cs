using UnityEngine;

namespace PirateRoguelike.Data
{
    public abstract class EventChoiceAction : ScriptableObject
    {
        public abstract void Execute(Core.PlayerContext context);
    }
}
