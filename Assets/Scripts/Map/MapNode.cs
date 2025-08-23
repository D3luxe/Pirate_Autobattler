using UnityEngine;
using UnityEngine.UI;
using PirateRoguelike.Data;

public class MapNode : MonoBehaviour
{
    public EncounterSO encounter;
    public Button button;
    public int columnIndex;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnNodeClicked);
        }
    }

    void OnNodeClicked()
    {
        if (GameSession.CurrentRunState != null)
        {
            if (GameSession.CurrentRunState.currentColumnIndex == columnIndex - 1)
            {
                GameSession.CurrentRunState.currentEncounterId = encounter.id;
                GameSession.CurrentRunState.currentColumnIndex = columnIndex;

                // Hide the map before loading the new scene
                if (MapUI.Instance != null)
                {
                    MapUI.Instance.gameObject.SetActive(false);
                    MapUI.Instance.PlaySailingAnimation(); // Play sailing animation
                }

                if (encounter.type == EncounterType.Boss)
                {
                    Debug.Log("Triggering Boss Buildup!");
                    // TODO: Implement actual visual/audio boss buildup
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(encounter.type.ToString());
            }
            else
            {
                Debug.LogWarning($"This node is not in the next column! Player column: {GameSession.CurrentRunState.currentColumnIndex}, Node column: {columnIndex}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot start encounter, GameSession is not active.");
        }
    }
}
