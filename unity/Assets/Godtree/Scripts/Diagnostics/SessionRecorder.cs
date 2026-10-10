using System;
using System.Globalization;
using System.IO;
using System.Text;
using BIMOS;
using Godtree.Combat;
using UnityEngine;

namespace Godtree.Diagnostics
{
    /// <summary>
    /// Records a play session so it can be reviewed afterwards without watching live:
    ///  - samples.csv: 36 times a second, where each real controller is, where its physics
    ///    hand actually is, the gap between them, and what the hand holds and how it moves;
    ///  - events.csv: grabs, releases and hits;
    ///  - frames/: one third-person and one first-person snapshot every second;
    ///  - summary.json: the headline numbers, written when play stops.
    /// Diagnostic tool for the sandbox. Costs a small hitch once a second for the snapshots.
    /// </summary>
    public sealed class SessionRecorder : MonoBehaviour
    {
        [SerializeField] private string outputRoot = "D:/godtree-work/recordings";
        [SerializeField] private float snapshotInterval = 1f;
        [SerializeField] private int physicsStepsPerSample = 4;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private string _dir;
        private StreamWriter _samples, _events;
        private Player _player;
        private Hand _left, _right;
        private Grab _leftHeld, _rightHeld;
        private Camera _snapshotCamera;
        private RenderTexture _rt;
        private Texture2D _tex;
        private int _step, _frameIndex, _sampleCount;
        private float _nextSnapshot;

        // Running statistics per hand: [0] left, [1] right.
        private readonly double[] _errSumEmpty = new double[2], _errSumHeld = new double[2];
        private readonly int[] _nEmpty = new int[2], _nHeld = new int[2];
        private readonly float[] _errMaxEmpty = new float[2], _errMaxHeld = new float[2];
        private readonly double[] _rotSumHeld = new double[2];
        private readonly float[] _rotMaxHeld = new float[2], _spinMaxHeld = new float[2];
        private int _grabs, _releases, _hits;

        private void Start()
        {
            try
            {
                _dir = Path.Combine(outputRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(Path.Combine(_dir, "frames"));
            }
            catch (Exception)
            {
                _dir = Path.Combine(Application.persistentDataPath, "recordings", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(Path.Combine(_dir, "frames"));
            }

            _samples = new StreamWriter(Path.Combine(_dir, "samples.csv"), false, new UTF8Encoding(false));
            _samples.WriteLine("t,head_x,head_y,head_z," +
                               "L_ctrl_x,L_ctrl_y,L_ctrl_z,L_hand_x,L_hand_y,L_hand_z,L_pos_err_m,L_rot_err_deg,L_grip,L_trigger,L_held,L_held_speed,L_held_spin_deg_s," +
                               "R_ctrl_x,R_ctrl_y,R_ctrl_z,R_hand_x,R_hand_y,R_hand_z,R_pos_err_m,R_rot_err_deg,R_grip,R_trigger,R_held,R_held_speed,R_held_spin_deg_s");
            _events = new StreamWriter(Path.Combine(_dir, "events.csv"), false, new UTF8Encoding(false));
            _events.WriteLine("t,event,hand,object,detail");

            var cameraObject = new GameObject("SessionRecorder snapshot camera");
            cameraObject.transform.SetParent(transform, false);
            _snapshotCamera = cameraObject.AddComponent<Camera>();
            _snapshotCamera.enabled = false;
            _snapshotCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _snapshotCamera.nearClipPlane = 0.05f;
            _rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
            _tex = new Texture2D(640, 360, TextureFormat.RGB24, false);

            ImpactFlash.Hit += OnHit;
            _active = this;
            Debug.Log("[Godtree] Recording session to " + _dir);
        }

        private bool FindRig()
        {
            if (_player != null && _left != null && _right != null)
                return true;
            _player = FindFirstObjectByType<Player>();
            if (_player == null)
                return false;
            foreach (Hand hand in FindObjectsByType<Hand>(FindObjectsSortMode.None))
            {
                if (hand.IsLeftHand) _left = hand;
                else _right = hand;
            }
            return _left != null && _right != null;
        }

        private void FixedUpdate()
        {
            if (_samples == null || !FindRig())
                return;

            TrackGrab(_left, ref _leftHeld, "left");
            TrackGrab(_right, ref _rightHeld, "right");

            if (++_step % physicsStepsPerSample != 0)
                return;

            var sb = new StringBuilder(400);
            Vector3 head = _player.ControllerRig.CameraTransform.position;
            sb.Append(Time.time.ToString("F3", Inv)).Append(',');
            AppendVector(sb, head);
            AppendHand(sb, _left, _player.ControllerRig.LeftControllerTransform, 0);
            AppendHand(sb, _right, _player.ControllerRig.RightControllerTransform, 1);
            sb.Length--;
            _samples.WriteLine(sb.ToString());
            _sampleCount++;
        }

        private void AppendHand(StringBuilder sb, Hand hand, Transform controller, int index)
        {
            PhysicsHand physicsHand = hand.PhysicsHand;
            Transform handTransform = hand.PhysicsHandTransform;
            // What the hand is being asked to reach (controller plus any grab offset) versus where it is.
            Vector3 wanted = physicsHand != null && physicsHand.Target != null
                ? physicsHand.Target.TransformPoint(physicsHand.TargetOffsetPosition)
                : controller.position;
            Quaternion wantedRotation = physicsHand != null && physicsHand.Target != null
                ? physicsHand.Target.rotation * physicsHand.TargetOffsetRotation
                : controller.rotation;
            float posError = Vector3.Distance(wanted, handTransform.position);
            float rotError = Quaternion.Angle(wantedRotation, handTransform.rotation);

            string heldName = "";
            float heldSpeed = 0f, heldSpin = 0f;
            Grab grab = hand.CurrentGrab;
            if (grab != null)
            {
                Rigidbody body = grab.GetComponentInParent<Rigidbody>();
                if (body != null)
                {
                    heldName = body.name.Replace(',', ' ');
                    heldSpeed = body.linearVelocity.magnitude;
                    heldSpin = body.angularVelocity.magnitude * Mathf.Rad2Deg;
                }
                _errSumHeld[index] += posError; _nHeld[index]++;
                _rotSumHeld[index] += rotError;
                if (posError > _errMaxHeld[index]) _errMaxHeld[index] = posError;
                if (rotError > _rotMaxHeld[index]) _rotMaxHeld[index] = rotError;
                if (heldSpin > _spinMaxHeld[index]) _spinMaxHeld[index] = heldSpin;
            }
            else
            {
                _errSumEmpty[index] += posError; _nEmpty[index]++;
                if (posError > _errMaxEmpty[index]) _errMaxEmpty[index] = posError;
            }

            AppendVector(sb, controller.position);
            AppendVector(sb, handTransform.position);
            sb.Append(posError.ToString("F4", Inv)).Append(',');
            sb.Append(rotError.ToString("F1", Inv)).Append(',');
            sb.Append(hand.HandInputReader.Grip.ToString("F2", Inv)).Append(',');
            sb.Append(hand.HandInputReader.Trigger.ToString("F2", Inv)).Append(',');
            sb.Append(heldName).Append(',');
            sb.Append(heldSpeed.ToString("F2", Inv)).Append(',');
            sb.Append(heldSpin.ToString("F0", Inv)).Append(',');
        }

        private static void AppendVector(StringBuilder sb, Vector3 v)
        {
            sb.Append(v.x.ToString("F3", Inv)).Append(',').Append(v.y.ToString("F3", Inv)).Append(',').Append(v.z.ToString("F3", Inv)).Append(',');
        }

        private void TrackGrab(Hand hand, ref Grab previous, string label)
        {
            Grab current = hand.CurrentGrab;
            if (current == previous)
                return;
            if (current != null)
            {
                _grabs++;
                Rigidbody body = current.GetComponentInParent<Rigidbody>();
                WriteEvent("grab", label, body != null ? body.name : current.name, current.GetType().Name);
            }
            else
            {
                _releases++;
                WriteEvent("release", label, previous != null ? previous.name : "", "");
            }
            previous = current;
        }

        private static SessionRecorder _active;

        /// <summary>Lets other sandbox code add a line to events.csv (a shot, a spell).</summary>
        public static void Note(string kind, string detail)
        {
            if (_active != null)
                _active.WriteEvent(kind, "", "", detail);
        }

        private void OnHit(string target, string by, float impulse)
        {
            _hits++;
            WriteEvent("hit", "", target, "by " + by + " impulse " + impulse.ToString("F1", Inv));
        }

        private void WriteEvent(string kind, string hand, string obj, string detail)
        {
            if (_events == null)
                return;
            _events.WriteLine(string.Join(",", Time.time.ToString("F3", Inv), kind, hand, obj.Replace(',', ' '), detail.Replace(',', ' ')));
        }

        private void LateUpdate()
        {
            if (_samples == null || !FindRig() || Time.time < _nextSnapshot)
                return;
            _nextSnapshot = Time.time + snapshotInterval;

            Transform head = _player.ControllerRig.CameraTransform;
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, flatForward);

            // Third person: behind and to the right, looking past the player at what they face.
            Vector3 from = head.position - flatForward * 1.9f + right * 1.1f + Vector3.up * 0.5f;
            Snapshot(from, Quaternion.LookRotation(head.position + flatForward * 0.9f - Vector3.up * 0.35f - from), 70f, "tp");
            // First person.
            Snapshot(head.position, head.rotation, 95f, "fp");
            _frameIndex++;
        }

        private void Snapshot(Vector3 position, Quaternion rotation, float fov, string tag)
        {
            _snapshotCamera.transform.SetPositionAndRotation(position, rotation);
            _snapshotCamera.fieldOfView = fov;
            _snapshotCamera.targetTexture = _rt;
            _snapshotCamera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
            _tex.Apply(false);
            RenderTexture.active = previous;
            _snapshotCamera.targetTexture = null;
            string name = string.Format(Inv, "{0:0000}_{1:0000.0}s_{2}.jpg", _frameIndex, Time.time, tag);
            File.WriteAllBytes(Path.Combine(_dir, "frames", name), _tex.EncodeToJPG(70));
        }

        private void OnDisable()
        {
            ImpactFlash.Hit -= OnHit;
            if (_active == this) _active = null;
            if (_samples == null)
                return;
            _samples.Flush(); _samples.Dispose(); _samples = null;
            _events.Flush(); _events.Dispose(); _events = null;

            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.AppendFormat(Inv, "  \"seconds\": {0:F1},\n  \"samples\": {1},\n  \"grabs\": {2},\n  \"releases\": {3},\n  \"hits\": {4},\n", Time.time, _sampleCount, _grabs, _releases, _hits);
            string[] names = { "left", "right" };
            for (int i = 0; i < 2; i++)
            {
                sb.AppendFormat(Inv,
                    "  \"{0}\": {{ \"empty_hand_gap_mean_cm\": {1:F1}, \"empty_hand_gap_max_cm\": {2:F1}, \"holding_gap_mean_cm\": {3:F1}, \"holding_gap_max_cm\": {4:F1}, \"holding_twist_mean_deg\": {5:F1}, \"holding_twist_max_deg\": {6:F1}, \"held_object_max_spin_deg_s\": {7:F0}, \"samples_holding\": {8} }}{9}\n",
                    names[i],
                    _nEmpty[i] > 0 ? _errSumEmpty[i] / _nEmpty[i] * 100 : 0, _errMaxEmpty[i] * 100,
                    _nHeld[i] > 0 ? _errSumHeld[i] / _nHeld[i] * 100 : 0, _errMaxHeld[i] * 100,
                    _nHeld[i] > 0 ? _rotSumHeld[i] / _nHeld[i] : 0, _rotMaxHeld[i], _spinMaxHeld[i], _nHeld[i], i == 0 ? "," : "");
            }
            sb.Append("}\n");
            File.WriteAllText(Path.Combine(_dir, "summary.json"), sb.ToString());
            Debug.Log("[Godtree] Session recording saved: " + _dir);
        }
    }
}
