using UnityEngine;
using UnityEngine.UIElements;
using System.ComponentModel;
using PirateRoguelike.Data;

namespace PirateRoguelike.UI.Components
{
    public class ItemElement : VisualElement
    {
        private Image _icon;
        private VisualElement _rarityFrame;
        private VisualElement _cooldownOverlay;
        private ItemInstance _itemInstance;
        public ISlotViewData SlotViewData { get; set; } // New field

        public Image IconElement => _icon; // New public property

        public ItemElement()
        {
            // Load UXML and add to hierarchy
            var visualTree = Resources.Load<VisualTreeAsset>("UI/Components/ItemElement"); // Assuming ItemElement.uxml is in Resources
            visualTree.CloneTree(this);

            _icon = this.Q<Image>("item-icon");
            _rarityFrame = this.Q<VisualElement>("rarity-frame");
            _cooldownOverlay = this.Q<VisualElement>("cooldown-overlay");
        }

        public void Bind(ItemInstance item)
        {
            if (_itemInstance != null)
            {
                // Unsubscribe from old item's property changes if needed
            }

            _itemInstance = item;

            if (_itemInstance != null)
            {
                _icon.sprite = _itemInstance.Def.icon;
                // Set rarity frame background based on rarity
                _rarityFrame.style.backgroundImage = new StyleBackground(GetRarityFrameSprite(_itemInstance.Def.rarity));
                // Set cooldown overlay visibility
                _cooldownOverlay.style.visibility = Visibility.Hidden; // Placeholder for now

                // Subscribe to item's property changes if needed
            }
            else
            {
                _icon.sprite = null;
                _rarityFrame.style.backgroundImage = StyleKeyword.None;
                _cooldownOverlay.style.visibility = Visibility.Hidden;
            }
        }

        private Sprite GetRarityFrameSprite(Rarity rarity)
        {
            // This needs to be implemented based on your project's asset loading
            // For now, return a placeholder or null
            return null; 
        }
    }
}