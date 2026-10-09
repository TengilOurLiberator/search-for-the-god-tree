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

            // Ground, unless the base scene already has visible geometry at the origin.
            if (!Physics.Raycast(new Vector3(0f, 5f, 0f), Vector3.down, 10f) && Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length == 0)
            {
                GameObject floor = Primitive(PrimitiveType.Cube, "Ground", root.transform, new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), ground);
                floor.isStatic = true;
            }

            // A stone plinth holding the sword at hand height, one step in front of the spawn.
            GameObject plinth = Primitive(PrimitiveType.Cube, "Plinth", root.transform, new Vector3(0.45f, 0.45f, 0.9f), new Vector3(0.5f, 0.9f, 0.5f), stone);
            plinth.isStatic = true;

            BuildSword(root.transform, new Vector3(0.45f, 0.96f, 0.9f), steel, leather);
            BuildDummy(root.transform, new Vector3(-0.3f, 0f, 1.9f), wood, straw, steel);

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

            GameObject torso = Primitive(PrimitiveType.Cylinder, "Torso", dummy.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.36f, 0.28f, 0.30f), straw);
            Rigidbody torsoBody = Body(torso, 14f);
            Spring(torsoBody, postBody, new Vector3(0f, -1f, 0f), 900f, 35f);
            torso.AddComponent<ImpactFlash>();

            GameObject head = Primitive(PrimitiveType.Sphere, "Head", dummy.transform, new Vector3(0f, 1.60f, 0f), new Vector3(0.26f, 0.28f, 0.26f), straw);
            Rigidbody headBody = Body(head, 4f);
            Spring(headBody, torsoBody, new Vector3(0f, -0.5f, 0f), 220f, 10f);
            head.AddComponent<ImpactFlash>();

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
            }
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
