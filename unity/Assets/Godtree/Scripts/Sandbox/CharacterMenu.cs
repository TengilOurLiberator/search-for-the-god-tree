using System;
using System.Collections.Generic;
using BIMOS;
using UnityEngine;
using UnityEngine.UI;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype of the character menu (decision 0013). Press B on the right
    /// controller to open or close it. It appears in front of you as a board:
    ///   left   your character, seen as in a mirror
    ///   right  your stats, and four equipment slots with arrow buttons you press with a hand
    /// Changing equipment changes the stats at once (see PlayerStats for what each one does).
    /// </summary>
    public sealed class CharacterMenu : MonoBehaviour
    {
        private Player _player;
        private Hand _rightHand;
        private bool _buttonWas;
        private GameObject _board;
        private Camera _mirrorCamera;
        private RenderTexture _mirrorTexture;
        private Text _statsText;
        private readonly Dictionary<EquipSlot, Text> _slotTexts = new Dictionary<EquipSlot, Text>();
        private Font _font;

        private void Update()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<Player>();
                if (_player == null)
                    return;
                foreach (Hand hand in _player.GetComponentsInChildren<Hand>(true))
                    if (!hand.IsLeftHand)
                        _rightHand = hand;
            }
            if (_rightHand == null)
                return;

            bool button = _rightHand.HandInputReader.SecondaryButton;
            if (button && !_buttonWas)
                Toggle();
            _buttonWas = button;

            if (_board != null && _board.activeSelf && _mirrorCamera != null)
            {
                // The mirror camera stands in front of the player, looking back at them.
                Transform head = _player.ControllerRig.CameraTransform;
                Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
                Vector3 from = head.position + flat * 2.1f - Vector3.up * 0.55f;
                _mirrorCamera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(head.position - Vector3.up * 0.6f - from));
            }
        }

        private void Toggle()
        {
            if (_board == null)
                Build();
            bool show = !_board.activeSelf || !_boardShownOnce;
            _boardShownOnce = true;
            _board.SetActive(show);
            _mirrorCamera.enabled = show;
            if (!show)
                return;

            Transform head = _player.ControllerRig.CameraTransform;
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            Vector3 position = head.position + flat * 0.62f - Vector3.up * 0.28f;
            _board.transform.SetPositionAndRotation(position, Quaternion.LookRotation(flat));
            Refresh();
        }

        private bool _boardShownOnce;

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _board = new GameObject("Character menu board");
            _board.transform.localScale = Vector3.one * 0.001f;
            var canvas = _board.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            _board.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 620f);
            Panel(_board.transform, Vector2.zero, new Vector2(1000f, 620f), new Color(0.13f, 0.13f, 0.23f, 0.95f));

            Label(_board.transform, "CHARACTER", new Vector2(-470f, 270f), new Vector2(940f, 50f), 40, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.42f));
            Label(_board.transform, "press B to close   |   push the arrows with your hand", new Vector2(-470f, -295f), new Vector2(940f, 30f), 22, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.9f));

            // Mirror view.
            _mirrorTexture = new RenderTexture(384, 576, 24);
            var mirrorObject = new GameObject("Mirror camera");
            mirrorObject.transform.SetParent(transform, false);
            _mirrorCamera = mirrorObject.AddComponent<Camera>();
            _mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _mirrorCamera.targetTexture = _mirrorTexture;
            _mirrorCamera.fieldOfView = 42f;
            _mirrorCamera.nearClipPlane = 0.1f;
            _mirrorCamera.cullingMask = ~LayerMask.GetMask("UI", "BIMOSMenu");
            var view = new GameObject("Mirror");
            view.transform.SetParent(_board.transform, false);
            var raw = view.AddComponent<RawImage>();
            raw.texture = _mirrorTexture;
            raw.uvRect = new Rect(1f, 0f, -1f, 1f); // flipped left-to-right, like a mirror
            Place(view.GetComponent<RectTransform>(), new Vector2(-470f, -250f), new Vector2(330f, 495f));

            _statsText = Label(_board.transform, "", new Vector2(-110f, 30f), new Vector2(580f, 215f), 30, TextAnchor.UpperLeft, new Color(1f, 0.97f, 0.92f));

            float y = -10f;
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                EquipSlot captured = slot;
                Button(_board.transform, "<", new Vector2(-110f, y - 58f), () => { PlayerStats.Instance.Cycle(captured, -1); Refresh(); });
                _slotTexts[slot] = Label(_board.transform, "", new Vector2(-35f, y - 58f), new Vector2(430f, 56f), 24, TextAnchor.MiddleLeft, new Color(1f, 0.97f, 0.92f));
                Button(_board.transform, ">", new Vector2(405f, y - 58f), () => { PlayerStats.Instance.Cycle(captured, 1); Refresh(); });
                y -= 64f;
            }
        }

        private void Refresh()
        {
            PlayerStats stats = PlayerStats.Instance;
            if (stats == null)
                return;
            _statsText.text =
                "<b>Vigor</b>     " + stats.Vigor.ToString("0") + "    <color=#9999BB>maximum health</color>\n" +
                "<b>Armour</b>    " + stats.Armour.ToString("0") + "\n" +
                "<b>Strength</b>  x" + stats.Strength.ToString("0.00") + "    <color=#9999BB>melee damage</color>\n" +
                "<b>Agility</b>   x" + stats.Agility.ToString("0.00") + "    <color=#9999BB>move speed</color>\n" +
                "<b>Focus</b>     x" + stats.Focus.ToString("0.00") + "    <color=#9999BB>spell power</color>";
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
            {
                EquipmentItem item = stats.Equipped(slot);
                _slotTexts[slot].text = "<b>" + slot + ":</b> " + (item == null ? "<color=#9999BB>nothing</color>" : item.name + "\n<size=19><color=#FFD86B>" + item.Describe() + "</color></size>");
            }
        }

        // ------------------------------------------------------------ small UI helpers

        private static void Place(RectTransform rect, Vector2 bottomLeft, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = bottomLeft;
            rect.sizeDelta = size;
        }

        private static Image Panel(Transform parent, Vector2 centre, Vector2 size, Color color)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centre;
            rect.sizeDelta = size;
            return image;
        }

        private Text Label(Transform parent, string content, Vector2 bottomLeft, Vector2 size, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            Place(go.GetComponent<RectTransform>(), bottomLeft, size);
            return text;
        }

        private void Button(Transform parent, string caption, Vector2 bottomLeft, Action onPress)
        {
            var go = new GameObject("Button " + caption);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            Place(go.GetComponent<RectTransform>(), bottomLeft, new Vector2(64f, 56f));
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(32f, 28f, 0f);
            collider.size = new Vector3(64f, 56f, 40f);
            var poke = go.AddComponent<PokeButton>();
            poke.face = image;
            poke.Pressed = onPress;
            Label(go.transform, caption, new Vector2(-32f, -28f), new Vector2(64f, 56f), 36, TextAnchor.MiddleCenter, Color.white);
        }
    }
}
