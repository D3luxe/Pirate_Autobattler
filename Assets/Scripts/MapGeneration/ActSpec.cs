using System;
using System.Collections.Generic;

namespace Pirate.MapGen
{
    [Serializable]
    public class ActSpec
    {
        public string ActId { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public float Branchiness { get; set; } // 0..1 target edges per node
        public ActFlags Flags { get; set; } = new ActFlags();
    }

    [Serializable]
    public class ActFlags
    {
        public bool EnableMetaKeys { get; set; }
        public bool EnableBurningElites { get; set; }
    }
}
