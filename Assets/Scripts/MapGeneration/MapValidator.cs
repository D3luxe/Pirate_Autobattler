using System;
using System.Collections.Generic;
using System.Linq;

namespace Pirate.MapGen
{
    public class MapValidator
    {
        /// <summary>
        /// Validates the generated map graph against a set of rules.
        /// </summary>
        /// <param name="graph">The map graph to validate.</param>
        /// <param name="rules">The rules to validate against.</param>
        /// <returns>An AuditReport detailing any violations.</returns>
        public AuditReport Validate(MapGraph graph, RulesSO rules)
        {
            AuditReport report = new AuditReport { IsValid = true };

            // Rule 1: At least one path exists from start to boss
            if (!HasPathToBoss(graph))
            {
                report.IsValid = false;
                report.Violations.Add("No valid path from start to boss.");
            }

            // Rule 2: Pre-boss Port present
            if (!HasPreBossPort(graph))
            {
                report.IsValid = false;
                report.Violations.Add("No Port node found on the row immediately before the Boss.");
            }

            // Rule 3: Mid-act Treasure present
            if (!HasMidActTreasure(graph, rules))
            {
                report.IsValid = false;
                report.Violations.Add("No Treasure node found within the specified mid-act window.");
            }


            // Rule 4: Counts within limits (min/max)
            var countViolations = AreCountsWithinLimits(graph, rules);
            if (countViolations.Any())
            {
                report.IsValid = false;
                report.Violations.AddRange(countViolations);
            }

            // Rule 5: Spacing rules respected
            var spacingViolations = AreSpacingRulesRespected(graph, rules);
            if (spacingViolations.Any())
            {
                report.IsValid = false;
                report.Violations.AddRange(spacingViolations);
            }

            // Rule 6: Elite early rows cap respected
            if (!IsEliteEarlyRowsCapRespected(graph, rules))
            {
                report.IsValid = false;
                report.Violations.Add($"Elite found in row <= {rules.Spacing.EliteEarlyRowsCap}.");
            }

            return report;
        }

        /// <summary>
        /// Checks if at least one path exists from the starting node (row 0) to the boss node (last row).
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <returns>True if a path exists, false otherwise.</returns>
        private bool HasPathToBoss(MapGraph graph)
        {
            if (!graph.Nodes.Any() || graph.Rows == 0) return false;

            Node startNode = graph.Nodes.FirstOrDefault(n => n.Row == 0);
            Node bossNode = graph.Nodes.FirstOrDefault(n => n.Row == graph.Rows - 1 && n.Type == NodeType.Boss);

            if (startNode == null || bossNode == null) return false;

            Queue<Node> queue = new Queue<Node>();
            HashSet<string> visited = new HashSet<string>();

            queue.Enqueue(startNode);
            visited.Add(startNode.Id);

            while (queue.Any())
            {
                Node current = queue.Dequeue();

                if (current.Id == bossNode.Id) return true;

                // Find outgoing edges from the current node
                var outgoingEdges = graph.Edges.Where(e => e.FromId == current.Id);
                foreach (var edge in outgoingEdges)
                {
                    Node neighbor = graph.Nodes.FirstOrDefault(n => n.Id == edge.ToId);
                    if (neighbor != null && !visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if there is at least one Port node on the row immediately before the Boss.
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <returns>True if a pre-boss Port exists, false otherwise.</returns>
        private bool HasPreBossPort(MapGraph graph)
        {
            if (graph.Rows < 2) return false; // Need at least 2 rows for a pre-boss row

            int preBossRow = graph.Rows - 2;
            return graph.Nodes.Any(n => n.Row == preBossRow && n.Type == NodeType.Port);
        }

        /// <summary>
        /// Checks if there is at least one Treasure node within the specified mid-act window.
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <param name="rules">The rules containing the mid-act window definition.</param>
        /// <returns>True if a mid-act Treasure exists, false otherwise.</returns>
        private bool HasMidActTreasure(MapGraph graph, RulesSO rules)
        {
            if (rules.Windows.MidTreasureRows == null || !rules.Windows.MidTreasureRows.Any()) return true; // No rule, so it's valid

            foreach (int row in rules.Windows.MidTreasureRows)
            {
                if (graph.Nodes.Any(n => n.Row == row && n.Type == NodeType.Treasure))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the counts of each node type are within the specified min/max limits.
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <param name="rules">The rules containing the count limits.</param>
        /// <returns>A list of violation messages.</returns>
        private List<string> AreCountsWithinLimits(MapGraph graph, RulesSO rules)
        {
            List<string> violations = new List<string>();
            var nodeCounts = graph.Nodes.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count());

            foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
            {
                int count = nodeCounts.GetValueOrDefault(type, 0);

                if (rules.Counts.Min.ContainsKey(type) && count < rules.Counts.Min[type])
                {
                    violations.Add($"Count for {type} ({count}) is below minimum ({rules.Counts.Min[type]}).");
                }
                if (rules.Counts.Max.ContainsKey(type) && count > rules.Counts.Max[type])
                {
                    violations.Add($"Count for {type} ({count}) is above maximum ({rules.Counts.Max[type]}).");
                }
            }
            return violations;
        }

        /// <summary>
        /// Checks if spacing rules are respected for Elite, Shop, and Port nodes.
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <param name="rules">The rules containing the spacing limits.</param>
        /// <returns>A list of violation messages.</returns>
        private List<string> AreSpacingRulesRespected(MapGraph graph, RulesSO rules)
        {
            List<string> violations = new List<string>();

            // Elite spacing
            if (rules.Spacing.EliteMinGap > 0)
            {
                var eliteNodes = graph.Nodes.Where(n => n.Type == NodeType.Elite).OrderBy(n => n.Row).ToList();
                for (int i = 1; i < eliteNodes.Count; i++)
                {
                    if (eliteNodes[i].Row - eliteNodes[i - 1].Row < rules.Spacing.EliteMinGap)
                    {
                        violations.Add($"Elite spacing violation between row {eliteNodes[i - 1].Row} and {eliteNodes[i].Row}. Minimum gap is {rules.Spacing.EliteMinGap}.");
                    }
                }
            }

            // Shop spacing
            if (rules.Spacing.ShopMinGap > 0)
            {
                var shopNodes = graph.Nodes.Where(n => n.Type == NodeType.Shop).OrderBy(n => n.Row).ToList();
                for (int i = 1; i < shopNodes.Count; i++)
                {
                    if (shopNodes[i].Row - shopNodes[i - 1].Row < rules.Spacing.ShopMinGap)
                    {
                        violations.Add($"Shop spacing violation between row {shopNodes[i - 1].Row} and {shopNodes[i].Row}. Minimum gap is {rules.Spacing.ShopMinGap}.");
                    }
                }
            }

            // Port spacing
            if (rules.Spacing.PortMinGap > 0)
            {
                var portNodes = graph.Nodes.Where(n => n.Type == NodeType.Port).OrderBy(n => n.Row).ToList();
                for (int i = 1; i < portNodes.Count; i++)
                {
                    if (portNodes[i].Row - portNodes[i - 1].Row < rules.Spacing.PortMinGap)
                    {
                        violations.Add($"Port spacing violation between row {portNodes[i - 1].Row} and {portNodes[i].Row}. Minimum gap is {rules.Spacing.PortMinGap}.");
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// Checks if the EliteEarlyRowsCap is respected.
        /// </summary>
        /// <param name="graph">The map graph.</param>
        /// <param name="rules">The rules containing the EliteEarlyRowsCap.</param>
        /// <returns>True if the cap is respected, false otherwise.</returns>
        private bool IsEliteEarlyRowsCapRespected(MapGraph graph, RulesSO rules)
        {
            if (rules.Spacing.EliteEarlyRowsCap <= 0) return true; // No cap, so it's respected

            return !graph.Nodes.Any(n => n.Type == NodeType.Elite && n.Row < rules.Spacing.EliteEarlyRowsCap);
        }
    }
}
