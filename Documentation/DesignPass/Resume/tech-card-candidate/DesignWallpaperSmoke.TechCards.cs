using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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

public static partial class DesignWallpaperSmoke
{
    static IEnumerator techCardSteps;

    [MenuItem("Tools/Design Pass/Four Layer Tech Cards Smoke")]
    public static void RunTechCards() { techCardSteps = null; Start("tech-cards"); }

    static void TickTechCards(XingGuangController controller, ChapterOneRuntime runtime)
    {
        try
        {
            if (techCardSteps == null) techCardSteps = TechCardSceneSteps(controller, runtime);
            if (techCardSteps.MoveNext())
            { due = EditorApplication.timeSinceStartup + (techCardSteps.Current is float delay ? delay : .25f); return; }
            (techCardSteps as IDisposable)?.Dispose(); techCardSteps = null;
            File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS"); Finish(0);
        }
        catch { (techCardSteps as IDisposable)?.Dispose(); techCardSteps = null; throw; }
    }

    static IEnumerator TechCardSceneSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave, "Card scene uses the isolated Main fixture");
        bool ticking = runtime.tickEnabled, controllerEnabled = controller.enabled, viewEnabled = controller.View.enabled, wasTest = runtime.TestMode;
        bool originalLanguage = controller.Sim.English;
        IDisposable language = null;
        try
        {
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = false;
            techPage = controller.View.Tree; techBridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
            techFit = techPage.root.GetComponentInParent<UiFitScale>();
            Check(techBridge != null && techFit != null, "Card fixture uses the rendered LCD and actual app window");
            techBridge.inputSurface.enabled = false; techPage.Pan.enabled = false;
            File.WriteAllText(Path.Combine(Output, "tech-card-actions.txt"), "");
            File.WriteAllText(Path.Combine(Output, "tech-raycasts.txt"), "");
            language = GameText.Bind(() => techEnglish ? GameText.English : GameText.Chinese);
            var sim = controller.Sim; sim.S.stage = XgCatalog.StageNames.Length - 1;
            sim.S.autoLabelRevealed = false;
            controller.View.Open("tree"); techPage.SetGrowthShelf(false);
            TechWindow(controller, new Vector2(840, 560), 1, -1);
            foreach (bool english in new[] { false, true })
            {
                TechSetLanguage(controller, english);
                string before = JsonUtility.ToJson(sim.S);
                CheckCardLayout(sim, "Mystery " + english);
                var mystery = TechNode("label.auto");
                Check(mystery.label.text == sim.NodeName(mystery.entry.First) && mystery.cost.text == "",
                    "Mystery card exposes no name or price in " + english);
                foreach (var node in techPage.root.GetComponentsInChildren<XgTechNodeView>())
                    if (node.entry.First.kind == XgNodeKind.Phenomenon)
                        Check(node.label.text == (english ? "???" : "？？？") && !Regex.IsMatch(node.square.Find("Effect").GetComponent<TMP_Text>().text, "[0-9]"),
                            "Undiscovered atlas card reveals no identity or numbers: " + node.entry.id);
                foreach (string id in new[] { "label.auto", "ph.amnesia" })
                {
                    var hidden = TechNode(id); PositionCardAction(techPage.TechDetailsTarget(id));
                    techBridge.inputSurface.enabled = true; Click(techPage.TechDetailsTarget(id)); techBridge.inputSurface.enabled = false;
                    Check(TechField<bool>("detailsOpen"), "Actual unknown card Details click opens safely: " + id);
                    Check(TechField<TMP_Text>("tipBody").text == XgTechCardCopy.Science(hidden.entry, sim),
                        "Unknown details contain only the safe discovery hint: " + id);
                    var hiddenClose = (RectTransform)TechField<RectTransform>("tooltip").Find("DetailsClose");
                    techBridge.inputSurface.enabled = true; Click(hiddenClose); techBridge.inputSurface.enabled = false;
                }
                Check(JsonUtility.ToJson(sim.S) == before, "Mystery card reads change no simulation state");
            }
            sim.S.autoLabelRevealed = true;
            foreach (var phenomenon in XgPhenomena.All) if (!sim.S.phenomena.seen.Contains(phenomenon.id)) sim.S.phenomena.seen.Add(phenomenon.id);
            var originalUnlocks = new List<string>(sim.S.unlocked);
            try
            {
                foreach (var entry in XgTechTree.Entries) if (entry.IsChain)
                    foreach (var member in entry.members) if (!sim.S.unlocked.Contains(member.id)) sim.S.unlocked.Add(member.id);
                foreach (bool english in new[] { false, true })
                { TechSetLanguage(controller, english); CheckCardLayout(sim, "All chains maxed " + english); }
            }
            finally { sim.S.unlocked.Clear(); sim.S.unlocked.AddRange(originalUnlocks); techPage.Refresh(); }

            // Real LCD details clicks must remain harmless even when the wallet is live.
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { false });
            runtime.Sim.S.money = 10000; sim.S.unlocked.Remove("weights");
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled, "Live card transaction retains the isolated no-tick/no-save fixture");
            foreach (bool english in new[] { false, true })
            {
                TechSetLanguage(controller, english); CheckCardLayout(sim, "Discovered " + english);
                foreach (var size in new[] { new Vector2(840, 560), new Vector2(240, 320), new Vector2(160, 240) })
                {
                    TechWindow(controller, size, 1, -1); techPage.SetGrowthShelf(false);
                    foreach (float delay in TechDismissTrayPopups()) yield return delay;
                    string label = "tech-card-" + (english ? "en-" : "zh-") + (int)size.x;
                    var weights = TechNode("weights");
                    var details = techPage.TechDetailsTarget("weights");
                    PositionCardAction(details);
                    weights.OnPointerEnter(new PointerEventData(EventSystem.current));
                    Canvas.ForceUpdateCanvases(); techPage.Tick(0);
                    var hit = GrowthRaycast(techBridge, details).pointerCurrentRaycast.gameObject;
                    Check(hit != null && hit.transform.IsChildOf(details), label + ": hover popup leaves Details reachable");
                    double money = controller.Host.Money, spent = sim.S.totalSpent; string before = JsonUtility.ToJson(sim.S);
                    techBridge.inputSurface.enabled = true; Click(details); techBridge.inputSurface.enabled = false;
                    yield return .2f;
                    Check(TechField<bool>("detailsOpen") && TechField<RectTransform>("tooltip").gameObject.activeSelf,
                        label + ": actual LCD Details click opens the persistent explanation");
                    Check(TechField<TMP_Text>("tipBody").text.StartsWith(XgTechCardCopy.Science(weights.entry, sim), StringComparison.Ordinal),
                        label + ": opened details uses the real 3-5 sentence science");
                    Check(JsonUtility.ToJson(sim.S) == before && controller.Host.Money == money && sim.S.totalSpent == spent && !sim.Has("weights"),
                        label + ": Details click does not purchase or mutate the simulation");
                    TechLayout(label); TechCapture(controller, label + "-details"); yield return .2f;
                    var scroll = TechField<ScrollRect>("tipScroll");
                    var wheel = GrowthRaycast(techBridge, scroll.viewport); wheel.scrollDelta = new Vector2(0, -200);
                    Check(wheel.pointerCurrentRaycast.gameObject != null && wheel.pointerCurrentRaycast.gameObject.transform.IsChildOf(scroll.transform), label + ": real canvas reaches details scrolling");
                    ExecuteEvents.ExecuteHierarchy(wheel.pointerCurrentRaycast.gameObject, wheel, ExecuteEvents.scrollHandler);
                    yield return .2f;
                    Canvas.ForceUpdateCanvases(); TechBottom(label);
                    var close = (RectTransform)scroll.transform.Find("DetailsClose");
                    var closeHit = GrowthRaycast(techBridge, close).pointerCurrentRaycast.gameObject;
                    Check(closeHit != null && closeHit.transform.IsChildOf(close), label + ": fixed Close is still reachable after scrolling");
                    techBridge.inputSurface.enabled = true; Click(close); techBridge.inputSurface.enabled = false;
                    Check(!TechField<bool>("detailsOpen") && !scroll.gameObject.activeSelf, label + ": actual LCD Close hides details");
                    Check(JsonUtility.ToJson(sim.S) == before && controller.Host.Money == money && sim.S.totalSpent == spent,
                        label + ": reading, scrolling and closing remain nontransactional");
                    File.AppendAllText(Path.Combine(Output, "tech-card-actions.txt"), label + " details/scroll/close money=" + money + " spent=" + spent + " unchanged\n");
                }
                TechWindow(controller, new Vector2(840, 560), 1, -1);
                var phenomenonNode = TechNode("ph.amnesia"); PositionCardAction(techPage.TechDetailsTarget(phenomenonNode.entry.id));
                techBridge.inputSurface.enabled = true; Click(techPage.TechDetailsTarget(phenomenonNode.entry.id)); techBridge.inputSurface.enabled = false;
                Check(TechField<TMP_Text>("tipBody").text.StartsWith(XgTechCardCopy.Science(phenomenonNode.entry, sim), StringComparison.Ordinal),
                    "Discovered phenomenon opens its three-sentence science in " + english);
                TechCapture(controller, "tech-card-phenomenon-" + (english ? "en" : "zh")); yield return .2f;
                var closePhenomenon = (RectTransform)TechField<RectTransform>("tooltip").Find("DetailsClose");
                techBridge.inputSurface.enabled = true; Click(closePhenomenon); techBridge.inputSurface.enabled = false;
            }
            TechWindow(controller, new Vector2(840, 560), 1, -1); TechSetLanguage(controller, false);
            var purchase = TechNode("weights");
            PositionCardAction(purchase.square); Canvas.ForceUpdateCanvases();
            double price = XgTechTree.Cost(purchase.entry, sim), wallet = controller.Host.Money, oldSpent = sim.S.totalSpent;
            Check(XgTechTree.Status(purchase.entry, sim, controller.Host) == XgSim.NodeStatus.Buyable && price > 0,
                "Actual card body is buyable after independent details reads");
            var purchaseHit = GrowthRaycast(techBridge, purchase.square).pointerCurrentRaycast.gameObject;
            Check(purchaseHit != null && purchaseHit.transform.IsChildOf(purchase.transform)
                && !purchaseHit.transform.IsChildOf(techPage.TechDetailsTarget("weights")), "Card purchase surface is distinct from Details");
            techBridge.inputSurface.enabled = true; Click(purchase.square); techBridge.inputSurface.enabled = false;
            Check(sim.Has("weights") && Math.Abs(wallet - controller.Host.Money - price) < 1e-9 && Math.Abs(sim.S.totalSpent - oldSpent - price) < 1e-9,
                "Actual LCD card purchase charges exactly once and grants the real node");
            File.AppendAllText(Path.Combine(Output, "tech-card-actions.txt"), "purchase weights price=" + price + " money " + wallet + " -> " + controller.Host.Money + " spent " + oldSpent + " -> " + sim.S.totalSpent + "\n");
            techPage.Refresh();
            foreach (bool english in new[] { false, true })
            {
                TechSetLanguage(controller, english); CheckCardLayout(sim, "Final " + english);
                techPage.FocusNode("lstm"); techPage.Pan.enabled = true; yield return .6f; techPage.Pan.enabled = false;
                controller.View.ShowTab("home"); controller.View.ShowTab("tree");
                PositionCardAction(TechNode("lstm").square);
                Check(!TechField<RectTransform>("tooltip").gameObject.activeSelf, "Full-card screenshot has no hovering detail overlay");
                TechCapture(controller, "tech-card-full-" + (english ? "en" : "zh")); yield return .3f;
            }
        }
        finally
        {
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { wasTest });
            runtime.tickEnabled = ticking; controller.enabled = controllerEnabled; controller.View.enabled = viewEnabled;
            controller.Sim.English = originalLanguage; language?.Dispose(); GameText.PublishChanged();
            if (techBridge != null) techBridge.inputSurface.enabled = true;
            if (techPage != null) techPage.Pan.enabled = true;
        }
    }

    static void PositionCardAction(RectTransform target)
    {
        typeof(XgTechTreePage).GetMethod("HideTooltip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(techPage, null);
        typeof(XgTechTreePage).GetField("hovered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(techPage, null);
        techPage.Pan.content.DOKill(); techPage.Pan.SetView(1, Vector2.zero);
        var node = target.GetComponentInParent<XgTechNodeView>();
        if (node != null) { node.square.DOKill(); node.square.localScale = Vector3.one; node.square.localRotation = Quaternion.identity; }
        Canvas.ForceUpdateCanvases();
        techPage.Pan.content.position += techPage.Pan.viewport.TransformPoint(techPage.Pan.viewport.rect.center) - target.TransformPoint(target.rect.center);
    }

    static void CheckCardLayout(XgSim sim, string label)
    {
        Canvas.ForceUpdateCanvases();
        var nodes = techPage.root.GetComponentsInChildren<XgTechNodeView>();
        foreach (var node in nodes)
        {
            Check(node.label.text == XgTechCardCopy.Name(node.entry, sim), label + ": card has its current formal name: " + node.entry.id);
            GrowthCheckRows(node.square, new[] { "Name", "Summary", "Effect", "Details" }, label + " " + node.entry.id);
            foreach (string field in new[] { "Name", "Summary", "Effect", "Cost" })
            {
                var text = node.square.Find(field).GetComponent<TMP_Text>(); text.ForceMeshUpdate(true);
                Check(text.GetPreferredValues(text.text, text.rectTransform.rect.width, Mathf.Infinity).y <= text.rectTransform.rect.height + 2,
                    label + ": actual TMP height fits " + node.entry.id + "/" + field);
                for (int line = 0; line < text.textInfo.lineCount; line++)
                    if (text.textInfo.lineInfo[line].characterCount > 0)
                        Check(text.textInfo.lineInfo[line].maxAdvance <= text.rectTransform.rect.width + 2, label + ": actual line fits " + node.entry.id + "/" + field);
                Check(!text.enableAutoSizing && text.fontSize >= 14, label + ": card copy keeps readable size " + field);
            }
            var summary = node.square.Find("Summary").GetComponent<TMP_Text>();
            Check(sim.English || summary.text.Length <= 12, label + ": Chinese summary at most 12 characters " + node.entry.id);
            Check(node.square.Find("Effect").GetComponent<TMP_Text>().text == XgTechCardCopy.Effect(node.entry, sim), label + ": effect uses real model " + node.entry.id);
        }
        for (int i = 0; i < nodes.Length; i++)
        {
            string collision = null;
            var rect = GrowthRectIn((RectTransform)nodes[i].transform, techPage.Pan.content);
            for (int j = i + 1; j < nodes.Length; j++)
                if (rect.Overlaps(GrowthRectIn((RectTransform)nodes[j].transform, techPage.Pan.content))) { collision = nodes[j].entry.id; break; }
            Check(collision == null, label + ": complete card " + nodes[i].entry.id + " overlaps no other card " + collision);
        }
    }
}
