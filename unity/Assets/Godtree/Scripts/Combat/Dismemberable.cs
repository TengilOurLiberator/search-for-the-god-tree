using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godtree.Combat
{
    /// <summary>Something that is harmed by being told so, not only by being pushed (creatures that move by animation).</summary>
    public interface IDamageable
    {
        /// <param name="point">Where it landed, world space.</param>
        /// <param name="impulse">Direction and strength of the blow.</param>
        /// <param name="damage">Health to remove.</param>
        /// <param name="blade">True if the blow could cut a limb off.</param>
        void TakeHit(Vector3 point, Vector3 impulse, float damage, bool blade);
    }

    /// <summary>
    /// A creature whose model was cut into ten pieces when it was made
    /// (tools/pipeline/blender_rig_body.py --parts): head, torso, upper and lower arms, upper and
    /// lower legs, each closed with a cap. Severing a joint turns the pieces beyond it into one
    /// loose physical object, frozen in the pose they had, and hides them on the body. Nothing is
    /// cut at run time, so it is cheap, and the stump and the limb are both solid.
    /// (Decision 0016. Method from docs/research/tools-survey-2026-10-10.md.)
    /// </summary>
    public sealed class Dismemberable : MonoBehaviour
    {
        /// <summary>joint name, the pieces that come off there, and the bone at the joint.</summary>
        private static readonly (string joint, string[] parts, string bone)[] Joints =
        {
            ("neck", new[] { "head" }, "head"),
            ("shoulder_l", new[] { "arm_upper_l", "arm_lower_l" }, "upperarm_l"),
            ("elbow_l", new[] { "arm_lower_l" }, "lowerarm_l"),
            ("shoulder_r", new[] { "arm_upper_r", "arm_lower_r" }, "upperarm_r"),
            ("elbow_r", new[] { "arm_lower_r" }, "lowerarm_r"),
            ("hip_l", new[] { "leg_upper_l", "leg_lower_l" }, "thigh_l"),
            ("knee_l", new[] { "leg_lower_l" }, "calf_l"),
            ("hip_r", new[] { "leg_upper_r", "leg_lower_r" }, "thigh_r"),
            ("knee_r", new[] { "leg_lower_r" }, "calf_r"),
        };

        /// <summary>Raised when a joint is severed: the creature, the joint, the loose piece.</summary>
        public static event Action<Dismemberable, string, GameObject> Severed;

        private readonly Dictionary<string, SkinnedMeshRenderer> _parts = new Dictionary<string, SkinnedMeshRenderer>();
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly HashSet<string> _gone = new HashSet<string>();

        /// <summary>True if the model has the pieces this needs.</summary>
        public bool Ready => _parts.Count >= 6;

        public bool HasLost(string part) => _gone.Contains(part);
        public bool LostHead => _gone.Contains("head");
        public bool LostALeg => _gone.Contains("leg_lower_l") || _gone.Contains("leg_lower_r");
        public bool LostBothArms => _gone.Contains("arm_lower_l") && _gone.Contains("arm_lower_r");
        public int PiecesLost => _gone.Count;

        private void Awake()
        {
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.name.StartsWith("part_"))
                    _parts[renderer.name.Substring(5)] = renderer;
            foreach (Transform bone in GetComponentsInChildren<Transform>(true))
                if (!_bones.ContainsKey(bone.name))
                    _bones[bone.name] = bone;
        }

        public Vector3 JointPosition(string joint)
        {
            foreach ((string name, string[] _, string bone) in Joints)
                if (name == joint && _bones.TryGetValue(bone, out Transform at))
                    return at.position;
            return transform.position;
        }

        /// <summary>The joint nearest a point that still has something to lose, or null if none is within reach.</summary>
        public string NearestJoint(Vector3 worldPoint, float reach)
        {
            string best = null;
            float bestDistance = reach;
            foreach ((string joint, string[] parts, string bone) in Joints)
            {
                if (!_bones.TryGetValue(bone, out Transform at) || !StillOn(parts))
                    continue;
                float distance = Vector3.Distance(at.position, worldPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = joint;
                }
            }
            return best;
        }

        private bool StillOn(string[] parts)
        {
            foreach (string part in parts)
                if (_parts.ContainsKey(part) && !_gone.Contains(part))
                    return true;
            return false;
        }

        /// <summary>
        /// Cut a joint. The pieces beyond it leave the body as one loose object thrown by
        /// <paramref name="impulse"/>. Returns that object, or null if there was nothing left there.
        /// </summary>
        public GameObject Sever(string joint, Vector3 impulse)
        {
            foreach ((string name, string[] parts, string bone) in Joints)
            {
                if (name != joint)
                    continue;
                var loose = new GameObject($"Severed {joint} ({this.name})");
                Vector3 pivot = _bones.TryGetValue(bone, out Transform at) ? at.position : transform.position;
                loose.transform.position = pivot;
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool any = false;
                foreach (string part in parts)
                {
                    if (!_parts.TryGetValue(part, out SkinnedMeshRenderer renderer) || _gone.Contains(part))
                        continue;
                    // Freeze the piece in the pose it has this moment.
                    var frozen = new Mesh { name = part + " (severed)" };
                    renderer.BakeMesh(frozen, true);
                    var piece = new GameObject(part, typeof(MeshFilter), typeof(MeshRenderer));
                    piece.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    piece.transform.SetParent(loose.transform, true);
                    piece.GetComponent<MeshFilter>().sharedMesh = frozen;
                    piece.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                    foreach (Vector3 corner in Corners(frozen.bounds))
                    {
                        Vector3 local = loose.transform.InverseTransformPoint(piece.transform.TransformPoint(corner));
                        if (!any) bounds = new Bounds(local, Vector3.zero);
                        else bounds.Encapsulate(local);
                        any = true;
                    }
                    renderer.enabled = false;
                    _gone.Add(part);
                }
                if (!any)
                {
                    Destroy(loose);
                    return null;
                }
                var box = loose.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size * 0.85f, Vector3.one * 0.05f);
                var body = loose.AddComponent<Rigidbody>();
                body.mass = Mathf.Clamp(bounds.size.x * bounds.size.y * bounds.size.z * 400f, 1.5f, 12f);
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                // It must not be shoved by the body it has just left.
                foreach (Collider mine in GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(box, mine, true);
                body.AddForce(impulse + Vector3.up * 1.5f * body.mass, ForceMode.Impulse);
                body.AddTorque(UnityEngine.Random.insideUnitSphere * 2f * body.mass, ForceMode.Impulse);
                Destroy(loose, 40f);
                Severed?.Invoke(this, joint, loose);
                return loose;
            }
            return null;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }
    }
}
