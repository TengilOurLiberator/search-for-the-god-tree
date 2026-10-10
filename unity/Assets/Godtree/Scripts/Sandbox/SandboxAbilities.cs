using BIMOS;
using Godtree.Combat;
using Godtree.Diagnostics;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Try-it-out prototypes for the sandbox, asked for by the owner ahead of the roadmap
    /// (the real bow and the real magic system come in later milestones):
    ///
    ///   BOW          Hold the bow in one hand. Bring the other hand to it, hold that hand's
    ///                TRIGGER, pull back, let go. Further pull, faster arrow.
    ///   MOTE BOLT    Empty hand: hold TRIGGER to gather a mote, let go to loose it. A
    ///                throwing motion sends it where your hand was going; otherwise it
    ///                flies where the controller points.
    ///   PUSH KNOT    Both hands empty: hold both TRIGGERS with your hands together, then
    ///                let go. Everything in front of you is shoved away.
    ///
    /// All three act through physics: the arrow and the bolt are rigid bodies that hit
    /// things, and the push is a force. Nothing decides an outcome by rule.
    /// </summary>
    public sealed class SandboxAbilities : MonoBehaviour
    {
        public AudioClip[] castClips, boltClips, pushClips, bowReleaseClips, arrowHitClips, boltHitClips;

        private const float PressAt = 0.6f, ReleaseAt = 0.3f;

        private Player _player;
        private Collider[] _rigColliders = new Collider[0];
        private readonly Hand[] _hands = new Hand[2];
        private readonly Transform[] _controllers = new Transform[2];
        private readonly bool[] _pressed = new bool[2], _charging = new bool[2], _drawing = new bool[2];
        private readonly float[] _chargeStart = new float[2];
        private readonly GameObject[] _orbs = new GameObject[2];
        private readonly Vector3[] _lastPalm = new Vector3[2], _palmVelocity = new Vector3[2];
        private GameObject _nockedArrow;
        private Material _moteMaterial, _woodMaterial, _steelMaterial, _waveMaterial;

        private bool FindRig()
        {
            if (_player != null)
                return true;
            _player = FindFirstObjectByType<Player>();
            if (_player == null)
                return false;
            foreach (Hand hand in _player.GetComponentsInChildren<Hand>(true))
                _hands[hand.IsLeftHand ? 0 : 1] = hand;
            _controllers[0] = _player.ControllerRig.LeftControllerTransform;
            _controllers[1] = _player.ControllerRig.RightControllerTransform;
            _rigColliders = _player.GetComponentsInChildren<Collider>(true);
            if (_hands[0] == null || _hands[1] == null)
            {
                _player = null;
                return false;
            }
            _moteMaterial = Unlit(new Color(1f, 0.85f, 0.42f));
            _waveMaterial = Unlit(new Color(1f, 0.85f, 0.42f, 0.35f));
            _woodMaterial = Lit(new Color(0.62f, 0.45f, 0.28f));
            _steelMaterial = Lit(new Color(0.73f, 0.76f, 0.81f));
            return true;
        }

        private void Update()
        {
            if (!FindRig())
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector3 palm = _hands[i].PalmTransform.position;
                if (Time.deltaTime > 0f)
                    _palmVelocity[i] = Vector3.Lerp(_palmVelocity[i], (palm - _lastPalm[i]) / Time.deltaTime, 0.35f);
                _lastPalm[i] = palm;
            }

            for (int i = 0; i < 2; i++)
            {
                int other = 1 - i;
                float trigger = _hands[i].HandInputReader.Trigger;
                bool wasPressed = _pressed[i];
                if (!wasPressed && trigger > PressAt) _pressed[i] = true;
                else if (wasPressed && trigger < ReleaseAt) _pressed[i] = false;

                bool free = _hands[i].CurrentGrab == null;
                bool otherHoldsBow = HoldsBow(_hands[other]);

                if (!wasPressed && _pressed[i] && free)
                {
                    if (otherHoldsBow && Vector3.Distance(_hands[i].PalmTransform.position, _hands[other].PalmTransform.position) < 0.45f)
                    {
                        _drawing[i] = true;
                        Play(castClips, _hands[i].PalmTransform.position, 0.3f);
                    }
                    else
                    {
                        _charging[i] = true;
                        _chargeStart[i] = Time.time;
                        Play(castClips, _hands[i].PalmTransform.position, 0.6f);
                    }
                }

                if (_drawing[i])
                {
                    if (!otherHoldsBow || !free) { _drawing[i] = false; HideNocked(); }
                    else if (_pressed[i]) ShowNocked(_hands[other].PalmTransform.position, _hands[i].PalmTransform.position);
                    else { _drawing[i] = false; HideNocked(); LooseArrow(_hands[other].PalmTransform.position, _hands[i].PalmTransform.position); }
                }

                if (_charging[i])
                {
                    if (!free) { _charging[i] = false; DestroyOrb(i); }
                    else if (_pressed[i]) UpdateOrb(i);
                    else ReleaseCharge(i);
                }
            }
        }

        private static bool HoldsBow(Hand hand)
        {
            if (hand.CurrentGrab == null)
                return false;
            Rigidbody body = hand.CurrentGrab.GetComponentInParent<Rigidbody>();
            return body != null && body.name.Contains("bow");
        }

        // ------------------------------------------------------------ bow

        private void ShowNocked(Vector3 bowHand, Vector3 drawHand)
        {
            if (_nockedArrow == null)
            {
                _nockedArrow = BuildArrowVisual();
                _nockedArrow.name = "Nocked arrow";
            }
            Vector3 direction = bowHand - drawHand;
            if (direction.sqrMagnitude < 0.0004f)
                return;
            _nockedArrow.transform.SetPositionAndRotation(drawHand + direction.normalized * 0.35f, Quaternion.LookRotation(direction));
        }

        private void HideNocked()
        {
            if (_nockedArrow != null)
                Destroy(_nockedArrow);
            _nockedArrow = null;
        }

        private void LooseArrow(Vector3 bowHand, Vector3 drawHand)
        {
            Vector3 pull = bowHand - drawHand;
            float draw = Mathf.Clamp(pull.magnitude, 0f, 0.75f);
            if (draw < 0.15f)
                return; // not drawn: nothing happens

            Vector3 direction = pull.normalized;
            float speed = Mathf.Lerp(8f, 42f, Mathf.InverseLerp(0.15f, 0.7f, draw));

            GameObject arrow = BuildArrowVisual();
            arrow.name = "Arrow";
            arrow.transform.SetPositionAndRotation(bowHand + direction * 0.4f, Quaternion.LookRotation(direction));
            var capsule = arrow.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.radius = 0.012f;
            capsule.height = 0.7f;
            var body = arrow.AddComponent<Rigidbody>();
            body.mass = 0.08f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var projectile = arrow.AddComponent<SandboxProjectile>();
            projectile.sticks = true;
            projectile.lifeSeconds = 25f;
            projectile.hitClips = arrowHitClips;
            IgnoreRig(arrow);
            body.linearVelocity = direction * speed;

            Play(bowReleaseClips, bowHand, 0.9f);
            SessionRecorder.Note("arrow", string.Format(System.Globalization.CultureInfo.InvariantCulture, "draw {0:F2} m speed {1:F0} m/s", draw, speed));
        }

        private GameObject BuildArrowVisual()
        {
            var root = new GameObject("ArrowVisual");
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.012f, 0.35f, 0.012f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = _woodMaterial;
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(tip.GetComponent<Collider>());
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 0.36f);
            tip.transform.localScale = new Vector3(0.03f, 0.03f, 0.07f);
            tip.GetComponent<MeshRenderer>().sharedMaterial = _steelMaterial;
            return root;
        }

        // ------------------------------------------------------------ spells

        private float Charge(int i) => Mathf.Clamp01((Time.time - _chargeStart[i]) / 0.8f);

        private bool HandsTogether() =>
            _charging[0] && _charging[1] &&
            Vector3.Distance(_hands[0].PalmTransform.position, _hands[1].PalmTransform.position) < 0.32f;

        private void UpdateOrb(int i)
        {
            if (_orbs[i] == null)
            {
                _orbs[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _orbs[i].name = "Mote (gathering)";
                Destroy(_orbs[i].GetComponent<Collider>());
                _orbs[i].GetComponent<MeshRenderer>().sharedMaterial = _moteMaterial;
                var glow = _orbs[i].AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.85f, 0.42f);
                glow.range = 2.5f;
            }
            float size = Mathf.Lerp(0.03f, HandsTogether() ? 0.2f : 0.12f, Charge(i));
            Vector3 position = _hands[i].PalmTransform.position + _controllers[i].forward * 0.1f;
            if (HandsTogether())
                position = (_hands[0].PalmTransform.position + _hands[1].PalmTransform.position) * 0.5f;
            _orbs[i].transform.position = position;
            _orbs[i].transform.localScale = Vector3.one * size;
            _orbs[i].GetComponent<Light>().intensity = 0.5f + 2.5f * Charge(i);
        }

        private void DestroyOrb(int i)
        {
            if (_orbs[i] != null)
                Destroy(_orbs[i]);
            _orbs[i] = null;
        }

        private void ReleaseCharge(int i)
        {
            float charge = Charge(i);
            bool together = HandsTogether();
            Vector3 origin = _orbs[i] != null ? _orbs[i].transform.position : _hands[i].PalmTransform.position;
            _charging[i] = false;
            DestroyOrb(i);

            if (together)
            {
                int other = 1 - i;
                float both = (charge + Charge(other)) * 0.5f;
                _charging[other] = false;
                DestroyOrb(other);
                if (both >= 0.15f)
                    PushKnot(origin, (_controllers[0].forward + _controllers[1].forward).normalized, both);
                return;
            }
            if (charge < 0.15f)
                return; // a tap does nothing

            Vector3 velocity = _palmVelocity[i];
            bool thrown = velocity.magnitude > 1.5f;
            Vector3 direction = thrown ? velocity.normalized : _controllers[i].forward;
            float speed = 10f + 12f * charge + (thrown ? Mathf.Min(velocity.magnitude, 6f) : 0f);

            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "Mote bolt";
            bolt.transform.position = origin + direction * 0.15f;
            bolt.transform.localScale = Vector3.one * Mathf.Lerp(0.06f, 0.12f, charge);
            bolt.GetComponent<MeshRenderer>().sharedMaterial = _moteMaterial;
            var glow = bolt.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.85f, 0.42f);
            glow.range = 3f;
            glow.intensity = 2f;
            var body = bolt.AddComponent<Rigidbody>();
            body.mass = Mathf.Lerp(0.15f, 0.4f, charge);
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var projectile = bolt.AddComponent<SandboxProjectile>();
            projectile.lifeSeconds = 5f;
            projectile.hitClips = boltHitClips;
            IgnoreRig(bolt);
            body.linearVelocity = direction * speed;

            Play(boltClips, origin, 0.8f);
            SessionRecorder.Note("mote_bolt", string.Format(System.Globalization.CultureInfo.InvariantCulture, "charge {0:F2} speed {1:F0} m/s {2}", charge, speed, thrown ? "thrown" : "pointed"));
        }

        private void PushKnot(Vector3 origin, Vector3 direction, float charge)
        {
            int pushed = 0;
            foreach (Collider hit in Physics.OverlapSphere(origin + direction * 2.2f, 3f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                Rigidbody body = hit.attachedRigidbody;
                if (body == null || body.isKinematic || body.GetComponentInParent<Player>() != null)
                    continue;
                Vector3 to = body.worldCenterOfMass - origin;
                float angle = Vector3.Angle(direction, to);
                if (angle > 55f)
                    continue;
                float falloff = Mathf.Clamp01(1f - to.magnitude / 5.5f) * Mathf.Clamp01(1f - angle / 80f);
                Vector3 impulse = (direction + Vector3.up * 0.15f).normalized * (4f + 8f * charge) * Mathf.Min(body.mass, 8f) * falloff;
                body.AddForce(impulse, ForceMode.Impulse);
                ImpactFlash flash = body.GetComponent<ImpactFlash>();
                if (flash != null)
                    flash.Pulse(falloff);
                pushed++;
            }

            GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wave.name = "Push knot wave";
            Destroy(wave.GetComponent<Collider>());
            wave.GetComponent<MeshRenderer>().sharedMaterial = _waveMaterial;
            wave.transform.position = origin + direction * 0.4f;
            wave.AddComponent<ExpandAndFade>().Begin(direction, 0.35f);

            Play(pushClips, origin, 1f);
            SessionRecorder.Note("push_knot", string.Format(System.Globalization.CultureInfo.InvariantCulture, "charge {0:F2} bodies {1}", charge, pushed));
        }

        // ------------------------------------------------------------ helpers

        private void IgnoreRig(GameObject projectile)
        {
            foreach (Collider mine in projectile.GetComponentsInChildren<Collider>())
                foreach (Collider rig in _rigColliders)
                    if (rig != null)
                        Physics.IgnoreCollision(mine, rig, true);
        }

        private static void Play(AudioClip[] clips, Vector3 at, float volume)
        {
            if (clips == null || clips.Length == 0)
                return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, at, volume);
        }

        private static Material Unlit(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            if (color.a < 1f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            return material;
        }

        private static Material Lit(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            return material;
        }

        /// <summary>The visible wave of a push knot: a sphere that grows, moves forward and is removed.</summary>
        private sealed class ExpandAndFade : MonoBehaviour
        {
            private Vector3 _direction;
            private float _life, _age;

            public void Begin(Vector3 direction, float life)
            {
                _direction = direction;
                _life = life;
                transform.localScale = Vector3.one * 0.3f;
            }

            private void Update()
            {
                _age += Time.deltaTime;
                transform.position += _direction * (9f * Time.deltaTime);
                transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 3.2f, _age / _life);
                if (_age >= _life)
                    Destroy(gameObject);
            }
        }
    }
}
