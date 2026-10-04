using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 「她爱我吗」 (design 女友系统与YY里的AI §6). Stage 5 on, once the chat with 林晴雯 is long enough (≥ 60 lines over
    /// ≥ 7 days, <see cref="LoveStats"/>) and she sent a cold signal in the last 48 hours, the inner voice wonders and
    /// the 对话 page offers 「她……爱我吗？」 (XgChatPage.Love.cs). It asks to read the chats first (【读吧】【算了】,
    /// XgSim.Love.cs); then this plays, over the whole desktop, with every number taken from the save:
    /// ① her lines stream in like tokens under 「N 条 · D 天」; ② attention links light four real messages with their
    /// timestamps; ③ three mini charts (her reply delay, who said goodnight first, her length and emoji, by week);
    /// ④ the concept board tugs 「语气:暖」「语气:冷」「主动」「记得」; ⑤ the first day's bulb and 是 / 否 buttons flicker
    /// and land (longer near 50; never a percentage); ⑥ one conclusion line (the local model with the §6.6 prompt,
    /// the §6.4 table offline) and one limit line. Afterwards YY opens her chat with the input focused; nothing is sent.
    /// Re-asks after the three-day cooldown play only ⑤ and ⑥.
    ///
    /// Click or Esc skips (the verdict still lands in the chat). It never takes the mouse or keyboard away from the
    /// desktop beyond its own overlay, has a hard time limit, and OnDisable removes the overlay and lets the
    /// question be asked again.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoveQuestionCutscene : MonoBehaviour
    {
        const float CheckEvery = 1f, GiveUpAfter = 60f, SceneLimit = 55f, ModelWait = 3f;
        const string PacketZh = "[红包]", PacketEn = "[Red packet]";
        static readonly Color Her = new Color32(240, 98, 146, 255), Mine = new Color32(18, 183, 245, 255);
        static readonly Color Soft = new Color(1, 1, 1, .62f), Faint = new Color(1, 1, 1, .32f);

        /// <summary>The cutscene covers the desktop now (inner voice and story cutscenes wait).</summary>
        public static bool Playing { get; private set; }
        static LoveQuestionCutscene instance;

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        XingGuangController lab;
        XgSim bound;
        AudioSource sound;
        float nextCheck, requestedAt, startedAt;
        bool requested, requestFull, hintQueued, canSkip, posted;
        LoveStats stats;
        int statsCount = -1, statsTotal = -1, modelTicket;
        double statsLast = double.NaN;
        Coroutine timeline, closing;
        RectTransform stage, frame;
        CanvasGroup stageGroup;
        Reading reading;

        /// <summary>Everything one telling needs, fixed when it starts.</summary>
        sealed class Reading
        {
            public bool full, yes, hesitant;
            public int tier;
            public LoveStats stats;
            public string conclusion, limit, prompt, model, girl, ai;
            public bool modelAsked, modelDone;
            public float warm, cold, initiative, remember;
        }

        /// <summary>Adds the cutscene next to the story presenter (once). Call from its Start().</summary>
        public static LoveQuestionCutscene Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var c = host.GetComponent<LoveQuestionCutscene>() ?? host.gameObject.AddComponent<LoveQuestionCutscene>();
            c.host = host;
            return c;
        }

        static string T(string zh, string en) => GameText.T(zh, en);

        void Awake()
        {
            instance = this;
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false; sound.spatialBlend = 0; sound.ignoreListenerPause = true;
        }

        void OnDisable()
        {
            // Never leave the overlay up or the question stuck waiting because the scene was cut short.
            if (timeline != null) StopCoroutine(timeline);
            if (closing != null) StopCoroutine(closing);
            timeline = closing = null;
            if (stage != null) Destroy(stage.gameObject);
            stage = frame = null;
            if (bound != null && bound.LoveVerdictPending && !posted) bound.CancelLoveVerdict();
            requested = hintQueued = false;
            reading = null;
            Playing = false;
        }

        void OnDestroy()
        {
            if (bound != null) bound.LoveVerdictRequested -= OnRequested;
            if (instance == this) instance = null;
        }

        void Update()
        {
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            var sim = lab != null ? lab.Sim : null;
            if (!ReferenceEquals(sim, bound)) Bind(sim);
            if (Playing)
            {
                if (closing == null && (canSkip && SkipPressed() || Time.unscaledTime - startedAt > SceneLimit)) Finish(true);
                return;
            }
            if (sim == null || runtime == null || runtime.Sim == null || runtime.TestMode) return;
            if (Time.unscaledTime >= nextCheck) { nextCheck = Time.unscaledTime + CheckEvery; Check(sim); }
            if (requested && closing == null)
            {
                if (CanPlay()) Begin();
                else if (Time.unscaledTime - requestedAt > GiveUpAfter) PostWithoutScene();
            }
        }

        void Bind(XgSim sim)
        {
            if (bound != null) bound.LoveVerdictRequested -= OnRequested;
            bound = sim;
            if (bound != null) bound.LoveVerdictRequested += OnRequested;
            requested = hintQueued = false;
            stats = null; statsCount = -1;
        }

        void OnRequested(bool full)
        {
            requested = true; requestFull = full; requestedAt = Time.unscaledTime;
        }

        // ───────────── her chat, as numbers ─────────────

        static bool TryHub(out YYChatHub hub, out GirlfriendState gf, out YYConversation conv)
        {
            hub = YYChatHub.Instance;
            gf = hub != null && hub.S != null ? hub.S.girlfriend : null;
            conv = hub != null && hub.S != null ? hub.Conversation(YYChatHub.GirlfriendId) : null;
            return gf != null && conv != null;
        }

        static DateTime ClockOf(GameState save, double gameSeconds)
            => gameSeconds < 0 ? GameCalendar.Start.AddSeconds(gameSeconds) : GameCalendar.ClockFor(save, gameSeconds);

        /// <summary>The statistics over her conversation, rebuilt only when it changed.</summary>
        LoveStats Stats(bool force)
        {
            if (runtime == null || runtime.Sim == null || !TryHub(out _, out var gf, out var conv)) return null;
            var lines = conv.messages;
            double last = lines.Count > 0 ? lines[lines.Count - 1].gameSeconds : -1;
            if (!force && stats != null && lines.Count == statsCount && last == statsLast && gf.totalMessages == statsTotal) return stats;
            var save = runtime.Sim.S;
            var list = new List<LoveMessage>(lines.Count);
            foreach (var m in lines)
            {
                if (m == null || m.kind != YYKind.Text || string.IsNullOrEmpty(m.text)) continue;
                bool her = m.from == YYChatHub.GirlfriendId;
                if (!her && m.from != YYChatHub.Me) continue;
                // Red packets are money, not words: they stay out of the length and emoji series.
                if (m.text.StartsWith(PacketZh, StringComparison.Ordinal) || m.text.StartsWith(PacketEn, StringComparison.Ordinal)) continue;
                list.Add(new LoveMessage(her, m.text, m.gameSeconds, ClockOf(save, m.gameSeconds)));
            }
            DateTime? first = gf.firstMessageDay >= 0 ? GameCalendar.DateOf(gf.firstMessageDay) : (DateTime?)null;
            stats = new LoveStats(list, gf.totalMessages, first);
            statsCount = lines.Count; statsLast = last; statsTotal = gf.totalMessages;
            return stats;
        }

        /// <summary>Keeps the lab told what day it is and whether there is enough to read; starts §6.2 when it is time.</summary>
        void Check(XgSim sim)
        {
            sim.LoveToday = GameCalendar.CurrentDay(runtime.Sim.S);
            var s = Stats(false);
            sim.LoveDataReady = s != null && s.EnoughHistory;
            if (hintQueued) { if (!InnerVoice.Busy) hintQueued = false; return; }
            if (sim.S.loveOffered || sim.S.stage < XgSim.LoveStage || !sim.LoveDataReady) return;
            if (!TryHub(out _, out var gf, out _) || !GirlfriendRules.RecentColdSignal(gf, runtime.Sim.S.gameSeconds)) return;
            if (!CanPlay() || InnerVoice.Busy || Held()) return;
            Wonder(sim, s, gf);
        }

        /// <summary>§6.2: the inner voice reads her cold reply (or her new signature) and thinks of asking it.</summary>
        void Wonder(XgSim sim, LoveStats s, GirlfriendState gf)
        {
            hintQueued = true;
            string word = s.Has(LovePickKind.Cold) ? s.Get(LovePickKind.Cold).message.text.Trim() : null;
            string sign = GameText.IsEnglish ? gf.signatureEn : gf.signature;
            if (!string.IsNullOrEmpty(word))
            {
                InnerVoice.Say("她回了个『" + word + "』。", "She replied \"" + word + "\".", 2f);
                InnerVoice.Say("……『" + word + "』是什么意思。", "…What does \"" + word + "\" even mean.", 2.2f);
            }
            else
            {
                InnerVoice.Say("她改了签名：『" + (gf.signature ?? "") + "』", "She changed her signature: \"" + (sign ?? "") + "\"", 2.4f);
                InnerVoice.Say("……这是写给谁的。", "…Who is that for.", 2f);
            }
            string ai = AiName(sim);
            InnerVoice.Say(ai + " 现在能读长句了。", ai + " can read long passages now.", 2.2f);
            InnerVoice.Say("……要不，问问它？", "…Maybe I should ask it?", 2.4f, () =>
            {
                if (!ReferenceEquals(sim, bound) || !sim.OfferLoveHint()) return;
                var view = lab != null ? lab.View : null;
                if (view == null || !view.Visible) return;
                view.Refresh(true);
                var tab = XgGuideHighlight.TabButton(view, "chat");
                if (tab != null) XgGuideHighlight.Pulse(tab, 5f);
            });
        }

        static string AiName(XgSim sim) => sim != null && sim.Profile.name.Length > 0 ? sim.Profile.name : T(AppNames.AiZh, AppNames.AiEn);

        static bool Held()
        {
            try { return InnerVoice.Hold != null && InnerVoice.Hold(); }
            catch (Exception e) { Debug.LogException(e); return false; }
        }

        bool CanPlay()
        {
            if (runtime == null || runtime.Sim == null || runtime.Sim.InPrologue || PrologueDirector.Desk == null) return false;
            if (host is StoryDesktopPresenter presenter && presenter.CutscenePlaying) return false;
            if (AutoLabelEpiphany.Playing || AiJoinsYy.Playing) return false;
            var director = host != null ? host.GetComponent<PrologueDirector>() : null;
            if (director != null && (director.Running || director.EndingPlaying)) return false;
            var origin = host != null ? host.GetComponent<OriginCurtain>() : null;
            if (origin != null && origin.Playing) return false;
            var holo = lab != null && lab.View != null ? lab.View.GetComponent<XgHoloCard>() : null;
            return holo == null || !holo.Showing;
        }

        static bool SkipPressed()
        {
            var mouse = Mouse.current; var keyboard = Keyboard.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame || keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        // ───────────── one telling ─────────────

        Reading Read(bool full)
        {
            var s = Stats(true);
            if (s == null || bound == null || !TryHub(out _, out var gf, out _)) return null;
            bool en = GameText.IsEnglish;
            var r = new Reading { full = full, stats = s, yes = GirlfriendRules.Loves(gf), hesitant = GirlfriendRules.Hesitant(gf) };
            int tier = (int)GirlfriendRules.Tier(gf);
            if (r.yes && tier < 2) tier = 2;
            if (!r.yes && tier >= 2) tier = 1;
            r.tier = tier;
            string late = null;
            if (s.Has(LovePickKind.Latest))
            {
                var c = s.Get(LovePickKind.Latest).message.clock;
                if (c.Hour >= 23 || c.Hour < 5) late = LoveStats.Spoken(c, en);
            }
            string delay = s.MyRecentReplyDelay >= 0 ? LoveStats.Duration(s.MyRecentReplyDelay, en) : null;
            string sign = en ? gf.signatureEn : gf.signature;
            if (string.IsNullOrEmpty(sign)) sign = null;
            r.conclusion = bound.LoveConclusionLine(tier, r.hesitant, late, delay, sign);
            r.limit = bound.LoveLimitLine(tier, r.hesitant);
            r.prompt = bound.LoveVerdictPrompt(r.yes, s.ReplyTrendText(en), s.GoodnightText(en), s.ColdText(en, sign), Memories(gf, en));
            r.girl = T("晴雯ˇ", "Qingwenˇ");
            r.ai = AiName(bound);
            // The concept board's four weights (0..1, shown as a pull left or right; never as a number).
            r.warm = new[] { .1f, .3f, .55f, .75f, .92f }[Mathf.Clamp(tier, 0, 4)];
            int recentHer = 0, recentCold = 0, starts = 0, herStarts = 0;
            for (int i = 0; i < s.messages.Count; i++)
            {
                var m = s.messages[i];
                if (m.fromHer && i >= s.messages.Count - 80) { recentHer++; if (LoveStats.IsColdWord(m.text)) recentCold++; }
                if (i == 0 || m.gameSeconds - s.messages[i - 1].gameSeconds >= 3 * 3600) { starts++; if (m.fromHer) herStarts++; }
            }
            r.cold = Mathf.Clamp01(.15f + (recentHer > 0 ? 3f * recentCold / recentHer : 0));
            r.initiative = starts > 0 ? Mathf.Clamp01((float)herStarts / starts) : .5f;
            r.remember = Mathf.Clamp01(.2f + (gf.memories != null ? gf.memories.Count : 0) / 12f);
            return r;
        }

        static string Memories(GirlfriendState gf, bool en)
        {
            if (gf.memories == null || gf.memories.Count == 0) return en ? "none" : "没有";
            var list = gf.memories.GetRange(Math.Max(0, gf.memories.Count - 4), Math.Min(4, gf.memories.Count));
            return string.Join(en ? "; " : "；", list);
        }

        void AskModel(Reading r)
        {
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready || bound == null) return;
            int ticket = ++modelTicket;
            var sim = bound;
            r.modelAsked = true;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", r.prompt),
                new KeyValuePair<string, string>("user", sim.LoveQuestion),
            };
            llm.Chat(messages, 160, .7f, reply =>
            {
                if (ticket != modelTicket) return;
                r.model = sim.CleanLoveLine(EraLexicon.Scrub(reply ?? "") ?? "", r.yes);
                r.modelDone = true;
            });
        }

        void Begin()
        {
            requested = false;
            reading = Read(requestFull);
            if (reading == null || PrologueDirector.Desk == null) { bound?.CancelLoveVerdict(); reading = null; return; }
            Playing = true; posted = false; canSkip = false;
            startedAt = Time.unscaledTime;
            AskModel(reading);
            BuildStage();
            timeline = StartCoroutine(reading.full ? Full(reading) : Short(reading));
        }

        /// <summary>The desktop could not be covered for a minute: the verdict goes into the chat without the scene.</summary>
        void PostWithoutScene()
        {
            requested = false;
            reading = Read(requestFull);
            if (reading == null) { bound?.CancelLoveVerdict(); return; }
            Post(reading);
            reading = null;
        }

        void Post(Reading r)
        {
            if (posted || r == null || bound == null) return;
            posted = true;
            bound.PostLoveVerdict(r.yes, string.IsNullOrEmpty(r.model) ? r.conclusion : r.model, r.limit);
            if (lab != null && lab.View != null) lab.View.Refresh(true);
        }

        void Finish(bool skipped)
        {
            if (closing != null) return;
            if (timeline != null) StopCoroutine(timeline);
            timeline = null;
            var r = reading;
            Post(r);
            closing = StartCoroutine(Close(r, skipped));
        }

        IEnumerator Close(Reading r, bool skipped)
        {
            // Fade out; the overlay keeps catching clicks until the button that skipped is released.
            for (float t = 0; t < .4f && stageGroup != null; t += Time.unscaledDeltaTime) { stageGroup.alpha = 1 - t / .4f; yield return null; }
            var mouse = Mouse.current;
            for (float t = 0; t < 1f && mouse != null && mouse.leftButton.isPressed; t += Time.unscaledDeltaTime) yield return null;
            if (stage != null) Destroy(stage.gameObject);
            stage = frame = null; stageGroup = null;
            Playing = false;
            reading = null;
            if (r != null && r.full)
            {
                InnerVoice.Say("……", "…", 1.2f);
                InnerVoice.Say("我去给她发个消息。", "I'll send her a message.", 2.4f);
            }
            yield return OpenHerChat();
            closing = null;
        }

        /// <summary>YY on her conversation, the caret in the input box. Nothing is sent for you.</summary>
        IEnumerator OpenHerChat()
        {
            var router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
            if (router == null || !router.Open(YYChatHub.AppId, YYChatHub.GirlfriendId)) yield break;
            yield return null; yield return null;
            var view = YYChatHub.Instance != null ? YYChatHub.Instance.View : null;
            var input = view != null ? view.GetComponentInChildren<TMP_InputField>(false) : null;
            if (input != null) { input.Select(); input.ActivateInputField(); }
        }

        // ───────────── the stage ─────────────

        void BuildStage()
        {
            var desk = PrologueDirector.Desk;
            stage = PrologueDesk.Rect("Love Question", desk.layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(stage, new Color(.02f, .03f, .06f, .93f), true);
            stageGroup = stage.gameObject.AddComponent<CanvasGroup>();
            stageGroup.alpha = 0;
            stage.SetAsLastSibling();
            // Everything is laid out on a 1600 × 900 frame scaled to the desktop.
            frame = PrologueDesk.Centered("Frame", stage, Vector2.zero, new Vector2(1600, 900));
            var size = desk.layer.rect.size;
            float k = Mathf.Min(size.x / 1600f, size.y / 900f) * .96f;
            frame.localScale = Vector3.one * (k > 0 ? k : 1);
            var hint = Label(PrologueDesk.Rect("Skip", stage, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-360, 12), new Vector2(-18, 40)), T("点击或 Esc 跳过", "Click or Esc to skip"), 14, Faint, TextAlignmentOptions.MidlineRight);
            hint.name = "Skip Hint";
            StartCoroutine(FadeIn(stageGroup));
        }

        static IEnumerator FadeIn(CanvasGroup g)
        {
            for (float t = 0; t < .35f && g != null; t += Time.unscaledDeltaTime) { g.alpha = t / .35f; yield return null; }
            if (g != null) g.alpha = 1;
        }

        TMP_Text Label(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = PrologueDirector.Desk.Text(rt, text, size, color, align);
            t.richText = false;
            return t;
        }

        static Image Box(RectTransform rt, Color color) => PrologueDesk.Fill(rt, color, false);
        static RectTransform At(string name, Transform parent, float x, float y, float w, float h) => PrologueDesk.Centered(name, parent, new Vector2(x, y), new Vector2(w, h));

        /// <summary>A straight line between two points of the same parent.</summary>
        static Image Line(Transform parent, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var d = b - a;
            var rt = PrologueDesk.Centered("Link", parent, (a + b) / 2, new Vector2(d.magnitude, thickness));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return Box(rt, color);
        }

        RectTransform Section(string name) => PrologueDesk.Rect(name, frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static string Cut(string text, int max)
        {
            string t = (text ?? "").Replace('\n', ' ').Trim();
            var info = new StringInfo(t);
            return info.LengthInTextElements <= max ? t : info.SubstringByTextElements(0, max) + "…";
        }

        static string Stamp(DateTime clock) => GameText.IsEnglish
            ? clock.ToString("MMM d HH:mm", CultureInfo.InvariantCulture)
            : clock.Month + "月" + clock.Day + "日 " + clock.ToString("HH:mm", CultureInfo.InvariantCulture);

        void Sfx(XgJuice.Sfx.Id id, int variant, float volume = .45f)
        {
            var clip = XgJuice.Sfx.Clip(id, Mathf.Clamp(variant, 0, XgJuice.Sfx.VariantsPerEvent - 1));
            if (sound != null && clip != null) sound.PlayOneShot(clip, volume);
        }

        // ───────────── the timelines ─────────────

        IEnumerator Full(Reading r)
        {
            yield return PrologueDesk.Wait(.35f);
            canSkip = true;
            var title = Label(At("Title", frame, 0, 410, 1400, 50), r.ai + T(" 在读你和她的聊天记录", " is reading your chats with her"), 26, Soft, TextAlignmentOptions.Center);
            var counter = Label(At("Counter", frame, 0, 350, 1200, 70), "", 46, Color.white, TextAlignmentOptions.Center);
            counter.fontStyle = FontStyles.Bold;
            yield return Stream(r, counter);
            counter.color = Soft;
            yield return Attention(r);
            yield return Evidence(r);
            yield return Brain(r);
            title.text = "";
            counter.text = "";
            yield return Verdict(r, false);
            yield return Words(r, 3f);
            timeline = null;
            Finish(false);
        }

        IEnumerator Short(Reading r)
        {
            yield return PrologueDesk.Wait(.25f);
            canSkip = true;
            yield return Verdict(r, true);
            yield return Words(r, 2.2f);
            timeline = null;
            Finish(false);
        }

        /// <summary>① 4 s: the YY window flies into its window and her lines stream up like tokens; the counter runs.</summary>
        IEnumerator Stream(Reading r, TMP_Text counter)
        {
            var s = r.stats;
            var sec = Section("Read");
            // YY's chat window with her last lines.
            var yy = At("YY", sec, -560, -40, 420, 520);
            Box(yy, new Color32(244, 247, 251, 255));
            var yyGroup = yy.gameObject.AddComponent<CanvasGroup>();
            var head = PrologueDesk.Rect("Head", yy, new Vector2(0, 1), Vector2.one, new Vector2(0, -52), Vector2.zero);
            Box(head, new Color32(16, 132, 220, 255));
            Label(PrologueDesk.Rect("Name", head, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-10, 0)), r.girl, 20, Color.white, TextAlignmentOptions.MidlineLeft);
            int shown = 0;
            for (int i = s.messages.Count - 1; i >= 0 && shown < 7; i--, shown++)
            {
                var m = s.messages[i];
                var bubble = PrologueDesk.Rect("Line", yy, new Vector2(0, 0), new Vector2(1, 0), new Vector2(m.fromHer ? 14 : 120, 14 + shown * 62), new Vector2(m.fromHer ? -120 : -14, 62 + shown * 62));
                Box(bubble, m.fromHer ? Color.white : Mine);
                Label(PrologueDesk.Rect("Text", bubble, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2)), Cut(m.text, 16), 15, m.fromHer ? new Color32(34, 40, 52, 255) : (Color32)Color.white, TextAlignmentOptions.MidlineLeft);
            }
            // Its window.
            var win = At("Lab", sec, 0, -60, 620, 560);
            Box(win, new Color32(27, 35, 72, 255));
            var winHead = PrologueDesk.Rect("Head", win, new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero);
            Box(winHead, new Color32(43, 54, 104, 255));
            Label(PrologueDesk.Rect("Name", winHead, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-10, 0)), r.ai, 18, Soft, TextAlignmentOptions.MidlineLeft);
            var flow = PrologueDesk.Rect("Flow", win, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -52));
            flow.gameObject.AddComponent<RectMask2D>();
            Sfx(XgJuice.Sfx.Id.Whoosh, 0);

            Vector2 from = yy.anchoredPosition;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                yy.anchoredPosition = Vector2.Lerp(from, win.anchoredPosition, k);
                yy.localScale = Vector3.one * Mathf.Lerp(1, .15f, k);
                yyGroup.alpha = 1 - k * k;
                yield return null;
            }
            Destroy(yy.gameObject);

            // Every line, as a token rising through its window (sampled evenly when there are many).
            int total = Math.Max(1, s.TotalMessages), days = Math.Max(1, s.DaySpan);
            var tokens = new List<RectTransform>();
            float span = 2.9f, spawnEvery = .055f, nextSpawn = 0; int spawned = 0, slots = (int)(span / spawnEvery);
            for (float t = 0; t < span + .1f; t += Time.unscaledDeltaTime)
            {
                if (t >= nextSpawn && t < span && s.messages.Count > 0)
                {
                    nextSpawn += spawnEvery;
                    var m = s.messages[Mathf.Clamp((int)((long)spawned * s.messages.Count / Math.Max(1, slots)), 0, s.messages.Count - 1)];
                    float w = Mathf.Clamp(LoveStats.Length(m.text) * 15 + 26, 46, 240);
                    var tok = PrologueDesk.Rect("Token", flow, new Vector2(.5f, 0), new Vector2(.5f, 0), Vector2.zero, Vector2.zero);
                    tok.sizeDelta = new Vector2(w, 26);
                    tok.anchoredPosition = new Vector2((m.fromHer ? -1 : 1) * UnityEngine.Random.Range(20f, 260f - w / 2), -14);
                    var c = m.fromHer ? Her : Mine; c.a = .85f;
                    Box(tok, c);
                    Label(PrologueDesk.Rect("Text", tok, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0)), Cut(m.text, 10), 13, Color.white, TextAlignmentOptions.Center).textWrappingMode = TextWrappingModes.NoWrap;
                    tokens.Add(tok);
                    spawned++;
                    if (spawned % 6 == 0) Sfx(XgJuice.Sfx.Id.Tick, spawned / 6 % 3, .12f);
                }
                foreach (var tok in tokens) if (tok != null) tok.anchoredPosition += new Vector2(0, 300 * Time.unscaledDeltaTime);
                float k = Mathf.Clamp01(t / span);
                int n = Mathf.RoundToInt(total * (1 - Mathf.Pow(1 - k, 2))), d = Mathf.Max(1, Mathf.RoundToInt(days * k));
                counter.text = T(n.ToString("N0", CultureInfo.InvariantCulture) + " 条 · " + d + " 天", n.ToString("N0", CultureInfo.InvariantCulture) + " lines · " + d + " days");
                yield return null;
            }
            counter.text = T(total.ToString("N0", CultureInfo.InvariantCulture) + " 条 · " + days + " 天", total.ToString("N0", CultureInfo.InvariantCulture) + " lines · " + days + " days");
            yield return PrologueDesk.Wait(.2f);
            Destroy(sec.gameObject);
        }

        string KindText(LovePick p)
        {
            switch (p.kind)
            {
                case LovePickKind.Longest: return T("她最长的一条", "Her longest");
                case LovePickKind.Latest: return T("她最晚的一条", "Her latest at night");
                case LovePickKind.Cold: return T("她最近的『" + Cut(p.message.text, 4) + "』", "Her latest \"" + Cut(p.message.text, 8) + "\"");
                default: return T("你没回应的那件事", "The thing you never answered");
            }
        }

        /// <summary>② 6 s: the list dims; attention links light her longest, latest, coldest and unanswered lines.</summary>
        IEnumerator Attention(Reading r)
        {
            var s = r.stats;
            var sec = Section("Attention");
            var query = At("Query", sec, -600, -20, 190, 190);
            var qi = Box(query, new Color(1f, .75f, .16f, .14f)); qi.sprite = PrologueDesk.Circle();
            Label(PrologueDesk.Rect("Text", query, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0)), r.full ? T("她……爱我吗？", "Does she… love me?") : "", 20, XgPalette.Gold, TextAlignmentOptions.Center);
            // Nine rows: the picks at 1, 3, 5, 7; real lines of hers, dimmed, in between.
            var picks = s.picks;
            int[] slots = { 1, 3, 5, 7 };
            var rows = new List<(RectTransform row, Image fill, TMP_Text text, int pick)>();
            var rng = new System.Random(s.Count * 31 + s.DaySpan);
            for (int i = 0; i < 9; i++)
            {
                int pick = Array.IndexOf(slots, i);
                if (pick >= picks.Count) pick = -1;
                var row = At("Row", sec, 150, 250 - i * 62, 900, 50);
                var fill = Box(row, new Color(1, 1, 1, .05f));
                string text;
                if (pick >= 0) text = picks[pick].message.text;
                else text = s.messages.Count > 0 ? s.messages[rng.Next(s.messages.Count)].text : "";
                var label = Label(PrologueDesk.Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-190, 0)), Cut(text, 30), 17, Faint, TextAlignmentOptions.MidlineLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                rows.Add((row, fill, label, pick));
            }
            yield return PrologueDesk.Wait(.5f);
            var links = new List<Image>();
            Vector2 origin = query.anchoredPosition + new Vector2(95, 0);
            foreach (var (row, fill, text, pick) in rows)
            {
                if (pick < 0) continue;
                var p = picks[pick];
                var link = Line(sec, origin, row.anchoredPosition - new Vector2(450, 0), 3, new Color(1f, .75f, .16f, 0));
                link.transform.SetAsFirstSibling();
                links.Add(link);
                Label(PrologueDesk.Rect("Kind", row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, 0), new Vector2(-16, 20)), KindText(p), 13, XgPalette.Gold, TextAlignmentOptions.BottomLeft);
                Label(PrologueDesk.Rect("When", row, new Vector2(1, 0), Vector2.one, new Vector2(-180, 0), new Vector2(-14, 0)), Stamp(p.message.clock), 14, Soft, TextAlignmentOptions.MidlineRight);
                Sfx(XgJuice.Sfx.Id.Ding, pick % 3, .25f);
                for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
                {
                    float k = t / .35f;
                    link.color = new Color(1f, .75f, .16f, k);
                    fill.color = new Color(1, 1, 1, Mathf.Lerp(.05f, .16f, k));
                    text.color = Color.Lerp(Faint, Color.white, k);
                    yield return null;
                }
                yield return PrologueDesk.Wait(.75f);
            }
            // The links breathe for the rest of the six seconds.
            float left = Mathf.Max(1f, 5.5f - picks.Count * 1.1f);
            for (float t = 0; t < left; t += Time.unscaledDeltaTime)
            {
                float a = .55f + .45f * Mathf.Sin(t * 5);
                foreach (var l in links) if (l != null) l.color = new Color(1f, .75f, .16f, a);
                yield return null;
            }
            Destroy(sec.gameObject);
        }

        /// <summary>③ 6 s: three small charts, by week: her reply delay, who said goodnight first, her length and emoji.</summary>
        IEnumerator Evidence(Reading r)
        {
            var s = r.stats;
            var sec = Section("Evidence");
            var weeks = s.ChartSlice();
            bool en = GameText.IsEnglish;

            // 1. Her average reply delay.
            var delays = s.Series(w => w.herReplyDelay);
            double maxDelay = 60; foreach (var v in delays) maxDelay = Math.Max(maxDelay, v);
            var known = delays.FindAll(v => v >= 0);
            string foot1 = known.Count == 0 ? T("没有可算的回复", "No replies to measure")
                : known.Count == 1 ? LoveStats.Duration(known[0], en) : LoveStats.Duration(known[0], en) + " → " + LoveStats.Duration(known[known.Count - 1], en);
            var bars1 = Chart(sec, -530, T("她的平均回复时间 · 按周", "Her average reply time · by week"), foot1);
            var grow = new List<(RectTransform bar, float height)>();
            for (int i = 0; i < weeks.Count; i++)
            {
                float h = delays[i] < 0 ? 4 : Mathf.Max(6, (float)(delays[i] / maxDelay) * 190);
                grow.Add((Bar(bars1, i, weeks.Count, 0, 1, delays[i] < 0 ? new Color(1, 1, 1, .2f) : Her), h));
            }
            yield return Grow(grow, .8f);
            yield return PrologueDesk.Wait(1.2f);

            // 2. Who said goodnight first.
            int maxNight = 1; foreach (var w in weeks) maxNight = Math.Max(maxNight, Math.Max(w.herGoodnightFirst, w.myGoodnightFirst));
            int her = 0, me = 0; foreach (var w in weeks) { her += w.herGoodnightFirst; me += w.myGoodnightFirst; }
            var bars2 = Chart(sec, 0, T("谁先说晚安 · 按周", "Who said goodnight first · by week"),
                T("她 " + her + " 晚 · 你 " + me + " 晚", "her " + her + " nights · you " + me));
            grow.Clear();
            for (int i = 0; i < weeks.Count; i++)
            {
                grow.Add((Bar(bars2, i, weeks.Count, 0, 2, Her), weeks[i].herGoodnightFirst == 0 ? 3 : 190f * weeks[i].herGoodnightFirst / maxNight));
                grow.Add((Bar(bars2, i, weeks.Count, 1, 2, Mine), weeks[i].myGoodnightFirst == 0 ? 3 : 190f * weeks[i].myGoodnightFirst / maxNight));
            }
            yield return Grow(grow, .8f);
            yield return PrologueDesk.Wait(1.2f);

            // 3. Her message length (bars) and emoji per message (dots).
            var lengths = s.Series(w => w.herLength); var emoji = s.Series(w => w.herEmoji);
            double maxLen = 4, maxEmoji = .5; foreach (var v in lengths) maxLen = Math.Max(maxLen, v); foreach (var v in emoji) maxEmoji = Math.Max(maxEmoji, v);
            int trend = LoveStats.Trend(lengths);
            string arrow = trend > 0 ? "↑" : trend < 0 ? "↓" : "→";
            double lastLen = lengths.Count > 0 ? lengths[lengths.Count - 1] : 0, lastEmoji = emoji.Count > 0 ? emoji[emoji.Count - 1] : 0;
            var bars3 = Chart(sec, 530, T("她的消息长度 / 表情 · 按周", "Her message length / emoji · by week"),
                T("最近一周 平均 " + lastLen.ToString("0.#", CultureInfo.InvariantCulture) + " 字 " + arrow + " · 表情 " + lastEmoji.ToString("0.#", CultureInfo.InvariantCulture),
                  "last week " + lastLen.ToString("0.#", CultureInfo.InvariantCulture) + " chars " + arrow + " · emoji " + lastEmoji.ToString("0.#", CultureInfo.InvariantCulture)));
            grow.Clear();
            var dots = new List<(RectTransform dot, float y)>();
            for (int i = 0; i < weeks.Count; i++)
            {
                var c = Her; c.a = .55f;
                grow.Add((Bar(bars3, i, weeks.Count, 0, 1, c), Mathf.Max(3, (float)(lengths[i] / maxLen) * 190)));
                float slot = 440f / Math.Max(1, weeks.Count);
                var dot = PrologueDesk.Rect("Emoji", bars3, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                dot.sizeDelta = new Vector2(12, 12);
                var img = Box(dot, XgPalette.Gold); img.sprite = PrologueDesk.Circle();
                dot.anchoredPosition = new Vector2(slot * (i + .5f), 0);
                dots.Add((dot, (float)(emoji[i] / maxEmoji) * 190));
            }
            for (float t = 0; t < .8f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / .8f);
                foreach (var (bar, height) in grow) if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, height * k);
                foreach (var (dot, y) in dots) if (dot != null) dot.anchoredPosition = new Vector2(dot.anchoredPosition.x, y * k);
                yield return null;
            }
            yield return PrologueDesk.Wait(1.4f);
            Destroy(sec.gameObject);
        }

        /// <summary>A chart panel; returns its plot area (bottom-left origin).</summary>
        RectTransform Chart(RectTransform parent, float x, string title, string foot)
        {
            var panel = At("Chart", parent, x, -20, 480, 340);
            Box(panel, new Color(1, 1, 1, .06f));
            Label(PrologueDesk.Rect("Title", panel, new Vector2(0, 1), Vector2.one, new Vector2(16, -44), new Vector2(-16, -8)), title, 18, Color.white, TextAlignmentOptions.MidlineLeft);
            Label(PrologueDesk.Rect("Foot", panel, Vector2.zero, new Vector2(1, 0), new Vector2(16, 8), new Vector2(-16, 40)), foot, 15, Soft, TextAlignmentOptions.MidlineLeft);
            var plot = PrologueDesk.Rect("Plot", panel, Vector2.zero, Vector2.one, new Vector2(20, 54), new Vector2(-20, -60));
            var axis = PrologueDesk.Rect("Axis", plot, Vector2.zero, new Vector2(1, 0), new Vector2(0, -2), Vector2.zero);
            Box(axis, Faint);
            Sfx(XgJuice.Sfx.Id.Clack, 0, .3f);
            return plot;
        }

        /// <summary>One bar (of <paramref name="of"/> side by side) in week <paramref name="i"/>; it starts at height 0.</summary>
        static RectTransform Bar(RectTransform plot, int i, int weeks, int which, int of, Color color)
        {
            float slot = 440f / Math.Max(1, weeks), width = Mathf.Min(34, slot * .7f / of);
            var bar = PrologueDesk.Rect("Bar", plot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            bar.pivot = new Vector2(.5f, 0);
            bar.sizeDelta = new Vector2(width, 0);
            bar.anchoredPosition = new Vector2(slot * (i + .5f) + (which - (of - 1) / 2f) * (width + 3), 0);
            Box(bar, color);
            return bar;
        }

        static IEnumerator Grow(List<(RectTransform bar, float height)> bars, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                foreach (var (bar, height) in bars) if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, height * k);
                yield return null;
            }
            foreach (var (bar, height) in bars) if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, height);
        }

        /// <summary>④ 4 s: the concept board. Four concepts light up and their weights pull left and right, then settle.</summary>
        IEnumerator Brain(Reading r)
        {
            var sec = Section("Brain");
            Label(At("Board", sec, 0, 250, 600, 40), T("概念盘", "Concept board"), 20, Soft, TextAlignmentOptions.Center);
            var center = At("Center", sec, 0, -20, 120, 120);
            var ci = Box(center, new Color(1, 1, 1, .1f)); ci.sprite = PrologueDesk.Circle();
            Label(PrologueDesk.Rect("Q", center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "？", 54, Color.white, TextAlignmentOptions.Center);
            string[] names = { T("语气:暖", "tone:warm"), T("语气:冷", "tone:cold"), T("主动", "reaches out"), T("记得", "remembers") };
            float[] targets = { r.warm, r.cold, r.initiative, r.remember };
            Vector2[] at = { new Vector2(-450, 110), new Vector2(450, 110), new Vector2(-450, -150), new Vector2(450, -150) };
            Color[] colors = { XgPalette.Gold, new Color32(120, 160, 230, 255), Her, XgPalette.Good };
            var fills = new RectTransform[4]; var links = new Image[4]; var nodes = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                links[i] = Line(sec, center.anchoredPosition, at[i], 3, new Color(colors[i].r, colors[i].g, colors[i].b, 0));
                links[i].transform.SetAsFirstSibling();
                var node = At("Concept", sec, at[i].x, at[i].y, 280, 74);
                nodes[i] = Box(node, new Color(colors[i].r, colors[i].g, colors[i].b, .15f));
                Label(PrologueDesk.Rect("Name", node, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), names[i], 24, Color.white, TextAlignmentOptions.Center);
                var track = PrologueDesk.Rect("Weight", node, Vector2.zero, new Vector2(1, 0), new Vector2(0, -22), new Vector2(0, -10));
                Box(track, new Color(1, 1, 1, .12f));
                var mid = PrologueDesk.Rect("Mid", track, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(-1, -3), new Vector2(1, 3));
                Box(mid, Soft);
                fills[i] = PrologueDesk.Rect("Fill", track, new Vector2(.5f, 0), new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
                Box(fills[i], colors[i]);
            }
            Sfx(XgJuice.Sfx.Id.Charge, 0, .3f);
            for (float t = 0; t < 3.7f; t += Time.unscaledDeltaTime)
            {
                float settle = Mathf.Clamp01(t / 3.2f);
                for (int i = 0; i < 4; i++)
                {
                    float light = Mathf.Clamp01((t - i * .25f) / .4f);
                    float w = Mathf.Clamp01(targets[i] + Mathf.Sin(t * 7 + i * 1.7f) * .38f * (1 - settle) * light);
                    float x = (w - .5f) * 2 * 140;
                    fills[i].offsetMin = new Vector2(Mathf.Min(0, x), 0); fills[i].offsetMax = new Vector2(Mathf.Max(0, x), 0);
                    var c = colors[i];
                    links[i].color = new Color(c.r, c.g, c.b, light * (.25f + .75f * Mathf.Abs(w - .5f) * 2));
                    nodes[i].color = new Color(c.r, c.g, c.b, .15f + .3f * light);
                }
                yield return null;
            }
            Destroy(sec.gameObject);
        }

        /// <summary>
        /// ⑤ the first day again: one bulb and 是 / 否. The light flickers between them three times (five near 50) and
        /// lands; a holo card turns over with the word. No percentage, anywhere.
        /// </summary>
        IEnumerator Verdict(Reading r, bool quick)
        {
            var sec = Section("Verdict");
            sec.SetAsLastSibling();
            var bulb = Label(At("Bulb", sec, 0, 200, 260, 260), "●", 200, new Color32(200, 204, 214, 255), TextAlignmentOptions.Center);
            var yesRt = At("Yes", sec, -95, 20, 150, 70); var noRt = At("No", sec, 95, 20, 150, 70);
            var yesFill = Box(yesRt, XgPalette.Button); var noFill = Box(noRt, XgPalette.Button);
            var yesText = Label(PrologueDesk.Rect("Label", yesRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("是", "Yes"), 26, XgPalette.Ink, TextAlignmentOptions.Center);
            var noText = Label(PrologueDesk.Rect("Label", noRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("否", "No"), 26, XgPalette.Ink, TextAlignmentOptions.Center);
            yield return PrologueDesk.Wait(quick ? .4f : .8f);

            int flickers = (r.hesitant ? 5 : 3) * 2;
            // Alternate, starting on the other answer so the last step lands on the verdict.
            bool on = flickers % 2 == 0 ? !r.yes : r.yes;
            for (int i = 0; i <= flickers; i++)
            {
                bool last = i == flickers;
                Light(on, yesFill, yesText, noFill, noText, bulb, last ? 1f : .55f);
                Sfx(last ? XgJuice.Sfx.Id.Stamp : XgJuice.Sfx.Id.Click, i % 3, last ? .55f : .3f);
                if (last) break;
                float k = (float)i / flickers;
                // Slower and slower; near 50 it lingers longer on each side.
                float hold = Mathf.Lerp(quick ? .12f : .16f, r.hesitant ? .6f : .42f, k * k);
                yield return PrologueDesk.Wait(hold);
                on = !on;
            }
            if (!r.yes) bulb.color = new Color32(96, 104, 122, 255);
            yield return PrologueDesk.Wait(.5f);

            // The holo card.
            var card = At("Card", sec, 0, -150, 380, 150);
            Box(card, XgPalette.Gold);
            var inner = PrologueDesk.Rect("Face", card, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            Box(inner, new Color32(250, 248, 240, 255));
            var word = Label(PrologueDesk.Rect("Word", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), bound != null ? bound.LoveVerdictWord(r.yes) : (r.yes ? "是。" : "否。"), 72, XgPalette.Ink, TextAlignmentOptions.Center);
            word.fontStyle = FontStyles.Bold;
            card.localScale = new Vector3(0, 1, 1);
            Sfx(XgJuice.Sfx.Id.Swoosh, 0, .4f);
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                float k = t / .45f;
                card.localScale = new Vector3(Mathf.Sin(k * Mathf.PI * .5f) * (1 + .08f * Mathf.Sin(k * Mathf.PI)), 1, 1);
                yield return null;
            }
            card.localScale = Vector3.one;
            yield return PrologueDesk.Wait(quick ? .5f : 1.2f);
        }

        static void Light(bool yes, Image yesFill, TMP_Text yesText, Image noFill, TMP_Text noText, TMP_Text bulb, float glow)
        {
            yesFill.color = yes ? XgPalette.Accent : XgPalette.Button; yesText.color = yes ? Color.white : XgPalette.Ink;
            noFill.color = yes ? XgPalette.Button : XgPalette.Accent; noText.color = yes ? XgPalette.Ink : Color.white;
            bulb.color = Color.Lerp(new Color32(200, 204, 214, 255), yes ? XgPalette.Gold : new Color32(150, 170, 220, 255), glow);
        }

        /// <summary>⑥ one conclusion line (the model's if it came in time) and one limit line.</summary>
        IEnumerator Words(Reading r, float hold)
        {
            for (float t = 0; t < ModelWait && r.modelAsked && !r.modelDone; t += Time.unscaledDeltaTime) yield return null;
            var sec = Section("Words");
            var first = Label(At("Conclusion", sec, 0, -285, 1300, 50), "", 28, Color.white, TextAlignmentOptions.Center);
            var second = Label(At("Limit", sec, 0, -340, 1300, 44), "", 21, Soft, TextAlignmentOptions.Center);
            string line = string.IsNullOrEmpty(r.model) ? r.conclusion : r.model;
            yield return PrologueDesk.Type(first, "", line, .05f);
            yield return PrologueDesk.Wait(.5f);
            yield return PrologueDesk.Type(second, "", r.limit, .045f);
            yield return PrologueDesk.Wait(hold);
        }

        // ───────────── QA (Play mode only; these write to the running save) ─────────────

        /// <summary>
        /// QA: fills her chat with <paramref name="lines"/> lines over 12 calendar days ending in a cold 「哦」, sets her
        /// affection, marks a cold signal now and lifts the lab to stage 5 if it is lower. The trigger then fires on
        /// its own within a second or two (inner voice, then the chip on the 对话 page).
        /// Call from the editor console in Play mode, e.g. through unityMCP execute_code:
        /// <c>LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.QaSeed(48);</c>
        /// </summary>
        public static string QaSeed(int affection = 48, int lines = 90)
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var controller = FindAnyObjectByType<XingGuangController>();
            if (runtime == null || runtime.Sim == null || controller == null || controller.Sim == null) return "no running game";
            if (!TryHub(out var hub, out var gf, out var conv)) return "YY or her conversation is missing";
            var save = runtime.Sim.S;
            double now = save.gameSeconds;
            string[] hers = { "今天室友又把我的酸奶喝了", "下周三考概率论，你说我能过吗？", "[微笑]", "你在干嘛", "晚安笨蛋", "我们宿舍断网了", "欢乐颂更新了！", "嗯", "你还不睡吗", "好想吃火锅[抠鼻]" };
            string[] mine = { "在弄电脑", "哈哈", "晚安", "能过的", "刚看到", "在呢", "嗯嗯" };
            var rng = new System.Random(7);
            for (int i = 0; i < lines; i++)
            {
                // Spread over the last 12 days of play time; earlier than the save began they date back before the start.
                double at = now - (12 * 86400.0) * (lines - i) / lines;
                bool her = rng.Next(100) < 55;
                conv.messages.Add(new YYMessage { from = her ? YYChatHub.GirlfriendId : YYChatHub.Me, text = her ? hers[rng.Next(hers.Length)] : mine[rng.Next(mine.Length)], gameSeconds = at });
            }
            conv.messages.Add(new YYMessage { from = YYChatHub.GirlfriendId, text = "哦", gameSeconds = now - 60 });
            conv.messages.Sort((a, b) => a.gameSeconds.CompareTo(b.gameSeconds));
            gf.affection = Mathf.Clamp(affection, 0, 100);
            gf.totalMessages = Math.Max(gf.totalMessages, conv.messages.Count);
            int today = GameCalendar.CurrentDay(save);
            gf.firstMessageDay = Math.Max(0, today - 12);
            gf.today = today;
            gf.lastColdSignalAt = Math.Max(1, now - 60);
            gf.lastColdSignalDay = today;
            if (controller.Sim.S.stage < XgSim.LoveStage) controller.Sim.S.stage = XgSim.LoveStage;
            if (instance != null) { instance.stats = null; instance.nextCheck = 0; }
            return "seeded " + (lines + 1) + " lines, affection " + gf.affection;
        }

        /// <summary>QA: plays the scene now on the current chat, skipping trigger, consent and cooldown (true = full, false = re-ask).</summary>
        public static string QaPlay(bool full = true)
        {
            if (instance == null) return "not installed";
            if (Playing) return "already playing";
            if (instance.bound == null) return "no lab";
            instance.requested = true; instance.requestFull = full; instance.requestedAt = Time.unscaledTime;
            return "requested";
        }
    }
}
