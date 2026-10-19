#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Embed in the existing isolated SidebarLayoutChecks runner; callbacks use its
// real LCD pointer/capture path. This does not open Unity, scenes or player saves.
public static class RelicExpansionUiChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException("Relic expansion UI: " + text); }
    static void CheckBackpackNavigationState(string before, string after)
    {
        // XgBackpackPage.Shown -> XgSim.MarkRelicsViewed clears only this UI unread list.
        // Keep every other saved field (including owned/seen items, equipment, progression and lab money) exact.
        var expected = JsonUtility.FromJson<XgState>(before);
        expected.relicFresh.Clear();
        if (JsonUtility.ToJson(expected) == after) return;
        string folder = Path.Combine("LocalVerification", "RelicNavigationState", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "before.json"), before);
        File.WriteAllText(Path.Combine(folder, "expected-after-mark-viewed.json"), JsonUtility.ToJson(expected, true));
        File.WriteAllText(Path.Combine(folder, "actual-after.json"), after);
        Check(false, "backpack navigation changed state beyond the documented relicFresh read marker; exact snapshots: " + Path.GetFullPath(folder));
    }
    static void Stage(XgSim sim, int n) { sim.S.abilitiesEmerged = Enumerable.Range(1, n).ToList(); sim.S.stage = sim.S.stageVision = sim.S.stageSequence = n; }
    static void CompletedStageEntryTeachingFixture(XgSim sim)
    {
        // This navigation fixture starts after the stage-entry teaching was completed.
        // Declare only those persisted answers; do not hide a product modal or award its training bonus.
        sim.S.trainingLessonObservedStage = sim.S.stage;
        foreach (var quiz in XgTrainingLessons.Quizzes)
            if (quiz.stage <= sim.S.stage && quiz.trigger == "stage_enter" && !sim.S.quizAnswered.Contains(quiz.id))
                sim.S.quizAnswered.Add(quiz.id);
        sim.S.quizQueued.RemoveAll(id => sim.S.quizAnswered.Contains(id));
    }
    static void Bind(XingGuangController ctl, XgSim sim, bool english)
    {
        sim.English = english;
        typeof(XingGuangController).GetProperty("Sim", Flags).GetSetMethod(true).Invoke(ctl, new object[] { sim });
        ctl.View.Bind(ctl); ctl.View.Open("backpack"); ctl.View.Refresh(true);
    }
    static RectTransform Slot(XingGuangView view, int oneBased)
        => (RectTransform)Field<RectTransform>(view.Backpack, "content").Find("Slot" + oneBased);
    static void ScrollTo(XingGuangView view, RectTransform slot)
    {
        var scroll = view.Backpack.root.Find("List").GetComponent<ScrollRect>();
        float overflow = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
        float top = -slot.offsetMax.y;
        scroll.verticalNormalizedPosition = overflow <= 0 ? 1 : 1 - Mathf.Clamp01((top - 20) / overflow);
        Canvas.ForceUpdateCanvases();
    }
    static void Detail(XingGuangView view, string node)
    {
        Check(view.Tab == "tree", "locked slot opens actual technology page");
        var shown = Field<object>(view.Tree, "detailShown");
        Check(shown != null && (bool)shown.GetType().GetMethod("Covers", Flags).Invoke(shown, new object[] { node }), "correct technology detail target " + node);
        Check(Field<RectTransform>(view.Tree, "tooltip").gameObject.activeInHierarchy, "technology details visible");
    }
    public static IEnumerator Frames(XingGuangController ctl, Action<bool> setEnglish,
        Action<RectTransform> click, Action<string> capture, bool profileOnly = false)
    {
        var runtime = ctl.runtime;
        Check(runtime != null && !runtime.useDiskSave && !runtime.tickEnabled && !runtime.TestMode, "isolated real-economy fixture required");
        Check(setEnglish != null && click != null && capture != null, "live locale/input/capture callbacks required");
        var original = ctl.Sim; string originalHouse = JsonUtility.ToJson(runtime.Sim.S);
        var brainWindow = ctl.GetComponent<XgBrainWindow>();
        bool brainEnabled = brainWindow != null && brainWindow.enabled;
        bool enabled = ctl.enabled; string oldTab = ctl.View.Tab;
        var window = ctl.Window.windowContainer; var oldSize = window.sizeDelta;
        ctl.enabled = false;
        try
        {
            for (int fixture = 0; fixture < (profileOnly ? 0 : 4); fixture++)
            {
                bool en = fixture >= 2, large = fixture % 2 == 1; setEnglish(en);
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, large ? 1400 : 960);
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, large ? 900 : 600);
                var sim = new XgSim(); sim.S.unlocked.Add("train.curve"); sim.S.spiderIntroDone = true;
                sim.S.teachIntroSaid = sim.S.teachThirdSaid = sim.S.teachTreeSaid = sim.S.teachTrainSaid = true;
                sim.S.teachingFlow = true; sim.S.chatTurns = 1;
                Stage(sim, 2); CompletedStageEntryTeachingFixture(sim); Bind(ctl, sim, en);
                ctl.View.Open("train"); ctl.View.Refresh(true); Canvas.ForceUpdateCanvases();
                var access = Field<XgBtn>(ctl.View.Train, "relicAccess");
                Check(access.rt.gameObject.activeInHierarchy && !access.button.interactable, "empty inventory shows a disabled training entry");
                Check(Field<TMP_Text>(ctl.View.Train, "relicAccessNote").text.Contains(en ? "first rare item" : "首件稀有道具"), "entry states the existing inventory gate");
                access.button.onClick.Invoke();
                Check(ctl.View.Tab == "train", "stale disabled callback preserves current page");
                sim.GrantRelic("redbull"); ctl.View.Refresh(true);
                ctl.View.Open("train"); ctl.View.Refresh(true);
                Check(!Field<XgBtn>(ctl.View.Train, "summary").rt.gameObject.activeSelf, "fixture hides the old model summary entry");
                Check(access.rt.gameObject.activeInHierarchy && access.button.interactable && access.button is XgButtonPressFeedback, "independent rare-items entry retains unified press feedback");
                Check(XgRelics.All.Length == 24 && XgRelics.Get("angel_term") != null, "original 24-item catalog includes angel term sheet");
                Check(Field<TMP_Text>(ctl.View.Train, "relicAccessNote").text.Contains("1/24"), "training entry shows actual owned count");
                for (int n = 0; n < 3; n++) yield return null;
                Check(sim.PendingTrainingQuiz() == null && !sim.TrainingKnowledgePending && !sim.TrainingLessonModalOpen,
                    "declared completed stage-entry teaching fixture has no active lesson modal before the real pointer click");
                capture("train-relic-entry-" + (en ? "en" : "zh") + "-" + (large ? "large" : "small") + ".png");
                Check(sim.RelicsFresh, "newly granted fixture item is unread until the real training-entry visit");
                // Isolate the synchronous navigation transaction from unrelated host updates during awaited frames.
                string beforeEntry = JsonUtility.ToJson(sim.S);
                double entryMoney = runtime.Sim.S.money;
                click(access.rt);
                Check(ctl.View.Backpack.root.gameObject.activeInHierarchy, "actual training entry pointer opens original backpack");
                Check(!sim.RelicsFresh, "actual backpack visit consumes the existing unread badge");
                CheckBackpackNavigationState(beforeEntry, JsonUtility.ToJson(sim.S));
                Check(runtime.Sim.S.money == entryMoney, "entry does not spend household money");
                yield return null;
                sim.S.teachingFlow = false; ctl.View.Refresh(true);
                runtime.Sim.S.money = 200000;
                string tag = "relic-" + (en ? "en" : "zh") + "-" + (large ? "large" : "small");
                for (int n = 0; n < 4; n++) yield return null;
                Check(Field<TMP_Text>(ctl.View.Backpack, "header").text.Contains("/4"), "initial header capacity");
                var strip = Field<RectTransform>(ctl.View, "relicStrip");
                click((RectTransform)strip.Find("NextSlots")); yield return null;
                Check(Field<int>(ctl.View, "relicStripPage") == 1, "top strip advances to slots five through eight");
                double stripMoney = runtime.Sim.S.money;
                click((RectTransform)strip.Find("Slot1"));
                for (int n = 0; n < 3; n++) yield return null;
                Detail(ctl.View, XgSim.RelicExpansion8Node);
                Check(runtime.Sim.S.money == stripMoney, "top strip locked navigation does not spend");
                click((RectTransform)strip.Find("NextSlots")); yield return null;
                Check(Field<int>(ctl.View, "relicStripPage") == 2, "top strip advances to slots nine through twelve");
                click((RectTransform)strip.Find("Slot4"));
                for (int n = 0; n < 3; n++) yield return null;
                Detail(ctl.View, XgSim.RelicExpansion12Node);
                click((RectTransform)strip.Find("NextSlots")); yield return null;
                Check(Field<int>(ctl.View, "relicStripPage") == 0, "top strip wraps to first page");
                click((RectTransform)strip.Find("PreviousSlots")); yield return null;
                Check(Field<int>(ctl.View, "relicStripPage") == 2, "top strip wraps backwards");
                ctl.View.Open("backpack"); ctl.View.Refresh(true);
                for (int n = 0; n < 2; n++) yield return null;
                var fifth = Slot(ctl.View, 5); ScrollTo(ctl.View, fifth);
                for (int n = 0; n < 2; n++) yield return null;
                Check(fifth.Find("Empty").GetComponent<TMP_Text>().text.Contains(en ? "Locked" : "封印"), "localized locked slot");
                capture(tag + "-locked.png"); double before = runtime.Sim.S.money;
                click(fifth);
                for (int n = 0; n < 3; n++) yield return null;
                Detail(ctl.View, XgSim.RelicExpansion8Node);
                Check(before == runtime.Sim.S.money && !sim.Has(XgSim.RelicExpansion8Node), "locked-slot navigation cannot spend");
                capture(tag + "-stage3-detail.png");
                Stage(sim, 3);
                Check(sim.BuyNode(XgSim.RelicExpansion8Node, ctl.Host), "first official purchase with real host");
                Check(Math.Abs(before - runtime.Sim.S.money - 12000) < 1e-8, "first host debit");
                ctl.View.Open("backpack"); ctl.View.Refresh(true);
                for (int n = 0; n < 3; n++) yield return null;
                Check(Field<TMP_Text>(ctl.View.Backpack, "header").text.Contains("/8"), "eight-slot UI refresh");
                var ninth = Slot(ctl.View, 9); ScrollTo(ctl.View, ninth);
                for (int n = 0; n < 2; n++) yield return null;
                click(ninth);
                for (int n = 0; n < 3; n++) yield return null;
                Detail(ctl.View, XgSim.RelicExpansion12Node); capture(tag + "-stage4-detail.png");
                Stage(sim, 4); before = runtime.Sim.S.money;
                Check(sim.BuyNode(XgSim.RelicExpansion12Node, ctl.Host), "second official purchase with real host");
                Check(Math.Abs(before - runtime.Sim.S.money - 48000) < 1e-8, "second host debit");
                sim.EquipRelic("redbull", 11);
                ctl.View.Open("backpack"); ctl.View.Refresh(true);
                for (int n = 0; n < 3; n++) yield return null;
                var twelfth = Slot(ctl.View, 12); ScrollTo(ctl.View, twelfth);
                for (int n = 0; n < 2; n++) yield return null;
                Check(Field<TMP_Text>(ctl.View.Backpack, "header").text.Contains("/12"), "twelve-slot UI refresh");
                Check(twelfth.Find("Name").GetComponent<TMP_Text>().text == XgRelics.Name(XgRelics.Get("redbull"), en), "twelfth equipped item rendered");
                capture(tag + "-twelve-slots.png"); click(twelfth);
                for (int n = 0; n < 2; n++) yield return null;
                Check(!sim.RelicEquipped("redbull") && sim.RelicOwned("redbull"), "actual twelfth-slot click unequips without losing item");
                // Fill all twelve through real grants, then choose the final replacement button.
                foreach (var r in XgRelics.All) sim.GrantRelic(r.id);
                if (sim.RelicFreeSlot >= 0) sim.EquipRelic("redbull");
                Check(sim.RelicsEquippedCount == 12 && sim.RelicFreeSlot == -1, "twelve filled for chooser");
                string incoming = sim.RelicsOwned.First(id => !sim.RelicEquipped(id));
                string outgoing = sim.S.relicSlots[11];
                ctl.View.Refresh(true);
                for (int n = 0; n < 2; n++) yield return null;
                var tile = (RectTransform)Field<RectTransform>(ctl.View.Backpack, "content").Find("Relic " + incoming);
                ScrollTo(ctl.View, tile); yield return null;
                click(tile);
                for (int n = 0; n < 2; n++) yield return null;
                var chooser = Field<RectTransform>(ctl.View.Backpack, "chooser");
                Check(chooser != null && chooser.gameObject.activeInHierarchy, "full-capacity click opens replacement chooser");
                Check(Field<XgBtn>(ctl.View.Backpack, "chooserCancel").label.text == (en ? "Cancel" : "算了"), "existing chooser refreshes cancel language");
                var buttons = Field<Array>(ctl.View.Backpack, "chooserButtons");
                for (int i = 0; i < 12; i++) Check(Field<RectTransform>(buttons.GetValue(i), "rt").gameObject.activeInHierarchy, "chooser offers slot " + (i + 1));
                capture(tag + "-full-chooser.png");
                click(Field<RectTransform>(buttons.GetValue(11), "rt"));
                for (int n = 0; n < 2; n++) yield return null;
                Check(sim.S.relicSlots[11] == incoming && sim.RelicOwned(outgoing) && !sim.RelicEquipped(outgoing), "twelfth replacement keeps displaced item owned");
                Check(sim.RelicsOwned.Count == XgRelics.All.Length && !chooser.gameObject.activeSelf, "replacement closes chooser without inventory loss");
                click((RectTransform)strip.Find("Slot4")); yield return null;
                Check(ctl.View.Tab == "home" && ctl.View.Backpack.root.gameObject.activeInHierarchy, "unlocked third-page top strip opens backpack inside profile");

                // A chooser left open during a save switch must not affect the newly bound lab.
                ctl.View.Refresh(true);
                tile = (RectTransform)Field<RectTransform>(ctl.View.Backpack, "content").Find("Relic " + outgoing);
                ScrollTo(ctl.View, tile); yield return null; click(tile); yield return null;
                Check(chooser.gameObject.activeSelf, "reopen chooser for save-switch fixture");
                var replacement = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
                string expectedSlots = string.Join(",", replacement.S.relicSlots);
                Bind(ctl, replacement, en); yield return null;
                Check(!chooser.gameObject.activeSelf && Field<int>(ctl.View, "relicStripPage") == 0, "save switch dismisses chooser and resets strip");
                Field<RectTransform>(buttons.GetValue(11), "rt").GetComponent<Button>().onClick.Invoke();
                Check(string.Join(",", replacement.S.relicSlots) == expectedSlots, "stale chooser callback cannot alter new save");
            }
            Bind(ctl, original, original.English);
            // The fully-unlocked fixture auto-opens an always-on-top brain beside
            // the app. It is an unrelated window, not a profile input target.
            if (brainWindow != null) { brainWindow.enabled = false; brainWindow.Hide(); }
            for (int lang = 0; lang < 2; lang++)
            {
                bool en = lang == 1; setEnglish(en); original.English = en;
                original.S.chatTurns = Math.Max(1, original.S.chatTurns);
                original.GrantRelic("redbull");
                if (!original.S.relicFresh.Contains("redbull")) original.S.relicFresh.Add("redbull");
                ctl.View.Open("home"); ctl.View.Refresh(true);
                float readyAt = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < readyAt) yield return null;
                var more = Field<XgBtn>(ctl.View, "profileMore");
                var overlay = Field<RectTransform>(ctl.View, "profileMoreOverlay");
                Check(more.label.text.Contains("●"), "unread backpack badge retained on More");
                capture("profile-before-more-" + (en ? "en" : "zh") + ".png");
                click(more.rt); yield return null;
                Check(overlay.gameObject.activeInHierarchy && original.RelicsFresh, "More opens without consuming unread items");
                Check(Field<XgBtn>(ctl.View, "profileFinal").rt.gameObject.activeSelf == ctl.View.FeatureOpen("final"), "finale original gate retained");
                capture("profile-more-" + (en ? "en" : "zh") + ".png");
                click(Field<XgBtn>(ctl.View, "profileCards").rt); yield return null;
                Check(ctl.View.Cards.root.gameObject.activeInHierarchy && !overlay.gameObject.activeSelf, "cards route retained and closes menu");
                click(ctl.View.NavButton("home")); yield return null;
                Check(ctl.View.Home.root.gameObject.activeInHierarchy, "return from cards to profile");
                click(more.rt); yield return null; click(Field<XgBtn>(ctl.View, "profileBackpack").rt); yield return null;
                Check(ctl.View.Backpack.root.gameObject.activeInHierarchy && !original.RelicsFresh, "actual backpack visit consumes badge");
                click(more.rt); yield return null; click(overlay); yield return null;
                Check(!overlay.gameObject.activeSelf, "outside click dismisses More");
                click(more.rt); yield return null; ctl.View.Open("train"); yield return null;
                Check(!overlay.gameObject.activeSelf, "page switch closes menu");
            }
            Debug.Log("RelicExpansionUiChecks PASS: 4 locale/size fixtures, real host purchases, top-strip three-page routing, slot twelve, full replacement chooser and save-switch guard; screenshots require human review.");
        }
        finally
        {
            JsonUtility.FromJsonOverwrite(originalHouse, runtime.Sim.S); window.sizeDelta = oldSize;
            setEnglish(original.English); Bind(ctl, original, original.English); ctl.View.Open(oldTab); ctl.enabled = enabled;
            if (brainWindow != null) brainWindow.enabled = brainEnabled;
        }
    }
}
#endif
