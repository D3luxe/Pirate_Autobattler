using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using Pirate.MapGen;
using PirateRoguelike.Services;
using PirateRoguelike.Saving;

namespace PirateRoguelike.UI
{
    public class EventController : MonoBehaviour
    {
        private EncounterSO currentEncounter;
        private VisualElement eventRoot; // The root of the cloned UXML

        void Start()
        {
            // 1. Determine the current encounter
            if (GameSession.DebugEncounterToLoad != null)
            {
                currentEncounter = GameSession.DebugEncounterToLoad;
                Debug.Log("EventController: Loaded debug encounter.");
            }
            else if (MapManager.Instance != null && GameSession.CurrentRunState != null)
            {
                var mapData = MapManager.Instance.GetMapGraphData();
                var currentNode = mapData.nodes.Find(n => n.id == GameSession.CurrentRunState.currentEncounterId);
                currentEncounter = GameDataRegistry.GetEncounter(currentNode.id);
            }

            if (currentEncounter == null)
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
            if (currentEncounter.eventUxml != null)
            {
                eventRoot = currentEncounter.eventUxml.CloneTree();
                globalRoot.Add(eventRoot);

                if(currentEncounter.eventUss != null)
                {
                    eventRoot.styleSheets.Add(currentEncounter.eventUss);
                }

                PopulateUI();
            }
            else
            {
                Debug.LogError($"EventController: Encounter '{currentEncounter.id}' is missing an eventUxml asset. Returning to Run scene.");
                ReturnToMap();
            }
        }

        private void PopulateUI()
        {
            var titleLabel = eventRoot.Q<Label>("title-label");
            var descriptionLabel = eventRoot.Q<Label>("description-label");
            var choicesContainer = eventRoot.Q<VisualElement>("choices-container");

            if (titleLabel != null) titleLabel.text = currentEncounter.eventTitle;
            if (descriptionLabel != null) descriptionLabel.text = currentEncounter.eventDescription;

            if (choicesContainer != null)
            {
                choicesContainer.Clear(); // Clear any placeholder content

                // Create a button for each choice
                foreach (var choice in currentEncounter.eventChoices)
                {
                    var button = new Button(() => OnChoiceSelected(choice))
                    {
                        text = choice.choiceText
                    };
                    button.AddToClassList("event-choice-button"); // For styling
                    choicesContainer.Add(button);
                }
            }
        }

        // Temporary concrete implementations for PlayerContext services
    // These should ideally be properly injected or resolved via a ServiceLocator
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
            // Find the item in inventory and remove it
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
            // Find the item in inventory and return it
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
        public void LoadRun(Saving.RunState runState) => GameSession.LoadRun(runState, GameDataRegistry.GetRunConfig());
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

        private void OnChoiceSelected(EventChoice choice)
        {
            var economyService = new ConcreteEconomyService();
            var inventoryService = new ConcreteInventoryService();
            var gameSessionService = new ConcreteGameSessionService();
            var runManagerService = new ConcreteRunManagerService();

            var playerContext = new PlayerContext(economyService, inventoryService, gameSessionService, runManagerService);

            foreach (var action in choice.actions)
            {
                action.Execute(playerContext);
            }

            ReturnToMap();
        }

        private void CleanupUI()
        {
            if (eventRoot != null && eventRoot.parent != null)
            {
                eventRoot.parent.Remove(eventRoot);
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
    }
}