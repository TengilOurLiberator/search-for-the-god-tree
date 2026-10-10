using BIMOS;
using UnityEngine;

namespace Godtree.Sandbox
{
    /// <summary>
    /// A person or man-shaped creature that moves: plays clips from the shared human animation
    /// set on its own skeleton, turns to face the player when they come near, and greets them.
    /// Sandbox prototype; the mind behind the movement (talking, fighting) is added by other
    /// components that call <see cref="Play"/>.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class NpcActor : MonoBehaviour
    {
        [Tooltip("Clip played when nothing else is going on.")]
        public string idleClip = "Idle_A";

        [Tooltip("Clip played while the player stands close. Empty: keep idling.")]
        public string attentiveClip = "Idle Listening";

        [Tooltip("Clip played once when the player first comes close. Empty: none.")]
        public string greetingClip = "Greeting";

        [Tooltip("How close the player must be, in metres, to be noticed.")]
        public float noticeDistance = 3.2f;

        [Tooltip("Turn to face the player when noticed.")]
        public bool facesPlayer = true;

        /// <summary>Name of the clip now playing.</summary>
        public string Current { get; private set; }

        /// <summary>True while the player is within the notice distance.</summary>
        public bool PlayerNear { get; private set; }

        /// <summary>Set by other components to stop this one choosing clips (a conversation, a fight).</summary>
        public bool Busy { get; set; }

        private Animator _animator;
        private Player _player;
        private Quaternion _homeFacing;
        private float _greetingUntil;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _homeFacing = transform.rotation;
        }

        private void Start()
        {
            // People who stand together should not move in step.
            Play(idleClip, 0f, Random.value);
        }

        /// <summary>Cross-fade to a clip by name. Unknown names are ignored and reported once.</summary>
        public void Play(string clip, float fade = 0.25f, float startAt = 0f)
        {
            if (string.IsNullOrEmpty(clip) || clip == Current)
                return;
            int state = Animator.StringToHash(clip);
            if (!_animator.HasState(0, state))
            {
                Debug.LogWarning($"[Godtree] {name}: no animation called '{clip}'");
                return;
            }
            Current = clip;
            if (fade <= 0f)
                _animator.Play(state, 0, startAt);
            else
                _animator.CrossFadeInFixedTime(state, fade, 0, startAt);
        }

        /// <summary>The player's head, or null before the rig exists.</summary>
        public Transform PlayerHead
        {
            get
            {
                if (_player == null)
                    _player = FindFirstObjectByType<Player>();
                return _player != null && _player.ControllerRig != null ? _player.ControllerRig.CameraTransform : null;
            }
        }

        private void Update()
        {
            Transform head = PlayerHead;
            if (head == null)
                return;
            Vector3 toPlayer = Vector3.ProjectOnPlane(head.position - transform.position, Vector3.up);
            bool near = toPlayer.magnitude < noticeDistance;

            if (facesPlayer)
            {
                Quaternion wanted = near && toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer.normalized) : _homeFacing;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, 140f * Time.deltaTime);
            }

            if (!Busy)
            {
                if (near && !PlayerNear && !string.IsNullOrEmpty(greetingClip))
                {
                    Play(greetingClip);
                    _greetingUntil = Time.time + 2.6f;
                }
                else if (Time.time >= _greetingUntil)
                {
                    Play(near && !string.IsNullOrEmpty(attentiveClip) ? attentiveClip : idleClip, 0.4f);
                }
            }
            PlayerNear = near;
        }
    }
}
