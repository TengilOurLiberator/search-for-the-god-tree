using UnityEngine;

namespace Godtree.Combat
{
    /// <summary>
    /// Plays one of several impact sounds where something hits this object, louder for
    /// harder hits. Sound is combat feedback (canon/audio-bible.md): the ear confirms
    /// what the hands did. Placeholder for the material matrix: each object simply
    /// carries the clips for its own material.
    /// </summary>
    public sealed class ImpactSound : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;

        [Tooltip("Impulse (newton seconds) below which a contact is silent.")]
        [SerializeField] private float minImpulse = 0.8f;

        [Tooltip("Impulse that plays at full volume.")]
        [SerializeField] private float fullImpulse = 12f;

        [SerializeField] private float minSecondsBetweenSounds = 0.06f;

        private float _lastPlayed = -1f;

        public AudioClip[] Clips
        {
            get => clips;
            set => clips = value;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (clips == null || clips.Length == 0)
                return;

            float impulse = collision.impulse.magnitude;
            if (impulse < minImpulse || Time.time - _lastPlayed < minSecondsBetweenSounds)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            _lastPlayed = Time.time;
            float volume = Mathf.Lerp(0.15f, 1f, Mathf.InverseLerp(minImpulse, fullImpulse, impulse));
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            AudioSource.PlayClipAtPoint(clip, point, volume);
        }
    }
}
