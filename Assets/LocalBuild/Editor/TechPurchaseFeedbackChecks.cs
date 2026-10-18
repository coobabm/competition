#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Real XgTechTreePage + real XgSim purchasing, with an in-memory IXgHost wallet.
// Does not instantiate ChapterOneRuntime, load Main or access a player save.
public static class TechPurchaseFeedbackChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    static IList Waves(XgJuice fx) => Field<IList>(fx, "waves");
    static bool Motion(XgTechNodeView node)
    {
        var tween = Field<Tween>(node, "squareMotion");
        return tween != null && tween.IsActive() && tween.IsPlaying();
    }
    static int AudioIndex => (int)typeof(XgJuice.Sfx).GetField("next", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    static GameObject ClickHandlerAt(EventSystem events, RectTransform rect, Vector2 local)
    {
        var pointer = new PointerEventData(events)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        events.RaycastAll(pointer, hits);
        Debug.Log("TECH_FEEDBACK_RAYCAST rect=" + rect.name + " screen=" + pointer.position + " size=" + rect.rect + " hits=" + string.Join(",", hits.ConvertAll(hit => hit.gameObject.name + ":" + hit.depth)));
        return hits.Count == 0 ? null : ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
    }
    [Serializable] sealed class Report { public string status = "running"; public List<string> passed = new List<string>(); }
    static void Check(Report report, bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException("Tech purchase feedback: " + label);
        report.passed.Add(label);
    }
    sealed class Wallet : IXgHost
    {
        public double money = 1e8;
        public bool refuse;
        public double Money => money;
        public double Compute => 4;
        public double VramMB => 65536;
        public string Blocker => null;
        public bool Spend(double amount) { if (refuse || amount > money) return false; money -= amount; return true; }
        public void Earn(double amount) { money += amount; }
        public void Train(double seconds) { }
    }
    sealed class PageHost : IXgPageHost
    {
        public XgSim sim = new XgSim();
        public Wallet wallet = new Wallet();
        public XgJuice juice;
        public XgTechTreePage tree;
        public bool visible = true;
        public XingGuangController Controller => null;
        public XgSim Sim => sim;
        public IXgHost Host => wallet;
        public XgJuice Juice => juice;
        public int BuyAmount { get; set; } = 1;
        public bool Visible => visible;
        public string Tab => "tree";
        public RectTransform ComboChip => null;
        public void Refresh(bool force) { tree.Refresh(); }
        public void ShowToast(string text, float seconds) { }
        public void ShowTab(string id) { }
        public bool BuyNode(XgNode node, RectTransform from) => tree.Buy(node, from);
    }

    public static IEnumerator Frames()
    {
        var report = new Report();
        var root = new GameObject("Tech feedback fixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(AudioListener));
        var eventsGo = new GameObject("Tech feedback events", typeof(EventSystem));
        var host = new PageHost();
        var tree = host.tree = new XgTechTreePage();
        bool hadZoom = PlayerPrefs.HasKey("LingGuang.TechZoom");
        float previousZoom = PlayerPrefs.GetFloat("LingGuang.TechZoom", 1);
        int bought = 0;
        Action<XgNode> onBought = node => { bought++; tree.OnBought(node); tree.Refresh(); };
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(1400, 900);
            host.juice = root.AddComponent<XgJuice>();
            host.juice.DisableMotionWhenReduced = true; // Same opt-in as XingGuangView.Build.
            host.juice.LocalHitStop = true; host.juice.Muted = true;
            host.juice.Visible = () => host.visible;
            var font = XgUiV4Fonts.Sans;
            Check(report, font != null, "Use the shipped CJK-capable exe font in the actual fixture");
            host.juice.Init(rt, rt, font);
            host.sim.English = true;
            tree.Init(host, new XgUi(font, null, true));
            tree.Build(rt); tree.Refresh(); host.sim.NodeBought += onBought;
            // Let the overlay/sub-Canvas register and render before testing real raycaster depth.
            // WaitForEndOfFrame does not resume in an Editor batch run; use natural frames.
            yield return null; yield return null;
            var node = tree.NodeTarget("label.raise").GetComponent<XgTechNodeView>();
            var pointer = new PointerEventData(eventsGo.GetComponent<EventSystem>())
                { button = PointerEventData.InputButton.Left, pointerId = -1 };
            Check(report, node != null && node.gameObject.activeInHierarchy, "Real purchasable node exists");
            Check(report, node.GetComponent<Image>().raycastTarget && !node.fill.raycastTarget, "Fixed hit square, animated face");
            Canvas.ForceUpdateCanvases();
            var viewport = tree.Pan.viewport;
            tree.Pan.SetView(1, new Vector2(viewport.rect.width * .5f - node.home.x, -viewport.rect.height * .5f - node.home.y));
            Canvas.ForceUpdateCanvases();
            yield return null;
            var hitRect = (RectTransform)node.transform;
            var edge = new Vector2(hitRect.rect.xMax - .5f, hitRect.rect.center.y);
            Check(report, ClickHandlerAt(EventSystem.current, hitRect, edge) == node.gameObject, "GraphicRaycaster resolves actual node edge before press");
            node.OnPointerDown(pointer);
            Check(report, node.square.localScale == Vector3.one * .95f && node.transform.localScale == Vector3.one, "Press is immediate 95%, hit geometry unchanged");
            Canvas.ForceUpdateCanvases();
            Check(report, ClickHandlerAt(EventSystem.current, hitRect, edge) == node.gameObject, "Shrunken face leaves original edge raycast target intact");
            node.OnPointerUp(pointer);
            yield return new WaitForSecondsRealtime(.3f);
            Check(report, node.square.localScale == Vector3.one && bought == 0, "Release restores rest without purchase");
            var details = node.GetComponentInChildren<XgButtonPressFeedback>(true);
            var detailsRect = (RectTransform)details.transform;
            var detailsHandler = ClickHandlerAt(EventSystem.current, detailsRect, detailsRect.rect.center);
            Check(report, detailsHandler == details.gameObject, "GraphicRaycaster gives Details its own click handler");
            ExecuteEvents.Execute(detailsHandler, pointer, ExecuteEvents.pointerClickHandler);
            Check(report, bought == 0 && Field<XgTechNodeView>(tree, "detailShown") == node, "Real raycast-resolved Details opens this card without buying");
            ExecuteEvents.Execute(detailsHandler, pointer, ExecuteEvents.pointerClickHandler);
            node.OnPointerDown(pointer); node.OnCancel(pointer); node.OnPointerClick(pointer);
            Check(report, node.square.localScale == Vector3.one && bought == 0, "Cancel cannot purchase or strand pressed face");
            node.OnPointerDown(pointer); node.OnBeginDrag(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer); node.OnEndDrag(pointer);
            Check(report, bought == 0 && node.square.localScale == Vector3.one, "Drag handoff cancels press and purchase");
            tree.Pan.SetView(1, Vector2.zero);
            double before = host.wallet.money, price = host.sim.NodeCost(XgTechTree.Next(node.entry, host.sim));
            node.OnPointerDown(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer);
            Check(report, bought == 1 && Math.Abs(before - host.wallet.money - price) < 1e-6, "Successful real purchase spends exactly live price once");
            Check(report, Waves(host.juice).Count == 1 && Field<Color>(Waves(host.juice)[0], "color") == XgDark.Gold, "Only successful purchase emits gold ring");
            Check(report, Motion(node), "Success starts node rebound");
            tree.Refresh(); tree.Refresh();
            Check(report, tree.NodeTarget("label.raise").GetComponent<XgTechNodeView>() == node && Motion(node), "Repeated Refresh reuses live animated node");
            node.OnPointerEnter(pointer); node.OnPointerExit(pointer);
            Check(report, Motion(node) && Field<bool>(node, "purchaseMotion"), "Hover enter and exit do not kill success feedback");
            float peak = node.square.localScale.x;
            float deadline = Time.realtimeSinceStartup + 2;
            while (Motion(node) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                peak = Mathf.Max(peak, node.square.localScale.x);
            }
            Check(report, peak > 1.01f, "Purchase visibly enlarges the node across rendered frames");
            Check(report, node.square.localScale == Vector3.one && !Motion(node), "Purchase returns to exact rest");

            host.juice.Clear(); host.wallet.refuse = true; before = host.wallet.money;
            tree.Click(node);
            Check(report, bought == 1 && host.wallet.money == before && Waves(host.juice).Count == 0, "Spend rejection cannot debit wallet or emit purchase ring");
            host.wallet.refuse = false; host.wallet.money = 0; tree.Click(node);
            Check(report, bought == 1 && host.wallet.money == 0 && Waves(host.juice).Count == 0, "Unaffordable click has no purchase feedback");
            host.wallet.money = 1e8;
            tree.Click(node); yield return null;
            host.juice.Reduced = true; tree.Refresh(); yield return null; yield return null;
            Check(report, tree.Pan.ReduceMotion, "Page watcher propagates reduce preference to pan");
            Check(report, !Motion(node) && node.square.localScale == Vector3.one && node.square.anchoredPosition == Vector2.zero, "Reduce mid-purchase immediately clears node tween and pose");
            Check(report, Waves(host.juice).Count == 0 && host.juice.ActiveFloaters == 0 && host.juice.ActiveParticles == 0, "Reduce clears host ring, price and particles");
            before = host.wallet.money; price = host.sim.NodeCost(XgTechTree.Next(node.entry, host.sim));
            int priorBought = bought; host.juice.Muted = false; int audio = AudioIndex;
            node.OnPointerDown(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer);
            Check(report, bought == priorBought + 1 && Math.Abs(before - host.wallet.money - price) < 1e-6, "Reduced purchase preserves actual rules and price");
            Check(report, !Motion(node) && node.square.localScale == Vector3.one && Waves(host.juice).Count == 0 && host.juice.ActiveParticles == 0, "Reduced purchase and press start no animation");
            Check(report, AudioIndex != audio, "Reduce does not mute purchase sounds");
            host.juice.Muted = true; audio = AudioIndex; tree.Click(node);
            Check(report, AudioIndex == audio, "Mute independently suppresses sounds");
            host.juice.Reduced = false;
            node.OnPointerDown(pointer); host.juice.Reduced = true; yield return null; yield return null;
            Check(report, node.square.localScale == Vector3.one, "Reduce mid-hold clears press without requiring refresh");
            host.juice.Reduced = false; tree.Click(node); tree.root.gameObject.SetActive(false);
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Closing page kills its node animation");
            tree.root.gameObject.SetActive(true); tree.Refresh();
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Reopen does not resume stale animation");
            tree.Click(node); host.visible = false; yield return null; yield return null;
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Canvas-group hidden host clears node motion");
            Check(report, !tree.Pan.Moving && !tree.Pan.ReduceMotion, "Hidden host cancels pan without changing the user's reduce preference");
            host.visible = true;
            tree.Pan.GlideTo(new Vector2(-900, -900));
            Check(report, tree.Pan.Moving, "Same-frame reopen can initiate navigation");
            host.visible = false; yield return null; yield return null;
            Check(report, !tree.Pan.Moving, "Same-frame reopen and hide cancels newly pending navigation");
            host.visible = true;
            tree.Click(node);
            host.sim.NodeBought -= onBought; host.sim = new XgSim(); host.sim.NodeBought += onBought;
            tree.Refresh();
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Rebinding simulation cancels previous purchase motion");
            report.status = "passed";
            string dir = Path.Combine(Path.GetTempPath(), "LingGuang-TechFeedback-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "report.json"); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("TechPurchaseFeedbackChecks PASS: " + path);
        }
        finally
        {
            host.sim.NodeBought -= onBought;
            host.juice?.Clear();
            UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(eventsGo);
            if (hadZoom) PlayerPrefs.SetFloat("LingGuang.TechZoom", previousZoom);
            else PlayerPrefs.DeleteKey("LingGuang.TechZoom");
        }
    }
}
#endif
