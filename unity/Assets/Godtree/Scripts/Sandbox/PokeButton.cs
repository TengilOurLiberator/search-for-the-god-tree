using System;
using BIMOS;
using UnityEngine;
using UnityEngine.UI;

namespace Godtree.Sandbox
{
    /// <summary>
    /// A button you press by pushing a hand into it. No pointer, no laser: your hands are
    /// the interface. Needs a trigger collider on the same object.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PokeButton : MonoBehaviour
    {
        public Action Pressed;
        public Image face;

        private static readonly Color Idle = new Color(0.23f, 0.23f, 0.43f), Down = new Color(1f, 0.85f, 0.42f);
        private float _nextPress, _litUntil;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < _nextPress)
                return;
            // Only the player's own physics hands count.
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.GetComponentInParent<Player>() == null)
                return;
            bool isHand = false;
            foreach (Hand hand in body.GetComponentInParent<Player>().GetComponentsInChildren<Hand>(true))
                if (hand.PhysicsHandCollider == other || (hand.PhysicsHandTransform != null && other.transform.IsChildOf(hand.PhysicsHandTransform)))
                    isHand = true;
            if (!isHand)
                return;

            _nextPress = Time.time + 0.45f;
            _litUntil = Time.time + 0.2f;
            Pressed?.Invoke();
        }

        private void Update()
        {
            if (face != null)
                face.color = Time.time < _litUntil ? Down : Idle;
        }
    }
}
