#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.OS;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real profile and gallery callbacks in an already-isolated, quiet Play fixture with saved memories.</summary>
public static class ProfileMemoriesUiChecks
{
    static void Need(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Profile memories UI: " + message); }

    [MenuItem("LingGuang/Verification/Profile remembered moments (isolated Play)")]
    public static void Run()
    {
        Need(Application.isPlaying && !string.IsNullOrEmpty(ChapterOneRuntime.VerificationSaveDirectory), "requires declared isolated Play fixture");
        var controller = XingGuangController.Instance;
        Need(controller != null && controller.runtime != null && controller.View != null, "real controller and profile view");
        var host = controller.GetComponent<XgEndingPreparationHost>();
        Need(host != null && host.CanOpenProfileMemories, "fixture must have existing memories and no active narrative hold or ending panel; " + GateState(controller, host));
        int count = host.ProfileMemoryCount;
        string before = JsonUtility.ToJson(controller.Sim.S);
        string tab = controller.View.Tab;
        bool windowWasOpen = controller.Window.isOn;
        var galleryField = typeof(XgEndingPreparationHost).GetField("gallery", BindingFlags.Instance | BindingFlags.NonPublic);
        Need(galleryField.GetValue(host) == null, "begin with gallery closed");
        try
        {
            controller.Window.OpenWindow();
            controller.View.Open("home"); controller.View.Refresh(true); Canvas.ForceUpdateCanvases();
            var homeNav = controller.View.NavButton("home").GetComponent<Button>();
            controller.View.ShowTab("cards"); homeNav.onClick.Invoke();
            Need(controller.View.Tab == "home" && controller.View.Home.root.gameObject.activeInHierarchy, "player-named sidebar entry returns from profile subpages to home");
            before = JsonUtility.ToJson(controller.Sim.S);
            var entry = controller.View.GetComponentsInChildren<Button>(true).Single(button => button.name == "Open remembered moments");
            Need(entry.gameObject.activeInHierarchy && entry.interactable, "remembered-moments entry is on the player-named profile");
            Need(entry.GetComponentInChildren<TMP_Text>(true).text == (GameText.IsEnglish ? "It remembers (" + count + ")" : "它记得（" + count + "）"), "entry displays the real localized memory count");
            var sections = (RectTransform)entry.transform.parent;
            Need(sections.GetComponentsInChildren<Button>().Length == 3, "profile header contains only personality, memories and More");
            var oldMin = sections.offsetMin; var oldMax = sections.offsetMax;
            var refresh = typeof(XingGuangView).GetMethod("RefreshProfileSections", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                sections.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 640);
                refresh.Invoke(controller.View, null); Canvas.ForceUpdateCanvases();
                var neighbors = sections.GetComponentsInChildren<Button>().OrderBy(button => Left((RectTransform)button.transform)).ToArray();
                for (int i = 1; i < neighbors.Length; i++)
                    Need(Right((RectTransform)neighbors[i - 1].transform) <= Left((RectTransform)neighbors[i].transform) + .01f, "640px profile header controls do not overlap");
            }
            finally { sections.offsetMin = oldMin; sections.offsetMax = oldMax; refresh.Invoke(controller.View, null); Canvas.ForceUpdateCanvases(); }
            Need(PrologueDirector.Desk.layer.Find("EndingNotesLauncher") == null, "old floating desktop launcher is absent");
            // Real authored question UI, opened only as a presentation fixture; no dispatch/answer receipt is written.
            var seedField = typeof(XgEndingPreparationHost).GetField("seedPanel", BindingFlags.Instance | BindingFlags.NonPublic);
            var seedAsset = Resources.Load<TextAsset>("LingGuangV05/Data/ending_shutdown_prompts");
            var seedCatalog = JsonUtility.FromJson<XgEndingSeedCatalog>(seedAsset.text);
            var prompt = seedCatalog.prompts.First(item => item.stage == controller.Sim.S.stage);
            typeof(XgEndingPreparationHost).GetMethod("ShowSeed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(host, new object[] { prompt });
            var question = (RectTransform)seedField.GetValue(host);
            Need(question != null && question.gameObject.activeInHierarchy, "authored pending question is visible");
            try
            {
                controller.View.Refresh(true);
                Need(!host.CanOpenProfileMemories && !entry.gameObject.activeSelf, "pending question disables the memories entry");
                entry.onClick.Invoke(); // A stale queued button callback must obey the same guard.
                Need(ReferenceEquals(seedField.GetValue(host), question) && question.gameObject.activeInHierarchy && galleryField.GetValue(host) == null, "stale memories callback cannot silently dismiss the pending question");
                Need(JsonUtility.ToJson(controller.Sim.S) == before, "question presentation and guarded entry do not write dispatch or answer state");
            }
            finally
            {
                typeof(XgEndingPreparationHost).GetMethod("CloseSeed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(host, null);
                controller.View.Refresh(true);
            }
            entry.onClick.Invoke();
            var gallery = (RectTransform)galleryField.GetValue(host);
            Need(gallery != null && gallery.gameObject.activeInHierarchy && gallery.name == "ItRemembersGallery", "profile opens the original gallery");
            Need(gallery.Find("MemoryViewport/MemoryCards") != null, "original memory cards and source links remain");
            string back = GameText.IsEnglish ? "Back" : "返回";
            var close = gallery.GetComponentsInChildren<Button>().Single(button => button.GetComponentInChildren<TMP_Text>(true)?.text == back);
            close.onClick.Invoke();
            Need(galleryField.GetValue(host) == null && controller.View.Tab == "home", "gallery Back returns to the profile");
            Need(host.ProfileMemoryCount == count && JsonUtility.ToJson(controller.Sim.S) == before, "read-only gallery entry preserves memory and progress state");
            Debug.Log("PROFILE_MEMORIES_UI_PASS: actual profile and original-gallery button callbacks; existing isolated memories, no fabricated records. Not physical pointer or natural-memory progression acceptance.");
        }
        finally
        {
            if (galleryField.GetValue(host) != null)
                typeof(XgEndingPreparationHost).GetMethod("CloseGallery", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(host, null);
            controller.View.Open(tab);
            if (!windowWasOpen) controller.Window.CloseWindow();
        }
    }

    // Read the real production gates; diagnostics must never override a hold to make the check pass.
    public static string GateState(XingGuangController controller, XgEndingPreparationHost host)
    {
        if (host == null) return "host=null";
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(XgEndingPreparationHost);
        var seed = (RectTransform)type.GetField("seedPanel", flags).GetValue(host);
        var settings = (WindowManager)type.GetField("narrativeSettings", flags).GetValue(host);
        return "enabled=" + host.isActiveAndEnabled + ", current=" + type.GetProperty("Current", flags).GetValue(host)
            + ", narrativeAvailable=" + type.GetProperty("NarrativeAvailable", flags).GetValue(host)
            + ", memories=" + host.ProfileMemoryCount + ", seed=" + (seed != null) + ", endingPanel=" + host.Showing
            + ", filter=" + (type.GetField("guard", flags).GetValue(host) != null)
            + ", text=" + (type.GetField("text", flags).GetValue(host) != null)
            + ", prologue=" + controller.runtime.Sim.InPrologue + ", minimized=" + PlayerWindowState.IsMinimized
            + ", story=" + StoryFocus.Active + ", exclusive=" + StoryFocus.Exclusive
            + ", innerVoice=" + InnerVoice.Busy + ", voicePending=" + InnerVoice.Pending
            + ", stagePresentation=" + controller.Sim.StagePresentationHeld
            + ", paused=" + XgLifeHostActivity.GamePaused + ", reading=" + XgLifeHostActivity.ReadingWindowVisible
            + ", groupScript=" + (YYChatHub.Instance?.GroupScriptPlaying == true) + ", saveSlots=" + SaveSlotsPanel.IsOpen
            + ", narrativeSettings=" + (settings != null && settings.isOn)
            + ", caseSettings=" + LingGuangV05.CaseMenu.CaseMenuHud.AnySettingsOpen
            + ", epilogueHold=" + (controller.Epilogue?.HoldingSimulation == true)
            + ", epilogueShowing=" + (controller.Epilogue?.Showing == true)
            + ", released=" + XgEpilogueFlow.HasCommittedRelease(controller.Sim.S) + ", chapterComplete=" + controller.Sim.S.chapterComplete;
    }

    static float Left(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return corners[0].x; }
    static float Right(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return corners[2].x; }
}
#endif
