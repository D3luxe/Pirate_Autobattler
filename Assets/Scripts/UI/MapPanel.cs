using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using PirateRoguelike.Data;
using System.Linq; // Added for Max extension method
using UnityEngine.UIElements.StyleSheets; // Added for AngleUnit

public class MapPanel : MonoBehaviour
{
    public VisualTreeAsset mapPanelAsset;
    public VisualTreeAsset nodeButtonAsset;

    private VisualElement _root;
    private VisualElement _mapNodesContainer; // New: for #MapNodes
    private VisualElement _mapLinesContainer; // New: for #MapLines

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.Clear();
        mapPanelAsset.CloneTree(_root);
        // Query the new containers
        _mapNodesContainer = _root.Q<VisualElement>("MapNodes");
        _mapLinesContainer = _root.Q<VisualElement>("MapLines");

        GenerateMapVisuals();
    }

    public void Show()
    {
        _root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        _root.style.display = DisplayStyle.None;
    }

    public bool IsVisible()
    {
        return _root.style.display == DisplayStyle.Flex;
    }

    private void GenerateMapVisuals()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager not found!");
            return;
        }

        var mapNodes = MapManager.Instance.GetMapNodes();
        if (mapNodes == null || mapNodes.Count == 0)
        {
            Debug.LogError("Map data not found or empty!");
            return;
        }

        // Clear existing nodes and lines
        _mapNodesContainer.Clear();
        _mapLinesContainer.Clear();

        // Step 1: Get actual node dimensions asynchronously
        if (nodeButtonAsset != null)
        {
            var tempNode = nodeButtonAsset.CloneTree();
            _mapNodesContainer.Add(tempNode); // Temporarily add to get resolvedStyle

            tempNode.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float actualNodeWidth = tempNode.resolvedStyle.width;
                float actualNodeHeight = tempNode.resolvedStyle.height;
                Debug.Log($"MapPanel Debug: actualNodeWidth: {actualNodeWidth}, actualNodeHeight: {actualNodeHeight}");
                _mapNodesContainer.Remove(tempNode); // Remove after getting dimensions

                // Only proceed if dimensions are valid
                if (!float.IsNaN(actualNodeWidth) && !float.IsNaN(actualNodeHeight))
                {
                    // Step 2: Now that we have node dimensions, schedule the main map generation
                    _mapNodesContainer.schedule.Execute(() => // Schedule main map generation
                    {
                        float containerWidth = _mapNodesContainer.resolvedStyle.width;
                        float containerHeight = _mapNodesContainer.resolvedStyle.height;

                        float horizontalGap = (mapNodes.Count > 1) ? (containerWidth - (mapNodes.Count * actualNodeWidth)) / (mapNodes.Count - 1) : 0;
                        float verticalGap = (mapNodes.Max(col => col.Count) > 1) ? (containerHeight - (mapNodes.Max(col => col.Count) * actualNodeHeight)) / (mapNodes.Max(col => col.Count) - 1) : 0;
                        Debug.Log($"MapPanel Debug: horizontalGap: {horizontalGap}, verticalGap: {verticalGap}");

                        // Create node visuals and add them to the container
                        List<VisualElement> currentNodeElements = new List<VisualElement>(); // Local list
                        for (int i = 0; i < mapNodes.Count; i++)
                        {
                            for (int j = 0; j < mapNodes[i].Count; j++)
                            {
                                var nodeData = mapNodes[i][j];
                                var nodeElement = nodeButtonAsset.CloneTree();
                                var button = nodeElement.Q<Button>();
                                button.text = nodeData.encounter.type.ToString();
                                button.clicked += () => OnNodeClicked(nodeData);

                                // Handle icon
                                var iconImage = nodeElement.Q<Image>("NodeIcon"); // Assuming an Image element named "NodeIcon" in the UXML
                                if (iconImage != null && !string.IsNullOrEmpty(nodeData.iconPath))
                                {
                                    Sprite iconSprite = Resources.Load<Sprite>(nodeData.iconPath);
                                    if (iconSprite != null)
                                    {
                                        iconImage.sprite = iconSprite;
                                        iconImage.style.display = DisplayStyle.Flex; // Show the icon
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"MapPanel: Could not load icon sprite at path: {nodeData.iconPath}");
                                        iconImage.style.display = DisplayStyle.None; // Hide if not found
                                    }
                                }
                                else if (iconImage != null)
                                {
                                    iconImage.style.display = DisplayStyle.None; // Hide if no icon path
                                }

                                // Handle tooltip
                                var tooltipLabel = nodeElement.Q<Label>("NodeTooltip"); // Assuming a Label element named "NodeTooltip" in the UXML
                                if (tooltipLabel != null)
                                {
                                    if (!string.IsNullOrEmpty(nodeData.tooltipText))
                                    {
                                        tooltipLabel.text = nodeData.tooltipText;
                                        tooltipLabel.style.display = DisplayStyle.None; // Hidden by default
                                        nodeElement.RegisterCallback<PointerEnterEvent>(evt => tooltipLabel.style.display = DisplayStyle.Flex);
                                        nodeElement.RegisterCallback<PointerLeaveEvent>(evt => tooltipLabel.style.display = DisplayStyle.None);
                                    }
                                    else
                                    {
                                        tooltipLabel.style.display = DisplayStyle.None; // Hide if no tooltip text
                                    }
                                }

                                _mapNodesContainer.Add(nodeElement); // Add to #MapNodes
                                currentNodeElements.Add(nodeElement); // Add to the local list
                                
                                // Position the node dynamically
                                nodeElement.style.position = Position.Absolute;
                                nodeElement.style.left = i * (actualNodeWidth + horizontalGap);
                                nodeElement.style.top = j * (actualNodeHeight + verticalGap);
                                Debug.Log($"MapPanel Debug: Node (i:{i}, j:{j}) - Left: {nodeElement.style.left.value.ToString()}, Top: {nodeElement.style.top.value.ToString()}");
                            }
                        }
                        Debug.Log($"MapPanel Debug: currentNodeElements populated. Count: {currentNodeElements.Count}");
                        int expectedNodeCount = 0;
                        foreach (var column in mapNodes)
                        {
                            expectedNodeCount += column.Count;
                        }
                        Debug.Log($"MapPanel Debug: Expected currentNodeElements count based on mapNodes: {expectedNodeCount}");

                        // Draw lines
                        int currentColumnNodeCount = 0; // Tracks total nodes processed so far for offset calculation
                        for (int i = 0; i < mapNodes.Count - 1; i++)
                        {
                            // Calculate the offset for the current column
                            int startNodeColumnOffset = currentColumnNodeCount;

                            // Update total nodes processed for the next iteration
                            currentColumnNodeCount += mapNodes[i].Count;

                            for (int j = 0; j < mapNodes[i].Count; j++)
                            {
                                // Correct indexing for startNode
                                var startNode = currentNodeElements[startNodeColumnOffset + j];

                                // Get startPos directly (resolvedStyle should be available here)
                                var startPos = new Vector2(startNode.resolvedStyle.left + startNode.resolvedStyle.width / 2, startNode.resolvedStyle.top + startNode.resolvedStyle.height / 2);

                                // Add null check for nextNodeIndices
                                if (mapNodes[i][j].nextNodeIndices != null)
                                {
                                    foreach (var nextNodeIndex in mapNodes[i][j].nextNodeIndices)
                                    {
                                        // Calculate the absolute index of the endNode in currentNodeElements
                                        int endNodeAbsoluteIndex = currentColumnNodeCount + nextNodeIndex;

                                        var endNode = currentNodeElements[endNodeAbsoluteIndex];

                                        // Get endPos directly (resolvedStyle should be available here)
                                        var endPos = new Vector2(endNode.resolvedStyle.left + endNode.resolvedStyle.width / 2, endNode.resolvedStyle.top + endNode.resolvedStyle.height / 2);

                                        // Calculate direction vector
                                        Vector2 direction = endPos - startPos;

                                        // Only draw line if there's a valid direction
                                        if (direction.sqrMagnitude > 0.0001f) // Check if vector is not zero or very close to zero
                                        {
                                            var line = new VisualElement();
                                            line.style.position = Position.Absolute;
                                            line.style.backgroundColor = Color.white;
                                            line.style.width = Vector2.Distance(startPos, endPos);
                                            line.style.height = 2;
                                            line.style.left = startPos.x;
                                            line.style.top = startPos.y;
                                            line.style.rotate = new Rotate(new Angle(Vector2.SignedAngle(Vector2.right, direction), AngleUnit.Degree));

                                            _mapLinesContainer.Add(line); // Add to #MapLines
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"MapPanel Warning: nextNodeIndices is null for node at column {i}, row {j}");
                                }
                            }
                        }
                    }); // end _mapNodesContainer.schedule.Execute for main map generation
                }
                else
                {
                    Debug.LogError("MapPanel Error: Failed to get valid node dimensions.");
                }
            }); // end tempNode.RegisterCallback
        }
        else
        {
            Debug.LogWarning("NodeButtonAsset is null. Cannot determine node dimensions.");
        }
    }

    private void OnNodeClicked(MapNodeData nodeData)
    {
        if (GameSession.CurrentRunState != null)
        {
            if (GameSession.CurrentRunState.currentColumnIndex == nodeData.columnIndex - 1)
            {
                GameSession.CurrentRunState.currentEncounterId = nodeData.encounter.id;
                GameSession.CurrentRunState.currentColumnIndex = nodeData.columnIndex;

                Hide();

                if (nodeData.encounter.type == EncounterType.Boss)
                {
                    Debug.Log("Triggering Boss Buildup!");
                    // TODO: Implement actual visual/audio boss buildup
                }

                UnityEngine.SceneManagement.SceneManager.LoadScene(nodeData.encounter.type.ToString());
            }
            else
            {
                Debug.LogWarning($"This node is not in the next column! Player column: {GameSession.CurrentRunState.currentColumnIndex}, Node column: {nodeData.columnIndex}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot start encounter, GameSession is not active.");
        }
    }
}
