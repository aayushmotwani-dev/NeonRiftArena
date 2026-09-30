#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonRift.Editor
{
    public static class NeonRiftBuilder
    {
        const string ScenePath = "Assets/NeonRift/Scenes/NeonRiftArena.unity";

        [MenuItem("Neon Rift/Build Interview Demo")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Assets/NeonRift/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Neon Rift Bootstrap").AddComponent<NeonRiftGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Aayush Portfolio";
            PlayerSettings.productName = "Spatial Rift Lab";
            PlayerSettings.bundleVersion = "3.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.aayushportfolio.spatialriftlab");
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

            string buildDirectory = Path.GetFullPath("Build/Windows");
            if (Directory.Exists(buildDirectory)) Directory.Delete(buildDirectory, true);
            Directory.CreateDirectory(buildDirectory);
            string executable = Path.Combine(buildDirectory, "SpatialRiftLab.exe");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Neon Rift build failed: " + report.summary.result);
            Debug.Log("SPATIAL_RIFT_BUILD_OK: " + executable + " (" + report.summary.totalSize + " bytes)");
        }
    }
}
#endif
