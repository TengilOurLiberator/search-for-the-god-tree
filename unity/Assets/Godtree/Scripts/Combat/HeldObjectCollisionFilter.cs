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
                    Release(before);
                if (now != null)
                    Hold(now);
                _heldBy[hand] = now;
            }
        }

        private void Hold(Rigidbody body)
        {
            _holdCount.TryGetValue(body, out int count);
            _holdCount[body] = count + 1;
            if (count > 0)
                return;
            body.solverIterations = Mathf.Max(body.solverIterations, heldSolverIterations);
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

        private IEnumerator RestoreLater(Rigidbody body)
        {
            yield return new WaitForSeconds(restoreDelaySeconds);
            // Picked up again in the meantime: leave it ignored.
            if (body != null && !_holdCount.ContainsKey(body))
                SetIgnored(body, false);
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
