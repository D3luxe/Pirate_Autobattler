using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using Pirate.MapGen;

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

        private void OnChoiceSelected(EventChoice choice)
        {
            // TODO: Implement the consequences of the choice based on the properties of EventChoice (e.g., goldCost, itemRewardId)
            Debug.Log($"Choice selected: {choice.choiceText}. Outcome Text: {choice.outcomeText}. Implementation of actual outcome is pending.");

            // For now, just return to the map after any choice.
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