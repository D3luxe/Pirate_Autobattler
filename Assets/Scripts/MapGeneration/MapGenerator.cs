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

            AuditReport audit = _validator.Validate(graph, rules);
            int currentRepairIteration = 0;

            while (!audit.IsValid && currentRepairIteration < maxRepairIterations)
            {
                Debug.Log($"Repairing map (iteration {currentRepairIteration + 1}/{maxRepairIterations})... Violations: {string.Join(", ", audit.Violations)}");

                // Prioritize critical violations
                if (audit.Violations.Contains("No valid path from start to boss."))
                {
                    // If no path to boss, re-generate skeleton and re-apply typing
                    // This is a drastic measure, but fixing connectivity is complex.
                    graph = GenerateSkeleton(actSpec, repairRng); // Use repairRng for determinism
                    ApplyTypingConstraints(graph, actSpec, rules, repairRng);
                }
                else if (audit.Violations.Contains("No Port node found on the row immediately before the Boss."))
                {
                    // Find an available node in the pre-boss row and re-type it to Port
                    int preBossRow = actSpec.Rows - 2;
                    Node targetNode = graph.Nodes.FirstOrDefault(n => n.Row == preBossRow && n.Type != NodeType.Boss && n.Type != NodeType.Port);
                    if (targetNode != null)
                    {
                        targetNode.Type = NodeType.Port;
                        targetNode.Tags.Add("preBossPort");
                    }
                }
                else if (audit.Violations.Contains("No Treasure node found within the specified mid-act window."))
                {
                    // Find an available node in the mid-act window and re-type it to Treasure
                    Node targetNode = graph.Nodes.FirstOrDefault(n => rules.Windows.MidTreasureRows.Contains(n.Row) && n.Type != NodeType.Boss && n.Type != NodeType.Treasure);
                    if (targetNode != null)
                    {
                        targetNode.Type = NodeType.Treasure;
                        targetNode.Tags.Add("midActTreasure");
                    }
                }
                else
                {
                    // Handle count and spacing violations
                    // This part would be more complex, iterating through violations and applying specific fixes.
                    // For now, a simple re-type of a random node to Battle as a fallback if no specific violation is handled.
                    Node nodeToRetype = graph.Nodes[(int)(repairRng.NextULong() % (ulong)graph.Nodes.Count)];
                    nodeToRetype.Type = NodeType.Battle; // Simple re-type to Battle as a fallback
                }

                // Re-validate after each repair attempt
                audit = _validator.Validate(graph, rules);
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
            MapGraph graph = new MapGraph();
            graph.Rows = actSpec.Rows;

            // 1. Create nodes for each column
            for (int r = 0; r < actSpec.Rows; r++)
            {
                                int nodesInRow = (r == 0 || r == actSpec.Rows - 1) ? 1 : (int)(rng.NextULong() % (ulong)(actSpec.Columns - 2)) + 2; // 2 to Columns-1 nodes for mid-rows
                if (r == 0) nodesInRow = 1; // First row always 1 node
                if (r == actSpec.Rows - 1) nodesInRow = 1; // Last row always 1 node (Boss)

                for (int c = 0; c < nodesInRow; c++)
                {
                    graph.Nodes.Add(new Node
                    {
                        Id = $"node_{r}_{c}",
                        Row = r,
                        Col = c,
                        Type = NodeType.Unknown // Default to unknown for now, will be typed later
                    });
                }
            }

            // 2. Wire edges
            for (int r = 0; r < actSpec.Rows - 1; r++)
            {
                List<Node> currentRowNodes = graph.Nodes.Where(n => n.Row == r).ToList();
                List<Node> nextRowNodes = graph.Nodes.Where(n => n.Row == r + 1).ToList();

                // Ensure every node in the next column has at least one incoming connection
                foreach (Node nextNode in nextRowNodes)
                {
                    bool isConnected = false;
                    foreach (Node currentNode in currentRowNodes)
                    {
                        // Check if an edge already exists (for re-runs or repair)
                        if (graph.Edges.Any(e => e.FromId == currentNode.Id && e.ToId == nextNode.Id))
                        {
                            isConnected = true;
                            break;
                        }
                    }

                    if (!isConnected)
                    {
                        // Connect from a random node in the current column
                        Node randomCurrentNode = currentRowNodes[(int)(rng.NextULong() % (ulong)currentRowNodes.Count)];
                        graph.Edges.Add(new Edge { FromId = randomCurrentNode.Id, ToId = nextNode.Id });
                    }
                }

                // Ensure every node in the current column has at least one outgoing connection
                foreach (Node currentNode in currentRowNodes)
                {
                    if (!graph.Edges.Any(e => e.FromId == currentNode.Id))
                    {
                        // Connect to a random node in the next column
                        Node randomNextNode = nextRowNodes[(int)(rng.NextULong() % (ulong)nextRowNodes.Count)];
                        graph.Edges.Add(new Edge { FromId = currentNode.Id, ToId = randomNextNode.Id });
                    }

                    // Add more random connections (branching) based on branchiness
                    // branchiness is 0..1, representing target edges per node
                    int targetConnections = (int)Math.Round(actSpec.Branchiness * nextRowNodes.Count);
                    while (graph.Edges.Count(e => e.FromId == currentNode.Id) < targetConnections)
                    {
                        Node randomNextNode = nextRowNodes[(int)(rng.NextULong() % (ulong)nextRowNodes.Count)];
                        if (!graph.Edges.Any(e => e.FromId == currentNode.Id && e.ToId == randomNextNode.Id))
                        {
                            graph.Edges.Add(new Edge { FromId = currentNode.Id, ToId = randomNextNode.Id });
                        }
                        // Prevent infinite loop if targetConnections is too high for available nextRowNodes
                        if (graph.Edges.Count(e => e.FromId == currentNode.Id) == nextRowNodes.Count) break;
                    }
                }
            }

            // 3. Reserve the top row for Boss; mark top-1 as preBossPortEligible.
            // This will be handled in Phase B (Typing Under Constraints) more explicitly.
            // For now, ensure the last node is marked as Boss type.
            Node bossNode = graph.Nodes.FirstOrDefault(n => n.Row == actSpec.Rows - 1);
            if (bossNode != null)
            {
                bossNode.Type = NodeType.Boss;
            }

            // Ensure at least one valid path from start to boss (basic check, more robust validation in Phase C)
            // This is implicitly handled by ensuring all nodes have in/out connections, but a dedicated pathfinding check is better.
            // For now, we assume the connection logic ensures reachability.

            return graph;
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
            // 1. Place Boss (already set in skeleton, but confirm/tag here)
            Node bossNode = graph.Nodes.FirstOrDefault(n => n.Row == actSpec.Rows - 1);
            if (bossNode != null)
            {
                Debug.Log($"Assigning Boss to row {actSpec.Rows - 1}");
                bossNode.Type = NodeType.Boss;
                bossNode.Tags.Add("boss");
            }

            // Keep track of placed nodes to avoid re-assigning types
            HashSet<string> placedNodeIds = new HashSet<string>();
            if (bossNode != null) placedNodeIds.Add(bossNode.Id);

            // Helper to get available nodes (not yet placed)
            Func<int, List<Node>> getAvailableNodesInRow = (row) => 
                graph.Nodes.Where(n => n.Row == row && !placedNodeIds.Contains(n.Id)).ToList();

            // 2. Place Pre-boss Port
            int preBossRow = actSpec.Rows - 2; // Row immediately before the boss
            if (preBossRow >= 0)
            {
                List<Node> availablePreBossNodes = getAvailableNodesInRow(preBossRow);
                if (availablePreBossNodes.Any())
                {
                    Node portNode = availablePreBossNodes[(int)(rng.NextULong() % (ulong)availablePreBossNodes.Count)];
                    portNode.Type = NodeType.Port;
                    portNode.Tags.Add("preBossPort");
                    placedNodeIds.Add(portNode.Id);
                }
                else
                {
                    // This should ideally not happen if skeleton generation is robust
                    // Log a warning or handle as an error for validation phase
                    Debug.Log($"Warning: No available nodes in pre-boss row {preBossRow} for Port.");
                }
            }

            // 3. Place Mid-act Treasure
            if (rules.Windows.MidTreasureRows != null && rules.Windows.MidTreasureRows.Any())
            {
                List<Node> potentialTreasureNodes = new List<Node>();
                foreach (int row in rules.Windows.MidTreasureRows)
                {
                    if (row >= 0 && row < actSpec.Rows - 1) // Ensure within valid range and not boss row
                    {
                        potentialTreasureNodes.AddRange(getAvailableNodesInRow(row));
                    }
                }

                if (potentialTreasureNodes.Any())
                {
                    Node treasureNode = potentialTreasureNodes[(int)(rng.NextULong() % (ulong)potentialTreasureNodes.Count)];
                    treasureNode.Type = NodeType.Treasure;
                    treasureNode.Tags.Add("midActTreasure");
                    placedNodeIds.Add(treasureNode.Id);
                }
                else
                {
                    Debug.Log("Warning: No available nodes in mid-treasure rows for Treasure.");
                }
            }

            // Initialize counts for each type based on rules.Counts.Targets
            Dictionary<NodeType, int> desiredCounts = new Dictionary<NodeType, int>();
            foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
            {
                desiredCounts[type] = rules.Counts.Targets.GetValueOrDefault(type, 0);
            }

            // Adjust desired counts for already placed guaranteed nodes
            foreach (Node node in graph.Nodes.Where(n => placedNodeIds.Contains(n.Id)))
            {
                if (desiredCounts.ContainsKey(node.Type))
                {
                    desiredCounts[node.Type]--;
                }
            }

            // 4. Place Elites
            // Helper to check if a node is too close to an already placed node of a specific type
            Func<Node, NodeType, int, bool> isTooClose = (newNode, typeToCheck, minGap) =>
            {
                return graph.Nodes.Any(n => n.Type == typeToCheck && Math.Abs(n.Row - newNode.Row) < minGap);
            };

            int elitesToPlace = desiredCounts.GetValueOrDefault(NodeType.Elite, 0);
            bool burningElitePlaced = false;

            // Prioritize placing elites in later rows first to respect early-row cap
            for (int r = actSpec.Rows - 2; r >= 0 && elitesToPlace > 0; r--)
            {
                if (r < rules.Spacing.EliteEarlyRowsCap) continue; // Respect early-row cap

                List<Node> availableNodesInRow = getAvailableNodesInRow(r);
                List<Node> eligibleNodes = availableNodesInRow.Where(n => !isTooClose(n, NodeType.Elite, rules.Spacing.EliteMinGap)).ToList();

                if (eligibleNodes.Any())
                {
                    // Try to place one elite per eligible row
                    Node eliteNode = eligibleNodes[(int)(rng.NextULong() % (ulong)eligibleNodes.Count)];
                    eliteNode.Type = NodeType.Elite;
                    placedNodeIds.Add(eliteNode.Id);
                    elitesToPlace--;

                    // Place Burning Elite if enabled and not yet placed
                    if (actSpec.Flags.EnableBurningElites && !burningElitePlaced)
                    {
                        eliteNode.Tags.Add("burning");
                        burningElitePlaced = true;
                    }
                }
            }

            // If we still need to place elites and couldn't due to early-row cap, try placing them earlier if allowed by min/max
            // This is a fallback and might violate early-row cap if min count is high
            for (int r = 0; r < actSpec.Rows - 1 && elitesToPlace > 0; r++)
            {
                List<Node> availableNodesInRow = getAvailableNodesInRow(r);
                List<Node> eligibleNodes = availableNodesInRow.Where(n => !isTooClose(n, NodeType.Elite, rules.Spacing.EliteMinGap)).ToList();

                if (eligibleNodes.Any())
                {
                    Node eliteNode = eligibleNodes[(int)(rng.NextULong() % (ulong)eligibleNodes.Count)];
                    eliteNode.Type = NodeType.Elite;
                    placedNodeIds.Add(eliteNode.Id);
                    elitesToPlace--;

                    if (actSpec.Flags.EnableBurningElites && !burningElitePlaced)
                    {
                        eliteNode.Tags.Add("burning");
                        burningElitePlaced = true;
                    }
                }
            }

            // Update desired counts after placing elites
            desiredCounts[NodeType.Elite] = elitesToPlace;

            // 5. Place Shops
            int shopsToPlace = desiredCounts.GetValueOrDefault(NodeType.Shop, 0);
            for (int r = 0; r < actSpec.Rows - 1 && shopsToPlace > 0; r++)
            {
                List<Node> availableNodesInRow = getAvailableNodesInRow(r);
                List<Node> eligibleNodes = availableNodesInRow.Where(n => !isTooClose(n, NodeType.Shop, rules.Spacing.ShopMinGap)).ToList();

                if (eligibleNodes.Any())
                {
                    Node shopNode = eligibleNodes[(int)(rng.NextULong() % (ulong)eligibleNodes.Count)];
                    shopNode.Type = NodeType.Shop;
                    placedNodeIds.Add(shopNode.Id);
                    shopsToPlace--;
                }
            }
            desiredCounts[NodeType.Shop] = shopsToPlace;

            // 6. Place Ports (remaining)
            int portsToPlace = desiredCounts.GetValueOrDefault(NodeType.Port, 0);
            for (int r = 0; r < actSpec.Rows - 1 && portsToPlace > 0; r++)
            {
                List<Node> availableNodesInRow = getAvailableNodesInRow(r);
                List<Node> eligibleNodes = availableNodesInRow.Where(n => !isTooClose(n, NodeType.Port, rules.Spacing.PortMinGap)).ToList();

                if (eligibleNodes.Any())
                {
                    Node portNode = eligibleNodes[(int)(rng.NextULong() % (ulong)eligibleNodes.Count)];
                    portNode.Type = NodeType.Port;
                    placedNodeIds.Add(portNode.Id);
                    portsToPlace--;
                }
            }
            desiredCounts[NodeType.Port] = portsToPlace;

            // 7. Fill Remaining Nodes (Unknowns, Battles)
            List<Node> unplacedNodes = graph.Nodes.Where(n => !placedNodeIds.Contains(n.Id)).ToList();
            foreach (Node node in unplacedNodes)
            {
                if (desiredCounts.GetValueOrDefault(NodeType.Unknown, 0) > 0)
                {
                    node.Type = NodeType.Unknown;
                    desiredCounts[NodeType.Unknown]--;
                }
                else
                {
                    Debug.LogWarning($"Node {node.Id} is being assigned as a Battle encounter. This is likely because all specific node types (Boss, Port, Treasure, Elite, Shop) have been placed, and the target count for Unknown nodes has been exhausted. Consider adjusting rules.Counts.Targets to include more non-Battle node types or Unknown nodes.");
                    node.Type = NodeType.Battle;
                    desiredCounts[NodeType.Battle]--;
                }
                placedNodeIds.Add(node.Id);
            }

            // Debugging: Display final NodeType assignment for each node
            Debug.Log("--- Final Node Type Assignments ---");
            foreach (Node node in graph.Nodes.OrderBy(n => n.Row).ThenBy(n => n.Col))
            {
                Debug.Log($"Node ID: {node.Id}, Type: {node.Type}");
            }
            Debug.Log("-----------------------------------");
        }
    }
}
