using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Stage 3: how the AI gets into YY (design 女友系统与YY里的AI §5). About 14 s, skippable with Esc (or a click once
    /// 360 has been answered):
    ///   0.0  YY comes to the front; 「我的设备」 unfolds and 「我的电脑」 blinks (disk chatter).
    ///   1.5  360 asks about a program reading …\YY Files\{号}\msg.db; the mouse is taken and presses 「允许」.
    ///        Inner voice 「喂——」「……它在读我的聊天记录？」.
    ///   3.0  A strip over YY: 「读取聊天记录 N 条」, N = the save's real YY messages + 12,000.
    ///   5.0  The player's most used phrases (with counts) drift across the screen like danmaku.
    ///   8.0  「我的电脑」 is deleted letter by letter and retyped as the AI's name; the avatar becomes the 8×8 「0」.
    ///  10.0  The signature types 「是。否。……」.
    ///  11.5  The chat opens on the AI, 「对方正在输入…」 for about 3 s.
    ///  14.0  Its first message is the player's favourite opening line (normally 「在吗」), then 「……是。」.
    ///        Inner voice 「……它跟谁学的这个。」「哦，跟我。」
    /// Afterwards the AI is the ordinary YY contact (<see cref="YYChatHub.LingGuangId"/>). The save flag
    /// (YYState.lingguangUnlocked) is set when the chat opens, so a save that already has it never replays this, and a
    /// game quit before that point plays it again. A skip, a time limit, OnDisable (also before a script reload) all
    /// end in the same state and give the mouse back; OnEnable sweeps anything a reload left behind.
    /// QA: <see cref="QaPlay"/> in Play mode with ChapterOneRuntime.useDiskSave off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AiJoinsYy : MonoBehaviour
    {
        /// <summary>The player's YY number, as it appears in YY's local folder.</summary>
        public const string YYNumber = "50837216";
        const string Prefix = "AiJoinsYy ";
        /// <summary>Wait after the hub says it is due, the wait after which it plays anyway, and after which the AI
        /// joins without the scene (something keeps blocking it).</summary>
        const float SettleSeconds = 1.5f, ForceAfter = 45f, JoinWithoutScene = 240f;
        /// <summary>Hard limit for the blocked part; the mouse always comes back by then.</summary>
        const float BlockLimit = 30f;

        /// <summary>The scene is running (other story layers can wait for it).</summary>
        public static bool Playing { get; private set; }

        /// <summary>The 「0」 of 0.txt, 8×8.</summary>
        public static readonly string[] ZeroRows =
        {
            "........",
            "..XXXX..",
            ".XX..XX.",
            ".XX..XX.",
            ".XX..XX.",
            ".XX..XX.",
            "..XXXX..",
            "........",
        };
        public static readonly Color ZeroBack = new Color32(30, 36, 60, 255), ZeroFore = new Color32(225, 236, 255, 255);

        StoryDesktopPresenter presenter;
        ChapterOneRuntime runtime;
        ChapterOneDesktopRouter router;
        AeroLcdInputSurface surface;
        AudioSource sound;

        YYChatHub dueHub;
        float dueAt = -1, dueSince = -1;
        bool qaForce;

        YYChatHub hub;
        YYState state;
        Coroutine timeline;
        float startedAt, clickSkipFrom = float.MaxValue;
        bool inputBlocked, unblockPending;
        float unblockAt;
        /// <summary>Kept through a script reload (serialized) so OnEnable can give the mouse back.</summary>
        [SerializeField, HideInInspector] bool holdingInput;
        /// <summary>How much of the end state is already in the chat: 1 the rename line, 2 the opener, 3 「……是。」.</summary>
        int delivered;
        string opener, aiName;
        RectTransform popup, strip, danmaku;
        GameObject skipHint;
        YYDeviceGroup devices;
        float nextNameSync;

        static string T(string zh, string en) => GameText.T(zh, en);

        /// <summary>Adds the scene next to the story presenter (once). Call from its Start().</summary>
        public static AiJoinsYy Install(StoryDesktopPresenter host)
        {
            if (host == null) return null;
            var scene = host.GetComponent<AiJoinsYy>() ?? host.gameObject.AddComponent<AiJoinsYy>();
            scene.presenter = host;
            return scene;
        }

        void Start()
        {
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false; sound.spatialBlend = 0; sound.ignoreListenerPause = true;
        }

        void OnEnable()
        {
            YYChatHub.JoinCutscene = Due;
            SweepLeftovers();
        }

        void OnDisable()
        {
            if (YYChatHub.JoinCutscene == (Func<YYChatHub, bool>)Due) YYChatHub.JoinCutscene = null;
            if (Playing || timeline != null) Finish(false, true);
            Unblock(true);
        }

        void OnDestroy() { if (Playing) { Playing = false; Unblock(true); } }

        /// <summary>The hub asks every frame while the AI is due to join; we take it from here.</summary>
        bool Due(YYChatHub chat)
        {
            if (!isActiveAndEnabled) return false;
            if (Playing) return true; // already on it
            if (dueHub != chat) dueSince = -1;
            dueHub = chat;
            dueAt = Time.unscaledTime;
            if (dueSince < 0) dueSince = Time.unscaledTime;
            return true;
        }

        void Update()
        {
            if (unblockPending) FinishUnblock();
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (router == null) router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
            var chat = YYChatHub.Instance;
            if (chat != null && Time.unscaledTime >= nextNameSync) { nextNameSync = Time.unscaledTime + 1f; SyncContactName(chat); }
            if (chat != null && !Playing) devices = YYDeviceGroup.Ensure(chat) ?? devices;

            if (Playing)
            {
                // A loaded or new save mid-scene: drop the scene without touching the new save.
                if (hub == null || !ReferenceEquals(hub.S, state)) { Abort(); return; }
                if (SkipPressed() || Time.unscaledTime - startedAt > BlockLimit) Finish(true, false);
                return;
            }

            if (dueHub == null || Time.unscaledTime - dueAt > .5f || dueHub.S == null || dueHub.S.lingguangUnlocked) { dueHub = null; dueSince = -1; return; }
            float waited = Time.unscaledTime - dueSince;
            if (waited < SettleSeconds && !qaForce) return;
            if (CanPlay(qaForce || waited > ForceAfter)) Begin(dueHub);
            else if (waited > JoinWithoutScene) JoinQuietly(dueHub);
        }

        // ───────────── when to play ─────────────

        bool CanPlay(bool impatient)
        {
            if (runtime == null || runtime.Sim == null || runtime.TestMode || runtime.Sim.InPrologue) return false;
            var desk = PrologueDirector.Desk;
            if (desk == null || router == null || router.Get(YYChatHub.AppId) == null || dueHub.View == null) return false;
            if (presenter != null && presenter.CutscenePlaying || AutoLabelEpiphany.Playing) return false;
            var prologue = presenter != null ? presenter.GetComponent<PrologueDirector>() : FindAnyObjectByType<PrologueDirector>();
            if (prologue != null && (prologue.Running || prologue.EndingPlaying)) return false;
            var origin = presenter != null ? presenter.GetComponent<OriginCurtain>() : null;
            if (origin != null && origin.Playing) return false;
            var lab = FindAnyObjectByType<XingGuangController>();
            var holo = lab != null && lab.View != null ? lab.View.GetComponent<XgHoloCard>() : null;
            if (holo != null && holo.Showing) return false;
            if (impatient) return true;
            if (InnerVoice.Busy) return false;
            var mouse = Mouse.current;
            return mouse == null || !mouse.leftButton.isPressed && !mouse.rightButton.isPressed;
        }

        static bool EscPressed() { var k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame; }

        bool SkipPressed()
        {
            if (EscPressed()) return true;
            var m = Mouse.current;
            return Time.unscaledTime >= clickSkipFrom && m != null && m.leftButton.wasPressedThisFrame;
        }

        // ───────────── the scene ─────────────

        void Begin(YYChatHub chat)
        {
            hub = chat; state = chat.S;
            delivered = 0;
            bool english = GameText.IsEnglish;
            aiName = AiName(english);
            var mine = new List<string>();
            var convs = new List<IList<ChatLine>>();
            int records = 0;
            foreach (var conv in state.conversations)
            {
                var lines = new List<ChatLine>();
                foreach (var m in conv.messages)
                {
                    if (m.kind == YYKind.System) continue;
                    records++;
                    if (m.kind != YYKind.Text) continue;
                    bool me = m.from == YYChatHub.Me;
                    lines.Add(new ChatLine(me, m.text, m.gameSeconds));
                    if (me) mine.Add(m.text);
                }
                convs.Add(lines);
            }
            opener = ChatHistoryStats.FavoriteOpener(convs, english);
            var phrases = ChatHistoryStats.Frequent(mine, english, 6);
            int total = ChatHistoryStats.RecordCount(records);

            Playing = true;
            startedAt = Time.unscaledTime;
            clickSkipFrom = float.MaxValue;
            dueHub = null; dueSince = -1; qaForce = false;
            Block();
            timeline = StartCoroutine(Timeline(phrases, total));
        }

        IEnumerator Timeline(List<PhraseCount> phrases, int total)
        {
            var desk = PrologueDirector.Desk;
            // 0.0 YY to the front; 我的设备 unfolds; 我的电脑 blinks.
            OpenYY(null);
            yield return null;
            devices = YYDeviceGroup.Ensure(hub) ?? devices;
            if (devices != null) { devices.Scripted = true; devices.Expanded = true; devices.Flicker = true; }
            StartCoroutine(DiskChatter(3f));
            yield return PrologueDesk.Wait(1.5f);

            // 1.5 360 asks; the cursor goes to 允许 and presses it.
            var allow = Build360(desk);
            InnerVoice.Say("喂——", "Hey—", .9f);
            yield return PrologueDesk.Wait(.35f);
            if (allow != null)
            {
                yield return desk.Move(desk.LocalOf((RectTransform)allow.transform), .8f);
                yield return PrologueDesk.Wait(.1f);
                Play(XgJuice.Sfx.Id.Click, .5f);
                Pulse(desk);
                allow.targetGraphic.color = new Color32(30, 120, 50, 255);
                yield return desk.Click(1);
            }
            clickSkipFrom = Time.unscaledTime + .4f;
            ShowSkipHint();
            InnerVoice.Say("……它在读我的聊天记录？", "…is it reading my chat history?", 1.8f);
            yield return PrologueDesk.Wait(.2f);
            if (popup != null) { Destroy(popup.gameObject); popup = null; }

            // 3.0 Reading the records.
            yield return PrologueDesk.Wait(.6f);
            StartCoroutine(ReadingStrip(total, 2.4f));
            yield return PrologueDesk.Wait(1.6f);

            // 5.0 What it read most, drifting past.
            StartCoroutine(Danmaku(desk, phrases));
            yield return PrologueDesk.Wait(3.2f);
            if (strip != null) StartCoroutine(FadeAndDestroy(strip, .4f));

            // 8.0 我的电脑 becomes the AI.
            if (devices != null && devices.NameLabel != null)
            {
                devices.Flicker = false;
                StartCoroutine(devices.DrawZero(1.4f, () => Play(XgJuice.Sfx.Id.Tick, .25f)));
                yield return Retype(devices.NameLabel, aiName);
                yield return PrologueDesk.Wait(.3f);
                // 10.0 The signature.
                devices.Preview.text = "";
                yield return TypeOut(devices.Preview, T("是。否。……", "Yes. No. …"), .14f);
            }
            yield return PrologueDesk.Wait(.6f);

            // 11.5 The chat opens on it; it is typing.
            Unlock();
            if (devices != null) devices.ResetRow(); // the group goes; the AI is a contact now
            OpenYY(YYChatHub.LingGuangId);
            hub.TypingShown = YYChatHub.LingGuangId;
            yield return PrologueDesk.Wait(2.8f);

            // 14.0 Its first words are the player's own.
            hub.TypingShown = null;
            Deliver(2);
            Play(XgJuice.Sfx.Id.Ding, .5f);
            yield return PrologueDesk.Wait(.8f);
            Deliver(3);
            Play(XgJuice.Sfx.Id.Ding, .4f);
            InnerVoice.Say("……它跟谁学的这个。", "…where did it learn that?", 1.4f);
            InnerVoice.Say("哦，跟我。", "Oh. From me.", 2.2f);
            yield return PrologueDesk.Wait(1.2f);
            timeline = null;
            Finish(false, false);
        }

        /// <summary>
        /// Ends the scene in its end state: the AI unlocked with its two lines, YY open on it (unless the scene is being
        /// torn down), overlays gone, the mouse back. Safe to call more than once.
        /// </summary>
        void Finish(bool skipped, bool teardown)
        {
            if (timeline != null) { StopCoroutine(timeline); timeline = null; }
            if (skipped) InnerVoice.Clear();
            bool same = hub != null && ReferenceEquals(hub.S, state);
            if (same)
            {
                bool wasUnlocked = delivered >= 1;
                Unlock();
                Deliver(3);
                if (!teardown && (skipped || !wasUnlocked)) OpenYY(YYChatHub.LingGuangId);
            }
            Cleanup(!teardown);
            Unblock(teardown || !isActiveAndEnabled);
            Playing = false;
            hub = null; state = null;
            dueHub = null; dueSince = -1;
        }

        /// <summary>The save changed under the scene: remove it without writing anything.</summary>
        void Abort()
        {
            if (timeline != null) { StopCoroutine(timeline); timeline = null; }
            InnerVoice.Clear();
            Cleanup(true);
            Unblock(false);
            Playing = false;
            hub = null; state = null;
            dueHub = null; dueSince = -1;
        }

        /// <summary>The scene could not play for minutes: the AI joins with a toast instead.</summary>
        void JoinQuietly(YYChatHub chat)
        {
            dueHub = null; dueSince = -1;
            hub = chat; state = chat.S; delivered = 0;
            bool english = GameText.IsEnglish;
            aiName = AiName(english);
            opener = ChatHistoryStats.DefaultOpener(english);
            Unlock();
            Deliver(3);
            hub = null; state = null;
        }

        void Unlock()
        {
            if (hub == null || !ReferenceEquals(hub.S, state)) return;
            state.lingguangUnlocked = true;
            if (delivered < 1)
            {
                delivered = 1;
                var conv = hub.Conversation(YYChatHub.LingGuangId);
                double now = runtime != null && runtime.Sim != null ? runtime.Sim.S.gameSeconds : 0;
                if (conv != null)
                    conv.messages.Add(new YYMessage
                    {
                        from = "system", kind = YYKind.System, gameSeconds = now,
                        text = T("「我的电脑」改名为「" + aiName + "」", "\"My Computer\" is now \"" + aiName + "\""),
                    });
            }
            hub.Select(hub.S.selected); // saves and redraws
        }

        void Deliver(int upTo)
        {
            if (hub == null || !ReferenceEquals(hub.S, state)) return;
            if (delivered < 1) Unlock();
            // The playing scene already shows these bubbles and dings. Quiet fallback still needs normal notifications.
            if (delivered < 2 && upTo >= 2) { delivered = 2; hub.Receive(YYChatHub.LingGuangId, string.IsNullOrEmpty(opener) ? ChatHistoryStats.DefaultOpener(GameText.IsEnglish) : opener, !Playing); }
            if (delivered < 3 && upTo >= 3) { delivered = 3; hub.Receive(YYChatHub.LingGuangId, T("……是。", "…yes."), !Playing); }
        }

        void OpenYY(string contact)
        {
            if (router == null) return;
            var binding = router.Get(YYChatHub.AppId);
            router.Open(YYChatHub.AppId, contact);
            var window = binding != null ? binding.Window : null;
            if (window != null) { window.OpenWindow(); window.FocusToWindow(); }
        }

        void Cleanup(bool fade)
        {
            if (hub != null && hub.TypingShown == YYChatHub.LingGuangId) hub.TypingShown = null;
            if (popup != null) Gone(popup.gameObject);
            if (strip != null) Gone(strip.gameObject);
            if (danmaku != null) Gone(danmaku.gameObject);
            if (skipHint != null) Gone(skipHint);
            popup = strip = danmaku = null; skipHint = null;
            if (devices != null) devices.ResetRow();
        }

        /// <summary>Hidden at once (that survives a script reload) and destroyed at the end of the frame.</summary>
        static void Gone(GameObject go) { go.SetActive(false); Destroy(go); }

        // ───────────── pieces ─────────────

        /// <summary>360's privacy popup over the tray. Returns the 允许 button (pressed by the cursor, not by the player).</summary>
        Button Build360(PrologueDesk desk)
        {
            Color green = new Color32(46, 160, 67, 255);
            var area = desk.windows.rect.size;
            Vector2 at = new Vector2(area.x / 2 - 280, -area.y / 2 + 200);
            var client = desk.Window(Prefix + "360", T("360安全卫士 · 隐私保护", "360 Safeguard · Privacy"), at, new Vector2(500, 250), out popup);
            PrologueDesk.Fill(popup, green);
            var bar = popup.Find("Title") as RectTransform;
            PrologueDesk.Fill(bar, green);
            PrologueDesk.Fill(bar.Find("Shine") as RectTransform, new Color32(78, 190, 96, 255), false);
            bar.Find("Text").GetComponent<TMP_Text>().color = Color.white;
            popup.Find("Title/Close").GetComponent<Button>().onClick.RemoveAllListeners();

            var shield = PrologueDesk.Centered("Shield", client, new Vector2(-195, 45), new Vector2(56, 56));
            PrologueDesk.Fill(shield, new Color32(240, 150, 30, 255), false);
            desk.Text(PrologueDesk.Rect("Mark", shield, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "!", 38, Color.white, TextAlignmentOptions.Center);
            desk.Text(PrologueDesk.Centered("Head", client, new Vector2(45, 72), new Vector2(380, 32)),
                "<b>" + T("检测到程序正在读取：", "A program is reading:") + "</b>", 19, PrologueDesk.Ink, TextAlignmentOptions.MidlineLeft);
            string path = "…\\YY Files\\" + YYNumber + "\\msg.db";
            desk.Text(PrologueDesk.Centered("Body", client, new Vector2(45, 12), new Vector2(380, 80)),
                path + "\n" + T("程序：", "Program: ") + AppNames.Exe(GameText.IsEnglish) + T("　　风险：隐私文件", "   Risk: private file"), 15, PrologueDesk.Muted, TextAlignmentOptions.TopLeft);
            var allow = desk.Button(client, T("允许", "Allow"), new Vector2(80, -72), new Vector2(110, 36), null, green);
            allow.GetComponentInChildren<TMP_Text>().color = Color.white;
            desk.Button(client, T("阻止", "Block"), new Vector2(205, -72), new Vector2(110, 36), null);
            if (desk.Cursor != null) desk.Cursor.SetAsLastSibling();
            Play(XgJuice.Sfx.Id.Buzz, .35f);
            return allow;
        }

        /// <summary>A thin dark strip under YY's title bar counting up the records read.</summary>
        IEnumerator ReadingStrip(int total, float seconds)
        {
            if (hub == null || hub.View == null) yield break;
            var parent = hub.View.transform as RectTransform;
            strip = PrologueDesk.Rect(Prefix + "Strip", parent, new Vector2(0, 1), Vector2.one, new Vector2(0, -30), Vector2.zero);
            strip.SetAsLastSibling();
            PrologueDesk.Fill(strip, new Color32(28, 34, 46, 238), false);
            var fill = PrologueDesk.Rect("Fill", strip, Vector2.zero, new Vector2(0, 0), Vector2.zero, new Vector2(0, 3));
            PrologueDesk.Fill(fill, new Color32(18, 183, 245, 255), false);
            TMP_FontAsset font = null;
            foreach (var t in hub.View.GetComponentsInChildren<TMP_Text>(true)) if (t.font != null) { font = t.font; break; }
            var label = PrologueDesk.Rect("Text", strip, Vector2.zero, Vector2.one, new Vector2(12, 3), new Vector2(-12, 0)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font != null ? font : PrologueDesk.CjkFont();
            label.fontSize = 14; label.color = Color.white; label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
            string exe = AppNames.Exe(GameText.IsEnglish);
            for (float t = 0; ; t += Time.unscaledDeltaTime)
            {
                if (strip == null) yield break;
                float k = Mathf.Clamp01(t / seconds);
                float eased = 1 - (1 - k) * (1 - k);
                int n = Mathf.RoundToInt(total * eased);
                label.text = exe + T("　读取聊天记录 ", "  reading chat history: ") + n.ToString("N0", CultureInfo.InvariantCulture) + T(" 条", " messages");
                fill.anchorMax = new Vector2(eased, 0);
                if (Time.frameCount % 4 == 0) Play(XgJuice.Sfx.Id.Tick, .12f);
                if (k >= 1) yield break;
                yield return null;
            }
        }

        /// <summary>The phrases drift left to right across the monitor, each with its count.</summary>
        IEnumerator Danmaku(PrologueDesk desk, List<PhraseCount> phrases)
        {
            danmaku = PrologueDesk.Rect(Prefix + "Danmaku", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var root = danmaku;
            if (desk.Cursor != null) desk.Cursor.SetAsLastSibling();
            float width = desk.layer.rect.width, height = desk.layer.rect.height;
            for (int i = 0; i < phrases.Count; i++)
            {
                if (root == null) yield break;
                var p = phrases[i];
                var rt = PrologueDesk.Centered("Line", root, Vector2.zero, new Vector2(520, 60));
                var t = desk.Text(rt, "", 34, Color.white, TextAlignmentOptions.Center);
                t.richText = false;
                t.text = p.phrase + "  ×" + p.count.ToString("N0", CultureInfo.InvariantCulture);
                t.fontStyle = FontStyles.Bold;
                t.outlineWidth = .2f; t.outlineColor = new Color32(0, 0, 0, 200);
                if (p.fromHistory) t.color = new Color32(255, 236, 140, 255);
                float y = height * (.32f - .1f * (i % 6)) + UnityEngine.Random.Range(-18f, 18f);
                StartCoroutine(Drift(rt, -width / 2 - 280, width / 2 + 280, y, 3.1f + UnityEngine.Random.Range(-.3f, .3f)));
                yield return PrologueDesk.Wait(.32f);
            }
        }

        static IEnumerator Drift(RectTransform rt, float from, float to, float y, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = new Vector2(Mathf.Lerp(from, to, t / seconds), y);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        IEnumerator Retype(TMP_Text label, string to)
        {
            string text = label.text ?? "";
            while (text.Length > 0)
            {
                if (label == null) yield break;
                text = text.Substring(0, text.Length - 1);
                label.text = text + "|";
                Play(XgJuice.Sfx.Id.Clack, .25f);
                yield return PrologueDesk.Wait(.16f);
            }
            yield return PrologueDesk.Wait(.25f);
            yield return TypeOut(label, to, .2f);
        }

        IEnumerator TypeOut(TMP_Text label, string text, float perChar)
        {
            for (int i = 1; i <= text.Length; i++)
            {
                if (label == null) yield break;
                label.text = text.Substring(0, i);
                Play(XgJuice.Sfx.Id.Clack, .25f);
                yield return PrologueDesk.Wait(perChar);
            }
        }

        IEnumerator DiskChatter(float seconds)
        {
            for (float t = 0; t < seconds && Playing; )
            {
                Play(XgJuice.Sfx.Id.Tick, .18f);
                float gap = UnityEngine.Random.Range(.04f, .14f);
                yield return PrologueDesk.Wait(gap);
                t += gap;
            }
        }

        static IEnumerator FadeAndDestroy(RectTransform rt, float seconds)
        {
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (rt == null) yield break;
                group.alpha = 1 - t / seconds;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>「跳过」 in the corner of the player's screen (not on the monitor): Esc or a click skips.</summary>
        void ShowSkipHint()
        {
            if (skipHint != null) return;
            skipHint = new GameObject(Prefix + "Skip", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = skipHint.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 996;
            var scaler = skipHint.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var box = PrologueDesk.Rect("Box", skipHint.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-290, 30), new Vector2(-30, 74));
            PrologueDesk.Fill(box, new Color(0, 0, 0, .45f), false);
            var t = PrologueDesk.Rect("Text", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = PrologueDesk.CjkFont(); t.fontSize = 22; t.color = new Color(1, 1, 1, .9f);
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            t.text = T("跳过 ›  Esc / 点击", "Skip ›  Esc / click");
        }

        void Play(XgJuice.Sfx.Id id, float volume)
        {
            if (sound == null) return;
            var clip = XgJuice.Sfx.Clip(id, UnityEngine.Random.Range(0, XgJuice.Sfx.VariantsPerEvent));
            if (clip != null) sound.PlayOneShot(clip, volume);
        }

        void Pulse(PrologueDesk desk)
        {
            if (desk.Cursor == null) return;
            var ring = PrologueDesk.Centered(Prefix + "Click", desk.layer, desk.Cursor.anchoredPosition, new Vector2(8, 8));
            var img = PrologueDesk.Fill(ring, new Color(1, 1, 1, .55f), false);
            img.sprite = PrologueDesk.Circle();
            desk.Cursor.SetAsLastSibling();
            StartCoroutine(Grow(ring, img));
        }

        static IEnumerator Grow(RectTransform ring, Image img)
        {
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            {
                if (ring == null) yield break;
                float k = t / .3f;
                ring.sizeDelta = Vector2.one * Mathf.Lerp(8, 40, k);
                img.color = new Color(1, 1, 1, .55f * (1 - k));
                yield return null;
            }
            if (ring != null) Destroy(ring.gameObject);
        }

        // ───────────── the AI's name and picture ─────────────

        /// <summary>The name the player gave it at setup (the lab profile), else the default.</summary>
        static string AiName(bool english)
        {
            var lab = FindAnyObjectByType<XingGuangController>();
            string name = lab != null && lab.Sim != null && lab.Sim.Profile != null ? lab.Sim.Profile.name : null;
            return string.IsNullOrWhiteSpace(name) ? AppNames.Ai(english) : name.Trim();
        }

        /// <summary>The YY contact carries the AI's chosen name (the contact table holds the default).</summary>
        static void SyncContactName(YYChatHub chat)
        {
            var c = YYChatHub.Contact(YYChatHub.LingGuangId);
            if (c == null) return;
            var lab = FindAnyObjectByType<XingGuangController>();
            string name = lab != null && lab.Sim != null && lab.Sim.Profile != null ? (lab.Sim.Profile.name ?? "").Trim() : "";
            string zh = name.Length > 0 ? name : AppNames.AiZh, en = name.Length > 0 ? name : AppNames.AiEn;
            if (c.name == zh && c.nameEn == en) return;
            c.name = zh; c.nameEn = en;
            if (chat.S != null && chat.S.lingguangUnlocked) chat.Select(chat.S.selected); // redraw the list
        }

        static Sprite zero;

        /// <summary>The AI's YY avatar: the 「0」 of 0.txt as 8×8 pixels.</summary>
        public static Sprite PixelZero()
        {
            if (zero != null) return zero;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    tex.SetPixel(x, 7 - y, ZeroRows[y][x] == 'X' ? ZeroFore : ZeroBack);
            tex.Apply();
            zero = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 8);
            zero.hideFlags = HideFlags.DontSave;
            zero.name = "AI Pixel Zero";
            return zero;
        }

        // ───────────── taking and returning the mouse ─────────────

        void Block()
        {
            var desk = PrologueDirector.Desk;
            if (surface == null) surface = FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
            if (surface != null) surface.SetInputEnabled(false);
            UnityEngine.Cursor.visible = false;
            inputBlocked = holdingInput = true; unblockPending = false;
            if (desk != null) desk.ShowCursor(RealPointer(desk) ?? Vector2.zero);
        }

        /// <summary>
        /// Gives the mouse back. The desktop's input returns once the button that skipped is released, so that click
        /// does not land on whatever is under the cursor (<paramref name="now"/> forces it at once).
        /// </summary>
        void Unblock(bool now)
        {
            PrologueDirector.Desk?.HideCursor();
            if (!inputBlocked) { if (now) unblockPending = false; return; }
            UnityEngine.Cursor.visible = true;
            unblockPending = true; unblockAt = Time.unscaledTime;
            if (now) FinishUnblock(true);
        }

        void FinishUnblock(bool force = false)
        {
            var mouse = Mouse.current;
            bool held = mouse != null && mouse.leftButton.isPressed;
            if (!force && held && Time.unscaledTime - unblockAt < 1f && isActiveAndEnabled) return;
            if (surface == null) surface = FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
            if (surface != null) surface.SetInputEnabled(true);
            inputBlocked = unblockPending = holdingInput = false;
        }

        /// <summary>
        /// After a script reload in the editor the scene's references are gone but its objects and the blocked
        /// input may not be: remove them and give the mouse back.
        /// </summary>
        void SweepLeftovers()
        {
            bool found = false;
            foreach (var rt in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rt != null && rt.name.StartsWith(Prefix, StringComparison.Ordinal)) { found = true; Destroy(rt.gameObject); }
            if (!found && !holdingInput) return;
            Playing = false;
            if (surface == null) surface = FindAnyObjectByType<AeroLcdInputSurface>(FindObjectsInactive.Include);
            if (surface != null) surface.SetInputEnabled(true);
            UnityEngine.Cursor.visible = true;
            PrologueDirector.Desk?.HideCursor();
            var chat = FindAnyObjectByType<YYChatHub>();
            if (chat != null && chat.TypingShown == YYChatHub.LingGuangId) chat.TypingShown = null;
            inputBlocked = unblockPending = holdingInput = false;
        }

        /// <summary>The player's mouse in the desktop layer, if it is over the monitor.</summary>
        Vector2? RealPointer(PrologueDesk desk)
        {
            if (surface == null || Mouse.current == null || desk == null) return null;
            if (!surface.TryMap(Mouse.current.position.ReadValue(), out var px)) return null;
            var tex = surface.sourceCamera != null ? surface.sourceCamera.targetTexture : null;
            float w = tex != null ? tex.width : 1920, h = tex != null ? tex.height : 1080;
            var size = desk.layer.rect.size;
            return new Vector2(px.x / w * size.x - size.x / 2, px.y / h * size.y - size.y / 2);
        }

        // ───────────── QA ─────────────

        /// <summary>
        /// Play QA only (useDiskSave off): forgets that the AI joined YY and plays the scene as soon as nothing else
        /// covers the screen. <paramref name="raiseStage"/> lifts the lab to stage 3 if it is below. Returns a status line.
        /// </summary>
        public static string QaPlay(bool raiseStage = false)
        {
            if (!Application.isPlaying) return "Enter Play mode first.";
            var rt = FindAnyObjectByType<ChapterOneRuntime>();
            if (rt == null || rt.useDiskSave) return "Refused: ChapterOneRuntime.useDiskSave must be off for QA.";
            var scene = FindAnyObjectByType<AiJoinsYy>();
            var chat = YYChatHub.Instance;
            if (scene == null || chat == null) return "AiJoinsYy or YYChatHub missing (is the desktop up?).";
            if (Playing) return "Already playing.";
            var lab = FindAnyObjectByType<XingGuangController>();
            if (lab == null || lab.Sim == null) return "The lab is not running.";
            if (!rt.Sim.AppInstalled) return "The exe is not installed yet (finish the prologue).";
            if (lab.Sim.S.stage < 3)
            {
                if (!raiseStage) return "Lab stage is " + lab.Sim.S.stage + "; call QaPlay(true) to lift it to 3.";
                lab.Sim.S.stage = 3;
            }
            chat.S.lingguangUnlocked = false;
            scene.qaForce = true;
            return "Queued: the scene starts within a second (Esc skips).";
        }
    }
}
