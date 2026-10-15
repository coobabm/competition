using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
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
    static int techStep;
    static IEnumerator techSteps;
    static IDisposable techLanguage;
    static bool techEnglish;
    static XgTechTreePage techPage;
    static AeroLcdSourceBridge techBridge;
    static UiFitScale techFit;

    [MenuItem("Tools/Design Pass/Tech Tree Scene Smoke")]
    public static void RunTech() { techStep = 0; Start("tech"); }

    // The common runner supplies the real Main scene after its isolated setup at step 180.
    // Each yield is a short frame-settling delay; capture always waits for a rendered frame.
    static void TickTech(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        try
        {
            if (techStep++ == 0) techSteps = TechSceneSteps(controller, runtime);
            if (techPage != null) techPage.Tick(0);
            if (techSteps.MoveNext())
            {
                due = EditorApplication.timeSinceStartup + (techSteps.Current is float seconds ? seconds : .25f);
                return;
            }
            TechDispose();
            File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS");
            Finish(0);
        }
        catch
        {
            TechDispose();
            throw; // The shared runner records the full failure and closes this isolated run.
        }
    }

    static void TechDispose()
    {
        (techSteps as IDisposable)?.Dispose(); techSteps = null;
        techLanguage?.Dispose(); techLanguage = null;
        GameText.PublishChanged();
    }

    static IEnumerator TechSceneSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave, "Tech scene uses the common isolated fixture");
        SkillTreeCopyChecks.Run();
        runtime.tickEnabled = false;
        controller.enabled = false;
        controller.View.enabled = false;
        techPage = controller.View.Tree;
        techBridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        techFit = techPage.root.GetComponentInParent<UiFitScale>();
        Check(techPage != null && techBridge != null && techFit != null, "Actual Main scene owns the production tech page and LCD canvas");
        // Ignore the user's stationary mouse while moving fixtures around the screen. Raycast and
        // handler checks below still use the actual canvas; the LCD relay is restored for purchase.
        techBridge.inputSurface.enabled = false;
        File.WriteAllText(Path.Combine(Output, "tech-raycasts.txt"), "");
        File.WriteAllText(Path.Combine(Output, "tech-cases.txt"), "");
        techLanguage = GameText.Bind(() => techEnglish ? GameText.English : GameText.Chinese);
        controller.Sim.S.stage = XgCatalog.StageNames.Length - 1;
        controller.Sim.S.autoLabelRevealed = true;
        controller.View.ShowTab("tree");
        techPage.Pan.enabled = false;
        yield return .5f;

        foreach (bool english in new[] { false, true })
        {
            TechSetLanguage(controller, english);
            TechWindow(controller, new Vector2(840, 560), 1, -1);
            // Measure actual production tooltip strings, including appended requirements/discounts.
            // Candidate text is never substituted into the production page.
            XgTechNodeView longestTitle = null, longestBody = null;
            float titleHeight = -1, bodyHeight = -1;
            foreach (var node in techPage.root.GetComponentsInChildren<XgTechNodeView>())
            {
                if (XgSim.IsAtlas(node.entry.First) || controller.Sim.NodeMystery(node.entry.First)) continue;
                techPage.Hover(node, true);
                float title = TechField<TMP_Text>("tipName").GetPreferredValues(TechField<TMP_Text>("tipName").text, 72, Mathf.Infinity).y;
                float body = TechField<TMP_Text>("tipBody").GetPreferredValues(TechField<TMP_Text>("tipBody").text, 116, Mathf.Infinity).y;
                if (title > titleHeight) { titleHeight = title; longestTitle = node; }
                if (body > bodyHeight) { bodyHeight = body; longestBody = node; }
            }
            Check(longestTitle != null && longestBody != null, "Longest live tooltip title and effect selected for " + (english ? "EN" : "ZH"));
            File.AppendAllText(Path.Combine(Output, "tech-cases.txt"), (english ? "EN" : "ZH") + " longest title=" + longestTitle.entry.id + " effect=" + longestBody.entry.id + "\n");
            var nodes = new[] { longestTitle, longestBody, longestTitle, longestBody };
            var sizes = new[] { new Vector2(840, 560), new Vector2(840, 560), new Vector2(240, 320), new Vector2(160, 240) };
            for (int index = 0; index < nodes.Length; index++)
            {
                string label = "tech-" + (english ? "en-" : "zh-") + index + "-" + nodes[index].entry.id.Replace('.', '-');
                TechWindow(controller, sizes[index], index == 2 ? 1.25f : index == 3 ? .75f : 1, index);
                TechShow(nodes[index], index);
                yield return .25f;
                // Normal desktop notifications are above application windows. Close them through
                // their real Close button before checking the unobscured tech page at that edge.
                foreach (float delay in TechDismissTrayPopups()) yield return delay;
                TechLayout(label);
                TechCapture(controller, label + "-top");
                yield return .25f; // Let the top frame render before any wheel input or failure.
                var scroll = TechField<ScrollRect>("tipScroll");
                var treePosition = techPage.Pan.content.anchoredPosition;
                var treeScale = techPage.Pan.content.localScale;
                var pointer = TechRaycast(scroll.viewport, new Vector2(.5f, .5f), label + " center");
                Check(pointer.pointerCurrentRaycast.gameObject != null
                    && pointer.pointerCurrentRaycast.gameObject.transform.IsChildOf(scroll.transform), label + ": real canvas raycast reaches the tooltip");
                // Crossing the gap keeps the card open; entering the card cancels its grace timeout.
                techPage.Hover(nodes[index], false);
                ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                pointer.scrollDelta = new Vector2(0, -10000);
                var receiver = ExecuteEvents.ExecuteHierarchy(pointer.pointerCurrentRaycast.gameObject, pointer, ExecuteEvents.scrollHandler);
                Check(receiver == scroll.gameObject, label + ": wheel is consumed by the tooltip");
                yield return .4f;
                Check(scroll.gameObject.activeInHierarchy, label + ": card survives the node-to-tooltip crossing");
                Check(Vector2.Distance(treePosition, techPage.Pan.content.anchoredPosition) < .1f && treeScale == techPage.Pan.content.localScale,
                    label + ": tooltip wheel leaves tree pan and zoom unchanged");
                TechBottom(label);
                TechCapture(controller, label + "-bottom");
                yield return .25f; // Keep the bottom state until the screenshot reaches end-of-frame.
                if (scroll.content.rect.height > scroll.viewport.rect.height + 1)
                {
                    Check(scroll.verticalScrollbar.gameObject.activeInHierarchy, label + ": long content exposes a scrollbar");
                    // Drive the actual scrollbar's pointer and drag handlers, with its source-camera raycast.
                    var bar = scroll.verticalScrollbar;
                    var start = TechRaycast(bar.handleRect, new Vector2(.5f, .5f));
                    start.pointerPressRaycast = start.pointerCurrentRaycast;
                    start.pressPosition = start.position;
                    ExecuteEvents.Execute(bar.gameObject, start, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.Execute(bar.gameObject, start, ExecuteEvents.beginDragHandler);
                    start.position = techBridge.sourceCamera.WorldToScreenPoint(((RectTransform)bar.transform).TransformPoint(new Vector2(0, ((RectTransform)bar.transform).rect.yMax)));
                    ExecuteEvents.Execute(bar.gameObject, start, ExecuteEvents.dragHandler);
                    ExecuteEvents.Execute(bar.gameObject, start, ExecuteEvents.endDragHandler);
                    Canvas.ForceUpdateCanvases();
                    Check(scroll.verticalNormalizedPosition > .98f, label + ": dragging the real scrollbar reaches the title again");
                }
                yield return .25f;
            }
        }

        TechWindow(controller, new Vector2(240, 320), 1, -1);
        TechSetLanguage(controller, false);
        var relu = TechNode("relu"); TechShow(relu, 0);
        yield return .25f;
        TechLayout("ReLU Chinese");
        var beforeName = TechField<TMP_Text>("tipName").text;
        TechSetLanguage(controller, true); // Do not leave or re-enter the hovered node.
        yield return .25f;
        Check(TechField<TMP_Text>("tipName").text != beforeName && TechField<TMP_Text>("tipName").text == XgTechTree.Name(relu.entry, controller.Sim), "Hovering ReLU refreshes its title on language change");
        Check(TechField<TMP_Text>("tipBody").text.Contains("98%") && TechField<TMP_Text>("tipBody").text.Contains("Effect:"), "Hovering ReLU refreshes its complete English effect");
        TechLayout("ReLU English after live switch"); TechCapture(controller, "tech-relu-live-language");
        var augment = TechNode("augment"); TechShow(augment, 1);
        yield return .25f;
        Check(TechField<TMP_Text>("tipName").text == XgTechTree.Name(augment.entry, controller.Sim)
            && TechField<TMP_Text>("tipBody").text.Contains("effective sample count"), "Rapid node switch replaces ReLU with the integrated augmentation effect");
        TechLayout("Augmentation English"); TechCapture(controller, "tech-augmentation");

        foreach (bool english in new[] { false, true })
        {
            TechSetLanguage(controller, english);
            controller.Sim.S.autoLabelRevealed = false; techPage.Refresh();
            var mystery = TechNode("label.auto"); TechShow(mystery, 0);
            yield return .25f;
            Check(TechField<TMP_Text>("tipBody").text == controller.Sim.NodeNote(mystery.entry.First)
                && TechField<TMP_Text>("tipCost").text == "？", "Mystery tooltip exposes only its hint in " + (english ? "EN" : "ZH"));
            TechCapture(controller, "tech-mystery-" + (english ? "en" : "zh"));
            controller.Sim.S.autoLabelRevealed = true;
            controller.Sim.S.payRaise = XgCatalog.RaiseMax; techPage.Refresh();
            TechShow(TechNode("label.raise"), 1);
            yield return .25f;
            Check(TechField<TMP_Text>("tipCost").text.Contains("MAX"), "Fully upgraded pay node has no further price in " + (english ? "EN" : "ZH"));
            TechLayout("Max pay " + (english ? "EN" : "ZH"));
        }

        // A language/size change, page switch and transient hiding must not retain an old card.
        controller.View.ShowTab("home"); controller.View.ShowTab("tree");
        Check(!TechField<RectTransform>("tooltip").gameObject.activeSelf, "Returning to the tech page clears the old hover card");
        TechShow(TechNode("relu"), 0);
        TechNode("relu").gameObject.SetActive(false); techPage.Tick(0);
        Check(!TechField<RectTransform>("tooltip").gameObject.activeSelf, "Hiding a hovered node closes its tooltip immediately");
        techPage.Refresh();
        TechWindow(controller, new Vector2(840, 560), 1, -1);
        var weights = TechNode("weights");
        controller.Sim.S.unlocked.Remove("weights");
        runtime.Sim.S.money = 10000;
        bool wasTest = runtime.TestMode;
        try
        {
            // The production host deliberately refuses all wallet access in TestMode. Enable
            // this one real transaction while the isolated runtime still cannot tick or save.
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { false });
            Check(!runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled, "Final purchase keeps disk saving and simulation ticking disabled");
            techPage.Refresh(); TechShow(weights, 0);
            var tip = TechField<ScrollRect>("tipScroll");
            ExecuteEvents.Execute(tip.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return .4f;
            techPage.Tick(0);
            Check(!tip.gameObject.activeSelf, "Leaving the tooltip closes it after its pointer grace period");
            double cost = XgTechTree.Cost(weights.entry, controller.Sim);
            double moneyBefore = runtime.Sim.S.money, spentBefore = controller.Sim.S.totalSpent;
            var status = XgTechTree.Status(weights.entry, controller.Sim, controller.Host);
            File.AppendAllText(Path.Combine(Output, "tech-raycasts.txt"), "purchase before: status=" + status + " hostMoney=" + controller.Host.Money + " cost=" + cost + " testMode=" + runtime.TestMode + "\n");
            Check(status == XgSim.NodeStatus.Buyable && cost > 0 && Math.Abs(controller.Host.Money - moneyBefore) < 1e-9,
                "Real wallet and prerequisites make the final node buyable");
            var target = TechRaycast(weights.square, new Vector2(.5f, .5f), "purchase node").pointerCurrentRaycast.gameObject;
            Check(target != null && target.transform.IsChildOf(weights.transform), "Real source canvas reaches the purchase node after closing its tooltip");
            TechCapture(controller, "tech-before-purchase");
            yield return .25f;
            // Use the same LCD pointer path as a player after reading and closing the card.
            techBridge.inputSurface.enabled = true;
            Click(weights.square);
            var relay = typeof(AeroLcdInputSurface).GetField("hoverData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(techBridge.inputSurface) as PointerEventData;
            File.AppendAllText(Path.Combine(Output, "tech-raycasts.txt"), "purchase after: relayHit=" + relay?.pointerCurrentRaycast.gameObject + " owned=" + controller.Sim.Has("weights") + " money=" + runtime.Sim.S.money + " spent=" + controller.Sim.S.totalSpent + "\n");
            Check(controller.Sim.Has("weights"), "Returning to a real node still purchases through the LCD pointer path");
            Check(Math.Abs((moneyBefore - runtime.Sim.S.money) - cost) < 1e-9
                && Math.Abs((controller.Sim.S.totalSpent - spentBefore) - cost) < 1e-9,
                "LCD purchase charges the displayed cost exactly once and records matching total spent");
            TechCapture(controller, "tech-purchased-after-tooltip");
            yield return .25f;
        }
        finally
        {
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { wasTest });
        }
    }

    static T TechField<T>(string field) => (T)typeof(XgTechTreePage).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(techPage);

    static IEnumerable<float> TechDismissTrayPopups()
    {
        float deadline = Time.unscaledTime + 15;
        var dismissed = new HashSet<int>();
        while (true)
        {
            bool busy = false;
            foreach (var tray in UnityEngine.Object.FindObjectsByType<TrayPopups>(FindObjectsSortMode.None))
            {
                if (!tray.IsBusy) continue;
                busy = true;
                foreach (var button in tray.GetComponentsInChildren<Button>())
                {
                    if (button.name != "Close" || dismissed.Contains(button.GetInstanceID())) continue;
                    var pointer = TechRaycast((RectTransform)button.transform, new Vector2(.5f, .5f), "dismiss tray notification");
                    var hit = pointer.pointerCurrentRaycast.gameObject;
                    // A sliding notification may not yet expose its close button. Wait for its
                    // production opening animation instead of bypassing the canvas hit test.
                    if (hit == null || !hit.transform.IsChildOf(button.transform)) continue;
                    var receiver = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
                    Check(receiver == button.gameObject, "Existing tray notification closes through its actual Close button");
                    dismissed.Add(button.GetInstanceID());
                }
            }
            if (!busy) yield break;
            Check(Time.unscaledTime < deadline, "Tray notifications finish their normal close animation before tech edge inspection");
            yield return .2f;
        }
    }

    static XgTechNodeView TechNode(string id)
    {
        var rect = techPage.NodeTarget(id);
        Check(rect != null, "Real scene tech node exists: " + id);
        return rect.GetComponent<XgTechNodeView>();
    }

    static void TechSetLanguage(XingGuangController controller, bool english)
    {
        techEnglish = english; controller.Sim.English = english;
        GameText.PublishChanged();
        techPage.Refresh(); techPage.Tick(0); Canvas.ForceUpdateCanvases();
    }

    static void TechWindow(XingGuangController controller, Vector2 pageSize, float scale, int corner)
    {
        var window = controller.Window.windowContainer;
        window.DOKill();
        techFit.minScale = techFit.maxScale = scale;
        techFit.Apply(true); Canvas.ForceUpdateCanvases();
        var size = window.rect.size + (pageSize - techPage.root.rect.size) * scale;
        window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        techFit.Apply(true); Canvas.ForceUpdateCanvases();
        Check(Vector2.Distance(techPage.root.rect.size, pageSize) < 2, "Actual application resize gives tech page " + pageSize + " at scale " + scale);
        var camera = techBridge.sourceCamera;
        var page = techPage.root;
        var screen = camera.pixelRect;
        Vector2 anchor = corner < 0 ? page.rect.center : new Vector2((corner & 1) == 0 ? page.rect.xMin : page.rect.xMax, (corner & 2) == 0 ? page.rect.yMax : page.rect.yMin);
        // Deliberately let the page overhang by 16 source pixels to exercise screen intersection.
        Vector2 target = corner < 0 ? screen.center : new Vector2((corner & 1) == 0 ? screen.xMin - 16 : screen.xMax + 16, (corner & 2) == 0 ? screen.yMax + 16 : screen.yMin - 16);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(page, target, camera, out var world);
        window.position += world - page.TransformPoint(anchor);
        techPage.Refresh(); techPage.Pan.SetView(1, Vector2.zero);
        Canvas.ForceUpdateCanvases();
    }

    static void TechShow(XgTechNodeView node, int corner)
    {
        techPage.Hover(node, false); techPage.Tick(0);
        // Reset the previous card before asking the real pointer-enter handler to reopen this node.
        typeof(XgTechTreePage).GetMethod("HideTooltip", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(techPage, null);
        typeof(XgTechTreePage).GetField("hovered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(techPage, null);
        node.transform.DOKill(); node.transform.localScale = Vector3.one;
        var view = techPage.Pan.viewport;
        var point = new Vector2((corner & 1) == 0 ? view.rect.xMin + 40 : view.rect.xMax - 40, (corner & 2) == 0 ? view.rect.yMax - 40 : view.rect.yMin + 40);
        techPage.Pan.content.position += view.TransformPoint(point) - node.square.TransformPoint(node.square.rect.center);
        node.OnPointerEnter(new PointerEventData(EventSystem.current));
        techPage.Tick(0); Canvas.ForceUpdateCanvases();
    }

    static PointerEventData TechRaycast(RectTransform rect, Vector2 normalized, string context = "scrollbar")
    {
        var local = rect.rect.min + Vector2.Scale(rect.rect.size, normalized);
        var data = new PointerEventData(EventSystem.current) { position = techBridge.sourceCamera.WorldToScreenPoint(rect.TransformPoint(local)), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        foreach (var raycaster in techBridge.inputSurface.sourceRaycasters) raycaster.Raycast(data, hits);
        var compare = typeof(AeroLcdInputSurface).GetMethod("CompareRaycasts", BindingFlags.Static | BindingFlags.NonPublic);
        hits.Sort((a, b) => (int)compare.Invoke(null, new object[] { a, b }));
        if (hits.Count > 0) data.pointerCurrentRaycast = hits[0];
        var trace = context + " source=" + data.position + " camera=" + techBridge.sourceCamera.pixelRect + " rect=" + rect.rect + " frame=" + Time.frameCount + "\n";
        foreach (var hit in hits)
        {
            string path = hit.gameObject.name;
            for (var parent = hit.gameObject.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            trace += "  " + path + " depth=" + hit.depth + " order=" + hit.sortingOrder + " distance=" + hit.distance + "\n";
        }
        File.AppendAllText(Path.Combine(Output, "tech-raycasts.txt"), trace);
        return data;
    }

    static Rect TechRectIn(RectTransform subject, RectTransform space)
    {
        var corners = new Vector3[4]; subject.GetWorldCorners(corners);
        var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var corner in corners)
        {
            var p = (Vector2)space.InverseTransformPoint(corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static void TechLayout(string label)
    {
        Canvas.ForceUpdateCanvases();
        var tip = TechField<RectTransform>("tooltip");
        Check(tip.gameObject.activeInHierarchy, label + ": tooltip is visible in the actual scene");
        var bounds = TechRectIn(tip, techPage.root); var page = techPage.root.rect;
        Check(bounds.xMin >= page.xMin + 3 && bounds.xMax <= page.xMax - 3 && bounds.yMin >= page.yMin + 3 && bounds.yMax <= page.yMax - 3, label + ": card stays inside the page");
        var corners = new Vector3[4]; tip.GetWorldCorners(corners); var screen = techBridge.sourceCamera.pixelRect;
        foreach (var corner in corners)
        {
            var p = techBridge.sourceCamera.WorldToScreenPoint(corner);
            Check(p.x >= screen.xMin - 1 && p.x <= screen.xMax + 1 && p.y >= screen.yMin - 1 && p.y <= screen.yMax + 1, label + ": card corner stays inside the rendered LCD");
        }
        foreach (var rect in techBridge.sourceCanvas.rootCanvas.GetComponentsInChildren<RectTransform>())
            if (rect.name == "Taskbar")
            {
                var bar = TechRectIn(rect, techPage.root);
                Check(!bounds.Overlaps(bar), label + ": card stays clear of the actual taskbar");
                break;
            }
        foreach (string field in new[] { "tipName", "tipLevel", "tipBody", "tipCost" })
        {
            var text = TechField<TMP_Text>(field); text.ForceMeshUpdate();
            float needed = text.GetPreferredValues(text.text, text.rectTransform.rect.width, Mathf.Infinity).y;
            Check(needed <= text.rectTransform.rect.height + 2, label + ": TMP has enough height for " + field);
            for (int i = 0; i < text.textInfo.lineCount; i++)
            {
                var line = text.textInfo.lineInfo[i];
                if (line.characterCount > 0) Check(line.maxAdvance <= text.rectTransform.rect.width + 2, label + ": wrapped line fits " + field);
            }
        }
    }

    static void TechBottom(string label)
    {
        Canvas.ForceUpdateCanvases();
        var scroll = TechField<ScrollRect>("tipScroll");
        if (scroll.content.rect.height > scroll.viewport.rect.height + 1)
            Check(scroll.verticalNormalizedPosition < .02f, label + ": wheel reaches the real bottom");
        var cost = (RectTransform)TechField<TMP_Text>("tipCost").transform.parent;
        var visible = TechRectIn(cost, scroll.viewport);
        Check(visible.yMin >= scroll.viewport.rect.yMin - 1 && visible.yMax <= scroll.viewport.rect.yMax + 1,
            label + ": entire cost/status bar is readable at the bottom");
        var hit = TechRaycast(cost, new Vector2(.5f, .5f), label + " cost").pointerCurrentRaycast.gameObject;
        Check(hit != null && hit.transform.IsChildOf(scroll.transform), label + ": cost/status row is not covered by another canvas control");
    }

    static void TechCapture(XingGuangController controller, string label) => controller.StartCoroutine(CaptureRendered(label));
}
