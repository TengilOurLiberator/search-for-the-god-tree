using System.IO;
using BIMOS;
using Godtree.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace Godtree.EditorTools
{
    /// <summary>
    /// Builds the milestone 2 VR sandbox from nothing: project settings (URP, OpenXR,
    /// physics rate, rig layer) and a scene with the BIMOS rig, a placeholder sword and
    /// a spring-jointed training dummy. Re-runnable; it overwrites what it made.
    ///
    /// Menu: Godtree > Build Sandbox.
    /// Batch: Unity -batchmode -quit -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.Build
    ///
    /// Everything here is placeholder geometry made of Unity primitives, so the sandbox
    /// depends on no generated asset.
    /// </summary>
    public static class SandboxBuilder
    {
        private const string Root = "Assets/Godtree";
        private const string SettingsDir = Root + "/Settings";
        private const string SandboxDir = Root + "/Sandbox";
        private const string ScenePath = Root + "/Scenes/Sandbox.unity";
        private const string BimosScene = "Packages/com.kadenzombie8.bimos/Assets/Scenes/BIMOSStandard.unity";
        private const string BimosDefaultPose = "Packages/com.kadenzombie8.bimos/Assets/HandPoses/Default.asset";
        private const string RigLayer = "BIMOSRig";
        private const string ArtDir = Root + "/Art";
        private const string AudioDir = Root + "/Audio";

        [MenuItem("Godtree/Build Sandbox")]
        public static void Build()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(SandboxDir + "/Materials");
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();

            EnsureLayer(RigLayer);
            EnsurePhysicsRate();
            EnsureUrp();
            EnsureOpenXr();
            BuildScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[Godtree] Sandbox built: " + ScenePath);
        }

        /// <summary>
        /// Renders the sandbox scene from two fixed viewpoints to docs/images, so the scene can
        /// be reviewed without opening the editor.
        /// Batch: Unity -batchmode -quit -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.Screenshots
        /// </summary>
        /// <summary>
        /// Opens the sandbox scene and presses Play. Menu: Godtree > Play Sandbox.
        /// Launch: Unity -projectPath unity -executeMethod Godtree.EditorTools.SandboxBuilder.OpenAndPlay
        /// </summary>
        [MenuItem("Godtree/Play Sandbox")]
        public static void OpenAndPlay()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }

        [MenuItem("Godtree/Render Sandbox Screenshots")]
        public static void Screenshots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/images"));
            Directory.CreateDirectory(outDir);
            Shot(new Vector3(1.6f, 1.7f, -1.4f), new Vector3(0f, 1.0f, 1.8f), Path.Combine(outDir, "sandbox_player_view.png"));
            Shot(new Vector3(0f, 7f, -4f), new Vector3(0f, 0.5f, 7f), Path.Combine(outDir, "sandbox_gallery_view.png"));
            Shot(new Vector3(0.9f, 1.25f, 0.45f), new Vector3(0.45f, 0.98f, 0.95f), Path.Combine(outDir, "sandbox_sword_closeup.png"));
        }

        private static void Shot(Vector3 from, Vector3 lookAt, string path)
        {
            var go = new GameObject("ScreenshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log("[Godtree] Screenshot: " + path);
        }

        // ------------------------------------------------------------ settings

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
                return;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 6; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue))
                    continue;
                slot.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[Godtree] Added layer '{layerName}' at index {i}");
                return;
            }
            Debug.LogError("[Godtree] No free layer slot for " + layerName);
        }

        private static void EnsurePhysicsRate()
        {
            // BIMOS 1.0.0 is tuned for a 144 Hz physics step (see its README, "Limitations").
            Time.fixedDeltaTime = 1f / 144f;
        }

        private static void EnsureUrp()
        {
            string rendererPath = SettingsDir + "/Godtree_Renderer.asset";
            string pipelinePath = SettingsDir + "/Godtree_URP.asset";

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                ResourceReloader.ReloadAllNullIn(rendererData, UniversalRenderPipelineAsset.packagePath);

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = false;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
        }

        private static void EnsureOpenXr()
        {
            const BuildTargetGroup group = BuildTargetGroup.Standalone;

            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(group))
                perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(group);
            if (!XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group))
                Debug.LogWarning("[Godtree] Could not assign the OpenXR loader; enable it by hand in Project Settings > XR Plug-in Management.");

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
            if (general != null)
                general.InitManagerOnStart = true;

            // Controller profiles for the common PC headsets. No vendor SDK is involved:
            // these are OpenXR interaction profiles shipped with Unity's OpenXR plugin.
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
            OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openXr == null)
            {
                Debug.LogWarning("[Godtree] OpenXR settings not created yet; open Project Settings > XR Plug-in Management > OpenXR once, then run Build Sandbox again.");
                return;
            }

            string[] wanted =
            {
                "OculusTouchControllerProfile", "MetaQuestTouchProControllerProfile", "MetaQuestTouchPlusControllerProfile",
                "ValveIndexControllerProfile", "HTCViveControllerProfile", "MicrosoftMotionControllerProfile",
                "HPReverbG2ControllerProfile", "KHRSimpleControllerProfile"
            };
            int enabledProfiles = 0;
            foreach (OpenXRInteractionFeature feature in openXr.GetFeatures<OpenXRInteractionFeature>())
            {
                if (System.Array.IndexOf(wanted, feature.GetType().Name) >= 0)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                    enabledProfiles++;
                }
            }
            Debug.Log($"[Godtree] OpenXR controller profiles enabled: {enabledProfiles}");
            EditorUtility.SetDirty(openXr);
        }

        // ------------------------------------------------------------ scene

        private static void BuildScene()
        {
            // Start from the scene BIMOS ships, which has the rig and spawn point wired correctly.
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(BimosScene, ScenePath))
            {
                Debug.LogError("[Godtree] Could not copy " + BimosScene);
                return;
            }
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Material ground = MakeMaterial("Sandbox_Ground", new Color(0.56f, 0.82f, 0.31f));
            Material wood = MakeMaterial("Sandbox_Wood", new Color(0.62f, 0.45f, 0.28f));
            Material straw = MakeMaterial("Sandbox_Straw", new Color(0.91f, 0.80f, 0.45f));
            Material steel = MakeMaterial("Sandbox_Steel", new Color(0.73f, 0.76f, 0.81f), 0.6f);
            Material leather = MakeMaterial("Sandbox_Leather", new Color(0.23f, 0.23f, 0.43f));
            Material stone = MakeMaterial("Sandbox_Stone", new Color(0.73f, 0.76f, 0.81f));

            var root = new GameObject("---GODTREE SANDBOX---");

            // A wide meadow-green ground just under the base scene's small floor, so nothing can
            // roll or be thrown off the edge of the world.
            GameObject floor = Primitive(PrimitiveType.Cube, "Ground", root.transform, new Vector3(0f, -0.52f, 0f), new Vector3(80f, 1f, 80f), ground);
            floor.isStatic = true;

            // A stone plinth holding the sword at hand height, one step in front of the spawn.
            GameObject plinth = Primitive(PrimitiveType.Cube, "Plinth", root.transform, new Vector3(0.45f, 0.45f, 0.9f), new Vector3(0.5f, 0.9f, 0.5f), stone);
            plinth.isStatic = true;

            BuildSword(root.transform, new Vector3(0.45f, 0.96f, 0.9f), steel, leather);
            BuildDummy(root.transform, new Vector3(-0.3f, 0f, 1.9f), wood, straw, steel);
            AddImpactSound(plinth, "sfx_sword_hit_stone");
            BuildGallery(root.transform);
            BuildInstructions(root.transform);
            BuildAmbience(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// Placeholder training sword: about 0.95 m long, 1.1 kg, blade along local +Y,
        /// held by a BIMOS line grab on the grip. Mass sits low so it swings like a sword.
        /// </summary>
        private static void BuildSword(Transform parent, Vector3 position, Material steel, Material leather)
        {
            var sword = new GameObject("wpn_sword_training (placeholder)");
            sword.transform.SetParent(parent, false);
            // Lying flat on the plinth, blade pointing away from the player.
            sword.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));

            var body = sword.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 50f;
            body.centerOfMass = new Vector3(0f, 0.12f, 0f);

            Primitive(PrimitiveType.Cube, "Blade", sword.transform, new Vector3(0f, 0.47f, 0f), new Vector3(0.05f, 0.72f, 0.012f), steel);
            Primitive(PrimitiveType.Cube, "Guard", sword.transform, new Vector3(0f, 0.10f, 0f), new Vector3(0.20f, 0.025f, 0.03f), steel);
            Primitive(PrimitiveType.Cylinder, "Grip", sword.transform, new Vector3(0f, 0f, 0f), new Vector3(0.032f, 0.09f, 0.032f), leather);
            Primitive(PrimitiveType.Sphere, "Pommel", sword.transform, new Vector3(0f, -0.105f, 0f), new Vector3(0.05f, 0.05f, 0.05f), steel);

            // Show the generated model if it has been imported; the primitives stay as colliders.
            var generated = AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir + "/Weapons/wpn_sword_training.glb");
            if (generated != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(generated, sword.transform);
                visual.name = "Visual (generated)";
                visual.transform.localPosition = new Vector3(0f, -0.13f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                foreach (MeshRenderer primitive in sword.GetComponentsInChildren<MeshRenderer>())
                    if (!primitive.transform.IsChildOf(visual.transform))
                        primitive.enabled = false;
            }

            var grabObject = new GameObject("Grab");
            grabObject.transform.SetParent(sword.transform, false);
            var start = new GameObject("Start");
            var end = new GameObject("End");
            start.transform.SetParent(grabObject.transform, false);
            end.transform.SetParent(grabObject.transform, false);
            start.transform.localPosition = new Vector3(0f, -0.07f, 0f);
            end.transform.localPosition = new Vector3(0f, 0.07f, 0f);

            var grab = grabObject.AddComponent<LineGrab>();
            grab.Start = start.transform;
            grab.End = end.transform;
            grab.HandPose = AssetDatabase.LoadAssetAtPath<HandPose>(BimosDefaultPose);
            grab.EnableGrabs = new Grab[0];
            grab.DisableGrabs = new Grab[0];

            // The grab line runs through the middle of the grip collider. BIMOS refuses grab
            // points that are inside something solid unless hand clipping is allowed.
            var serializedGrab = new SerializedObject(grab);
            serializedGrab.FindProperty("_allowHandClipping").boolValue = true;
            serializedGrab.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Training dummy, first in-house physically simulated target: a fixed post with a
        /// torso, head and two arms hung on spring-driven joints, so every part reacts to
        /// the real force of a hit and then recovers its pose.
        /// </summary>
        private static void BuildDummy(Transform parent, Vector3 position, Material wood, Material straw, Material steel)
        {
            var dummy = new GameObject("enm_dummy_training (placeholder)");
            dummy.transform.SetParent(parent, false);
            dummy.transform.position = position;

            GameObject baseA = Primitive(PrimitiveType.Cube, "BaseA", dummy.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.08f, 0.12f), wood);
            GameObject baseB = Primitive(PrimitiveType.Cube, "BaseB", dummy.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.08f, 0.9f), wood);
            baseA.isStatic = true;
            baseB.isStatic = true;

            GameObject post = Primitive(PrimitiveType.Cylinder, "Post", dummy.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.45f, 0.1f), wood);
            Rigidbody postBody = post.AddComponent<Rigidbody>();
            postBody.isKinematic = true;
            AddImpactSound(post, "sfx_sword_hit_wood_heavy");

            GameObject torso = Primitive(PrimitiveType.Cylinder, "Torso", dummy.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.36f, 0.28f, 0.30f), straw);
            Rigidbody torsoBody = Body(torso, 14f);
            Spring(torsoBody, postBody, new Vector3(0f, -1f, 0f), 900f, 35f);
            torso.AddComponent<ImpactFlash>();
            AddImpactSound(torso, "sfx_sword_hit_straw");

            GameObject head = Primitive(PrimitiveType.Sphere, "Head", dummy.transform, new Vector3(0f, 1.60f, 0f), new Vector3(0.26f, 0.28f, 0.26f), straw);
            Rigidbody headBody = Body(head, 4f);
            Spring(headBody, torsoBody, new Vector3(0f, -0.5f, 0f), 220f, 10f);
            head.AddComponent<ImpactFlash>();
            AddImpactSound(head, "sfx_sword_hit_metal");

            GameObject helmet = Primitive(PrimitiveType.Sphere, "Helmet", head.transform, new Vector3(0f, 0.28f, 0f), new Vector3(1.15f, 0.7f, 1.15f), steel);
            Object.DestroyImmediate(helmet.GetComponent<Collider>());

            for (int side = -1; side <= 1; side += 2)
            {
                string label = side < 0 ? "ArmL" : "ArmR";
                GameObject arm = Primitive(PrimitiveType.Cylinder, label, dummy.transform, new Vector3(side * 0.36f, 1.28f, 0f), new Vector3(0.07f, 0.18f, 0.07f), wood);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Rigidbody armBody = Body(arm, 2f);
                // Anchor at the shoulder end of the arm (cylinder local Y runs along its length).
                Spring(armBody, torsoBody, new Vector3(0f, side * 1f, 0f), 120f, 6f);
                arm.AddComponent<ImpactFlash>();
                AddImpactSound(arm, "sfx_sword_hit_wood_light");
            }
        }

        /// <summary>
        /// Lays the scene out around the player:
        ///  - a weapon table on the left with every generated weapon, all of them grabbable;
        ///  - loose small props on the right to pick up and throw;
        ///  - large props, buildings and enemies as solid scenery further back.
        /// </summary>
        private static void BuildGallery(Transform parent)
        {
            var gallery = new GameObject("Generated models");
            gallery.transform.SetParent(parent, false);

            Material wood = MakeMaterial("Sandbox_Wood", new Color(0.62f, 0.45f, 0.28f));

            // Weapon table.
            const float tableTop = 0.85f;
            GameObject table = Primitive(PrimitiveType.Cube, "Weapon table", gallery.transform, new Vector3(-1.5f, tableTop / 2f, 1.1f), new Vector3(2.2f, tableTop, 0.9f), wood);
            table.isStatic = true;
            AddImpactSound(table, "sfx_sword_hit_wood_heavy");

            float slotX = -2.35f;
            foreach (GameObject prefab in LoadModels("Weapons"))
            {
                if (prefab.name == "wpn_sword_training")
                    continue; // the sword on the plinth is this model
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                bool isSpear = prefab.name.Contains("spear");
                LayFlat(instance, alongX: isSpear);
                Vector3 slot = isSpear ? new Vector3(-1.5f, tableTop, 1.42f) : new Vector3(slotX, tableTop, 1.0f);
                RestOn(instance, slot);
                MakeGrabbable(instance, prefab.name.Contains("shield") ? 2.5f : 1.0f);
                if (!isSpear)
                    slotX += 0.42f;
            }

            // Loose props to pick up, and scenery.
            string[] loose = { "prp_crate", "prp_chest", "prp_barrel", "prp_mote_vial", "prp_lantern_husk", "prp_bench" };
            int looseIndex = 0;
            float sceneryX = -9f;
            foreach (GameObject prefab in LoadModels("Props"))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                if (System.Array.IndexOf(loose, prefab.name) >= 0)
                {
                    RestOn(instance, new Vector3(1.5f + (looseIndex % 3) * 0.85f, 0f, 0.4f + (looseIndex / 3) * 0.9f));
                    looseIndex++;
                    MakeGrabbable(instance, prefab.name.Contains("vial") ? 0.2f : (prefab.name.Contains("lantern") ? 0.6f : 6f));
                }
                else
                {
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    RestOn(instance, new Vector3(sceneryX, 0f, 6.5f));
                    MakeSolid(instance);
                    sceneryX += 3.6f;
                }
            }

            float envX = -6f;
            foreach (GameObject prefab in LoadModels("Environment"))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                RestOn(instance, new Vector3(envX, 0f, 12f));
                MakeSolid(instance);
                envX += 8f;
            }

            float enemyX = 4f;
            foreach (GameObject prefab in LoadModels("Enemies"))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                instance.transform.localRotation = Quaternion.Euler(0f, 200f, 0f);
                RestOn(instance, new Vector3(enemyX, 0f, 9f));
                MakeSolid(instance);
                enemyX += 3.5f;
            }
        }

        private static System.Collections.Generic.List<GameObject> LoadModels(string folder)
        {
            var result = new System.Collections.Generic.List<GameObject>();
            string dir = ArtDir + "/" + folder;
            if (!AssetDatabase.IsValidFolder(dir))
                return result;
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { dir });
            System.Array.Sort(guids, (a, b) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));
            foreach (string guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null)
                    result.Add(prefab);
            }
            return result;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        /// <summary>Rotates a model so its longest side lies horizontally (along Z, or X if asked).</summary>
        private static void LayFlat(GameObject go, bool alongX)
        {
            go.transform.rotation = Quaternion.identity;
            Vector3 size = WorldBounds(go).size;
            Quaternion rotation = Quaternion.identity;
            if (size.y >= size.x && size.y >= size.z)
                rotation = Quaternion.Euler(90f, 0f, 0f);          // upright: tip it forward
            else if (size.x >= size.z)
                rotation = Quaternion.Euler(0f, 90f, 0f);          // lying along X: turn it along Z
            if (alongX)
                rotation = Quaternion.Euler(0f, 90f, 0f) * rotation;
            go.transform.rotation = rotation;
        }

        /// <summary>Moves a model so the centre of its base sits on the given point.</summary>
        private static void RestOn(GameObject go, Vector3 point)
        {
            Bounds bounds = WorldBounds(go);
            Vector3 baseCentre = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            go.transform.position += point - baseCentre + Vector3.up * 0.01f;
        }

        /// <summary>
        /// Makes a generated model a physical object the BIMOS hands can pick up anywhere on
        /// its surface: a rigidbody, a convex collider per mesh, and an auto grab on each.
        /// </summary>
        private static void MakeGrabbable(GameObject go, float mass)
        {
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 50f;

            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;

                AutoGrab grab = filter.gameObject.AddComponent<AutoGrab>();
                grab.EnableGrabs = new Grab[0];
                grab.DisableGrabs = new Grab[0];
            }
        }

        /// <summary>Scenery you cannot walk or swing through.</summary>
        private static void MakeSolid(GameObject go)
        {
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
            go.isStatic = true;
        }

        /// <summary>A board beside the player that says how to play. World-space UI, legacy font, no extra packages.</summary>
        private static void BuildInstructions(Transform parent)
        {
            var board = new GameObject("How to play (board)");
            board.transform.SetParent(parent, false);
            Vector3 position = new Vector3(1.55f, 1.55f, 2.2f);
            board.transform.position = position;
            board.transform.rotation = Quaternion.LookRotation(position - new Vector3(0f, 1.45f, 0f));
            board.transform.localScale = Vector3.one * 0.001f;

            var canvas = board.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = board.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1300f, 1350f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(board.transform, false);
            var image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.13f, 0.13f, 0.23f, 0.94f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(board.transform, false);
            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.lineSpacing = 1.1f;
            text.color = new Color(1f, 0.97f, 0.92f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.text =
                "<size=70><b>HOW TO PLAY</b></size>\n\n" +
                "<b><color=#FFD86B>PICK UP</color></b>   Put your hand on a thing and HOLD the\n" +
                "<b>GRIP</b> button (side of the controller, under your\nmiddle finger). Let go to drop it. Not the trigger.\n\n" +
                "<b><color=#FFD86B>SWORD</color></b>   On the stone block. Grab the dark handle.\n" +
                "<b><color=#FFD86B>OTHER WEAPONS</color></b>   On the wooden table to your left.\nGrab them anywhere.\n\n" +
                "<b><color=#FFD86B>FIGHT</color></b>   Swing at the straw dummy. Harder swings\nflash brighter and sound louder. Nothing is scripted:\nit reacts to the real hit.\n\n" +
                "<b><color=#FFD86B>MOVE</color></b>   Left stick. Click it to run.\n" +
                "<b><color=#FFD86B>TURN</color></b>   Right stick left or right.\n" +
                "<b><color=#FFD86B>CROUCH / JUMP</color></b>   Right stick down / click it.\n" +
                "<b><color=#FFD86B>MENU</color></b>   Y button: comfort, teleport, turning.";
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(60f, 50f);
            textRect.offsetMax = new Vector2(-60f, -50f);

            // A post so the board is not floating.
            Material wood = MakeMaterial("Sandbox_Wood", new Color(0.62f, 0.45f, 0.28f));
            GameObject post = Primitive(PrimitiveType.Cube, "Board post", parent, new Vector3(position.x, 0.44f, position.z) + board.transform.forward * 0.03f, new Vector3(0.08f, 0.88f, 0.08f), wood);
            post.isStatic = true;
        }

        /// <summary>Quiet meadow wind so the space is not dead silent.</summary>
        private static void BuildAmbience(Transform parent)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Sfx/sfx_wind_meadow_01.ogg");
            if (clip == null)
                return;
            var ambience = new GameObject("Ambience (meadow wind)");
            ambience.transform.SetParent(parent, false);
            var source = ambience.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.25f;
            source.spatialBlend = 0f;
        }

        /// <summary>Gives an object the numbered variations of one generated impact sound.</summary>
        private static void AddImpactSound(GameObject target, string soundId)
        {
            var clips = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 9; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Sfx/{soundId}_{i:00}.ogg");
                if (clip != null)
                    clips.Add(clip);
            }
            if (clips.Count == 0)
                return;
            target.AddComponent<ImpactSound>().Clips = clips.ToArray();
        }

        // ------------------------------------------------------------ helpers

        private static Rigidbody Body(GameObject go, float mass)
        {
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.angularDamping = 0.5f;
            return body;
        }

        /// <summary>Position locked, rotation free but pulled back to the rest pose by a spring.</summary>
        private static void Spring(Rigidbody body, Rigidbody connectedTo, Vector3 localAnchor, float spring, float damper)
        {
            ConfigurableJoint joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedTo;
            joint.anchor = localAnchor;
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive = new JointDrive { positionSpring = spring, positionDamper = damper, maximumForce = float.MaxValue };
            joint.enableCollision = false;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(string name, Color color, float smoothness = 0.1f)
        {
            string path = $"{SandboxDir}/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
