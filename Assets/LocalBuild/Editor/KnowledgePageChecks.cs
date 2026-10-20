#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Real page and pointer raycasts in a disposable scene, without loading player saves.
public static class KnowledgePageChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    [Serializable] sealed class Report { public string status = "running", failure = ""; public List<string> passed = new List<string>(); }
    static void Check(Report report, bool value, string label)
    { if (!value) { report.failure = label; throw new InvalidOperationException("Knowledge page: " + label); } report.passed.Add(label); }
    sealed class Wallet : IXgHost
    {
        public double money = 1e8;
        public double Money => money;
        public double Compute => 4;
        public double VramMB => 65536;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount > money) return false; money -= amount; return true; }
        public void Earn(double amount) { money += amount; }
        public void Train(double seconds) { }
    }
    sealed class PageHost : IXgPageHost
    {
        public XgSim sim = new XgSim(); public Wallet wallet = new Wallet();
        public XgJuice juice; public XgTechTreePage tree;
        public XingGuangController Controller => null;
        public XgSim Sim => sim;
        public IXgHost Host => wallet;
        public XgJuice Juice => juice;
        public int BuyAmount { get; set; } = 1;
        public bool Visible => true;
        public string Tab => "tree";
        public RectTransform ComboChip => null;
        public void Refresh(bool force) { tree.Refresh(); }
        public void ShowToast(string text, float seconds) { }
        public void ShowTab(string id) { }
        public bool BuyNode(XgNode node, RectTransform from) => tree.Buy(node, from);
    }
    static PointerEventData Pointer(RectTransform rect)
    {
        return new PointerEventData(EventSystem.current) {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left, pointerId = -1
        };
    }
    static GameObject Hit(RectTransform rect)
    {
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(rect), hits);
        return hits.Count == 0 ? null : hits[0].gameObject;
    }
    static void Click(Report report, Button button, string label)
    {
        var rect = (RectTransform)button.transform;
        var hit = Hit(rect);
        Check(report, hit != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == button.gameObject, label + " resolves through actual raycast");
        ExecuteEvents.Execute(button.gameObject, Pointer(rect), ExecuteEvents.pointerClickHandler);
    }
    static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    public static IEnumerator Frames()
    {
        var report = new Report();
        var root = new GameObject("Knowledge fixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var events = new GameObject("Knowledge events", typeof(EventSystem));
        var host = new PageHost(); var tree = host.tree = new XgTechTreePage();
        bool hadZoom = PlayerPrefs.HasKey("LingGuang.TechZoom"); float zoom = PlayerPrefs.GetFloat("LingGuang.TechZoom", 1);
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(1400, 900);
            host.juice = root.AddComponent<XgJuice>();
            host.juice.DisableMotionWhenReduced = true; host.juice.Reduced = true;
            host.juice.LocalHitStop = true; host.juice.Muted = true; host.juice.Visible = () => true;
            var font = XgUiV4Fonts.Sans;
            Check(report, font != null, "Use shipped CJK font"); host.juice.Init(rt, rt, font);
            tree.Init(host, new XgUi(font, null, true)); tree.Build(rt); tree.Refresh();
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            var tab = Field<XgBtn>(tree, "knowledgeTab");
            foreach (var button in Field<List<XgBtn>>(tree, "toolbar"))
                Check(report, !Bounds(tab.rt).Overlaps(Bounds(button.rt)), "Knowledge tab does not overlap toolbar " + button.label.text);
            var legend = tree.root.Find("Legend") as RectTransform;
            Check(report, legend != null && !Bounds(tab.rt).Overlaps(Bounds(legend)), "Knowledge tab does not overlap legend at fixture viewport");
            double money = host.wallet.money;
            Click(report, tab.button, "Knowledge tab");
            yield return null; yield return null; Canvas.ForceUpdateCanvases();
            var overlay = Field<RectTransform>(tree, "knowledgeRoot");
            Check(report, overlay.gameObject.activeInHierarchy && !tree.Pan.viewport.gameObject.activeSelf, "Knowledge opens and suspends underlying tree canvas");
            var categories = Field<List<XgBtn>>(tree, "knowledgeCategories");
            Click(report, categories[4].button, "Hardware category");
            yield return null; Canvas.ForceUpdateCanvases();
            var rows = Field<List<XgBtn>>(tree, "knowledgeRows"); int visible = 0;
            for (int i = 0; i < rows.Count; i++) if (rows[i].rt.gameObject.activeSelf)
            { visible++; Check(report, XgKnowledge.Entries[i].Category == 3, "Hardware filter excludes other categories: " + i); }
            Check(report, visible == 24, "Hardware shows exactly 24 terms");
            var search = Field<TMP_InputField>(tree, "knowledgeSearch");
            search.text = "no_matching_term_123";
            Check(report, rows.TrueForAll(b => !b.rt.gameObject.activeSelf), "Search hides all unmatched rows");
            Check(report, !string.IsNullOrEmpty(Field<TMP_Text>(tree, "knowledgeCount").text), "Empty result displays explanatory copy");
            search.text = "  COMPUTE  "; visible = 0;
            for (int i = 0; i < rows.Count; i++) if (rows[i].rt.gameObject.activeSelf)
            { visible++; Check(report, XgKnowledge.Entries[i].Matches("COMPUTE"), "English search matches visible term " + i); }
            Check(report, visible > 0, "Case-insensitive English search works in hardware category");
            search.text = "算力";
            Check(report, rows[Array.FindIndex(XgKnowledge.Entries, e => e.Key == "算力")].rt.gameObject.activeSelf, "Chinese search includes Compute");
            search.text = "";
            tree.OpenKnowledge("算力"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            var detail = Field<RectTransform>(tree, "knowledgeDetail");
            var related = detail.GetComponentsInChildren<Button>();
            Check(report, related.Length == 3, "Compute exposes its three related links");
            Click(report, related[0], "Related term");
            Check(report, Field<XgKnowledge.Entry>(tree, "knowledgeSelected").Key == "浮点运算", "Related link navigates to floating-point operations");
            tree.OpenKnowledge("激活函数"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            int activationIndex = Array.FindIndex(XgKnowledge.Entries, e => e.Key == "激活函数");
            Check(report, rows[activationIndex].label.text.Contains("新"), "V4 term has a new badge");
            Check(report, Field<XgKnowledge.Entry>(tree, "knowledgeSelected").Key == "激活函数", "V4 term opens from public entry point");
            bool usageVisible = false;
            foreach (var label in detail.GetComponentsInChildren<TMP_Text>())
                if (label.text == XgKnowledge.Find("激活函数").ChineseUsage) usageVisible = true;
            Check(report, usageVisible, "V4 gameplay context is visible separately from explanation");
            related = detail.GetComponentsInChildren<Button>();
            Check(report, related.Length == 4, "V4 activation exposes four related terms");
            // Longer entries place the last related link below the viewport; reach it
            // through the page's real scroll view before verifying pointer hit testing.
            detail.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
            yield return null; Canvas.ForceUpdateCanvases();
            Click(report, related[3], "V4 related Softmax term");
            Check(report, Field<XgKnowledge.Entry>(tree, "knowledgeSelected").Key == "归一化指数", "V4 to V4 related link navigates correctly");
            for (int i = 0; i < 4; i++) tree.Refresh(); tree.Shown();
            yield return null; Canvas.ForceUpdateCanvases();
            Check(report, overlay.GetSiblingIndex() == overlay.parent.childCount - 1, "Refresh and Shown preserve overlay stacking");
            Check(report, !Field<RectTransform>(tree, "tooltip").gameObject.activeSelf && !Field<RectTransform>(tree, "eraCard").gameObject.activeSelf, "Tooltip and era card stay hidden while reading");
            var hit = Hit(detail);
            Check(report, hit != null && hit.transform.IsChildOf(overlay), "Detail background raycast cannot reach tree nodes");
            var back = Field<XgBtn>(tree, "knowledgeBack");
            Click(report, back.button, "Return to tree");
            Check(report, !overlay.gameObject.activeSelf && tree.Pan.viewport.gameObject.activeSelf, "Return restores tree viewport");
            Check(report, tree.FocusNodeDetails("label.raise"), "Source node opens pinned details");
            tree.OpenKnowledge("算力"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            Check(report, Field<string>(tree, "knowledgeOrigin") == "label.raise", "Opening from details remembers source node");
            Click(report, back.button, "Return to source node");
            Check(report, Field<RectTransform>(tree, "tooltip").gameObject.activeSelf && Field<XgTechNodeView>(tree, "detailShown").entry.First.id == "label.raise", "Return reopens original node details");
            Check(report, host.wallet.money == money, "Browsing never spends money");
            report.status = "passed";

        }
        finally
        {
            if (report.status == "running") { report.status = "failed"; if (string.IsNullOrEmpty(report.failure)) report.failure = "Fixture interrupted; see runner exception and Editor log."; }
            string folder = Path.Combine(Path.GetTempPath(), "LingGuang-Knowledge-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "report.json"); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("KnowledgePageChecks " + (report.status == "passed" ? "PASS" : "FAIL") + ": " + path);
            host.juice?.Clear(); UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(events);
            if (hadZoom) PlayerPrefs.SetFloat("LingGuang.TechZoom", zoom); else PlayerPrefs.DeleteKey("LingGuang.TechZoom");
        }
    }
}
#endif
