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
    [Serializable] sealed class Report { public string status = "running"; public List<string> passed = new List<string>(); public List<string> uncovered = new List<string>(); public List<string> screenshots = new List<string>(); }
    // Render the real fixture Canvas, not a recreated HTML/mock UI. Manual Camera.Render works
    // in batch PlayMode where WaitForEndOfFrame does not resume. Restore all render state.
    static void CaptureCanvas(Canvas canvas, string directory, string name, Report report)
    {
        var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; float plane = canvas.planeDistance;
        var oldTarget = RenderTexture.active;
        GameObject cameraObject = null; RenderTexture target = null; Texture2D pixels = null;
        try
        {
            cameraObject = new GameObject("Temporary technology QA screenshot camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f, .025f, .04f);
            camera.orthographic = true; camera.orthographicSize = 450; camera.nearClipPlane = .01f; camera.farClipPlane = 100;
            target = new RenderTexture(1400, 900, 24); target.Create(); camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            pixels = new Texture2D(1400, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1400, 900), 0, 0); pixels.Apply();
            string path = Path.Combine(directory, name + ".png"); File.WriteAllBytes(path, pixels.EncodeToPNG());
            report.screenshots.Add(path);
        }
        finally
        {
            canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.planeDistance = plane;
            RenderTexture.active = oldTarget;
            if (pixels != null) UnityEngine.Object.Destroy(pixels);
            if (cameraObject != null) { cameraObject.GetComponent<Camera>().targetTexture = null; UnityEngine.Object.Destroy(cameraObject); }
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            Canvas.ForceUpdateCanvases();
        }
    }
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
        string dir = Path.Combine(Path.GetTempPath(), "LingGuang-TechFeedback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var root = new GameObject("Tech feedback fixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(AudioListener));
        var eventsGo = new GameObject("Tech feedback events", typeof(EventSystem));
        var host = new PageHost();
        var tree = host.tree = new XgTechTreePage();
        bool hadZoom = PlayerPrefs.HasKey("LingGuang.TechZoom");
        float previousZoom = PlayerPrefs.GetFloat("LingGuang.TechZoom", 1);
        int bought = 0;
        IDisposable locale = null;
        Action<XgNode> onBought = node => { bought++; tree.OnBought(node); tree.Refresh(); };
        try
        {
            locale = LingGuangV05.Runtime.GameText.Bind(() => "zh-CN");
            LingGuangV05.Runtime.GameText.PublishChanged();
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(1400, 900);
            host.juice = root.AddComponent<XgJuice>();
            host.juice.DisableMotionWhenReduced = true; // Same opt-in as XingGuangView.Build.
            host.juice.LocalHitStop = true; host.juice.Muted = true;
            host.juice.Visible = () => host.visible;
            var font = XgUiV4Fonts.Sans;
            Check(report, font != null, "Use the shipped CJK-capable exe font in the actual fixture");
            host.juice.Init(rt, rt, font);
            host.sim.English = false;
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
            double untouchedMoney = host.wallet.money;
            node.OnPointerEnter(pointer);
            Check(report, Field<RectTransform>(tree, "tooltip").gameObject.activeSelf && Field<XgTechNodeView>(tree, "detailShown") == node
                && bought == 0 && host.wallet.money == untouchedMoney, "Hover opens immediately without spending or waiting for Tick");
            CaptureCanvas(root.GetComponent<Canvas>(), dir, "01-node-card", report);
            var priceAction = node.cost.GetComponent<XgTechPriceAction>();
            var priceRect = node.cost.rectTransform;
            Canvas.ForceUpdateCanvases();
            var priceHandler = ClickHandlerAt(EventSystem.current, priceRect, priceRect.rect.center);
            Check(report, priceAction != null && priceHandler == priceAction.gameObject, "Hover explanation leaves the separate price action clickable");
            node.OnPointerExit(pointer);
            Check(report, !Field<RectTransform>(tree, "tooltip").gameObject.activeSelf, "Pointer exit closes explanation immediately without waiting for Tick");
            yield return new WaitForSecondsRealtime(.2f);
            node.OnPointerDown(pointer); node.OnCancel(pointer); node.OnPointerClick(pointer);
            Check(report, node.square.localScale == Vector3.one && bought == 0, "Cancel cannot purchase or strand pressed face");
            node.OnPointerDown(pointer); node.OnBeginDrag(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer); node.OnEndDrag(pointer);
            Check(report, bought == 0 && node.square.localScale == Vector3.one, "Drag handoff cancels press and purchase");
            priceAction.OnPointerDown(pointer); priceAction.OnBeginDrag(pointer); priceAction.OnEndDrag(pointer); priceAction.OnPointerClick(pointer);
            Check(report, bought == 0 && host.wallet.money == untouchedMoney, "Dragging the price row cannot purchase on release");
            tree.Pan.SetView(1, Vector2.zero);
            double before = host.wallet.money, price = host.sim.NodeCost(XgTechTree.Next(node.entry, host.sim));
            node.OnPointerDown(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer);
            Check(report, bought == 1 && Math.Abs(before - host.wallet.money - price) < 1e-6, "Node icon click purchases and spends exactly live price once");
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

            before = host.wallet.money;
            int ownedLevel = XgTechTree.Level(node.entry, host.sim);
            node.OnPointerDown(pointer); node.OnPointerUp(pointer); node.OnPointerClick(pointer);
            yield return null;
            var knowledge = Field<RectTransform>(tree, "knowledgeRoot");
            Check(report, knowledge.gameObject.activeInHierarchy && !tree.Pan.viewport.gameObject.activeSelf,
                "Clicking an owned icon opens its knowledge reader and suspends tree hit targets");
            var article = Field<XgKnowledge.Entry>(tree, "knowledgeSelected");
            Check(report, article != null && article.Key == XgTechTreeCopy.KnowledgeFor(node.entry.First, host.sim).Key,
                "Owned icon selects its own article instead of reusing an unrelated glossary selection");
            Check(report, Field<int>(tree, "knowledgeCategory") == article.Category,
                "Synthetic node article selects its own sidebar category, not the previously viewed category");
            foreach (var text in Field<RectTransform>(tree, "knowledgeDetail").GetComponentsInChildren<TMPro.TMP_Text>())
                Check(report, text.text != "延伸阅读" && text.text != "Related terms" &&
                    !text.text.StartsWith("以2016年前后技术为背景", StringComparison.Ordinal) &&
                    !text.text.StartsWith("Set around 2016.", StringComparison.Ordinal),
                    "Function-only article has no empty related heading or unrelated historical footer");
            Check(report, bought == 1 && host.wallet.money == before && XgTechTree.Level(node.entry, host.sim) == ownedLevel,
                "Opening owned repeatable node does not spend or buy its next level");
            CaptureCanvas(root.GetComponent<Canvas>(), dir, "02-owned-knowledge", report);
            Field<XgBtn>(tree, "knowledgeBack").rt.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            tree.Pan.CancelMotion();
            Check(report, !knowledge.gameObject.activeSelf && tree.Pan.viewport.gameObject.activeInHierarchy &&
                Field<XgTechNodeView>(tree, "detailShown") == node, "Knowledge back button returns to the originating node");
            CaptureCanvas(root.GetComponent<Canvas>(), dir, "03-return-to-node", report);
            tree.ToggleExplanation(node); // Close the intentionally pinned return card before later hover checks.
            Check(report, node.cost.text.Contains("Upgrade") || node.cost.text.Contains("升级"), "Repeatable owned node advertises a separate upgrade price action");

            foreach (var cardView in Field<List<XgTechNodeView>>(tree, "views"))
            {
                if (!cardView.gameObject.activeInHierarchy) continue;
                cardView.label.ForceMeshUpdate(true, true);
                Check(report, cardView.label.overflowMode != TMPro.TextOverflowModes.Ellipsis &&
                    !cardView.label.isTextTruncated && !cardView.label.isTextOverflowing,
                    "Complete rendered technology label fits without ellipsis: " + cardView.entry.id);
                var shownNode = XgTechTree.Next(cardView.entry, host.sim) ?? cardView.entry.members[cardView.entry.members.Count - 1];
                if (!XgTechTreeCopy.HasNumericEffect(shownNode, host.sim))
                {
                    var tag = XgTechTreeCopy.Tag(shownNode, host.sim);
                    Check(report, string.IsNullOrEmpty(tag) || !cardView.label.text.Contains(tag),
                        "Non-numeric node omits metadata: " + cardView.entry.id);
                }
            }

            host.juice.Clear(); host.wallet.refuse = true; before = host.wallet.money;
            tree.Click(node, true);
            Check(report, bought == 1 && host.wallet.money == before && Waves(host.juice).Count == 0, "Spend rejection cannot debit wallet or emit purchase ring");
            host.wallet.refuse = false; host.wallet.money = 0; tree.Click(node, true);
            Check(report, bought == 1 && host.wallet.money == 0 && Waves(host.juice).Count == 0, "Unaffordable click has no purchase feedback");
            host.wallet.money = 1e8;
            tree.Click(node, true); yield return null;
            host.juice.Reduced = true; tree.Refresh(); yield return null; yield return null;
            Check(report, tree.Pan.ReduceMotion, "Page watcher propagates reduce preference to pan");
            Check(report, !Motion(node) && node.square.localScale == Vector3.one && node.square.anchoredPosition == Vector2.zero, "Reduce mid-purchase immediately clears node tween and pose");
            Check(report, Waves(host.juice).Count == 0 && host.juice.ActiveFloaters == 0 && host.juice.ActiveParticles == 0, "Reduce clears host ring, price and particles");
            before = host.wallet.money; price = host.sim.NodeCost(XgTechTree.Next(node.entry, host.sim));
            int priorBought = bought; host.juice.Muted = false; int audio = AudioIndex;
            priceAction.OnPointerDown(pointer); priceAction.OnPointerClick(pointer);
            Check(report, bought == priorBought + 1 && Math.Abs(before - host.wallet.money - price) < 1e-6, "Reduced purchase preserves actual rules and price");
            Check(report, !Motion(node) && node.square.localScale == Vector3.one && Waves(host.juice).Count == 0 && host.juice.ActiveParticles == 0, "Reduced purchase and press start no animation");
            Check(report, AudioIndex != audio, "Reduce does not mute purchase sounds");
            host.juice.Muted = true; audio = AudioIndex; tree.Click(node, true);
            Check(report, AudioIndex == audio, "Mute independently suppresses sounds");
            host.juice.Reduced = false;
            node.OnPointerDown(pointer); host.juice.Reduced = true; yield return null; yield return null;
            Check(report, node.square.localScale == Vector3.one, "Reduce mid-hold clears press without requiring refresh");
            host.juice.Reduced = false; tree.Click(node, true); tree.root.gameObject.SetActive(false);
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Closing page kills its node animation");
            tree.root.gameObject.SetActive(true); tree.Refresh();
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Reopen does not resume stale animation");
            tree.Click(node, true); host.visible = false; yield return null; yield return null;
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Canvas-group hidden host clears node motion");
            Check(report, !tree.Pan.Moving && !tree.Pan.ReduceMotion, "Hidden host cancels pan without changing the user's reduce preference");
            host.visible = true;
            tree.Pan.GlideTo(new Vector2(-900, -900));
            Check(report, tree.Pan.Moving, "Same-frame reopen can initiate navigation");
            host.visible = false; yield return null; yield return null;
            Check(report, !tree.Pan.Moving, "Same-frame reopen and hide cancels newly pending navigation");
            host.visible = true;
            tree.Click(node, true);
            host.sim.NodeBought -= onBought; host.sim = new XgSim(); host.sim.NodeBought += onBought;
            tree.Refresh();
            Check(report, !Motion(node) && node.square.localScale == Vector3.one, "Rebinding simulation cancels previous purchase motion");
            // The card's own price row is what 「点击价格购买」 points at, and it covers the square's row:
            // it must buy through the same spend path, not just look like a button.
            host.visible = true;
            node = tree.NodeTarget("label.raise").GetComponent<XgTechNodeView>();
            int cardBought = bought;
            double cardMoney = host.wallet.money;
            double cardPrice = host.sim.NodeCost(XgTechTree.Next(node.entry, host.sim));
            node.OnPointerEnter(pointer);
            Check(report, !Field<CanvasGroup>(tree, "tipGroup").blocksRaycasts, "Ordinary hover details do not intercept node clicks");
            node.OnPointerExit(pointer);
            tree.ToggleExplanation(node);
            Check(report, Field<CanvasGroup>(tree, "tipGroup").blocksRaycasts, "Explicitly pinned details enable their controls");
            Canvas.ForceUpdateCanvases();
            var cardBar = Field<Image>(tree, "tipCostBar");
            var cardAction = cardBar != null ? cardBar.GetComponent<XgTechTipPriceAction>() : null;
            Check(report, Field<RectTransform>(tree, "tooltip").gameObject.activeSelf && Field<XgTechNodeView>(tree, "detailShown") == node
                && cardBar.raycastTarget && cardAction != null && cardAction.page == tree, "Card price row is a live purchase target for the shown square");
            ExecuteEvents.Execute(cardAction.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(report, bought == cardBought + 1 && Math.Abs(cardMoney - host.wallet.money - cardPrice) < 1e-6, "Clicking the card's price row buys exactly the shown price");
            // Exercise every currently purchasable catalog entry through its actual node handler.
            // Re-scan after purchases so newly unlocked descendants join the same real fixture.
            // Do not fabricate purchases or bypass quest/chat conditions just to force coverage.
            host.juice.Reduced = true; host.juice.Muted = true; host.wallet.money = 1e100;
            host.sim.S.stage = 6; tree.Refresh();
            var exercised = new HashSet<string>();
            bool progressed;
            do
            {
                progressed = false;
                foreach (var entry in XgTechTree.Entries)
                {
                    if (XgTechTree.Status(entry, host.sim, host.wallet) != XgSim.NodeStatus.Buyable) continue;
                    var next = XgTechTree.Next(entry, host.sim);
                    if (next == null) continue;
                    int levelBefore = XgTechTree.Level(entry, host.sim);
                    string coverageKey = next.id + "@" + (levelBefore + 1);
                    if (exercised.Contains(coverageKey)) continue;
                    var target = tree.NodeTarget(next.id);
                    var liveNode = target != null ? target.GetComponent<XgTechNodeView>() : null;
                    if (liveNode == null || !liveNode.gameObject.activeInHierarchy) continue;
                    tree.Pan.SetView(1, Vector2.zero);
                    host.wallet.money = 1e8 + host.sim.NodeCost(next);
                    double catalogBefore = host.wallet.money, catalogPrice = host.sim.NodeCost(next);
                    int catalogBought = bought;
                    liveNode.OnPointerEnter(pointer);
                    Check(report, Field<XgTechNodeView>(tree, "detailShown") == liveNode
                        && Field<RectTransform>(tree, "tooltip").gameObject.activeSelf, "Catalog immediate hover: " + next.id);
                    liveNode.OnPointerExit(pointer);
                    Check(report, !Field<RectTransform>(tree, "tooltip").gameObject.activeSelf, "Catalog immediate exit: " + next.id);
                    if (levelBefore == 0)
                    { liveNode.OnPointerDown(pointer); liveNode.OnPointerUp(pointer); liveNode.OnPointerClick(pointer); }
                    else
                    {
                        var upgrade = liveNode.cost.GetComponent<XgTechPriceAction>();
                        Check(report, upgrade != null, "Repeatable catalog node retains an upgrade action: " + coverageKey);
                        upgrade.OnPointerDown(pointer); upgrade.OnPointerClick(pointer);
                    }
                    double difference = Math.Abs(catalogBefore - host.wallet.money - catalogPrice);
                    Check(report, bought == catalogBought + 1 && XgTechTree.Level(entry, host.sim) == levelBefore + 1
                        && difference <= Math.Max(1e-5, Math.Abs(catalogPrice) * 1e-12), "Catalog node click ledger: " + coverageKey);
                    exercised.Add(coverageKey); progressed = true;
                    host.wallet.money = 1e100;
                }
            } while (progressed);
            foreach (var entry in XgTechTree.Entries)
            {
                int level = XgTechTree.Level(entry, host.sim), max = XgTechTree.MaxLevel(entry);
                var pending = XgTechTree.Next(entry, host.sim);
                var target = pending != null ? tree.NodeTarget(pending.id) : null;
                string reason = pending == null ? "no next purchase (owned or atlas)"
                    : XgTechTree.Status(entry, host.sim, host.wallet).ToString() + "; target="
                        + (target == null ? "missing" : target.gameObject.activeInHierarchy ? "visible" : "hidden");
                for (int missing = level + 1; missing <= max; missing++)
                    report.uncovered.Add(entry.id + " level " + missing + ": " + reason);
            }
            // Explicit regression: possessing the pretrain insight must not hijack a buyable secret icon.
            host.sim.NodeBought -= onBought; host.sim = new XgSim(); host.sim.NodeBought += onBought;
            host.sim.S.stage = 6;
            host.sim.S.unlocked.Add("transformer");
            host.sim.S.pretrainStalled = true;
            host.sim.S.insights.Add("pretrain");
            host.wallet.money = 1e8; tree.Refresh(); tree.Pan.SetView(1, Vector2.zero);
            var secretEntry = XgTechTree.EntryOf("secret.6");
            Check(report, XgTechTree.Status(secretEntry, host.sim, host.wallet) == XgSim.NodeStatus.Buyable,
                "Pretrain insight regression fixture is genuinely buyable");
            var secretNode = tree.NodeTarget("secret.6").GetComponent<XgTechNodeView>();
            double secretMoney = host.wallet.money, secretPrice = host.sim.NodeCost(XgTechTree.Next(secretEntry, host.sim));
            int secretBought = bought, secretLevel = XgTechTree.Level(secretEntry, host.sim);
            var previousInsight = XgTreePage.OpenInsightCard;
            int insightOpened = 0;
            try
            {
                XgTreePage.OpenInsightCard = _ => insightOpened++;
                secretNode.OnPointerDown(pointer); secretNode.OnPointerUp(pointer); secretNode.OnPointerClick(pointer);
                Check(report, bought == secretBought + 1 && XgTechTree.Level(secretEntry, host.sim) == secretLevel + 1
                    && Math.Abs(secretMoney - host.wallet.money - secretPrice) < 1e-6 && insightOpened == 0,
                    "Buyable secret.6 with pretrain insight purchases instead of opening insight");
            }
            finally { XgTreePage.OpenInsightCard = previousInsight; }
            Check(report, exercised.Count > 0, "Catalog sweep exercised available entries via real pointer click");
            Debug.Log("TECH_CATALOG_CLICK_COVERAGE exercised=" + exercised.Count + " total=" + XgTechTree.Entries.Count
                + "; remaining entries retain real stage, quest, ownership and visibility gates");
            report.status = "passed";
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "report.json"); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("TechPurchaseFeedbackChecks PASS: " + path);
        }
        finally
        {
            locale?.Dispose();
            LingGuangV05.Runtime.GameText.PublishChanged();
            host.sim.NodeBought -= onBought;
            host.juice?.Clear();
            UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(eventsGo);
            if (hadZoom) PlayerPrefs.SetFloat("LingGuang.TechZoom", previousZoom);
            else PlayerPrefs.DeleteKey("LingGuang.TechZoom");
        }
    }
}
#endif
