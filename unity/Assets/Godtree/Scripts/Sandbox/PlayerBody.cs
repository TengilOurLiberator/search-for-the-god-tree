using System.Collections.Generic;
using BIMOS;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype: gives the player rig a generated character's body in place of its grey
    /// mannequin. The body file holds a mesh in a T-pose with its binding written into four extra
    /// texture-coordinate sets (tools/pipeline/blender_rig_body.py) and no skeleton. Here the
    /// skinned mesh is rebuilt against the rig's own skeleton, whatever units and axes the rig's
    /// mesh uses, and swapped onto the rig's renderer. The rig's animation is untouched.
    /// If anything does not line up, the mannequin is left in place.
    /// </summary>
    public sealed class PlayerBody : MonoBehaviour
    {
        [Tooltip("The body mesh: T-pose, metres, positions relative to the hip joint, binding in UV sets 2 to 5.")]
        public Mesh source;

        public Material material;

        [Tooltip("Hip joint to head joint, in metres, in the skeleton the body was bound to. Sets the scale.")]
        public float hipToHeadMetres = 0.648f;

        // The same list, in the same order, as BONES in tools/pipeline/blender_rig_body.py.
        private static readonly string[] BoneNames =
        {
            "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
        };

        /// <summary>True once the generated body is on the rig.</summary>
        public bool Applied { get; private set; }

        /// <summary>Why the body was not applied, if it was not.</summary>
        public string Problem { get; private set; }

        private bool _tried;

        // The rig hides the head (a first-person player must not see the inside of their own
        // face), so a mirror shows a headless body. A separate copy of the head is drawn on a
        // layer of its own that only the mirror camera looks at.
        public const int MirrorOnlyLayer = 29;
        private Transform _headBone, _mirrorHead;
        private float _nextCameraSweep;

        private void LateUpdate()
        {
            if (_mirrorHead == null || _headBone == null)
                return;
            // The rig hides the head by shrinking the neck bone to nothing, which also collapses
            // the head bone onto the neck. So the copy is placed from the neck's position and
            // turn (both still valid), the head's own offset and turn, and the chest's size.
            Transform neck = _headBone.parent;
            Vector3 size = neck.parent != null ? neck.parent.lossyScale : Vector3.one;
            _mirrorHead.SetPositionAndRotation(
                neck.position + neck.rotation * Vector3.Scale(size, _headBone.localPosition),
                neck.rotation * _headBone.localRotation);
            _mirrorHead.localScale = size;
            // Every camera but the mirror's must leave the head copy out; cameras come and go.
            if (Time.unscaledTime >= _nextCameraSweep)
            {
                _nextCameraSweep = Time.unscaledTime + 1f;
                foreach (Camera cam in Camera.allCameras)
                    if (cam.name != "Mirror camera")
                        cam.cullingMask &= ~(1 << MirrorOnlyLayer);
            }
        }

        private void Update()
        {
            if (_tried || source == null || material == null)
                return;
            Player player = FindFirstObjectByType<Player>();
            if (player == null)
                return;
            _tried = true;
            foreach (SkinnedMeshRenderer renderer in player.transform.root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (System.Array.Exists(renderer.bones, bone => bone != null && bone.name == "Hips"))
                {
                    Problem = Apply(renderer);
                    Applied = Problem == null;
                    if (!Applied)
                        Debug.LogWarning("[Godtree] Player body not applied, the mannequin stays: " + Problem);
                    return;
                }
            }
            Problem = "the rig has no skinned body with a Hips bone";
            Debug.LogWarning("[Godtree] Player body not applied: " + Problem);
        }

        private string Apply(SkinnedMeshRenderer renderer)
        {
            Mesh original = renderer.sharedMesh;
            Matrix4x4[] bind = original.bindposes;
            Transform[] bones = renderer.bones;
            if (bind.Length != bones.Length)
                return "the rig's mesh and bone list disagree";

            // Where each bone sits at rest, in the rig mesh's own space.
            var rigIndex = new Dictionary<string, int>();
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null)
                    rigIndex[bones[i].name] = i;
            var boneMap = new int[BoneNames.Length];
            for (int i = 0; i < BoneNames.Length; i++)
            {
                if (!rigIndex.TryGetValue(BoneNames[i], out boneMap[i]))
                    return "the rig has no bone called " + BoneNames[i];
            }
            Vector3 Rest(string bone) => bind[rigIndex[bone]].inverse.GetColumn(3);

            // The rig mesh's directions and size, from its own skeleton.
            Vector3 hips = Rest("Hips");
            Vector3 up = (Rest("Head") - hips).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Rest("RightHand") - Rest("LeftHand"), up).normalized;
            Vector3 forward = Vector3.Cross(right, up);
            if (Vector3.Dot(Rest("LeftToes") - Rest("LeftFoot"), forward) < 0f)
                return "the rig's mesh space is mirrored";
            float scale = (Rest("Head") - hips).magnitude / hipToHeadMetres;

            // The binding, read back from the extra texture-coordinate sets.
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            var sets = new List<Vector2>[4];
            for (int s = 0; s < 4; s++)
            {
                sets[s] = new List<Vector2>();
                source.GetUVs(s + 1, sets[s]);
                if (sets[s].Count != vertices.Length)
                    return "the body mesh has no binding in texture-coordinate set " + (s + 2);
            }
            // Some importers store the second coordinate upside down; weights must add up to one.
            double total = 0;
            for (int v = 0; v < vertices.Length; v++)
                for (int s = 0; s < 4; s++)
                    total += sets[s][v].y;
            bool flipped = Mathf.Abs((float)(total / vertices.Length) - 1f) > 0.05f;
            if (flipped && Mathf.Abs((float)((vertices.Length * 4 - total) / vertices.Length) - 1f) > 0.05f)
                return "the body mesh's weights do not add up to one either way up";

            var weights = new BoneWeight[vertices.Length];
            Vector3 leftHand = Vector3.zero, rightHand = Vector3.zero;
            int leftCount = 0, rightCount = 0;
            int leftHandNumber = System.Array.IndexOf(BoneNames, "LeftHand"), rightHandNumber = System.Array.IndexOf(BoneNames, "RightHand");
            for (int v = 0; v < vertices.Length; v++)
            {
                var number = new int[4];
                var weight = new float[4];
                float sum = 0f;
                for (int s = 0; s < 4; s++)
                {
                    number[s] = Mathf.Clamp(Mathf.FloorToInt(sets[s][v].x * 64f), 0, BoneNames.Length - 1);
                    weight[s] = Mathf.Clamp01(flipped ? 1f - sets[s][v].y : sets[s][v].y);
                    sum += weight[s];
                }
                if (sum < 0.0001f) { weight[0] = 1f; sum = 1f; }
                weights[v] = new BoneWeight
                {
                    boneIndex0 = boneMap[number[0]], weight0 = weight[0] / sum,
                    boneIndex1 = boneMap[number[1]], weight1 = weight[1] / sum,
                    boneIndex2 = boneMap[number[2]], weight2 = weight[2] / sum,
                    boneIndex3 = boneMap[number[3]], weight3 = weight[3] / sum,
                };
                if (number[0] == leftHandNumber) { leftHand += vertices[v]; leftCount++; }
                if (number[0] == rightHandNumber) { rightHand += vertices[v]; rightCount++; }
            }
            if (leftCount == 0 || rightCount == 0)
                return "the body mesh has no hands bound";

            // The body file's directions: up is up, right is from its left hand to its right hand.
            Vector3 sourceUp = Vector3.up;
            Vector3 sourceRight = Vector3.ProjectOnPlane(rightHand / rightCount - leftHand / leftCount, sourceUp).normalized;
            Vector3 sourceForward = Vector3.Cross(sourceRight, sourceUp);

            var placed = new Vector3[vertices.Length];
            var turned = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 p = vertices[v];
                placed[v] = hips + scale * (right * Vector3.Dot(p, sourceRight) + up * Vector3.Dot(p, sourceUp) + forward * Vector3.Dot(p, sourceForward));
                Vector3 n = v < normals.Length ? normals[v] : Vector3.up;
                turned[v] = right * Vector3.Dot(n, sourceRight) + up * Vector3.Dot(n, sourceUp) + forward * Vector3.Dot(n, sourceForward);
            }

            var mesh = new Mesh { name = source.name + " (on rig)" };
            mesh.indexFormat = vertices.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = placed;
            mesh.normals = turned;
            mesh.uv = source.uv;
            mesh.triangles = source.triangles;
            mesh.boneWeights = weights;
            mesh.bindposes = bind;
            mesh.RecalculateBounds();

            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { material };
            renderer.updateWhenOffscreen = true;

            // The head copy for mirrors: the triangles that belong to the head, in the head bone's own space.
            int headNumber = System.Array.IndexOf(BoneNames, "Head");
            Matrix4x4 toHead = bind[boneMap[headNumber]];
            var isHead = new bool[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                float share = 0f;
                for (int s = 0; s < 4; s++)
                    if (Mathf.Clamp(Mathf.FloorToInt(sets[s][v].x * 64f), 0, BoneNames.Length - 1) == headNumber)
                        share += flipped ? 1f - sets[s][v].y : sets[s][v].y;
                isHead[v] = share >= 0.5f;
            }
            int[] all = source.triangles;
            var headTriangles = new List<int>();
            for (int i = 0; i < all.Length; i += 3)
                if (isHead[all[i]] && isHead[all[i + 1]] && isHead[all[i + 2]])
                    headTriangles.AddRange(new[] { all[i], all[i + 1], all[i + 2] });
            if (headTriangles.Count > 0)
            {
                var headVertices = new Vector3[vertices.Length];
                var headNormals = new Vector3[vertices.Length];
                for (int v = 0; v < vertices.Length; v++)
                {
                    headVertices[v] = isHead[v] ? toHead.MultiplyPoint3x4(placed[v]) : Vector3.zero;
                    headNormals[v] = toHead.MultiplyVector(turned[v]).normalized;
                }
                var headMesh = new Mesh { name = source.name + " (head, for mirrors)", indexFormat = mesh.indexFormat };
                headMesh.vertices = headVertices;
                headMesh.normals = headNormals;
                headMesh.uv = source.uv;
                headMesh.triangles = headTriangles.ToArray();
                headMesh.RecalculateBounds();
                var headObject = new GameObject("Player head (mirror only)", typeof(MeshFilter), typeof(MeshRenderer));
                headObject.layer = MirrorOnlyLayer;
                headObject.GetComponent<MeshFilter>().sharedMesh = headMesh;
                headObject.GetComponent<MeshRenderer>().sharedMaterial = material;
                _mirrorHead = headObject.transform;
                _headBone = bones[boneMap[headNumber]];
            }            Debug.Log($"[Godtree] Player body applied: {vertices.Length} vertices, scale {scale:F3}, weights {(flipped ? "upside down in the file" : "as written")}, head triangles {headTriangles.Count / 3}, rig head bone scale {bones[boneMap[headNumber]].localScale}");
            return null;
        }
    }
}
