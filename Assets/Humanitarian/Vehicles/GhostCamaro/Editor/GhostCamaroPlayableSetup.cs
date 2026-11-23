#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanitarian.Vehicles;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Humanitarian.EditorTools
{
    /// <summary>
    /// One-command playable setup for Unity 6 / URP. Importing binary FBX is optional for the
    /// physics demo, but a real Ghost Camaro FBX is required for the original visual fidelity.
    /// No Minecraft assets are imported.
    /// </summary>
    public static class GhostCamaroPlayableSetup
    {
        private const string Base = "Assets/Humanitarian/Vehicles/GhostCamaro";
        private const string Fbx = Base + "/Models/GhostCamaro.fbx";
        private const string Prefab = Base + "/Prefabs/GhostCamaro_Driveable.prefab";
        private const string Scene = "Assets/Humanitarian/Scenes/GhostCamaro_DrivingDemo.unity";
        private const string MaterialFolder = Base + "/Materials";
        private const string Controller = Base + "/Animations/GhostCamaro_Preview.controller";
        private const string Atlas = Base + "/Textures/Ghost_Camaro_Textures.png";
        private const string GlassTex = Base + "/Textures/Glass_V5.png";

        [MenuItem("Humanitarian/Ghost Camaro/3. Build DRIVEABLE car + demo scene")]
        public static void BuildDriveable()
        {
            try
            {
                Ensure(Base + "/Prefabs");
                Ensure(MaterialFolder);
                Ensure("Assets/Humanitarian/Scenes");
                var prefab = BuildCarPrefab();
                BuildDemoScene(prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(Scene);
                Debug.Log("Humanitarian: DRIVABLE Ghost Camaro created in " + Scene);
                EditorUtility.DisplayDialog("Humanitarian — Ghost Camaro",
                    "Playable driving demo created!\n\nScene: " + Scene +
                    "\n\nPress Play. W/A/S/D drive, Space brake, F5 camera, E exit.\n" +
                    (File.Exists(Fbx) ? "Original FBX loaded." :
                        "WARNING: Original FBX missing; a temporary visible car was generated. Copy the provided Models/ and Textures/ to Assets and rebuild."),
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Humanitarian driving setup failed", exception.ToString(), "OK");
            }
        }

        private static GameObject BuildCarPrefab()
        {
            var root = new GameObject("GhostCamaro_Driveable");
            try
            {
                root.transform.position = Vector3.zero;
                Transform visual;
                GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
                if (imported != null)
                {
                    var display = (GameObject)PrefabUtility.InstantiatePrefab(imported);
                    display.name = "Visual";
                    display.transform.SetParent(root.transform, false);
                    visual = display.transform;
                    // The source GLB is Y-up, front is -X. Detect the front wheel direction
                    // from the actual imported FBX hierarchy, then align it with Unity +Z.
                    var tempRig = root.AddComponent<GhostCamaroRigReferences>();
                    tempRig.visualRoot = visual;
                    tempRig.AutoBind();
                    if (tempRig.wheelFL != null && tempRig.wheelFR != null &&
                        tempRig.wheelRL != null && tempRig.wheelRR != null)
                    {
                        Vector3 front = (tempRig.wheelFL.position + tempRig.wheelFR.position) * 0.5f;
                        Vector3 rear = (tempRig.wheelRL.position + tempRig.wheelRR.position) * 0.5f;
                        Vector3 direction = front - rear;
                        direction.y = 0f;
                        if (direction.sqrMagnitude > 0.01f)
                            visual.localRotation = Quaternion.FromToRotation(direction.normalized, Vector3.forward)
                                * visual.localRotation;
                    }
                    NormalizeModelToMeters(visual);
                    BindURPMaterials(visual);
                    var animator = visual.GetComponent<Animator>();
                    if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                    animator.applyRootMotion = false;
                    var controller = EnsurePreviewController();
                    if (controller != null) animator.runtimeAnimatorController = controller;
                }
                else
                {
                    visual = BuildTemporaryCar(root.transform);
                    Debug.LogWarning("Humanitarian: Original GhostCamaro.fbx is not in the repository. " +
                        "A procedural TEST car will be used until the real source asset is added. " +
                        "This is not a replacement for the supplied 40 MB original model.");
                }

                var rig = root.GetComponent<GhostCamaroRigReferences>();
                if (rig == null) rig = root.AddComponent<GhostCamaroRigReferences>();
                rig.visualRoot = visual;
                rig.AutoBind();
                var drive = root.AddComponent<GhostCamaroDrive>();
                drive.visualRoot = visual;
                drive.steeringWheel = rig.steeringWheel;

                // Select left/right by actual vehicle local X (the source asset's naming
                // convention can be mirrored after its coordinate-system conversion).
                var fronts = new List<Transform> { rig.wheelFL, rig.wheelFR };
                var rears = new List<Transform> { rig.wheelRL, rig.wheelRR };
                if (fronts.All(x => x != null)) fronts.Sort((a,b) =>
                    root.transform.InverseTransformPoint(a.position).x.CompareTo(root.transform.InverseTransformPoint(b.position).x));
                if (rears.All(x => x != null)) rears.Sort((a,b) =>
                    root.transform.InverseTransformPoint(a.position).x.CompareTo(root.transform.InverseTransformPoint(b.position).x));
                drive.wheelFL = fronts[0]; drive.wheelFR = fronts[1];
                drive.wheelRL = rears[0]; drive.wheelRR = rears[1];
                drive.colliderFL = AddWheel(root.transform, "WheelCollider_FL", drive.wheelFL, new Vector3(-0.8f, 0.35f, 1.45f));
                drive.colliderFR = AddWheel(root.transform, "WheelCollider_FR", drive.wheelFR, new Vector3(0.8f, 0.35f, 1.45f));
                drive.colliderRL = AddWheel(root.transform, "WheelCollider_RL", drive.wheelRL, new Vector3(-0.8f, 0.35f, -1.28f));
                drive.colliderRR = AddWheel(root.transform, "WheelCollider_RR", drive.wheelRR, new Vector3(0.8f, 0.35f, -1.28f));
                AddChassis(root);
                AddLights(root, drive);
                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 1690;
                rb.centerOfMass = new Vector3(0, 0.42f, 0);
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var preview = root.AddComponent<GhostCamaroAnimationPreview>();
                preview.animator = visual.GetComponent<Animator>();
                bool okay = PrefabUtility.SaveAsPrefabAsset(root, Prefab) != null;
                if (!okay) throw new IOException("PrefabUtility.SaveAsPrefabAsset returned null for " + Prefab);
                return AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void NormalizeModelToMeters(Transform visual)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds modelBounds = renderers[0].bounds;
            foreach (var renderer in renderers) modelBounds.Encapsulate(renderer.bounds);
            float depth = modelBounds.size.z;
            if (depth > 0.001f)
            {
                float factor = 5.18f / depth; // Original Ghost Camaro bounding length from source GLB.
                if (factor > 0.01f && factor < 100f)
                    visual.localScale *= factor;
            }
            modelBounds = renderers[0].bounds;
            foreach (var renderer in renderers) modelBounds.Encapsulate(renderer.bounds);
            // Align the model with the root: ground = Y0, body centered in X/Z.
            visual.position -= new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
        }

        private static AnimatorController EnsurePreviewController()
        {
            var modelImporter = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (modelImporter != null &&
                (!modelImporter.importAnimation || modelImporter.animationType != ModelImporterAnimationType.Generic))
            {
                modelImporter.importAnimation = true;
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.importBlendShapes = true;
                modelImporter.SaveAndReimport();
            }
            Ensure(Base + "/Animations");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(c => c.name).ToArray();
            var machine = controller.layers[0].stateMachine;
            foreach (var clip in clips)
            {
                string clipName = clip.name;
                int separator = clipName.LastIndexOf('|');
                if (separator >= 0) clipName = clipName.Substring(separator + 1);
                if (machine.states.Any(state => state.state.name == clipName)) continue;
                var state = machine.AddState(clipName);
                state.motion = clip;
                state.writeDefaultValues = false;
            }
            Debug.Log("Ghost Camaro: imported " + clips.Length +
                " FBX animation clips (source GLB contains 62; check FBX actual count).");
            return controller;
        }

        private static WheelCollider AddWheel(Transform parent, string name, Transform wheelMesh, Vector3 fallback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Vector3 position = wheelMesh != null ? parent.InverseTransformPoint(wheelMesh.position) : fallback;
            if (position.magnitude < 0.01f || Mathf.Abs(position.x) > 2f || Mathf.Abs(position.z) > 3f)
                position = fallback;
            go.transform.localPosition = position;
            WheelCollider w = go.AddComponent<WheelCollider>();
            w.radius = 0.35f;
            w.mass = 28f;
            w.wheelDampingRate = 1.0f;
            w.suspensionDistance = 0.16f;
            var spring = w.suspensionSpring;
            spring.spring = 40000f;
            spring.damper = 5200f;
            spring.targetPosition = 0.5f;
            w.suspensionSpring = spring;
            var forward = w.forwardFriction;
            forward.stiffness = 1.4f;
            w.forwardFriction = forward;
            var sideways = w.sidewaysFriction;
            sideways.stiffness = 1.15f;
            w.sidewaysFriction = sideways;
            return w;
        }

        private static void AddChassis(GameObject root)
        {
            var body = root.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.83f, -0.08f);
            body.size = new Vector3(1.79f, 0.68f, 4.52f);
            // These dimensions intentionally exclude the wheels and front plough.
        }

        private static void AddLights(GameObject root, GhostCamaroDrive drive)
        {
            drive.headlampLeft = LightAt(root.transform, "Headlamp_Left", new Vector3(-0.65f,0.72f,2.24f),
                Color.white, LightType.Spot, 21f, 40f, 4f);
            drive.headlampRight = LightAt(root.transform, "Headlamp_Right", new Vector3(0.65f,0.72f,2.24f),
                Color.white, LightType.Spot, 21f, 40f, 4f);
            drive.brakeLightLeft = LightAt(root.transform, "Brake_L", new Vector3(-0.68f,0.72f,-2.44f),
                Color.red, LightType.Point, 3f, 0f, 1f);
            drive.brakeLightRight = LightAt(root.transform, "Brake_R", new Vector3(0.68f,0.72f,-2.44f),
                Color.red, LightType.Point, 3f, 0f, 1f);
            drive.indicatorFL = LightAt(root.transform, "Indicator_FL", new Vector3(-0.82f,0.66f,2.3f),
                new Color(1f,.53f,.03f), LightType.Point, 1.6f,0f,.85f);
            drive.indicatorFR = LightAt(root.transform, "Indicator_FR", new Vector3(0.82f,0.66f,2.3f),
                new Color(1f,.53f,.03f), LightType.Point, 1.6f,0f,.85f);
            drive.indicatorRL = LightAt(root.transform, "Indicator_RL", new Vector3(-0.82f,0.67f,-2.35f),
                new Color(1f,.53f,.03f), LightType.Point, 1.6f,0f,.85f);
            drive.indicatorRR = LightAt(root.transform, "Indicator_RR", new Vector3(0.82f,0.67f,-2.35f),
                new Color(1f,.53f,.03f), LightType.Point, 1.6f,0f,.85f);
        }

        private static Light LightAt(Transform parent, string name, Vector3 pos, Color color,
            LightType type, float range, float angle, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            var l = go.AddComponent<Light>();
            l.type = type; l.color = color; l.range = range; l.intensity = intensity;
            if (type == LightType.Spot) l.spotAngle = angle;
            l.enabled = false;
            return l;
        }

        private static void BindURPMaterials(Transform visual)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader missing. Is URP installed?");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            var glassAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(GlassTex);
            var cache = new Dictionary<string, Material>();
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var old = renderer.sharedMaterials;
                var newMats = new Material[old.Length];
                for (int i=0;i<old.Length;i++)
                {
                    Material original = old[i];
                    string name = original != null ? original.name : "CarPaint";
                    string normalized = name.ToLowerInvariant();
                    bool glass = normalized.Contains("glass") || normalized.Contains("windshield") ||
                                 normalized.Contains("window") || renderer.name.ToLowerInvariant().Contains("glass");
                    string key = (glass ? "Glass_" : "Body_") + name.Replace('/', '_').Replace(':', '_');
                    if (!cache.TryGetValue(key, out var material))
                    {
                        string path = MaterialFolder + "/" + Sanitize(key) + ".mat";
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null)
                        {
                            material = new Material(shader) { name = key };
                            Texture texture = null;
                            if (original != null)
                            {
                                if (original.HasProperty("_BaseMap")) texture = original.GetTexture("_BaseMap");
                                if (texture == null && original.HasProperty("_MainTex"))
                                    texture = original.GetTexture("_MainTex");
                            }
                            if (texture == null) texture = glass ? glassAtlas : atlas;
                            if (texture != null) material.SetTexture("_BaseMap", texture);
                            material.SetColor("_BaseColor", glass ? new Color(0.28f,0.38f,0.44f,0.42f) : Color.white);
                            material.SetFloat("_Smoothness", glass ? 0.88f : 0.24f);
                            if (glass)
                            {
                                material.SetFloat("_Surface", 1f);
                                material.SetFloat("_Blend", 0f);
                                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                material.SetFloat("_ZWrite", 0f);
                                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                            }
                            AssetDatabase.CreateAsset(material, path);
                        }
                        cache[key] = material;
                    }
                    newMats[i] = material;
                }
                renderer.sharedMaterials = newMats;
            }
        }

        private static string Sanitize(string value)
        {
            return new string(value.Select(ch => char.IsLetterOrDigit(ch) || ch=='_' || ch=='-' ? ch : '_').ToArray());
        }

        private static Transform BuildTemporaryCar(Transform root)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            var black = NewMaterial("Temporary_MatteBlack", new Color(.055f,.058f,.063f));
            var glass = NewMaterial("Temporary_BlueGlass", new Color(.23f,.33f,.38f));
            var rubber = NewMaterial("Temporary_Rubber", new Color(.07f,.07f,.07f));
            var metal = NewMaterial("Temporary_Steel", new Color(.23f,.25f,.26f));
            Cube(visual, "Body", new Vector3(0,.72f,0), new Vector3(1.89f,.46f,4.5f),black);
            Cube(visual, "Cabin", new Vector3(0,1.10f,-.40f), new Vector3(1.60f,.38f,2.10f), glass);
            Cube(visual, "Hood", new Vector3(0,.99f,1.30f), new Vector3(1.8f,.07f,1.78f),black);
            Cube(visual, "Trunk", new Vector3(0,.97f,-1.61f), new Vector3(1.8f,.09f,1.05f),black);
            Cube(visual, "Armored_Plough", new Vector3(0,.37f,2.37f), new Vector3(2.1f,.24f,.12f),metal);
            Cube(visual, "Steering_Wheel", new Vector3(-.40f,1.0f,-.10f), new Vector3(.30f,.30f,.06f),metal);
            foreach (var spec in new[] { ("Wheel_FL",-.8f,1.45f), ("Wheel_FR",.8f,1.45f),
                                         ("Wheel_RL",-.8f,-1.28f), ("Wheel_RR",.8f,-1.28f) })
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = spec.Item1;
                go.transform.SetParent(visual,false);
                go.transform.localPosition = new Vector3(spec.Item2,.35f,spec.Item3);
                go.transform.localRotation = Quaternion.Euler(0,0,90);
                go.transform.localScale = new Vector3(.34f,.13f,.34f);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = rubber;
            }
            return visual;
        }

        private static Material NewMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 local, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent,false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void BuildDemoScene(GameObject vehiclePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            sun.transform.rotation = Quaternion.Euler(48f, -25f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground — driveable surface";
            ground.transform.localScale = new Vector3(40f,1f,40f);
            ground.GetComponent<Renderer>().sharedMaterial = NewMaterial("Test_Asphalt", new Color(.28f,.28f,.29f));
            for (int i=-25;i<=25;i++)
            {
                if (i%3 != 0) continue;
                var stripe = Cube(ground.transform, "Lane stripe " + i,
                    new Vector3(0f,.001f,i/40f), new Vector3(.0022f,.001f,.004f),
                    NewMaterial("Lane_White",new Color(.9f,.9f,.75f)));
            }
            var car = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab);
            car.name = "GhostCamaro — playable";
            car.transform.position = new Vector3(0f,0.05f,0f);
            var player = new GameObject("DemoPlayer — E to enter / exit");
            player.transform.position = new Vector3(-2.5f,1.0f,-3.5f);
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = .35f; capsule.center = new Vector3(0f,.9f,0f);
            var mainCamera = new GameObject("Main Camera");
            mainCamera.tag = "MainCamera";
            var camera = mainCamera.AddComponent<Camera>();
            mainCamera.AddComponent<AudioListener>();
            var driver = player.AddComponent<GhostCamaroDemoDriver>();
            driver.vehicle = car.GetComponent<GhostCamaroDrive>();
            driver.playerCamera = camera;
            driver.startInCar = true;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Scene))
                throw new IOException("Failed to save " + Scene);
        }

        private static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            for (int i=1;i<parts.Length;i++)
            {
                string parent = string.Join("/", parts.Take(i));
                string child = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }
}
#endif