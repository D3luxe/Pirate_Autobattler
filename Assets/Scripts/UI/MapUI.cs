using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Add this
using PirateRoguelike.Data;

public class MapUI : MonoBehaviour
{
    public static MapUI Instance { get; private set; }

    public GameObject nodePrefab;
    public GameObject linePrefab;
    public Transform mapContainer;
    public Transform lineContainer;

    [Header("Node Colors")]
    [SerializeField] private Color eliteNodeColor = Color.red;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            
        }
    }

    public void RenderMap(List<List<MapNodeData>> mapData)
    {
        if (nodePrefab == null || mapContainer == null || linePrefab == null || lineContainer == null)
        {
            Debug.LogError("MapUI is not set up correctly. Assign all prefabs and containers.");
            return;
        }

        if (mapContainer != null)
        {
            foreach (Transform child in mapContainer) Destroy(child.gameObject);
        }
        if (lineContainer != null)
        {
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
        }

        var instantiatedNodes = new List<List<MapNode>>();

        RectTransform mapContainerRect = mapContainer.GetComponent<RectTransform>();
        if (mapContainerRect == null)
        {
            Debug.LogError("mapContainer does not have a RectTransform component.");
            return;
        }

        // Get the actual width and height of the mapContainer at runtime
        float actualMapContainerWidth = mapContainerRect.rect.width;
        float actualMapContainerHeight = mapContainerRect.rect.height;

        float maxNodesInColumn = mapData.Max(column => column.Count);
        
        // Calculate columnWidth and nodeHeight dynamically based on actual mapContainer size
        float columnWidth = actualMapContainerWidth / mapData.Count;
        float nodeHeight = actualMapContainerHeight / maxNodesInColumn; 
        float horizontalJitter = 50f; // Keep jitter for visual variety

        // Calculate total rendered width and height of the map for centering
        float mapTotalRenderedWidth = mapData.Count * columnWidth;
        float mapTotalRenderedHeight = maxNodesInColumn * nodeHeight;

        // First Pass: Instantiate all the UI nodes
        for (int i = 0; i < mapData.Count; i++)
        {
            instantiatedNodes.Add(new List<MapNode>());
            // Calculate yOffset to center nodes vertically within their column
            float yOffset = (actualMapContainerHeight - (mapData[i].Count * nodeHeight)) / 2f; 
            
            // Calculate xPos relative to the mapContainer's center, ensuring the whole map is centered
            float xPos = (i * columnWidth) - (mapTotalRenderedWidth / 2f) + (columnWidth / 2f); 

            for (int j = 0; j < mapData[i].Count; j++)
            {
                GameObject nodeInstance = Instantiate(nodePrefab, mapContainer);
                float xJitter = Random.Range(-horizontalJitter, horizontalJitter);
                // Calculate yPos relative to the mapContainer's center, ensuring the whole map is centered
                float yPos = yOffset + j * nodeHeight + (nodeHeight / 2f) - (mapTotalRenderedHeight / 2f); 
                nodeInstance.transform.localPosition = new Vector3(xPos + xJitter, yPos, 0);

                MapNode uiNode = nodeInstance.GetComponent<MapNode>();
                MapNodeData dataNode = mapData[i][j];
                
                uiNode.encounter = dataNode.encounter;
                uiNode.columnIndex = dataNode.columnIndex;

                TextMeshProUGUI buttonText = uiNode.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    if (uiNode.encounter.type == EncounterType.Event)
                    {
                        buttonText.text = $"{uiNode.encounter.type}"; // Just show "Event"
                    }
                    else
                    {
                        buttonText.text = string.Format("{0}\n{1}", uiNode.encounter.type, uiNode.encounter.eventTitle);
                    }
                }

                // Mark elite nodes
                if (dataNode.isElite)
                {
                    if (uiNode.button != null) uiNode.button.image.color = eliteNodeColor;
                }

                instantiatedNodes[i].Add(uiNode);
            }
        }

        // Second Pass: Connect the UI nodes and draw lines
        for (int i = 0; i < mapData.Count - 1; i++)
        {
            for (int j = 0; j < mapData[i].Count; j++)
            {
                MapNodeData dataNode = mapData[i][j];
                MapNode uiNode = instantiatedNodes[i][j];

                foreach (int nextNodeIndex in dataNode.nextNodeIndices)
                {
                    MapNode nextUiNode = instantiatedNodes[i + 1][nextNodeIndex];
                    DrawLine(uiNode.transform.localPosition, nextUiNode.transform.localPosition);
                }
            }
        }

        // Third Pass: Set node interactivity based on player progress
        int playerColumn = GameSession.CurrentRunState != null ? GameSession.CurrentRunState.currentColumnIndex : -1;
        Debug.Log($"Player is at column: {playerColumn}. Enabling column {playerColumn + 1}");
        for (int i = 0; i < instantiatedNodes.Count; i++)
        {
            bool isNextColumn = (i == playerColumn + 1);
            foreach (MapNode uiNode in instantiatedNodes[i])
            {
                uiNode.button.interactable = isNextColumn;
                var colors = uiNode.button.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                uiNode.button.colors = colors;
            }
        }
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineGO = Instantiate(linePrefab, lineContainer);
        RectTransform lineRect = lineGO.GetComponent<RectTransform>();
        
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        
        lineRect.sizeDelta = new Vector2(distance, 5f);
        lineRect.pivot = new Vector2(0, 0.5f);
        lineRect.localPosition = start;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        //Debug.Log($"Drawing line from {start} to {end}. Line local position: {lineRect.localPosition}");
    }

    public void PlaySailingAnimation()
    {
        Debug.Log("Playing sailing animation...");
        // TODO: Implement actual sailing animation (e.g., fade to black, ship moving across screen)
    }
}

