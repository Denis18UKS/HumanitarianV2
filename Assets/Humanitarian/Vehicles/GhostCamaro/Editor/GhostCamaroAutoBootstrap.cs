#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Humanitarian.EditorTools
{
    /// <summary>Builds an initial Play-ready demo scene on first editor load only.
    /// Safe around unsaved scenes; the manual menu command always remains available.</summary>
    public sealed class GhostCamaroAutoBootstrap : AssetPostprocessor
    {
        private const string ScenePath = "Assets/Humanitarian/Scenes/GhostCamaro_DrivingDemo.unity";
        private const string ModelPath = "Assets/Humanitarian/Vehicles/GhostCamaro/Models/GhostCamaro.fbx";

        [InitializeOnLoadMethod]
        private static void OnScriptsLoaded()
        {
            EditorApplication.delayCall += InitialScene;
        }

        private static void InitialScene()
        {
            if (File.Exists(ScenePath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InitialScene;
                return;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.Log("Ghost Camaro auto-setup paused: current scene has unsaved changes. " +
                    "Use Humanitarian > Ghost Camaro > 3. Build DRIVEABLE car + demo scene.");
                return;
            }
            GhostCamaroPlayableSetup.BuildDriveable();
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool modelJustArrived = false;
            foreach (string path in importedAssets)
                if (path == ModelPath) { modelJustArrived = true; break; }
            if (!modelJustArrived) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                {
                    Debug.Log("Ghost Camaro FBX arrived. Use Humanitarian > Ghost Camaro > 3. " +
                        "Build DRIVEABLE car + demo scene after saving your open scene.");
                    return;
                }
                GhostCamaroPlayableSetup.BuildDriveable();
            };
        }
    }
}
#endif