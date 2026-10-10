using System.Collections;
using System.IO;
using BIMOS;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Godtree.Diagnostics
{
    /// <summary>What the robot's virtual controller reports. Every control is a plain number.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RobotPadState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('G', 'R', 'B', 'T');

        [InputControl(layout = "Axis")] public float leftGrip;
        [InputControl(layout = "Axis")] public float rightGrip;
        [InputControl(layout = "Axis")] public float leftTrigger;
        [InputControl(layout = "Axis")] public float rightTrigger;
        [InputControl(layout = "Button", format = "FLT")] public float leftPrimary;
        [InputControl(layout = "Button", format = "FLT")] public float rightPrimary;
        [InputControl(layout = "Button", format = "FLT")] public float leftSecondary;
        [InputControl(layout = "Button", format = "FLT")] public float rightSecondary;
        [InputControl(layout = "Button", format = "FLT")] public float run;
        [InputControl(layout = "Button", format = "FLT")] public float jump;
        [InputControl(layout = "Vector2")] public Vector2 move;
        [InputControl(layout = "Vector2")] public Vector2 turn;
    }

    /// <summary>The robot's virtual controller. A device of its own, so nothing else binds to it by accident.</summary>
    [InputControlLayout(stateType = typeof(RobotPadState), displayName = "Godtree Robot Pad")]
    public sealed class RobotPad : InputDevice
    {
    }
    /// <summary>
    /// A stand-in for a person in a headset, so the game can be played with nobody there.
    /// It takes over the rig's head and controller poses and presses the buttons through the
    /// same input actions a real controller uses (by way of a virtual gamepad bound to them),
    /// so everything downstream of input runs exactly as it does for a player.
    ///
    /// It cannot judge comfort or fun. It can show whether a thing works, how the physics
    /// responds, and what it looks like, by running set routines and saving pictures.
    ///
    /// Create it with <see cref="Create"/> and wait for <see cref="Ready"/>. Hand targets are
    /// given either in world space or in "body space": metres to the right, up from the floor
    /// and forward of where the head is, following the way the head faces.
    /// </summary>
    public sealed class RobotPlayer : MonoBehaviour
    {
        public bool Ready { get; private set; }
        public Player Player { get; private set; }
        public Hand LeftHand { get; private set; }
        public Hand RightHand { get; private set; }

        private ControllerRig _rig;
        private RobotPad _pad;
        private RobotPadState _state;

        private readonly Vector3[] _handPosition = { new Vector3(-0.22f, 1.05f, 0.30f), new Vector3(0.22f, 1.05f, 0.30f) };
        private readonly Quaternion[] _handRotation = { Quaternion.identity, Quaternion.identity };
        private readonly bool[] _handInWorld = new bool[2];
        private Vector3 _headLocal = new Vector3(0f, 1.65f, 0f);
        private Quaternion _headLocalRotation = Quaternion.identity;

        public static RobotPlayer Create()
        {
            return new GameObject("Robot player").AddComponent<RobotPlayer>();
        }

        private IEnumerator Start()
        {
            while ((Player = FindFirstObjectByType<Player>()) == null)
                yield return null;
            _rig = Player.ControllerRig;
            foreach (Hand hand in Player.transform.root.GetComponentsInChildren<Hand>(true))
            {
                if (hand.IsLeftHand) LeftHand = hand;
                else RightHand = hand;
            }

            // Poses come from here, not from tracking.
            foreach (Transform tracked in new[] { _rig.CameraTransform, _rig.LeftControllerTransform, _rig.RightControllerTransform })
                foreach (Behaviour behaviour in tracked.GetComponents<Behaviour>())
                    if (behaviour != null && behaviour.GetType().Name.Contains("TrackedPoseDriver"))
                        behaviour.enabled = false;

            // Input must keep flowing with no window in front, as in an unattended run.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.RegisterLayout<RobotPad>();
            _pad = InputSystem.AddDevice<RobotPad>();

            foreach (Hand hand in new[] { LeftHand, RightHand })
            {
                HandInputReader reader = hand.HandInputReader;
                bool left = hand.IsLeftHand;
                Bind(reader.GripAction, left ? "<RobotPad>/leftGrip" : "<RobotPad>/rightGrip");
                Bind(reader.TriggerAction, left ? "<RobotPad>/leftTrigger" : "<RobotPad>/rightTrigger");
                Bind(reader.TriggerButtonAction, left ? "<RobotPad>/leftTrigger" : "<RobotPad>/rightTrigger");
                Bind(reader.PrimaryButtonAction, left ? "<RobotPad>/leftPrimary" : "<RobotPad>/rightPrimary");
                Bind(reader.SecondaryButtonAction, left ? "<RobotPad>/leftSecondary" : "<RobotPad>/rightSecondary");
            }
            InputReader movement = Player.InputReader != null ? Player.InputReader : Player.transform.root.GetComponentInChildren<InputReader>(true);
            if (movement != null)
            {
                Bind(movement.MoveAction, "<RobotPad>/move");
                Bind(movement.TurnAction, "<RobotPad>/turn");
                Bind(movement.CrouchAction, "<RobotPad>/turn");
                Bind(movement.RunAction, "<RobotPad>/run");
                Bind(movement.JumpAction, "<RobotPad>/jump");
            }

            ApplyPoses();
            yield return null;
            yield return null;
            Ready = true;
        }

        private static void Bind(InputActionReference reference, string path)
        {
            if (reference == null || reference.action == null)
                return;
            InputAction action = reference.action;
            foreach (InputBinding binding in action.bindings)
                if (binding.path == path)
                    return;
            bool wasEnabled = action.enabled;
            action.Disable();
            action.AddBinding(path);
            if (wasEnabled)
                action.Enable();
        }

        private void OnDestroy()
        {
            if (_pad != null && _pad.added)
                InputSystem.RemoveDevice(_pad);
        }

        // ---------------------------------------------------------------- buttons

        public void SetGrip(bool left, bool held)
        {
            if (left) _state.leftGrip = held ? 1f : 0f;
            else _state.rightGrip = held ? 1f : 0f;
        }

        public void SetTrigger(bool left, float amount)
        {
            if (left) _state.leftTrigger = amount;
            else _state.rightTrigger = amount;
        }

        /// <summary>A on the right controller, X on the left.</summary>
        public void SetPrimary(bool left, bool held)
        {
            if (left) _state.leftPrimary = held ? 1f : 0f;
            else _state.rightPrimary = held ? 1f : 0f;
        }

        /// <summary>B on the right controller, Y on the left.</summary>
        public void SetSecondary(bool left, bool held)
        {
            if (left) _state.leftSecondary = held ? 1f : 0f;
            else _state.rightSecondary = held ? 1f : 0f;
        }

        /// <summary>Left stick: x strafes, y walks forward.</summary>
        public void SetMove(Vector2 stick) => _state.move = stick;

        /// <summary>Right stick: x turns.</summary>
        public void SetTurn(float amount) => _state.turn = new Vector2(amount, 0f);
        // ---------------------------------------------------------------- poses

        public Transform Head => _rig.CameraTransform;

        /// <summary>Floor point under the head.</summary>
        public Vector3 Feet => new Vector3(Head.position.x, Player.transform.position.y, Head.position.z);

        private Quaternion Facing
        {
            get
            {
                Vector3 flat = Vector3.ProjectOnPlane(Head.forward, Vector3.up);
                return flat.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation(flat.normalized);
            }
        }

        public Vector3 BodyToWorld(Vector3 body) => FloorUnderHead() + Facing * body;
        public Quaternion BodyToWorld(Quaternion body) => Facing * body;

        private Vector3 FloorUnderHead()
        {
            // The head's height above the rig's floor is its local height, whatever the rig's scale.
            Vector3 head = Head.position;
            float floor = head.y - Head.localPosition.y * _rig.transform.lossyScale.y;
            return new Vector3(head.x, floor, head.z);
        }

        public void SetHandBody(bool left, Vector3 position, Quaternion rotation)
        {
            int i = left ? 0 : 1;
            _handInWorld[i] = false;
            _handPosition[i] = position;
            _handRotation[i] = rotation;
        }

        public void SetHandWorld(bool left, Vector3 position, Quaternion rotation)
        {
            int i = left ? 0 : 1;
            _handInWorld[i] = true;
            _handPosition[i] = position;
            _handRotation[i] = rotation;
        }

        public Vector3 HandTargetWorld(bool left)
        {
            int i = left ? 0 : 1;
            return _handInWorld[i] ? _handPosition[i] : BodyToWorld(_handPosition[i]);
        }

        public Quaternion HandTargetRotationWorld(bool left)
        {
            int i = left ? 0 : 1;
            return _handInWorld[i] ? _handRotation[i] : BodyToWorld(_handRotation[i]);
        }

        /// <summary>Look somewhere by turning the head only (the body follows a real turn of the stick).</summary>
        public void LookAt(Vector3 worldPoint)
        {
            Transform parent = Head.parent;
            Vector3 local = parent != null ? parent.InverseTransformPoint(worldPoint) : worldPoint;
            _headLocalRotation = Quaternion.LookRotation(local - _headLocal);
        }

        public void LookAhead() => _headLocalRotation = Quaternion.identity;

        /// <summary>Glide a hand to a world pose over some seconds.</summary>
        public IEnumerator MoveHandWorld(bool left, Vector3 position, Quaternion rotation, float seconds)
        {
            Vector3 fromPosition = HandTargetWorld(left);
            Quaternion fromRotation = HandTargetRotationWorld(left);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                SetHandWorld(left, Vector3.Lerp(fromPosition, position, k), Quaternion.Slerp(fromRotation, rotation, k));
                yield return null;
            }
            SetHandWorld(left, position, rotation);
        }

        /// <summary>Glide a hand to a body-space pose over some seconds.</summary>
        public IEnumerator MoveHandBody(bool left, Vector3 position, Quaternion rotation, float seconds)
        {
            int i = left ? 0 : 1;
            Quaternion inverse = Quaternion.Inverse(Facing);
            Vector3 fromPosition = _handInWorld[i] ? inverse * (_handPosition[i] - FloorUnderHead()) : _handPosition[i];
            Quaternion fromRotation = _handInWorld[i] ? inverse * _handRotation[i] : _handRotation[i];
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                SetHandBody(left, Vector3.Lerp(fromPosition, position, k), Quaternion.Slerp(fromRotation, rotation, k));
                yield return null;
            }
            SetHandBody(left, position, rotation);
        }

        /// <summary>
        /// Walk with the stick until the feet are within <paramref name="stopAt"/> metres of a
        /// point on the ground, or the time runs out. Returns by leaving the stick centred.
        /// </summary>
        public IEnumerator WalkTo(Vector3 worldPoint, float stopAt, float timeout = 8f, float stick = 0.6f)
        {
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                Vector3 away = Vector3.ProjectOnPlane(worldPoint - Head.position, Vector3.up);
                if (away.magnitude <= stopAt)
                    break;
                Vector3 local = Quaternion.Inverse(Facing) * away.normalized;
                SetMove(new Vector2(local.x, local.z) * stick);
                yield return null;
            }
            SetMove(Vector2.zero);
            yield return new WaitForSeconds(0.4f);
        }

        /// <summary>How far the feet are from a point, measured along the ground.</summary>
        public float GroundDistanceTo(Vector3 worldPoint) => Vector3.ProjectOnPlane(worldPoint - Head.position, Vector3.up).magnitude;

        private void Update()
        {
            if (_pad == null)
                return;
            InputSystem.QueueStateEvent(_pad, _state);
            ApplyPoses();
        }

        private void ApplyPoses()
        {
            _rig.CameraTransform.localPosition = _headLocal;
            _rig.CameraTransform.localRotation = _headLocalRotation;
            _rig.LeftControllerTransform.SetPositionAndRotation(HandTargetWorld(true), HandTargetRotationWorld(true));
            _rig.RightControllerTransform.SetPositionAndRotation(HandTargetWorld(false), HandTargetRotationWorld(false));
        }

        // ---------------------------------------------------------------- pictures

        /// <summary>Save what an onlooker would see: a camera off to one side, looking at the player.</summary>
        public void Snapshot(string path, Vector3 bodyOffset, float lookHeight = 1.1f, Vector3? lookAtWorld = null)
        {
            Vector3 from = BodyToWorld(bodyOffset);
            Vector3 target = lookAtWorld ?? (FloorUnderHead() + Vector3.up * lookHeight);
            Render(path, from, Quaternion.LookRotation(target - from), 60f);
        }

        /// <summary>Save what the player sees.</summary>
        public void SnapshotEyes(string path)
        {
            Render(path, Head.position, Head.rotation, 90f);
        }

        private void Render(string path, Vector3 position, Quaternion rotation, float fieldOfView)
        {
            var cameraObject = new GameObject("Robot snapshot camera");
            var cam = cameraObject.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 2000f;
            cam.cullingMask = _rig.CameraTransform.GetComponent<Camera>().cullingMask;
            cam.transform.SetPositionAndRotation(position, rotation);
            var target = new RenderTexture(960, 540, 24);
            cam.targetTexture = target;
            cam.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToJPG(85));
            cam.targetTexture = null;
            Destroy(cameraObject);
            Destroy(target);
            Destroy(image);
        }
    }
}
