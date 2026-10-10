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
            GameObject plinth = Primitive(PrimitiveType.Cube, "Plinth", root.transform, new Vector3(0.45f, 0.45f, 0.9f), new Vector3(0.45f, 0.9f, 1.15f), stone);
            plinth.isStatic = true;

            BuildSword(root.transform, new Vector3(0.45f, 0.9f, 0.9f));
            BuildDummy(root.transform, new Vector3(-0.3f, 0f, 1.9f), wood, straw, steel);
            AddImpactSound(plinth, "sfx_sword_hit_stone");
            BuildGallery(root.transform);
            BuildInstructions(root.transform);
            new GameObject("Session recorder", typeof(Godtree.Diagnostics.SessionRecorder)).transform.SetParent(root.transform, false);
            new GameObject("Held object collision filter", typeof(HeldObjectCollisionFilter)).transform.SetParent(root.transform, false);
            BuildAbilities(root.transform);
            new GameObject("Distance grab", typeof(Godtree.Sandbox.DistanceGrab)).transform.SetParent(root.transform, false);
            new GameObject("Player stats", typeof(Godtree.Sandbox.PlayerStats)).transform.SetParent(root.transform, false);
            new GameObject("Damage feedback", typeof(Godtree.Sandbox.DamageFeedback)).transform.SetParent(root.transform, false);
            new GameObject("Character menu", typeof(Godtree.Sandbox.CharacterMenu)).transform.SetParent(root.transform, false);
            BuildAmbience(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// The training sword: the generated model, lying on the plinth, grabbable anywhere on
        /// its surface. It keeps whatever angle your hand had when you took it, which is what
        /// tracked well in the first recorded session (the earlier fixed-pose grip did not).
        /// </summary>
        private static void BuildSword(Transform parent, Vector3 plinthTop)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir + "/Weapons/wpn_sword_training.glb");
            if (prefab == null)
            {
                Debug.LogError("[Godtree] wpn_sword_training.glb is missing; the sandbox has no sword");
                return;
            }
            var sword = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            sword.name = "wpn_sword_training";
            LayFlat(sword, alongX: false);
            RestOn(sword, plinthTop);
            MakeWeapon(sword, 0.5f);
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

            // Weapon tables on the left: two for one-handed weapons, a long one behind for polearms and the like.
            const float tableTop = 0.85f;
            float[] tableZ = { 1.1f, 2.5f, 3.9f };
            for (int i = 0; i < tableZ.Length; i++)
            {
                GameObject table = Primitive(PrimitiveType.Cube, "Weapon table " + (i + 1), gallery.transform, new Vector3(-2.3f, tableTop / 2f, tableZ[i]), new Vector3(2.9f, tableTop, i == 2 ? 1.3f : 1.0f), wood);
                table.isStatic = true;
                AddImpactSound(table, "sfx_sword_hit_wood_heavy");
            }

            int shortIndex = 0, longIndex = 0;
            foreach (GameObject prefab in LoadModels("Weapons"))
            {
                if (prefab.name == "wpn_sword_training")
                    continue; // the sword on the plinth is this model
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                Vector3 size = WorldBounds(instance).size;
                bool isLong = Mathf.Max(size.x, size.y, size.z) > 1.15f;
                LayFlat(instance, alongX: isLong);
                Vector3 slot = isLong
                    ? new Vector3(-2.3f, tableTop, tableZ[2] - 0.5f + 0.24f * longIndex++)
                    : new Vector3(-3.5f + 0.47f * (shortIndex % 6), tableTop, tableZ[shortIndex / 6 % 2]);
                if (!isLong)
                    shortIndex++;
                RestOn(instance, slot);
                bool heavy = prefab.name.Contains("great") || prefab.name.Contains("halberd") || prefab.name.Contains("warhammer") || prefab.name.Contains("shield");
                MakeWeapon(instance, heavy ? 0.9f : 0.5f);
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

            // Scenery in a wide arc beyond the dummy: small pieces near, big ones far.
            var scenery = LoadModels("Environment");
            scenery.Sort((x, y) => WorldBoundsOfPrefab(x).CompareTo(WorldBoundsOfPrefab(y)));
            for (int i = 0; i < scenery.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(scenery[i], gallery.transform);
                float angle = Mathf.Lerp(-70f, 70f, scenery.Count == 1 ? 0.5f : i % 2 == 0 ? 0.5f + 0.5f * i / scenery.Count : 0.5f - 0.5f * i / scenery.Count);
                float radius = 9f + 1.1f * i;
                Vector3 spot = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0f, 3f + Mathf.Cos(angle * Mathf.Deg2Rad) * radius);
                instance.transform.localRotation = Quaternion.Euler(0f, 180f + angle, 0f);
                RestOn(instance, spot);
                MakeSolid(instance);
            }

            // The people of Huskhaven, standing about as statues until they can be rigged.
            var people = LoadModels("Characters");
            for (int i = 0; i < people.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(people[i], gallery.transform);
                float angle = Mathf.Lerp(25f, 75f, people.Count == 1 ? 0.5f : (float)i / (people.Count - 1));
                Vector3 spot = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 5.2f, 0f, 1f + Mathf.Cos(angle * Mathf.Deg2Rad) * 5.2f);
                instance.transform.localRotation = Quaternion.LookRotation(new Vector3(-spot.x, 0f, 1f - spot.z));
                RestOn(instance, spot);
                MakeSolid(instance);
            }

            // Creatures on the far left, facing the player.
            var creatures = LoadModels("Enemies");
            for (int i = 0; i < creatures.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(creatures[i], gallery.transform);
                Vector3 spot = new Vector3(-7f - 2.4f * (i % 4), 0f, 5.5f + 3.5f * (i / 4));
                instance.transform.localRotation = Quaternion.LookRotation(new Vector3(-spot.x, 0f, -spot.z));
                RestOn(instance, spot);
                MakeSolid(instance);
            }
        }

        private static float WorldBoundsOfPrefab(GameObject prefab)
        {
            float largest = 0f;
            foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null)
                    largest = Mathf.Max(largest, filter.sharedMesh.bounds.size.magnitude);
            return largest;
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

        /// <summary>
        /// A weapon: physical like any grabbable, but grabbed into a proper hold (see GripPose)
        /// wherever it is touched or pulled from.
        /// </summary>
        private static void MakeWeapon(GameObject go, float mass)
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
            }

            GripPose pose = go.AddComponent<GripPose>();
            ConfigureGrip(go, pose);
            SnappedGrab grab = go.AddComponent<SnappedGrab>();
            grab.EnableGrabs = new Grab[0];
            grab.DisableGrabs = new Grab[0];
        }

        /// <summary>Works out where the handle is and which way the weapon points, from its shape.</summary>
        private static void ConfigureGrip(GameObject go, GripPose pose)
        {
            var points = new System.Collections.Generic.List<Vector3>();
            foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;
                foreach (Vector3 v in filter.sharedMesh.vertices)
                    points.Add(go.transform.InverseTransformPoint(filter.transform.TransformPoint(v)));
            }
            if (points.Count == 0)
                return;

            Bounds b = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points)
                b.Encapsulate(p);
            Vector3 size = b.size;
            int longAxis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
            int thinAxis = size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
            if (thinAxis == longAxis)
                thinAxis = (longAxis + 1) % 3;
            int midAxis = 3 - longAxis - thinAxis;
            Vector3 Unit(int axis) => axis == 0 ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);

            string name = go.name;
            if (name.Contains("shield"))
            {
                pose.style = HoldStyle.Shield;
                pose.localMainAxis = Unit(thinAxis);
                pose.localSideAxis = Unit(longAxis);
                pose.localGripPoint = b.center - Unit(thinAxis) * (size[thinAxis] * 0.5f);
                return;
            }
            if (name.Contains("bow"))
            {
                pose.style = HoldStyle.Bow;
                pose.localMainAxis = Unit(longAxis);
                pose.localSideAxis = Unit(midAxis);
                pose.localGripPoint = b.center;
                return;
            }

            // Blade-like: find which end is the bulky one (guard and pommel on a sword, the head on an axe or spear).
            float low = b.min[longAxis], length = size[longAxis];
            float widestLow = 0f, widestHigh = 0f;
            foreach (Vector3 p in points)
            {
                Vector3 off = p - b.center;
                off[longAxis] = 0f;
                float along = (p[longAxis] - low) / Mathf.Max(length, 1e-5f);
                if (along < 0.3f) widestLow = Mathf.Max(widestLow, off.magnitude);
                else if (along > 0.7f) widestHigh = Mathf.Max(widestHigh, off.magnitude);
            }
            bool bulkyEndIsLow = widestLow > widestHigh;
            bool handleAtBulkyEnd = name.Contains("sword"); // a sword's guard and pommel are its bulk; on a dagger, axe or spear the business end is
            // Every generated weapon is drawn standing upright with its handle at the bottom, so for an
            // upright model the handle is simply the low end. The bulk test is only for models that lie down.
            bool handleIsLow = longAxis == 1 ? true : (handleAtBulkyEnd ? bulkyEndIsLow : !bulkyEndIsLow);
            bool pole = name.Contains("spear") || name.Contains("halberd") || name.Contains("staff");
            float fromHandleEnd = pole ? 0.40f : (name.Contains("dagger") ? 0.18f : (name.Contains("great") ? 0.10f : 0.13f));

            pose.style = HoldStyle.Blade;
            pose.localMainAxis = Unit(longAxis) * (handleIsLow ? 1f : -1f);
            pose.localSideAxis = Unit(midAxis);
            Vector3 grip = b.center;
            grip[longAxis] = handleIsLow ? low + fromHandleEnd * length : low + (1f - fromHandleEnd) * length;
            pose.localGripPoint = grip;
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
            Vector3 position = new Vector3(1.6f, 1.45f, 2.3f);
            board.transform.position = position;
            board.transform.rotation = Quaternion.LookRotation(position - new Vector3(0f, 1.45f, 0f));
            board.transform.localScale = Vector3.one * 0.001f;

            var canvas = board.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = board.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1300f, 2050f);

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
            text.fontSize = 38;
            text.lineSpacing = 1.1f;
            text.color = new Color(1f, 0.97f, 0.92f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.text =
                "<size=70><b>HOW TO PLAY</b></size>\n\n" +
                "<b><color=#FFD86B>PICK UP</color></b>   POINT at a thing (a gold dot appears over\nit) and squeeze <b>GRIP</b> (side button, middle finger).\nIt flies to your hand. Let go of grip to drop it.\nYou can also just touch it and squeeze grip.\n\n" +
                "<b><color=#FFD86B>WEAPONS</color></b>   Sword on the stone block, sixteen more\non the tables to your left. They snap into a proper hold.\n\n" +
                "<b><color=#FFD86B>FIGHT</color></b>   Swing at the straw dummy. Harder swings\nflash brighter and sound louder.\n\n" +
                "<b><color=#FFD86B>BOW</color></b>   Hold the bow in one hand. Bring your other\nhand to it, hold that hand's TRIGGER, pull back,\nlet go. Pull further to shoot faster.\n\n" +
                "<b><color=#FFD86B>MOTE BOLT</color></b>   Empty hand: hold TRIGGER to gather\nlight, let go to loose it. Throw it or point it.\n" +
                "<b><color=#FFD86B>PUSH KNOT</color></b>   Both hands empty and together:\nhold both TRIGGERS, then let go.\n" +
                "<b><color=#FFD86B>LIFT KNOT</color></b>   Both TRIGGERS, hands apart: loose\nthings in front of you float until you let go.\n" +
                "<b><color=#FFD86B>WARD KNOT</color></b>   Empty hand: hold A or X for a shield of light.\n\n" +
                "<b><color=#FFD86B>CHARACTER</color></b>   Press B (right hand) for your stats,\nequipment and a mirror. Push the arrows with a hand.\n\n" +
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

        /// <summary>The bow and spell prototypes, with their sounds.</summary>
        private static void BuildAbilities(Transform parent)
        {
            var go = new GameObject("Sandbox abilities (bow and spell prototypes)");
            go.transform.SetParent(parent, false);
            var abilities = go.AddComponent<Godtree.Sandbox.SandboxAbilities>();
            abilities.castClips = LoadClips("sfx_mote_chime");
            abilities.boltClips = LoadClips("sfx_knot_cast");
            abilities.pushClips = LoadClips("sfx_knot_cast");
            abilities.bowReleaseClips = LoadClips("sfx_bow_release");
            abilities.arrowHitClips = LoadClips("sfx_arrow_hit_wood");
            abilities.boltHitClips = LoadClips("sfx_mote_chime");
        }

        private static AudioClip[] LoadClips(string soundId)
        {
            var clips = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 9; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Sfx/{soundId}_{i:00}.ogg");
                if (clip != null)
                    clips.Add(clip);
            }
            return clips.ToArray();
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
