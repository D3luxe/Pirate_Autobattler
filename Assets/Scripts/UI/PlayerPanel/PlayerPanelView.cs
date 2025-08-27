
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateRoguelike.UI
{
    public class PlayerPanelView
    {
        private VisualElement _root;
        private VisualTreeAsset _slotTemplate;
        private PlayerUIThemeSO _theme;
        private int _battleSpeed = 1;

        // --- Queried Elements ---
        private Label _shipNameLabel;
        private VisualElement _shipSpriteElement;
        private VisualElement _hpBarForeground;
        private VisualElement _equipmentBar;
        private List<VisualElement> _slotElements = new List<VisualElement>();
        private Label _goldLabel, _livesLabel, _depthLabel;
        private Button _pauseButton, _settingsButton, _battleSpeedButton;
        private Image _battleSpeedIcon;

        public PlayerPanelView(VisualElement root, VisualTreeAsset slotTemplate, PlayerUIThemeSO theme)
        {
            _root = root;
            _slotTemplate = slotTemplate;
            _theme = theme;
            QueryElements();
            RegisterCallbacks();
        }

        private void QueryElements()
        {
            _shipNameLabel = _root.Q<Label>("ship-name");
            _shipSpriteElement = _root.Q<VisualElement>("ship-sprite");
            _hpBarForeground = _root.Q<VisualElement>("hp-bar__foreground");
            _equipmentBar = _root.Q<VisualElement>("equipment-bar");
            _goldLabel = _root.Q<Label>("gold-label");
            _livesLabel = _root.Q<Label>("lives-label");
            _depthLabel = _root.Q<Label>("depth-label");
            _pauseButton = _root.Q<Button>("pause-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _battleSpeedButton = _root.Q<Button>("battle-speed-button");
            _battleSpeedIcon = _battleSpeedButton.Q<Image>();
        }

        private void RegisterCallbacks()
        {
            _pauseButton.clicked += () => PlayerPanelEvents.OnPauseClicked?.Invoke();
            _settingsButton.clicked += () => PlayerPanelEvents.OnSettingsClicked?.Invoke();
            _battleSpeedButton.clicked += ToggleBattleSpeed;
        }

        public void BindInitialData(IPlayerPanelData data)
        {
            // Bind Ship Data
            _shipNameLabel.text = data.ShipData.ShipName;
            _shipSpriteElement.style.backgroundImage = new StyleBackground(data.ShipData.ShipSprite);
            UpdateHp(data.ShipData.CurrentHp, data.ShipData.MaxHp);

            // Bind HUD Data
            UpdateGold(data.HudData.Gold);
            UpdateLives(data.HudData.Lives);
            UpdateDepth(data.HudData.Depth);
            
            // Set initial icons from theme
            _root.Q<Image>("gold-icon").sprite = _theme.goldIcon;
            _root.Q<Image>("lives-icon").sprite = _theme.livesIcon;
            _root.Q<Image>("depth-icon").sprite = _theme.depthIcon;
            _pauseButton.Q<Image>().sprite = _theme.pauseIcon;
            _settingsButton.Q<Image>().sprite = _theme.settingsIcon;
            UpdateBattleSpeedIcon();

            // Create and Bind Slots
            UpdateInventory(data.EquipmentSlots);
        }

        public void UpdateInventory(List<ISlotViewData> slots)
        {
            _equipmentBar.Clear();
            _slotElements.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                var slotInstance = _slotTemplate.Instantiate();
                var slotElement = slotInstance.Q<VisualElement>("slot");
                slotElement.userData = slots[i].SlotId;
                _equipmentBar.Add(slotElement);
                _slotElements.Add(slotElement);
                
                slotElement.AddManipulator(new SlotManipulator(slots[i].SlotId));

                BindSlot(slotElement, slots[i]);
            }
        }

        private void BindSlot(VisualElement slotElement, ISlotViewData slotData)
        {
            var icon = slotElement.Q<Image>("icon");
            icon.sprite = slotData.IsEmpty ? null : slotData.Icon;
            icon.style.visibility = slotData.IsEmpty ? Visibility.Hidden : Visibility.Visible;

            slotElement.Q<VisualElement>("cooldown-overlay").style.scale = new Scale(new Vector2(1, slotData.CooldownPercent));

            slotElement.ClearClassList();
            slotElement.AddToClassList("slot");

            if (slotData.IsEmpty) slotElement.AddToClassList("slot--empty");
            if (slotData.IsDisabled) slotElement.AddToClassList("slot--disabled");
            if (slotData.IsPotentialMergeTarget) slotElement.AddToClassList("slot--merge");

            if (!string.IsNullOrEmpty(slotData.Rarity))
            {
                slotElement.AddToClassList($"rarity--{slotData.Rarity.ToLower()}");
            }
        }

        public void UpdateHp(float current, float max)
        {
            float percentage = (max > 0) ? (current / max) * 100f : 0f;
            _hpBarForeground.style.width = new Length(percentage, LengthUnit.Percent);
        }

        public void UpdateGold(int amount) => _goldLabel.text = amount.ToString();
        public void UpdateLives(int amount) => _livesLabel.text = amount.ToString();
        public void UpdateDepth(int amount) => _depthLabel.text = amount.ToString();

        private void ToggleBattleSpeed()
        {
            _battleSpeed = _battleSpeed == 1 ? 2 : 1;
            UpdateBattleSpeedIcon();
            PlayerPanelEvents.OnBattleSpeedChanged?.Invoke(_battleSpeed);
        }

        private void UpdateBattleSpeedIcon()
        {
            _battleSpeedIcon.sprite = _battleSpeed == 1 ? _theme.battleSpeed1xIcon : _theme.battleSpeed2xIcon;
        }
    }
}
