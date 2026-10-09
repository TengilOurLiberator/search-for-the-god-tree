using UnityEngine;

namespace Godtree.Combat
{
    /// <summary>
    /// Flashes this body's renderers when something hits it, brighter for harder hits.
    /// Placeholder feedback for the VR sandbox: it shows that a hit came from a real
    /// collision and how hard it was. Not a damage system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ImpactFlash : MonoBehaviour
    {
        [Tooltip("Impulse (newton seconds) below which a contact is ignored.")]
        [SerializeField] private float minImpulse = 1.5f;

        [Tooltip("Impulse that produces a full-strength flash.")]
        [SerializeField] private float fullImpulse = 12f;

        [SerializeField] private Color flashColor = new Color(1f, 0.30f, 0.37f);
        [SerializeField] private float fadeSeconds = 0.35f;
        [SerializeField] private bool logHits = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _block;
        private float _strength;

        /// <summary>Impulse of the most recent hit that passed the threshold.</summary>
        public float LastImpulse { get; private set; }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i].sharedMaterial;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
            _block = new MaterialPropertyBlock();
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impulse = collision.impulse.magnitude;
            if (impulse < minImpulse)
                return;

            LastImpulse = impulse;
            _strength = Mathf.Max(_strength, Mathf.InverseLerp(minImpulse, fullImpulse, impulse));

            if (logHits)
                Debug.Log($"[Godtree] {name} hit by {collision.collider.name}: impulse {impulse:F1} N s");
        }

        private void Update()
        {
            if (_strength <= 0f)
                return;

            _strength = Mathf.Max(0f, _strength - Time.deltaTime / fadeSeconds);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, Color.Lerp(_baseColors[i], flashColor, _strength));
                _renderers[i].SetPropertyBlock(_block);
            }
        }
    }
}
