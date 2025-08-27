using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using PirateRoguelike.Data;
using System.Linq;
using UnityEngine.UIElements.StyleSheets;
using Pirate.MapGen;

public class MapPanel : MonoBehaviour
{
    public VisualTreeAsset mapPanelAsset;
    public VisualTreeAsset nodeButtonAsset;

    private VisualElement _root;
    private VisualElement _mapNodesContainer;
    private VisualElement _mapLinesContainer;
    private VisualElement _pannableMapContent;
    private VisualElement _mapBackgroundSprite;
    private List<List<MapNodeData>> _mapNodes; // Make mapNodes a class member

    private bool _isDragging = false;
    private Vector2 _lastMousePosition;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.Clear();
        mapPanelAsset.CloneTree(_root);
        _mapNodesContainer = _root.Q<VisualElement>("MapNodes");
        _mapLinesContainer = _root.Q<VisualElement>("MapLines");
        _pannableMapContent = _root.Q<VisualElement>("PannableMapContent");
        _mapBackgroundSprite = _root.Q<VisualElement>("MapBackgroundSprite");

        _pannableMapContent.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _pannableMapContent.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _pannableMapContent.RegisterCallback<PointerUpEvent>(OnPointerUp);

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

    private const float NODE_WIDTH = 100f;
    private const float NODE_HEIGHT = 100f;
    private const float HORIZONTAL_PADDING = 20f;
    private const float VERTICAL_PADDING = 20f;

    private void GenerateMapVisuals()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager not found!");
            return;
        }

        MapManager.Instance.GenerateMapIfNeeded();

        _mapNodes = MapManager.Instance.GetMapNodes();
        if (_mapNodes == null || _mapNodes.Count == 0)
        {
            Debug.LogError("Map data not found or empty!");
            return;
        }

                _mapNodesContainer.Clear();
        _mapLinesContainer.Clear();

        // No need for asynchronous measurement anymore, as node dimensions are fixed by USS
        _mapNodesContainer.schedule.Execute(() =>
        {
            List<VisualElement> currentNodeElements = new List<VisualElement>();
            for (int i = 0; i < _mapNodes.Count; i++)
            {
                for (int j = 0; j < _mapNodes[i].Count; j++)
                {
                    var nodeData = _mapNodes[i][j];
                    var nodeElement = nodeButtonAsset.CloneTree();
                    nodeElement.AddToClassList("map-node"); // Apply the fixed size style
                    var button = nodeElement.Q<Button>();
                    button.text = nodeData.encounter.type.ToString();
                    button.clicked += () => OnNodeClicked(nodeData);

                    var iconImage = nodeElement.Q<Image>("NodeIcon");
                    if (iconImage != null && !string.IsNullOrEmpty(nodeData.iconPath))
                    {
                        Sprite iconSprite = Resources.Load<Sprite>(nodeData.iconPath);
                        if (iconSprite != null)
                        {
                            iconImage.sprite = iconSprite;
                            iconImage.style.display = DisplayStyle.Flex;
                        }
                        else
                        {
                            Debug.LogWarning($"MapPanel: Could not load icon sprite at path: {nodeData.iconPath}");
                            iconImage.style.display = DisplayStyle.None;
                        }
                    }
                    else if (iconImage != null)
                    {
                        iconImage.style.display = DisplayStyle.None;
                    }

                    var tooltipLabel = nodeElement.Q<Label>("NodeTooltip");
                    if (tooltipLabel != null)
                    {
                        if (!string.IsNullOrEmpty(nodeData.tooltipText))
                        {
                            tooltipLabel.text = nodeData.tooltipText;
                            tooltipLabel.style.display = DisplayStyle.None;
                            nodeElement.RegisterCallback<PointerEnterEvent>(evt => tooltipLabel.style.display = DisplayStyle.Flex);
                            nodeElement.RegisterCallback<PointerLeaveEvent>(evt => tooltipLabel.style.display = DisplayStyle.None);
                        }
                        else
                        {
                            tooltipLabel.style.display = DisplayStyle.None;
                        }
                    }

                    _mapNodesContainer.Add(nodeElement);
                    currentNodeElements.Add(nodeElement);
                    
                    nodeElement.style.position = Position.Absolute;
                    nodeElement.style.left = 10f + nodeData.rowIndex * (NODE_WIDTH + HORIZONTAL_PADDING);
                    nodeElement.style.top = 10f + (_mapNodes.Count - 1 - nodeData.columnIndex) * (NODE_HEIGHT + VERTICAL_PADDING);
                    Debug.Log($"MapPanel Debug: Node (i:{nodeData.columnIndex}, j:{nodeData.rowIndex}) - Left: {nodeElement.style.left.value.ToString()}, Top: {nodeElement.style.top.value.ToString()}");
                }
            }
            
            int expectedNodeCount = 0;
            foreach (var column in _mapNodes)
            {
                expectedNodeCount += column.Count;
            }
            Debug.Log($"MapPanel Debug: Expected currentNodeElements count based on _mapNodes: {expectedNodeCount}");

            // Draw lines
            int currentColumnNodeCount = 0;
            for (int i = 0; i < _mapNodes.Count - 1; i++)
            {
                int startNodeColumnOffset = currentColumnNodeCount;
                currentColumnNodeCount += _mapNodes[i].Count;

                for (int j = 0; j < _mapNodes[i].Count; j++)
                {
                    var startNode = currentNodeElements[startNodeColumnOffset + j];
                    var startPos = new Vector2(startNode.resolvedStyle.left + startNode.resolvedStyle.width / 2, startNode.resolvedStyle.top + startNode.resolvedStyle.height / 2);

                    if (_mapNodes[i][j].nextNodeIndices != null)
                    {
                        foreach (var nextNodeIndex in _mapNodes[i][j].nextNodeIndices)
                        {
                            int endNodeAbsoluteIndex = currentColumnNodeCount + nextNodeIndex;
                            var endNode = currentNodeElements[endNodeAbsoluteIndex];
                            var endPos = new Vector2(endNode.resolvedStyle.left + endNode.resolvedStyle.width / 2, endNode.resolvedStyle.top + endNode.resolvedStyle.height / 2);

                            Vector2 direction = endPos - startPos;

                            if (direction.sqrMagnitude > 0.0001f)
                            {
                                var line = new VisualElement();
                                line.style.position = Position.Absolute;
                                line.style.backgroundColor = Color.white;
                                line.style.width = Vector2.Distance(startPos, endPos);
                                line.style.height = 2;
                                line.style.left = startPos.x;
                                line.style.top = startPos.y;
                                line.style.rotate = new Rotate(new Angle(Vector2.SignedAngle(Vector2.right, direction), AngleUnit.Degree));

                                _mapLinesContainer.Add(line);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"MapPanel Warning: nextNodeIndices is null for node at column {i}, row {j}");
                    }
                }
            }
            UpdateNodeVisuals(_mapNodes, currentNodeElements);

            // Calculate total map content height and set it to _mapBackgroundSprite
            float maxNodeTop = 0f;

            if (_mapNodes != null && _mapNodes.Any())
            {
                maxNodeTop = 10f + (_mapNodes.Count - 1 - 0) * (NODE_HEIGHT + VERTICAL_PADDING);
            }
            float totalMapContentHeight = maxNodeTop + NODE_HEIGHT + 10f; // Add node height and some bottom offset
            _mapBackgroundSprite.style.height = totalMapContentHeight + 150f;

        });
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _isDragging = true;
        _lastMousePosition = evt.position;
        _pannableMapContent.CapturePointer(evt.pointerId);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_isDragging)
        {
            float deltaY = evt.position.y - _lastMousePosition.y;
            float newTop = _pannableMapContent.style.top.value.value + deltaY;

            // Calculate limits based on actual map content and visible area
            float maxNodeTop = 0f;

            if (_mapNodes != null && _mapNodes.Any())
            {
                // The nodes are laid out from bottom to top, so the highest 'top' value (lowest visual position)
                // will be for nodes in the first column (columnIndex = 0).
                // We need to find the maximum 'top' value among all nodes in the first column.
                // Since all nodes in a column have the same columnIndex, we can just use the formula for columnIndex = 0.
                maxNodeTop = 10f + (_mapNodes.Count - 1 - 0) * (NODE_HEIGHT + VERTICAL_PADDING);
            }

            float totalMapContentHeight = maxNodeTop + NODE_HEIGHT + 10f; // Add node height and some bottom offset

            float visibleViewportHeight = _root.resolvedStyle.height;

            // Define padding for the top and bottom of the visible area
            float topPadding = 50f; // Example: 50 pixels from the top
            float bottomPadding = 50f; // Example: 50 pixels from the bottom

            // Calculate minPanTop (when the bottom of the map content aligns with the bottom of the viewport)
            // The map content moves up, so a more negative 'top' value means it's moved further up.
            // When the bottom of the map content is at the bottom of the viewport,
            // its 'top' position will be: visibleViewportHeight - totalMapContentHeight - bottomPadding
            float minPanTop = visibleViewportHeight - totalMapContentHeight - bottomPadding;

            // Calculate maxPanTop (when the top of the map content aligns with the top of the viewport)
            // The map content moves down, so a more positive 'top' value means it's moved further down.
            // When the top of the map content is at the top of the viewport,
            // its 'top' position will be: topPadding
            float maxPanTop = topPadding;

            newTop = Mathf.Clamp(newTop, minPanTop, maxPanTop);

            _pannableMapContent.style.top = newTop;
            _lastMousePosition = evt.position;
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDragging = false;
        _pannableMapContent.ReleasePointer(evt.pointerId);
    }

    private void UpdateNodeVisuals(List<List<MapNodeData>> mapNodes, List<VisualElement> currentNodeElements)
    {
        if (GameSession.CurrentRunState == null)
        {
            Debug.LogWarning("GameSession.CurrentRunState is null. Cannot update map node visuals.");
            return;
        }

        int playerCurrentColumn = GameSession.CurrentRunState.currentColumnIndex;
        string playerCurrentEncounterId = GameSession.CurrentRunState.currentEncounterId;

        List<MapNodeData> flatMapNodes = _mapNodes.SelectMany(column => column).ToList();

        MapNodeData playerNodeData = flatMapNodes.FirstOrDefault(n => 
            n.columnIndex == playerCurrentColumn && 
            n.encounter.id == playerCurrentEncounterId);

        HashSet<int> reachableNodeUniqueIds = new HashSet<int>();
        if (playerNodeData != null)
        {
            reachableNodeUniqueIds = new HashSet<int>(playerNodeData.reachableNodeIndices);
        }

        int nodeElementIndex = 0;
        foreach (var column in _mapNodes)
        {
            foreach (var nodeData in column)
            {
                VisualElement nodeElement = currentNodeElements[nodeElementIndex];

                if (playerNodeData != null && nodeData.GetUniqueNodeId() == playerNodeData.GetUniqueNodeId())
                {
                    nodeElement.AddToClassList("current-node");
                    nodeElement.RemoveFromClassList("reachable-node");
                    nodeElement.RemoveFromClassList("unreachable-node");
                }
                else if (reachableNodeUniqueIds.Contains(nodeData.GetUniqueNodeId()))
                {
                    nodeElement.AddToClassList("reachable-node");
                    nodeElement.RemoveFromClassList("current-node");
                    nodeElement.RemoveFromClassList("unreachable-node");
                }
                else if (playerNodeData != null && nodeData.columnIndex == playerCurrentColumn + 1 && playerNodeData.nextNodeIndices.Contains(nodeData.rowIndex))
                {
                    nodeElement.AddToClassList("reachable-node");
                    nodeElement.RemoveFromClassList("current-node");
                    nodeElement.RemoveFromClassList("unreachable-node");
                }
                else
                {
                    nodeElement.AddToClassList("unreachable-node");
                    nodeElement.RemoveFromClassList("current-node");
                    nodeElement.RemoveFromClassList("reachable-node");
                }
                nodeElementIndex++;
            }
        }
    }

    private void OnNodeClicked(MapNodeData nodeData)
    {
        if (GameSession.CurrentRunState == null)
        {
            Debug.LogWarning("Cannot start encounter, GameSession is not active.");
            return;
        }

        MapNodeData playerCurrentNode = MapManager.Instance.GetMapNodes()
            .SelectMany(column => column)
            .FirstOrDefault(n => 
                n.columnIndex == GameSession.CurrentRunState.currentColumnIndex && 
                n.encounter.id == GameSession.CurrentRunState.currentEncounterId);

        bool isReachable = false;
        if (playerCurrentNode != null)
        {
            if (nodeData.columnIndex == playerCurrentNode.columnIndex + 1 && 
                playerCurrentNode.nextNodeIndices.Contains(nodeData.rowIndex))
            {
                isReachable = true;
            }
        }
        else if (nodeData.columnIndex == 0)
        {
            isReachable = true;
        }

        if (!isReachable)
        {
            Debug.LogWarning($"Node at column {nodeData.columnIndex}, row {nodeData.rowIndex} is not reachable from current position.");
            return;
        }

        if (nodeData.nodeType == NodeType.Unknown)
        {
            Rules rules = GameDataRegistry.GetRunConfig().rules;
            PityState currentPityState = GameSession.CurrentRunState.pityState;
            UnknownContext currentUnknownContext = GameSession.CurrentRunState.unknownContext;

            IRandomNumberGenerator rng = new Xoshiro256ss(123456789);

            UnknownOutcome outcome = Unknowns.ResolveUnknown(
                null,
                currentUnknownContext,
                rng,
                currentPityState,
                rules
            );

            nodeData.nodeType = outcome.Outcome;
            EncounterSO resolvedEncounter = null;
            if (nodeData.nodeType == NodeType.Boss)
            {
                resolvedEncounter = GameDataRegistry.GetEncounter("enc_boss");
            }
            else if (nodeData.nodeType == NodeType.Elite)
            {
                List<EncounterSO> availableEliteEncounters = GameDataRegistry.GetAllEncounters().Where(e => e.isElite).ToList();
                if (availableEliteEncounters.Any())
                {
                    resolvedEncounter = availableEliteEncounters[UnityEngine.Random.Range(0, availableEliteEncounters.Count)];
                }
            }
            else
            {
                EncounterType resolvedEncounterType = (EncounterType)nodeData.nodeType;
                List<EncounterSO> availableEncounters = GameDataRegistry.GetAllEncounters().Where(e => e.type == resolvedEncounterType && !e.isElite).ToList();
                if (availableEncounters.Any())
                {
                    resolvedEncounter = availableEncounters[UnityEngine.Random.Range(0, availableEncounters.Count)];
                }
            }

            if (resolvedEncounter != null)
            {
                nodeData.encounter = resolvedEncounter;
                nodeData.iconPath = resolvedEncounter.iconPath;
                nodeData.tooltipText = resolvedEncounter.tooltipText;
                nodeData.isElite = resolvedEncounter.isElite;
            }
            else
            {
                Debug.LogWarning($"Could not find a suitable encounter for resolved node type {nodeData.nodeType}. Falling back to Battle.");
                nodeData.encounter = GameDataRegistry.GetAllEncounters().FirstOrDefault(e => e.type == EncounterType.Battle && !e.isElite);
                if (nodeData.encounter == null)
                {
                    Debug.LogError("No Battle encounter found for fallback.");
                }
            }

            GameSession.CurrentRunState.pityState = outcome.NewPityState;

            Debug.Log($"Unknown node resolved to: {nodeData.nodeType}");
        }

        GameSession.CurrentRunState.currentEncounterId = nodeData.encounter.id;
        GameSession.CurrentRunState.currentColumnIndex = nodeData.columnIndex;

        UpdateNodeVisuals(_mapNodes, _mapNodesContainer.Children().ToList());

        Hide();

        if (nodeData.encounter.type == EncounterType.Boss)
        {
            Debug.Log("Triggering Boss Buildup!");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(nodeData.encounter.type.ToString());
    }
}