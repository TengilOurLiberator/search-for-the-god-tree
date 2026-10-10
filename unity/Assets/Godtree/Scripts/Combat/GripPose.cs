using BIMOS;
using UnityEngine;

namespace Godtree.Combat
{
    public enum HoldStyle
    {
        /// <summary>Handle in the fist, the long part coming out of the thumb side: sword, dagger, axe, spear.</summary>
        Blade,
        /// <summary>Held at the middle, upright in the fist.</summary>
        Bow,
        /// <summary>Held at the centre of the back, face away from the holder.</summary>
        Shield
    }

    /// <summary>
    /// Where and how an object sits in the hand. When it is grabbed, the object is moved
    /// into this pose in the hand, so the player never has to pick it up "the right way".
    /// The axes are worked out from the model's shape when the sandbox is built.
    /// </summary>
    public sealed class GripPose : MonoBehaviour
    {
        public HoldStyle style = HoldStyle.Blade;

        [Tooltip("Point on the object, in its own space, that goes to the palm.")]
        public Vector3 localGripPoint;

        [Tooltip("Blade and bow: from the grip toward the tip. Shield: the outward face normal.")]
        public Vector3 localMainAxis = Vector3.up;

        [Tooltip("A direction across the object: a blade's edge, the way a bow curves.")]
        public Vector3 localSideAxis = Vector3.forward;

        [Tooltip("How far the main axis is tilted up from the controller's pointing direction, in degrees.")]
        public float tiltDegrees = 35f;

        /// <summary>Moves the object into the hand in its proper hold.</summary>
        public void SnapInto(Hand hand)
        {
            Player player = hand.GetComponentInParent<Player>();
            Transform controller = hand.IsLeftHand ? player.ControllerRig.LeftControllerTransform : player.ControllerRig.RightControllerTransform;

            Vector3 mainWorld = style == HoldStyle.Shield
                ? controller.forward
                : Quaternion.AngleAxis(-tiltDegrees, controller.right) * controller.forward;
            Vector3 sideWorld = Vector3.ProjectOnPlane(controller.up, mainWorld);
            if (sideWorld.sqrMagnitude < 1e-4f)
                sideWorld = Vector3.ProjectOnPlane(controller.forward, mainWorld);

            Quaternion rotation = Quaternion.LookRotation(mainWorld, sideWorld.normalized) *
                                  Quaternion.Inverse(Quaternion.LookRotation(localMainAxis, localSideAxis));
            Vector3 position = hand.PalmTransform.position - rotation * Vector3.Scale(localGripPoint, transform.lossyScale);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
                body.rotation = rotation;
            }
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
