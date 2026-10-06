using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The protagonist's inner voice: short first-person lines as an italic subtitle with a soft shadow, low on the
    /// screen (above the story narration box), on the player's side of the glass rather than on the monitor.
    /// Lines are queued and shown one at a time: fade in, type out, hold, fade out. Text keeps both languages and
    /// follows a language switch while it is on screen. Never takes input.
    ///
    /// Usage:
    ///   InnerVoice.Say("手腕好酸。", "My wrist aches.");          // queue a line (default hold)
    ///   InnerVoice.Say(zh, en, 4f);                              // hold for 4 s after typing
    ///   yield return InnerVoice.SayAndWait(zh, en);              // in a cutscene: wait until it has faded out
    ///   InnerVoice.Busy / InnerVoice.Pending                     // something on screen / lines waiting
    ///   InnerVoice.Clear();                                      // drop the queue and fade out now (skips)
    /// It creates itself on first use and needs no setup.
    /// </summary>
    public sealed class InnerVoice : MonoBehaviour
    {
        public const float DefaultSeconds = 2.8f;
        const float FadeIn = .25f, FadeOut = .45f, PerChar = .03f, Gap = .35f;

        struct Line { public string zh, en; public float seconds; public int ticket; public Action shown; }

        static InnerVoice instance;
        static int nextTicket, lastFinished;

        readonly Queue<Line> queue = new Queue<Line>();
        /// <summary>A line a cutscene interrupted; it plays again first once the screen is clear.</summary>
        Line? resume;
        CanvasGroup group;
        TMP_Text text, shadow;
        Line current;
        bool speaking, english;
        Coroutine routine;

        /// <summary>
        /// While this returns true (a story cutscene, the prologue or a flash card covers the screen), waiting lines
        /// stay queued and a line on screen is taken back to be said again afterwards. Set by the story presenter.
        /// </summary>
        public static Func<bool> Hold;

        static bool Held
        {
            get
            {
                try { return Hold != null && Hold(); }
                catch (Exception e) { Debug.LogException(e); return false; }
            }
        }

        /// <summary>
        /// Queues a line. <paramref name="seconds"/> is how long it stays after it has been typed out.
        /// <paramref name="shown"/> runs once the whole line has been on screen, i.e. the player could read it.
        /// </summary>
        public static int Say(string zh, string en, float seconds = DefaultSeconds, Action shown = null)
        {
            if (string.IsNullOrEmpty(zh) && string.IsNullOrEmpty(en)) return lastFinished;
            var v = Instance();
            var line = new Line { zh = zh ?? en, en = string.IsNullOrEmpty(en) ? zh : en, seconds = Mathf.Max(.5f, seconds), ticket = ++nextTicket, shown = shown };
            v.queue.Enqueue(line);
            if (v.routine == null) v.routine = v.StartCoroutine(v.Run());
            return line.ticket;
        }

        /// <summary>Queues a line and waits until it has faded out (or was cleared). For cutscene coroutines.</summary>
        public static IEnumerator SayAndWait(string zh, string en, float seconds = DefaultSeconds)
        {
            int ticket = Say(zh, en, seconds);
            while (instance != null && lastFinished < ticket) yield return null;
        }

        /// <summary>A line is on screen or waiting.</summary>
        public static bool Busy => instance != null && (instance.speaking || instance.queue.Count > 0 || instance.resume.HasValue);
        public static int Pending => instance != null ? instance.queue.Count : 0;

        /// <summary>Drops every waiting line and fades the current one out quickly.</summary>
        public static void Clear()
        {
            if (instance == null) return;
            instance.queue.Clear();
            instance.resume = null;
            lastFinished = nextTicket;
            if (instance.routine != null) { instance.StopCoroutine(instance.routine); instance.routine = null; }
            instance.speaking = false;
            instance.StartCoroutine(instance.FadeAway());
        }

        static InnerVoice Instance()
        {
            if (instance != null) return instance;
            var go = new GameObject("LingGuang Inner Voice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            instance = go.AddComponent<InnerVoice>();
            instance.Build(go);
            return instance;
        }

        void Build(GameObject go)
        {
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 995; // over the desktop and the prologue thoughts, under the story overlay's cutscenes
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var root = (RectTransform)go.transform;

            var box = PrologueDesk.Rect("Line", root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-760, 196), new Vector2(760, 276));
            group = box.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            // A very soft band so the words read over a bright window, then a blurred-looking shadow under the text.
            var band = PrologueDesk.Rect("Band", box, Vector2.zero, Vector2.one, new Vector2(120, 6), new Vector2(-120, -6));
            PrologueDesk.Fill(band, new Color(0, 0, 0, .22f), false);
            shadow = Label(PrologueDesk.Rect("Shadow", box, Vector2.zero, Vector2.one, new Vector2(2, -3), new Vector2(2, -3)), new Color(0, 0, 0, .55f));
            text = Label(PrologueDesk.Rect("Text", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(.97f, .96f, .9f, 1));
            text.outlineWidth = .08f; text.outlineColor = new Color32(0, 0, 0, 110);
            GameText.Changed += OnLanguage;
        }

        TMP_Text Label(RectTransform rt, Color color)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = PrologueDesk.CjkFont();
            t.fontSize = 32; t.color = color; t.fontStyle = FontStyles.Italic;
            t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false; t.richText = true;
            return t;
        }

        void OnDestroy()
        {
            GameText.Changed -= OnLanguage;
            if (instance == this) { instance = null; lastFinished = nextTicket; }
        }

        void OnLanguage()
        {
            if (!speaking || text == null) return;
            english = GameText.IsEnglish;
            SetText(english ? current.en : current.zh, text.maxVisibleCharacters);
        }

        void SetText(string value, int visible)
        {
            text.text = shadow.text = value;
            text.maxVisibleCharacters = shadow.maxVisibleCharacters = visible;
        }

        IEnumerator Run()
        {
            while (queue.Count > 0 || resume.HasValue)
            {
                while (Held) yield return null;
                current = resume ?? queue.Dequeue();
                resume = null;
                speaking = true;
                english = GameText.IsEnglish;
                SetText(english ? current.en : current.zh, 0);
                text.ForceMeshUpdate();
                bool cut = false;
                for (float t = 0; t < FadeIn && !(cut = Held); t += Time.unscaledDeltaTime) { group.alpha = t / FadeIn; yield return null; }
                if (!cut) group.alpha = 1;
                int length = text.GetParsedText().Length;
                float typed = 0;
                while (!cut && text.maxVisibleCharacters < length)
                {
                    if (cut = Held) break;
                    typed += Time.unscaledDeltaTime / PerChar;
                    int shown = Mathf.Min(length, (int)typed);
                    text.maxVisibleCharacters = shadow.maxVisibleCharacters = shown;
                    length = text.GetParsedText().Length; // a language switch may change it
                    yield return null;
                }
                if (!cut) text.maxVisibleCharacters = shadow.maxVisibleCharacters = 99999;
                for (float t = 0; !cut && t < current.seconds; t += Time.unscaledDeltaTime) { if (cut = Held) break; yield return null; }
                if (cut)
                {
                    // Something covered the screen mid-line: hide it and say the whole line again afterwards.
                    float from = group.alpha;
                    for (float t = 0; t < .15f; t += Time.unscaledDeltaTime) { group.alpha = from * (1 - t / .15f); yield return null; }
                    group.alpha = 0;
                    speaking = false;
                    resume = current;
                    continue;
                }
                var seen = current.shown;
                current.shown = null;
                if (seen != null) { try { seen(); } catch (Exception e) { Debug.LogException(e); } }
                for (float t = 0; t < FadeOut; t += Time.unscaledDeltaTime) { group.alpha = 1 - t / FadeOut; yield return null; }
                group.alpha = 0;
                speaking = false;
                lastFinished = Mathf.Max(lastFinished, current.ticket);
                if (queue.Count > 0) for (float t = 0; t < Gap; t += Time.unscaledDeltaTime) yield return null;
            }
            routine = null;
        }

        IEnumerator FadeAway()
        {
            float start = group.alpha;
            for (float t = 0; t < .2f && start > 0 && !speaking; t += Time.unscaledDeltaTime) { group.alpha = start * (1 - t / .2f); yield return null; }
            if (!speaking) group.alpha = 0;
        }
    }
}
