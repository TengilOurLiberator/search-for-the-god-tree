using BIMOS;
using UnityEngine;

namespace Godtree.Combat
{
    /// <summary>
    /// A BIMOS grab that brings the object to the hand in its <see cref="GripPose"/>,
    /// instead of making the hand conform to wherever the object was touched.
    /// </summary>
    [RequireComponent(typeof(GripPose))]
    public sealed class SnappedGrab : Grab
    {
        public override void OnGrab(Hand hand)
        {
            GetComponent<GripPose>().SnapInto(hand);
            base.OnGrab(hand);
        }
    }
}