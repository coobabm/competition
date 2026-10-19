#if UNITY_EDITOR
using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class BrainWindowPlayQa
{
    static IDisposable localeBinding;
    static string locale;
    static XgPersonaArchiveWindow archive;
    static XgSim priorPersonaSim;
    static string priorReasons;
    static double personaWallet;

    // File isolation is mandatory before scene load. It does NOT isolate PlayerPrefs.
    static string RequireIsolation()
    {
        string root = VerificationSavePaths.Root;
        if (string.IsNullOrEmpty(root))
            throw new InvalidOperationException("Requires a fresh editor launched with --verification-save-dir and an absolute owned directory outside the project; protect Editor preferences before execution.");
        return Path.GetFullPath(root);
    }

    static void SetLocale(string id)
    {
        if (localeBinding == null) localeBinding = GameText.Bind(() => locale);
        locale = id;
        GameText.PublishChanged();
        ctl.View.Refresh(true);
    }
    static void ReleaseLocale()
    {
        if (localeBinding == null) return;
        localeBinding.Dispose(); localeBinding = null;
        GameText.PublishChanged();
    }
    static Button PersonaEntry()
    {
        foreach (var button in ctl.View.GetComponentsInChildren<Button>(true))
            if (button.gameObject.name == "Open personality archive") return button;
        throw new Exception("Profile personality archive UI entry missing");
    }
    static void ClickPersonaEntry()
    {
        var button = PersonaEntry();
        Check(button.isActiveAndEnabled && button.IsInteractable(), "Profile archive entry is active and interactable");
        button.onClick.Invoke();
    }
    static XgTechNodeView PersonaNode()
    {
        foreach (var node in ctl.View.GetComponentsInChildren<XgTechNodeView>(true))
            if (node.Covers(XgSim.PersonaArchiveNode) && node.isActiveAndEnabled) return node;
        throw new Exception("Visible personality archive research node missing");
    }
    static void ClickPersonaNode()
    {
        Check(EventSystem.current != null, "EventSystem exists for research UI callback");
        var node = PersonaNode();
        var price = node.GetComponentInChildren<XgTechPriceAction>();
        Check(price != null && price.isActiveAndEnabled && price.node == node, "Archive research price action is active and bound to its node");
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        price.OnPointerDown(pointer);
        price.OnPointerClick(pointer);
    }
    static string ArchiveText()
    {
        return archive.transform.Find("Personality archive frame/History scroll/History content/History text").GetComponent<TMP_Text>().text;
    }
    [Serializable] sealed class PersonaChartRenderReport
    {
        public string language, shader;
        public bool rendererExists, culled, canvasEnabled;
        public int vertices, indices, materials, nontransparentVertices;
        public float alpha, inheritedAlpha;
        public Vector3 meshBounds;
    }
    static void CheckChartRendering(XgPersonaArchiveGraphic chart)
    {
        Canvas.ForceUpdateCanvases();
        var renderer = chart.canvasRenderer;
        var report = new PersonaChartRenderReport { language = GameText.LanguageId, rendererExists = renderer != null };
        if (renderer != null)
        {
            // Unity 6 returns the renderer's mesh. Inspect it without modifying or destroying it.
            var mesh = renderer.GetMesh();
            report.culled = renderer.cull;
            report.materials = renderer.materialCount;
            report.alpha = renderer.GetAlpha();
            report.inheritedAlpha = renderer.GetInheritedAlpha();
            if (mesh != null)
            {
                report.vertices = mesh.vertexCount;
                for (int i = 0; i < mesh.subMeshCount; i++) report.indices += (int)mesh.GetIndexCount(i);
                foreach (var vertexColor in mesh.colors32) if (vertexColor.a > 0) report.nontransparentVertices++;
                report.meshBounds = mesh.bounds.size;
            }
            var material = report.materials > 0 ? renderer.GetMaterial(0) : null;
            report.shader = material != null && material.shader != null ? material.shader.name : "";
            report.canvasEnabled = chart.canvas != null && chart.canvas.isActiveAndEnabled;
        }
        File.WriteAllText(Path.Combine(output, GameText.IsEnglish ? "persona-chart-en.json" : "persona-chart-zh.json"), JsonUtility.ToJson(report, true));
        Check(report.rendererExists, "Personality chart has CanvasRenderer");
        Check(report.canvasEnabled && !report.culled, "Personality chart canvas enabled and renderer not culled");
        Check(report.materials > 0 && !string.IsNullOrEmpty(report.shader), "Personality chart has submitted material and shader");
        Check(report.alpha > 0 && report.inheritedAlpha > 0, "Personality chart renderer and inherited alpha are nonzero");
        // Base chart alone has 14 line quads: 56 vertices and 84 indices.
        Check(report.vertices >= 56 && report.indices >= 84 && report.nontransparentVertices >= 56, "Personality chart submits visible triangle mesh");
        Check(report.meshBounds.x > 0 && report.meshBounds.y > 0, "Personality chart submitted mesh has positive bounds");
    }
    static void CheckArchive(string heading, string historyHeading)
    {
        archive = ctl.View.GetComponentInChildren<XgPersonaArchiveWindow>(true);
        Check(archive != null && archive.gameObject.activeInHierarchy, "Personality archive visible");
        var chart = archive.GetComponentInChildren<XgPersonaArchiveGraphic>();
        Check(chart != null && chart.isActiveAndEnabled && chart.rectTransform.rect.width > 0 && chart.rectTransform.rect.height > 0, "Personality chart exists with positive bounds");
        CheckChartRendering(chart);
        var title = archive.transform.Find("Personality archive frame/Title").GetComponent<TMP_Text>();
        Check(title.text.Contains(heading), "Archive localized heading: " + heading);
        string history = ArchiveText();
        Check(history.Contains(historyHeading), "Archive localized history: " + historyHeading);
        Check(ctl.Sim.PersonaArchiveDays.Count > 0 && ctl.Sim.PersonaArchiveReasons.Count > 0, "Actual rating changes provide chart days and history reasons");
        Check(history.Contains(ctl.Sim.PersonaReasonText(ctl.Sim.PersonaArchiveReasons[0])), "History includes actual recorded rating reason");
    }
    static void EnqueuePersonaArchive()
    {
        steps.Enqueue(() => {
            // Bind a stage-2 fixture rather than trigger a stage-promotion presentation.
            var lab = new XgSim(); lab.S.stage = 2;
            var house = new ChapterOneSim();
            house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 100000;
            house.S.aiName = "QA archive";
            house.S.gpuModels.Clear(); house.S.gpuModels.Add("gtx980ti"); house.S.gpuCount = 1;
            house.S.labState = JsonUtility.ToJson(lab.S);
            ctl.runtime.SetSimulationForTests(house);
            ctl.GetType().GetMethod("BindSave", Flags).Invoke(ctl, new object[] { house });
            EnableFixtureEconomy(ctl.runtime);
            SetLocale(GameText.Chinese);
            ctl.View.ShowTab("home"); ctl.View.Refresh(true);
            Check(!ctl.Sim.PersonaArchiveUnlocked && !ctl.Sim.PersonaArchiveEligible, "Archive initially locked before ratings");
            Check(!ctl.View.SetupShowing && ctl.View.Tab == "home", "Profile page ready for locked entry");
            ClickPersonaEntry();
        });
        steps.Enqueue(() => {
            Check(ctl.View.Tab == "tree", "Locked profile entry navigates to research");
            var shown = typeof(XgTechTreePage).GetField("detailShown", Flags).GetValue(ctl.View.Tree) as XgTechNodeView;
            Check(shown != null && shown.Covers(XgSim.PersonaArchiveNode), "Locked entry focuses archive research details");
            double before = ctl.Host.Money;
            ClickPersonaNode();
            Check(!ctl.Sim.PersonaArchiveUnlocked && ctl.Host.Money == before, "Ineligible research click does not charge");
            Check(ctl.Sim.Today > 0, "Game date synchronized before actual ratings");
            for (int i = 0; i < 15; i++) {
                ctl.Sim.AddLine("ai", i % 2 == 0 ? "好呀！" : "收到");
                Check(ctl.Sim.RateReply(ctl.Sim.S.chat.Count - 1, true), "Valid fixture rating " + (i + 1));
            }
            Check(ctl.Sim.PersonaRatingCount == 15 && ctl.Sim.PersonaArchiveEligible, "Stage 2 plus 15 valid ratings qualifies");
            ctl.View.Refresh(true);
            personaWallet = ctl.Host.Money;
            ClickPersonaNode();
            Check(ctl.Sim.PersonaArchiveUnlocked, "Research price UI callback purchases archive");
            Check(Math.Abs(personaWallet - ctl.Host.Money - 300) < .001, "Actual production wallet charged exactly 300");
            double paidWallet = ctl.Host.Money;
            ClickPersonaNode();
            Check(ctl.Host.Money == paidWallet, "Owned research node cannot charge twice");
            ctl.View.ShowTab("home"); ctl.View.Refresh(true); ClickPersonaEntry();
        });
        steps.Enqueue(() => {
            CheckArchive("性格档案", "开局写下");
            Check(QaDesktopCapture.Save(Path.Combine(output, "persona-archive-zh.png")), "Rendered desktop captured: persona-archive-zh.png");
        });
        steps.Enqueue(() => SetLocale(GameText.English));
        steps.Enqueue(() => {
            CheckArchive("Personality archive", "Opening wish");
            Check(QaDesktopCapture.Save(Path.Combine(output, "persona-archive-en.png")), "Rendered desktop captured: persona-archive-en.png");
        });
        steps.Enqueue(() => {
            var buttons = archive.GetComponentsInChildren<Button>();
            Check(buttons.Length == 1 && buttons[0].IsInteractable(), "Archive close UI button available");
            buttons[0].onClick.Invoke();
            Check(!archive.gameObject.activeSelf, "Archive close button hides overlay");
            ClickPersonaEntry();
            ctl.Window.MinimizeWindow(); // Real native window lifecycle, not physical pointer input.
            Check(!archive.gameObject.activeSelf, "Native window minimize closes archive");
        });
        steps.Enqueue(() => ctl.Open());
        steps.Enqueue(() => {
            ctl.View.ShowTab("home"); ClickPersonaEntry();
            Check(archive.gameObject.activeSelf, "Profile entry reopens archive after native minimize");
            ctl.Window.CloseWindow();
            Check(!archive.gameObject.activeSelf, "Native window close closes archive");
        });
        steps.Enqueue(() => ctl.Open());
        steps.Enqueue(() => {
            ctl.View.ShowTab("home"); ClickPersonaEntry();
            Check(archive.gameObject.activeSelf, "Archive open before Sim replacement");
            priorPersonaSim = ctl.Sim;
            priorReasons = JsonUtility.ToJson(priorPersonaSim.S);
            var nextLab = new XgSim(); nextLab.S.stage = 2;
            var nextWorld = new ChapterOneSim();
            nextWorld.S.prologue = 2; nextWorld.S.appInstallState = 2; nextWorld.S.money = 100000;
            nextWorld.S.aiName = "QA replacement";
            nextWorld.S.gpuModels.Clear(); nextWorld.S.gpuModels.Add("gtx980ti"); nextWorld.S.gpuCount = 1;
            nextWorld.S.labState = JsonUtility.ToJson(nextLab.S);
            ctl.runtime.SetSimulationForTests(nextWorld);
            ctl.GetType().GetMethod("BindSave", Flags).Invoke(ctl, new object[] { nextWorld });
            EnableFixtureEconomy(ctl.runtime);
        });
        steps.Enqueue(() => {
            Check(archive == null || !archive.gameObject.activeSelf, "Sim replacement closes prior archive");
            Check(!ReferenceEquals(priorPersonaSim, ctl.Sim), "Controller owns replacement Sim");
            Check(!ctl.Sim.PersonaArchiveUnlocked && ctl.Sim.PersonaRatingCount == 0 && ctl.Sim.PersonaArchiveDays.Count == 0 && ctl.Sim.PersonaArchiveReasons.Count == 0, "Replacement Sim inherits no ownership, ratings, chart days or reasons");
            Check(JsonUtility.ToJson(priorPersonaSim.S) == priorReasons, "Replacement does not mutate prior Sim state");
            ctl.Open(); ctl.View.ShowTab("home"); ctl.View.Refresh(true); ClickPersonaEntry();
            Check(ctl.View.Tab == "tree" && (archive == null || !archive.gameObject.activeSelf), "Replacement profile entry routes to locked research without old overlay");
            foreach (string file in new[] { "purchased.png", "answer-feedback.png", "persona-archive-zh.png", "persona-archive-en.png" })
                Check(File.Exists(Path.Combine(output, file)) && new FileInfo(Path.Combine(output, file)).Length > 0, "Screenshot written: " + file);
        });
    }
}
#endif
