using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pirate.MapGen
{
    [Serializable]
    public class UnknownResolution
    {
        [Tooltip("If true, Unknown nodes resolve independently of adjacency rules.")]
        public bool IgnoreAdjacencyRulesOnResolve = true;
        [Tooltip("If true, Unknown nodes still respect structural bans on resolve.")]
        public bool RespectStructuralBansOnResolve = true;

        [Header("Pity System Settings")]
        public float BattlePityBase = 0.10f;
        public float BattlePityIncrement = 0.10f;
        public float TreasurePityBase = 0.02f;
        public float TreasurePityIncrement = 0.02f;
        public float ShopPityBase = 0.03f;
        public float ShopPityIncrement = 0.03f;
        [Tooltip("If no other type procs, fallback to Event.")]
        public bool FallbackToEvent = true;
        [Tooltip("If true, pity counts are reset per act.")]
        public bool PityPerAct = true;
    }
}