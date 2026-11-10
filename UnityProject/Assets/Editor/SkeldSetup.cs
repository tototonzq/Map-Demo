using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SkeldMap.EditorTools
{
    // เมนู Skeld ▸ Setup Project — สร้าง Scene พร้อมเล่น + ตั้งค่าโปรเจกต์ที่เหมาะกับเกมนี้
    public static class SkeldSetup
    {
        const string ScenePath = "Assets/Scenes/Skeld.unity";

        [MenuItem("Skeld/Setup Project (Scene + Settings)")]
        public static void Setup()
        {
            PlayerSettings.productName = "Patient Zero";
            PlayerSettings.colorSpace = ColorSpace.Linear; // three.js คำนวณแสงแบบ linear → ตั้งให้ตรงกัน

            ReimportKenneyAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();

            new GameObject("SkeldGame").AddComponent<SkeldGame>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Skeld] สร้าง " + ScenePath + " แล้ว — กด Play ได้เลย");
        }

        // บังคับ import ชุด Kenney ใหม่ เพื่อให้กฎใน KenneyImportPostprocessor (Read/Write, Point filter) มีผลกับไฟล์เดิม
        [MenuItem("Skeld/Reimport Kenney Assets")]
        public static void ReimportKenneyAssets()
        {
            foreach (var dir in new[] { "Assets/Resources/SpaceKit", "Assets/Resources/Characters", "Assets/Resources/MedicalAids" })
                if (Directory.Exists(dir))
                    AssetDatabase.ImportAsset(dir, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        }

        // เรียกจาก command line: Unity -batchmode -quit -executeMethod SkeldMap.EditorTools.SkeldSetup.SetupBatch
        public static void SetupBatch() => Setup();
    }
}
