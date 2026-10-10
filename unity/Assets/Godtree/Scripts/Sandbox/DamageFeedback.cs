using Godtree.Combat;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype: turns real hits into visible damage numbers and gives the training
    /// dummy a health total, so the effect of strength, focus and different weapons can be
    /// read at a glance. Damage = impulse of the actual collision x 2 x the stat that applies.
    /// When the dummy's health runs out it is "defeated" and resets a moment later.
    /// </summary>
    public sealed class DamageFeedback : MonoBehaviour
    {
        public static DamageFeedback Instance { get; private set; }

        [SerializeField] private float dummyMaxHealth = 150f;

        [Tooltip("Additive glow material for hit sparks. No sparks if empty.")]
        public Material sparkMaterial;

        private ParticleSystem _sparks;

        private float _dummyHealth;
        private float _resetAt = -1f;
        private TextMesh _healthText;
        private Transform _dummyHead;
        private Font _font;

        private void Awake()
        {
            Instance = this;
            _dummyHealth = dummyMaxHealth;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ImpactFlash.HitAt += OnHit;
        }

        private void OnDestroy()
        {
            ImpactFlash.HitAt -= OnHit;
        }

        private void OnHit(GameObject target, GameObject striker, Vector3 point, float impulse)
        {
            bool spell = striker != null && striker.name.StartsWith("Mote");
            float damage = impulse * 2f * (spell ? PlayerStats.FocusNow : PlayerStats.StrengthNow);
            Report(target, point, damage, spell);
            Sparks(point, Mathf.InverseLerp(2f, 30f, impulse), spell ? new Color(1f, 0.85f, 0.42f) : new Color(1f, 0.93f, 0.78f));
        }

        /// <summary>A burst of sparks and a brief flash where a hit landed; strength 0 to 1.</summary>
        public void Sparks(Vector3 point, float strength, Color colour)
        {
            if (sparkMaterial == null)
                return;
            if (_sparks == null)
            {
                var holder = new GameObject("Hit sparks");
                holder.transform.SetParent(transform, false);
                _sparks = holder.AddComponent<ParticleSystem>();
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = _sparks.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
                main.gravityModifier = 1.1f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 400;
                ParticleSystem.EmissionModule emission = _sparks.emission;
                emission.enabled = false;
                ParticleSystem.ShapeModule shape = _sparks.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.02f;
                ParticleSystem.SizeOverLifetimeModule shrink = _sparks.sizeOverLifetime;
                shrink.enabled = true;
                shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
                var sparkRenderer = holder.GetComponent<ParticleSystemRenderer>();
                sparkRenderer.sharedMaterial = sparkMaterial;
                sparkRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            strength = Mathf.Clamp01(strength);
            var burst = new ParticleSystem.EmitParams { position = point, startColor = colour, applyShapeToPosition = true };
            _sparks.Emit(burst, Mathf.RoundToInt(Mathf.Lerp(5f, 26f, strength)));
            // One large, very short glow: the flash of the blow itself.
            var flash = new ParticleSystem.EmitParams
            {
                position = point, startColor = colour, velocity = Vector3.zero,
                startSize = Mathf.Lerp(0.12f, 0.45f, strength), startLifetime = 0.09f
            };
            _sparks.Emit(flash, 1);
        }

        /// <summary>How many spark particles are alive; for tests.</summary>
        public int LiveSparks => _sparks != null ? _sparks.particleCount : 0;

        /// <summary>For effects that push with a force and so cause no collision of their own.</summary>
        public void Report(GameObject target, Vector3 point, float damage, bool spell)
        {
            if (damage < 1f)
                return;
            Popup(point, Mathf.RoundToInt(damage).ToString(), spell ? new Color(1f, 0.85f, 0.42f) : Color.white, Mathf.Lerp(0.05f, 0.11f, Mathf.InverseLerp(5f, 80f, damage)));

            if (target != null && target.GetComponentInParent<Transform>() != null && IsDummy(target.transform))
            {
                if (_resetAt > 0f)
                    return;
                _dummyHealth = Mathf.Max(0f, _dummyHealth - damage);
                if (_dummyHealth <= 0f)
                {
                    _resetAt = Time.time + 2.5f;
                    Popup(point + Vector3.up * 0.3f, "DEFEATED", new Color(1f, 0.3f, 0.37f), 0.12f);
                }
            }
        }

        private static bool IsDummy(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.name.StartsWith("enm_dummy_training"))
                    return true;
            return false;
        }

        private void Update()
        {
            if (_dummyHead == null)
            {
                GameObject head = GameObject.Find("Head");
                if (head != null && IsDummy(head.transform))
                    _dummyHead = head.transform;
                else
                    return;
            }
            if (_healthText == null)
                _healthText = MakeText("Dummy health", 0.06f, Color.white);
            if (_resetAt > 0f && Time.time >= _resetAt)
            {
                _resetAt = -1f;
                _dummyHealth = dummyMaxHealth;
            }
            _healthText.text = _resetAt > 0f ? "DEFEATED" : Mathf.CeilToInt(_dummyHealth) + " / " + Mathf.RoundToInt(dummyMaxHealth);
            _healthText.color = _resetAt > 0f ? new Color(1f, 0.3f, 0.37f) : Color.Lerp(new Color(1f, 0.3f, 0.37f), Color.white, _dummyHealth / dummyMaxHealth);
            _healthText.transform.position = _dummyHead.position + Vector3.up * 0.45f;
            FaceCamera(_healthText.transform);
        }

        private void Popup(Vector3 at, string text, Color color, float size)
        {
            TextMesh mesh = MakeText("Damage number", size, color);
            mesh.text = text;
            mesh.transform.position = at + Random.insideUnitSphere * 0.05f;
            FaceCamera(mesh.transform);
            mesh.gameObject.AddComponent<FloatUp>();
        }

        private TextMesh MakeText(string objectName, float size, Color color)
        {
            var go = new GameObject(objectName);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            mesh.fontSize = 64;
            mesh.characterSize = size * 0.16f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return mesh;
        }

        private static void FaceCamera(Transform t)
        {
            Camera cam = Camera.main;
            if (cam != null)
                t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);
        }

        private sealed class FloatUp : MonoBehaviour
        {
            private float _age;
            private TextMesh _mesh;

            private void Awake() { _mesh = GetComponent<TextMesh>(); }

            private void Update()
            {
                _age += Time.deltaTime;
                transform.position += Vector3.up * (0.5f * Time.deltaTime);
                FaceCamera(transform);
                Color c = _mesh.color;
                c.a = Mathf.Clamp01(1.2f - _age);
                _mesh.color = c;
                if (_age > 1.2f)
                    Destroy(gameObject);
            }
        }
    }
}
