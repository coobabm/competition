using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The 消耗品 shelf this scenario drives does not exist on lingguang-v05: consumables became rare items (背包,
// XgSim.Relics.cs) there on 2026-10-15. Define XG_CONSUMABLES_SHELF to bring the original scenario back with them.
#if XG_CONSUMABLES_SHELF
public static partial class DesignWallpaperSmoke
{
    static IEnumerator growthSteps;

    [MenuItem("Tools/Design Pass/Growth Scene Smoke")]
    public static void RunGrowth() { growthSteps = null; Start("growth"); }

    // The common isolated Main-scene setup hands off at step 220. Only the main agent runs Unity.
    static void TickGrowth(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        try
        {
            if (growthSteps == null) growthSteps = GrowthSceneSteps(controller, runtime);
            if (growthSteps.MoveNext())
            {
                due = EditorApplication.timeSinceStartup + (growthSteps.Current is float delay ? delay : .25f);
                return;
            }
            (growthSteps as IDisposable)?.Dispose(); growthSteps = null;
            File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0);
        }
        catch { (growthSteps as IDisposable)?.Dispose(); growthSteps = null; throw; }
    }

    static IEnumerator GrowthSceneSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave, "Growth checks use the common isolated scene fixture");
        bool wasTicking = runtime.tickEnabled, wasController = controller.enabled, wasView = controller.View.enabled;
        bool wasEnglish = controller.Sim.English, wasTest = runtime.TestMode;
        IDisposable language = null;
        try
        {
            File.WriteAllText(Path.Combine(Output, "growth-purchase.txt"), "");
            File.WriteAllText(Path.Combine(Output, "growth-backing.txt"), "");
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = false;
            var page = controller.View.Tree;
            var sim = controller.Sim;
            bool english = false;
            language = GameText.Bind(() => english ? GameText.English : GameText.Chinese);
            Check(sim.S.stage == 1 && sim.S.best.Count == 0 && !sim.S.paidAssessmentReached
                && !sim.S.treeIntroDelivered && sim.S.migratedBeats.Count == 0 && !controller.FeatureVisible("tree"),
                "New-game growth alias fixture has no graded model, old tree intro or migrated unlock");
            foreach (string route in new[] { "tree", "items", "abilities", "growth" })
            {
                Check(controller.View.FeatureOpen(route) && controller.View.FeatureOpen(route) == controller.View.FeatureOpen("tree"),
                    "New-game legacy entry has the same access as growth without granting core unlocks: " + route);
                controller.View.Open(route);
                Check(controller.View.Tab == "tree" && page.root.gameObject.activeInHierarchy
                    && !controller.View.Items.root.gameObject.activeSelf && !controller.View.Abilities.root.gameObject.activeSelf,
                    "Legacy route opens the single growth page: " + route);
                Check(controller.View.NavButton(route) == controller.View.NavButton("tree"),
                    "Legacy guide highlight reaches the growth navigation: " + route);
            }
            page.SetGrowthShelf(false);
            controller.View.Open("home");
            yield return .25f;
            var homeBuy = Array.Find(controller.View.Home.root.GetComponentsInChildren<Button>(true), button => button.name == "BuyItems");
            Check(homeBuy != null && homeBuy.gameObject.activeInHierarchy && homeBuy.IsInteractable(),
                "New-game home exposes its real BuyItems action");
            var entryBridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
            var entryPointer = GrowthRaycast(entryBridge, (RectTransform)homeBuy.transform);
            Check(entryPointer.pointerCurrentRaycast.gameObject != null
                && entryPointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(homeBuy.transform),
                "Real source canvas reaches the new-game home BuyItems action");
            Click((RectTransform)homeBuy.transform);
            yield return .25f;
            Check(controller.View.Tab == "tree" && page.GrowthShelfVisible,
                "Actual LCD click on new-game home BuyItems opens growth and its shelf");
            Check(controller.Host.Money < XgConsumables.All[0].price
                && !page.GrowthPackTarget(XgConsumables.All[0].id).GetComponent<Button>().IsInteractable()
                && !controller.FeatureVisible("tree"),
                "Opening growth through the alias preserves unaffordable purchase and core unlock gates");
            controller.View.Open("train");
            Canvas.ForceUpdateCanvases();
            var quickBar = Array.Find(controller.View.Train.root.GetComponentsInChildren<RectTransform>(true), rect => rect.name == "QuickBar");
            Check(quickBar != null && quickBar.gameObject.activeInHierarchy,
                "New-game training exposes the restock entry through the same growth access rule");
            controller.View.Open("tree");
            var legacyGuide = GrowthGuideSteps(controller, runtime);
            try { while (legacyGuide.MoveNext()) yield return legacyGuide.Current; }
            finally { (legacyGuide as IDisposable)?.Dispose(); }
            var nav = controller.View.NavButton("tree").parent;
            Check(!nav.Find("Tab items").gameObject.activeSelf && !nav.Find("Tab abilities").gameObject.activeSelf,
                "Items and abilities have no duplicate navigation rows");
            float previousX = float.NegativeInfinity;
            for (int ability = 1; ability <= XgSim.AbilityCount; ability++)
            {
                var target = page.GrowthAbilityTarget(ability);
                Check(target != null && target.gameObject.activeInHierarchy && target.localPosition.x > previousX,
                    "Real ability route runs left to right: " + ability);
                previousX = target.localPosition.x;
                Check(page.NodeTarget("ab." + ability) == target && page.FocusNode("ab." + ability),
                    "Existing ability node targets resolve to the integrated ability card: " + ability);
            }
            foreach (var discount in XgSim.AbilityDiscounts)
            {
                var items = discount.item.Length > 0 ? new[] { discount.item } : discount.itemsAny;
                foreach (var id in items)
                    Check(XgTechTreePage.GrowthColumn(XgTechTree.EntryOf(id)) == discount.ability - 1,
                        "Discount item is beneath the ability it actually helps: " + id);
            }
            string beforeRead = JsonUtility.ToJson(sim.S);
            foreach (bool useEnglish in new[] { false, true })
            {
                english = useEnglish; sim.English = useEnglish;
                GameText.PublishChanged(); page.Refresh(); Canvas.ForceUpdateCanvases();
                for (int ability = 1; ability <= XgSim.AbilityCount; ability++)
                {
                    var card = page.GrowthAbilityTarget(ability);
                    Check(card.Find("Name").GetComponent<TMP_Text>().text.Contains(XgSim.AbilityName(ability, useEnglish)),
                        "Growth ability title uses current language: " + ability);
                    if (ability > 1)
                    {
                        Check(card.Find("Parameters").GetComponent<TMP_Text>().text.Contains(XgSim.ParamsText(sim.ParamsThreshold(ability)))
                            && card.Find("Samples").GetComponent<TMP_Text>().text.Contains(XgSim.SamplesText(sim.SamplesThreshold(ability))),
                            "Integrated ability card reads the actual discounted thresholds: " + ability);
                    }
                    GrowthCheckTexts(card, "Ability " + ability + (useEnglish ? " EN" : " ZH"));
                }
            }
            Check(JsonUtility.ToJson(sim.S) == beforeRead, "Reading the integrated growth route changes no saved simulation fields");
            Check(sim.S.stage == 1 && !page.IsNodeShown("transformer"), "Future technology visibility still comes from the simulation");

            // Real discount combinations plus a large current count exercise wrapped former
            // thresholds, using only the disposable fixture. No catalog text or values are replaced.
            var unlocked = new List<string>(sim.S.unlocked);
            double trained = sim.S.trainedParamsK;
            try
            {
                sim.S.trainedParamsK = 987654321012345;
                foreach (var discount in XgSim.AbilityDiscounts)
                {
                    string id = discount.item.Length > 0 ? discount.item : discount.itemsAny[0];
                    if (!sim.S.unlocked.Contains(id)) sim.S.unlocked.Add(id);
                }
                foreach (bool useEnglish in new[] { false, true })
                {
                    english = useEnglish; sim.English = useEnglish;
                    GameText.PublishChanged(); page.Refresh(); Canvas.ForceUpdateCanvases();
                    for (int ability = 2; ability <= XgSim.AbilityCount; ability++)
                    {
                        var card = page.GrowthAbilityTarget(ability);
                        GrowthCheckTexts(card, "Discounted large-count ability " + ability + (useEnglish ? " EN" : " ZH"));
                        GrowthCheckRows(card, new[] { "Description", "Parameters", "ParameterBar", "Samples", "SampleBar" }, "Ability rows remain separate");
                        var parameters = card.Find("Parameters").GetComponent<TMP_Text>();
                        var samples = card.Find("Samples").GetComponent<TMP_Text>();
                        Check((sim.ParamsDiscount(ability) >= 1 || parameters.text.Contains("<s>"))
                            && (sim.SamplesDiscount(ability) >= 1 || samples.text.Contains("<s>")),
                            "Actual discounts show the former threshold: " + ability);
                    }
                }
            }
            finally
            {
                sim.S.unlocked.Clear(); sim.S.unlocked.AddRange(unlocked); sim.S.trainedParamsK = trained;
                page.Refresh();
            }
            Check(JsonUtility.ToJson(sim.S) == beforeRead, "Long-threshold fixture restores every simulation field");

            foreach (bool useEnglish in new[] { false, true })
            {
                english = useEnglish; sim.English = useEnglish; GameText.PublishChanged();
                var narrow = GrowthShelfSteps(controller, page, useEnglish);
                try { while (narrow.MoveNext()) yield return narrow.Current; }
                finally { (narrow as IDisposable)?.Dispose(); }
            }
            Check(JsonUtility.ToJson(sim.S) == beforeRead, "Narrow shelf reading, scrolling and closing do not change saved simulation fields");

            // The actual wallet intentionally refuses transactions in TestMode. Only this isolated
            // transaction section becomes live; saving and both simulation tick drivers stay off.
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { false });
            runtime.Sim.S.money = 10000;
            sim.S.unlocked.Remove("mlp");
            page.SetGrowthShelf(false); page.Refresh();
            var node = XgCatalog.Node("mlp");
            double thresholdBefore = sim.ParamsThreshold(2), moneyBefore = controller.Host.Money, cost = sim.NodeCost(node);
            Check(sim.Status(node, controller.Host) == XgSim.NodeStatus.Buyable && page.FocusNode("mlp"),
                "Real MLP node is buyable on the integrated growth page");
            yield return .6f;
            Click(page.NodeTarget("mlp").GetComponent<XgTechNodeView>().square);
            yield return .3f;
            Check(sim.Has("mlp") && Math.Abs(moneyBefore - controller.Host.Money - cost) < 1e-9,
                "LCD purchase on growth grants the real item and charges exactly its price");
            page.Refresh();
            Check(sim.ParamsThreshold(2) < thresholdBefore
                && page.GrowthAbilityTarget(2).Find("Parameters").GetComponent<TMP_Text>().text.Contains(XgSim.ParamsText(sim.ParamsThreshold(2))),
                "Buying the real item immediately updates its ability threshold on the same page");

            controller.View.Open("items");
            yield return .3f;
            Check(page.GrowthShelfVisible, "Legacy items route exposes the consumable shelf");
            var pack = XgConsumables.All[0];
            var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
            var buy = page.GrowthPackTarget(pack.id);
            var scroll = buy.GetComponentInParent<ScrollRect>();
            GrowthPurchaseEvidence(controller, runtime, bridge, buy, scroll, pack, "before-scroll");
            // Reopening the shelf keeps the player's reading position. Earlier narrow-window
            // checks deliberately finished at its bottom, so expose the first pack via real input.
            var wheel = GrowthRaycast(bridge, scroll.viewport);
            Check(wheel.pointerCurrentRaycast.gameObject != null
                && wheel.pointerCurrentRaycast.gameObject.transform.IsChildOf(scroll.transform),
                "Real source canvas reaches the shelf before scrolling back to its first pack");
            wheel.scrollDelta = new Vector2(0, 10000);
            Check(ExecuteEvents.ExecuteHierarchy(wheel.pointerCurrentRaycast.gameObject, wheel, ExecuteEvents.scrollHandler) == scroll.gameObject,
                "Real shelf wheel handler scrolls upward for the final purchase");
            yield return .25f; Canvas.ForceUpdateCanvases();
            GrowthPurchaseEvidence(controller, runtime, bridge, buy, scroll, pack, "after-scroll");
            var buyBounds = GrowthRectIn(buy, scroll.viewport);
            Check(scroll.verticalNormalizedPosition > .98f
                && buyBounds.xMin >= scroll.viewport.rect.xMin - 1 && buyBounds.xMax <= scroll.viewport.rect.xMax + 1
                && buyBounds.yMin >= scroll.viewport.rect.yMin - 1 && buyBounds.yMax <= scroll.viewport.rect.yMax + 1,
                "First pack purchase button is fully visible after the real wheel reaches the shelf top");
            int stockBefore = sim.ConsumableCount(pack.id);
            moneyBefore = controller.Host.Money;
            double spentBefore = sim.S.totalSpent;
            Check(stockBefore + pack.pack <= XgSim.MaxStock, "Isolated consumable fixture has room for one pack");
            Check(buy.GetComponent<Button>().IsInteractable() && moneyBefore >= pack.price && !runtime.TestMode,
                "Visible first pack is enabled with enough real Host money");
            var pointer = GrowthRaycast(bridge, buy);
            Check(pointer.pointerCurrentRaycast.gameObject != null
                && pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(buy),
                "Real source canvas reaches the visible first pack purchase button");
            controller.StartCoroutine(CaptureRendered("growth-before-shelf-purchase")); yield return .25f;
            GrowthPurchaseEvidence(controller, runtime, bridge, buy, scroll, pack, "before-click");
            Click(buy);
            GrowthPurchaseEvidence(controller, runtime, bridge, buy, scroll, pack, "immediately-after-click");
            var clickedRelay = typeof(AeroLcdInputSurface).GetField("hoverData", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(bridge.inputSurface) as PointerEventData;
            Check(clickedRelay?.pointerCurrentRaycast.gameObject != null
                && clickedRelay.pointerCurrentRaycast.gameObject.transform.IsChildOf(buy),
                "Actual LCD pointer relay reaches the first pack purchase button");
            yield return .25f;
            GrowthPurchaseEvidence(controller, runtime, bridge, buy, scroll, pack, "settled-after-click");
            Check(sim.ConsumableCount(pack.id) == stockBefore + pack.pack
                && Math.Abs(moneyBefore - controller.Host.Money - pack.price) < 1e-9
                && Math.Abs(sim.S.totalSpent - spentBefore - pack.price) < 1e-9,
                "Real LCD shelf purchase updates stock and charges one pack exactly once");
            var era = (RectTransform)page.root.Find("EraCard");
            var eraContinue = (RectTransform)era.Find("EraContinue");
            Check(era.gameObject.activeInHierarchy && eraContinue != null,
                "Real MLP purchase still presents its era card before the player continues");
            foreach (bool useEnglish in new[] { false, true })
            {
                english = useEnglish; sim.English = useEnglish; GameText.PublishChanged();
                page.Refresh(); Canvas.ForceUpdateCanvases();
                Check(eraContinue.GetComponentInChildren<TMP_Text>().text == (useEnglish ? "Continue" : "继续"),
                    "Visible era Continue action refreshes with the selected language: " + (useEnglish ? "EN" : "ZH"));
            }
            var eraPointer = GrowthRaycast(bridge, eraContinue);
            Check(eraPointer.pointerCurrentRaycast.gameObject != null
                && eraPointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(eraContinue),
                "Real source canvas reaches the era card Continue action");
            Click(eraContinue);
            yield return .25f;
            Check(!era.gameObject.activeSelf, "Actual LCD Continue click closes the era card before the full growth screenshot");
            page.FocusGrowthAbility(2); page.Refresh();
            yield return .5f;
            controller.StartCoroutine(CaptureRendered("growth-ability-item-and-shelf"));
            yield return .25f;
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
                "Growth transaction checks retain disk and tick isolation");
        }
        finally
        {
            language?.Dispose(); controller.Sim.English = wasEnglish; GameText.PublishChanged();
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { wasTest });
            runtime.tickEnabled = wasTicking; controller.enabled = wasController; controller.View.enabled = wasView;
        }
    }

    static string GrowthObjectPath(GameObject item)
    {
        if (item == null) return "<none>";
        string path = item.name;
        for (var parent = item.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }

    static void GrowthPurchaseEvidence(XingGuangController controller, ChapterOneRuntime runtime, AeroLcdSourceBridge bridge,
        RectTransform buy, ScrollRect scroll, XgConsumableDef pack, string label)
    {
        Canvas.ForceUpdateCanvases();
        var hit = GrowthRaycast(bridge, buy);
        var relay = typeof(AeroLcdInputSurface).GetField("hoverData", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(bridge.inputSurface) as PointerEventData;
        string Number(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        File.AppendAllText(Path.Combine(Output, "growth-purchase.txt"), label + " frame=" + Time.frameCount
            + " pack=" + pack.id + " quantity=" + pack.pack + " price=" + Number(pack.price)
            + " stock=" + controller.Sim.ConsumableCount(pack.id) + " money=" + Number(controller.Host.Money)
            + " wallet=" + Number(runtime.Sim.S.money) + " spent=" + Number(controller.Sim.S.totalSpent)
            + " testMode=" + runtime.TestMode + " disk=" + runtime.useDiskSave
            + " relayEnabled=" + bridge.inputSurface.enabled + " interactable=" + buy.GetComponent<Button>().IsInteractable()
            + " scroll=" + Number(scroll.verticalNormalizedPosition) + " content=" + scroll.content.anchoredPosition
            + " buttonInViewport=" + GrowthRectIn(buy, scroll.viewport) + " viewport=" + scroll.viewport.rect
            + " source=" + hit.position + " camera=" + bridge.sourceCamera.pixelRect
            + " sourceHit=" + GrowthObjectPath(hit.pointerCurrentRaycast.gameObject)
            + " relayHit=" + GrowthObjectPath(relay?.pointerCurrentRaycast.gameObject) + "\n");
    }

    static void GrowthCheckTexts(RectTransform owner, string label)
    {
        foreach (var text in owner.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate(true);
            Check(text.GetPreferredValues(text.text, text.rectTransform.rect.width, Mathf.Infinity).y <= text.rectTransform.rect.height + 2,
                label + ": TMP height fits " + text.name);
            for (int line = 0; line < text.textInfo.lineCount; line++)
                if (text.textInfo.lineInfo[line].characterCount > 0)
                    Check(text.textInfo.lineInfo[line].maxAdvance <= text.rectTransform.rect.width + 2,
                        label + ": wrapped line fits " + text.name);
            Check(!text.enableAutoSizing && text.fontSize >= 12, label + ": readable font size is retained for " + text.name);
        }
    }

    static void GrowthCheckShelfBacking(XgTechTreePage page, RectTransform shelf, string label)
    {
        var plates = new List<Image> { shelf.GetComponent<Image>() };
        foreach (var pack in XgConsumables.All) plates.Add(page.GrowthPackTarget(pack.id).parent.GetComponent<Image>());
        foreach (var plate in plates)
        {
            File.AppendAllText(Path.Combine(Output, "growth-backing.txt"), label + " plate=" + plate.name
                + " colorAlpha=" + plate.color.a + " rendererAlpha=" + plate.canvasRenderer.GetAlpha()
                + " inheritedAlpha=" + plate.canvasRenderer.GetInheritedAlpha() + "\n");
            Check(plate.color.a >= .999f && plate.canvasRenderer.GetAlpha() >= .999f
                && plate.canvasRenderer.GetInheritedAlpha() >= .999f,
                label + ": shelf backing remains opaque after the actual glass update: " + plate.name);
        }
    }

    static Rect GrowthRectIn(RectTransform subject, RectTransform space)
    {
        var corners = new Vector3[4]; subject.GetWorldCorners(corners);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (var corner in corners)
        {
            var point = (Vector2)space.InverseTransformPoint(corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static void GrowthCheckRows(RectTransform owner, string[] names, string label)
    {
        float bottom = owner.rect.yMax;
        foreach (string name in names)
        {
            var bounds = GrowthRectIn((RectTransform)owner.Find(name), owner);
            Check(bounds.yMax <= bottom + 1 && bounds.yMin >= owner.rect.yMin - 1
                && bounds.xMin >= owner.rect.xMin - 1 && bounds.xMax <= owner.rect.xMax + 1,
                label + ": " + name + " remains inside its card without overlapping the previous row");
            bottom = bounds.yMin;
        }
    }

    static PointerEventData GrowthRaycast(AeroLcdSourceBridge bridge, RectTransform target)
    {
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = bridge.sourceCamera.WorldToScreenPoint(target.TransformPoint(target.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        foreach (var raycaster in bridge.inputSurface.sourceRaycasters) raycaster.Raycast(pointer, hits);
        var compare = typeof(AeroLcdInputSurface).GetMethod("CompareRaycasts", BindingFlags.Static | BindingFlags.NonPublic);
        hits.Sort((a, b) => (int)compare.Invoke(null, new object[] { a, b }));
        if (hits.Count > 0) pointer.pointerCurrentRaycast = hits[0];
        return pointer;
    }

    static IEnumerator GrowthShelfSteps(XingGuangController controller, XgTechTreePage page, bool english)
    {
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var fit = page.root.GetComponentInParent<UiFitScale>();
        Check(bridge != null && fit != null, "Shelf checks use the actual LCD and application window");
        var window = controller.Window.windowContainer;
        Vector2 savedSize = window.sizeDelta, savedPosition = window.anchoredPosition;
        float savedMin = fit.minScale, savedMax = fit.maxScale;
        bool savedRelay = bridge.inputSurface.enabled, savedPan = page.Pan.enabled;
        bool savedShelf = page.GrowthShelfVisible;
        var savedContentPosition = page.Pan.content.anchoredPosition;
        var savedContentScale = page.Pan.content.localScale;
        try
        {
            bridge.inputSurface.enabled = false; page.Pan.enabled = false; window.DOKill();
            foreach (var size in new[] { new Vector2(840, 560), new Vector2(240, 320), new Vector2(160, 240) })
            {
                fit.minScale = fit.maxScale = 1; fit.Apply(true); Canvas.ForceUpdateCanvases();
                var desired = window.rect.size + size - page.root.rect.size;
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desired.x);
                window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desired.y);
                fit.Apply(true); Canvas.ForceUpdateCanvases();
                RectTransformUtility.ScreenPointToWorldPointInRectangle(page.root, bridge.sourceCamera.pixelRect.center, bridge.sourceCamera, out var world);
                window.position += world - page.root.TransformPoint(page.root.rect.center);
                controller.View.Open("items"); page.Refresh(); Canvas.ForceUpdateCanvases();
                Check(Vector2.Distance(page.root.rect.size, size) < 2, "Actual growth page resize reaches " + size);
                yield return .65f; // Include the production glass effect's half-second refresh.
                string label = "growth-shelf-" + (english ? "en-" : "zh-") + (int)size.x;
                var shelf = (RectTransform)page.root.Find("GrowthConsumables");
                GrowthCheckShelfBacking(page, shelf, label);
                if (english && size.x == 160)
                {
                    // Reproduce a backing that was treated before it became protected, through
                    // the real glass update. Only this isolated fixture's ancestor name changes.
                    string protectedName = shelf.name;
                    try
                    {
                        shelf.name = "GrowthShelfPreviouslyUnprotected";
                        yield return .65f;
                        Check(shelf.GetComponent<Image>().canvasRenderer.GetAlpha() < .99f,
                            "Glass restore fixture actually produced a previously treated shelf backing");
                    }
                    finally { shelf.name = protectedName; }
                    yield return .65f;
                    GrowthCheckShelfBacking(page, shelf, label + " restored-after-treatment");
                }
                var bounds = GrowthRectIn(shelf, page.root);
                Check(page.GrowthShelfVisible && bounds.xMin >= page.root.rect.xMin && bounds.xMax <= page.root.rect.xMax
                    && bounds.yMin >= page.root.rect.yMin && bounds.yMax <= page.root.rect.yMax,
                    label + ": forced narrow shelf stays inside the page");
                GrowthCheckTexts(shelf, label);
                var scroll = shelf.Find("List").GetComponent<ScrollRect>();
                scroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                foreach (var definition in XgConsumables.All)
                {
                    var card = (RectTransform)page.GrowthPackTarget(definition.id).parent;
                    GrowthCheckRows(card, new[] { "Name", "Effect", "Stock", "GrowthBuyPack " + definition.id }, label);
                }
                var close = (RectTransform)shelf.Find("GrowthShelfClose");
                var title = GrowthRectIn((RectTransform)shelf.Find("Title"), shelf);
                var closeBounds = GrowthRectIn(close, shelf);
                Check(!title.Overlaps(closeBounds) && !GrowthRectIn(scroll.viewport, shelf).Overlaps(closeBounds),
                    label + ": title and scroll viewport leave the close button clear");
                var closePointer = GrowthRaycast(bridge, close);
                Check(closePointer.pointerCurrentRaycast.gameObject != null
                    && closePointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(close), label + ": real canvas can reach shelf close");
                var navigation = controller.View.NavButton("tree");
                var navPointer = GrowthRaycast(bridge, navigation);
                Check(navPointer.pointerCurrentRaycast.gameObject != null
                    && navPointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(navigation), label + ": shelf leaves growth navigation reachable");
                if (english && size.x == 160)
                {
                    controller.StartCoroutine(CaptureRendered(label + "-top")); yield return .25f;
                }
                var pointer = GrowthRaycast(bridge, scroll.viewport);
                Check(pointer.pointerCurrentRaycast.gameObject != null
                    && pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(scroll.transform), label + ": real canvas reaches the scroll list");
                pointer.scrollDelta = new Vector2(0, -10000);
                var panPosition = page.Pan.content.anchoredPosition; var panScale = page.Pan.content.localScale;
                Check(ExecuteEvents.ExecuteHierarchy(pointer.pointerCurrentRaycast.gameObject, pointer, ExecuteEvents.scrollHandler) == scroll.gameObject,
                    label + ": shelf consumes wheel input");
                yield return .25f; Canvas.ForceUpdateCanvases();
                Check(scroll.verticalNormalizedPosition < .02f
                    && Vector2.Distance(panPosition, page.Pan.content.anchoredPosition) < .1f && panScale == page.Pan.content.localScale,
                    label + ": wheel reaches the shelf bottom without moving the tree");
                var last = page.GrowthPackTarget(XgConsumables.All[XgConsumables.All.Length - 1].id);
                var lastBounds = GrowthRectIn(last, scroll.viewport);
                Check(lastBounds.yMin >= scroll.viewport.rect.yMin - 1 && lastBounds.yMax <= scroll.viewport.rect.yMax + 1,
                    label + ": final purchase button is fully visible at the bottom");
                if (english && size.x == 160)
                {
                    controller.StartCoroutine(CaptureRendered(label + "-bottom")); yield return .25f;
                }
                closePointer = GrowthRaycast(bridge, close);
                Check(ExecuteEvents.ExecuteHierarchy(closePointer.pointerCurrentRaycast.gameObject, closePointer, ExecuteEvents.pointerClickHandler) == close.gameObject
                    && !page.GrowthShelfVisible, label + ": actual close button collapses the shelf after scrolling");
                var toggle = (RectTransform)page.root.Find("GrowthShelfToggle");
                var togglePointer = GrowthRaycast(bridge, toggle);
                Check(togglePointer.pointerCurrentRaycast.gameObject != null
                    && togglePointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(toggle), label + ": shelf can be reopened from the header");
                ExecuteEvents.ExecuteHierarchy(togglePointer.pointerCurrentRaycast.gameObject, togglePointer, ExecuteEvents.pointerClickHandler);
                Check(page.GrowthShelfVisible, label + ": actual header toggle reopens the shelf");
            }
        }
        finally
        {
            fit.minScale = savedMin; fit.maxScale = savedMax;
            window.sizeDelta = savedSize; window.anchoredPosition = savedPosition; fit.Apply(true);
            page.SetGrowthShelf(savedShelf); page.Refresh();
            page.Pan.content.localScale = savedContentScale; page.Pan.content.anchoredPosition = savedContentPosition;
            page.Pan.enabled = savedPan; bridge.inputSurface.enabled = savedRelay; Canvas.ForceUpdateCanvases();
        }
    }
}

#else
public static partial class DesignWallpaperSmoke
{
    [MenuItem("Tools/Design Pass/Growth Scene Smoke")]
    public static void RunGrowth() { Start("growth"); }

    static void TickGrowth(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        if(step!=220)return;step=221;controller.StartCoroutine(CurrentGrowthChecks(controller,runtime));
    }
    static IEnumerator CurrentGrowthChecks(XingGuangController controller,ChapterOneRuntime runtime)
    {
        var run=CurrentGrowthSteps(controller,runtime);Exception failure=null;
        while(true){bool more=false;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){failure=e;}if(failure!=null||!more)break;yield return current;}
        (run as IDisposable)?.Dispose();File.WriteAllText(Path.Combine(Output,"result.txt"),failure==null?"PASS":"FAIL: "+failure);Finish(failure==null?0:1);
    }
    static IEnumerator CurrentGrowthSteps(XingGuangController controller,ChapterOneRuntime runtime)
    {
        Check(!runtime.useDiskSave&&!runtime.tickEnabled,"Growth uses isolated purchased-curve fixture");
        purchasedBrain.Hide();controller.Open();yield return new WaitForSecondsRealtime(.4f);
        var sim=controller.Sim;var page=controller.View.Tree;
        foreach(string route in new[]{"tree","items","abilities","growth"})
        {
            controller.View.Open(route);yield return new WaitForSecondsRealtime(.15f);
            Check(controller.View.Tab=="tree"&&page.root.gameObject.activeInHierarchy,"Current merged route opens one tree: "+route);
            Check(controller.View.NavButton(route)==controller.View.NavButton("tree"),"Alias highlights single navigation: "+route);
            Check(!controller.View.Items.root.gameObject.activeSelf&&!controller.View.Abilities.root.gameObject.activeSelf,"No duplicate legacy page appears: "+route);
        }
        // Consumable shelf was replaced upstream by rare inventory. Opening its old
        // alias must not resurrect dead purchase controls or change owned resources.
        string unchanged=JsonUtility.ToJson(sim.S);double wallet=controller.Host.Money;
        page.SetGrowthShelf(true);Check(!page.GrowthShelfVisible,"Removed consumable shelf is not recreated by old alias");
        Check(JsonUtility.ToJson(sim.S)==unchanged&&controller.Host.Money==wallet,"Legacy shelf alias spends nothing and grants nothing");
        foreach(bool english in new[]{false,true})
        {
            using(GameText.Bind(()=>english?GameText.English:GameText.Chinese))
            {
                sim.English=english;GameText.PublishChanged();page.Refresh();Canvas.ForceUpdateCanvases();
                string stable=JsonUtility.ToJson(sim.S);float previous=float.NegativeInfinity;
                for(int ability=1;ability<=XgSim.AbilityCount;ability++)
                {
                    var card=page.GrowthAbilityTarget(ability);Check(card!=null&&card.gameObject.activeInHierarchy&&card.localPosition.x>previous,"Current six-ability route ordered: "+ability);previous=card.localPosition.x;
                    Check(card.Find("Name").GetComponent<TMP_Text>().text.Contains(XgSim.AbilityName(ability,english)),"Real ability name follows locale: "+ability);
                    if(ability>1)
                    {
                        string parameters=card.Find("Parameters").GetComponent<TMP_Text>().text;
                        string samples=card.Find("Samples").GetComponent<TMP_Text>().text;
                        Check(parameters.Contains(XgSim.ParamsText(sim.TrainedParamsK))&&samples.Contains(XgSim.SamplesText(sim.TrainedSamples)),"Current learned counts bind actual simulation: "+ability);
                        Check(parameters.Contains(sim.ParamsLineKnown(ability)?XgSim.ParamsText(sim.ParamsThreshold(ability)):"?"),"Parameter threshold obeys knowledge gate: "+ability);
                        Check(samples.Contains(sim.SamplesLineKnown(ability)?XgSim.SamplesText(sim.SamplesThreshold(ability)):"?"),"Data threshold obeys knowledge gate: "+ability);
                    }
                    foreach(var text in card.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Actual growth text fits "+ability+"/"+text.name+"/"+english);}
                }
                page.Refresh();Check(stable==JsonUtility.ToJson(sim.S),"Reading growth route never edits saved learning");
                yield return new WaitForEndOfFrame();Capture(english?"growth-current-en":"growth-current-zh");
            }
        }
        GameText.PublishChanged();
        XgNode candidate=null;
        foreach(var node in XgCatalog.Nodes)if(sim.Status(node,controller.Host)==XgSim.NodeStatus.Buyable&&node.cost>0&&node.maxLevel==1){candidate=node;break;}
        Check(candidate!=null,"At least one real paid technology is available after curve purchase");
        var money=runtime.Sim.S.money;runtime.Sim.S.money=0;
        Check(!sim.BuyNode(candidate.id,controller.Host)&&!sim.Has(candidate.id),"Current technology purchase cannot bypass insufficient funds");
        runtime.Sim.S.money=money;wallet=controller.Host.Money;
        Check(sim.BuyNode(candidate.id,controller.Host)&&sim.Has(candidate.id)&&controller.Host.Money<wallet,"Actual technology purchase grants item and charges current wallet");
        double after=controller.Host.Money;Check(!sim.BuyNode(candidate.id,controller.Host)&&controller.Host.Money==after,"Repeated technology purchase cannot charge twice");
        Check(!runtime.useDiskSave&&!runtime.tickEnabled,"Growth boundary fixtures leave player saves untouched");
    }

    // Shared with the lesson-goal and sense-start scenarios.
    static PointerEventData GrowthRaycast(AeroLcdSourceBridge bridge, RectTransform target)
    {
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = bridge.sourceCamera.WorldToScreenPoint(target.TransformPoint(target.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        foreach (var raycaster in bridge.inputSurface.sourceRaycasters) raycaster.Raycast(pointer, hits);
        var compare = typeof(AeroLcdInputSurface).GetMethod("CompareRaycasts", BindingFlags.Static | BindingFlags.NonPublic);
        hits.Sort((a, b) => (int)compare.Invoke(null, new object[] { a, b }));
        if (hits.Count > 0) pointer.pointerCurrentRaycast = hits[0];
        return pointer;
    }
}
#endif
