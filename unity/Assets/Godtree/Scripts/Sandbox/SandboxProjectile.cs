using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// A physical projectile for the sandbox prototypes: an arrow that turns to face the
    /// way it flies and sticks where it lands, or a mote bolt that bursts on impact.
    /// Damage is whatever the real collision delivers; nothing is scripted.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SandboxProjectile : MonoBehaviour
    {
        public bool sticks;
        public float lifeSeconds = 12f;
        public AudioClip[] hitClips;

        private Rigidbody _body;
        private bool _landed;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            Destroy(gameObject, lifeSeconds);
        }

        private void FixedUpdate()
        {
            if (sticks && !_landed && _body.linearVelocity.sqrMagnitude > 1f)
                _body.MoveRotation(Quaternion.LookRotation(_body.linearVelocity));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_landed)
                return;
            _landed = true;

            if (hitClips != null && hitClips.Length > 0)
            {
                AudioClip clip = hitClips[Random.Range(0, hitClips.Length)];
                if (clip != null)
                    AudioSource.PlayClipAtPoint(clip, transform.position, Mathf.Clamp01(collision.relativeVelocity.magnitude / 20f));
            }

            if (sticks)
            {
                // The collision has already delivered its impulse; now the arrow stays put.
                _body.isKinematic = true;
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    c.enabled = false;
                transform.position += transform.forward * 0.05f;
                transform.SetParent(collision.transform, true);
            }
            else
            {
                Destroy(gameObject, 0.05f);
            }
        }
    }
}
