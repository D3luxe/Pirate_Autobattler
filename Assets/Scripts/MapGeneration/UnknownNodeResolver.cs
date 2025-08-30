using System;
using System.Collections.Generic;
using UnityEngine;
using PirateRoguelike.Data;

namespace Pirate.MapGen
{
    public class UnknownNodeResolver
    {
        /// <summary>
        /// Resolves an Unknown node to a specific NodeType based on pity system and rules.
        /// </summary>
        /// <param name="unknownNode">The node to resolve.</param>
        /// <param name="actSpec">The current act specification.</param>
        /// <param name="rules">The map generation rules.</param>
        /// <param name="rng">The random number generator for deterministic resolution.</param>
        /// <param name="pityState">The current pity state.</param>
        /// <param name="unknownContext">The context for unknown node resolution, including modifiers.</param>
        /// <returns>The resolved NodeType.</returns>
        public NodeType ResolveUnknownNode(Node unknownNode, ActSpec actSpec, RulesSO rules, IRandomNumberGenerator rng, PityState pityState, UnknownContext unknownContext)
        {
            // Use UnknownWeights from rules
            UnknownWeights uw = rules.UnknownWeights;

            // Calculate base probabilities using PityAccumulated from PityState
            float battleProb = Mathf.Clamp(uw.BattlePityBase + uw.BattlePityIncrement * pityState.PityAccumulated[NodeType.Battle], 0f, 1f);
            float treasureProb = Mathf.Clamp(uw.TreasurePityBase + uw.TreasurePityIncrement * pityState.PityAccumulated[NodeType.Treasure], 0f, 1f);
            float shopProb = Mathf.Clamp(uw.ShopPityBase + uw.ShopPityIncrement * pityState.PityAccumulated[NodeType.Shop], 0f, 1f);

            // Apply modifiers from UnknownContext
            battleProb *= unknownContext.Modifiers.GetValueOrDefault(NodeType.Battle, 1.0f);
            treasureProb *= unknownContext.Modifiers.GetValueOrDefault(NodeType.Treasure, 1.0f);
            shopProb *= unknownContext.Modifiers.GetValueOrDefault(NodeType.Shop, 1.0f);

            // Determine eligible types based on structural bans
            List<NodeType> eligibleTypes = new List<NodeType> { NodeType.Battle, NodeType.Treasure, NodeType.Shop, NodeType.Event };

            // Rule 5: still respects structural bans (e.g., Port banned on R-2)
            // If the node is in the pre-pre-boss row (R-2 relative to R-1, which is actSpec.Rows - 3)
            // and Port is a potential resolution type, it should be banned.
            // Since Port is not a direct resolution type for Unknown, this specific ban doesn't apply directly here.
            // However, if any other structural ban were to apply to Battle, Treasure, Shop, or Event, it would be applied here.

            // For example, if a rule banned Battle nodes in the last row (which is Boss), we'd remove Battle from eligibleTypes.
            // Currently, no such structural bans are defined for Battle, Treasure, Shop, Event in the prompt for Unknown resolution.

            // Generate a random value for resolution
            float roll = (float)rng.NextDouble();

            NodeType resolvedType = NodeType.Event; // Default fallback

            // Sequential proc order with pity
            if (roll < battleProb && eligibleTypes.Contains(NodeType.Battle))
            {
                resolvedType = NodeType.Battle;
                pityState.ResetPity(NodeType.Battle);
                pityState.IncrementOtherPities(NodeType.Battle);
            }
            else if (roll < battleProb + treasureProb && eligibleTypes.Contains(NodeType.Treasure))
            {
                resolvedType = NodeType.Treasure;
                pityState.ResetPity(NodeType.Treasure);
                pityState.IncrementOtherPities(NodeType.Treasure);
            }
            else if (roll < battleProb + treasureProb + shopProb && eligibleTypes.Contains(NodeType.Shop))
            {
                resolvedType = NodeType.Shop;
                pityState.ResetPity(NodeType.Shop);
                pityState.IncrementOtherPities(NodeType.Shop);
            }
            else
            {
                // If none procs -> Event
                resolvedType = NodeType.Event;
                pityState.IncrementAllPities();
            }

            Debug.Log($"Resolved Unknown node {unknownNode.Id} (Row: {unknownNode.Row}) to {resolvedType}. Battle Pity: {pityState.PityAccumulated[NodeType.Battle]}, Treasure Pity: {pityState.PityAccumulated[NodeType.Treasure]}, Shop Pity: {pityState.PityAccumulated[NodeType.Shop]}");

            return resolvedType;
        }
    }
}
