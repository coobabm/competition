#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine.UI;

// Dedicated batch invocation: -executeMethod StreakFeedbackChecks.Run (without -quit).
// Starts an unsaved empty scene, never Main, and never changes Enter Play Mode options.
[InitializeOnLoad]
public static partial class StreakFeedbackChecks
{
    const string Key="StreakFeedbackChecks.Directory";
    const string OldOverrideKey="StreakFeedbackChecks.OldOverride";
    static string Root=>SessionState.GetString(Key,"");
    static Report report;
    static double deadline;
    static bool finishing;
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion,detailReport="";
        public bool enterPlayModeOptionsEnabled;
        public int enterPlayModeOptions;
        public List<string> errors=new List<string>();
        public string scope="Batch-only empty scene; actual streak sounds, reduced motion and hidden-window coroutine. No game runtime or Main scene is loaded. Editor play-mode settings are observed, never modified.";
    }
    static StreakFeedbackChecks(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use a dedicated batch Editor for this runner");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-StreakFeedbackBatch-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        SessionState.SetString(Key,dir);SessionState.SetString(OldOverrideKey,ChapterOneRuntime.VerificationSaveDirectory??"");
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(dir,"UnusedIsolatedSave");
        // Prevent an existing editor scene (and any real save owner in it) from being played.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        report=NewReport();Save();EditorApplication.EnterPlaymode();
    }
    static Report NewReport()=>new Report{unityVersion=Application.unityVersion,
        enterPlayModeOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,enterPlayModeOptions=(int)EditorSettings.enterPlayModeOptions};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IsolateBeforeLoad()
    {
        if(string.IsNullOrEmpty(Root))return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"UnusedIsolatedSave");
    }
    static void State(PlayModeStateChange state)
    {
        if(string.IsNullOrEmpty(Root))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            report=NewReport();finishing=false;deadline=EditorApplication.timeSinceStartup+120;
            Application.logMessageReceived+=Log;EditorApplication.update+=Watchdog;
            // Empty scene is an invariant, not merely a setup intention.
            if(UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(FindObjectsInactive.Include)!=null)
            {Complete(false,"Unexpected runtime exists in empty-scene fixture");return;}
            var go=new GameObject("Streak feedback isolated runner");
            go.AddComponent<StreakFeedbackDriver>().StartChecks();
        }
        else if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;
            string dir=Root;bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            string previous=SessionState.GetString(OldOverrideKey,"");ChapterOneRuntime.VerificationSaveDirectory=previous.Length==0?null:previous;
            SessionState.EraseString(OldOverrideKey);SessionState.EraseString(Key);
            Debug.Log("Streak feedback batch "+(passed?"PASS":"FAIL")+": "+Path.Combine(dir,"report.json"));
            EditorApplication.Exit(passed?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(report==null||finishing)return;
        const string prefix="StreakFeedbackDetails ";
        if(message.StartsWith(prefix,StringComparison.Ordinal))
        {
            int i=message.IndexOf(": ",StringComparison.Ordinal);if(i>=0)report.detailReport=message.Substring(i+2);
        }
        if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.errors.Add(message+"\n"+stack);
    }
    static void Watchdog()
    {
        if(!finishing&&EditorApplication.timeSinceStartup>deadline)Complete(false,"Timed out after 120 seconds");
    }
    public static void Complete(bool passed,string failure)
    {
        if(finishing)return;
        finishing=true;
        if(report==null)report=NewReport();
        passed=passed&&report.errors.Count==0;
        if(string.IsNullOrEmpty(report.detailReport)){passed=false;failure+=" Missing detailed streak-feedback report.";}
        report.status=passed?"passed":"failed";report.failure=failure??"";Save();
        if(passed)File.WriteAllText(Path.Combine(Root,"passed.txt"),"Streak feedback coroutine passed in empty scene");
        Application.logMessageReceived-=Log;EditorApplication.update-=Watchdog;EditorApplication.ExitPlaymode();
    }
    static void Save(){if(!string.IsNullOrEmpty(Root)&&report!=null)File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));}
}
public sealed class StreakFeedbackDriver : MonoBehaviour
{
    public void StartChecks(){StartCoroutine(Execute());}
    IEnumerator Execute()
    {
        var checks=StreakFeedbackChecks.Frames();string error=null;
        try
        {
            while(true)
            {
                bool more=false;object current=null;
                try{more=checks.MoveNext();if(more)current=checks.Current;}catch(Exception e){error=e.ToString();}
                if(error!=null||!more)break;
                yield return current;
            }
        }
        finally{(checks as IDisposable)?.Dispose();}
        // Allow deferred destruction/unload callbacks to run before deciding the outcome.
        yield return null;yield return null;
        StreakFeedbackChecks.Complete(error==null,error);
    }
}
public static partial class StreakFeedbackChecks
{
    [Serializable] sealed class Detail
    {
        public string status = "running";
        public List<string> passed = new List<string>();
    }
    static T Read<T>(object instance, string name) => (T)typeof(XgJuice).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    static int AudioIndex => (int)typeof(XgJuice.Sfx).GetField("next", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    static AudioSource LastSource => ((AudioSource[])typeof(XgJuice.Sfx).GetField("sources", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))[AudioIndex];
    static void Assert(Detail detail, bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("Streak feedback: " + label);
        detail.passed.Add(label);
    }
    static bool NoMotion(XgJuice fx, RectTransform badge) =>
        fx.ActiveFloaters == 0 && fx.ActiveParticles == 0 && Read<IList>(fx, "waves").Count == 0 &&
        Read<IList>(fx, "blinks").Count == 0 && !fx.HasActiveKnock(badge) && !fx.IsHitStopped &&
        Read<float>(fx, "flashDuration") == 0 && Read<float>(fx, "shakeDuration") == 0;
    public static IEnumerator Frames()
    {
        var detail = new Detail();
        var root = new GameObject("Isolated streak fixture", typeof(RectTransform), typeof(Canvas), typeof(AudioListener));
        float oldTimeScale = Time.timeScale;
        bool visible = true;
        XgJuice fx = null;
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var rect = (RectTransform)root.transform;
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            var badge = (RectTransform)badgeGo.transform; badge.SetParent(rect, false);
            var walletGo = new GameObject("Wallet", typeof(RectTransform));
            var wallet = (RectTransform)walletGo.transform; wallet.SetParent(rect, false); wallet.anchoredPosition = new Vector2(300, 200);
            fx = root.AddComponent<XgJuice>();
            fx.DisableMotionWhenReduced = true; fx.LocalHitStop = true; fx.Visible = () => visible;
            var font = XgUiV4Fonts.Sans;
            Assert(detail, font != null, "Shipped exe font loads");
            fx.Init(rect, rect, font);
            foreach (string name in new[] { "Up2", "Up3", "Up4", "Up5", "Hit2", "Hit3", "Hit4", "Hit5", "Drop" })
            {
                var clip = XgJuice.Sfx.StreakClip(name);
                Assert(detail, clip != null && clip == Resources.Load<AudioClip>("XgSfx/Streak/" + name), name + " is an actual Resources clip");
                float limit = Time.realtimeSinceStartup + 10;
                clip.LoadAudioData();
                while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < limit) yield return null;
                Assert(detail, clip.loadState == AudioDataLoadState.Loaded && clip.samples > 0 && clip.channels > 0 && clip.length > 0, name + " audio data loads with nonempty samples");
            }
            for (int tier = 2; tier <= 5; tier++)
            {
                fx.Clear(); int audio = AudioIndex;
                fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, tier);
                Assert(detail, AudioIndex != audio && LastSource.clip == XgJuice.Sfx.StreakClip("Up" + tier), "Tier " + tier + " uses its own promotion clip");
                Assert(detail, fx.ActiveFloaters > 0 && Read<IList>(fx, "waves").Count > 0 && fx.HasActiveKnock(badge), "Tier " + tier + " starts visible callout, ring and badge bounce");
                Assert(detail, fx.ActiveParticles <= XgJuice.ParticleLimit && fx.ActiveFloaters <= XgJuice.FloaterLimit, "Tier " + tier + " respects effect pool limits");
                yield return null;
                fx.Clear(); audio = AudioIndex;
                fx.StreakHit(Vector2.zero, tier);
                Assert(detail, AudioIndex != audio && LastSource.clip == XgJuice.Sfx.StreakClip("Hit" + tier) && fx.ActiveFloaters > 0, "Tier " + tier + " regular hit keeps tier sound and stamp");
                fx.Clear(); audio = AudioIndex;
                fx.StreakDrop(Vector2.zero, badge, tier, 1);
                Assert(detail, AudioIndex != audio && LastSource.clip == XgJuice.Sfx.StreakClip("Drop") && fx.HasActiveKnock(badge), "Tier " + tier + " drop has sound and badge response");
            }
            fx.Clear();
            var up5 = XgJuice.Sfx.StreakClip("Up5");
            fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 5);
            var streakSource = LastSource;
            Assert(detail, streakSource.clip == up5, "Promotion tail source contains Up5 before muting");
            var ordinaryClip = XgJuice.Sfx.Clip(XgJuice.Sfx.Id.Click, 0);
            fx.PlayClip(ordinaryClip);
            var ordinarySource = LastSource;
            Assert(detail, ordinarySource != streakSource && ordinarySource.clip == ordinaryClip, "Unrelated effect occupies a different shared-pool source");
            // A player music source is outside this pool and must remain entirely untouched.
            var musicSource = root.AddComponent<AudioSource>(); musicSource.clip = ordinaryClip;
            XgJuice.Sfx.StopStreakClips();
            Assert(detail, !streakSource.isPlaying && streakSource.clip == null, "Muting immediately stops and clears the existing promotion tail");
            Assert(detail, ordinarySource.clip == ordinaryClip && musicSource.clip == ordinaryClip, "Stopping streak tails preserves other effects and external music sources");
            Assert(detail, XgJuice.Sfx.StreakClip("Up5") == up5, "Stopping tails does not invalidate the clip cache");
            XgJuice.Sfx.StopStreakClips();
            Assert(detail, ordinarySource.clip == ordinaryClip, "Repeated stop is safe and leaves unrelated clips intact");
            fx.Clear(); fx.Muted = true; int before = AudioIndex;
            fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 5); fx.StreakHit(Vector2.zero, 5); fx.StreakDrop(Vector2.zero, badge, 5, 1);
            Assert(detail, AudioIndex == before && fx.ActiveFloaters > 0, "Mute suppresses all three sounds without suppressing visuals");
            fx.Muted = false; fx.Reduced = true;
            yield return null; yield return null;
            Assert(detail, NoMotion(fx, badge), "Enabling reduce clears ongoing streak motion");
            for (int tier = 2; tier <= 5; tier++)
            {
                before = AudioIndex; fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, tier);
                Assert(detail, AudioIndex != before && NoMotion(fx, badge), "Reduced promotion " + tier + " retains sound but no animation");
                before = AudioIndex; fx.StreakHit(Vector2.zero, tier);
                Assert(detail, AudioIndex != before && NoMotion(fx, badge), "Reduced hit " + tier + " retains sound but no animation");
                before = AudioIndex; fx.StreakDrop(Vector2.zero, badge, tier, 1);
                Assert(detail, AudioIndex != before && NoMotion(fx, badge), "Reduced drop " + tier + " retains sound but no animation");
            }
            fx.Reduced = false; fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 5); visible = false;
            yield return null; yield return null;
            Assert(detail, NoMotion(fx, badge) && badge.localScale == Vector3.one, "Hiding clears ongoing feedback and restores badge");
            before = AudioIndex;
            fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 5); fx.StreakHit(Vector2.zero, 5); fx.StreakDrop(Vector2.zero, badge, 5, 1);
            Assert(detail, AudioIndex == before && NoMotion(fx, badge), "Hidden calls start neither sound nor animation");
            visible = true; fx.StreakHit(Vector2.zero, 2);
            Assert(detail, AudioIndex != before && fx.ActiveFloaters > 0, "Reopening accepts fresh feedback");
            fx.Clear(); before = AudioIndex;
            fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 1); fx.StreakHit(Vector2.zero, 1); fx.StreakDrop(Vector2.zero, badge, 2, 2);
            Assert(detail, AudioIndex == before && NoMotion(fx, badge), "Base multiplier and unchanged drop do not emit streak feedback");
            var sim = new LingGuangV05.XingGuang.XgSim();
            var energyType = typeof(XgJuice).Assembly.GetType("LingGuangV05.Desktop.XingGuang.XgStreakEnergyIndicator", true);
            var energy = Activator.CreateInstance(energyType, new object[] { new XgUi(font, null, true), rect });
            var cells = (Image[])energyType.GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(energy);
            int[] counts = { 0, 1, 2, 3, 8, 9, 0 };
            int[] fills = { 0, 1, 2, 1, 2, 2, 0 };
            for (int i = 0; i < counts.Length; i++)
            {
                sim.S.combo = counts[i]; energyType.GetMethod("Refresh").Invoke(energy, new object[] { sim });
                Color lit = XgJuice.StreakColor(Math.Min(5, sim.StreakMultiplier + 1));
                int filled = 0; foreach (var cell in cells) { if (cell.color == lit) filled++; }
                Assert(detail, filled == fills[i] && !cells[0].raycastTarget && !cells[1].raycastTarget,
                    "Energy cells match streak " + counts[i] + " and do not intercept input");
            }
            fx.Clear();
            foreach (int count in new[] { 19, 21, 49, 51 }) fx.StreakMilestone(Vector2.zero, count);
            Assert(detail, fx.ActiveFloaters == 0, "Milestones never fire on surrounding streak counts");
            foreach (int count in new[] { 20, 50 })
            {
                fx.Clear(); before = AudioIndex;
                fx.ComboHit(Vector2.zero, badge, badge, wallet, count, Vector2.up, Color.white);
                Assert(detail, fx.ActiveFloaters == 2 && AudioIndex == before,
                    "Milestone " + count + " replaces regular count with two lines without extra audio");
            }
            fx.Clear(); fx.Reduced = true;
            fx.StreakMilestone(Vector2.zero, 20); fx.StreakMilestone(Vector2.zero, 50);
            Assert(detail, NoMotion(fx, badge), "Reduced motion disables both milestone celebrations");
            fx.Reduced = false; visible = false;
            fx.StreakMilestone(Vector2.zero, 50);
            Assert(detail, NoMotion(fx, badge), "Hidden window does not start a milestone");
            visible = true;
            int inputActions = 0, settlements = 0;
            var context = new LingGuangV05.Desktop.XingGuang.MiniGames.XgMiniContext(rect,
                new XgUi(font, null, true), sim, null, "sense", () => visible, () => 1,
                text => { }, (correct, seconds) => settlements++);
            bool clickBeforeSpecial = false;
            Action answer = () =>
            {
                inputActions++;
                var clip = LastSource.clip;
                clickBeforeSpecial = false;
                for (int variant = 0; variant < XgJuice.Sfx.VariantsPerEvent; variant++)
                    clickBeforeSpecial |= clip == XgJuice.Sfx.Clip(XgJuice.Sfx.Id.Click, variant);
                fx.StreakUp(Vector2.zero, new Vector2(180, 100), badge, wallet, 2);
            };
            var inputButton = context.Button("Input", 0, 0, 100, 40, "Answer", answer);
            before = AudioIndex; inputButton.button.onClick.Invoke();
            Assert(detail, inputActions == 1 && clickBeforeSpecial && AudioIndex == (before + 2) % 10
                && LastSource.clip == XgJuice.Sfx.StreakClip("Up2"), "Minigame button layers exactly one ordinary click before its special sound");
            fx.Clear(); fx.Reduced = true;
            before = AudioIndex; context.PlayerInput(answer);
            Assert(detail, inputActions == 2 && clickBeforeSpecial && AudioIndex == (before + 2) % 10
                && NoMotion(fx, badge), "Keyboard shared input keeps click and special sound with reduced motion");
            before = AudioIndex; context.Report(true); context.ReportTimed(false, 2);
            Assert(detail, settlements == 2 && AudioIndex == before, "Automatic/report-only settlement never fabricates a player click");
            sim.S.mute = true; fx.Muted = true;
            before = AudioIndex; inputButton.button.onClick.Invoke();
            Assert(detail, inputActions == 3 && AudioIndex == before, "Muted player input still performs action without either sound");
            sim.S.mute = false; fx.Muted = false; visible = false;
            before = AudioIndex; context.PlayerInput(answer);
            Assert(detail, inputActions == 3 && AudioIndex == before, "Hidden minigame input performs neither action nor sound");
            visible = true; context.Close(); context.PlayerInput(answer);
            Assert(detail, inputActions == 3 && AudioIndex == before, "Disposed minigame input cannot play stale click");
            fx.Reduced = false;
            Assert(detail, Time.timeScale == oldTimeScale, "Local hit stop never changes global simulation speed");
            detail.status = "passed";
        }
        finally
        {
            if (fx != null) fx.Clear();
            UnityEngine.Object.Destroy(root);
            Time.timeScale = oldTimeScale;
            string path = Path.Combine(Root, "details.json");
            File.WriteAllText(path, JsonUtility.ToJson(detail, true));
            Debug.Log("StreakFeedbackDetails " + detail.status + ": " + path);
        }
    }
}
#endif
