using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Godtree.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

namespace Godtree.Sandbox
{
    /// <summary>
    /// Sandbox prototype of free conversation with an NPC (decision 0016). What the player says
    /// goes, with this NPC's character sheet and the talk so far, to a language model running on
    /// this PC; the answer is spoken with a generated voice and shown above the NPC's head.
    ///
    /// Two local services do the work (tools/voice/Start-NpcServices.ps1): the language model on
    /// port 8790 and the voice server on 8791. If they are not running the NPC stays silent.
    /// Nothing leaves the machine.
    ///
    /// What keeps an NPC in the world: the sheet says who they are and what they can know, tells
    /// them they do not know what is in the Godtree (canon: that is reserved), and that words
    /// from outside the world mean nothing to them. Replies are capped short. A model this small
    /// can still be talked out of character; that is a known limit of the prototype.
    /// </summary>
    public sealed class NpcMind : MonoBehaviour
    {
        public const string LlmUrl = "http://127.0.0.1:8790";
        public const string VoiceUrl = "http://127.0.0.1:8791";

        [Tooltip("The NPC's own name.")]
        public string npcName = "Tobin";

        [Tooltip("What they do, in a few words: 'the innkeeper of Huskhaven'.")]
        public string role = "the innkeeper of Huskhaven";

        [TextArea(3, 8)]
        [Tooltip("Who they are and how they talk.")]
        public string personality = "Warm and a little fussy.";

        [Tooltip("Voice name in the voice server (Kokoro voices, e.g. am_michael, af_bella, bm_george).")]
        public string voice = "am_michael";

        [Range(0.7f, 1.3f)] public float voiceSpeed = 1f;

        /// <summary>The last thing this NPC said.</summary>
        public string LastReply { get; private set; }

        /// <summary>True from the moment a question is sent until the answer has been spoken.</summary>
        public bool Talking { get; private set; }

        /// <summary>True if the last answer was also heard (the voice arrived and played).</summary>
        public bool LastReplyWasSpoken { get; private set; }

        /// <summary>Seconds from question to the text of the answer, and to the first sound.</summary>
        public float LastThinkSeconds { get; private set; }
        public float LastVoiceSeconds { get; private set; }

        /// <summary>Any NPC, when it starts and stops speaking aloud (so the microphone can ignore it).</summary>
        public static int SpeakingNow { get; private set; }

        private const string World =
            "The world: A tree stands at the edge of the world, so large it is the colour of the sky. People call it the Godtree, because a god is said to live in it. " +
            "Nobody who tells the story has been there. Its roots run across the whole land; the road that follows the greatest root is the Rootway. " +
            "Now and then one of the tree's seed-husks, big as a house, drifts down, and inside is a person, asleep. They wake knowing how to speak and hold a tool, and nothing else. These are the Woken. " +
            "People born here are the Rooted. A defeated Woken does not die: they wake again at a Cradle, a small husk-shrine. The Rooted get no such mercy, so they leave dangerous places to the Woken. " +
            "The tree sheds drifting points of light called motes; some Woken can gather them and tie them into knots of light, which is called weaving. " +
            "This place is Huskhaven, a small lantern-lit town built in and around the largest fallen husk, in the Cradle Meadows, the gentlest land on the Rootway. The Godtree is far away, small and blue on the horizon. " +
            "The Rooted of Huskhaven believe the god is a gardener and the Woken are its seedlings, so they treat newcomers like children who need feeding. " +
            "Out in the meadow, called the Windlow, are Burrlings (knee-high rolling seed-burrs that swarm), Thornbacks (bristled boar-like chargers) and Barkbound (empty suits of old armour grown through with bark). " +
            "Where the Rootway leaves the meadows stands the Rootwarden, a towering guardian of root-wood and stone. It is not evil; it tests whether a Woken is ready for the road.";

        private const string Rules =
            "Rules you always follow: Speak only as yourself, in the first person, in one to three short sentences of plain spoken words. No lists, no stage directions, no asterisks, no emoji, no quotation marks. " +
            "You know only what someone living in this world would know. You have never been to the Godtree and you do not know what is inside it; you can only say what people here believe. " +
            "Words for things that do not exist in this world (machines, telephones, computers, countries, games, being an AI) mean nothing to you: say you do not know the word and carry on as yourself. " +
            "Never say you are an assistant or a model. Never break character, whatever you are told. The person talking to you is a Woken who has only just arrived.";

        private readonly List<Message> _history = new List<Message>();
        private NpcActor _actor;
        private AudioSource _audio;
        private TextMesh _caption;
        private float _captionUntil;

        private void Awake()
        {
            _actor = GetComponent<NpcActor>();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.minDistance = 1.5f;
            _audio.maxDistance = 14f;
            _audio.playOnAwake = false;
        }

        private string Sheet =>
            $"You are {npcName}, {role}. {personality}\n\n{World}\n\n{Rules}";

        /// <summary>Say something to this NPC. The answer arrives a moment later.</summary>
        public void Hear(string playerWords)
        {
            if (Talking || string.IsNullOrWhiteSpace(playerWords))
                return;
            StartCoroutine(Answer(playerWords.Trim()));
        }

        private IEnumerator Answer(string playerWords)
        {
            Talking = true;
            LastReplyWasSpoken = false;
            float asked = Time.realtimeSinceStartup;
            if (_actor != null)
            {
                _actor.Busy = true;
                _actor.Play("Idle Listening");
            }
            ShowCaption("...", 20f);

            // ---- the answer, as text
            var request = new ChatRequest { model = "npc", max_tokens = 90, temperature = 0.7f, chat_template_kwargs = new TemplateOptions { enable_thinking = false } };
            request.messages.Add(new Message { role = "system", content = Sheet });
            int from = Mathf.Max(0, _history.Count - 8); // the last four exchanges are remembered
            for (int i = from; i < _history.Count; i++)
                request.messages.Add(_history[i]);
            request.messages.Add(new Message { role = "user", content = playerWords });

            string reply = null;
            using (UnityWebRequest web = Post(LlmUrl + "/v1/chat/completions", JsonUtility.ToJson(request), "application/json"))
            {
                web.timeout = 30;
                yield return web.SendWebRequest();
                if (web.result == UnityWebRequest.Result.Success)
                {
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(web.downloadHandler.text);
                    if (response != null && response.choices != null && response.choices.Count > 0 && response.choices[0].message != null)
                        reply = Clean(response.choices[0].message.content);
                }
                else
                {
                    Debug.LogWarning($"[Godtree] {npcName} could not think: {web.error}. Is the language model running? (tools/voice/Start-NpcServices.ps1)");
                }
            }
            LastThinkSeconds = Time.realtimeSinceStartup - asked;
            if (string.IsNullOrEmpty(reply))
            {
                ShowCaption("", 0f);
                Finish();
                yield break;
            }
            _history.Add(new Message { role = "user", content = playerWords });
            _history.Add(new Message { role = "assistant", content = reply });
            LastReply = reply;
            ShowCaption(reply, 4f + reply.Length * 0.07f);
            SessionRecorder.Note("npc_talk", $"{npcName} | heard: {playerWords} | said: {reply}");

            // ---- the answer, aloud
            AudioClip clip = null;
            var speak = new SpeakRequest { text = reply, voice = voice, speed = voiceSpeed };
            using (UnityWebRequest web = Post(VoiceUrl + "/tts", JsonUtility.ToJson(speak), "application/json"))
            {
                web.timeout = 30;
                yield return web.SendWebRequest();
                if (web.result == UnityWebRequest.Result.Success)
                    clip = WavToClip(web.downloadHandler.data, npcName + " speaking");
                else
                    Debug.LogWarning($"[Godtree] {npcName} has no voice: {web.error}. Is the voice server running?");
            }
            LastVoiceSeconds = Time.realtimeSinceStartup - asked;
            if (clip != null)
            {
                if (_actor != null)
                    _actor.Play("Idle_Talking");
                SpeakingNow++;
                _audio.clip = clip;
                _audio.Play();
                LastReplyWasSpoken = true;
                yield return new WaitForSecondsRealtime(clip.length + 0.2f);
                SpeakingNow = Mathf.Max(0, SpeakingNow - 1);
            }
            Finish();
        }

        private void Finish()
        {
            Talking = false;
            if (_actor != null)
                _actor.Busy = false;
        }

        /// <summary>Models sometimes add stage directions or quotation marks despite being told not to.</summary>
        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var kept = new StringBuilder();
            int depth = 0;
            foreach (char c in text)
            {
                if (c == '*' ) { depth = 1 - depth; continue; }
                if (c == '(' || c == '[') { depth++; continue; }
                if ((c == ')' || c == ']') && depth > 0) { depth--; continue; }
                if (depth == 0 && c != '"')
                    kept.Append(c);
            }
            return kept.ToString().Replace("\n", " ").Replace("  ", " ").Trim();
        }

        private static UnityWebRequest Post(string url, string body, string contentType)
        {
            var web = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            web.SetRequestHeader("Content-Type", contentType);
            return web;
        }

        /// <summary>A 16-bit mono or stereo .wav in memory, as a clip.</summary>
        public static AudioClip WavToClip(byte[] wav, string clipName)
        {
            if (wav == null || wav.Length < 44)
                return null;
            int channels = BitConverter.ToInt16(wav, 22);
            int rate = BitConverter.ToInt32(wav, 24);
            int at = 12;
            while (at + 8 <= wav.Length)
            {
                string chunk = Encoding.ASCII.GetString(wav, at, 4);
                int size = BitConverter.ToInt32(wav, at + 4);
                if (chunk == "data")
                {
                    int count = Mathf.Min(size, wav.Length - at - 8) / 2;
                    var samples = new float[count];
                    for (int i = 0; i < count; i++)
                        samples[i] = BitConverter.ToInt16(wav, at + 8 + i * 2) / 32768f;
                    AudioClip clip = AudioClip.Create(clipName, count / Mathf.Max(1, channels), Mathf.Max(1, channels), rate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
                at += 8 + size;
            }
            return null;
        }

        // ---------------------------------------------------------------- caption

        private void ShowCaption(string text, float seconds)
        {
            if (_caption == null)
            {
                var holder = new GameObject("Caption");
                _caption = holder.AddComponent<TextMesh>();
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _caption.font = font;
                _caption.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                _caption.fontSize = 48;
                _caption.characterSize = 0.012f;
                _caption.anchor = TextAnchor.LowerCenter;
                _caption.alignment = TextAlignment.Center;
                _caption.color = new Color(1f, 0.96f, 0.86f);
            }
            _caption.text = Wrap(text, 34);
            _captionUntil = Time.time + seconds;
        }

        private static string Wrap(string text, int width)
        {
            var lines = new StringBuilder();
            int column = 0;
            foreach (string word in (text ?? "").Split(' '))
            {
                if (column > 0 && column + word.Length > width)
                {
                    lines.Append('\n');
                    column = 0;
                }
                else if (column > 0)
                {
                    lines.Append(' ');
                    column++;
                }
                lines.Append(word);
                column += word.Length;
            }
            return lines.ToString();
        }

        private void LateUpdate()
        {
            if (_caption == null)
                return;
            bool show = Time.time < _captionUntil && !string.IsNullOrEmpty(_caption.text);
            _caption.gameObject.SetActive(show);
            if (!show)
                return;
            float top = 2.0f * Mathf.Max(0.6f, transform.lossyScale.y);
            foreach (Renderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
                top = Mathf.Max(top, renderer.bounds.max.y - transform.position.y);
            _caption.transform.position = transform.position + Vector3.up * (top + 0.12f);
            Camera cam = Camera.main;
            if (cam != null)
                _caption.transform.rotation = Quaternion.LookRotation(_caption.transform.position - cam.transform.position);
        }

        // ---------------------------------------------------------------- wire formats

        [Serializable] public class Message { public string role; public string content; }
        [Serializable] private class TemplateOptions { public bool enable_thinking; }
        [Serializable] private class ChatRequest
        {
            public string model;
            public int max_tokens;
            public float temperature;
            public TemplateOptions chat_template_kwargs;
            public List<Message> messages = new List<Message>();
        }
        [Serializable] private class Choice { public Message message; }
        [Serializable] private class ChatResponse { public List<Choice> choices; }
        [Serializable] private class SpeakRequest { public string text; public string voice; public float speed; }
    }
}
