using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using Pirate.MapGen;
using PirateRoguelike.Services;
using PirateRoguelike.Saving;
using System.Collections.Generic;

namespace PirateRoguelike.UI
{
    public class EventController : MonoBehaviour
    {
        [Tooltip("The default UXML asset to use for events that don't specify a custom one.")]
        [SerializeField] private VisualTreeAsset m_DefaultEventUxml;

        private EncounterSO m_CurrentEncounter;

        // UI Elements
        private VisualElement m_EventRoot;
        private VisualElement m_ChoiceView;
        private VisualElement m_OutcomeView;

        private Label m_TitleLabel;
        private Label m_DescriptionLabel;
        private Label m_OutcomeTextLabel;

        private VisualElement m_ChoicesContainer;
        private Button m_ContinueButton;


        void Start()
        {
            // 1. Determine the current encounter
            if (GameSession.DebugEncounterToLoad != null)
            {
                m_CurrentEncounter = GameSession.DebugEncounterToLoad;
                GameSession.DebugEncounterToLoad = null; // Consume the debug encounter
                Debug.Log("EventController: Loaded debug encounter.");
            }
            else if (GameSession.CurrentRunState != null && !string.IsNullOrEmpty(GameSession.CurrentRunState.currentEncounterId))
            {
                m_CurrentEncounter = GameDataRegistry.GetEncounter(GameSession.CurrentRunState.currentEncounterId);
            }

            if (m_CurrentEncounter == null)
            {
                Debug.LogError("EventController: Could not determine the current encounter. Returning to Run scene.");
                ReturnToMap();
                return;
            }

            // 2. Get the global UI root
            var globalRoot = ServiceLocator.Resolve<GlobalUIService>().GlobalUIRoot;
            if (globalRoot == null)
            {
                 Debug.LogError("EventController: GlobalUIService or its GlobalUIRoot is not available. Returning to Run scene.");
                ReturnToMap();
                return;
            }

            // 3. Instantiate and populate the UI from UXML
            var uxmlToLoad = m_CurrentEncounter.eventUxml != null ? m_CurrentEncounter.eventUxml : m_DefaultEventUxml;

            if (uxmlToLoad != null)
            {
                m_EventRoot = uxmlToLoad.CloneTree();
                globalRoot.Add(m_EventRoot);

                if(m_CurrentEncounter.eventUss != null)
                {
                    m_EventRoot.styleSheets.Add(m_CurrentEncounter.eventUss);
                }

                QueryUIElements();
                PopulateChoiceView();
                RegisterCallbacks();
            }
            else
            {
                Debug.LogError($"EventController: No UXML asset provided for encounter '{m_CurrentEncounter.id}' and no default is set. Returning to Run scene.");
                ReturnToMap();
            }
        }

        private void QueryUIElements()
        {
            m_ChoiceView = m_EventRoot.Q<VisualElement>("choice-view");
            m_OutcomeView = m_EventRoot.Q<VisualElement>("outcome-view");

            m_TitleLabel = m_EventRoot.Q<Label>("event-title");
            m_DescriptionLabel = m_EventRoot.Q<Label>("event-description");
            m_ChoicesContainer = m_EventRoot.Q<VisualElement>("choice-button-group");

            m_OutcomeTextLabel = m_EventRoot.Q<Label>("outcome-text");
            m_ContinueButton = m_EventRoot.Q<Button>("continue-button");
        }


        private void PopulateChoiceView()
        {
            if (m_TitleLabel != null) m_TitleLabel.text = m_CurrentEncounter.eventTitle;
            if (m_DescriptionLabel != null) m_DescriptionLabel.text = m_CurrentEncounter.eventDescription;

            if (m_ChoicesContainer != null)
            {
                m_ChoicesContainer.Clear(); // Clear any placeholder content

                foreach (var choice in m_CurrentEncounter.eventChoices)
                {
                    var button = new Button(() => OnChoiceSelected(choice))
                    {
                        text = choice.choiceText
                    };
                    button.AddToClassList("event-choice-button"); // For styling
                    m_ChoicesContainer.Add(button);
                }
            }
        }

        private void RegisterCallbacks()
        {
            if (m_ContinueButton != null)
            {
                m_ContinueButton.RegisterCallback<ClickEvent>(evt => ReturnToMap());
            }
        }


        private void OnChoiceSelected(EventChoice choice)
        {
            if (m_ChoicesContainer != null)
            {
                m_ChoicesContainer.Query<Button>().ForEach(button => button.SetEnabled(false));
            }

            var economyService = new ConcreteEconomyService();
            var inventoryService = new ConcreteInventoryService();
            var gameSessionService = new ConcreteGameSessionService();
            var runManagerService = new ConcreteRunManagerService();

            var playerContext = new PlayerContext(economyService, inventoryService, gameSessionService, runManagerService);

            foreach (var action in choice.actions)
            {
                if (action != null) action.Execute(playerContext);
            }

            if (m_OutcomeTextLabel != null) m_OutcomeTextLabel.text = choice.outcomeText;
            if (m_ChoiceView != null) m_ChoiceView.style.display = DisplayStyle.None;
            if (m_OutcomeView != null) m_OutcomeView.style.display = DisplayStyle.Flex;
        }

        private void CleanupUI()
        {
            if (m_EventRoot != null && m_EventRoot.parent != null)
            {
                m_EventRoot.parent.Remove(m_EventRoot);
            }
        }

        public void ReturnToMap()
        {
            CleanupUI();
            SceneManager.LoadScene("Run");
        }

        void OnDestroy()
        {
            CleanupUI();
        }

        private class ConcreteEconomyService : IEconomyService
        {
            public void AddGold(int amount) => GameSession.Economy.AddGold(amount);
            public bool TrySpendGold(int amount) => GameSession.Economy.TrySpendGold(amount);
            public void AddLives(int amount) => GameSession.Economy.AddLives(amount);
            public void LoseLife() => GameSession.Economy.LoseLife();
        }

        private class ConcreteInventoryService : IInventoryService
        {
            public bool AddItem(string itemId)
            {
                ItemSO itemSO = GameDataRegistry.GetItem(itemId);
                if (itemSO == null) return false;
                return GameSession.Inventory.AddItem(new ItemInstance(itemSO));
            }

            public bool RemoveItem(string itemId)
            {
                for (int i = 0; i < GameSession.Inventory.Slots.Count; i++)
                {
                    if (GameSession.Inventory.Slots[i].Item != null && GameSession.Inventory.Slots[i].Item.Def.id == itemId)
                    {
                        GameSession.Inventory.RemoveItemAt(i);
                        return true;
                    }
                }
                return false;
            }

            public ItemInstance GetItem(string itemId)
            {
                foreach (var slot in GameSession.Inventory.Slots)
                {
                    if (slot.Item != null && slot.Item.Def.id == itemId)
                    {
                        return slot.Item;
                    }
                }
                return null;
            }
        }

        private class ConcreteGameSessionService : IGameSessionService
        {
            public ShipState PlayerShip => GameSession.PlayerShip;
            public void SetPlayerShip(ShipState newShipState) => GameSession.PlayerShip = newShipState;
            public void SetNextEncounter(string encounterId) => GameSession.CurrentRunState.currentEncounterId = encounterId;
            public void LoadRun(RunState runState) => GameSession.LoadRun(runState, GameDataRegistry.GetRunConfig());
            public void StartNewRun() => GameSession.StartNewRun(GameDataRegistry.GetRunConfig(), GameDataRegistry.GetShip("player_ship_default"));
        }

        private class ConcreteRunManagerService : IRunManagerService
        {
            public void ReturnToMap() => SceneManager.LoadScene("Run");
            public void LoadEncounter(string encounterId)
            {
                GameSession.CurrentRunState.currentEncounterId = encounterId;
                SceneManager.LoadScene("Run");
            }
        }
    }
}
