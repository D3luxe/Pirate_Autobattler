using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Core;
using PirateRoguelike.Data;
using Pirate.MapGen;

namespace PirateRoguelike.UI
{
    public class PortController : MonoBehaviour
    {
        private EncounterSO currentEncounter;

        void Start()
        {
            // 1. Determine the current encounter
            if (GameSession.DebugEncounterToLoad != null)
            {
                currentEncounter = GameSession.DebugEncounterToLoad;
                Debug.Log("PortController: Loaded debug encounter.");
            }
            else if (MapManager.Instance != null && GameSession.CurrentRunState != null)
            {
                var mapData = MapManager.Instance.GetMapGraphData();
                var currentNode = mapData.nodes.Find(n => n.id == GameSession.CurrentRunState.currentEncounterId);
                currentEncounter = GameDataRegistry.GetEncounter(currentNode.id);
            }

            if (currentEncounter == null)
            {
                Debug.LogError("PortController: Could not determine the current encounter. Returning to Run scene.");
                ReturnToMap();
                return;
            }

            // 2. Apply healing
            if (GameSession.PlayerShip != null)
            {
                float healAmount = GameSession.PlayerShip.Def.baseMaxHealth * currentEncounter.portHealPercent;
                GameSession.PlayerShip.Heal((int)healAmount);
                Debug.Log($"Player ship healed by {(int)healAmount} HP.");
            }
            else
            {
                Debug.LogError("PortController: PlayerShip is null. Cannot apply healing.");
            }
        }

        // 3. Provide a way to return to the map
        public void ReturnToMap()
        {
            SceneManager.LoadScene("Run");
        }
    }
}
