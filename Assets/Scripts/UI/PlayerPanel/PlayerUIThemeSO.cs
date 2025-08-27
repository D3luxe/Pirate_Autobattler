
using UnityEngine;

namespace PirateRoguelike.UI
{
    [CreateAssetMenu(fileName = "PlayerUITheme", menuName = "PirateRoguelike/UI/Player UI Theme")]
    public class PlayerUIThemeSO : ScriptableObject
    {
        [Header("Fonts")]
        public Font mainFont;

        [Header("Slots")]
        public Sprite emptySlotBackground;
        public Sprite filledSlotBackground;

        [Header("Rarity Visuals")]
        public Sprite bronzeFrame;
        public Sprite silverFrame;
        public Sprite goldFrame;
        public Sprite diamondFrame;

        [Header("Control Icons")]
        public Sprite pauseIcon;
        public Sprite settingsIcon;
        public Sprite battleSpeed1xIcon;
        public Sprite battleSpeed2xIcon;

        [Header("HUD Icons")]
        public Sprite goldIcon;
        public Sprite livesIcon;
        public Sprite depthIcon;
    }
}
