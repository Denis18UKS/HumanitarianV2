#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Humanitarian.Vehicles;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Humanitarian.EditorTools
{
    /// <summary>Selective conversion of the mixed Minecraft/Blender archive to a Unity 6 vehicle.</summary>
    public static class GhostCamaroUnityImporter
    {
        private const string Base = "Assets/Humanitarian/Vehicles/GhostCamaro";
        private const string FbxPath = Base + "/Models/GhostCamaro.fbx";
        private const string ControllerPath = Base + "/Animations/GhostCamaro_Preview.controller";
        private const string PrefabPath = Base + "/Prefabs/GhostCamaro.prefab";

        private static readonly (string zipEntry, string unityPath)[] Sources =
        {
            ("Exports/FBX/Vehicles/GhostCamaro.fbx", FbxPath),
            ("Textures/Vehicles/Ghost_Camaro_Textures.png", Base + "/Textures/Ghost_Camaro_Textures.png"),
            ("Textures/Vehicles/Ghost_Camaro_V5_FX.png", Base + "/Textures/Ghost_Camaro_V5_FX.png"),
            ("Textures/Vehicles/Glass_V5.png", Base + "/Textures/Glass_V5.png"),
            ("Textures/Vehicles/Side_Tint_V5.png", Base + "/Textures/Side_Tint_V5.png")
        };

        [MenuItem("Humanitarian/Ghost Camaro/1. Import from original mixed ZIP...")]
        public static void ImportMixedArchive()
        {
            string path = EditorUtility.OpenFilePanel("Select GhostCamaro-NEXT-FULL-PROJECT ZIP", "", "zip");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    foreach (var entry in Sources)
                    {
                        var member = archive.GetEntry(entry.zipEntry);
                        if (member == null) throw new FileNotFoundException("Required asset missing from archive", entry.zipEntry);
                        string destination = Path.GetFullPath(entry.unityPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        using (var input = member.Open())
                        using (var output = File.Create(destination)) input.CopyTo(output);
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                BuildPrefab();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Ghost Camaro import failed", exception.Message, "OK");
            }
        }

        [MenuItem("Humanitarian/Ghost Camaro/2. Rebuild preview clips and prefab")]
        public static void BuildPrefab()
        {
            if (!File.Exists(FbxPath))
            {
                EditorUtility.DisplayDialog("Ghost Camaro", "No FBX in " + FbxPath + ". Run step 1 first.", "OK");
                return;
            }

            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("The imported asset is not an FBX ModelImporter.");
            bool update = !importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic;
            if (update)
            {
                importer.importAnimation = true;
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importBlendShapes = true;
                importer.SaveAndReimport();
            }
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (asset == null) throw new InvalidOperationException("Unity could not import GhostCamaro.fbx as GameObject.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .GroupBy(c => CleanName(c.name), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(c => c.name)
                .ToList();

            EnsureDirectory(Base + "/Animations");
            EnsureDirectory(Base + "/Prefabs");
            // Preview controller: plays one clip at a time, NOT multi-layer gameplay animations.
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var neutral = machine.AddState("Neutral");
            machine.defaultState = neutral;
            foreach (var clip in clips)
            {
                var state = machine.AddState(CleanName(clip.name));
                state.motion = clip;
                state.writeDefaultValues = false;
            }

            var root = new GameObject("GhostCamaro");
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                var animator = visual.GetComponent<Animator>() ?? visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                var rig = root.AddComponent<GhostCamaroRigReferences>();
                rig.visualRoot = visual.transform;
                rig.AutoBind();
                var preview = root.AddComponent<GhostCamaroAnimationPreview>();
                preview.animator = animator;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string message = "Imported " + clips.Count + " Unity animation clips from FBX. " +
                             "Source archive GLB has 62 named clips; check FBX clips and mapping in Inspector. " +
                             "Preview prefab: " + PrefabPath;
            Debug.Log(message);
            EditorUtility.DisplayDialog("Ghost Camaro imported", message +
                "\n\nThis imports visuals and preview clips, not driving physics.", "OK");
        }

        private static string CleanName(string name)
        {
            int index = name.LastIndexOf('|');
            return index >= 0 ? name.Substring(index + 1) : name;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif
