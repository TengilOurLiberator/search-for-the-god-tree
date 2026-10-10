using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Godtree.Sandbox
{
    /// <summary>
    /// The player's voice: listens on the default microphone (the headset's), finds where a
    /// sentence starts and stops by loudness, sends it to the local voice server to be turned
    /// into words, and gives the words to the NPC the player is standing with and looking at.
    /// There is no button: stand near someone, look at them, and speak.
    ///
    /// It does not listen while an NPC is speaking, so an NPC's voice coming out of the headset
    /// is not taken for the player's. With no microphone, or no voice server, it does nothing.
    /// </summary>
    public sealed class VoiceInput : MonoBehaviour
    {
        [Tooltip("Loudness (0 to 1) above which sound counts as speech. Raise it in a noisy room.")]
        public float speechLevel = 0.02f;

        [Tooltip("Quiet seconds that end a sentence.")]
        public float endSilence = 0.8f;

        /// <summary>What was last heard, and who it was given to.</summary>
        public string LastHeard { get; private set; }
        public string LastListener { get; private set; }
        public bool HasMicrophone { get; private set; }
        public int Utterances { get; private set; }

        private const int Rate = 16000;
        private AudioClip _mic;
        private string _device;
        private int _readAt;
        private readonly System.Collections.Generic.List<float> _speech = new System.Collections.Generic.List<float>();
        private bool _speaking;
        private float _quietFor, _spokeFor;
        private bool _busy;

        private void Start()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.Log("[Godtree] No microphone: NPCs can still be spoken to by script, not by voice.");
                return;
            }
            _device = Microphone.devices[0];
            _mic = Microphone.Start(_device, true, 10, Rate);
            HasMicrophone = _mic != null;
            if (HasMicrophone)
                Debug.Log("[Godtree] Listening on microphone: " + _device);
        }

        private void OnDestroy()
        {
            if (HasMicrophone)
                Microphone.End(_device);
        }

        private void Update()
        {
            if (!HasMicrophone)
                return;
            int writeAt = Microphone.GetPosition(_device);
            int available = writeAt - _readAt;
            if (available < 0)
                available += _mic.samples;
            if (available < Rate / 20)
                return; // work in pieces of at least a twentieth of a second
            var piece = new float[available];
            _mic.GetData(piece, _readAt);
            _readAt = writeAt;

            // An NPC is talking, or the last sentence is still being dealt with: do not listen.
            if (NpcMind.SpeakingNow > 0 || _busy)
            {
                _speaking = false;
                _speech.Clear();
                return;
            }

            double sum = 0;
            foreach (float sample in piece)
                sum += sample * sample;
            float level = Mathf.Sqrt((float)(sum / piece.Length));
            float seconds = piece.Length / (float)Rate;

            if (level >= speechLevel)
            {
                _speaking = true;
                _quietFor = 0f;
                _spokeFor += seconds;
            }
            else if (_speaking)
            {
                _quietFor += seconds;
            }
            if (_speaking)
            {
                _speech.AddRange(piece);
                bool tooLong = _speech.Count > Rate * 12;
                if (_quietFor >= endSilence || tooLong)
                {
                    if (_spokeFor >= 0.3f)
                        StartCoroutine(Understand(_speech.ToArray()));
                    _speech.Clear();
                    _speaking = false;
                    _spokeFor = 0f;
                    _quietFor = 0f;
                }
            }
        }

        /// <summary>The NPC the player is talking to: near, and most nearly straight ahead.</summary>
        public static NpcMind Listener()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return null;
            NpcMind best = null;
            float bestScore = 0.35f; // must be at least roughly in front
            foreach (NpcMind mind in FindObjectsByType<NpcMind>(FindObjectsSortMode.None))
            {
                Vector3 to = mind.transform.position + Vector3.up * 1.4f - cam.transform.position;
                if (to.magnitude > 4.5f)
                    continue;
                float score = Vector3.Dot(cam.transform.forward, to.normalized) - to.magnitude * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = mind;
                }
            }
            return best;
        }

        /// <summary>Turn recorded speech into words and hand them to whoever is listening.</summary>
        public IEnumerator Understand(float[] samples)
        {
            _busy = true;
            NpcMind listener = Listener();
            if (listener != null)
            {
                using (var web = new UnityWebRequest(NpcMind.VoiceUrl + "/stt", "POST"))
                {
                    web.uploadHandler = new UploadHandlerRaw(ToWav(samples, Rate));
                    web.downloadHandler = new DownloadHandlerBuffer();
                    web.SetRequestHeader("Content-Type", "audio/wav");
                    web.timeout = 30;
                    yield return web.SendWebRequest();
                    if (web.result == UnityWebRequest.Result.Success)
                    {
                        string text = JsonUtility.FromJson<Heard>(web.downloadHandler.text).text;
                        // Whisper writes this for silence or noise; it is not something the player said.
                        if (!string.IsNullOrWhiteSpace(text) && text.Trim().Length > 1 && !text.Contains("[BLANK_AUDIO]"))
                        {
                            LastHeard = text.Trim();
                            LastListener = listener.npcName;
                            Utterances++;
                            listener.Hear(LastHeard);
                        }
                    }
                }
            }
            _busy = false;
        }

        public static byte[] ToWav(float[] samples, int rate)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + bytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(bytes);
                foreach (float sample in samples)
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * 32767f));
                return stream.ToArray();
            }
        }

        [Serializable] private class Heard { public string text; }
    }
}
