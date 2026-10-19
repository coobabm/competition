#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.Casino;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LingGuangV05.Desktop.Apps;

namespace LingGuangV05.Editor
{
    /// <summary>Explicit Play QA only. Refuses to mutate any session which has disk saving enabled.</summary>
    public static class CasinoPlayQa
    {
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Report { public bool completed; public int passed, failed; public List<Check> checks = new List<Check>(); }
        public static Report Result;
        public static bool Running;
        static readonly Queue<Action> steps = new Queue<Action>();
        static double next;
        static ChapterOneRuntime runtime;
        static XingGuangController controller;
        static WebBrowserApp browser;
        static WindowManager window;
        static string tabA;
        static double before;
        static int rounds;
        static IDisposable locale;
        static bool withFeedback;
        static Func<bool> storyProbe;
        static string Dir => Path.Combine(Application.dataPath, "..", "Evidence", "Casino");
        static CasinoPage Page => UnityEngine.Object.FindAnyObjectByType<CasinoPage>();
        static XgSim Sim => controller.Sim;

        public static void Begin(bool includeFeedback = false)
        {
            runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (!Application.isPlaying || runtime == null || runtime.useDiskSave) throw new InvalidOperationException("Start Play with useDiskSave=false before casino QA.");
            if (Running) throw new InvalidOperationException("QA already running");
            withFeedback = includeFeedback;
            controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            browser = UnityEngine.Object.FindAnyObjectByType<WebBrowserApp>(FindObjectsInactive.Include);
            window = browser.GetComponentInParent<WindowManager>(true);
            Result = new Report(); steps.Clear(); Running = true;
            runtime.tickEnabled = false; controller.enabled = false;
            // A fresh declared save owner, not a switch disabling the scam for QA.
            var fixture = new XgSim(); fixture.S.stage = XgSim.FeatureUnlock["casino"];
            var house = new LingGuangV05.Core.ChapterOneSim(); house.S.prologue=2;house.S.appInstallState=2;house.S.money=10000;
            house.S.labState=JsonUtility.ToJson(fixture.S);runtime.SetSimulationForTests(house);
            if(!controller.BoundTo(house))throw new InvalidOperationException("Casino fixture owner did not bind");
            Sim.S.stage=XgSim.FeatureUnlock["casino"];Sim.Clock = 100;
            var p = UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
            typeof(PrologueDirector).GetMethod("StopPrologue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(p, null);
            var sd = UnityEngine.Object.FindAnyObjectByType<StoryDirector>(); if (sd != null) sd.enabled = false;
            // A story moment left on screen holds the advert by design (StoryFocus); this QA is about the casino, so lift it.
            storyProbe = LingGuangV05.Core.StoryFocus.Probe; LingGuangV05.Core.StoryFocus.Probe = null;
            window.OpenWindow();
            ChapterOneQaReloadGuard.LockForPlayQa(100);
            steps.Enqueue(() => CasinoDesktop.Instance.OpenSite());
            steps.Enqueue(() => { CheckNow("Native 888.vip opens playable page", Page != null); tabA = browser.currentTabs.First(t => t.tabPage != null && t.tabPage.activeInHierarchy).guid; });
            steps.Enqueue(() => Click("Stake 10"));
            foreach (string g in new[] { "slots", "dice", "roulette", "dragon" })
            {
                string game = g;
                steps.Enqueue(() => { Sim.Clock += 2; Click("Game " + game); });
                steps.Enqueue(() => { before = runtime.Sim.S.money; rounds = Sim.Casino.rounds; Click("Place Bet"); Click("Place Bet"); CheckNow(game + " double click settles once", Sim.Casino.rounds == rounds + 1); });
                steps.Enqueue(() => { }); steps.Enqueue(() => { });
                steps.Enqueue(() => { var r = Sim.Casino.history.Last(); CheckNow(game + " wallet matches gross settlement", runtime.Sim.S.money == before - r.stake + r.returned); Capture(game); });
            }
            steps.Enqueue(() => { Sim.Clock += 2; Sim.Casino.rng = 12345; Click("Game blackjack"); });
            steps.Enqueue(() =>
            {
                before = runtime.Sim.S.money; rounds = Sim.Casino.rounds;
                Click("Place Bet"); Click("Place Bet");
                CheckNow("Blackjack deals once and holds stake", Sim.BlackjackActive && runtime.Sim.S.money == before - 10);
                CheckNow("Dealer hole card is hidden", Page.GetComponentsInChildren<TMP_Text>().Any(t => t.name == "Hidden Dealer Card" && t.text == "888"));
                Capture("blackjack-hand");
                Sim.Clock += 1;
            });
            steps.Enqueue(() => window.CloseWindow());
            steps.Enqueue(() => CasinoDesktop.Instance.OpenSite());
            steps.Enqueue(() => { Sim.Clock += 2; Click("Game blackjack"); });
            steps.Enqueue(() => { }); // restored cards now genuinely travel from the shoe before they can be clicked
            steps.Enqueue(() =>
            {
                CheckNow("Unfinished blackjack hand persists after closing", Sim.BlackjackActive && Sim.Casino.blackjack.player.Count == 2);
                Click("Blackjack hit"); Click("Blackjack hit"); CheckNow("Blackjack hit duplicate is rejected", Sim.Casino.blackjack.player.Count == 3);
                Sim.Clock += 1;
            });
            steps.Enqueue(() =>
            {
                if (Sim.BlackjackActive) { Click("Blackjack stand"); Click("Blackjack stand"); }
                CheckNow("Blackjack settles only once", Sim.Casino.rounds == rounds + 1 && !Sim.BlackjackActive);
                var r = Sim.Casino.history.Last();
                CheckNow("Blackjack wallet settlement", runtime.Sim.S.money == before - r.stake + r.returned);
                Capture("blackjack-result");
            });
            steps.Enqueue(() => { runtime.Sim.S.money = 0; rounds = Sim.Casino.rounds; });
            steps.Enqueue(() => { Click("Place Bet"); CheckNow("Zero wallet cannot wager", Sim.Casino.rounds == rounds); runtime.Sim.S.money = 10000; window.MinimizeWindow(); });
            steps.Enqueue(() => CasinoDesktop.Instance.OpenSite());
            steps.Enqueue(() => { Sim.Clock += 2; Click("Game roulette"); CheckNow("Advert entry restores minimized browser interaction", Page.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Table Title").text == CasinoPage.GameName("roulette")); });
            steps.Enqueue(() => Click("Place Bet"));
            steps.Enqueue(() => { }); steps.Enqueue(() => { });
            steps.Enqueue(() => browser.CreateNewTab("888.vip"));
            steps.Enqueue(() => { Sim.Clock += 2; Click("Game roulette"); });
            steps.Enqueue(() => Click("Place Bet"));
            steps.Enqueue(() => { }); steps.Enqueue(() => { });
            steps.Enqueue(() => browser.SwitchToTab(tabA));
            steps.Enqueue(() =>
            {
                var face = Page.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Value");
                CheckNow("Returning to old tab shows shared latest outcome", face.text == Sim.Casino.history.Last().values[0].ToString());
                Sim.Casino.adRemaining = 0; Sim.AdvanceCasinoAd(1, false);
            });
            steps.Enqueue(() =>
            {
                var ad = PrologueDirector.Desk.layer.Find("Casino Advert") as RectTransform;
                CheckNow("Midgame advert appears", ad != null && ad.gameObject.activeInHierarchy);
                // The target is just outside the tiny X, not a replacement hit area.
                var miss = PrologueDesk.Rect("QA miss", ad, Vector2.one, Vector2.one, new Vector2(-27, -12), new Vector2(-25, -10));
                ChapterOnePlayQa.Click(miss); UnityEngine.Object.Destroy(miss.gameObject);
                CheckNow("Miss near X does not dismiss advert", Sim.Casino.adVisible);
                var tray = PrologueDirector.Desk.layer.GetComponent<TrayPopups>() ?? PrologueDirector.Desk.layer.gameObject.AddComponent<TrayPopups>();
                tray.desk = PrologueDirector.Desk; tray.Enqueue("360安全卫士", "QA：这条正常通知不能被广告盖住。", 30);
            });
            steps.Enqueue(() =>
            {
                var ad = PrologueDirector.Desk.layer.Find("Casino Advert") as RectTransform;
                var tray = PrologueDirector.Desk.layer.Find("Tray Popup Area") as RectTransform;
                CheckNow("Normal tray notice and advert do not overlap", ad.offsetMin.y >= tray.offsetMax.y);
                Capture("advert-and-notice");
            });
            steps.Enqueue(() =>
            {
                var ad = PrologueDirector.Desk.layer.Find("Casino Advert") as RectTransform;
                ChapterOnePlayQa.Click(ad.Find("Close Casino Advert") as RectTransform);
                CheckNow("Exact tiny X dismisses advert", !Sim.Casino.adVisible);
                Sim.Clock += 2;
            });
            steps.Enqueue(() =>
            {
                Sim.AdvanceCasinoAd(1, false); CheckNow("Closed advert does not instantly return", !Sim.Casino.adVisible);
                before = runtime.Sim.S.money; rounds = Sim.Casino.rounds;
                Click("Place Bet"); window.CloseWindow();
            });
            steps.Enqueue(() => CasinoDesktop.Instance.OpenSite());
            steps.Enqueue(() =>
            {
                var r = Sim.Casino.history.Last();
                CheckNow("Closing during reveal does not cancel or duplicate payout", Sim.Casino.rounds == rounds + 1 && runtime.Sim.S.money == before - r.stake + r.returned);
                window.FullscreenWindow();
            });
            steps.Enqueue(() => { Capture("maximized"); VerifyPersistedWallet(); });
            steps.Enqueue(() => { locale = GameText.Bind(() => GameText.English); GameText.PublishChanged(); Click("Game blackjack"); });
            steps.Enqueue(() => { Capture("blackjack-english"); CheckNow("Blackjack is localized", Page.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "BLACKJACK")); });
            steps.Enqueue(() => { locale?.Dispose(); locale = null; GameText.PublishChanged(); });
            next = EditorApplication.timeSinceStartup + .8; EditorApplication.update += Tick;
            Write();
        }

        static void VerifyPersistedWallet()
        {
            runtime.SaveNow(); // saving is disabled, so explicitly exercise the same nested payload shape in memory
            var outer = JsonUtility.FromJson<LingGuangV05.Core.GameState>(JsonUtility.ToJson(runtime.Sim.S));
            outer.labState = JsonUtility.ToJson(Sim.S);
            var restoredOuter = JsonUtility.FromJson<LingGuangV05.Core.GameState>(JsonUtility.ToJson(outer));
            var restored = new XgSim(JsonUtility.FromJson<XgState>(restoredOuter.labState));
            CheckNow("Nested native save roundtrip preserves wallet and casino", restoredOuter.money == runtime.Sim.S.money && restored.Casino.rounds == Sim.Casino.rounds && restored.Casino.rng == Sim.Casino.rng && restored.Casino.history.Count == Sim.Casino.history.Count && restored.S.casinoSaidFirst && restored.S.casinoWatching && restored.S.casinoAiSeq == Sim.S.casinoAiSeq);
        }

        static void Tick()
        {
            if (!Running) return;
            if (!Application.isPlaying || runtime == null || runtime.useDiskSave) { CheckNow("Play stayed isolated", false); Finish(); return; }
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .85;
            if (steps.Count == 0) { Finish(); return; }
            try { steps.Dequeue()(); }
            catch (Exception e) { CheckNow("QA exception", false, e.ToString()); Finish(); }
            Write();
        }
        static void Click(string name)
        {
            var b = Page.GetComponentsInChildren<Button>(true).First(x => x.name == name);
            ChapterOnePlayQa.Click(b.transform as RectTransform);
        }
        static void CheckNow(string name, bool ok, string detail = "")
        {
            Result.checks.Add(new Check { name = name, passed = ok, detail = detail });
            if (ok) Result.passed++; else Result.failed++;
        }
        static void Capture(string name) { Directory.CreateDirectory(Dir); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, name + ".png")); }
        static void Finish()
        {
            Running = false; Result.completed = steps.Count == 0; EditorApplication.update -= Tick;
            if (storyProbe != null) { LingGuangV05.Core.StoryFocus.Probe = storyProbe; storyProbe = null; }
            locale?.Dispose(); locale = null; Write(); ChapterOneQaReloadGuard.Release();
            if (Result.completed && Result.failed == 0 && withFeedback) CasinoJuicePlayQa.Begin();
        }
        static void Write() { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, "play-qa.json"), JsonUtility.ToJson(Result, true)); }
    }
}
#endif
