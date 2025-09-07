using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class GlobalUIService : MonoBehaviour
    {
        private UIDocument _globalUIOverlayDocument;

        public VisualElement GlobalUIRoot => _globalUIOverlayDocument?.rootVisualElement;

        public void Initialize(UIDocument globalUIOverlayDocument)
        {
            _globalUIOverlayDocument = globalUIOverlayDocument;
        }
    }
}
