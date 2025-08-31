using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private VisualTreeAsset _shipDisplayElementUXML;
        [SerializeField] private VisualTreeAsset _slotElementUXML;
        [SerializeField] private VisualTreeAsset _shopItemElementUXML;

        public VisualTreeAsset ShipDisplayElementUXML => _shipDisplayElementUXML;
        public VisualTreeAsset SlotElementUXML => _slotElementUXML;
        public VisualTreeAsset ShopItemElementUXML => _shopItemElementUXML;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}