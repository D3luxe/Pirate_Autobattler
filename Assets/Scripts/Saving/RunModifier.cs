using System;

namespace PirateRoguelike.Saving
{
    [Serializable]
    public class RunModifier
    {
        public string id; // Unique ID for the modifier
        public float value; // Generic value for the modifier (e.g., percentage increase)
        public string description; // Description for display

        public RunModifier(string id, float value, string description)
        {
            this.id = id;
            this.value = value;
            this.description = description;
        }
    }
}