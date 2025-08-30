using System;
using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data; // Added for EncounterType
using UnityEngine;

namespace Pirate.MapGen
{
    public class MapGenerator
    {
        private MapValidator _validator = new MapValidator();

        /// <summary>
        /// Generates a complete map based on the act specification and rules.
        /// Orchestrates Phase A (Skeleton), Phase B (Typing), and Phase C (Validation & Repair).
        /// </summary>
        /// <param name="actSpec">The specification for the act.</param>
        /// <param name="rules">The generation rules.</param>
        /// <param name="seed">The seed for deterministic generation.</param>
        /// <param name="maxRepairIterations">Maximum attempts to repair the map if validation fails.</param>
        /// <returns>A GenerationResult containing the generated map and audit report.</returns>
        public GenerationResult GenerateMap(ActSpec actSpec, RulesSO rules, ulong seed, int maxRepairIterations = 50)
        {
            GenerationResult result = new GenerationResult
            {
                Seed = seed,
                SubSeeds = new SubSeeds()
            };

            // Phase A: Skeleton Generation
            IRandomNumberGenerator skeletonRng = new Xoshiro256ss(SeedUtility.CreateSubSeed(seed, "skeleton"));
            result.SubSeeds.Skeleton = skeletonRng.NextULong(); // Store the initial state of the sub-seed
            MapGraph graph = GenerateSkeleton(actSpec, skeletonRng);

            // Phase B: Typing Under Constraints
            IRandomNumberGenerator typingRng = new Xoshiro256ss(SeedUtility.CreateSubSeed(seed, "typing"));
            result.SubSeeds.Typing = typingRng.NextULong(); // Store the initial state of the sub-seed
            ApplyTypingConstraints(graph, actSpec, rules, typingRng);

            // Phase C: Validation & Repair
            IRandomNumberGenerator repairRng = new Xoshiro256ss(SeedUtility.CreateSubSeed(seed, "repairs"));
            result.SubSeeds.Repairs = repairRng.NextULong(); // Store the initial state of the sub-seed

            AuditReport audit = _validator.Validate(graph, rules, actSpec);
            int currentRepairIteration = 0;

            while (!audit.IsValid && currentRepairIteration < maxRepairIterations)
            {
                Debug.Log($"Repairing map (iteration {currentRepairIteration + 1}/{maxRepairIterations})... Violations: {string.Join(", ", audit.Violations)}");

                bool repairedThisIteration = false;

                // Attempt to fix specific violations
                foreach (string violation in audit.Violations.ToList()) // ToList to avoid modifying collection while iterating
                {
                    if (violation.Contains("No valid path from start to boss."))
                    {
                        // This is a drastic measure, but fixing connectivity is complex.
                        // If no path to boss, re-generate skeleton and re-apply typing
                        graph = GenerateSkeleton(actSpec, repairRng); // Use repairRng for determinism
                        ApplyTypingConstraints(graph, actSpec, rules, repairRng);
                        repairedThisIteration = true;
                        break; // Restart validation after drastic change
                    }
                    else if (violation.Contains("No Port node found on the row immediately before the Boss."))
                    {
                        int preBossRow = actSpec.Rows - 2;
                        Node targetNode = graph.Nodes.FirstOrDefault(n => n.Row == preBossRow && n.Type != NodeType.Boss && n.Type != NodeType.Port);
                        if (targetNode != null)
                        {
                            targetNode.Type = NodeType.Port;
                            targetNode.Tags.Add("preBossPort");
                            repairedThisIteration = true;
                        }
                    }
                    else if (violation.Contains("No Treasure node found within the specified mid-act window."))
                    {
                        Node targetNode = graph.Nodes.FirstOrDefault(n => rules.Windows.MidTreasureRows.Contains(n.Row) && n.Type != NodeType.Boss && n.Type != NodeType.Treasure);
                        if (targetNode != null)
                        {
                            targetNode.Type = NodeType.Treasure;
                            targetNode.Tags.Add("midActTreasure");
                            repairedThisIteration = true;
                        }
                    }
                    // Add more specific repair logic for other violations here
                    // For example, for spacing violations, identify the offending nodes and try to re-type them
                    // For "Boss edges are correct" violations, identify the incorrect edges/nodes and try to fix them
                }

                if (!repairedThisIteration)
                {
                    // Fallback: If no specific repair was applied, try a general re-type of a random non-locked node
                    Node nodeToRetype = graph.Nodes.Where(n => !n.Tags.Contains("fixed_first_row") && !n.Tags.Contains("boss") && !n.Tags.Contains("fixed_pre_boss_port") && !n.Tags.Contains("fixed_treasure_row")).OrderBy(n => repairRng.NextULong()).FirstOrDefault();
                    if (nodeToRetype != null)
                    {
                        NodeType originalType = nodeToRetype.Type;
                        List<NodeType> eligibleTypes = GetEligibleNodeTypes(nodeToRetype, graph, actSpec, rules, new HashSet<string>(graph.Nodes.Where(n => n.Type != NodeType.Unknown).Select(n => n.Id))); // Pass all currently typed nodes as placed
                        if (eligibleTypes.Any())
                        {
                            // Try to re-type to a random eligible type
                            SerializableDictionary<NodeType, int> currentBandOdds = GetOddsForNodeRow(nodeToRetype.Row, actSpec.Rows, rules);
                            NodeType attemptedType = SelectWeightedRandomType(eligibleTypes, currentBandOdds, repairRng);
                            nodeToRetype.Type = attemptedType;
                            repairedThisIteration = true;
                        }
                        else
                        {
                            // As a last resort, if no eligible types, re-type to FallbackNodeType
                            nodeToRetype.Type = rules.Spacing.FallbackNodeType;
                            repairedThisIteration = true;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No non-locked nodes available for general re-typing fallback. Map might be unrepairable.");
                        break; // Cannot repair further
                    }
                }

                // Re-validate after each repair attempt
                audit = _validator.Validate(graph, rules, actSpec);
                currentRepairIteration++;
            }

            result.Graph = graph;
            result.Audits = audit;

            if (!audit.IsValid)
            {
                result.Warnings.Add($"Map could not be fully repaired after {maxRepairIterations} iterations. Remaining violations: {string.Join(", ", audit.Violations)}");
            }

            return result;
        }

        /// <summary>
        /// Generates the basic layered DAG skeleton of the map.
        /// Phase A of the map generation process.
        /// </summary>
        /// <param name="actSpec">The specification for the act, including rows, columns, and branchiness.</param>
        /// <param name="rng">The random number generator for deterministic generation.</param>
        /// <returns>A MapGraph representing the skeleton of the map.</returns>
        public MapGraph GenerateSkeleton(ActSpec actSpec, IRandomNumberGenerator rng)
        {
            if (actSpec.Columns % 2 == 0)
            {
                throw new ArgumentException($"Number of columns ({actSpec.Columns}) must be odd for boss node placement.");
            }

            MapGraph graph = new MapGraph();
            graph.Rows = actSpec.Rows;

            // 1. Create nodes
            for (int r = 0; r < actSpec.Rows; r++)
            {
                for (int c = 0; c < actSpec.Columns; c++)
                {
                    graph.Nodes.Add(new Node { Id = $"node_{r}_{c}", Row = r, Col = c, Type = NodeType.Unknown });
                }
            }

            var allUsedNodeIds = new HashSet<string>();
            var allUsedEdgeIds = new HashSet<string>();
            var edgesByRow = new List<Tuple<int, int>>[actSpec.Rows - 1];
            for (int i = 0; i < edgesByRow.Length; i++) edgesByRow[i] = new List<Tuple<int, int>>();

            var startCols = new int[6];
            int pathsGenerated = 0;
            const int MAX_TOTAL_RESTARTS = 500;
            int totalRestarts = 0;

            // 2. Generate 6 paths
            while (pathsGenerated < 6)
            {
                if (totalRestarts > MAX_TOTAL_RESTARTS)
                {
                    Debug.LogError("Max total restarts exceeded. Aborting skeleton generation.");
                    break;
                }

                var pathDecisionStack = new Stack<PathStep>();
                
                // Determine start column
                int startCol;
                if (pathsGenerated == 0) { startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns); }
                else if (pathsGenerated == 1) { do { startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns); } while (startCol == startCols[0]); }
                else { startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns); }
                if (pathsGenerated < startCols.Length) startCols[pathsGenerated] = startCol;

                int currentCol = startCol;
                int r = 0;
                
                bool pathFailed = false;

                while (r < actSpec.Rows - 1)
                {
                    List<int> candidates = GetThreeNearestCandidates(currentCol, actSpec.Columns, rng);
                    List<int> validCandidates = FilterCandidatesByCrossing(candidates, r, currentCol, edgesByRow[r]);

                    if (r == actSpec.Rows - 2) // Pre-boss row
                    {
                        int bossCol = actSpec.Columns / 2;
                        if (validCandidates.Contains(bossCol)) { validCandidates = new List<int> { bossCol }; }
                        else { validCandidates.Clear(); }
                    }

                    if (validCandidates.Any())
                    {
                        int chosenNextCol = validCandidates[(int)(rng.NextULong() % (ulong)validCandidates.Count)];
                        
                        pathDecisionStack.Push(new PathStep
                        {
                            Row = r,
                            CurrentCol = currentCol,
                            ChosenNextCol = chosenNextCol,
                            RemainingCandidates = new List<int>(validCandidates.Where(c => c != chosenNextCol))
                        });
                        
                        currentCol = chosenNextCol;
                        r++;
                    }
                    else // No valid candidates, must backtrack
                    {
                        if (!pathDecisionStack.Any())
                        {
                            pathFailed = true;
                            break; 
                        }

                        PathStep lastStep = null;
                        do {
                            if (!pathDecisionStack.Any()) {
                                pathFailed = true;
                                break;
                            }
                            lastStep = pathDecisionStack.Pop();
                        } while (!lastStep.RemainingCandidates.Any());

                        if (pathFailed || lastStep == null) break;

                        // We found a step with alternatives. Restore state and retry.
                        r = lastStep.Row;
                        currentCol = lastStep.CurrentCol;
                        
                        int nextCandidate = lastStep.RemainingCandidates[0];
                        lastStep.RemainingCandidates.RemoveAt(0);
                        pathDecisionStack.Push(lastStep); // Push back with fewer candidates

                        // Manually perform the next step
                        pathDecisionStack.Push(new PathStep
                        {
                            Row = r,
                            CurrentCol = currentCol,
                            ChosenNextCol = nextCandidate,
                            RemainingCandidates = new List<int>() // Will be generated if this fails
                        });
                        currentCol = nextCandidate;
                        r++;
                    }
                }

                if (pathFailed)
                {
                    totalRestarts++;
                    Debug.Log($"Path {pathsGenerated + 1} failed and will be restarted. Total restarts: {totalRestarts}");
                }
                else // Path completed
                {
                    Debug.Log($"Path {pathsGenerated + 1} completed.");
                    // Add nodes and edges from the completed path to the overall used sets
                    var finalPath = new List<PathStep>(pathDecisionStack);
                    finalPath.Reverse();

                    // Add start node
                    allUsedNodeIds.Add($"node_0_{startCol}");

                    foreach(var step in finalPath)
                    {
                        allUsedNodeIds.Add($"node_{step.Row}_{step.CurrentCol}");
                        allUsedNodeIds.Add($"node_{step.Row + 1}_{step.ChosenNextCol}");
                        string edgeId = $"edge_{step.Row}_{step.CurrentCol}_{step.ChosenNextCol}";
                        if(allUsedEdgeIds.Add(edgeId))
                        {
                            graph.Edges.Add(new Edge { Id = edgeId, FromId = $"node_{step.Row}_{step.CurrentCol}", ToId = $"node_{step.Row + 1}_{step.ChosenNextCol}" });
                            edgesByRow[step.Row].Add(Tuple.Create(step.CurrentCol, step.ChosenNextCol));
                        }
                    }
                    pathsGenerated++;
                }
            }

            // 3. Pruning
            graph.Nodes.RemoveAll(n => !allUsedNodeIds.Contains(n.Id));
            graph.Edges.RemoveAll(e => !allUsedEdgeIds.Contains(e.Id));

            return graph;
        }

        /// <summary>
        /// Assigns fixed and guaranteed node types to the map graph.
        /// This is Phase B.1 of the map generation process, handling immutable placements.
        /// </summary>
        /// <param name="graph">The map graph skeleton.</param>
        /// <param name="actSpec">The act specification.</param>
        /// <param name="rules">The generation rules.</param>
        /// <param name="rng">The random number generator.</param>
        /// <param name="placedNodeIds">A HashSet to track IDs of nodes that have already been assigned a type.</param>
        private void AssignFixedAndGuaranteedNodes(MapGraph graph, ActSpec actSpec, RulesSO rules, IRandomNumberGenerator rng, HashSet<string> placedNodeIds)
        {
            // Helper to get available nodes (not yet placed)
            Func<int, List<Node>> getAvailableNodesInRow = (row) =>
                graph.Nodes.Where(n => n.Row == row && !placedNodeIds.Contains(n.Id)).ToList();

            // 1. Fixed Row: Row 0 = all Battle (Monster)
            List<Node> firstRowNodes = graph.Nodes.Where(n => n.Row == 0).ToList();
            foreach (Node node in firstRowNodes)
            {
                node.Type = NodeType.Battle; // Assuming Monster maps to Battle NodeType
                node.Tags.Add("fixed_first_row");
                placedNodeIds.Add(node.Id);
            }
            Debug.Log($"Assigned all nodes in Row 0 to Battle. Count: {firstRowNodes.Count}");

            // 2. Fixed Row: Row R = single Boss node
            Node bossNode = graph.Nodes.FirstOrDefault(n => n.Row == actSpec.Rows - 1);
            if (bossNode != null)
            {
                bossNode.Type = NodeType.Boss;
                bossNode.Tags.Add("boss");
                placedNodeIds.Add(bossNode.Id);
                Debug.Log($"Assigned Boss to row {actSpec.Rows - 1}.");

                // Ensure all nodes in the pre-boss row connect to the boss node
                int preBossRow = actSpec.Rows - 2;
                if (preBossRow >= 0)
                {
                    List<Node> preBossNodes = graph.Nodes.Where(n => n.Row == preBossRow).ToList();
                    foreach (Node preBossNode in preBossNodes)
                    {
                        // Check if an edge already exists from preBossNode to bossNode
                        if (!graph.Edges.Any(e => e.FromId == preBossNode.Id && e.ToId == bossNode.Id))
                        {
                            // Create and add the edge
                            Edge newEdge = new Edge { Id = $"edge_{preBossNode.Row}_{preBossNode.Col}_{bossNode.Col}", FromId = preBossNode.Id, ToId = bossNode.Id };
                            graph.Edges.Add(newEdge);
                            Debug.Log($"Added missing edge from {preBossNode.Id} to {bossNode.Id}");
                        }
                    }
                }
            }

            // 3. Fixed Row: Row R-1 = all Port (pre-boss rest row)
            int preBossPortRow = actSpec.Rows - 2;
            if (preBossPortRow >= 0)
            {
                List<Node> preBossPortNodes = graph.Nodes.Where(n => n.Row == preBossPortRow).ToList();
                foreach (Node node in preBossPortNodes)
                {
                    node.Type = NodeType.Port;
                    node.Tags.Add("fixed_pre_boss_port");
                    placedNodeIds.Add(node.Id);
                }
                Debug.Log($"Assigned all nodes in Row {preBossPortRow} to Port. Count: {preBossPortNodes.Count}");
            }

            // 4. Fixed Row: Row ⌈0.6R⌉ = all Treasure
            int treasureRow = (int)Math.Ceiling(0.6 * actSpec.Rows);
            if (treasureRow >= 0 && treasureRow < actSpec.Rows - 1) // Ensure within valid range and not boss row
            {
                List<Node> midTreasureNodes = graph.Nodes.Where(n => n.Row == treasureRow).ToList();
                foreach (Node node in midTreasureNodes)
                {
                    node.Type = NodeType.Treasure;
                    node.Tags.Add("fixed_treasure_row");
                    placedNodeIds.Add(node.Id);
                }
                Debug.Log($"Assigned all nodes in Row {treasureRow} to Treasure. Count: {midTreasureNodes.Count}");
            }
        }

        /// <summary>
        /// Applies typing constraints to the map graph.
        /// Phase B of the map generation process.
        /// </summary>
        /// <param name="graph">The map graph skeleton.</param>
        /// <param name="actSpec">The act specification.</param>
        /// <param name="rules">The generation rules.</param>
        /// <param name="rng">The random number generator.</param>
        public void ApplyTypingConstraints(MapGraph graph, ActSpec actSpec, RulesSO rules, IRandomNumberGenerator rng)
        {
            HashSet<string> placedNodeIds = new HashSet<string>();

            // Phase B.1: Assign Fixed and Guaranteed Nodes
            AssignFixedAndGuaranteedNodes(graph, actSpec, rules, rng, placedNodeIds);

            // Phase B.2: Assign remaining nodes using weighted odds and re-rolls
            AssignNodeTypesWeighted(graph, actSpec, rules, rng, placedNodeIds);

            // Debugging: Display final NodeType assignment for each node
            Debug.Log("--- Final Node Type Assignments ---");
            foreach (Node node in graph.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Col))
            {
                Debug.Log($"Node ID: {node.Id}, Type: {node.Type}");
            }
            Debug.Log("-----------------------------------");
        }

        /// <summary>
        /// Assigns node types to the remaining unplaced nodes using weighted odds and re-rolls.
        /// This is Phase B.2 of the map generation process.
        /// </summary>
        /// <param name="graph">The map graph skeleton.</param>
        /// <param name="actSpec">The act specification.</param>
        /// <param name="rules">The generation rules.</param>
        /// <param name="rng">The random number generator.</param>
        /// <param name="placedNodeIds">A HashSet tracking IDs of nodes that have already been assigned a type.</param>
        private void AssignNodeTypesWeighted(MapGraph graph, ActSpec actSpec, RulesSO rules, IRandomNumberGenerator rng, HashSet<string> placedNodeIds)
        {
            List<Node> unplacedNodes = graph.Nodes.Where(n => !placedNodeIds.Contains(n.Id)).ToList();

            // Sort unplaced nodes by row to process them in order
            unplacedNodes = unplacedNodes.OrderBy(n => n.Row).ThenBy(n => n.Col).ToList();

            foreach (Node node in unplacedNodes)
            {
                NodeType chosenType = NodeType.Battle; // Default fallback

                // Determine eligible node types for the current node
                List<NodeType> eligibleTypes = GetEligibleNodeTypes(node, graph, actSpec, rules, placedNodeIds);
                Debug.Log($"[AssignNodeTypesWeighted] Node {node.Id} (Row: {node.Row}): Eligible Types: {string.Join(", ", eligibleTypes)}");

                if (eligibleTypes.Any())
                {
                    bool typeAssigned = false;
                    NodeType originalNodeType = node.Type; // Store original type for potential revert

                    // Determine the correct RowBandOdds for the current node's row
                    SerializableDictionary<NodeType, int> currentBandOdds = GetOddsForNodeRow(node.Row, actSpec.Rows, rules);

                    for (int i = 0; i < rules.Spacing.MaxRerollAttempts; i++)
                    {
                        NodeType attemptedType = SelectWeightedRandomType(eligibleTypes, currentBandOdds, rng); // Pass currentBandOdds
                        node.Type = attemptedType; // Temporarily assign the attempted type

                        // Validate the entire graph with the attempted type
                        AuditReport audit = _validator.Validate(graph, rules, actSpec);

                        Debug.Log($"[AssignNodeTypesWeighted] Node {node.Id} (Attempt {i + 1}): Attempted Type: {attemptedType}, IsValid: {audit.IsValid}, Violations: {string.Join(", ", audit.Violations)}");

                        if (audit.IsValid)
                        {
                            chosenType = attemptedType;
                            typeAssigned = true;
                            break; // Valid type found, exit re-roll loop
                        }
                        else
                        {
                            // If invalid, revert the node's type for the next attempt
                            node.Type = originalNodeType;
                            // Optionally log the reason for re-roll for debugging
                            // Debug.LogWarning($"Attempted type {attemptedType} for node {node.Id} failed validation: {string.Join(", ", audit.Violations)}. Re-rolling...");
                        }
                    }

                    if (!typeAssigned)
                    {
                        // Fallback if re-rolls fail
                        chosenType = rules.Spacing.FallbackNodeType;
                        Debug.LogWarning($"[AssignNodeTypesWeighted] Failed to assign a type to node {node.Id} after {rules.Spacing.MaxRerollAttempts} re-rolls. Falling back to {chosenType}.");
                    }
                }
                else
                {
                    // No eligible types found, fall back
                    chosenType = rules.Spacing.FallbackNodeType;
                    Debug.LogWarning($"[AssignNodeTypesWeighted] No eligible types found for node {node.Id}. Falling back to {chosenType}.");
                }

                node.Type = chosenType;
                placedNodeIds.Add(node.Id);
                Debug.Log($"[AssignNodeTypesWeighted] Node {node.Id} (Row: {node.Row}) Final Chosen Type: {chosenType}");
            }
        }

        /// <summary>
        /// Selects a node type based on weighted odds.
        /// </summary>
        private NodeType SelectWeightedRandomType(List<NodeType> availableTypes, SerializableDictionary<NodeType, int> currentBandOdds, IRandomNumberGenerator rng)
        {
            Debug.Log($"[SelectWeightedRandomType] Available Types: {string.Join(", ", availableTypes)}");
            Debug.Log($"[SelectWeightedRandomType] Current Band Odds: {string.Join(", ", currentBandOdds.Select(kvp => kvp.ToString()))}");

            if (!availableTypes.Any())
            {
                Debug.LogError("SelectWeightedRandomType called with no available types.");
                return NodeType.Battle; // Should not happen if GetEligibleNodeTypes works correctly
            }

            // Filter currentBandOdds to only include available types
            Dictionary<NodeType, int> filteredOdds = currentBandOdds
                .Where(kvp => availableTypes.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Debug.Log($"[SelectWeightedRandomType] Filtered Odds: {string.Join(", ", filteredOdds.Select(kvp => kvp.ToString()))}");

            if (!filteredOdds.Any())
            {
                Debug.LogWarning("No weighted odds found for available types in current band. Returning first available type.");
                return availableTypes.First();
            }

            int totalWeight = filteredOdds.Sum(kvp => kvp.Value);
            Debug.Log($"[SelectWeightedRandomType] Total Weight: {totalWeight}");

            if (totalWeight <= 0)
            {
                Debug.LogWarning("Total weight is zero or negative in current band. Returning first available type.");
                return availableTypes.First();
            }

            int randomNumber = (int)(rng.NextULong() % (ulong)totalWeight);
            Debug.Log($"[SelectWeightedRandomType] Random Number (0 to {totalWeight - 1}): {randomNumber}");

            foreach (var entry in filteredOdds)
            {
                randomNumber -= entry.Value;
                if (randomNumber < 0)
                {
                    Debug.Log($"[SelectWeightedRandomType] Chosen Type: {entry.Key}");
                    return entry.Key;
                }
            }

            // Fallback, should not be reached
            Debug.LogWarning("[SelectWeightedRandomType] Fallback reached. Returning first available type.");
            return availableTypes.First();
        }

        /// <summary>
        /// Determines the eligible node types for a given node based on all generation rules.
        /// </summary>
        private List<NodeType> GetEligibleNodeTypes(Node node, MapGraph graph, ActSpec actSpec, RulesSO rules, HashSet<string> placedNodeIds)
        {
            List<NodeType> eligibleTypes = Enum.GetValues(typeof(NodeType)).Cast<NodeType>().ToList();

            // Remove types that are already fixed or boss
            eligibleTypes.Remove(NodeType.Boss);
            eligibleTypes.Remove(NodeType.Port); // Ports are fixed in pre-boss row
            eligibleTypes.Remove(NodeType.Treasure); // Treasures are fixed in mid-act row
            eligibleTypes.Remove(NodeType.Event); // Events are only resolved from Unknown nodes, not generated directly

            // The first row is fixed to Battle, so Battle should not be an eligible type for other nodes
            // This is implicitly handled by AssignFixedAndGuaranteedNodes marking Row 0 nodes as placed.
            // No need to remove NodeType.Battle here unless it's a specific ban for other rows.

            // Rule 2: Structural bans & availability
            // Elites unlock at row ⌈0.35R⌉
            int eliteUnlockRow = (int)Math.Ceiling(0.35 * actSpec.Rows);
            if (node.Row < eliteUnlockRow)
            {
                eligibleTypes.Remove(NodeType.Elite);
            }

            // No Port on row R-2 (already handled by fixed rows, but good to keep in mind for general bans)
            // This rule is now implicitly handled by the fixed row assignment for R-1 (all Port) and the fact that R-2 is not a fixed Port row.
            // If rules.Bans.BanPortOnPrePreBossRow is true, and this row is R-2, then remove Port.
            if (rules.Flags.BanPortOnPrePreBossRow && node.Row == actSpec.Rows - 3) // R-3 is R-2 relative to R-1 (pre-boss port row)
            {
                eligibleTypes.Remove(NodeType.Port);
            }

            // Rule 3: Adjacency rules (generation-time only)
            // Along any single path: no Elite→Elite, Shop→Shop, Port→Port consecutives.
            // At a split, a parent’s children must be different types, except when the next row is uniform by design.

            // To implement adjacency rules, we need to know the parent nodes and their types.
            // This requires looking at the graph's edges.

            // Get parent nodes
            List<Node> parentNodes = graph.Edges
                .Where(e => e.ToId == node.Id)
                .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.FromId))
                .Where(n => n != null && placedNodeIds.Contains(n.Id)) // Only consider parents that have already been typed
                .ToList();

            // Check for consecutive bans
            foreach (Node parent in parentNodes)
            {
                if (rules.Flags.NoEliteToElite && parent.Type == NodeType.Elite)
                {
                    eligibleTypes.Remove(NodeType.Elite);
                }
                if (rules.Flags.NoShopToShop && parent.Type == NodeType.Shop)
                {
                    eligibleTypes.Remove(NodeType.Shop);
                }
                if (rules.Flags.NoPortToPort && parent.Type == NodeType.Port)
                {
                    eligibleTypes.Remove(NodeType.Port);
                }
            }

            // Check for children must be different types (if applicable)
            // This rule applies to the *children* of a parent, not the current node's type based on its parents.
            // This rule is better enforced when selecting types for children, or as a validation step.
            // For now, I will skip this part as it's more complex to enforce during the current node's typing.
            // It might be better handled as a post-processing step or during validation.

            // Check for children must be different types (if applicable)
            if (rules.Flags.ChildrenMustBeDifferentTypes)
            {
                // Check if the current row is a uniform row (Treasure or Port)
                int treasureRow = (int)Math.Ceiling(0.6 * actSpec.Rows);
                int preBossPortRow = actSpec.Rows - 2;
                bool isUniformRow = (node.Row == treasureRow) || (node.Row == preBossPortRow);

                if (!isUniformRow)
                {
                    foreach (Node parent in parentNodes)
                    {
                        // Find all children of this parent in the current row
                        List<Node> siblings = graph.Edges
                            .Where(e => e.FromId == parent.Id && graph.Nodes.Any(n => n.Id == e.ToId && n.Row == node.Row))
                            .Select(e => graph.Nodes.FirstOrDefault(n => n.Id == e.ToId))
                            .Where(n => n != null && n.Id != node.Id && placedNodeIds.Contains(n.Id)) // Only consider already placed siblings
                            .ToList();

                        if (siblings.Any()) // If this parent has other children in this row
                        {
                            foreach (Node sibling in siblings)
                            {
                                eligibleTypes.Remove(sibling.Type); // Remove sibling's type from eligible types for current node
                            }
                        }
                    }
                }
            }

            // Ensure that the list of eligible types is not empty. If it is, something is wrong.
            if (!eligibleTypes.Any())
            {
                Debug.LogWarning($"No eligible types remaining for node {node.Id} after applying rules. This should not happen. Falling back to {rules.Spacing.FallbackNodeType}.");
                // As a last resort, allow FallbackNodeType
                eligibleTypes.Add(rules.Spacing.FallbackNodeType);
            }

            return eligibleTypes;
        }

        /// <summary>
        /// Helper method to get the correct odds for a given row
        /// </summary>
        private SerializableDictionary<NodeType, int> GetOddsForNodeRow(int row, int totalRows, RulesSO rules)
        {
            // Determine fixed rows for band calculation
            int treasureRow = (int)Math.Ceiling(0.6 * totalRows);
            int eliteUnlockRow = (int)Math.Ceiling(0.35 * totalRows);
            int preBossPortRow = totalRows - 2;

            // Prioritize specific bands
            foreach (var bandOdds in rules.Spacing.RowBandGenerationOdds)
            {
                if (row >= bandOdds.MinRow && row <= bandOdds.MaxRow)
                {
                    return bandOdds.Odds;
                }
            }

            // Fallback to a default if no specific band matches
            // This assumes there's a "Default" band with MinRow=0, MaxRow=totalRows
            var defaultBand = rules.Spacing.RowBandGenerationOdds.FirstOrDefault(b => b.Band == RowBand.Default);
            if (defaultBand != null)
            {
                return defaultBand.Odds;
            }

            // If no default band is found, return an empty dictionary or throw an error
            Debug.LogError($"No generation odds defined for row {row} and no default band found. Returning empty odds.");
            return new SerializableDictionary<NodeType, int>();
        }

        /// <summary>
        /// Helper method to get up to 3 nearest column indices, with random tie-breaking.
        /// </summary>
        private List<int> GetThreeNearestCandidates(int currentCol, int totalColumns, IRandomNumberGenerator rng)
        {
            List<int> candidates = new List<int>();

            // Add current column
            candidates.Add(currentCol);

            // Add adjacent columns if they exist
            if (currentCol > 0) candidates.Add(currentCol - 1);
            if (currentCol < totalColumns - 1) candidates.Add(currentCol + 1);

            // Sort by distance from currentCol, then randomly for ties
            return candidates.OrderBy(c => Math.Abs(c - currentCol))
                             .ThenBy(c => rng.NextULong())
                             .Take(3)
                             .ToList();
        }

        /// <summary>
        /// Determines if two edges (r, a) -> (r+1, b) and (r, c) -> (r+1, d) cross.
        /// </summary>
        private bool CheckForCrossing(int a, int b, int c, int d)
        {
            // Edges (a,b) and (c,d) cross if (a < c and b > d) or (a > c and b < d)
            return (a < c && b > d) || (a > c && b < d);
        }

        /// <summary>
        /// Filters a list of candidate next columns based on crossing checks with existing edges.
        /// </summary>
        private List<int> FilterCandidatesByCrossing(List<int> candidates, int currentRow, int currentCol, List<Tuple<int, int>> existingEdgesInRow)
        {
            List<int> validCandidates = new List<int>();
            foreach (int candidateNextCol in candidates)
            {
                bool crosses = false;
                foreach (var existingEdge in existingEdgesInRow)
                {
                    if (CheckForCrossing(currentCol, candidateNextCol, existingEdge.Item1, existingEdge.Item2))
                    {
                        crosses = true;
                        break;
                    }
                }
                if (!crosses)
                {
                    validCandidates.Add(candidateNextCol);
                }
            }
            return validCandidates;
        }

        /// <summary>
        /// Struct to hold backtracking information for path generation.
        /// </summary>
        private class PathStep
        {
            public int Row;
            public int CurrentCol;
            public int ChosenNextCol;
            public List<int> RemainingCandidates; // Candidates that were not chosen at this step
        }
    }
}
