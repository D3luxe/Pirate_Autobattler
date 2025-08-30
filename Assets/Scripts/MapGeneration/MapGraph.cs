using System;
using System.Collections.Generic;

namespace Pirate.MapGen
{
    [Serializable]
    public class MapGraph
    {
        public int Rows { get; set; }
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Edge> Edges { get; set; } = new List<Edge>();
    }

    [Serializable]
    public class Node
    {
        public string Id { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public NodeType Type { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        // Using a dictionary for meta to allow flexible key-value pairs
        public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        public HashSet<int> PathIndices { get; set; } = new HashSet<int>();
    }

    [Serializable]
    public class Edge
    {
        public string Id { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public HashSet<int> PathIndices { get; set; } = new HashSet<int>();
    }

    public enum NodeType
    {
        Battle,
        Elite,
        Port,
        Shop,
        Treasure,
        Event,
        Unknown,
        Boss
    }
}
