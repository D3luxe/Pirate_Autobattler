using System;
using System.Collections.Generic;

namespace Pirate.MapGen
{
    [Serializable]
    public class MapGraphData
    {
        public int rows;
        public List<Node> nodes;
        public List<Edge> edges;
        public SubSeeds subSeeds;
        public Constants constants;

        [Serializable]
        public class Node
        {
            public string id;
            public int row;
            public int col;
            public string type; // Corresponds to NodeType enum name (e.g., "Battle", "Elite")
            public List<string> tags; // e.g., "boss-preview", "burning-elite", "meta-key"
        }

        [Serializable]
        public class Edge
        {
            public string fromId;
            public string toId;
        }

        [Serializable]
        public class SubSeeds
        {
            public ulong decorations;
        }

        [Serializable]
        public class Constants
        {
            public float rowHeight;
            public float laneWidth;
            public float mapPaddingX;
            public float mapPaddingY;
            public float minHorizontalSeparation;
            public float jitter;
        }
    }
}
