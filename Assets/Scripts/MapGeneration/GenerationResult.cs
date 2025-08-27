using System;
using System.Collections.Generic;

namespace Pirate.MapGen
{
    [Serializable]
    public class GenerationResult
    {
        public MapGraph Graph { get; set; }
        public AuditReport Audits { get; set; }
        public ulong Seed { get; set; }
        public SubSeeds SubSeeds { get; set; } = new SubSeeds();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [Serializable]
    public class AuditReport
    {
        public bool IsValid { get; set; }
        public List<string> Violations { get; set; } = new List<string>();
    }

    [Serializable]
    public class SubSeeds
    {
        public ulong Skeleton { get; set; }
        public ulong Typing { get; set; }
        public ulong Repairs { get; set; }
        public ulong Decorations { get; set; }
        public ulong UnknownResolution { get; set; }
    }
}
