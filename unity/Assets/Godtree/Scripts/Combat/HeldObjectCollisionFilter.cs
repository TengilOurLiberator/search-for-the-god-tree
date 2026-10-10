using System.Collections;
using System.Collections.Generic;
using BIMOS;
using UnityEngine;

namespace Godtree.Combat
{
    /// <summary>
    /// While a hand holds an object, that object does not collide with the player's own
    /// body. Without this a long weapon is constantly pushed around by the holder's arms,
    /// head and legs, and thrashes in the hand (measured in the first recorded play
    /// session: up to 160 degrees off the controller, spinning at thousands of degrees a
    /// second). Collisions come back a moment after release, so a dropped or thrown
    /// object does not snag on the body on its way out.
    /// One instance per scene.
    /// </summary>
    public sealed class HeldObjectCollisionFilter : MonoBehaviour
    {
        [SerializeField] private float restoreDelaySeconds = 0.35f;

        [Tooltip("Physics solver iterations given to a body while it is held; more keeps the grip joint stiff.")]
        [SerializeField] private int heldSolverIterations = 30;

        private Player _player;
        private Hand[] _hands = new Hand[0];
        private Collider[] _rigColliders = new Collider[0];
        private readonly Dictionary<Hand, Rigidbody> _heldBy = new Dictionary<Hand, Rigidbody>();
        private readonly Dictionary<Rigidbody, int> _holdCount = new Dictionary<Rigidbody, int>();
        private readonly Dictionary<Hand, ConfigurableJoint> _looseJoints = new Dictionary<Hand, ConfigurableJoint>();
        private readonly Dictionary<Hand, JointDrive> _originalTurnDrive = new Dictionary<Hand, JointDrive>();

        [Tooltip("How much harder the hand holds its angle while it carries something. The rig's own setting lets a long weapon trail 20 to 30 degrees behind the controller.")]
        [SerializeField] private float heldTurnStrength = 5f;

        private bool FindRig()
        {
            if (_player != null)
                return true;
            _player = FindFirstObjectByType<Player>();
            if (_player == null)
                return false;
            _hands = _player.GetComponentsInChildren<Hand>(true);
            _rigColliders = _player.GetComponentsInChildren<Collider>(true);
            return true;
        }

        private void FixedUpdate()
        {
            if (!FindRig())
                return;

            foreach (Hand hand in _hands)
            {
                Rigidbody now = hand.CurrentGrab != null ? hand.CurrentGrab.GetComponentInParent<Rigidbody>() : null;
                _heldBy.TryGetValue(hand, out Rigidbody before);
                if (now == before)
                    continue;

                if (before != null)
                {
                    if (_looseJoints.TryGetValue(hand, out ConfigurableJoint loose) && loose != null)
                        Destroy(loose);
                    _looseJoints.Remove(hand);
                    StiffenHand(hand, false);
                    Release(before);
                }
                if (now != null)
                {
                    bool second = _holdCount.ContainsKey(now);
                    Hold(now);
                    StiffenHand(hand, true);
                    if (second)
                        LoosenSecondHand(hand, now);
                }
                _heldBy[hand] = now;
            }
        }

        private void StiffenHand(Hand hand, bool holding)
        {
            ConfigurableJoint joint = hand.IsLeftHand ? _player.PhysicsRig.LeftHandJoint : _player.PhysicsRig.RightHandJoint;
            if (joint == null)
                return;
            if (holding)
            {
                if (!_originalTurnDrive.ContainsKey(hand))
                    _originalTurnDrive[hand] = joint.slerpDrive;
                JointDrive original = _originalTurnDrive[hand];
                joint.slerpDrive = new JointDrive
                {
                    positionSpring = original.positionSpring * heldTurnStrength,
                    positionDamper = original.positionDamper * Mathf.Sqrt(heldTurnStrength) * 1.5f,
                    maximumForce = original.maximumForce * heldTurnStrength
                };
            }
            else if (_originalTurnDrive.TryGetValue(hand, out JointDrive original))
            {
                joint.slerpDrive = original;
                _originalTurnDrive.Remove(hand);
            }
        }

        private void Hold(Rigidbody body)
        {
            _holdCount.TryGetValue(body, out int count);
            _holdCount[body] = count + 1;
            if (count > 0)
                return;
            body.solverIterations = Mathf.Max(body.solverIterations, heldSolverIterations);
            body.maxDepenetrationVelocity = 3f; // never let an overlap turn into a launch
            SetIgnored(body, true);
        }

        private void Release(Rigidbody body)
        {
            if (!_holdCount.TryGetValue(body, out int count))
                return;
            count--;
            if (count > 0)
            {
                _holdCount[body] = count;
                return;
            }
            _holdCount.Remove(body);
            StartCoroutine(RestoreLater(body));
        }

        /// <summary>
        /// Two rigid grips on one object fight each other whenever the two controllers turn
        /// differently (measured: the spear sat 105 degrees off the hands when held in both).
        /// The first hand keeps its rigid grip and decides the angle; the second hand's grip
        /// is replaced by one that holds position but lets the object turn in the palm.
        /// </summary>
        private void LoosenSecondHand(Hand hand, Rigidbody body)
        {
            FixedJoint rigid = hand.PhysicsHandTransform.GetComponent<FixedJoint>();
            if (rigid == null || rigid.connectedBody != body)
                return;
            Vector3 anchorWorld = hand.PalmTransform.position;
            Destroy(rigid);

            ConfigurableJoint loose = hand.PhysicsHandTransform.gameObject.AddComponent<ConfigurableJoint>();
            loose.connectedBody = body;
            loose.autoConfigureConnectedAnchor = false;
            loose.anchor = hand.PhysicsHandTransform.InverseTransformPoint(anchorWorld);
            loose.connectedAnchor = body.transform.InverseTransformPoint(anchorWorld);
            loose.xMotion = loose.yMotion = loose.zMotion = ConfigurableJointMotion.Locked;
            loose.angularXMotion = loose.angularYMotion = loose.angularZMotion = ConfigurableJointMotion.Free;
            loose.enableCollision = false;
            _looseJoints[hand] = loose;
        }

        private IEnumerator RestoreLater(Rigidbody body)
        {
            // Collisions come back only once the object is clear of the player. Switching them
            // on while it still overlaps a hand or arm makes the physics engine shove the two
            // apart violently (a hand was flung metres away in the second recorded session).
            float waited = 0f;
            yield return new WaitForSeconds(restoreDelaySeconds);
            while (body != null && !_holdCount.ContainsKey(body) && waited < 6f && OverlapsRig(body))
            {
                yield return new WaitForSeconds(0.1f);
                waited += 0.1f;
            }
            if (body != null && !_holdCount.ContainsKey(body))
                SetIgnored(body, false);
        }

        private bool OverlapsRig(Rigidbody body)
        {
            foreach (Collider objectCollider in body.GetComponentsInChildren<Collider>())
            {
                if (objectCollider.isTrigger)
                    continue;
                Bounds bounds = objectCollider.bounds;
                bounds.Expand(0.06f);
                foreach (Collider rigCollider in _rigColliders)
                {
                    if (rigCollider != null && !rigCollider.isTrigger && rigCollider.enabled && bounds.Intersects(rigCollider.bounds))
                        return true;
                }
            }
            return false;
        }

        private void SetIgnored(Rigidbody body, bool ignored)
        {
            if (body == null)
                return;
            foreach (Collider objectCollider in body.GetComponentsInChildren<Collider>())
            {
                if (objectCollider.isTrigger)
                    continue;
                foreach (Collider rigCollider in _rigColliders)
                {
                    if (rigCollider != null && rigCollider != objectCollider)
                        Physics.IgnoreCollision(objectCollider, rigCollider, ignored);
                }
            }
        }
    }
}
