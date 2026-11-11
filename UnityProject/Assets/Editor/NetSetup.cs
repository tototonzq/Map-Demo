using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace SkeldMap.EditorTools
{
    // เมนู Skeld ▸ Create Net Prefab — สร้าง Assets/Resources/Net/NetPlayer.prefab (NetworkObject + NetPlayer)
    public static class NetSetup
    {
        const string Dir = "Assets/Resources/Net";
        const string PrefabPath = Dir + "/NetPlayer.prefab";

        [MenuItem("Skeld/Create Net Prefab")]
        public static void CreateNetPlayerPrefab()
        {
            Directory.CreateDirectory(Dir);
            var go = new GameObject("NetPlayer");
            go.AddComponent<NetworkObject>();
            go.AddComponent<NetPlayer>();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Skeld] สร้าง " + PrefabPath + " แล้ว");
        }
    }
}
