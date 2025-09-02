using UnityEngine;
using UnityEditor;
using Pirate.MapGen;

public class CreateMapManagerPrefab
{
    [MenuItem("Game/Tools/Create MapManager Prefab")]
    public static void CreatePrefab()
    {
        var go = new GameObject("MapManager");
        go.AddComponent<MapManager>();
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/MapManager.prefab");
        GameObject.DestroyImmediate(go);
    }
}
