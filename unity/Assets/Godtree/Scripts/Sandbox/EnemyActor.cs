using BIMOS;
using Godtree.Combat;
using Godtree.Diagnostics;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype of a man-shaped creature that fights: it idles, notices the player, walks
    /// up, and strikes when in reach. Blows from weapons, arrows and spells hurt it. A hard blow
    /// from a blade near a joint cuts that limb off; the blow that kills it always does. Losing
    /// the head or a leg kills it; losing both forearms leaves it unable to strike.
    /// It moves by animation on the shared human skeleton, not by physics, so it is told about
    /// hits (IDamageable) and reads its own collisions.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyActor : MonoBehaviour, IDamageable
    {
        public float maxHealth = 90f;
        public float walkSpeed = 1.0f;
        public float noticeDistance = 7.5f;
        public float strikeDistance = 1.6f;
        public float strikeDamage = 12f;

        [Tooltip("Impulse (newton seconds) a blade needs to cut a limb off a creature that is still healthy.")]
        public float severImpulse = 3f;

        [Tooltip("Seconds after death before it stands up again where it started. 0: never.")]
        public float respawnSeconds = 25f;

        public string idleClip = "Zombie_Idle", walkClip = "Zombie_Walk", strikeClip = "Zombie_Scratch", hurtClip = "Hit_Chest", deathClip = "Death_A";

        public float Health { get; private set; }
        public bool Dead { get; private set; }
        public int Strikes { get; private set; }

        /// <summary>Raised when any creature dies: the creature and whether a limb came off with the blow.</summary>
        public static event System.Action<EnemyActor, bool> Died;

        private Animator _animator;
        private Dismemberable _limbs;
        private Player _player;
        private Collider[] _playerColliders = new Collider[0];
        private Vector3 _home;
        private Quaternion _homeFacing;
        private string _clip;
        private float _busyUntil, _nextStrike, _strikeLandsAt = -1f, _diedAt;
        private float _slowUntil, _slowFactor = 1f;

        /// <summary>True while frost or roots are holding it back.</summary>
        public bool Slowed => Time.time < _slowUntil;

        /// <summary>Frost and roots: for some seconds it walks at a fraction of its speed (0 holds it where it stands) and strikes less often.</summary>
        public void Slow(float seconds, float factor)
        {
            _slowUntil = Mathf.Max(_slowUntil, Time.time + seconds);
            _slowFactor = Mathf.Clamp01(factor);
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _limbs = GetComponent<Dismemberable>();
            _home = transform.position;
            _homeFacing = transform.rotation;
            Health = maxHealth;
        }

        private void Play(string clip, float fade = 0.2f)
        {
            if (clip == _clip || string.IsNullOrEmpty(clip))
                return;
            int state = Animator.StringToHash(clip);
            if (!_animator.HasState(0, state))
                return;
            _clip = clip;
            _animator.CrossFadeInFixedTime(state, fade, 0, 0f);
        }

        private Transform PlayerHead
        {
            get
            {
                if (_player == null)
                {
                    _player = FindFirstObjectByType<Player>();
                    if (_player != null)
                        _playerColliders = _player.transform.root.GetComponentsInChildren<Collider>(true);
                }
                return _player != null && _player.ControllerRig != null ? _player.ControllerRig.CameraTransform : null;
            }
        }

        private void Update()
        {
            if (Dead)
            {
                if (respawnSeconds > 0f && Time.time - _diedAt > respawnSeconds)
                    Respawn();
                return;
            }
            Transform head = PlayerHead;
            if (head == null)
                return;
            Vector3 toPlayer = Vector3.ProjectOnPlane(head.position - transform.position, Vector3.up);
            float distance = toPlayer.magnitude;

            // A strike in progress lands part-way through its swing, if the player is still in reach.
            if (_strikeLandsAt > 0f && Time.time >= _strikeLandsAt)
            {
                _strikeLandsAt = -1f;
                if (distance < strikeDistance + 0.4f && PlayerStats.Instance != null && CanReach(head))
                {
                    PlayerStats.Instance.TakeDamage(strikeDamage, transform.position + Vector3.up * 1.2f);
                    Strikes++;
                    SessionRecorder.Note("enemy_strike", name);
                }
            }
            if (Time.time < _busyUntil)
                return;

            if (distance > noticeDistance)
            {
                Play(idleClip, 0.4f);
                return;
            }
            if (distance > 0.05f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPlayer.normalized), 160f * Time.deltaTime);

            bool canStrike = _limbs == null || !_limbs.LostBothArms;
            if (distance > strikeDistance)
            {
                Play(walkClip);
                float pace = Slowed ? _slowFactor : 1f;
                _animator.speed = Slowed ? Mathf.Max(0.15f, _slowFactor) : 1f;
                Vector3 step = transform.forward * (walkSpeed * pace * Time.deltaTime);
                // Do not walk through walls or tables: look a body's width ahead at waist height.
                if (!Physics.SphereCast(transform.position + Vector3.up * 0.9f, 0.25f, transform.forward, out RaycastHit ahead, 0.45f, Physics.AllLayers, QueryTriggerInteraction.Ignore)
                    || ahead.collider.transform.IsChildOf(transform) || IsPlayer(ahead.collider))
                    transform.position += step;
                else
                    transform.position += transform.right * (walkSpeed * 0.6f * Time.deltaTime);
            }
            else if (canStrike && Time.time >= _nextStrike && CanReach(head))
            {
                Play(strikeClip, 0.1f);
                _clip = null; // so the same strike can be played again next time
                _strikeLandsAt = Time.time + 0.55f;
                _busyUntil = Time.time + 1.3f;
                _nextStrike = Time.time + (Slowed ? 5f : 3.2f);
            }
            else
            {
                Play(idleClip, 0.3f);
            }
        }

        /// <summary>Nothing solid between its chest and the player's head: it cannot strike through a wall.</summary>
        private bool CanReach(Transform head)
        {
            Vector3 chest = transform.position + Vector3.up * 1.2f;
            foreach (RaycastHit hit in Physics.RaycastAll(chest, head.position - chest, Vector3.Distance(chest, head.position), Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || IsPlayer(hit.collider))
                    continue;
                Rigidbody body = hit.rigidbody;
                if (body != null && !body.isKinematic)
                    continue; // loose things (a held weapon, a thrown crate) do not shield the player
                return false;
            }
            return true;
        }

        private bool IsPlayer(Collider collider)
        {
            return _player != null && collider.transform.IsChildOf(_player.transform.root);
        }

        // ---------------------------------------------------------------- being hit

        // When each thing last landed a blow, so one that stays in contact (a blade drawn across,
        // a weapon pressed in and then swung again) counts again after a moment, and a single
        // touch does not count many times.
        private readonly System.Collections.Generic.Dictionary<Rigidbody, float> _lastBlow = new System.Collections.Generic.Dictionary<Rigidbody, float>();

        private void OnCollisionEnter(Collision collision) => Struck(collision, 0.12f);

        private void OnCollisionStay(Collision collision) => Struck(collision, 0.4f);

        private void Struck(Collision collision, float gapSeconds)
        {
            if (Dead || IsPlayer(collision.collider) && collision.rigidbody == null)
                return;
            if (collision.rigidbody != null && _lastBlow.TryGetValue(collision.rigidbody, out float last) && Time.time - last < gapSeconds)
                return;
            Rigidbody striker = collision.rigidbody;
            if (striker == null || striker.isKinematic)
                return;
            // Bare hands and the player's own body do not count as blows.
            if (striker.GetComponentInParent<Player>() != null)
                return;
            // This body is moved by animation, so the physics engine's own figure for the blow can
            // be enormous (the weapon is pressed into something that will not give). Judge the blow
            // by how fast the thing was moving and how heavy it is.
            // Capped at 12 m/s: now and then the engine reports a wild speed for one frame.
            float impulse = Mathf.Min(collision.relativeVelocity.magnitude, 12f) * Mathf.Min(striker.mass, 1.5f);
            if (impulse < 1.2f)
                return;
            _lastBlow[striker] = Time.time;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : striker.worldCenterOfMass;
            bool spell = striker.name.StartsWith("Mote") || striker.name.StartsWith("Spell");
            GripPose weapon = striker.GetComponent<GripPose>();
            bool blade = weapon != null && (weapon.style == HoldStyle.Blade);
            // Six health per newton second: a decent one-handed sword stroke takes about fifteen.
            // (At four, the robot facing two creatures at once could not win in seventy strokes.)
            float damage = impulse * 6f * (spell ? PlayerStats.FocusNow : PlayerStats.StrengthNow);
            TakeHit(point, collision.relativeVelocity.normalized * -impulse, damage, blade);
        }

        public void TakeHit(Vector3 point, Vector3 impulse, float damage, bool blade)
        {
            if (Dead || damage < 0.5f)
                return;
            Health = Mathf.Max(0f, Health - damage);
            if (DamageFeedback.Instance != null)
            {
                DamageFeedback.Instance.Popup(point, Mathf.RoundToInt(damage).ToString(), Color.white, Mathf.Lerp(0.05f, 0.11f, Mathf.InverseLerp(5f, 80f, damage)));
                DamageFeedback.Instance.Sparks(point, Mathf.InverseLerp(4f, 40f, damage), new Color(0.75f, 0.03f, 0.05f));
            }
            SessionRecorder.Note("enemy_hit", string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} damage {1:F0} health {2:F0} {3}", name, damage, Health, blade ? "blade" : "blunt"));

            bool severed = false;
            bool killing = Health <= 0f;
            bool weakened = Health <= maxHealth * 0.5f;
            if (blade && _limbs != null && _limbs.Ready && (killing || (weakened && impulse.magnitude >= severImpulse)))
            {
                // The blow that kills always takes something with it, reaching further to find a joint.
                // A hard blow on a weakened creature takes an arm or a leg; the head only comes off at the end.
                string joint = _limbs.NearestJoint(point, killing ? 1.2f : 0.38f);
                if (joint == "neck" && !killing)
                    joint = null;
                if (joint != null)
                {
                    Vector3 at = _limbs.JointPosition(joint);
                    GameObject piece = _limbs.Sever(joint, impulse.normalized * Mathf.Clamp(impulse.magnitude, 3f, 14f));
                    severed = piece != null;
                    if (severed)
                    {
                        BloodBurst.At(at, transform, DamageFeedback.Instance);
                        SessionRecorder.Note("sever", name + " " + joint);
                        Health = Mathf.Max(0f, Health - 35f);
                    }
                }
            }
            if (Health <= 0f || (_limbs != null && (_limbs.LostHead || _limbs.LostALeg)))
                Die(severed);
            else
            {
                Play(hurtClip, 0.05f);
                _clip = null;
                _busyUntil = Time.time + 0.45f;
                _strikeLandsAt = -1f; // a hit interrupts its swing
            }
        }

        private void Die(bool severed)
        {
            Dead = true;
            Health = 0f;
            _diedAt = Time.time;
            _strikeLandsAt = -1f;
            _clip = null;
            Play(deathClip, 0.1f);
            foreach (Collider collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
            SessionRecorder.Note("enemy_death", name);
            Died?.Invoke(this, severed);
        }

        private void Respawn()
        {
            // A fresh copy would be cleaner; for the sandbox, rebuilding this one is enough:
            // the scene is reloaded between sessions, and severed pieces cannot grow back.
            if (_limbs != null && _limbs.PiecesLost > 0)
            {
                respawnSeconds = 0f;
                gameObject.SetActive(false);
                return;
            }
            Dead = false;
            Health = maxHealth;
            transform.SetPositionAndRotation(_home, _homeFacing);
            foreach (Collider collider in GetComponentsInChildren<Collider>())
                collider.enabled = true;
            _clip = null;
            Play(idleClip, 0f);
        }
    }

    /// <summary>Stylised blood where a limb came off (decision 0016): a burst, then a short spurt from the stump.</summary>
    public sealed class BloodBurst : MonoBehaviour
    {
        private float _until, _next;
        private DamageFeedback _feedback;
        private static readonly Color Blood = new Color(0.72f, 0.02f, 0.05f);

        public static void At(Vector3 point, Transform follow, DamageFeedback feedback)
        {
            if (feedback == null)
                return;
            feedback.Sparks(point, 1f, Blood);
            feedback.Sparks(point + Vector3.up * 0.05f, 1f, Blood);
            var holder = new GameObject("Blood spurt");
            holder.transform.SetParent(follow, true);
            holder.transform.position = point;
            BloodBurst spurt = holder.AddComponent<BloodBurst>();
            spurt._feedback = feedback;
            spurt._until = Time.time + 2.5f;
        }

        private void Update()
        {
            if (Time.time > _until)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < _next)
                return;
            _next = Time.time + 0.12f;
            _feedback.Sparks(transform.position, 0.25f, Blood);
        }
    }
}
