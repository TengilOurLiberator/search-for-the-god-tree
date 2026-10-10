using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>Makes a fire's light waver in brightness the way firelight does.</summary>
    [RequireComponent(typeof(Light))]
    public sealed class FireFlicker : MonoBehaviour
    {
        [SerializeField] private float baseIntensity = 2.2f;
        [SerializeField] private float waver = 0.5f;
        [SerializeField] private float speed = 7f;

        private Light _light;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(_seed, Time.time * speed) - 0.5f;
            _light.intensity = baseIntensity + noise * 2f * waver;
        }
    }
}
