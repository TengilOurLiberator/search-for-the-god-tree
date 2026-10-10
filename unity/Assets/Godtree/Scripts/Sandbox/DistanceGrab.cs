using BIMOS;
using Godtree.Combat;
using Godtree.Diagnostics;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Point at something, squeeze GRIP, and it flies to your hand and is grabbed there.
    /// Objects with a <see cref="GripPose"/> arrive in their proper hold.
    ///
    /// It only acts when the hand is empty and nothing is close enough to grab by touch,
    /// so reaching out and taking something still works as before. The object travels as a
    /// real rigid body: it can be blocked, and it has momentum when it arrives.
    /// </summary>
    public sealed class DistanceGrab : MonoBehaviour
    {
        [SerializeField] private float range = 10f;
        [SerializeField] private float aimRadius = 0.14f;
        [SerializeField] private float flySpeed = 13f;
        [SerializeField] private float arriveDistance = 0.14f;

        private Player _player;
        private Collider[] _rigColliders = new Collider[0];
        private readonly Hand[] _hands = new Hand[2];
        private readonly Transform[] _controllers = new Transform[2];
        private readonly bool[] _gripWas = new bool[2];
        private readonly int[] _pendingFrames = new int[2];
        private readonly Rigidbody[] _aimed = new Rigidbody[2], _flying = new Rigidbody[2];
        private readonly float[] _flyStarted = new float[2];
        private readonly GameObject[] _markers = new GameObject[2];
        private Material _markerMaterial;

        private bool FindRig()
        {
            if (_player != null)
                return true;
            _player = FindFirstObjectByType<Player>();
            if (_player == null)
                return false;
            foreach (Hand hand in _player.GetComponentsInChildren<Hand>(true))
                _hands[hand.IsLeftHand ? 0 : 1] = hand;
            if (_hands[0] == null || _hands[1] == null)
            {
                _player = null;
                return false;
            }
            _controllers[0] = _player.ControllerRig.LeftControllerTransform;
            _controllers[1] = _player.ControllerRig.RightControllerTransform;
            _rigColliders = _player.GetComponentsInChildren<Collider>(true);
            _markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _markerMaterial.SetColor("_BaseColor", new Color(1f, 0.85f, 0.42f));
            return true;
        }

        private void Update()
        {
            if (!FindRig())
                return;

            for (int i = 0; i < 2; i++)
            {
                Hand hand = _hands[i];
                bool grip = hand.HandInputReader.Grip >= 0.5f;
                bool busy = hand.CurrentGrab != null || _flying[i] != null || hand.HandInputReader.Trigger > 0.3f;

                _aimed[i] = busy ? null : FindAimed(i);
                ShowMarker(i, _aimed[i]);

                // Give the rig's own touch-grab the first chance; act two frames later if it took nothing.
                if (grip && !_gripWas[i] && !busy)
                    _pendingFrames[i] = 2;
                if (_pendingFrames[i] > 0 && --_pendingFrames[i] == 0 && grip && hand.CurrentGrab == null && _aimed[i] != null)
                    BeginFlight(i, _aimed[i]);

                if (_flying[i] != null && !grip)
                    EndFlight(i, false);

                _gripWas[i] = grip;
            }
        }

        private Rigidbody FindAimed(int i)
        {
            Transform controller = _controllers[i];
            RaycastHit[] hits = Physics.SphereCastAll(controller.position + controller.forward * 0.25f, aimRadius, controller.forward, range, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            // Of everything the wide beam touches, take the thing nearest the line the hand points
            // along, not the first thing touched: on a crowded table the first is often the
            // neighbour (the robot player pulled the wrong weapon three times in twenty-five).
            Rigidbody best = null;
            float bestDistance = float.MaxValue;
            Vector3 origin = controller.position, along = controller.forward;
            foreach (RaycastHit hit in hits)
            {
                Rigidbody body = hit.rigidbody;
                if (body == null || body.isKinematic)
                    continue;
                Vector3 nearest = hit.collider.ClosestPoint(origin + along * Vector3.Dot(hit.point - origin, along));
                // Long things lying side by side have shapes that overlap each other's middles, so
                // the beam is inside several at once; then the one whose middle is nearest the line wins.
                float offLine = Vector3.Cross(along, nearest - origin).magnitude
                                + 1.0f * Vector3.Cross(along, body.worldCenterOfMass - origin).magnitude + hit.distance * 0.002f;
                if (offLine >= bestDistance)
                    continue;
                if (body.GetComponentInParent<Player>() != null || body.GetComponentInChildren<Grab>() == null)
                    continue;
                if (body == _flying[0] || body == _flying[1] || IsHeld(body))
                    continue;
                best = body;
                bestDistance = offLine;
            }
            return best;
        }

        private bool IsHeld(Rigidbody body)
        {
            foreach (Hand hand in _hands)
                if (hand.CurrentGrab != null && hand.CurrentGrab.GetComponentInParent<Rigidbody>() == body)
                    return true;
            return false;
        }

        private void ShowMarker(int i, Rigidbody target)
        {
            if (target == null)
            {
                if (_markers[i] != null)
                    _markers[i].SetActive(false);
                return;
            }
            if (_markers[i] == null)
            {
                _markers[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _markers[i].name = "Distance grab marker";
                Destroy(_markers[i].GetComponent<Collider>());
                _markers[i].GetComponent<MeshRenderer>().sharedMaterial = _markerMaterial;
                _markers[i].transform.localScale = Vector3.one * 0.06f;
            }
            Bounds bounds = new Bounds(target.worldCenterOfMass, Vector3.zero);
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            _markers[i].SetActive(true);
            _markers[i].transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.12f + 0.03f * Mathf.Sin(Time.time * 6f), bounds.center.z);
        }

        private void BeginFlight(int i, Rigidbody body)
        {
            _flying[i] = body;
            _flyStarted[i] = Time.time;
            body.useGravity = false;
            SetRigIgnored(body, true);
            SessionRecorder.Note("distance_grab", body.name);
        }

        private void FixedUpdate()
        {
            if (_player == null)
                return;
            for (int i = 0; i < 2; i++)
            {
                Rigidbody body = _flying[i];
                if (body == null)
                    continue;
                Vector3 palm = _hands[i].PalmTransform.position;
                GripPose pose = body.GetComponent<GripPose>();
                Vector3 from = pose != null ? body.transform.TransformPoint(pose.localGripPoint) : body.worldCenterOfMass;
                Vector3 to = palm - from;
                if (to.magnitude < arriveDistance || Time.time - _flyStarted[i] > 1.5f)
                {
                    EndFlight(i, true);
                    continue;
                }
                body.linearVelocity = to.normalized * Mathf.Min(flySpeed, to.magnitude * 14f + 2f);
                body.angularVelocity *= 0.9f;
            }
        }

        private void EndFlight(int i, bool arrived)
        {
            Rigidbody body = _flying[i];
            _flying[i] = null;
            if (body == null)
                return;
            body.useGravity = true;

            Hand hand = _hands[i];
            if (!arrived || hand.CurrentGrab != null)
            {
                SetRigIgnored(body, false);
                return;
            }

            Grab grab = body.GetComponent<Grab>();
            if (grab == null)
            {
                // No pose: bring its middle to the palm and let the touch-grab take it as it lies.
                grab = body.GetComponentInChildren<Grab>();
                body.linearVelocity = Vector3.zero;
                body.position += hand.PalmTransform.position - body.worldCenterOfMass;
                body.transform.position = body.position;
            }
            if (grab != null && grab.enabled)
                grab.OnGrab(hand);
            else
                SetRigIgnored(body, false);
        }

        private void SetRigIgnored(Rigidbody body, bool ignored)
        {
            foreach (Collider mine in body.GetComponentsInChildren<Collider>())
            {
                if (mine.isTrigger)
                    continue;
                foreach (Collider rig in _rigColliders)
                    if (rig != null && rig != mine)
                        Physics.IgnoreCollision(mine, rig, ignored);
            }
        }
    }
}
