using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    [CreateAssetMenu(fileName = "UIAssetRegistry", menuName = "Pirate/UI/UI Asset Registry")]
    public class UIAssetRegistry : ScriptableObject
    {
        [SerializeField] private VisualTreeAsset _shipDisplayElementUXML;
        [SerializeField] private VisualTreeAsset _slotElementUXML;
        [SerializeField] private VisualTreeAsset _itemElementUXML;

        public VisualTreeAsset ShipDisplayElementUXML => _shipDisplayElementUXML;
        public VisualTreeAsset SlotElementUXML => _slotElementUXML;
        public VisualTreeAsset ItemElementUXML => _itemElementUXML;
    }
}
