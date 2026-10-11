using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype: what the player has equipped in the character menu is shown on their
    /// body. Each piece is a rigid model fixed to one bone and sized from the skeleton itself:
    /// a helm on the head (seen in mirrors only, like the head), a breast piece on the chest,
    /// guards on both forearms or rings on both wrists, a charm at the belt. Nothing bends;
    /// a breast piece will clip a little when the player twists.
    /// </summary>
    public sealed class PlayerEquipment : MonoBehaviour
    {
        public enum Mount { Head, Chest, Forearms, Wrists, Belt }

        [Serializable]
        public sealed class Piece
        {
            [Tooltip("The item's name exactly as in PlayerStats.")]
            public string item;
            [Tooltip("A model in the scene to copy (kept inactive).")]
            public GameObject template;
            public Mount mount;
            [Tooltip("Turn about the model's own up axis, in degrees, if it came out facing the wrong way.")]
            public float yaw;
        }

        public List<Piece> pieces = new List<Piece>();

        /// <summary>How many models are on the body now.</summary>
        public int Shown => _shown.Count;

        private readonly List<GameObject> _shown = new List<GameObject>();
        private PlayerBody _body;
        private bool _subscribed;

        private void Update()
        {
            if (_body == null)
                _body = FindFirstObjectByType<PlayerBody>();
            if (_body == null || !_body.Applied || PlayerStats.Instance == null)
                return;
            if (!_subscribed)
            {
                PlayerStats.Instance.Changed += Refresh;
                _subscribed = true;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && PlayerStats.Instance != null)
                PlayerStats.Instance.Changed -= Refresh;
        }

        public void Refresh()
        {
            foreach (GameObject old in _shown)
                Destroy(old);
            _shown.Clear();
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                EquipmentItem worn = PlayerStats.Instance.Equipped(slot);
                if (worn == null)
                    continue;
                Piece piece = pieces.Find(p => p.item == worn.name);
                if (piece == null || piece.template == null)
                    continue;
                try
                {
                    Put(piece);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[Godtree] could not show {worn.name} on the body: {error.Message}");
                }
            }
        }

        private GameObject Copy(Piece piece, Transform parent, int layer)
        {
            GameObject copy = Instantiate(piece.template, parent);
            copy.name = piece.item;
            copy.SetActive(true);
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>())
                Destroy(collider);
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
            _shown.Add(copy);
            return copy;
        }

        /// <summary>The model's own box, in its root's space, at scale one.</summary>
        private static Bounds ModelBox(GameObject model)
        {
            Matrix4x4 toRoot = model.transform.worldToLocalMatrix;
            var box = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 m = toRoot * filter.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (!any) box = new Bounds(p, Vector3.zero);
                    else box.Encapsulate(p);
                    any = true;
                }
            }
            return box;
        }

        /// <summary>The model's box in world space as it now stands.</summary>
        private static Bounds WorldBox(GameObject model)
        {
            var box = new Bounds(model.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) box = renderer.bounds;
                else box.Encapsulate(renderer.bounds);
                any = true;
            }
            return box;
        }

        /// <summary>
        /// The angle, in degrees about the model's up axis, from its X axis to the level direction
        /// in which it is widest (found from where its points lie, not from its box).
        /// </summary>
        private static float WidestLevelAngle(GameObject model)
        {
            double sxx = 0, szz = 0, sxz = 0, mx = 0, mz = 0;
            int n = 0;
            var points = new List<Vector3>();
            Matrix4x4 toRoot = model.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Matrix4x4 m = toRoot * filter.transform.localToWorldMatrix;
                foreach (Vector3 v in filter.sharedMesh.vertices)
                    points.Add(m.MultiplyPoint3x4(v));
            }
            foreach (Vector3 p in points) { mx += p.x; mz += p.z; n++; }
            if (n < 3)
                return 0f;
            mx /= n; mz /= n;
            foreach (Vector3 p in points)
            {
                double x = p.x - mx, z = p.z - mz;
                sxx += x * x; szz += z * z; sxz += x * z;
            }
            return (float)(0.5 * System.Math.Atan2(2 * sxz, sxx - szz) * 180.0 / System.Math.PI);
        }

        private static int LongAxis(Vector3 size) => size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
        private static int ThinAxis(Vector3 size) => size.x <= size.y && size.x <= size.z ? 0 : (size.y <= size.z ? 1 : 2);
        private static Vector3 Unit(int axis) => axis == 0 ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);

        private void Put(Piece piece)
        {
            Transform hips = _body.Bone("Hips"), neck = _body.Bone("Neck");
            Transform leftShoulder = _body.Bone("LeftUpperArm"), rightShoulder = _body.Bone("RightUpperArm");
            // Directions of the body as it stands now.
            Vector3 up = (neck.position - hips.position).normalized;
            Vector3 right = Vector3.ProjectOnPlane(rightShoulder.position - leftShoulder.position, up).normalized;
            Vector3 forward = Vector3.Cross(right, up);
            float shoulders = Vector3.Distance(leftShoulder.position, rightShoulder.position);

            switch (piece.mount)
            {
                case Mount.Forearms:
                case Mount.Wrists:
                    foreach (string side in new[] { "Left", "Right" })
                    {
                        Transform lower = _body.Bone(side + "LowerArm"), hand = _body.Bone(side + "Hand");
                        GameObject copy = Copy(piece, lower, 0);
                        Bounds box = ModelBox(copy);
                        Vector3 along = hand.position - lower.position;
                        float length = along.magnitude;
                        if (piece.mount == Mount.Forearms)
                        {
                            // A sleeve: its long way lies along the forearm, covering the outer two thirds.
                            int longAxis = LongAxis(box.size);
                            float across = Mathf.Max(box.size[(longAxis + 1) % 3], box.size[(longAxis + 2) % 3]);
                            float fit = length * 0.62f / box.size[longAxis];
                            float girth = Mathf.Max(fit, shoulders * 0.26f / across); // wide enough to go round the arm
                            Vector3 scale = Vector3.one * girth;
                            scale[longAxis] = fit;
                            copy.transform.rotation = Quaternion.FromToRotation(Unit(longAxis), along.normalized);
                            copy.transform.localScale = Vector3.Scale(scale, InverseLossy(lower));
                            copy.transform.position = lower.position + along * 0.60f - copy.transform.TransformVector(box.center);
                        }
                        else
                        {
                            // A ring: its thin way lies along the forearm, at the wrist.
                            int thin = ThinAxis(box.size);
                            float across = Mathf.Max(box.size[(thin + 1) % 3], box.size[(thin + 2) % 3]);
                            copy.transform.rotation = Quaternion.FromToRotation(Unit(thin), along.normalized);
                            copy.transform.localScale = InverseLossy(lower) * (shoulders * 0.24f / across);
                            copy.transform.position = lower.position + along * 0.93f - copy.transform.TransformVector(box.center);
                        }
                    }
                    break;

                case Mount.Chest:
                {
                    Transform chest = _body.Bone("UpperChest") ?? _body.Bone("Chest");
                    GameObject copy = Copy(piece, chest, 0);
                    // Generated models do not arrive facing a known way. A breast piece is widest from
                    // shoulder to shoulder, so its widest level direction is turned to lie across the body.
                    float turn = WidestLevelAngle(copy) + piece.yaw;
                    copy.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, -turn, 0f);
                    copy.transform.localScale = InverseLossy(chest);
                    Bounds box = WorldBox(copy);
                    float span = Mathf.Abs(Vector3.Dot(box.size, new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z))));
                    // Shoulder caps and all, a little over one and a half times the distance between the shoulder joints.
                    float fit = shoulders * 1.62f / Mathf.Max(span, 1e-4f);
                    copy.transform.localScale = InverseLossy(chest) * fit;
                    box = WorldBox(copy);
                    Vector3 topCentre = box.center + up * Vector3.Dot(box.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)));
                    copy.transform.position += neck.position + up * (shoulders * 0.10f) + forward * (shoulders * 0.03f) - topCentre;
                    break;
                }
                case Mount.Head:
                {
                    if (_body.MirrorHead == null)
                        return;
                    GameObject copy = Copy(piece, _body.MirrorHead, PlayerBody.MirrorOnlyLayer);
                    Bounds box = ModelBox(copy);
                    Bounds head = _body.HeadBox; // in the mirror head's own space
                    copy.transform.localRotation = Quaternion.Euler(0f, piece.yaw, 0f);
                    float headWidth = Mathf.Max(head.size.x, head.size.z);
                    float fit = headWidth * 1.34f / Mathf.Max(box.size.x, box.size.z); // roomy enough that hair does not poke through
                    copy.transform.localScale = Vector3.one * fit;
                    // Crown to crown, a finger's width proud of the hair.
                    Vector3 crown = new Vector3(head.center.x, head.max.y + headWidth * 0.02f, head.center.z);
                    Vector3 modelCrown = box.center + Vector3.up * box.extents.y;
                    copy.transform.localPosition = crown - copy.transform.localRotation * (modelCrown * fit);
                    break;
                }

                case Mount.Belt:
                {
                    GameObject copy = Copy(piece, hips, 0);
                    Bounds box = ModelBox(copy);
                    copy.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0f, piece.yaw, 0f);
                    float fit = shoulders * 0.36f / Mathf.Max(box.size.x, box.size.y, box.size.z);
                    copy.transform.localScale = InverseLossy(hips) * fit;
                    Vector3 top = box.center + Vector3.up * box.extents.y;
                    copy.transform.position = hips.position - right * (shoulders * 0.42f) + forward * (shoulders * 0.30f) + up * (shoulders * 0.10f) - copy.transform.TransformVector(top);
                    break;
                }
            }
        }

        /// <summary>The scale a child needs under this bone to come out at world scale one.</summary>
        private static Vector3 InverseLossy(Transform bone)
        {
            Vector3 s = bone.lossyScale;
            return new Vector3(1f / Mathf.Max(1e-5f, s.x), 1f / Mathf.Max(1e-5f, s.y), 1f / Mathf.Max(1e-5f, s.z));
        }
    }
}
