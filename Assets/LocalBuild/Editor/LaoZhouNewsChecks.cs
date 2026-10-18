#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

// Isolated EditMode consumer checks. Timed iterators are advanced manually; no real-time/UI/disk-save claim.
public static class LaoZhouNewsChecks
{
    const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static int assertions;
    static void Need(bool condition, string label) { assertions++; if (!condition) throw new Exception("LZ2: " + label); }
    static FieldInfo Field(Type type, string name) => type.GetField(name, F) ?? throw new MissingFieldException(type.FullName, name);
    static T Get<T>(object owner, string name) => (T)Field(owner.GetType(), name).GetValue(owner);
    static void Set(object owner, string name, object value) => Field(owner.GetType(), name).SetValue(owner, value);
    static object Call(object owner, string method, params object[] args)
    {
        try { return owner.GetType().GetMethod(method, F).Invoke(owner, args); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
    }
    static TextAsset Asset(string path) { var asset = Resources.Load<TextAsset>(path); Need(asset != null, "actual resource " + path); return asset; }
    static List<object> Available(YYChatHub hub) => ((IEnumerable)Call(hub, "AvailableGroupPresets")).Cast<object>().ToList();
    static object Preset(YYChatHub hub, string id) => Available(hub).FirstOrDefault(p => Get<string>(p, "id") == id);
    static Queue<string> Queue(YYChatHub hub) => Get<Queue<string>>(hub, "presetReplyQueue");
    static YYConversation Group(YYChatHub hub) => hub.S.conversations.Find(c => c.id == "netbar");
    static void Date(ChapterOneRuntime runtime, DateTime day)
    {
        var state = runtime.Sim.S;
        state.eraMarks = new List<EraMark> { new EraMark { at = 0, day = GameCalendar.DayIndex(day) } };
        state.newYearAt = 0;
        Need(GameCalendar.Now(state).Date == day.Date, "real game calendar " + day.ToString("yyyy-MM-dd"));
    }
    static IEnumerator Delivery(YYChatHub hub) => (IEnumerator)Call(hub, "DeliverPresetReplies", hub.S, Get<int>(hub, "presetReplyGeneration"));
    static void DeliverAll(YYChatHub hub, string[] expected)
    {
        int before = Group(hub).messages.Count;
        var iterator = Delivery(hub);
        try
        {
            bool waiting = iterator.MoveNext();
            for (int i = 0; i < expected.Length; i++)
            {
                Need(waiting && iterator.Current is WaitForSecondsRealtime, "actual delivery yields its normal wait");
                waiting = iterator.MoveNext();
                Need(Group(hub).messages.Count == before + i + 1, "exactly one Receive per resumed wait");
                Need(Group(hub).messages.Last().text == expected[i], "actual Receive preserves localized reply/order");
            }
            Need(!waiting && Queue(hub).Count == 0, "actual iterator exhausts and clears queue");
        }
        finally { (iterator as IDisposable)?.Dispose(); }
    }
    static void News()
    {
        Asset("LingGuangV05/Browser/browser_news");
        Field(typeof(BoduView), "newsLoaded").SetValue(null, false);
        typeof(BoduView).GetMethod("EnsureNews", F).Invoke(null, null);
        Need(XgNewsCatalog.All.Count == 50, "browser actual EnsureNews loaded LZ2's 50 records");
        foreach (string id in new[] { "lz2_t01_alphago", "lz2_t02_xiaoice", "lz2_t03_tay", "lz2_t04_resnet", "n0928", "lz2_t06_siri" })
        {
            var article = XgNewsCatalog.Get(id); Need(article != null, "required article " + id);
            var date = System.DateTime.ParseExact(article.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            Need(!XgNewsCatalog.Available(date.AddDays(-1)).Contains(article), "news hidden day before " + id);
            Need(XgNewsCatalog.Available(date).Contains(article) && XgNewsCatalog.Available(date.AddDays(1)).Contains(article), "news available on/after date " + id);
            for (int stage = 1; stage <= 6; stage++) Need(!XgNewsCatalog.Home(date.AddDays(-1), stage).Contains(article), "stage cannot reveal future " + id);
            Need(!string.IsNullOrWhiteSpace(article.body) && !string.IsNullOrWhiteSpace(article.bodyEn) && article.body != article.bodyEn, "paired distinct news " + id);
            Need(article.sourceUrl.StartsWith("https://", StringComparison.Ordinal), "parsed factual source " + id);
            foreach (bool english in new[] { false, true }) using (GameText.Bind(() => english ? GameText.English : GameText.Chinese))
            {
                Need(GameText.T(article.title, article.titleEn) == (english ? article.titleEn : article.title), "actual title localization " + id);
                Need(GameText.T(article.body, article.bodyEn) == (english ? article.bodyEn : article.body), "actual body localization " + id);
            }
        }
        Need(!XgNewsCatalog.Home(new DateTime(2016, 5, 31), 6).Any(a => a.id == "n0928"), "stage six May cannot reveal September GNMT");
        Field(typeof(EraContent), "events").SetValue(null, null);
        Asset(EraContent.ResourcePath);
        foreach (string id in new[] { "ai_tay", "gnmt", "lz2_t03_discussion", "lz2_t04_discussion" })
        {
            var e = EraContent.Events.Get(id); Need(e != null, "actual EraContent resource " + id);
            Need(!EraContent.Events.Visible(e.channel, e.date.AddDays(-1)).Contains(e), "era hidden before " + id);
            Need(EraContent.Events.Visible(e.channel, e.date).Contains(e), "era available on date " + id);
            Need(!EraEvents.IsPush(e.channel), "no added auto-push " + id);
            foreach (bool english in new[] { false, true }) using (GameText.Bind(() => english ? GameText.English : GameText.Chinese))
            {
                Need(EraContent.Title(e) == (english ? e.titleEn : e.title), "actual era title localization " + id);
                Need(EraContent.Text(e) == (english ? e.textEn : e.text), "actual era body localization " + id);
            }
        }
        Need(EraContent.Events.Get("gnmt").date == new DateTime(2016, 9, 28), "era GNMT corrected date");
    }
    static void GroupConsumers(ChapterOneRuntime runtime, YYChatHub hub)
    {
        Asset("LingGuangV05/YY/yy_group_presets");
        Field(typeof(YYChatHub), "groupPresets").SetValue(null, null);
        var cases = new[] { ("g29", new DateTime(2016, 5, 25)), ("lz2_g_xiaoice", new DateTime(2016, 5, 29)),
            ("lz2_g_gnmt", new DateTime(2016, 9, 28)), ("lz2_g_voice", new DateTime(2016, 6, 10)) };
        foreach (var item in cases)
        {
            Date(runtime, item.Item2.AddDays(-1)); Need(Preset(hub, item.Item1) == null, "actual group filter hides day before " + item.Item1);
            Date(runtime, item.Item2); var preset = Preset(hub, item.Item1); Need(preset != null, "actual JsonUtility + group filter opens on date " + item.Item1);
            Date(runtime, item.Item2.AddDays(1)); Need(Preset(hub, item.Item1) != null, "group remains available day after " + item.Item1);
            Date(runtime, item.Item2);
            foreach (bool english in new[] { false, true }) using (GameText.Bind(() => english ? GameText.English : GameText.Chinese))
            {
                var choices = Available(hub); hub.S.groupPresetTurn = choices.FindIndex(p => Get<string>(p, "id") == item.Item1);
                string selected = Get<string>(preset, english ? "textEn" : "text");
                var replies = Get<string[]>(preset, english ? "repliesEn" : "replies");
                Need(hub.ChoicesOf("netbar")[0] == selected, "public ChoicesOf localizes actual preset " + item.Item1);
                int before = Group(hub).messages.Count, turn = hub.S.groupPresetTurn;
                hub.Send("netbar", selected);
                Need(Group(hub).messages.Count == before + 1 && Group(hub).messages.Last().from == YYChatHub.Me, "public Send records actual player choice");
                Need(hub.S.groupPresetTurn == turn + 1 && Queue(hub).SequenceEqual(replies), "Send traverses real preset queue in selected language");
                DeliverAll(hub, replies);
            }
        }
        Date(runtime, new DateTime(2016, 5, 31)); Need(Preset(hub, "lz2_g_gnmt") == null, "May group excludes September news");
    }
    static void SaveAndRebind(ChapterOneRuntime runtime, YYChatHub hub, bool english)
    {
        using (GameText.Bind(() => english ? GameText.English : GameText.Chinese))
        {
            Date(runtime, new DateTime(2016, 9, 28));
            var preset = Preset(hub, "lz2_g_gnmt");
            string selected = Get<string>(preset, english ? "textEn" : "text");
            var replies = Get<string[]>(preset, english ? "repliesEn" : "replies");
            hub.Send("netbar", selected);
            var oldState = hub.S; var stale = Delivery(hub);
            try
            {
                Need(stale.MoveNext() && stale.Current is WaitForSecondsRealtime, "old actual iterator waits before save");
                Call(hub, "WriteToSave"); // The real YY Saving subscriber; memory runtime SaveNow intentionally skips persistence.
                Need(!string.IsNullOrEmpty(runtime.Sim.S.chatState), "real WriteToSave produces YY JSON");
                var roundtrip = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S));
                int turn = hub.S.groupPresetTurn, messages = Group(hub).messages.Count;
                runtime.SetSimulationForTests(new ChapterOneSim(roundtrip)); // Changed -> real YY OnRuntimeChanged -> BindSave.
                Need(!ReferenceEquals(oldState, hub.S), "actual runtime event rebinds a new YY state");
                Need(Group(hub).messages.Count == messages && Group(hub).messages.Last().text == selected && hub.S.groupPresetTurn == turn,
                    "JsonUtility save/rebind preserves real player history and rotation");
                Need(!hub.GroupScriptPlaying, "stale save owner cannot report active group playback");
                // Queue another real preset before the old iterator wakes. Its stale finally must not erase new work.
                hub.Send("netbar", selected); var next = Queue(hub).ToArray();
                Need(next.SequenceEqual(replies), "new save receives only its newly requested queue");
                Need(!stale.MoveNext(), "old iterator stops after actual save rebind");
                Need(Queue(hub).SequenceEqual(next) && Group(hub).messages.Count == messages + 1, "stale wake/finally cannot deliver or clear new-save work");
                DeliverAll(hub, replies);
                Call(hub, "WriteToSave");
                int delivered = Group(hub).messages.Count;
                var saved = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S));
                runtime.SetSimulationForTests(new ChapterOneSim(saved));
                Need(Group(hub).messages.Count == delivered && Queue(hub).Count == 0, "delivered replies retained without replay after second save/rebind");
            }
            finally { (stale as IDisposable)?.Dispose(); }
        }
    }
    static void FocusGate(ChapterOneRuntime runtime, YYChatHub hub)
    {
        Date(runtime, new DateTime(2016, 6, 10)); var preset = Preset(hub, "lz2_g_voice");
        using (GameText.Bind(() => GameText.Chinese)) hub.Send("netbar", Get<string>(preset, "text"));
        int before = Group(hub).messages.Count; var iterator = Delivery(hub);
        var prior = StoryFocus.Probe;
        try
        {
            Need(iterator.MoveNext(), "focus fixture reaches timed boundary"); StoryFocus.Probe = () => true;
            Need(iterator.MoveNext() && iterator.Current == null && Group(hub).messages.Count == before, "actual delivery holds while story focus is active");
            StoryFocus.Probe = () => false;
            Need(iterator.MoveNext() && Group(hub).messages.Count == before + 1, "actual delivery resumes after story focus releases");
        }
        finally { StoryFocus.Probe = prior; (iterator as IDisposable)?.Dispose(); }
    }
    [MenuItem("Tools/Local Verification/Lao Zhou historical news consumers")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || YYChatHub.Instance != null)
            throw new InvalidOperationException("Run in isolated EditMode with no initialized YY hub.");
        if (StoryFocus.Active) throw new InvalidOperationException("Run outside a focused story.");
        assertions = 0;
        var oldRandom = UnityEngine.Random.state;
        var news = (List<XgNewsArticle>)Field(typeof(XgNewsCatalog), "articles").GetValue(null); var priorNews = news.ToArray();
        var newsLoaded = Field(typeof(BoduView), "newsLoaded").GetValue(null);
        var priorEra = Field(typeof(EraContent), "events").GetValue(null);
        var priorPresets = Field(typeof(YYChatHub), "groupPresets").GetValue(null);
        var fixture = new GameObject("LZ2 isolated consumer verification"); fixture.SetActive(false);
        YYChatHub hub = null;
        try
        {
            var runtime = fixture.AddComponent<ChapterOneRuntime>(); runtime.useDiskSave = false; runtime.tickEnabled = false;
            runtime.SetSimulationForTests(new ChapterOneSim());
            hub = fixture.AddComponent<YYChatHub>(); hub.Initialize(runtime, null);
            News(); GroupConsumers(runtime, hub); SaveAndRebind(runtime, hub, false); SaveAndRebind(runtime, hub, true); FocusGate(runtime, hub);
            Debug.Log("LAO_ZHOU_NEWS_PASS " + assertions + " assertions: Resources/real browser and era loaders, actual YY JsonUtility/filter/ChoicesOf/Send/Receive, bilingual order, real YY WriteToSave + GameState round-trip + runtime-event rebind, stale queue and story-focus guards. EditMode manual iterator stepping; no real-time, physical UI or disk-save claim.");
        }
        finally
        {
            // Unity does not guarantee OnDestroy for a GameObject that was never active.
            if (hub != null) Call(hub, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(fixture);
            news.Clear(); news.AddRange(priorNews); Field(typeof(BoduView), "newsLoaded").SetValue(null, newsLoaded);
            Field(typeof(EraContent), "events").SetValue(null, priorEra); Field(typeof(YYChatHub), "groupPresets").SetValue(null, priorPresets);
            UnityEngine.Random.state = oldRandom;
        }
    }
    // Optional existing real-time PlayMode lifecycle regression. Parent runs this in its isolated PlayMode runner.
    public static IEnumerator PlaybackFrames() => LifeGroupPlaybackChecks.Frames();
}
#endif
