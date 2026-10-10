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
        }

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
