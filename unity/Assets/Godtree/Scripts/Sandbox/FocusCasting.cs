using System.Collections;
using System.Collections.Generic;
using BIMOS;
using Godtree.Combat;
using Godtree.Diagnostics;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype of casting through staffs and wands (decision 0016).
    ///
    /// Hold a staff or wand, hold TRIGGER on that hand, let go. The light at the tip grows and
    /// changes as the hold passes each spell the focus knows:
    ///   a tap        the element's first spell (every focus)
    ///   0.7 seconds  its second spell (tier 2 and 3)
    ///   1.7 seconds  its third spell (tier 3)
    /// Ember throws fire that bursts; Frost throws ice that slows and holds things; Storm strikes
    /// at once and jumps between targets. Higher tiers make each spell bigger. Everything acts
    /// through physics: spells push real bodies, and damage follows the push.
    /// </summary>
    public sealed class FocusCasting : MonoBehaviour
    {
        [Tooltip("Additive glow material for spell lights and bursts.")]
        public Material glowMaterial;

        [Tooltip("Additive glow material set up for trails and bolts of lightning.")]
        public Material trailMaterial;

        public AudioClip[] chargeClips, castClips, hitClips;

        /// <summary>Name of the last spell cast, for tests and the session record.</summary>
        public static string LastSpell { get; private set; }

        /// <summary>How many spells have been cast since the scene loaded.</summary>
        public static int Casts { get; private set; }

        /// <summary>How many bodies spells have pushed, burned, frozen or struck since the scene loaded.</summary>
        public static int BodiesAffected { get; private set; }

        private Player _player;
        private Collider[] _rigColliders = new Collider[0];
        private readonly Hand[] _hands = new Hand[2];
        private readonly bool[] _charging = new bool[2];
        private readonly float[] _started = new float[2];
        private readonly int[] _shownSpell = new int[2];
        private readonly GameObject[] _tipLight = new GameObject[2];
        private Material _solid;

        private void Awake()
        {
            LastSpell = null;
            Casts = 0;
            BodiesAffected = 0;
        }

        private bool FindRig()
        {
            if (_player != null)
                return true;
            _player = FindFirstObjectByType<Player>();
            if (_player == null)
                return false;
            foreach (Hand hand in _player.transform.root.GetComponentsInChildren<Hand>(true))
                _hands[hand.IsLeftHand ? 0 : 1] = hand;
            if (_hands[0] == null || _hands[1] == null)
            {
                _player = null;
                return false;
            }
            _rigColliders = _player.transform.root.GetComponentsInChildren<Collider>(true);
            _solid = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            return true;
        }

        private static SpellFocus FocusIn(Hand hand)
        {
            if (hand.CurrentGrab == null)
                return null;
            Rigidbody body = hand.CurrentGrab.GetComponentInParent<Rigidbody>();
            return body != null ? body.GetComponent<SpellFocus>() : null;
        }

        private void Update()
        {
            if (!FindRig())
                return;
            for (int i = 0; i < 2; i++)
            {
                SpellFocus focus = FocusIn(_hands[i]);
                bool pressed = focus != null && _hands[i].HandInputReader.Trigger > 0.55f;
                if (pressed && !_charging[i])
                {
                    _charging[i] = true;
                    _started[i] = Time.time;
                    _shownSpell[i] = 0;
                    Play(chargeClips, focus.Tip, 0.5f);
                }
                else if (!pressed && _charging[i])
                {
                    _charging[i] = false;
                    HideTip(i);
                    if (focus != null)
                        Cast(focus, focus.SpellFor(Time.time - _started[i]));
                }
                if (_charging[i] && focus != null)
                    ShowTip(i, focus, Time.time - _started[i]);
            }
        }

        private void ShowTip(int i, SpellFocus focus, float held)
        {
            if (_tipLight[i] == null)
            {
                _tipLight[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _tipLight[i].name = "Spell gathering";
                Destroy(_tipLight[i].GetComponent<Collider>());
                Light glow = _tipLight[i].AddComponent<Light>();
                glow.type = LightType.Point;
                glow.range = 3f;
            }
            Color colour = SpellFocus.ColourOf(focus.element);
            int spell = focus.SpellFor(held);
            var material = _tipLight[i].GetComponent<MeshRenderer>();
            if (material.sharedMaterial == null || material.sharedMaterial.shader != _solid.shader)
                material.sharedMaterial = new Material(_solid);
            material.sharedMaterial.SetColor("_BaseColor", Color.Lerp(colour, Color.white, 0.25f) * 1.5f);
            _tipLight[i].transform.position = focus.Tip;
            // Each spell reached makes the light step up in size, so the hold can be judged by eye.
            float size = 0.05f + 0.045f * spell + 0.01f * Mathf.Sin(Time.time * 18f);
            _tipLight[i].transform.localScale = Vector3.one * size;
            Light light = _tipLight[i].GetComponent<Light>();
            light.color = colour;
            light.intensity = 1f + 1.5f * spell;
            if (spell != _shownSpell[i])
            {
                if (_shownSpell[i] != 0)
                    Play(chargeClips, focus.Tip, 0.7f);
                _shownSpell[i] = spell;
            }
        }

        private void HideTip(int i)
        {
            if (_tipLight[i] != null)
                Destroy(_tipLight[i]);
            _tipLight[i] = null;
        }

        // ---------------------------------------------------------------- casting

        private void Cast(SpellFocus focus, int spell)
        {
            float power = (0.8f + 0.35f * focus.tier) * PlayerStats.FocusNow;
            Vector3 tip = focus.Tip, aim = focus.Aim;
            Color colour = SpellFocus.ColourOf(focus.element);
            LastSpell = SpellFocus.NameOf(focus.element, spell);
            Casts++;
            Play(castClips, tip, 0.9f);
            SessionRecorder.Note("spell", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} tier {1} power {2:F2}", LastSpell, focus.tier, power));

            switch (focus.element)
            {
                case SpellElement.Ember:
                    if (spell == 1) Launch(focus, tip, aim, colour, 0.07f, 24f, 0.25f * power, burst: 0.9f, impulse: 5f * power, slow: 0f);
                    else if (spell == 2) Launch(focus, tip, aim, colour, 0.16f, 15f, 0.5f * power, burst: 2.6f, impulse: 16f * power, slow: 0f);
                    else Cone(tip, aim, colour, reach: 6f, halfAngle: 40f, impulse: 20f * power, slow: 0f);
                    break;
                case SpellElement.Frost:
                    if (spell == 1) Launch(focus, tip, aim, colour, 0.06f, 30f, 0.3f * power, burst: 0.7f, impulse: 3f * power, slow: 2f);
                    else if (spell == 2) Launch(focus, tip, aim, colour, 0.14f, 16f, 0.4f * power, burst: 2.8f, impulse: 4f * power, slow: 5f);
                    else Cone(tip, aim, colour, reach: 7f, halfAngle: 50f, impulse: 3f * power, slow: 8f);
                    break;
                default:
                    if (spell == 1) Strike(tip, aim, colour, jumps: 0, impulse: 6f * power);
                    else if (spell == 2) Strike(tip, aim, colour, jumps: 3, impulse: 8f * power);
                    else Thunderclap(tip, aim, colour, 3.5f, 26f * power);
                    break;
            }
        }

        /// <summary>A flying spell: travels straight, bursts where it lands.</summary>
        private void Launch(SpellFocus focus, Vector3 tip, Vector3 aim, Color colour, float size, float speed, float mass, float burst, float impulse, float slow)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "Spell bolt";
            bolt.transform.position = tip + aim * (size + 0.05f);
            bolt.transform.localScale = Vector3.one * size;
            var material = new Material(_solid);
            material.SetColor("_BaseColor", Color.Lerp(colour, Color.white, 0.25f) * 1.5f);
            bolt.GetComponent<MeshRenderer>().sharedMaterial = material;
            Light glow = bolt.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = colour;
            glow.range = 2f + size * 12f;
            glow.intensity = 2.5f;
            if (trailMaterial != null)
            {
                TrailRenderer trail = bolt.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMaterial;
                trail.time = 0.3f;
                trail.startWidth = size * 1.7f;
                trail.endWidth = 0f;
                trail.startColor = new Color(colour.r, colour.g, colour.b, 0.9f);
                trail.endColor = new Color(colour.r, colour.g, colour.b, 0f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Rigidbody body = bolt.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            SpellBolt spell = bolt.AddComponent<SpellBolt>();
            spell.Begin(this, colour, burst, impulse, slow);
            // A spell must not strike its caster or the focus it left.
            foreach (Collider mine in bolt.GetComponentsInChildren<Collider>())
            {
                foreach (Collider rig in _rigColliders)
                    if (rig != null) Physics.IgnoreCollision(mine, rig, true);
                foreach (Collider own in focus.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(mine, own, true);
            }
            body.linearVelocity = aim * speed;
        }

        /// <summary>Push, and perhaps slow, every loose body within a radius of a point.</summary>
        public void Burst(Vector3 centre, Color colour, float radius, float impulse, float slow, Rigidbody except = null)
        {
            var seen = new HashSet<Rigidbody>();
            foreach (Collider hit in Physics.OverlapSphere(centre, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody body = hit.attachedRigidbody;
                if (Told(hit, centre, impulse, slow, radius))
                    continue;
                if (body == null || body == except || !seen.Add(body) || body.isKinematic || IsPlayer(body))
                    continue;
                Vector3 away = body.worldCenterOfMass - centre;
                float falloff = Mathf.Clamp01(1f - away.magnitude / (radius * 1.2f));
                Affect(body, (away.normalized + Vector3.up * 0.25f).normalized * impulse * falloff, body.worldCenterOfMass, slow);
            }
            Flash(centre, colour, radius);
            Play(hitClips, centre, Mathf.Clamp01(radius / 2.5f));
        }

        /// <summary>A wide wash from the tip: everything loose in a cone ahead.</summary>
        private void Cone(Vector3 tip, Vector3 aim, Color colour, float reach, float halfAngle, float impulse, float slow)
        {
            var seen = new HashSet<Rigidbody>();
            foreach (Collider hit in Physics.OverlapSphere(tip + aim * reach * 0.5f, reach * 0.6f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody body = hit.attachedRigidbody;
                if (Vector3.Angle(aim, hit.bounds.center - tip) <= halfAngle && Told(hit, tip, impulse, slow, reach))
                    continue;
                if (body == null || !seen.Add(body) || body.isKinematic || IsPlayer(body))
                    continue;
                Vector3 to = body.worldCenterOfMass - tip;
                if (to.magnitude > reach || Vector3.Angle(aim, to) > halfAngle)
                    continue;
                float falloff = Mathf.Clamp01(1.1f - to.magnitude / reach);
                Affect(body, (aim + Vector3.up * 0.2f).normalized * impulse * falloff, body.worldCenterOfMass, slow);
            }
            for (int puff = 0; puff < 5; puff++)
                Flash(tip + aim * (0.6f + puff * reach / 5.5f), colour, 0.5f + puff * 0.35f);
            Play(hitClips, tip + aim * 2f, 1f);
        }

        /// <summary>Lightning: strikes the first thing along the aim at once, then jumps to things nearby.</summary>
        private void Strike(Vector3 tip, Vector3 aim, Color colour, int jumps, float impulse)
        {
            Vector3 from = tip;
            Rigidbody target = null;
            IDamageable creature = null;
            Vector3 point = tip + aim * 18f;
            foreach (RaycastHit hit in Physics.SphereCastAll(tip + aim * 0.2f, 0.25f, aim, 18f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody body = hit.rigidbody;
                if (body != null && IsPlayer(body))
                    continue;
                if (hit.distance < Vector3.Distance(tip, point))
                {
                    point = hit.point;
                    target = body != null && !body.isKinematic ? body : null;
                    creature = hit.collider.GetComponentInParent<IDamageable>();
                }
            }
            var struck = new HashSet<Rigidbody>();
            for (int jump = 0; jump <= jumps; jump++)
            {
                Bolt(from, point, colour);
                Flash(point, colour, 0.5f);
                if (jump == 0 && creature != null)
                {
                    creature.TakeHit(point, aim * impulse, impulse * 4.5f, false);
                    BodiesAffected++;
                }
                if (target == null)
                    break;
                struck.Add(target);
                Affect(target, ((point - from).normalized + Vector3.up * 0.2f).normalized * impulse, point, 0f);
                impulse *= 0.75f;

                // The nearest body not yet struck, within five metres, takes the next jump.
                Rigidbody next = null;
                float nearest = 5f;
                foreach (Collider near in Physics.OverlapSphere(point, 5f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    Rigidbody body = near.attachedRigidbody;
                    if (body == null || body.isKinematic || struck.Contains(body) || IsPlayer(body) || body.GetComponent<ImpactFlash>() == null)
                        continue;
                    float distance = Vector3.Distance(point, body.worldCenterOfMass);
                    if (distance < nearest)
                    {
                        nearest = distance;
                        next = body;
                    }
                }
                if (next == null)
                    break;
                from = point;
                point = next.worldCenterOfMass;
                target = next;
            }
            Play(hitClips, point, 1f);
        }

        private void Thunderclap(Vector3 tip, Vector3 aim, Color colour, float radius, float impulse)
        {
            Vector3 point = tip + aim * 12f;
            if (Physics.Raycast(tip + aim * 0.3f, aim, out RaycastHit hit, 30f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                point = hit.point;
            Bolt(point + Vector3.up * 14f, point, colour);
            Bolt(tip, point, colour);
            Burst(point, colour, radius, impulse, 0f);
        }

        // ---------------------------------------------------------------- effects on bodies

        private void Affect(Rigidbody body, Vector3 impulse, Vector3 at, float slowSeconds)
        {
            body.AddForceAtPosition(impulse * Mathf.Min(body.mass, 10f) * 0.5f, at, ForceMode.Impulse);
            BodiesAffected++;
            if (DamageFeedback.Instance != null)
                DamageFeedback.Instance.Report(body.gameObject, at, impulse.magnitude * 2.2f + slowSeconds * 2f, true);
            ImpactFlash flash = body.GetComponent<ImpactFlash>();
            if (flash != null)
                flash.Pulse(Mathf.Clamp01(impulse.magnitude / 12f + 0.3f));
            if (slowSeconds > 0f)
            {
                Chilled chill = body.GetComponent<Chilled>();
                if (chill == null)
                    chill = body.gameObject.AddComponent<Chilled>();
                chill.Extend(slowSeconds);
            }
        }

        private static bool IsPlayer(Rigidbody body) => body.GetComponentInParent<Player>() != null;

        private readonly HashSet<IDamageable> _toldThisFrame = new HashSet<IDamageable>();
        private int _toldFrame = -1;

        /// <summary>
        /// Creatures that move by animation cannot be pushed, so they are told they were hit.
        /// True if this collider belongs to one (it is then dealt with; once per burst).
        /// </summary>
        private bool Told(Collider hit, Vector3 centre, float impulse, float slow, float radius)
        {
            IDamageable creature = hit.GetComponentInParent<IDamageable>();
            if (creature == null)
                return false;
            if (_toldFrame != Time.frameCount)
            {
                _toldFrame = Time.frameCount;
                _toldThisFrame.Clear();
            }
            if (_toldThisFrame.Add(creature))
            {
                Vector3 point = hit.ClosestPoint(centre);
                float falloff = Mathf.Clamp01(1.15f - Vector3.Distance(point, centre) / Mathf.Max(radius, 0.1f));
                creature.TakeHit(point, (point - centre).normalized * impulse * falloff, (impulse * 3f + slow * 2f) * falloff, false);
                BodiesAffected++;
            }
            return true;
        }

        // ---------------------------------------------------------------- pictures and sound

        private void Flash(Vector3 at, Color colour, float radius)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Spell burst";
            Destroy(flash.GetComponent<Collider>());
            // See-through and fading, so a burst reads as light and not as a solid ball.
            var material = new Material(_solid);
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", new Color(colour.r, colour.g, colour.b, 0.5f));
            flash.GetComponent<MeshRenderer>().sharedMaterial = material;
            flash.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flash.transform.position = at;
            flash.AddComponent<BurstVisual>().Begin(radius);
            if (DamageFeedback.Instance != null)
                DamageFeedback.Instance.Sparks(at, Mathf.Clamp01(radius / 2f), colour);
        }

        private void Bolt(Vector3 from, Vector3 to, Color colour)
        {
            var holder = new GameObject("Lightning");
            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.sharedMaterial = trailMaterial != null ? trailMaterial : _solid;
            line.startColor = line.endColor = new Color(colour.r, colour.g, colour.b, 1f);
            line.startWidth = line.endWidth = 0.09f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int points = 9;
            line.positionCount = points;
            for (int p = 0; p < points; p++)
            {
                Vector3 along = Vector3.Lerp(from, to, p / (points - 1f));
                if (p > 0 && p < points - 1)
                    along += Random.insideUnitSphere * 0.18f;
                line.SetPosition(p, along);
            }
            Destroy(holder, 0.14f);
        }

        private static void Play(AudioClip[] clips, Vector3 at, float volume)
        {
            if (clips == null || clips.Length == 0)
                return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, at, volume);
        }

        /// <summary>A sphere of light that swells and is gone in a quarter of a second.</summary>
        private sealed class BurstVisual : MonoBehaviour
        {
            private float _radius, _age;

            public void Begin(float radius)
            {
                _radius = radius;
                transform.localScale = Vector3.one * 0.1f;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                float k = _age / 0.25f;
                transform.localScale = Vector3.one * Mathf.Lerp(0.1f, _radius * 1.4f, Mathf.Sqrt(k));
                Material material = GetComponent<MeshRenderer>().sharedMaterial;
                Color colour = material.GetColor("_BaseColor");
                colour.a = 0.5f * (1f - k);
                material.SetColor("_BaseColor", colour);
                if (k >= 1f)
                    Destroy(gameObject);
            }
        }
    }

    /// <summary>A spell in flight. Bursts on the first thing it touches, or when its time is up.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SpellBolt : MonoBehaviour
    {
        private FocusCasting _caster;
        private Color _colour;
        private float _burst, _impulse, _slow;
        private bool _done;

        public void Begin(FocusCasting caster, Color colour, float burst, float impulse, float slow)
        {
            _caster = caster;
            _colour = colour;
            _burst = burst;
            _impulse = impulse;
            _slow = slow;
            Destroy(gameObject, 4f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_done)
                return;
            _done = true;
            Vector3 at = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            if (_caster != null)
                _caster.Burst(at, _colour, _burst, _impulse, _slow);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Frost on a body: it moves as if through deep snow until the time runs out, and shows blue.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Chilled : MonoBehaviour
    {
        private Rigidbody _body;
        private float _until, _linear, _angular;
        private bool _applied;

        public bool Active => _applied;

        public void Extend(float seconds)
        {
            if (_body == null)
                _body = GetComponent<Rigidbody>();
            if (!_applied)
            {
                _linear = _body.linearDamping;
                _angular = _body.angularDamping;
                _body.linearDamping = _linear + 6f;
                _body.angularDamping = _angular + 6f;
                _applied = true;
            }
            _until = Mathf.Max(_until, Time.time + seconds);
        }

        private void Update()
        {
            if (!_applied || Time.time < _until)
                return;
            _body.linearDamping = _linear;
            _body.angularDamping = _angular;
            _applied = false;
            Destroy(this);
        }
    }
}
