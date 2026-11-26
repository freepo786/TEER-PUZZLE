using System.IO;
using UnityEditor;
using UnityEngine;

namespace TeerPuzzle.Editor
{
    /// <summary>
    /// Teer Puzzle v1.4 — CI scene-build step + Remove Ads par banner hide.
    /// Unity mein: menu "Teer Puzzle" &gt; "Apply Project Settings (v1.4)".
    /// Ye script player settings (naam, package, IL2CPP, SDK levels) lagata hai
    /// aur MainScene.unity (agar maujood ho) ko Build Settings mein dalta hai.
    /// </summary>
    public static class ProjectSetup
    {
        public const string Version = "1.4";
        public const string BundleIdPlaceholder = "com.rehan.teerpuzzle";

        [MenuItem("Teer Puzzle/Apply Project Settings (v1.4)")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = "Rehan";
            PlayerSettings.productName = "Teer Puzzle";
            PlayerSettings.bundleVersion = "1.4.0";
            PlayerSettings.Android.bundleVersionCode = 4;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleIdPlaceholder);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            // API levels as ints (26 = Android 8.0, 36 = Android 16) — cast taake
            // enum member na hone par bhi compile ho.
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;

            string scenePath = "Assets/Scenes/MainScene.unity";
            if (File.Exists(scenePath))
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(scenePath, true)
                };
                Debug.Log("[Teer Puzzle] MainScene build settings mein add kar di.");
            }
            else
            {
                Debug.LogWarning("[Teer Puzzle] MainScene.unity abhi nahi mili — pehle Tools > Teer Puzzle > Build Main Scene chalao, phir ye dobara chalao.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Teer Puzzle] v1.4 project settings apply ho gayin. Bundle ID (placeholder): " + BundleIdPlaceholder);
        }
    }
}
