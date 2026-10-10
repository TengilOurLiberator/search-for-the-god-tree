using UnityEngine;

namespace Godtree.Sandbox
{
    public enum SpellElement { Ember, Frost, Storm }

    /// <summary>
    /// Marks a staff or wand as something spells are cast through (decision 0016): it has an
    /// element and a tier. Tier 1 knows one spell, tier 2 two, tier 3 three; higher tiers also
    /// make every spell bigger. Which spell comes out depends on how long TRIGGER is held.
    /// </summary>
    public sealed class SpellFocus : MonoBehaviour
    {
        public SpellElement element = SpellElement.Ember;

        [Range(1, 3)] public int tier = 1;

        [Tooltip("Where spells leave the focus, in this object's own space: its far end.")]
        public Vector3 localTip = Vector3.forward * 0.3f;

        [Tooltip("The way spells fly from the tip, in this object's own space.")]
        public Vector3 localAim = Vector3.forward;

        public Vector3 Tip => transform.TransformPoint(localTip);
        public Vector3 Aim => transform.TransformDirection(localAim).normalized;

        public static Color ColourOf(SpellElement element)
        {
            switch (element)
            {
                case SpellElement.Ember: return new Color(1f, 0.45f, 0.12f);
                case SpellElement.Frost: return new Color(0.55f, 0.85f, 1f);
                default: return new Color(0.75f, 0.6f, 1f);
            }
        }

        /// <summary>The spell a hold of this many seconds gives on this focus: 1, 2 or 3.</summary>
        public int SpellFor(float heldSeconds)
        {
            int wanted = heldSeconds >= 1.7f ? 3 : heldSeconds >= 0.7f ? 2 : 1;
            return Mathf.Min(wanted, tier);
        }

        public static string NameOf(SpellElement element, int spell)
        {
            switch (element)
            {
                case SpellElement.Ember: return spell == 1 ? "Ember bolt" : spell == 2 ? "Fireball" : "Flame wave";
                case SpellElement.Frost: return spell == 1 ? "Frost shard" : spell == 2 ? "Frost burst" : "Winter's grasp";
                default: return spell == 1 ? "Spark" : spell == 2 ? "Chain lightning" : "Thunderclap";
            }
        }
    }
}
