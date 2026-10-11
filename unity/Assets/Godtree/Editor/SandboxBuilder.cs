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
            // The townsfolk can only talk if their two helper programs are running. Start them
            // alongside (a separate hidden process; if it fails the game still plays, in silence).
            try
            {
                string script = Path.GetFullPath(Path.Combine(Application.dataPath, "../../tools/voice/Start-NpcServices.ps1"));
                if (File.Exists(script))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{script}\"")
                    {
                        UseShellExecute = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    });
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[Godtree] Could not start the NPC services: " + error.Message);
            }
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }

        [MenuItem("Godtree/Render Sandbox Screenshots")]
        public static void Screenshots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/images"));
            Directory.CreateDirectory(outDir);
            // People and creatures stand in their idle pose, not the T-pose they are stored in.
            foreach (Animator animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                if (controller == null || controller.layers.Length == 0)
                    continue;
                string idle = animator.GetComponent<Godtree.Sandbox.EnemyActor>() != null ? animator.GetComponent<Godtree.Sandbox.EnemyActor>().idleClip : "Idle_A";
                foreach (UnityEditor.Animations.ChildAnimatorState state in controller.layers[0].stateMachine.states)
                    if (state.state.name == idle && state.state.motion is AnimationClip clip)
                        clip.SampleAnimation(animator.gameObject, 0.4f + 0.37f * (Mathf.Abs(animator.transform.position.x * 7f + animator.transform.position.z) % 3f));
            }
            // Run fires forward a little, or a still picture shows them unlit.
            foreach (ParticleSystem system in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if (system.transform.parent == null || system.transform.parent.GetComponent<ParticleSystem>() == null)
                    system.Simulate(2f, true, true);
            Shot(new Vector3(1.6f, 1.7f, -1.4f), new Vector3(0f, 1.0f, 1.8f), Path.Combine(outDir, "sandbox_player_view.png"));
            Shot(new Vector3(0f, 7f, -4f), new Vector3(0f, 0.5f, 7f), Path.Combine(outDir, "sandbox_gallery_view.png"));
            Shot(new Vector3(0.9f, 1.25f, 0.45f), new Vector3(0.45f, 0.98f, 0.95f), Path.Combine(outDir, "sandbox_sword_closeup.png"));
            Shot(new Vector3(0f, 1.65f, -3f), new Vector3(0f, 6f, 40f), Path.Combine(outDir, "sandbox_horizon_view.png"));
            Shot(new Vector3(0f, 1.65f, 6f), new Vector3(0f, 5f, 40f), Path.Combine(outDir, "sandbox_road_view.png"));
            Shot(new Vector3(4.4f, 1.65f, 0.4f), new Vector3(7.8f, 0.8f, 3.8f), Path.Combine(outDir, "sandbox_camp_view.png"));
            Shot(new Vector3(0f, 1.65f, 15f), new Vector3(0f, 2f, 27f), Path.Combine(outDir, "sandbox_gate_view.png"));
            Shot(new Vector3(3.3f, 1.5f, 15.4f), new Vector3(4.6f, 1.25f, 17.4f), Path.Combine(outDir, "sandbox_elder_view.png"));
            Shot(new Vector3(0f, 46f, 6f), new Vector3(0f, 0f, 9f), Path.Combine(outDir, "sandbox_town_above.png"));
            Shot(new Vector3(4f, 9f, -6f), new Vector3(-9f, 1.5f, 12f), Path.Combine(outDir, "sandbox_town_west.png"));
            Shot(new Vector3(-4f, 9f, -6f), new Vector3(10f, 1.5f, 10f), Path.Combine(outDir, "sandbox_town_east.png"));
            Shot(new Vector3(0f, 1.65f, 2f), new Vector3(0f, 2.2f, 24f), Path.Combine(outDir, "sandbox_town_square.png"));
            Shot(new Vector3(0f, 7f, 27f), new Vector3(0f, 1f, 45f), Path.Combine(outDir, "sandbox_meadow_view.png"));
        }

        private static void Shot(Vector3 from, Vector3 lookAt, string path)
        {
            var go = new GameObject("ScreenshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.transform.position = from;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = 60f;
            cam.farClipPlane = 2000f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
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
                pipeline.supportsHDR = true;
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

            Material ground = MakeMaterial("Sandbox_Ground", new Color(0.40f, 0.66f, 0.30f));
            Material wood = MakeMaterial("Sandbox_Wood", new Color(0.62f, 0.45f, 0.28f));
            Material straw = MakeMaterial("Sandbox_Straw", new Color(0.91f, 0.80f, 0.45f));
            Material steel = MakeMaterial("Sandbox_Steel", new Color(0.73f, 0.76f, 0.81f), 0.6f);
            Material leather = MakeMaterial("Sandbox_Leather", new Color(0.23f, 0.23f, 0.43f));
            Material stone = MakeMaterial("Sandbox_Stone", new Color(0.58f, 0.61f, 0.66f));

            var root = new GameObject("---GODTREE SANDBOX---");

            // A wide meadow-green ground just under the base scene's small floor, so nothing can
            // roll or be thrown off the edge of the world.
            GameObject floor = Primitive(PrimitiveType.Cube, "Ground", root.transform, new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 120f), ground);
            floor.isStatic = true;
            var meadow = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Environment/env_ground_meadow_albedo.png");
            if (meadow != null)
            {
                // The ground shader hides the texture's repeat and paints the road toward the arch.
                Shader groundShader = Shader.Find("Godtree/Ground");
                if (groundShader != null && groundShader.isSupported)
                    ground.shader = groundShader;
                ground.SetTexture("_BaseMap", meadow);
                ground.SetTextureScale("_BaseMap", new Vector2(32f, 32f));
                ground.SetColor("_BaseColor", new Color(0.92f, 0.98f, 0.90f));
                EditorUtility.SetDirty(ground);
            }
            // The base scene's own small grey floor is hidden; its collider stays, level with the meadow.
            GameObject baseEnvironment = GameObject.Find("---ENVIRONMENT---");
            if (baseEnvironment != null)
                foreach (MeshRenderer baseRenderer in baseEnvironment.GetComponentsInChildren<MeshRenderer>())
                    baseRenderer.enabled = false;
            // The base scene's floor is a loose object at the top level, exactly level with the meadow,
            // so the two flickered through each other in grey patches.
            foreach (GameObject top in scene.GetRootGameObjects())
                if (top.name == "Ground" && top.TryGetComponent(out MeshRenderer topRenderer))
                    topRenderer.enabled = false;

            // A stone plinth holding the sword at hand height, one step in front of the spawn.
            GameObject plinth = Primitive(PrimitiveType.Cube, "Plinth", root.transform, new Vector3(0.45f, 0.45f, 0.9f), new Vector3(0.45f, 0.9f, 1.15f), stone);
            plinth.isStatic = true;

            BuildSword(root.transform, new Vector3(0.45f, 0.9f, 0.9f));
            BuildDummy(root.transform, new Vector3(-0.3f, 0f, 1.9f), wood, straw, steel);
            AddImpactSound(plinth, "sfx_sword_hit_stone");
            Dress(plinth, ArtDir + "/Props/prp_plinth_stone.glb", root.transform);
            BuildGallery(root.transform);
            BuildInstructions(root.transform);
            new GameObject("Session recorder", typeof(Godtree.Diagnostics.SessionRecorder)).transform.SetParent(root.transform, false);
            new GameObject("Held object collision filter", typeof(HeldObjectCollisionFilter)).transform.SetParent(root.transform, false);
            BuildAbilities(root.transform);
            new GameObject("Distance grab", typeof(Godtree.Sandbox.DistanceGrab)).transform.SetParent(root.transform, false);
            new GameObject("Player stats", typeof(Godtree.Sandbox.PlayerStats)).transform.SetParent(root.transform, false);
            var feedback = new GameObject("Damage feedback").AddComponent<Godtree.Sandbox.DamageFeedback>();
            feedback.transform.SetParent(root.transform, false);
            feedback.sparkMaterial = AssetDatabase.LoadAssetAtPath<Material>(SandboxDir + "/Materials/Sandbox_Flame.mat");
            new GameObject("Character menu", typeof(Godtree.Sandbox.CharacterMenu)).transform.SetParent(root.transform, false);
            BuildAmbience(root.transform);
            new GameObject("Voice input (talk to NPCs)", typeof(Godtree.Sandbox.VoiceInput)).transform.SetParent(root.transform, false);
            BuildPlayerBody(root.transform);
            BuildPlayerEquipment(root.transform);

            ApplyToonToGeneratedModels(root);
            SliceDummy();
            BuildLook(root.transform);

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

        // The dummy's three drawn pieces and the two heights (world, at rest) where the model is cut.
        private static GameObject[] _dummyPieces;
        private static float _dummyLowCut, _dummyNeckCut;

        /// <summary>
        /// Training dummy, first in-house physically simulated target. It is the generated
        /// sack-and-helmet model, drawn three times with each copy showing one slice: the stand
        /// (fixed), the sack (hung on a spring from the post) and the head (hung on a spring from
        /// the sack). Every part reacts to the real force of a hit and then recovers its pose.
        /// The cut heights are found from the model's own outline: the post is its narrowest
        /// place low down and the neck its narrowest place high up.
        /// </summary>
        private static void BuildDummy(Transform parent, Vector3 position, Material wood, Material straw, Material steel)
        {
            var dummy = new GameObject("enm_dummy_training (placeholder)");
            dummy.transform.SetParent(parent, false);
            dummy.transform.position = position;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir + "/Props/prp_dummy_training.glb");
            if (prefab == null)
            {
                Debug.LogError("[Godtree] prp_dummy_training.glb is missing; the sandbox has no dummy");
                return;
            }

            const float height = 1.7f;
            string[] names = { "Dummy stand (model)", "Dummy sack (model)", "Dummy head (model)" };
            _dummyPieces = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dummy.transform);
                piece.name = names[i];
                piece.transform.localRotation = Quaternion.Euler(0f, DummyYaw, 0f);
                piece.transform.localScale *= height / WorldBounds(piece).size.y;
                RestOn(piece, position);
                _dummyPieces[i] = piece;
            }

            // Outline of the model: how wide it is at each height.
            var points = new System.Collections.Generic.List<Vector3>();
            // Points are spread across every triangle, not just its corners: the simplified mesh
            // has long triangles down the post with no corners in between.
            foreach (MeshFilter filter in _dummyPieces[0].GetComponentsInChildren<MeshFilter>())
            {
                Vector3[] vertices = filter.sharedMesh.vertices;
                int[] indices = filter.sharedMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 a = filter.transform.TransformPoint(vertices[indices[i]]);
                    Vector3 b = filter.transform.TransformPoint(vertices[indices[i + 1]]);
                    Vector3 c = filter.transform.TransformPoint(vertices[indices[i + 2]]);
                    float longest = Mathf.Max((a - b).magnitude, (b - c).magnitude, (c - a).magnitude);
                    int steps = Mathf.Clamp(Mathf.CeilToInt(longest / 0.025f), 1, 24);
                    for (int s = 0; s <= steps; s++)
                        for (int q = 0; q <= steps - s; q++)
                            points.Add(a + (b - a) * (s / (float)steps) + (c - a) * (q / (float)steps));
                }
            }
            const int bins = 48;
            float bottom = position.y;
            var width = new float[bins];
            for (int b = 0; b < bins; b++)
            {
                float from = bottom + height * b / bins, to = bottom + height * (b + 1) / bins;
                width[b] = SliceBounds(points, from, to, out Bounds slice) ? Mathf.Max(slice.size.x, slice.size.z) : (b > 0 ? width[b - 1] : 0f);
            }
            int postBin = Narrowest(width, bins * 8 / 100, bins * 40 / 100);
            int sackBin = postBin;
            while (sackBin < bins - 1 && width[sackBin] < width[postBin] * 1.7f)
                sackBin++;
            // The neck is the narrowest place between the middle of the sack and the helmet's brim,
            // the brim being the widest place near the top.
            int brimBin = bins * 70 / 100;
            for (int b = brimBin; b <= bins * 92 / 100; b++)
                if (width[b] > width[brimBin])
                    brimBin = b;
            int neckBin = Narrowest(width, bins * 55 / 100, brimBin);
            _dummyLowCut = bottom + height * sackBin / bins;
            _dummyNeckCut = bottom + height * (neckBin + 0.5f) / bins;
            SliceBounds(points, bottom, _dummyLowCut, out Bounds stand);
            SliceBounds(points, _dummyLowCut, _dummyNeckCut, out Bounds sack);
            SliceBounds(points, _dummyNeckCut, bottom + height + 1f, out Bounds helm);
            Debug.Log($"[Godtree] dummy cuts: post {width[postBin]:F2} m wide, sack from {_dummyLowCut:F2} m, neck at {_dummyNeckCut:F2} m; sack {sack.size}, head {helm.size}");

            // Stand: plain hidden shapes to collide with, never moving.
            GameObject foot = Primitive(PrimitiveType.Cube, "Base", dummy.transform, new Vector3(0f, 0.06f, 0f), new Vector3(stand.size.x * 0.8f, 0.12f, stand.size.z * 0.8f), wood);
            foot.isStatic = true;
            foot.GetComponent<MeshRenderer>().enabled = false;
            float postHeight = _dummyLowCut - bottom;
            GameObject post = Primitive(PrimitiveType.Cylinder, "Post", dummy.transform, new Vector3(0f, postHeight * 0.5f, 0f), new Vector3(width[postBin], postHeight * 0.5f, width[postBin]), wood);
            post.GetComponent<MeshRenderer>().enabled = false;
            Rigidbody postBody = post.AddComponent<Rigidbody>();
            postBody.isKinematic = true;
            AddImpactSound(post, "sfx_sword_hit_wood_heavy");

            // Sack: one round body hung from the top of the post.
            var torso = new GameObject("Torso");
            torso.transform.SetParent(dummy.transform, false);
            torso.transform.position = sack.center;
            Rigidbody torsoBody = Body(torso, 14f);
            // The narrower way across: the wider one includes the stub arms.
            float sackWidth = Mathf.Min(sack.size.x, sack.size.z) * 0.95f;
            GameObject sackShape = Primitive(PrimitiveType.Capsule, "Shape", torso.transform, Vector3.zero, new Vector3(sackWidth, Mathf.Max(sack.size.y, sackWidth) * 0.5f, sackWidth), straw);
            sackShape.GetComponent<MeshRenderer>().enabled = false;
            _dummyPieces[1].transform.SetParent(torso.transform, true);
            // Stiffer and better damped than the first dummy: this sack is taller and carries the
            // head high above the pivot, and with the old values it swayed for six seconds.
            Spring(torsoBody, postBody, new Vector3(0f, _dummyLowCut - sack.center.y, 0f), 2600f, 170f);
            torso.AddComponent<ImpactFlash>();
            AddImpactSound(torso, "sfx_sword_hit_straw");

            // Head and helmet: a ball hung from the neck.
            var head = new GameObject("Head");
            head.transform.SetParent(dummy.transform, false);
            head.transform.position = helm.center;
            Rigidbody headBody = Body(head, 2f);
            float headWidth = Mathf.Min((helm.size.x + helm.size.z) * 0.5f, helm.size.y) * 0.95f;
            GameObject headShape = Primitive(PrimitiveType.Sphere, "Shape", head.transform, Vector3.zero, Vector3.one * headWidth, steel);
            headShape.GetComponent<MeshRenderer>().enabled = false;
            _dummyPieces[2].transform.SetParent(head.transform, true);
            Spring(headBody, torsoBody, new Vector3(0f, _dummyNeckCut - helm.center.y, 0f), 320f, 16f);
            head.AddComponent<ImpactFlash>();
            AddImpactSound(head, "sfx_sword_hit_metal");
        }

        // How the generated town pieces must be turned so that a house front faces +Z, the wall runs
        // along X and the gate's passage runs along Z. Set by looking at a render from above.
        private const float HouseYaw = -90f, WallYaw = 0f, GateYaw = 0f;

        // Which way the dummy model is turned so its painted target faces the player.
        private const float DummyYaw = 180f;

        /// <summary>
        /// Gives each drawn piece of the dummy its own copy of the model's material, set to show
        /// only that piece's slice. Runs after the toon materials exist.
        /// </summary>
        private static void SliceDummy()
        {
            if (_dummyPieces == null)
                return;
            for (int i = 0; i < _dummyPieces.Length; i++)
            {
                int r = 0;
                foreach (Renderer renderer in _dummyPieces[i].GetComponentsInChildren<Renderer>())
                {
                    string path = $"{SandboxDir}/Materials/Sandbox_DummyPiece{i}_{r++}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(renderer.sharedMaterial);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    material.shader = renderer.sharedMaterial.shader;
                    material.CopyPropertiesFromMaterial(renderer.sharedMaterial);
                    Transform t = renderer.transform;
                    Vector3 axis = t.InverseTransformDirection(Vector3.up).normalized;
                    float low = Vector3.Dot(axis, t.InverseTransformPoint(new Vector3(t.position.x, _dummyLowCut, t.position.z)));
                    float neck = Vector3.Dot(axis, t.InverseTransformPoint(new Vector3(t.position.x, _dummyNeckCut, t.position.z)));
                    material.SetVector("_ClipAxis", axis);
                    material.SetFloat("_ClipMin", i == 0 ? -100000f : i == 1 ? low : neck);
                    material.SetFloat("_ClipMax", i == 0 ? low : i == 1 ? neck : 100000f);
                    // Both sides drawn, so a cut edge shows the inside of the sack and not a hole.
                    material.SetFloat("_Cull", 0f);
                    EditorUtility.SetDirty(material);
                    renderer.sharedMaterial = material;
                }
            }
            _dummyPieces = null;
        }

        /// <summary>Bounds of the points whose height lies in a band; false if there are none.</summary>
        private static bool SliceBounds(System.Collections.Generic.List<Vector3> points, float from, float to, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Vector3 point in points)
            {
                if (point.y < from || point.y >= to)
                    continue;
                if (!any)
                    bounds = new Bounds(point, Vector3.zero);
                else
                    bounds.Encapsulate(point);
                any = true;
            }
            return any;
        }

        private static int Narrowest(float[] width, int from, int to)
        {
            int best = from;
            for (int b = from; b <= to && b < width.Length; b++)
                if (width[b] < width[best])
                    best = b;
            return best;
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

            // Weapon tables on the left, in two rows: tables for one-handed weapons and deeper ones
            // for polearms and the like. Six short weapons or five long ones to a table.
            const float tableTop = 0.85f;
            var shortTables = new[] { new Vector3(-2.3f, 0f, 1.1f), new Vector3(-2.3f, 0f, 2.5f), new Vector3(-5.5f, 0f, 1.1f), new Vector3(-5.5f, 0f, 2.5f) };
            var longTables = new[] { new Vector3(-2.3f, 0f, 3.9f), new Vector3(-5.5f, 0f, 3.9f), new Vector3(-5.5f, 0f, -0.5f) };
            int tableNumber = 0;
            foreach (Vector3[] group in new[] { shortTables, longTables })
            {
                foreach (Vector3 at in group)
                {
                    bool deep = group == longTables;
                    GameObject table = Primitive(PrimitiveType.Cube, "Weapon table " + (++tableNumber), gallery.transform, new Vector3(at.x, tableTop / 2f, at.z), new Vector3(2.9f, tableTop, deep ? 1.3f : 1.0f), wood);
                    table.isStatic = true;
                    AddImpactSound(table, "sfx_sword_hit_wood_heavy");
                    Dress(table, ArtDir + "/Props/prp_table_long.glb", gallery.transform);
                }
            }

            int shortIndex = 0, longIndex = 0;
            foreach (GameObject prefab in LoadModels("Weapons"))
            {
                if (prefab.name == "wpn_sword_training")
                    continue; // the sword on the plinth is this model
                if (prefab.name.StartsWith("wpn_staff_") || prefab.name.StartsWith("wpn_wand_"))
                    continue; // staffs and wands have tables of their own
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                Vector3 size = WorldBounds(instance).size;
                bool isLong = Mathf.Max(size.x, size.y, size.z) > 1.15f;
                LayFlat(instance, alongX: isLong);
                Vector3 slot;
                if (isLong)
                {
                    Vector3 at = longTables[Mathf.Min(longIndex / 5, longTables.Length - 1)];
                    slot = new Vector3(at.x, tableTop, at.z - 0.48f + 0.24f * (longIndex % 5));
                    longIndex++;
                }
                else
                {
                    Vector3 at = shortTables[Mathf.Min(shortIndex / 6, shortTables.Length - 1)];
                    slot = new Vector3(at.x - 1.2f + 0.47f * (shortIndex % 6), tableTop, at.z);
                    shortIndex++;
                }
                RestOn(instance, slot);
                bool heavy = prefab.name.Contains("great") || prefab.name.Contains("halberd") || prefab.name.Contains("warhammer") || prefab.name.Contains("scythe") || prefab.name.Contains("glaive");
                bool isShield = prefab.name.Contains("shield") || prefab.name.Contains("buckler");
                // Shields are held at the centre of a broad face and sag in the hand if heavy.
                MakeWeapon(instance, isShield ? 0.4f : heavy ? 0.9f : 0.5f);
            }
            BuildSpellTable(gallery.transform, wood, tableTop);

            // Loose props to pick up, and scenery.
            string[] loose = { "prp_crate", "prp_chest", "prp_barrel", "prp_mote_vial", "prp_lantern_husk", "prp_bench" };
            int looseIndex = 0;
            float sceneryX = -9f;
            foreach (GameObject prefab in LoadModels("Props"))
            {
                if (prefab.name == "prp_table_long" || prefab.name == "prp_plinth_stone")
                    continue; // used as the tables and the sword's plinth
                if (System.Array.IndexOf(loose, prefab.name) < 0)
                    continue; // scenery is placed by the layout below
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                {
                    RestOn(instance, new Vector3(2.1f + (looseIndex % 3) * 0.95f, 0f, -0.3f + (looseIndex / 3) * 1.0f));
                    looseIndex++;
                    MakeGrabbable(instance, prefab.name.Contains("vial") ? 0.2f : (prefab.name.Contains("lantern") ? 0.6f : 6f));
                }

            }

            // Huskhaven, a small walled town round the spawn, and the Windlow meadow beyond its gate
            // (decision 0016). The road runs north from the square, through the gate, across the
            // meadow where the creatures are, to the stone arch where the Rootwarden waits.
            // name, x, z, turn in degrees. A model may be placed more than once.
            var layout = new (string name, float x, float z, float yaw)[]
            {
                // the wall: a gate in the north side, on the road
                ("env_town_gate", 0f, 24f, GateYaw),
                ("env_town_wall", -7.6f, 24f, WallYaw), ("env_town_wall", 7.6f, 24f, WallYaw),
                ("env_town_wall", -15.6f, 24f, WallYaw), ("env_town_wall", 15.6f, 24f, WallYaw),
                ("env_town_wall", -19.6f, 20f, WallYaw + 90f), ("env_town_wall", -19.6f, 12f, WallYaw + 90f), ("env_town_wall", -19.6f, 4f, WallYaw + 90f), ("env_town_wall", -19.6f, -4f, WallYaw + 90f),
                ("env_town_wall", 19.6f, 20f, WallYaw + 90f), ("env_town_wall", 19.6f, 12f, WallYaw + 90f), ("env_town_wall", 19.6f, 4f, WallYaw + 90f), ("env_town_wall", 19.6f, -4f, WallYaw + 90f),
                ("env_town_wall", -16f, -8f, WallYaw), ("env_town_wall", -8f, -8f, WallYaw), ("env_town_wall", 0f, -8f, WallYaw), ("env_town_wall", 8f, -8f, WallYaw), ("env_town_wall", 16f, -8f, WallYaw),
                ("env_watchtower", 6.2f, 20.6f, 0f),
                // the west side of the square: inn, smithy, a husk home
                ("env_inn_huskhaven", -12f, 12.5f, HouseYaw + 90f), ("env_smithy", -11.5f, 3f, HouseYaw), ("env_house_husk", -12.5f, 19.5f, HouseYaw + 110f),
                // the east side: cottages and the storehouse
                ("env_house_cottage_a", 12f, 17.5f, HouseYaw - 90f), ("env_house_cottage_b", 14f, 10.5f, HouseYaw - 90f), ("env_storehouse", 13.5f, -2.5f, HouseYaw - 90f),
                // the square
                ("env_fountain", 4.6f, 12.5f, 0f), ("prp_cradle_shrine", -4.2f, 16.6f, 180f),
                ("env_lantern_post", -2.3f, 7f, 0f), ("env_lantern_post", 2.3f, 7f, 0f), ("env_lantern_post", -2.3f, 14f, 0f), ("env_lantern_post", 2.3f, 14f, 0f), ("env_lantern_post", -2.3f, 20.5f, 0f), ("env_lantern_post", 2.3f, 20.5f, 0f),
                ("prp_notice_board", 2.9f, 4.4f, 160f), ("prp_market_stall", 6.4f, 8.8f, 200f), ("prp_well", -6.2f, 8.4f, 0f), ("prp_cart", 9.8f, -3.6f, 60f),
                
                ("env_campfire", 7.4f, 3.4f, 0f), ("env_stump", 8.3f, 1.9f, 30f), ("env_tent", 11.6f, 4.6f, -70f),
                ("env_tree_blob", -16.5f, -3f, 0f), ("env_tree_blob", 16.5f, 21f, 80f), ("env_bush", -8.5f, 21.5f, 0f), ("env_bush", 9.5f, 20.5f, 90f), ("env_bush", -15f, 7.5f, 200f),
                ("prp_fence", -7.5f, -3.5f, 20f), ("prp_dummy_training", -4.2f, 5.6f, 160f), ("prp_dummy_training", -5.8f, 6.3f, 190f),
                ("env_banner", -3.4f, 22.6f, 0f), ("env_banner", 3.4f, 22.6f, 0f),
                // the Windlow, outside the gate: low walls, wind-bent trees, rocks, and the road to the arch
                ("env_stone_wall", -9f, 33f, 20f), ("env_stone_wall", 11f, 39f, -30f), ("env_stone_wall", -13f, 45f, 70f),
                ("env_tree_blob", -8f, 30f, 40f), ("env_tree_blob", 9f, 31f, 250f), ("env_tree_blob", 17f, 42f, 200f), ("env_tree_blob", -19f, 38f, 130f), ("env_tree_blob", -6f, 50f, 10f), ("env_tree_blob", 12f, 52f, 300f),
                ("env_bush", -4.2f, 29f, 0f), ("env_bush", 5f, 36f, 90f), ("env_bush", 13f, 33f, 200f), ("env_bush", -11f, 40f, 300f), ("env_bush", 2f, 44f, 45f),
                ("env_rock_cluster", 4f, 31f, 20f), ("env_rock_cluster", -14f, 30f, 110f), ("env_boulder", -4f, 38f, 70f), ("env_boulder", 15f, 47f, 10f), ("env_rock_cluster", 7f, 48f, 200f),
                ("prp_signpost", 2.4f, 27.5f, -20f), ("env_arch", 0f, 54f, 0f),
            };
            // Old models kept in the repository but no longer shown: their replacements are in the town.
            var retired = new System.Collections.Generic.HashSet<string> { "env_husk_house" };
            var placed = new System.Collections.Generic.HashSet<string>();
            foreach ((string name, float x, float z, float yaw) in layout)
            {
                string folder = name.StartsWith("env_") ? "Environment" : "Props";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtDir}/{folder}/{name}.glb");
                if (prefab == null)
                    continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                // The gate must be wide enough to walk through with room to spare. At its made size
                // the robot player could not find a way through it and walked round the wall.
                if (name == "env_arch")
                    instance.transform.localScale *= 1.8f;
                // The lantern posts came out with lanterns a metre across; at this size they are head height.
                if (name == "env_lantern_post")
                    instance.transform.localScale *= 0.72f;
                RestOn(instance, new Vector3(x, 0f, z));
                MakeSolid(instance);
                placed.Add(name);
            }

            // Anything not in the layout yet still appears, in a row at the back, so nothing is hidden.
            float spareX = -12f;
            foreach (string folder in new[] { "Environment", "Props" })
            {
                foreach (GameObject prefab in LoadModels(folder))
                {
                    if (placed.Contains(prefab.name) || retired.Contains(prefab.name) || prefab.name.StartsWith("prp_eq_") || prefab.name == "prp_table_long" || prefab.name == "prp_plinth_stone" || System.Array.IndexOf(loose, prefab.name) >= 0)
                        continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gallery.transform);
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    RestOn(instance, new Vector3(spareX, 0f, -13f));
                    MakeSolid(instance);
                    spareX += 5f;
                }
            }

            // The people of Huskhaven, each where they would be, as statues until they can be rigged:
            // name, where they stand (x, z), what they face (x, z).
            var spots = new System.Collections.Generic.Dictionary<string, (float x, float z, float lookX, float lookZ)>
            {
                { "chr_rooted_merchant", (5.9f, 10.1f, 2f, 5f) },
                { "chr_rooted_smith", (-7.6f, 3.2f, 0f, 2f) },
                { "chr_rooted_innkeeper", (-8.2f, 12.5f, 0f, 10f) },
                { "chr_rooted_elder", (-3.2f, 15.6f, 0f, 9f) },
                { "chr_rooted_child", (3.2f, 11.2f, 4.6f, 12.5f) },
                { "chr_woken_starter", (9.3f, 3.1f, 7.4f, 3.4f) },
                { "chr_woken_weaver", (8.4f, 5.1f, 7.4f, 3.4f) },
                { "chr_woken_shieldbearer", (6.4f, 5.2f, 7.4f, 3.4f) }
            };
            var people = LoadModels("Characters");
            float spareSpot = 0f;
            RuntimeAnimatorController humanAnimator = BuildHumanAnimator();
            foreach (GameObject person in people)
            {
                if (person.name.StartsWith("chr_body_"))
                    continue; // a body for the player, not a townsperson
                if (person.GetComponentInChildren<SkinnedMeshRenderer>() != null && humanAnimator != null)
                {
                    // Rigged people stand where their unrigged version stood (same name without "rig_").
                    string role = person.name.Replace("chr_rig_", "chr_");
                    if (!spots.TryGetValue(role, out (float x, float z, float lookX, float lookZ) place))
                        place = (1.6f + (spareSpot += 1.3f), 6.2f, 0f, 0f);
                    GameObject actor = PlaceActor(gallery.transform, person, humanAnimator, new Vector3(place.x, 0f, place.z), new Vector3(place.lookX, 0f, place.lookZ), "Idle_A", person.name.Contains("child") ? 1.25f : 1.72f);
                    GiveMind(actor, person.name);
                    continue;
                }                var instance = (GameObject)PrefabUtility.InstantiatePrefab(person, gallery.transform);
                if (!spots.TryGetValue(person.name, out (float x, float z, float lookX, float lookZ) spot))
                    spot = (12f + (spareSpot += 1.2f), 1f, 0f, 0f);
                instance.transform.localRotation = Quaternion.LookRotation(new Vector3(spot.lookX - spot.x, 0f, spot.lookZ - spot.z));
                RestOn(instance, new Vector3(spot.x, 0f, spot.z));
                MakeSolid(instance);
            }

            // Creatures wait beyond the wall, either side of the road, facing the gate.
            var creatures = LoadModels("Enemies");
            for (int i = 0; i < creatures.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(creatures[i], gallery.transform);
                int side = i % 2 == 0 ? -1 : 1;
                Vector3 spot = new Vector3(side * (4.5f + 4f * (i / 2)), 0f, 34f + 4f * (i / 2));
                if (creatures[i].GetComponentInChildren<SkinnedMeshRenderer>() != null && humanAnimator != null)
                {
                    // Man-shaped and rigged: it lives, fights and can lose limbs. Each kind has its
                    // place in the meadow, its size, its toughness and what it carries.
                    // kind -> (x, z, height in metres, health, weapon model or null)
                    var kinds = new System.Collections.Generic.Dictionary<string, (float x, float z, float tall, float health, string weapon)>
                    {
                        { "enm_barkbound", (-4.5f, 33f, 1.85f, 90f, "wpn_mace") },
                        { "enm_barkbound_scout", (5.5f, 36f, 1.75f, 55f, "wpn_handaxe") },
                        { "enm_barkbound_brute", (-7.5f, 42f, 2.25f, 170f, "wpn_warhammer") },
                        { "enm_husk_wight", (7f, 44f, 1.95f, 120f, null) },
                        { "enm_rootwarden", (0f, 49f, 2.9f, 260f, null) }, // in the road before the arch, as the world bible has it
                    };
                    Object.DestroyImmediate(instance);
                    if (!kinds.TryGetValue(creatures[i].name, out (float x, float z, float tall, float health, string weapon) kind))
                        kind = (spot.x, spot.z, 1.85f, 90f, null);
                    GameObject enemy = PlaceActor(gallery.transform, creatures[i], humanAnimator, new Vector3(kind.x, 0f, kind.z), new Vector3(0f, 0f, 24f), "Zombie_Idle", kind.tall);
                    MakeEnemy(enemy, kind.tall, kind.health, kind.weapon);
                    continue;
                }                instance.transform.localRotation = Quaternion.LookRotation(new Vector3(-spot.x, 0f, 24f - spot.z));
                RestOn(instance, spot);
                MakeSolid(instance);
            }

            BuildCampfire(gallery.transform, new Vector3(7.4f, 0f, 3.4f));
            AddLoopingSound(gallery.transform, "Shrine hum", "sfx_cradle_hum_01", new Vector3(-4.2f, 1f, 16.6f), 0.35f, 9f);
        }

        /// <summary>Flames, sparks, a wavering warm light and a crackle on the campfire model.</summary>
        private static void BuildCampfire(Transform parent, Vector3 position)
        {
            Shader glow = Shader.Find("Godtree/Glow");
            if (glow == null)
            {
                Debug.LogWarning("[Godtree] Godtree/Glow shader not found; the campfire stays cold");
                return;
            }
            string path = SandboxDir + "/Materials/Sandbox_Flame.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(glow);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = glow;
            material.SetFloat("_Intensity", 2.2f);
            material.SetFloat("_Softness", 1.5f);
            EditorUtility.SetDirty(material);

            var fire = new GameObject("Campfire (flames)");
            fire.transform.SetParent(parent, false);
            fire.transform.position = position + Vector3.up * 0.22f;
            fire.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // particle cones point along Z

            ParticleSystem flames = fire.AddComponent<ParticleSystem>();
            ConfigureFire(flames, material, rate: 34f, life: (0.55f, 1.0f), speed: (0.55f, 1.1f), size: (0.22f, 0.42f), radius: 0.16f,
                new Color(1f, 0.86f, 0.42f), new Color(1f, 0.42f, 0.12f), new Color(0.75f, 0.12f, 0.05f));

            var sparkObject = new GameObject("Sparks");
            sparkObject.transform.SetParent(fire.transform, false);
            ParticleSystem sparks = sparkObject.AddComponent<ParticleSystem>();
            ConfigureFire(sparks, material, rate: 7f, life: (1.2f, 2.2f), speed: (0.9f, 1.8f), size: (0.025f, 0.05f), radius: 0.12f,
                new Color(1f, 0.9f, 0.6f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.3f, 0.1f));
            ParticleSystem.NoiseModule drift = sparks.noise;
            drift.enabled = true;
            drift.strength = 0.5f;
            drift.frequency = 0.8f;

            var lightObject = new GameObject("Firelight");
            lightObject.transform.SetParent(fire.transform, false);
            lightObject.transform.position = position + Vector3.up * 0.7f;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.28f);
            light.range = 6f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
            lightObject.AddComponent<Godtree.Sandbox.FireFlicker>();

            AddLoopingSound(fire.transform, "Crackle", "sfx_lantern_flame_01", position + Vector3.up * 0.3f, 0.5f, 8f);
        }

        private static void ConfigureFire(ParticleSystem system, Material material, float rate, (float min, float max) life, (float min, float max) speed,
            (float min, float max) size, float radius, Color young, Color middle, Color old)
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.min, life.max);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.min, speed.max);
            main.startSize = new ParticleSystem.MinMaxCurve(size.min, size.max);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = radius;

            ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(young, 0f), new GradientColorKey(middle, 0.45f), new GradientColorKey(old, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;

            ParticleSystem.SizeOverLifetimeModule shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A sound that plays on a loop from one place and fades with distance.</summary>
        private static void AddLoopingSound(Transform parent, string name, string clipId, Vector3 position, float volume, float range)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Sfx/{clipId}.ogg");
            if (clip == null)
                return;
            var holder = new GameObject(name + " (sound)");
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            AudioSource source = holder.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = range;
        }

        /// <summary>
        /// A table of staffs and wands on the right (decision 0016): three elements, and tiers
        /// one to three. Each is the same wand or staff model with a coloured stone at its tip
        /// that tells the element (colour) and the tier (size).
        /// </summary>
        private static void BuildSpellTable(Transform parent, Material wood, float tableTop)
        {
            var centre = new Vector3(4.3f, tableTop / 2f, 2.7f);
            GameObject table = Primitive(PrimitiveType.Cube, "Spell table", parent, centre, new Vector3(3.2f, tableTop, 1.3f), wood);
            table.isStatic = true;
            AddImpactSound(table, "sfx_sword_hit_wood_heavy");
            Dress(table, ArtDir + "/Props/prp_table_long.glb", parent);

            var wand = AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir + "/Weapons/wpn_wand_thorn.glb");
            var staff = AssetDatabase.LoadAssetAtPath<GameObject>(ArtDir + "/Weapons/wpn_staff_weaver.glb");
            if (wand == null || staff == null)
            {
                Debug.LogWarning("[Godtree] wand or staff model missing; no spell table");
                return;
            }
            // A second, deeper table behind the first for the great staffs.
            GameObject table2 = Primitive(PrimitiveType.Cube, "Spell table 2", parent, centre + new Vector3(0f, 0f, 1.6f), new Vector3(3.2f, tableTop, 1.0f), wood);
            table2.isStatic = true;
            AddImpactSound(table2, "sfx_sword_hit_wood_heavy");
            Dress(table2, ArtDir + "/Props/prp_table_long.glb", parent);

            // Three tiers of each element: a wand (one spell), a plain staff (two), a great staff
            // (three). Each element has its own wand and great staff where that model exists;
            // otherwise the plain wand or staff stands in. Every one carries its element's stone.
            GameObject Own(string id, GameObject fallback)
            {
                var own = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtDir}/Weapons/{id}.glb");
                return own != null ? own : fallback;
            }
            var elements = new[] { Godtree.Sandbox.SpellElement.Ember, Godtree.Sandbox.SpellElement.Frost, Godtree.Sandbox.SpellElement.Storm, Godtree.Sandbox.SpellElement.Root };
            var list = new System.Collections.Generic.List<(GameObject prefab, Godtree.Sandbox.SpellElement element, int tier, Vector3 slot, bool alongX)>();
            for (int e = 0; e < elements.Length; e++)
            {
                string word = elements[e].ToString().ToLowerInvariant();
                list.Add((Own("wpn_wand_" + word, wand), elements[e], 1, new Vector3(2.82f + 0.31f * e, tableTop, 2.7f), false));
                list.Add((staff, elements[e], 2, new Vector3(4.75f, tableTop, 2.25f + 0.3f * e), true));
                list.Add((Own("wpn_staff_" + word, staff), elements[e], 3, new Vector3(4.3f, tableTop, 3.92f + 0.25f * e), true));
            }
            var foci = list.ToArray();            string[] numerals = { "", "I", "II", "III" };
            foreach ((GameObject prefab, Godtree.Sandbox.SpellElement element, int tier, Vector3 slot, bool alongX) in foci)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = $"{prefab.name} ({element} {numerals[tier]})";
                LayFlat(instance, alongX);
                RestOn(instance, slot);
                MakeWeapon(instance, tier >= 2 ? 0.6f : 0.25f);

                // Spells leave from the end furthest from the hand, along the weapon's length.
                GripPose pose = instance.GetComponentInChildren<GripPose>();
                Rigidbody body = instance.GetComponentInChildren<Rigidbody>();
                if (pose == null || body == null)
                    continue;
                Vector3 grip = pose.transform.TransformPoint(pose.localGripPoint);
                Vector3 axis = pose.transform.TransformDirection(pose.localMainAxis).normalized;
                float furthest = 0f;
                foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                    foreach (Vector3 vertex in filter.sharedMesh.vertices)
                        furthest = Mathf.Max(furthest, Vector3.Dot(filter.transform.TransformPoint(vertex) - grip, axis));
                Vector3 tip = grip + axis * furthest;
                var focus = body.gameObject.AddComponent<Godtree.Sandbox.SpellFocus>();
                focus.element = element;
                focus.tier = tier;
                focus.localTip = body.transform.InverseTransformPoint(tip);
                focus.localAim = body.transform.InverseTransformDirection(axis);

                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = "Element stone";
                Object.DestroyImmediate(stone.GetComponent<Collider>());
                stone.transform.SetParent(body.transform, true);
                stone.transform.position = tip - axis * 0.02f;
                float stoneSize = 0.035f + 0.02f * tier;
                Vector3 lossy = body.transform.lossyScale;
                stone.transform.localScale = new Vector3(stoneSize / lossy.x, stoneSize / lossy.y, stoneSize / lossy.z);
                string stonePath = $"{SandboxDir}/Materials/Sandbox_Stone_{element}.mat";
                var stoneMaterial = AssetDatabase.LoadAssetAtPath<Material>(stonePath);
                if (stoneMaterial == null)
                {
                    stoneMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(stoneMaterial, stonePath);
                }
                stoneMaterial.SetColor("_BaseColor", Godtree.Sandbox.SpellFocus.ColourOf(element) * 1.8f);
                EditorUtility.SetDirty(stoneMaterial);
                stone.GetComponent<MeshRenderer>().sharedMaterial = stoneMaterial;
                stone.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// One animator controller for everyone on the shared human skeleton: a state per clip in
        /// the animation file, named after the clip. Clips are copied so that the ones meant to
        /// repeat can be marked as looping (the imported ones cannot be changed).
        /// </summary>
        private static UnityEditor.Animations.AnimatorController BuildHumanAnimator()
        {
            const string source = ArtDir + "/Animations/anm_human_basic.glb";
            string clipDir = SandboxDir + "/Animations";
            Directory.CreateDirectory(clipDir);
            AssetDatabase.Refresh();
            string controllerPath = SandboxDir + "/HumanActor.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            UnityEditor.Animations.AnimatorStateMachine machine = controller.layers[0].stateMachine;
            string[] once = { "Greeting", "Yes", "Head Nod", "Reject", "Angry", "Confused", "Hit_", "Death_", "Sword_Regular", "Sword_Attack", "Punch_", "Zombie_Scratch", "Zombie Yell", "Victory", "Interact", "Spell_" };
            int count = 0;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(source))
            {
                if (!(asset is AnimationClip imported) || imported.name.StartsWith("__preview"))
                    continue;
                string clipPath = $"{clipDir}/{imported.name.Replace(' ', '_')}.anim";
                AssetDatabase.DeleteAsset(clipPath);
                AnimationClip clip = Object.Instantiate(imported);
                clip.name = imported.name;
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = !System.Array.Exists(once, prefix => imported.name.StartsWith(prefix));
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, clipPath);
                UnityEditor.Animations.AnimatorState state = machine.AddState(imported.name);
                state.motion = clip;
                if (imported.name == "Idle_A")
                    machine.defaultState = state;
                count++;
            }
            Debug.Log($"[Godtree] human animator: {count} clips");
            return count > 0 ? controller : null;
        }

        /// <summary>A person on the shared human skeleton, standing at a spot, alive.</summary>
        private static GameObject PlaceActor(Transform parent, GameObject prefab, RuntimeAnimatorController controller, Vector3 spot, Vector3 lookAt, string idleClip, float heightMetres)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = spot;
            Vector3 facing = Vector3.ProjectOnPlane(lookAt - spot, Vector3.up);
            if (facing.sqrMagnitude > 0.01f)
                instance.transform.rotation = Quaternion.LookRotation(facing.normalized);
            float modelHeight = WorldBounds(instance).size.y;
            if (heightMetres > 0f && modelHeight > 0.01f)
                instance.transform.localScale *= heightMetres / modelHeight;

            // The clips address bones as "Armature/root/...". The character file has its bones
            // directly under its top, so give them the same parent the clip file has.
            if (instance.transform.Find("Armature") == null && instance.transform.Find("root") != null)
            {
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                var armature = new GameObject("Armature");
                armature.transform.SetParent(instance.transform, false);
                instance.transform.Find("root").SetParent(armature.transform, false);
            }
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var actor = instance.AddComponent<Godtree.Sandbox.NpcActor>();
            actor.idleClip = idleClip;

            foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                skinned.updateWhenOffscreen = true;
            // Something to bump into and to point at.
            var capsule = instance.AddComponent<CapsuleCollider>();
            float scale = instance.transform.lossyScale.y;
            capsule.height = heightMetres / scale;
            capsule.radius = 0.22f / scale;
            capsule.center = new Vector3(0f, heightMetres * 0.5f / scale, 0f);
            return instance;
        }

        /// <summary>
        /// Turns a placed, animated figure into a creature that fights: shapes on its limbs for
        /// weapons to strike, the limb-losing component if its model was cut into parts, and the
        /// mind that walks it up to the player.
        /// </summary>
        private static void MakeEnemy(GameObject enemy, float heightMetres, float health, string weaponModel)
        {
            Object.DestroyImmediate(enemy.GetComponent<Godtree.Sandbox.NpcActor>());
            Object.DestroyImmediate(enemy.GetComponent<CapsuleCollider>());
            Rigidbody body = enemy.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var bones = new System.Collections.Generic.Dictionary<string, Transform>();
            foreach (Transform bone in enemy.GetComponentsInChildren<Transform>())
                if (!bones.ContainsKey(bone.name))
                    bones[bone.name] = bone;
            float size = heightMetres / 1.72f;
            // from bone, to bone, radius in metres on a person 1.72 m tall
            var limbs = new (string from, string to, float radius)[]
            {
                ("upperarm_l", "lowerarm_l", 0.06f), ("lowerarm_l", "hand_l", 0.055f),
                ("upperarm_r", "lowerarm_r", 0.06f), ("lowerarm_r", "hand_r", 0.055f),
                ("thigh_l", "calf_l", 0.085f), ("calf_l", "foot_l", 0.07f),
                ("thigh_r", "calf_r", 0.085f), ("calf_r", "foot_r", 0.07f),
                ("pelvis", "neck_01", 0.17f), ("neck_01", "head_leaf", 0.12f),
            };
            foreach ((string from, string to, float radius) in limbs)
            {
                if (!bones.TryGetValue(from, out Transform a) || !bones.TryGetValue(to, out Transform b))
                    continue;
                var shape = new GameObject("Hit shape " + from);
                shape.transform.SetParent(a, false);
                Vector3 along = b.position - a.position;
                shape.transform.SetPositionAndRotation(a.position, Quaternion.LookRotation(along.normalized));
                float scale = shape.transform.lossyScale.x;
                var capsule = shape.AddComponent<CapsuleCollider>();
                capsule.direction = 2;
                capsule.radius = radius * size / scale;
                capsule.height = (along.magnitude + radius * size) / scale;
                capsule.center = new Vector3(0f, 0f, along.magnitude * 0.5f / scale);
            }

            bool inParts = false;
            foreach (SkinnedMeshRenderer renderer in enemy.GetComponentsInChildren<SkinnedMeshRenderer>())
                inParts |= renderer.name.StartsWith("part_");
            if (inParts)
                enemy.AddComponent<Dismemberable>();
            var mind = enemy.AddComponent<Godtree.Sandbox.EnemyActor>();
            mind.maxHealth = health;
            mind.strikeDistance = 1.5f * Mathf.Sqrt(size);
            mind.strikeDamage = 12f * size;
            mind.walkSpeed = 1.0f * Mathf.Sqrt(size);
            // An armoured thing stands and strikes like a fighter; the big ones lumber.
            if (enemy.name.Contains("barkbound") && !enemy.name.Contains("brute"))
            {
                mind.idleClip = "Fighting Idle";
                mind.walkClip = "Walk";
                mind.strikeClip = weaponModel != null ? "Sword_Regular_A" : "Punch_Cross";
            }
            if (enemy.name.Contains("scout"))
                mind.walkSpeed = 1.9f;
            if (enemy.name.Contains("brute"))
            {
                mind.walkSpeed = 0.8f;
                mind.strikeDamage = 22f;
            }

            // Something in its right hand: for show, fixed to the hand bone. These models stand
            // head up and handle down, so the handle end goes in the fist and the head points
            // the way the creature faces.
            if (weaponModel != null && bones.TryGetValue("hand_r", out Transform hand))
            {
                var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtDir}/Weapons/{weaponModel}.glb");
                if (weaponPrefab != null)
                {
                    var weapon = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab, enemy.transform);
                    weapon.name = "Carried " + weaponModel;
                    weapon.transform.rotation = Quaternion.LookRotation(Vector3.up, -enemy.transform.forward) * Quaternion.identity;
                    weapon.transform.rotation = Quaternion.FromToRotation(Vector3.up, enemy.transform.forward);
                    weapon.transform.localScale *= size;
                    Bounds held = WorldBounds(weapon);
                    Vector3 handleEnd = held.center - enemy.transform.forward * Vector3.Dot(held.extents, new Vector3(Mathf.Abs(enemy.transform.forward.x), Mathf.Abs(enemy.transform.forward.y), Mathf.Abs(enemy.transform.forward.z)));
                    weapon.transform.position += hand.position - handleEnd - enemy.transform.forward * (0.07f * size);
                    weapon.transform.SetParent(hand, true);
                }
            }        }

        /// <summary>
        /// Who each person is, for free conversation (decision 0016). Names follow the world
        /// bible's rule for the Rooted (short, soft). These people and their natures are sandbox
        /// inventions of the lead, listed for the owner in docs/lead-handoff.md; they are not canon.
        /// </summary>
        private static void GiveMind(GameObject actor, string model)
        {
            var sheets = new System.Collections.Generic.Dictionary<string, (string name, string role, string nature, string voice, float speed)>
            {
                { "chr_rooted_innkeeper", ("Tobin", "the innkeeper of Huskhaven", "A stout, warm, slightly fussy man who cannot see a hungry person without feeding them. He calls every Woken 'seedling', is proud of his stew, and worries when the young ones go out past the wall.", "am_michael", 1.0f) },
                { "chr_rooted_smith", ("Mira", "the smith of Huskhaven", "Blunt, cheerful and practical. She likes talking about tools, edges and a good hard swing, and teases newcomers gently. She mends what the Woken break and thinks they break too much.", "af_bella", 1.05f) },
                { "chr_rooted_elder", ("Hale", "the elder of Huskhaven, who keeps the Cradle shrine", "Old, slow and kind. He speaks in short sayings about seeds, rain and patience, and believes the gardener story more firmly than anyone. He has watched many Woken leave for the tree and none come back.", "bm_george", 0.92f) },
                { "chr_rooted_merchant", ("Senna", "a travelling merchant who has stopped in Huskhaven", "Quick, sly and friendly. She has walked a little way up the Rootway, further than most Rooted dare, and likes to hint at what she saw there, with some exaggeration. She will always try to sell you something.", "bf_emma", 1.05f) },
                { "chr_rooted_child", ("Nilo", "a village child of eight in Huskhaven", "Curious and fearless, full of questions. Nilo wants to be a Woken and fight Burrlings with a wooden sword, and thinks the Woken are the most exciting people in the world.", "af_sky", 1.12f) },
                { "chr_woken_starter", ("Aren", "a Woken who woke from his husk only a few days ago", "Eager and unsure. He knows little more than the newcomer does, admits it freely, and is working up the courage to go out through the gate.", "am_puck", 1.0f) },
                { "chr_woken_weaver", ("Lysa", "a Woken weaver resting in Huskhaven", "Calm and precise, fascinated by motes. She can gather them in her hands and tie them into knots of light, and will explain how it feels, though she says it cannot really be taught in words.", "af_heart", 0.98f) },
                { "chr_woken_shieldbearer", ("Doran", "a Woken who fights with a shield", "Steady, protective, a man of few words. He has faced Thornbacks in the Windlow and has plain advice about keeping your guard up and your feet moving.", "am_adam", 0.97f) },
            };
            if (!sheets.TryGetValue(model, out (string name, string role, string nature, string voice, float speed) sheet))
                return;
            var mind = actor.AddComponent<Godtree.Sandbox.NpcMind>();
            mind.npcName = sheet.name;
            mind.role = sheet.role;
            mind.personality = sheet.nature;
            mind.voice = sheet.voice;
            mind.voiceSpeed = sheet.speed;
            actor.name = $"{model} ({sheet.name})";
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
            if (name.Contains("shield") || name.Contains("buckler"))
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

            // Blade-like. The weapon's own long direction is found from its shape (its principal
            // axis), not from its bounding box, because some models stand at an angle.
            Vector3 mean = Vector3.zero;
            foreach (Vector3 p in points) mean += p;
            mean /= points.Count;
            Vector3 axis = PrincipalAxis(points, mean, Vector3.zero);
            Vector3 side = PrincipalAxis(points, mean, axis);

            float minAlong = float.MaxValue, maxAlong = float.MinValue;
            foreach (Vector3 p in points)
            {
                float along = Vector3.Dot(p - mean, axis);
                minAlong = Mathf.Min(minAlong, along);
                maxAlong = Mathf.Max(maxAlong, along);
            }
            float length = Mathf.Max(maxAlong - minAlong, 1e-5f);

            // Where along its length is the weapon widest? On a sword that is the crossguard, at
            // the handle end; on an axe, hammer, club or spear it is the head, at the far end.
            // (Concept art is not always drawn handle-down. Checked with Godtree > Render Grip Check.)
            var radii = new System.Collections.Generic.List<Vector2>(points.Count);
            foreach (Vector3 p in points)
            {
                Vector3 rel = p - mean;
                float along = Vector3.Dot(rel, axis);
                radii.Add(new Vector2((rel - axis * along).magnitude, (along - minAlong) / length));
            }
            radii.Sort((x, y) => y.x.CompareTo(x.x));
            int widest = Mathf.Max(1, radii.Count / 50);
            float widestAlong = 0f;
            for (int i = 0; i < widest; i++)
                widestAlong += radii[i].y;
            widestAlong /= widest;

            bool swordLike = name.Contains("sword") || name.Contains("rapier") || name.Contains("scimitar") || name.Contains("dagger") || name.Contains("sabre");
            bool handleIsLow = swordLike ? widestAlong < 0.5f : widestAlong >= 0.5f;
            bool pole = name.Contains("spear") || name.Contains("halberd") || name.Contains("staff") || name.Contains("glaive") || name.Contains("scythe") || name.Contains("greataxe"); // a great axe held at its end hung 107 degrees off in the hand
            float fromHandleEnd = pole ? 0.40f : (name.Contains("dagger") ? 0.18f : (name.Contains("great") ? 0.10f : 0.13f));

            pose.style = HoldStyle.Blade;
            pose.localMainAxis = handleIsLow ? axis : -axis;
            pose.localSideAxis = side;
            float gripAlong = handleIsLow ? minAlong + fromHandleEnd * length : maxAlong - fromHandleEnd * length;
            pose.localGripPoint = mean + axis * gripAlong;
        }

        /// <summary>
        /// The direction in which a cloud of points is most spread out, by power iteration on
        /// its covariance. Pass a direction to exclude to get the next most spread one.
        /// </summary>
        private static Vector3 PrincipalAxis(System.Collections.Generic.List<Vector3> points, Vector3 mean, Vector3 exclude)
        {
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (Vector3 p in points)
            {
                Vector3 d = p - mean;
                if (exclude != Vector3.zero)
                    d -= exclude * Vector3.Dot(d, exclude);
                xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
                yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
            }
            Vector3 v = new Vector3(0.577f, 0.61f, 0.54f);
            for (int i = 0; i < 60; i++)
            {
                v = new Vector3(xx * v.x + xy * v.y + xz * v.z, xy * v.x + yy * v.y + yz * v.z, xz * v.x + yz * v.y + zz * v.z);
                if (exclude != Vector3.zero)
                    v -= exclude * Vector3.Dot(v, exclude);
                if (v.sqrMagnitude < 1e-20f)
                    return Vector3.up;
                v.Normalize();
            }
            return v;
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
                "<b><color=#FFD86B>WARD KNOT</color></b>   Empty hand: hold A or X for a shield of light.\n" +
                "<b><color=#FFD86B>TALK</color></b>   Stand by someone, look at them, and speak.\nThey answer aloud. (Needs the NPC services running.)\n" +
                "<b><color=#FFD86B>STAFFS, WANDS</color></b>   On the tables to your right. Hold one,\nhold TRIGGER, let go. Red fire, blue frost, violet storm, green root.\nWand: one spell. Staff: two. Great staff: three; hold longer.\n\n" +
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
            Shader glow = Shader.Find("Godtree/Glow");
            if (glow != null)
            {
                string trailPath = SandboxDir + "/Materials/Sandbox_Trail.mat";
                var trail = AssetDatabase.LoadAssetAtPath<Material>(trailPath);
                if (trail == null)
                {
                    trail = new Material(glow);
                    AssetDatabase.CreateAsset(trail, trailPath);
                }
                trail.shader = glow;
                trail.SetFloat("_Intensity", 2.4f);
                trail.SetFloat("_Softness", 1.2f);
                trail.SetFloat("_Trail", 1f);
                EditorUtility.SetDirty(trail);
                abilities.glowMaterial = trail;

                var casting = new GameObject("Focus casting (staffs and wands)").AddComponent<Godtree.Sandbox.FocusCasting>();
                casting.transform.SetParent(parent, false);
                casting.glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(SandboxDir + "/Materials/Sandbox_Flame.mat");
                casting.trailMaterial = trail;
                casting.chargeClips = LoadClips("sfx_mote_chime");
                casting.castClips = LoadClips("sfx_knot_cast");
                casting.hitClips = LoadClips("sfx_mote_chime");
            }
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

        /// <summary>
        /// Shows a generated model in place of a plain box: the model is stretched to the box's
        /// size and position and the box is hidden but keeps its simple, reliable collider.
        /// </summary>
        private static void Dress(GameObject box, string modelPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
                return;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            visual.name = box.name + " (model)";
            Bounds target = box.GetComponent<Renderer>().bounds;
            Bounds bounds = WorldBounds(visual);
            // Turn the model so its long side runs the same way as the box's.
            if ((bounds.size.x > bounds.size.z) != (target.size.x > target.size.z))
            {
                visual.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = WorldBounds(visual);
            }
            Vector3 stretch = new Vector3(target.size.x / bounds.size.x, target.size.y / bounds.size.y, target.size.z / bounds.size.z);
            // The stretch is in world axes; after a quarter turn the model's own X and Z are swapped.
            bool turned = Mathf.Abs(visual.transform.rotation.eulerAngles.y - 90f) < 1f;
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, turned ? new Vector3(stretch.z, stretch.y, stretch.x) : stretch);
            visual.transform.position += target.center - WorldBounds(visual).center;
            box.GetComponent<MeshRenderer>().enabled = false;
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

        // ------------------------------------------------------------ look

        /// <summary>
        /// Generated models arrive with the importer's standard material. Give each the toon
        /// shader with its own painted texture, so they are lit the way the art bible asks.
        /// </summary>
        private static void ApplyToonToGeneratedModels(GameObject root)
        {
            Shader toon = Shader.Find("Godtree/Toon");
            if (toon == null)
            {
                Debug.LogError("[Godtree] Godtree/Toon shader not found");
                return;
            }
            string dir = SandboxDir + "/Materials/Toon";
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            var cache = new System.Collections.Generic.Dictionary<Material, Material>();
            var used = new System.Collections.Generic.HashSet<string>();
            int swapped = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.shader == toon || !AssetDatabase.GetAssetPath(source).EndsWith(".glb"))
                        continue;
                    if (!cache.TryGetValue(source, out Material replacement))
                    {
                        Texture texture = null;
                        foreach (string property in new[] { "baseColorTexture", "_BaseMap", "_MainTex" })
                            if (texture == null && source.HasProperty(property))
                                texture = source.GetTexture(property);
                        string modelName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source));
                        string path = texture != null ? $"{dir}/{modelName}.mat" : $"{dir}/{modelName}_{source.name.Replace(' ', '_')}.mat";
                        for (int n = 2; !used.Add(path); n++)
                            path = $"{dir}/{modelName}_{n}.mat";
                        replacement = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (replacement == null)
                        {
                            replacement = new Material(toon);
                            AssetDatabase.CreateAsset(replacement, path);
                        }
                        replacement.shader = toon;
                        replacement.SetTexture("_BaseMap", texture);
                        // A painted model's colour is in its texture. One with no texture (the caps on
                        // cut limbs) keeps the plain colour it came with.
                        Color plain = Color.white;
                        foreach (string property in new[] { "baseColorFactor", "_BaseColor", "_Color" })
                            if (texture == null && source.HasProperty(property))
                            {
                                plain = source.GetColor(property);
                                break;
                            }
                        replacement.SetColor("_BaseColor", plain);
                        // Things the wind moves: metres of sway at three metres up, and the height it starts at.
                        float sway = modelName == "env_tree_blob" ? 0.10f : modelName == "env_bush" ? 0.05f : modelName == "env_banner" ? 0.07f : modelName == "env_tent" ? 0.015f : 0f;
                        replacement.SetFloat("_Sway", sway);
                        replacement.SetFloat("_SwayFrom", modelName == "env_tree_blob" ? 1.6f : modelName == "env_banner" ? 0.9f : 0.2f);
                        // Generated people have some inside-out faces; drawn two-sided they show no holes.
                        replacement.SetFloat("_Cull", modelName.StartsWith("chr_") ? 0f : 2f);
                        EditorUtility.SetDirty(replacement);
                        cache[source] = replacement;
                    }
                    materials[i] = replacement;
                    changed = true;
                    swapped++;
                }
                if (changed)
                    renderer.sharedMaterials = materials;
            }
            // Materials left over from models no longer in the scene.
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { dir }))
            {
                string stale = AssetDatabase.GUIDToAssetPath(guid);
                if (!used.Contains(stale))
                    AssetDatabase.DeleteAsset(stale);
            }
            Debug.Log($"[Godtree] Toon materials applied: {swapped}");
        }

        /// <summary>Sun, sky, haze, colour grading and music: the mood of a bright afternoon in the meadows.</summary>
        private static void BuildLook(Transform parent)
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional)
                    continue;
                light.color = new Color(1f, 0.96f, 0.86f);
                light.intensity = 1.05f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
                RenderSettings.sun = light;
            }

            // Painted-looking sky with drifting clouds; Unity's plain procedural sky if the shader is missing.
            Shader skyShader = Shader.Find("Godtree/Sky");
            bool ownSky = skyShader != null && skyShader.isSupported;
            string skyPath = SandboxDir + (ownSky ? "/Materials/Sandbox_CloudSky.mat" : "/Materials/Sandbox_Sky.mat");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(ownSky ? skyShader : Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            if (ownSky)
            {
                sky.shader = skyShader;
                sky.SetColor("_ZenithColor", new Color(0.20f, 0.42f, 0.86f));
                sky.SetColor("_HorizonColor", new Color(0.72f, 0.86f, 0.98f));
                sky.SetColor("_GroundColor", new Color(0.70f, 0.83f, 0.96f));
                sky.SetFloat("_Coverage", 0.46f);
            }
            else
            {
                sky.SetColor("_SkyTint", new Color(0.36f, 0.62f, 0.95f));
                sky.SetColor("_GroundColor", new Color(0.80f, 0.89f, 0.98f));
                sky.SetFloat("_AtmosphereThickness", 0.7f);
                sky.SetFloat("_Exposure", 1.25f);
                sky.SetFloat("_SunSize", 0.035f);
            }
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.70f, 0.78f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.50f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.70f, 0.83f, 0.96f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 420f;

            string profilePath = SettingsDir + "/Sandbox_PostProcess.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.4f);
                bloom.scatter.Override(0.65f);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.Neutral);
                var grade = profile.Add<ColorAdjustments>(true);
                grade.postExposure.Override(0f);
                grade.contrast.Override(9f);
                grade.saturation.Override(14f);
                foreach (VolumeComponent component in profile.components)
                    AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(profile);
            }
            BuildGodtreeBackdrop(parent);
            BuildHills(parent);

            var volumeObject = new GameObject("Post-processing (global)");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var polishObject = new GameObject("Scene polish");
            polishObject.transform.SetParent(parent, false);
            var polish = polishObject.AddComponent<Godtree.Sandbox.ScenePolish>();
            polish.music = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Music/mus_huskhaven_town_a.ogg");
        }

        /// <summary>
        /// Pillar 2: the Godtree is always ahead. A painted card of the tree stands far beyond the
        /// meadow, straight ahead of where the player starts, hazed by distance.
        /// </summary>
        private static void BuildGodtreeBackdrop(Transform parent)
        {
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/Environment/env_godtree_backdrop.png");
            Shader shader = Shader.Find("Godtree/Backdrop");
            if (image == null || shader == null)
            {
                Debug.LogWarning("[Godtree] Godtree backdrop image or shader missing; the horizon stays empty");
                return;
            }
            string path = SandboxDir + "/Materials/Sandbox_GodtreeBackdrop.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", image);
            material.SetColor("_HazeColor", new Color(0.70f, 0.83f, 0.96f));
            material.SetFloat("_Haze", 0.42f);
            material.SetFloat("_Opacity", 0.94f);
            EditorUtility.SetDirty(material);

            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Quad);
            card.name = "The Godtree (distant backdrop)";
            Object.DestroyImmediate(card.GetComponent<Collider>());
            card.transform.SetParent(parent, false);
            const float size = 560f;
            card.transform.position = new Vector3(0f, size * 0.5f - 60f, 820f);
            card.transform.localScale = new Vector3(size, size, 1f);
            var renderer = card.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>
        /// Rolling hills all the way round, so the meadow does not end in a straight line. One
        /// ring-shaped mesh, lower straight ahead so the road and the Godtree stay in view. Too far
        /// to reach, so it has no collider.
        /// </summary>
        private static void BuildHills(Transform parent)
        {
            const int around = 160, rings = 16;
            const float inner = 58f, outer = 460f;
            var vertices = new Vector3[(rings + 1) * (around + 1)];
            var triangles = new int[rings * around * 6];
            for (int i = 0; i <= rings; i++)
            {
                float t = i / (float)rings;
                float radius = Mathf.Lerp(inner, outer, t);
                for (int j = 0; j <= around; j++)
                {
                    float angle = (j % around) / (float)around * Mathf.PI * 2f;
                    float x = Mathf.Sin(angle) * radius, z = Mathf.Cos(angle) * radius;
                    float ahead = Mathf.InverseLerp(0.55f, 0.97f, Mathf.Cos(angle));
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.4f, t));
                    float height = rise * (10f + 58f * Mathf.PerlinNoise(x * 0.008f + 50f, z * 0.008f + 50f) + 16f * Mathf.PerlinNoise(x * 0.03f + 9f, z * 0.03f + 9f));
                    height *= Mathf.Lerp(1f, 0.28f, ahead);
                    height -= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1f, t)) * 90f;
                    vertices[i * (around + 1) + j] = new Vector3(x, height - 0.3f, z);
                }
            }
            int n = 0;
            for (int i = 0; i < rings; i++)
            {
                for (int j = 0; j < around; j++)
                {
                    int a = i * (around + 1) + j, b = a + around + 1;
                    triangles[n++] = a; triangles[n++] = b; triangles[n++] = a + 1;
                    triangles[n++] = a + 1; triangles[n++] = b; triangles[n++] = b + 1;
                }
            }
            var mesh = new Mesh { name = "Hills", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string meshPath = SandboxDir + "/Hills.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            // The same material as the meadow floor, which paints by world position, so the grass
            // and the road carry on up the slopes without a seam.
            var material = AssetDatabase.LoadAssetAtPath<Material>(SandboxDir + "/Materials/Sandbox_Ground.mat");

            var hills = new GameObject("Hills (distant)", typeof(MeshFilter), typeof(MeshRenderer));
            hills.transform.SetParent(parent, false);
            hills.isStatic = true;
            hills.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = hills.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// The player's own body: copies the generated body mesh into a plain mesh the game can
        /// read, gives it the toon look, and adds the component that puts it on the rig at start.
        /// </summary>
        private static void BuildPlayerBody(Transform parent)
        {
            const string modelPath = ArtDir + "/Characters/chr_body_woken_starter.glb";
            Mesh imported = null;
            Texture texture = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Mesh mesh && imported == null)
                    imported = mesh;
                if (asset is Texture found && texture == null)
                    texture = found;
            }
            if (imported == null)
            {
                Debug.LogWarning("[Godtree] chr_body_woken_starter.glb is missing; the player keeps the rig's mannequin");
                return;
            }

            var copy = new Mesh { name = "PlayerBodySource" };
            copy.indexFormat = imported.indexFormat;
            copy.vertices = imported.vertices;
            copy.normals = imported.normals;
            copy.uv = imported.uv;
            var set = new System.Collections.Generic.List<Vector2>();
            for (int channel = 1; channel <= 4; channel++)
            {
                imported.GetUVs(channel, set);
                copy.SetUVs(channel, set);
            }
            copy.triangles = imported.triangles;
            copy.RecalculateBounds();
            string meshPath = SandboxDir + "/PlayerBodySource.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(copy, meshPath);

            Material material = MakeMaterial("Sandbox_PlayerBody", Color.white);
            material.SetTexture("_BaseMap", texture);
            // Outer faces only: looking down from inside the neck, the player must not see the inside of the tunic.
            material.SetFloat("_Cull", 2f);
            EditorUtility.SetDirty(material);

            var body = new GameObject("Player body").AddComponent<Godtree.Sandbox.PlayerBody>();
            body.transform.SetParent(parent, false);
            body.source = copy;
            body.material = material;
            Debug.Log($"[Godtree] player body source: {copy.vertexCount} vertices, bounds {copy.bounds.center} {copy.bounds.size}, texture {(texture != null ? texture.name : "none")}");
        }

        /// <summary>
        /// The models for the twelve things in the character menu, kept in the scene switched
        /// off, and the component that copies the worn ones onto the player's body.
        /// </summary>
        private static void BuildPlayerEquipment(Transform parent)
        {
            var items = new (string item, string model, Godtree.Sandbox.PlayerEquipment.Mount mount, float yaw)[]
            {
                ("Leather Cap", "prp_eq_cap_leather", Godtree.Sandbox.PlayerEquipment.Mount.Head, EquipmentYaw),
                ("Iron Helm", "prp_eq_helm_iron", Godtree.Sandbox.PlayerEquipment.Mount.Head, EquipmentYaw),
                ("Weaver's Hood", "prp_eq_hood_weaver", Godtree.Sandbox.PlayerEquipment.Mount.Head, EquipmentYaw),
                ("Padded Jack", "prp_eq_body_padded", Godtree.Sandbox.PlayerEquipment.Mount.Chest, EquipmentYaw),
                ("Chain Shirt", "prp_eq_body_iron", Godtree.Sandbox.PlayerEquipment.Mount.Chest, EquipmentYaw),
                ("Mote-silk Robe", "prp_eq_body_silk", Godtree.Sandbox.PlayerEquipment.Mount.Chest, EquipmentYaw),
                ("Iron Bracers", "prp_eq_bracer_iron", Godtree.Sandbox.PlayerEquipment.Mount.Forearms, 0f),
                ("Runner's Wraps", "prp_eq_wrap_runner", Godtree.Sandbox.PlayerEquipment.Mount.Forearms, 0f),
                ("Knot-rings", "prp_eq_ring_knot", Godtree.Sandbox.PlayerEquipment.Mount.Wrists, 0f),
                ("Husk Charm", "prp_eq_charm_husk", Godtree.Sandbox.PlayerEquipment.Mount.Belt, 0f),
                ("Thornback Tusk", "prp_eq_charm_tusk", Godtree.Sandbox.PlayerEquipment.Mount.Belt, 0f),
                ("Mote Lantern", "prp_eq_charm_lantern", Godtree.Sandbox.PlayerEquipment.Mount.Belt, 0f),
            };
            var holder = new GameObject("Player equipment");
            holder.transform.SetParent(parent, false);
            var equipment = holder.AddComponent<Godtree.Sandbox.PlayerEquipment>();
            var templates = new GameObject("Templates (switched off)");
            templates.transform.SetParent(holder.transform, false);
            int found = 0;
            foreach ((string item, string model, Godtree.Sandbox.PlayerEquipment.Mount mount, float yaw) in items)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtDir}/Props/{model}.glb");
                if (prefab == null)
                    continue;
                var template = (GameObject)PrefabUtility.InstantiatePrefab(prefab, templates.transform);
                template.name = model;
                template.SetActive(false);
                equipment.pieces.Add(new Godtree.Sandbox.PlayerEquipment.Piece { item = item, template = template, mount = mount, yaw = yaw });
                found++;
            }
            Debug.Log($"[Godtree] player equipment: {found} of {items.Length} models found");
        }

        // How head and chest pieces must be turned so their front faces the way the player faces.
        // Set by looking at the menu's mirror picture.
        private const float EquipmentYaw = 0f;

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
                material = new Material(Shader.Find("Godtree/Toon"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            if (material.shader.name != "Godtree/Toon")
                material.shader = Shader.Find("Godtree/Toon");
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
