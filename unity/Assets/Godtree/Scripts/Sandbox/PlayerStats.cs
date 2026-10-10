using System;
using System.Collections.Generic;
using BIMOS;
using UnityEngine;

namespace Godtree.Sandbox
{
    public enum EquipSlot { Head, Body, Hands, Charm }

    /// <summary>One piece of sandbox equipment and what it changes.</summary>
    [Serializable]
    public sealed class EquipmentItem
    {
        public string name;
        public EquipSlot slot;
        public float vigor;          // added to maximum health
        public float armour;         // added to armour
        public float strength;       // added to the melee multiplier (0.25 = +25 %)
        public float agility;        // added to the movement multiplier
        public float focus;          // added to the spell multiplier

        public string Describe()
        {
            var parts = new List<string>();
            if (vigor != 0) parts.Add((vigor > 0 ? "+" : "") + vigor.ToString("0") + " vigor");
            if (armour != 0) parts.Add((armour > 0 ? "+" : "") + armour.ToString("0") + " armour");
            if (strength != 0) parts.Add((strength > 0 ? "+" : "") + (strength * 100).ToString("0") + "% strength");
            if (agility != 0) parts.Add((agility > 0 ? "+" : "") + (agility * 100).ToString("0") + "% agility");
            if (focus != 0) parts.Add((focus > 0 ? "+" : "") + (focus * 100).ToString("0") + "% focus");
            return parts.Count == 0 ? "" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Sandbox prototype of character stats and equipment (decision 0013). Not the final
    /// role-playing system. What each stat really does today:
    ///   Strength  multiplies the damage number of a melee hit (the hit itself is still a
    ///             real collision; strength does not move the weapon for you)
    ///   Focus     multiplies spell power: bolt speed and weight, push and lift force
    ///   Agility   multiplies walking and running speed
    ///   Vigor     maximum health, shown only: nothing attacks the player yet
    ///   Armour    shown only, for the same reason
    /// </summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        public event Action Changed;

        public float Vigor { get; private set; } = 100f;
        public float Armour { get; private set; }
        public float Strength { get; private set; } = 1f;
        public float Agility { get; private set; } = 1f;
        public float Focus { get; private set; } = 1f;

        public static float StrengthNow => Instance != null ? Instance.Strength : 1f;
        public static float FocusNow => Instance != null ? Instance.Focus : 1f;

        /// <summary>Health now; Vigor is the most it can be. Comes back slowly when nothing has hit for a while.</summary>
        public float Health { get; private set; } = 100f;

        /// <summary>Raised when the player is hurt: damage taken after armour, and where it came from.</summary>
        public event Action<float, Vector3> Hurt;

        public int TimesDefeated { get; private set; }

        private float _lastHurt = -100f;

        /// <summary>A blow against the player. Armour takes a point off per point, never below a fifth of the blow.</summary>
        public void TakeDamage(float amount, Vector3 from)
        {
            float taken = Mathf.Max(amount * 0.2f, amount - Armour);
            Health = Mathf.Max(0f, Health - taken);
            _lastHurt = Time.time;
            Hurt?.Invoke(taken, from);
            if (Health <= 0f)
            {
                // Sandbox: no death yet. Count it and stand the player back up at full health.
                TimesDefeated++;
                Health = Vigor;
            }
        }

        private void LateUpdate()
        {
            Health = Mathf.Min(Health, Vigor);
            if (Time.time - _lastHurt > 6f && Health < Vigor)
                Health = Mathf.Min(Vigor, Health + Vigor * 0.08f * Time.deltaTime);
        }

        private readonly Dictionary<EquipSlot, List<EquipmentItem>> _choices = new Dictionary<EquipSlot, List<EquipmentItem>>();
        private readonly Dictionary<EquipSlot, int> _equipped = new Dictionary<EquipSlot, int>();
        private Player _player;
        private float _baseWalk, _baseRun;

        private void Awake()
        {
            Instance = this;
            Add(EquipSlot.Head, "Leather Cap", vigor: 15, armour: 1);
            Add(EquipSlot.Head, "Iron Helm", vigor: 10, armour: 4, agility: -0.05f);
            Add(EquipSlot.Head, "Weaver's Hood", focus: 0.20f);
            Add(EquipSlot.Body, "Padded Jack", vigor: 25, armour: 2);
            Add(EquipSlot.Body, "Chain Shirt", vigor: 20, armour: 6, agility: -0.10f);
            Add(EquipSlot.Body, "Mote-silk Robe", vigor: -10, focus: 0.35f);
            Add(EquipSlot.Hands, "Iron Bracers", strength: 0.25f);
            Add(EquipSlot.Hands, "Runner's Wraps", agility: 0.15f);
            Add(EquipSlot.Hands, "Knot-rings", focus: 0.25f);
            Add(EquipSlot.Charm, "Husk Charm", vigor: 30);
            Add(EquipSlot.Charm, "Thornback Tusk", strength: 0.15f, agility: 0.10f);
            Add(EquipSlot.Charm, "Mote Lantern", focus: 0.20f);
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
                _equipped[slot] = -1;
            Recalculate();
        }

        private void Add(EquipSlot slot, string itemName, float vigor = 0, float armour = 0, float strength = 0, float agility = 0, float focus = 0)
        {
            if (!_choices.ContainsKey(slot))
                _choices[slot] = new List<EquipmentItem>();
            _choices[slot].Add(new EquipmentItem { name = itemName, slot = slot, vigor = vigor, armour = armour, strength = strength, agility = agility, focus = focus });
        }

        /// <summary>The item in a slot, or null for nothing.</summary>
        public EquipmentItem Equipped(EquipSlot slot)
        {
            int index = _equipped[slot];
            return index < 0 ? null : _choices[slot][index];
        }

        /// <summary>Steps a slot to its next (+1) or previous (-1) choice, passing through "nothing".</summary>
        public void Cycle(EquipSlot slot, int step)
        {
            int count = _choices[slot].Count;
            int index = _equipped[slot] + step;
            if (index >= count) index = -1;
            if (index < -1) index = count - 1;
            _equipped[slot] = index;
            Recalculate();
        }

        private void Recalculate()
        {
            Vigor = 100f; Armour = 0f; Strength = 1f; Agility = 1f; Focus = 1f;
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                EquipmentItem item = Equipped(slot);
                if (item == null) continue;
                Vigor += item.vigor; Armour += item.armour; Strength += item.strength; Agility += item.agility; Focus += item.focus;
            }
            ApplyAgility();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<Player>();
                if (_player != null)
                {
                    _baseWalk = _player.WalkSpeed;
                    _baseRun = _player.RunSpeed;
                    ApplyAgility();
                }
            }
        }

        private void ApplyAgility()
        {
            if (_player == null)
                return;
            _player.WalkSpeed = _baseWalk * Agility;
            _player.RunSpeed = _baseRun * Agility;
        }
    }
}
