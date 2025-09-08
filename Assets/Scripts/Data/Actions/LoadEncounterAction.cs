using UnityEngine;

namespace PirateRoguelike.Data.Actions
{
    [CreateAssetMenu(fileName = "LoadEncounterAction", menuName = "Event Actions/Load Encounter")]
    public class LoadEncounterAction : EventChoiceAction
    {
        [SerializeField] private string encounterId;

        public override void Execute(Core.PlayerContext context)
        {
            if (context.RunManager == null)
            {
                Debug.LogError("RunManagerService not available in PlayerContext for LoadEncounterAction.");
                return;
            }

            if (string.IsNullOrEmpty(encounterId))
            {
                Debug.LogWarning("Encounter ID is empty for LoadEncounterAction. No encounter loaded.");
                return;
            }

            context.RunManager.LoadEncounter(encounterId);
            Debug.Log($"Loading encounter: {encounterId}");
        }
    }
}
