#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using LingGuangV05.Desktop.Games;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static partial class BoardGamesUiPlayQa
{
    public static void RunGameCenter() => RunMode("center");

    static Rect CenterBounds(RectTransform target, RectTransform space)
    {
        var corners = new Vector3[4]; target.GetWorldCorners(corners);
        var points = corners.Select(p => space.InverseTransformPoint(p)).ToArray();
        return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
    }

    static void InspectCenterHome(string variant)
    {
        Canvas.ForceUpdateCanvases();
        var home = Field<RectTransform>(hub, "home");
        foreach (var game in hub.GetComponents<HubGame>())
        {
            var card = (RectTransform)home.Find("Card " + game.Id);
            var blurb = card.Find("Blurb").GetComponent<TMP_Text>();
            var state = card.Find("State").GetComponent<TMP_Text>();
            Inspect(blurb, game.Id + " home " + variant);
            Require(blurb.text == game.Blurb, game.Id + " home uses complete introduction " + variant);
            Require(CenterBounds(blurb.rectTransform, card).yMin >= CenterBounds(state.rectTransform, card).yMax + 3.9f,
                game.Id + " introduction stays above state with 4px gap " + variant);
        }
        Capture("game-center-home-" + variant + ".png", home);
    }

    static IEnumerator GameCenterChecks()
    {
        yield return null;
        var wait = ToolWait(1.5f); while (wait.MoveNext()) yield return wait.Current;
        var games = hub.GetComponents<HubGame>();
        Require(games.Select(g => g.Id).OrderBy(x => x).SequenceEqual(new[] { "gomoku", "go", "chess", "ludo", "checkers_cn", "monopoly" }.OrderBy(x => x)),
            "Exactly six registered games; dormant xiangqi_endgame/chess_mate rule cases are not installed games");
        ClickNamed(hub.transform, "Nav home"); yield return null;
        InspectCenterHome("computer");
        foreach (var game in games)
        {
            ClickNamed(hub.transform, "Nav " + game.Id); yield return null; yield return null;
            var card = Field<RectTransform>(game, "ruleCard");
            Require(card.gameObject.activeInHierarchy, game.Id + " actual first-open rules visible");
            var records = Field<Button>(game, "recordsButton");
            Require(!records.gameObject.activeSelf, game.Id + " stage one records entry hidden");
            var scroll = card.GetComponentInChildren<ScrollRect>();
            Require(scroll != null && scroll.vertical && !scroll.horizontal && scroll.viewport.GetComponent<RectMask2D>() != null,
                game.Id + " actual rules use vertical clipped scrolling");
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            var text = Field<TMP_Text>(game, "ruleText"); Inspect(text, game.Id + " full scroll content");
            Require(text.text.Contains("同一种棋") && text.text.Contains("之后新开"), game.Id + " rules explain same-game and post-purchase boundaries");
            var viewportBounds = CenterBounds(scroll.viewport, card);
            foreach (string control in new[] { "Start", "Don't show", "Game records" })
                Require(viewportBounds.yMin >= CenterBounds((RectTransform)card.Find(control), card).yMax,
                    game.Id + " rule viewport does not overlap " + control);
            Capture("game-center-rules-" + game.Id + "-top.png", card);
            // Chinese fits the normal viewport. Exercise the same real ScrollRect in a
            // deliberately shorter test viewport, without changing font, text or controls.
            var originalViewportMin = scroll.viewport.offsetMin;
            if (scroll.content.rect.height <= scroll.viewport.rect.height + 1)
            {
                float reduction = scroll.viewport.rect.height - scroll.content.rect.height + 60;
                scroll.viewport.offsetMin += new Vector2(0, reduction);
                Canvas.ForceUpdateCanvases();
                Check(true, game.Id + " scrolling uses constrained viewport fixture; default Chinese already fits");
            }
            if (scroll.content.rect.height > scroll.viewport.rect.height + 1)
            {
                var e = Pointer(scroll.viewport); e.scrollDelta = new Vector2(0, -100);
                RequireTop(scroll.viewport, e); LcdSurface().OnScroll(e); yield return null; yield return null;
                Require(scroll.verticalNormalizedPosition < .01f, game.Id + " real LCD wheel reaches bottom");
            }
            text.ForceMeshUpdate();
            var last = text.textInfo.characterInfo.Take(text.textInfo.characterCount).Last(c => c.isVisible);
            var bottom = scroll.viewport.InverseTransformPoint(text.transform.TransformPoint(last.bottomLeft));
            var top = scroll.viewport.InverseTransformPoint(text.transform.TransformPoint(last.topRight));
            Require(bottom.y >= scroll.viewport.rect.yMin - 1 && top.y <= scroll.viewport.rect.yMax + 1,
                game.Id + " final rule characters visible at bottom of viewport");
            Capture("game-center-rules-" + game.Id + "-bottom.png", card);
            scroll.viewport.offsetMin = originalViewportMin;
            ClickNamed(card, "Start"); yield return null;
        }
        ctl.Sim.S.stage = 2;
        foreach (var game in games)
        {
            game.SyncBoardOwner();
            Require(Field<Button>(game, "recordsButton").gameObject.activeSelf, game.Id + " stage two offers records entry");
        }
        var first = games.First(g => g.Id == "gomoku");
        ClickNamed(hub.transform, "Nav gomoku"); yield return null;
        ClickNamed(Area(first), "Rules"); yield return null;
        var button = Field<Button>(first, "recordsButton");
        Require(button.gameObject.activeInHierarchy, "Stage two before first completion offers record navigation");
        Require(Field<TMP_Text>(first, "recordsLabel").text.Contains("先与自己的AI下完一局"), "Entry explains unfinished first-game prerequisite");
        double money = ctl.Host.Money; int rounds = ctl.Sim.S.boardRounds.Count;
        ClickRect((RectTransform)button.transform); yield return null; yield return null;
        wait = ToolWait(1); while (wait.MoveNext()) yield return wait.Current;
        Require(ctl.View.Tab == "tree", "Actual records button opens Tech tab");
        var detail = Field<XgTechNodeView>(ctl.View.Tree, "detailShown");
        Require(detail != null && detail.Covers("gamedata") && Field<bool>(ctl.View.Tree, "explanationPinned"), "Exact gamedata node details pinned");
        Require(!ctl.Sim.Has("gamedata") && ctl.Host.Money == money && ctl.Sim.S.boardRounds.Count == rounds,
            "Navigation neither buys recorder, spends money nor invents a completed round");
        Capture("game-center-gamedata-navigation.png", ctl.View.transform as RectTransform);
        // Adversarial stale callback: replace the owner without allowing a frame/UI resync.
        var original = ctl.Sim; var replacement = new XgSim(); replacement.S.stage = 2;
        var simProperty = typeof(XingGuangController).GetProperty("Sim");
        ctl.View.ShowTab("train"); string tabBefore = ctl.View.Tab;
        try
        {
            simProperty.SetValue(ctl, replacement); button.onClick.Invoke();
            Require(ctl.View.Tab == tabBefore && !replacement.Has("gamedata") && ctl.Host.Money == money,
                "Retained old-save records callback cannot navigate or purchase on new owner");
        }
        finally { simProperty.SetValue(ctl, original); }
        original.S.unlocked.Add("gamedata"); first.SyncBoardOwner();
        Require(!button.gameObject.activeSelf, "Owned recorder hides the entry (ownership fixture, not purchase test)");
        original.S.unlocked.Remove("gamedata"); original.S.stage = 6;
        ctl.Window.CloseWindow(); gameWindow.OpenWindow(); yield return null;
        foreach (var game in games)
        {
            game.SyncBoardOwner(); ClickNamed(hub.transform, "Nav " + game.Id); yield return null;
            var card = Field<RectTransform>(game, "ruleCard"); if (card.gameObject.activeInHierarchy) ClickNamed(card, "Start");
            ClickNamed(Area(game), "Private AI"); yield return null;
        }
        ClickNamed(hub.transform, "Nav home"); yield return null;
        InspectCenterHome("own-ai-stage6");
    }
}
#endif
