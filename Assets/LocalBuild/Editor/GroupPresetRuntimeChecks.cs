#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEngine;

// Actual hub/Send/Receive/JsonUtility, inactive isolated fixture, manual iterator steps.
// This does not claim real-time 8/3-second waits, UI input, disk IO or native model inference.
public static class GroupPresetRuntimeChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int checks;
    static void Need(bool ok, string label) { checks++; if (!ok) throw new Exception("GROUP_LIFECYCLE: " + label); }
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, F).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, F).SetValue(o, value);
    static object Call(object o, string name, params object[] args)
    {
        try { return o.GetType().GetMethod(name, F).Invoke(o, args); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
    }
    static YYConversation Group(YYChatHub h) => h.Conversation(YYChatHub.EraGroup);
    static Queue<string> Replies(YYChatHub h) => Get<Queue<string>>(h, "presetReplyQueue");
    static Dictionary<string, float> Pending(YYChatHub h) => Get<Dictionary<string, float>>(h, "pending");
    static List<object> Presets(YYChatHub h) => ((IEnumerable)Call(h, "AvailableGroupPresets")).Cast<object>().ToList();
    static IEnumerator Cold(YYChatHub h) => (IEnumerator)Call(h, "DeliverGroupColdScenes", h.S, Get<int>(h, "groupColdGeneration"));
    static void Fresh(ChapterOneRuntime runtime)
    {
        var s = new ChapterOneSim();
        GameCalendar.Advance(s.S, GameCalendar.DayIndex(new DateTime(2016, 6, 6)));
        runtime.SetSimulationForTests(s);
    }
    static void Wait(IEnumerator e, float seconds, string label)
    {
        Need(e.MoveNext() && e.Current is WaitForSecondsRealtime, label + " wait object");
        Need(Math.Abs(((WaitForSecondsRealtime)e.Current).waitTime - seconds) < .001, label + " wait duration");
    }
    static void CountsAndAuthored(ChapterOneRuntime runtime, YYChatHub h)
    {
        Fresh(runtime);
        var initial = h.ChoicesOf(YYChatHub.EraGroup);
        Need(initial.Length == 3, "fresh day has three actual resources");
        h.Send(YYChatHub.EraGroup, "ordinary fixture one");
        Need(h.S.groupPresets.weeklyFreeInputs == 1, "actual first free Send counts once");
        int generation = Get<int>(h, "groupReplyGeneration");
        Pending(h)[YYChatHub.EraGroup] = 999f;
        Get<HashSet<string>>(h, "inFlight").Add(YYChatHub.EraGroup);
        h.Send(YYChatHub.EraGroup, initial[0]);
        var authored = Replies(h).ToArray();
        Need(authored.Length > 0 && h.S.groupPresets.weeklyFreeInputs == 1, "preset keeps authored queue and skips free counter");
        Need(!Pending(h).ContainsKey(YYChatHub.EraGroup) && !Get<HashSet<string>>(h, "inFlight").Contains(YYChatHub.EraGroup)
            && generation != Get<int>(h, "groupReplyGeneration"), "preset invalidates older generic request ownership");
        Need(h.ChoicesOf(YYChatHub.EraGroup).Length == 2, "actual Send removes one bubble without refill");
        h.Send(YYChatHub.EraGroup, "ordinary fixture two");
        Need(h.S.groupPresets.weeklyFreeInputs == 2 && h.S.groupPresets.pendingColdScenes.Count == 1, "actual second free Send is cold");
        Need(Replies(h).SequenceEqual(authored), "cold turn leaves preset replies intact");
        h.Send(YYChatHub.EraGroup, "sticker fixture", "smile");
        Need(h.S.groupPresets.weeklyFreeInputs == 2, "sticker never counts");
        h.Script(YYChatHub.EraGroup, _ => true);
        h.Send(YYChatHub.EraGroup, "guide choice fixture"); h.EndScript();
        Need(h.S.groupPresets.weeklyFreeInputs == 2, "handled guide never counts");
        h.Send(YYChatHub.EraGroup, "ordinary fixture three");
        h.Send(YYChatHub.EraGroup, "ordinary fixture four");
        h.Send(YYChatHub.EraGroup, "ordinary fixture five");
        Need(h.S.groupPresets.weeklyFreeInputs == 5 && h.S.groupPresets.pendingColdScenes.Count == 2, "actual fifth free Send is second cold scene");

        Fresh(runtime);
        h.Send(YYChatHub.EraGroup, "ordinary before joke");
        h.Send(YYChatHub.EraGroup, "GPT");
        var joke = Replies(h).ToArray();
        Need(joke.Length == 2 && h.S.groupPresets.weeklyFreeInputs == 1, "future-name author reply is queued without counting");
        h.Send(YYChatHub.EraGroup, "ordinary after joke");
        Need(h.S.groupPresets.pendingColdScenes.Count == 1 && Replies(h).SequenceEqual(joke), "future joke then second free Send retains both author lines");
        var old = h.S; int oldGeneration = Get<int>(h, "presetReplyGeneration");
        var oldIterator = (IEnumerator)Call(h, "DeliverPresetReplies", old, oldGeneration);
        Need(oldIterator.MoveNext(), "old author iterator waits");
        Fresh(runtime);
        h.Send(YYChatHub.EraGroup, "GPT"); var newQueue = Replies(h).ToArray();
        Need(!oldIterator.MoveNext() && Replies(h).SequenceEqual(newQueue), "old author iterator cannot clear new-save queue");
        (oldIterator as IDisposable)?.Dispose();

        Fresh(runtime);
        h.Send(YYChatHub.EraGroup, "ordinary before legacy quiz");
        Get<Dictionary<string, string>>(h, "quizReplies")[YYChatHub.EraGroup] = "authored old quiz fixture";
        Pending(h)[YYChatHub.EraGroup] = 99f;
        h.Send(YYChatHub.EraGroup, "ordinary while old quiz is queued");
        Need(h.S.groupPresets.weeklyFreeInputs == 1 && Get<Dictionary<string, string>>(h, "quizReplies").ContainsKey(YYChatHub.EraGroup), "pending old quiz is not swallowed or counted");
    }
    static void History(ChapterOneRuntime runtime, YYChatHub h)
    {
        Fresh(runtime);
        var p = Presets(h); Need(p.Count >= 4, "history fixture has four real resources");
        var old = new YYState { groupPresets = null, groupPresetTurn = 999 };
        old.conversations.Add(new YYConversation { id = YYChatHub.EraGroup });
        var lines = old.conversations[0].messages;
        lines.Add(new YYMessage { from = YYChatHub.Me, text = Get<string>(p[0], "text") });
        lines.Add(new YYMessage { from = YYChatHub.Me, text = Get<string>(p[1], "textEn") });
        lines.Add(new YYMessage { from = YYChatHub.Me, text = Get<string>(p[2], "text"), byAi = true });
        lines.Add(new YYMessage { from = YYChatHub.Me, text = Get<string>(p[3], "text") + "!" });
        lines.Add(new YYMessage { from = YYChatHub.Me, text = Get<string>(p[3], "text"), sticker = "smile" });
        var state = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S));
        state.chatState = JsonUtility.ToJson(old);
        runtime.SetSimulationForTests(new ChapterOneSim(state));
        var sent = h.S.groupPresets.sentIds;
        Need(sent.Count == 2 && sent.Contains(Get<string>(p[0], "id")) && sent.Contains(Get<string>(p[1], "id")), "legacy imports exact real player Chinese and English only");
        Need(h.S.groupPresets.weeklyFreeInputs == 0 && h.S.groupPresetTurn == 999, "legacy counter never fabricates sent history or weekly turns");
        Fresh(runtime); Need(h.S.groupPresets.sentIds.Count == 0, "different save inherits no sent ids");
    }
    static void ColdResume(ChapterOneRuntime runtime, YYChatHub h)
    {
        Fresh(runtime);
        h.Send(YYChatHub.EraGroup, "cold fixture one"); h.Send(YYChatHub.EraGroup, "cold fixture two");
        int before = Group(h).messages.Count;
        var e = Cold(h);
        try
        {
            Wait(e, 8f, "first cold line");
            StoryFocus.Probe = () => true;
            Need(e.MoveNext() && e.Current == null && Group(h).messages.Count == before, "story focus retains cold line");
            StoryFocus.Probe = null;
            Set(h, "presetReplyRunning", true);
            Need(e.MoveNext() && e.Current == null && Group(h).messages.Count == before, "author queue retains cold line");
            Set(h, "presetReplyRunning", false);
            h.Script(YYChatHub.EraGroup, _ => false);
            Need(e.MoveNext() && e.Current == null && Group(h).messages.Count == before, "guide script retains cold line");
            h.EndScript();
            using (GameText.Bind(() => GameText.Chinese)) Wait(e, 3f, "second cold line");
            Need(Group(h).messages.Count == before + 1 && h.S.groupPresets.pendingColdScenes[0].nextLine == 1, "first line delivered and progress kept together");
            Call(h, "WriteToSave");
            var restored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(runtime.Sim.S));
            runtime.SetSimulationForTests(new ChapterOneSim(restored));
            Need(h.S.groupPresets.pendingColdScenes[0].nextLine == 1, "actual JsonUtility + runtime rebind keeps partial scene");
            Set(h, "groupColdOwner", h.S);
            Need(!e.MoveNext() && ReferenceEquals(Get<YYState>(h, "groupColdOwner"), h.S), "old cold iterator/finally cannot mutate new owner");
            var resumed = Cold(h);
            try
            {
                Wait(resumed, 3f, "restored remaining line");
                using (GameText.Bind(() => GameText.English)) Need(!resumed.MoveNext(), "resumed second line completes scene");
                Need(Group(h).messages.Count == before + 2 && Group(h).messages.Last().text == GroupPresetLifecycle.ColdLine(1, 1, true), "only remaining line delivered in current language");
                Need(h.S.groupPresets.pendingColdScenes.Count == 0, "completed cold scene removed");
            }
            finally { (resumed as IDisposable)?.Dispose(); }
        }
        finally { (e as IDisposable)?.Dispose(); StoryFocus.Probe = null; Set(h, "presetReplyRunning", false); }
    }
    [MenuItem("Tools/Local Verification/Group preset lifecycle")]
    public static void Run()
    {
        if (Application.isPlaying || YYChatHub.Instance != null || StoryFocus.Active)
            throw new InvalidOperationException("Run in isolated EditMode without a live YY hub/story.");
        checks = 0;
        var random = UnityEngine.Random.state; var probe = StoryFocus.Probe;
        var priorPresets = typeof(YYChatHub).GetField("groupPresets", F).GetValue(null);
        var fixture = new GameObject("Group lifecycle isolated actual consumer checks"); fixture.SetActive(false);
        YYChatHub hub = null;
        try
        {
            var runtime = fixture.AddComponent<ChapterOneRuntime>(); runtime.useDiskSave = false; runtime.tickEnabled = false;
            runtime.SetSimulationForTests(new ChapterOneSim());
            hub = fixture.AddComponent<YYChatHub>(); hub.Initialize(runtime, null);
            foreach (bool en in new[] { false, true }) using (GameText.Bind(() => en ? GameText.English : GameText.Chinese)) CountsAndAuthored(runtime, hub);
            History(runtime, hub); ColdResume(runtime, hub);
            Debug.Log("GROUP_PRESET_RUNTIME_PASS " + checks + " actual Send/Receive/JsonUtility and manually stepped iterator assertions; no real-time/UI/disk/model claim.");
        }
        finally
        {
            StoryFocus.Probe = probe; UnityEngine.Random.state = random;
            if (hub != null) Call(hub, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(fixture);
            typeof(YYChatHub).GetField("groupPresets", F).SetValue(null, priorPresets);
        }
    }
}
#endif
