#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Humanitarian.EditorTools
{
    /// <summary>
    /// A GitHub checkout includes 43 binary fragments of the *original* vehicle-only asset ZIP.
    /// Reassemble, SHA-256 verify and extract only the Ghost Camaro FBX and four PNG textures.
    /// No manual importing of a Minecraft archive is required.
    /// </summary>
    public sealed class GhostCamaroAutoBootstrap : AssetPostprocessor
    {
        private const string Base = "Assets/Humanitarian/Vehicles/GhostCamaro";
        private const string ScenePath = "Assets/Humanitarian/Scenes/GhostCamaro_DrivingDemo.unity";
        private const string ModelPath = Base + "/Models/GhostCamaro.fbx";
        private const string PartsDirectory = Base + "/Source/Parts";
        private const int PartCount = 43;
        private const string ExpectedSha256 = "a3f68a701ef60946c1c93fc5a0add5d9c400110b7be515010a33dea18cbc7348";
        private static bool busy;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += FirstRun;
        }

        private static void FirstRun()
        {
            if (busy || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += FirstRun;
                return;
            }
            if (File.Exists(ScenePath) && File.Exists(ModelPath)) return;
            if (!File.Exists(ModelPath) && File.Exists(PartPath(0)))
            {
                try
                {
                    busy = true;
                    ExtractOriginalModel();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorUtility.DisplayDialog("Humanitarian: Ghost Camaro assets",
                        "Unable to reassemble the original model from GitHub:\n" +
                        exception.Message, "OK");
                    return;
                }
                finally { busy = false; }
                EditorApplication.delayCall += FirstRun;
                return;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogWarning("Ghost Camaro: save the active scene first, then use " +
                    "Humanitarian > Ghost Camaro > 3. Build DRIVEABLE car + demo scene.");
                return;
            }
            RunDemoBuild();
        }

        private static string PartPath(int index) =>
            PartsDirectory + "/part" + index.ToString("000") + ".bin";

        private static void ExtractOriginalModel()
        {
            for (int i = 0; i < PartCount; i++)
                if (!File.Exists(PartPath(i)))
                    throw new FileNotFoundException("Missing binary asset chunk " + i, PartPath(i));

            string mergedZip = Path.GetFullPath("Library/GhostCamaro_OriginalUnityAssets.zip");
            Directory.CreateDirectory(Path.GetDirectoryName(mergedZip));
            using (var target = new FileStream(mergedZip, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < PartCount; i++)
                using (var source = File.OpenRead(PartPath(i)))
                    source.CopyTo(target);
            }
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(mergedZip))
            {
                string digest = BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", "").ToLowerInvariant();
                if (digest != ExpectedSha256)
                    throw new InvalidDataException("Asset checksum mismatch: " + digest +
                        ". Expected " + ExpectedSha256);
            }

            string[] required = {
                "Models/GhostCamaro.fbx",
                "Textures/Ghost_Camaro_Textures.png",
                "Textures/Ghost_Camaro_V5_FX.png",
                "Textures/Glass_V5.png",
                "Textures/Side_Tint_V5.png"
            };
            using (var archive = ZipFile.OpenRead(mergedZip))
            {
                foreach (string relative in required)
                {
                    var entry = archive.GetEntry(relative);
                    if (entry == null)
                        throw new FileNotFoundException("Asset ZIP missing: " + relative);
                }
                foreach (string relative in required)
                {
                    var entry = archive.GetEntry(relative);
                    string destination = Path.GetFullPath(Base + "/" + relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var input = entry.Open())
                    using (var output = File.Create(destination))
                        input.CopyTo(output);
                }
            }
            Debug.Log("Ghost Camaro: verified original FBX and 4 textures from GitHub (SHA-256 OK).");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void RunDemoBuild()
        {
            if (busy) return;
            try
            {
                busy = true;
                GhostCamaroPlayableSetup.BuildDriveable();
            }
            finally { busy = false; }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (busy || !importedAssets.Contains(ModelPath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                    EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) return;
                RunDemoBuild();
            };
        }
    }
}
#endif
