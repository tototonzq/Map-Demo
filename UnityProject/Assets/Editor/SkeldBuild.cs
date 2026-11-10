using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkeldMap.EditorTools
{
    // เมนู Skeld ▸ Build Windows (x64) → Builds/Windows/PatientZero.exe
    public static class SkeldBuild
    {
        const string ScenePath = "Assets/Scenes/Skeld.unity";
        const string OutExe = "Builds/Windows/PatientZero.exe";

        [MenuItem("Skeld/Build Windows (x64)")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) SkeldSetup.Setup();
            Directory.CreateDirectory(Path.GetDirectoryName(OutExe));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            Debug.Log($"[SkeldBuild] {report.summary.result} → {Path.GetFullPath(OutExe)}");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
