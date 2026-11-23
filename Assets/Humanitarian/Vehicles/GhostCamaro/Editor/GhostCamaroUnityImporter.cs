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

        [MenuItem("Humanitarian/Ghost Camaro/1. Import from ZIP (original or Unity package)...")]
        public static void ImportMixedArchive()
        {
            string path = EditorUtility.OpenFilePanel("Select original GhostCamaro ZIP or Unity Stage 1 ZIP", "", "zip");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    // Resolve and validate ALL required files before writing anything into Assets.
                    // The original mixed ZIP and the Unity-only package use different directories.
                    var selected = Sources.Select(source => new
                    {
                        source.zipEntry,
                        source.unityPath,
                        member = FindZipMember(archive, source.zipEntry, source.unityPath)
                    }).ToList();

                    var missing = selected.Where(x => x.member == null)
                        .Select(x => "- " + x.zipEntry + "\n  or " + x.unityPath)
                        .ToArray();
                    if (missing.Length > 0)
                    {
                        string availableFbx = string.Join("\n", archive.Entries
                            .Where(x => x.FullName.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                            .Take(8).Select(x => "  " + x.FullName));
                        throw new FileNotFoundException(
                            "This ZIP does not contain all required Ghost Camaro assets.\n\nMissing:\n" +
                            string.Join("\n", missing) +
                            "\n\nSelect GhostCamaro-NEXT-FULL-PROJECT(1).zip or " +
                            "Humanitarian_GhostCamaro_Unity_Import_Stage1.zip.\n" +
                            (availableFbx.Length > 0 ? "\nFBX files found:\n" + availableFbx : "\nNo FBX files were found."));
                    }

                    foreach (var item in selected)
                    {
                        string destination = Path.GetFullPath(item.unityPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        using (var input = item.member.Open())
                        using (var output = File.Create(destination))
                            input.CopyTo(output);
                        Debug.Log("Ghost Camaro ZIP: " + item.member.FullName + " -> " + item.unityPath);
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

        // Accept either source layout, including ZIPs with an extra top-level directory.
        // Never select unrelated Minecraft/Blockbench resources by filename alone.
        private static ZipArchiveEntry FindZipMember(ZipArchive archive, string originalPath, string unityPath)
        {
            string[] expectedPaths = { originalPath, unityPath };
            var files = archive.Entries.Where(x => !string.IsNullOrEmpty(x.Name)).ToArray();

            foreach (string expected in expectedPaths)
            {
                var exact = files.FirstOrDefault(x =>
                    string.Equals(NormalizeZipPath(x.FullName), expected, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;
            }

            foreach (string expected in expectedPaths)
            {
                var matches = files.Where(x => NormalizeZipPath(x.FullName)
                    .EndsWith("/" + expected, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length == 1) return matches[0];
                if (matches.Length > 1)
                    throw new InvalidDataException("Ambiguous archive layout. More than one entry matches: " +
                        expected + "\n" + string.Join("\n", matches.Select(x => x.FullName)));
            }

            return null;
        }

        private static string NormalizeZipPath(string value)
        {
            return value.Replace('\\', '/').TrimStart('/');
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
                // UnityEngine.Object uses special null semantics: a missing/destroyed
                // component may compare equal to null while ?? still sees a managed wrapper.
                // Do not use GetComponent<Animator>() ?? AddComponent<Animator>() here.
                var animator = visual.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = visual.AddComponent<Animator>();
                    Debug.Log("Ghost Camaro: added missing Animator to imported Visual root.");
                }
                if (animator == null)
                    throw new InvalidOperationException(
                        "Ghost Camaro: failed to add an Animator to the imported Visual root. " +
                        "Check the imported FBX prefab and Console for an earlier error.");

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
