using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 错题: the training map's area can show the last exam's wrong cards instead, drawn the way the 标注台 draws them
    /// (a handwritten digit, a captcha icon, a face, a Go position, or the text), each with what the model said, and
    /// the line that names what most of the mistakes share.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        public const int MistakeTiles = 4;

        sealed class MistakeTile
        {
            public RectTransform root;
            public XgDigitGraphic digit;
            public XgCaptchaGraphic captcha;
            public XgFaceGraphic face;
            public XgGoGraphic go;
            public TMP_Text text, caption;
        }

        XgBtn mapTab, mistakesTab;
        RectTransform mistakesArea;
        TMP_Text mistakesEmpty;
        readonly List<MistakeTile> mistakeTiles = new List<MistakeTile>();
        bool showMistakes;

        void BuildMistakes()
        {
            mapTab = ui.Button(diagnosticArea, "", () => { showMistakes = false; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 12);
            mapTab.rt.anchorMin = mapTab.rt.anchorMax = new Vector2(1, 1);
            mapTab.rt.offsetMin = new Vector2(-196, -21); mapTab.rt.offsetMax = new Vector2(-100, -1);
            mistakesTab = ui.Button(diagnosticArea, "", () => { showMistakes = true; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 12);
            mistakesTab.rt.anchorMin = mistakesTab.rt.anchorMax = new Vector2(1, 1);
            mistakesTab.rt.offsetMin = new Vector2(-96, -21); mistakesTab.rt.offsetMax = new Vector2(-2, -1);
            UiTip.Add(mistakesTab.rt, "上一轮考试答错的题，原样摆出来。错题的共同点就是线索：总把 7 认成 1？挪了位置就认不出？线索一远就答错？",
                "The cards the last exam got wrong, as they are. What they share is the clue: always reading 7 as 1? Lost once a digit moves? Wrong whenever the clue is far back?");

            mistakesArea = Rect("Mistakes", diagnosticArea, Vector2.zero, Vector2.one, new Vector2(3, 20), new Vector2(-3, -25));
            mistakesEmpty = ui.Text(Rect("Empty", mistakesArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 13, XgPalette.Muted, TextAlignmentOptions.Center);
            for (int i = 0; i < MistakeTiles; i++)
            {
                var t = new MistakeTile();
                t.root = Rect("Mistake" + i, mistakesArea, new Vector2(i / (float)MistakeTiles, 0), new Vector2((i + 1f) / MistakeTiles, 1), new Vector2(3, 0), new Vector2(-3, 0));
                Panel(t.root, Color.white).raycastTarget = false;
                var picture = Rect("Picture", t.root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-34, -72), new Vector2(34, -4));
                t.digit = Rect("Digit", picture, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<XgDigitGraphic>();
                t.digit.color = new Color32(30, 34, 52, 255);
                t.captcha = Rect("Captcha", picture, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<XgCaptchaGraphic>();
                t.face = Rect("Face", picture, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<XgFaceGraphic>();
                t.go = Rect("Go", picture, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<XgGoGraphic>();
                t.digit.raycastTarget = t.captcha.raycastTarget = t.face.raycastTarget = t.go.raycastTarget = false;
                t.text = ui.Text(Rect("Text", t.root, new Vector2(0, .36f), Vector2.one, new Vector2(6, 2), new Vector2(-6, -4)), "", 12, XgPalette.Ink, TextAlignmentOptions.Center);
                t.text.enableAutoSizing = true; t.text.fontSizeMin = 8; t.text.fontSizeMax = 13;
                t.caption = ui.Text(Rect("Caption", t.root, Vector2.zero, new Vector2(1, .36f), new Vector2(4, 2), new Vector2(-4, -2)), "", 11, XgPalette.Bad, TextAlignmentOptions.Center);
                t.caption.enableAutoSizing = true; t.caption.fontSizeMin = 8; t.caption.fontSizeMax = 12;
                mistakeTiles.Add(t);
            }
            mistakesArea.gameObject.SetActive(false);
        }

        /// <summary>The two tabs, and the mistakes in place of the map when that tab is on. True when the mistakes are shown.</summary>
        bool RefreshMistakes(XgRun run)
        {
            bool fresh = run.mistakeDataset == run.dataset;
            int wrong = fresh ? run.mistakeCount : 0;
            mapTab.Set(T("训练图式", "Training map"), true, showMistakes ? XgPalette.Button : XgPalette.Accent, showMistakes ? XgPalette.Ink : Color.white);
            mistakesTab.Set(T("错题 ", "Mistakes ") + wrong, true, showMistakes ? XgPalette.Accent : XgPalette.Button, showMistakes ? Color.white : XgPalette.Ink);
            mistakesArea.gameObject.SetActive(showMistakes);
            diagnostic.gameObject.SetActive(!showMistakes);
            if (!showMistakes) return false;
            foreach (var p in previousDiagnostics) p.gameObject.SetActive(false);

            diagnosticTitle.text = T("错题 · 上一轮考试", "Mistakes · last exam");
            var shown = Sim.ShownMistakes(run, mistakeTiles.Count);
            mistakesEmpty.text = shown.Count > 0 ? "" : fresh && run.examCount > 0 ? T("上一轮考试全答对了。", "The last exam had no mistakes.") : T("练一轮，这里会摆出考错的题。", "Train an epoch: the wrong exam cards show here.");
            for (int i = 0; i < mistakeTiles.Count; i++)
            {
                var t = mistakeTiles[i];
                t.root.gameObject.SetActive(i < shown.Count);
                if (i >= shown.Count) continue;
                var m = shown[i]; var s = m.card.source;
                bool digit = m.dataset == "mnist" && s != null;
                bool captcha = (m.dataset == "cifar" || m.dataset == "imagenet") && s != null;
                bool face = m.dataset == "meme" && s != null;
                bool go = m.dataset == "go" && s != null;
                bool picture = digit || captcha || face || go;
                t.digit.gameObject.SetActive(digit); t.captcha.gameObject.SetActive(captcha);
                t.face.gameObject.SetActive(face); t.go.gameObject.SetActive(go);
                if (digit) t.digit.Show(s.digit, s.seed, s.level);
                if (captcha) t.captcha.Show(s.digit, s.seed, s.level);
                if (face) t.face.Show(s.digit, s.seed);
                if (go) t.go.Show(s.line, s.digit, s.asked);
                t.text.gameObject.SetActive(!picture);
                if (!picture) t.text.text = Sim.CardText(m.card, m.dataset);
                t.caption.text = digit ? Sim.MistakeLine(m) : (picture ? Sim.CardText(m.card, m.dataset) + "\n" : "") + Sim.MistakeAnswer(m);
            }
            string pattern = fresh ? Sim.MistakePattern(run) : null;
            diagnosticNote.text = pattern ?? (fresh && run.examCount > 0
                ? T("上一轮考试 " + run.examCount + " 题，错 " + run.mistakeCount + " 题。", "Last exam: " + run.mistakeCount + " wrong of " + run.examCount + ".")
                : "");
            return true;
        }
    }
}
