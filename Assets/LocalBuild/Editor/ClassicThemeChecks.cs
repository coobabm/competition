using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Use actual pages in SelectionIsolationInteractionChecks. Capture after a rendered frame separately.
public static class ClassicThemeChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Get(object page, string name) => page.GetType().GetField(name, Flags).GetValue(page);
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static Rect Bounds(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    public static void AssertLayoutAndPrediction(XgLabelPage page)
    {
        var workspace=(RectTransform)Get(page,"workspace");
        var viewport=(RectTransform)Get(page,"bodyViewport");
        var content=(RectTransform)Get(page,"body");
        var toggle=LingGuangV05.Desktop.Zhongbao.ZhongbaoSkin.Frame((XgBtn)Get(page,"gameToggle"));
        var history=(RectTransform)workspace.Find("Recent");var streak=(RectTransform)workspace.Find("StreakEnergy");
        Vector2 original=workspace.offsetMax;
        try
        {
            foreach(float shrink in new[]{0f,80f})
            {
                workspace.offsetMax=new Vector2(original.x,original.y-shrink);
                workspace.ForceUpdateRectTransforms();viewport.ForceUpdateRectTransforms();page.Refresh();
                content.ForceUpdateRectTransforms();
                Need(!Bounds(history).Overlaps(Bounds(streak)),"History overlaps streak row");
                Need(!Bounds(history).Overlaps(Bounds(toggle))&&!Bounds(streak).Overlaps(Bounds(toggle)),"Mode switch overlaps footer indicators");
                Need(!Bounds(viewport).Overlaps(Bounds(history))&&!Bounds(viewport).Overlaps(Bounds(toggle)),"Scrollable card viewport overlaps footer");
                Need(content.rect.height>=530&&content.rect.height>=viewport.rect.height-.1f,"Short windows must scroll instead of overflowing the footer");
                Need(viewport.GetComponent<RectMask2D>()!=null&&viewport.GetComponent<ScrollRect>().content==content,"Card overflow is not clipped and scrollable");
            }
        }
        finally{workspace.offsetMax=original;workspace.ForceUpdateRectTransforms();page.Refresh();}
        var sim=(XgSim)typeof(XgPage).GetProperty("Sim",Flags).GetValue(page);
        var card=(XgCard)typeof(XgLabelPage).GetProperty("CurrentCard",Flags).GetValue(page);
        bool work=(bool)typeof(XgLabelPage).GetProperty("WorkContext",Flags).GetValue(page);
        var prediction=(TMP_Text)Get(page,"suggestion");
        if(!work&&sim.ReviewCard(card.dataset)==null&&sim.TeachingGuess(card,out bool guess))
        {
            Need((bool)Get(page,"shownGuessValid")&&(bool)Get(page,"shownGuess")==guess,"Classic view does not show the actual board prediction");
            Need(prediction.text.Contains(sim.AiDisplayName)&&prediction.text.Contains(XgUi.T(guess?"是":"否",guess?"Yes":"No")),"Actual prediction disappeared from classic view");
            Need(prediction.text.Contains(LingGuangV05.Runtime.AiContentNotice.Footer(XgUi.En)),"AI content disclosure missing");
            if(XgSim.DirectTruthJudgment(card))Need(!(bool)Get(page,"shownTeachingEvaluation"),"Prediction display changed statement-answer semantics");
        }
        if(work)Need(string.IsNullOrEmpty(prediction.text),"Company desk invented an AI prediction");
        var companion=page.root.GetComponentInChildren<XgTeachCompanion>(true);
        if(companion!=null&&!work)
        {
            var title=(TMP_Text)Get(companion,"title");var counts=(TMP_Text)Get(companion,"next");
            var response=(TMP_Text)Get(companion,"response");
            Need(title.text==XgUi.T("教学记录","Teaching record"),"Companion still displays explanation heading");
            Need(counts.text.Contains(sim.S.handCorrect.ToString())&&counts.text.Contains(sim.S.handWrong.ToString()),"Companion lost actual correct/wrong counts");
            Need(!response.text.Contains("练完")&&!response.text.Contains("After training"),"Companion still repeats guidance prose");
            Need(companion.transform.Find("LearningCells").childCount==32,"Companion concept cells were removed");
            Need(((RectTransform)companion.transform).rect.height<=410.1f,"Companion retains obsolete explanation whitespace");
        }
        Debug.Log("CLASSIC_LAYOUT_PREDICTION_PASS");
    }
    public static void Assert(XgLabelPage page, bool workContext, bool visualCard)
    {
        var card = (XgCard)typeof(XgLabelPage).GetProperty("CurrentCard", Flags).GetValue(page);
        var id = card.id;
        page.Refresh();
        Need(((XgCard)typeof(XgLabelPage).GetProperty("CurrentCard", Flags).GetValue(page)).id == id, "Theme refresh replaced current question");
        var workspace = (RectTransform)Get(page, "workspace");
        var inner = workspace.Find("Inner").GetComponent<Image>();
        Need(inner.color == (workContext ? Color.white : XgDark.Card), "Workspace context colour incorrect");
        var paper = (Image)Get(page, "paperImage");
        var plain = (TMP_Text)Get(page, "text");
        // The fixture must choose a non-gold, settled card so transient gold/wrong feedback is not mistaken for the theme.
        Need(!card.gold && (float)Get(page,"redTimer") <= 0, "Choose a non-gold card with settled feedback");
        if(visualCard)Need(((Graphic)Get(page,"digit")).gameObject.activeSelf,"Visual fixture must render the digit graphic before checking its paper");
        Need(paper.color == (workContext || visualCard ? XgPalette.Paper : XgDark.Paper), "Content paper colour incorrect: dataset="+card.dataset+", actual="+paper.color+", visual="+visualCard);
        if (!visualCard) Need(plain.color == (workContext ? XgPalette.Ink : XgDark.Ink), "Plain text has wrong contrast");
        var yes = (XgBtn)Get(page, "yes"); var no = (XgBtn)Get(page, "no");
        Need(yes.image.color == (workContext ? Color.white : XgDark.Button) && no.image.color == (workContext ? Color.white : XgDark.Button), "Answer button fills leaked across contexts");
        Need(yes.label.color == (workContext ? LingGuangV05.Desktop.Zhongbao.ZhongbaoSkin.Ink : XgDark.Ink), "Answer text contrast incorrect");
        if (visualCard)
        {
            var digit = (Graphic)Get(page, "digit");
            if (digit.gameObject.activeSelf) Need(digit.color == new Color32(30,34,52,255), "Handwritten digit ink changed");
        }
        AssertLayoutAndPrediction(page);
        Debug.Log("CLASSIC_THEME_CHECK_PASS: " + (workContext ? "crowd-light" : visualCard ? "teach-visual-paper" : "teach-text-dark"));
    }
}
