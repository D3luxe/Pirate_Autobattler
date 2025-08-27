using System.Collections.Generic;
using System.Linq;
using PirateRoguelike.Data;
using System; // Added for Math.Min

namespace Pirate.MapGen
{
    public static class Unknowns
    {
        public static UnknownOutcome ResolveUnknown(
            MapNodeData nodeId, // Using MapNodeData for now, can be refined to NodeId later
            UnknownContext ctx,
            IRandomNumberGenerator rng,
            PityState currentPityState,
            Rules rules)
        {
            // 1) weights := start + pityAccumulated
            Dictionary<NodeType, int> currentWeights = new Dictionary<NodeType, int>();
            foreach (var entry in rules.UnknownWeights.Start)
            {
                currentWeights[entry.Key] = entry.Value + currentPityState.PityAccumulated[entry.Key];
            }

            // 2) weights := ApplyModifiers(weights, modifiers)
            foreach (var entry in ctx.Modifiers)
            {
                currentWeights[entry.Key] = (int)(currentWeights[entry.Key] * entry.Value);
            }

            // 3) outcome := SampleDiscrete(weights, rng)
            NodeType outcome = SampleDiscrete(currentWeights, rng);

            // 4) Increase pity for all outcomes ≠ outcome, clamp by caps
            PityState newPityState = new PityState();
            foreach (var entry in currentPityState.PityAccumulated)
            {
                if (entry.Key != outcome)
                {
                    newPityState.PityAccumulated[entry.Key] = System.Math.Min(
                        entry.Value + rules.UnknownWeights.Pity[entry.Key],
                        rules.UnknownWeights.Caps[entry.Key]
                    );
                }
                else
                {
                    newPityState.PityAccumulated[entry.Key] = 0; // Reset pity for the chosen outcome
                }
            }

            // 5) Return { outcome, newPityState }
            return new UnknownOutcome { Outcome = outcome, NewPityState = newPityState };
        }

        private static NodeType SampleDiscrete(Dictionary<NodeType, int> weights, IRandomNumberGenerator rng)
        {
            int totalWeight = weights.Sum(x => x.Value);
            if (totalWeight <= 0) return NodeType.Battle; // Fallback

            int randomNumber = (int)(rng.NextULong() % (ulong)totalWeight);

            foreach (var entry in weights)
            {
                if (randomNumber < entry.Value)
                {
                    return entry.Key;
                }
                randomNumber -= entry.Value;
            }
            return weights.Keys.Last(); // Should not happen if totalWeight > 0
        }
    }

    public class UnknownOutcome
    {
        public NodeType Outcome { get; set; }
        public PityState NewPityState { get; set; }
    }
}
