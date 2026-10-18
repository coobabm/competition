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
        Need(paper.color == (workContext || visualCard ? XgPalette.Paper : XgDark.Paper), "Content paper colour incorrect");
        if (!visualCard) Need(plain.color == (workContext ? XgPalette.Ink : XgDark.Ink), "Plain text has wrong contrast");
        var yes = (XgBtn)Get(page, "yes"); var no = (XgBtn)Get(page, "no");
        Need(yes.image.color == (workContext ? Color.white : XgDark.Button) && no.image.color == (workContext ? Color.white : XgDark.Button), "Answer button fills leaked across contexts");
        Need(yes.label.color == (workContext ? LingGuangV05.Desktop.Zhongbao.ZhongbaoSkin.Ink : XgDark.Ink), "Answer text contrast incorrect");
        if (visualCard)
        {
            var digit = (Graphic)Get(page, "digit");
            if (digit.gameObject.activeSelf) Need(digit.color == new Color32(30,34,52,255), "Handwritten digit ink changed");
        }
        Debug.Log("CLASSIC_THEME_CHECK_PASS: " + (workContext ? "crowd-light" : visualCard ? "teach-visual-paper" : "teach-text-dark"));
    }
}
