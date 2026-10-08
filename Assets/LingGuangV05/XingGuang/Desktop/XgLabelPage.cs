using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
using AppNames = LingGuangV05.Core.AppNames;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 标注台: one desk per dataset, a yes/no card each. Right answers pay (× combo, ×3 on 前方高能) and add a sample;
    /// wrong ones pay nothing, break the combo and are discarded. Picture desks draw their cards in code.
    /// </summary>
    public sealed partial class XgLabelPage : XgPage
    {
        // Layout (the approved mockup): 任务大厅 | workspace | right column, side by side in the page area.
        const float HallW = 252, SideW = 300, ColumnGap = 10;

        RectTransform workspace, body, timerBar, timerFill;
        TMP_Text wkTitle, wkRule, qno;
        RectTransform confidenceRoot, confidenceFill, coopPanel, feedRoot, captionFocus;
        UnityEngine.UI.Slider threshold;
        RectTransform previewRoot, previewMemoryRoot, previewMemoryFill;
        XgLabelPreviewGraphic previewGraphic;
        UnityEngine.UI.ScrollRect previewSourceScroll, previewCandidateScroll;
        TMP_Text previewHeading, previewSource, previewCandidate, previewMemoryText;
        long previewId = -1;
        bool previewEnglish;
        XgSim previewOwner;
        float previewReadTime;
        TMP_Text thresholdText, coopStats, brainStatus, specialInfo;
        // 摆渡众包 quality control: the credit chip (right column) and the frozen-account banner (over the feed strip).
        RectTransform platformChip, frozenBanner;
        Image platformImage;
        TMP_Text platformText, frozenText;
        XgBtn appealBtn, recheckBtn;
        XgCaptchaCard captchaCard;
        bool wasFrozen, wasPaused, wasCaptcha;
        readonly List<TMP_Text> feedTexts = new List<TMP_Text>();
        readonly List<RectTransform> feedRects = new List<RectTransform>();
        readonly List<UnityEngine.UI.Image> feedImages = new List<UnityEngine.UI.Image>();
        readonly List<long> feedIds = new List<long>();
        readonly List<float> feedAges = new List<float>();
        sealed class AuditFlight
        {
            public RectTransform rect;
            public TMP_Text label;
            public Vector2 from, to;
            public float age;
            public bool active;
        }
        readonly List<AuditFlight> auditFlights = new List<AuditFlight>();
        long displayedId, lastFeedId;
        XgSim displayedSim, commentOwner;
        int displayedMode;
        float translationTime;
        int translationFadeCount = -1;
        bool refreshingThreshold;
        /// <summary>The desk ids of the hall, open ones first (see <see cref="TabDesks"/>).</summary>
        readonly List<string> deskIds = new List<string>();
        /// <summary>How many of <see cref="deskIds"/> are open: the open desks come first, the locked ones after them.</summary>
        int openTabs;
        XgDigitGraphic digit;
        XgCaptchaGraphic captchaA, captchaB;
        XgFaceGraphic face;
        XgGoGraphic go;
        TMP_Text meta, poem, text, caption, question, suggestion, feedback, steps;
        XgBtn yes, no, toTrain;
        XgBtn[] amountBtns;
        XgShopRow auto, pack;
        XgGlow paperGlow;
        Image paperImage;
        float feedbackTimer, redTimer;
        long commentCardId = -1;
        string comment;

        public RectTransform Paper => paper;
        RectTransform paper;
        /// <summary>Hand answers given on this page (read by the resident spider, which watches the labelling).</summary>
        public int AnswerCount { get; private set; }
        /// <summary>The latest answer went against the checkpoint's guess shown on the card.</summary>
        public bool LastAnswerDisagreed { get; private set; }
        bool shownGuessValid, shownGuess;
        /// <summary>The checkpoint's guess shown on the card (the spider presses this button when it answers).</summary>
        public bool HasShownGuess => shownGuessValid && Mode == 0;
        public bool ShownGuessYes => shownGuess;
        /// <summary>The 是 / 否 (算术: 对 / 错) buttons, for the spider's leg to reach.</summary>
        public RectTransform YesButton => yes != null ? yes.rt : null;
        public RectTransform NoButton => no != null ? no.rt : null;
        /// <summary>Id of the card on screen now (the spider decides once per card).</summary>
        public long ShownCardId => Mode == 0 && Sim != null ? CurrentCard.id : -1;

        /// <summary>
        /// The resident spider presses a button itself (XgSim.SpiderAnswer: the checkpoint's guess, settled like an
        /// automatic label: it never costs money). The page shows a short note and the next card.
        /// </summary>
        public XgSpiderAnswer SpiderPress()
        {
            if (Sim == null || Mode != 0) return default;
            var r = Sim.SpiderAnswer(Desk, Host);
            if (!r.accepted) return r;
            var button = r.yes ? yes.rt : no.rt;
            Fx.Knock(button, .12f);
            feedback.color = r.correct ? XgPalette.Good : XgPalette.Bad;
            feedback.text = (r.correct ? "<color=#2F9E44>" : "<color=#D63031>") + T("它替你按了「" + YesNoWord(r.yes) + "」", "It pressed \"" + YesNoWord(r.yes) + "\" for you")
                + (r.correct ? "  +¥" + N(r.pay, "0.00") : T("，按错了（不扣钱）", ", wrongly (it costs nothing)")) + "</color>";
            feedbackTimer = 3f;
            if (r.correct) Fx.Float(Fx.At(button, new Vector2(0, 40)), "+¥" + N(r.pay, "0.00"), XgPalette.Money, 20);
            Fx.Play(r.correct ? XgJuice.Sfx.Id.Tick : XgJuice.Sfx.Id.Clack, 1.2f, .6f);
            Remember(r.correct ? HistoryMark.SpiderRight : HistoryMark.SpiderWrong);
            SpiderReacts(r.correct, false);
            Refresh();
            return r;
        }
        string Desk => Sim.S.desk;
        XgCard CurrentCard => Sim.ReviewCard(Desk) ?? Sim.Card(Desk);
        bool EndingReady => host.Controller != null && host.Controller.EndingQuestionReady;
        // 0 ordinary/review, 1 research experiment, 2 final question, 3 chapter complete.
        int Mode => Sim.LabelPresentationMode(EndingReady);
        static string Safe(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
        static string VisibleText(string zh, string en) => En && !string.IsNullOrWhiteSpace(en) ? en : zh ?? "";
        static bool HasPreviewSurface(XgCard card) => !string.IsNullOrEmpty(card.patternA) ||
            ((card.kind == "long" || card.kind == "translation" || card.kind == "order" && card.bottleneckPreview) &&
             (!string.IsNullOrEmpty(card.sourceText) || !string.IsNullOrEmpty(card.candidateText)));

        /// <summary>The 算术 desk is true/false: its buttons say 对 / 错, every other desk 是 / 否.</summary>
        bool TrueFalseDesk => XgCatalog.Desk(Desk)?.kind == XgDeskKind.Arith;
        string YesNoWord(bool yesButton) => TrueFalseDesk ? (yesButton ? T("对", "True") : T("错", "False")) : (yesButton ? T("是", "Yes") : T("否", "No"));

        public override void Build(RectTransform area)
        {
            root = Rect("label", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            BuildHall(root);

            // ── the workspace ──
            workspace = ZhongbaoSkin.Box(ui, root, "Workspace", Vector2.zero, Vector2.one, new Vector2(HallW + ColumnGap, 0), new Vector2(-(SideW + ColumnGap), 0));
            ZhongbaoSkin.Header(ui, workspace, "标注台", "Labelling desk", out wkTitle, out wkRule);
            UiTip.Add(wkRule, () => Sim == null ? "" : PayTip());
            body = Rect("Body", workspace, Vector2.zero, Vector2.one, new Vector2(1, 38), new Vector2(-1, -35));
            BuildFeed(body);
            BuildFrozenBanner(body);
            meta = ui.Text(Rect("Meta", body, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -58), new Vector2(-96, -36)), "", 14, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            meta.textWrappingMode = TextWrappingModes.NoWrap; meta.overflowMode = TextOverflowModes.Ellipsis;
            qno = ui.Text(Rect("QuestionNo", body, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-94, -58), new Vector2(-14, -36)), "", 13, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);

            paper = Rect("Paper", body, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-224, -276), new Vector2(224, -66));
            paperGlow = Glow(paper, XgPalette.Gold, 6);
            paperImage = Panel(paper, XgPalette.Paper);
            digit = Square<XgDigitGraphic>("Digit", 214);
            digit.color = new Color32(30, 34, 52, 255);
            poem = ui.Text(Rect("Poem", paper, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10)), "", 64, XgPalette.Ink, TextAlignmentOptions.Center);
            poem.textWrappingMode = TextWrappingModes.NoWrap;
            poem.enableAutoSizing = true; poem.fontSizeMin = 28; poem.fontSizeMax = 52; poem.lineSpacing = 6;
            text = ui.Text(Rect("Text", paper, Vector2.zero, Vector2.one, new Vector2(22, 12), new Vector2(-22, -12)), "", 26, XgPalette.Ink, TextAlignmentOptions.Center);
            text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 30;
            captchaA = Rect("CaptchaA", paper, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-107, -107), new Vector2(107, 107)).gameObject.AddComponent<XgCaptchaGraphic>();
            captchaB = Rect("CaptchaB", paper, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(8, -100), new Vector2(208, 100)).gameObject.AddComponent<XgCaptchaGraphic>();
            captchaA.raycastTarget = captchaB.raycastTarget = false;
            face = Rect("Face", paper, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-88, -184), new Vector2(88, -8)).gameObject.AddComponent<XgFaceGraphic>();
            face.raycastTarget = false;
            captionFocus = Rect("ReportedAttentionRegion", face.transform, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Panel(captionFocus, new Color(1, .72f, .1f, .22f)).raycastTarget = false;
            var outline = captionFocus.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1, .65f, .05f, .9f); outline.effectDistance = new Vector2(2, 2);
            captionFocus.gameObject.SetActive(false);
            caption = ui.Text(Rect("Caption", paper, Vector2.zero, new Vector2(1, 0), new Vector2(10, 6), new Vector2(-10, 48)), "", 24, XgPalette.Ink, TextAlignmentOptions.Center);
            caption.fontStyle = FontStyles.Bold;
            go = Square<XgGoGraphic>("Go", 226);
            BuildPreviewSurface();

            timerBar = Rect("Timer", body, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-224, -288), new Vector2(224, -282));
            timerFill = Bar(timerBar, "Fill", new Color(0, 0, 0, .08f), XgPalette.Gold);

            question = ui.Text(Strip("Question", body, 292, 38), "", 24, XgPalette.Ink, TextAlignmentOptions.Center);
            question.fontStyle = FontStyles.Bold;
            question.enableAutoSizing = true; question.fontSizeMin = 14; question.fontSizeMax = 24;
            suggestion = ui.Text(Strip("Suggestion", body, 332, 44), "", 15, XgPalette.Accent, TextAlignmentOptions.Top);
            confidenceRoot = Strip("Confidence", body, 378, 4, 60, 60);
            confidenceFill = Bar(confidenceRoot, "Fill", XgPalette.Line, XgPalette.Accent);
            yes = ZhongbaoSkin.Outlined(ui, body, "", () => OnAnswer(true), 24, new Color32(203, 210, 230, 255));
            no = ZhongbaoSkin.Outlined(ui, body, "", () => OnAnswer(false), 24, new Color32(203, 210, 230, 255));
            var yesFrame = ZhongbaoSkin.Frame(yes); var noFrame = ZhongbaoSkin.Frame(no);
            yesFrame.name = "AnswerYes"; noFrame.name = "AnswerNo";
            yesFrame.anchorMin = yesFrame.anchorMax = new Vector2(.5f, 1); yesFrame.offsetMin = new Vector2(-214, -454); yesFrame.offsetMax = new Vector2(-8, -394);
            noFrame.anchorMin = noFrame.anchorMax = new Vector2(.5f, 1); noFrame.offsetMin = new Vector2(8, -454); noFrame.offsetMax = new Vector2(214, -394);
            feedback = ui.Text(Rect("Feedback", body, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -516), new Vector2(-16, -460)), "", 15, XgPalette.Good, TextAlignmentOptions.Top);
            // The desk's info line: samples, difficulty, hot word, the next step.
            var infoBack = Rect("InfoBox", body, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -582), new Vector2(-14, -520));
            Panel(infoBack, ZhongbaoSkin.Page).raycastTarget = false;
            steps = ui.Text(Rect("Info", infoBack, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2)), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            steps.enableAutoSizing = true; steps.fontSizeMin = 10; steps.fontSizeMax = 13;
            // Built after the card and the answer buttons so it covers them while a captcha waits.
            captchaCard = new XgCaptchaCard(ui, body, 30, 428, () => Sim, OnCaptchaAnswered);
            BuildHistoryStrip();

            BuildSide(root);

            UiTip.Add(qno, () => Sim == null ? "" : HandStats());
            UiTip.Add(yes.rt, () => TrueFalseDesk ? Lang.T("算式是对的，就点「对」。答对 +1 连击；点错要扣钱，还会断连击。") : Lang.T("题面说得对，就点「是」。答对 +1 连击。"));
            UiTip.Add(no.rt, () => TrueFalseDesk ? Lang.T("算式是错的，就点「错」。答对 +1 连击；点错要扣钱，还会断连击。") : Lang.T("题面说得不对，就点「否」。答错连击清零。"));
            UiTip.Add(auto.Root, () => Sim != null && Sim.AutoLabelHidden ? Lang.T("？？？\n也许有更省力的办法……")
                : Lang.T("自动答题：模型替你答题，每秒都有收入。\n需要任一模型的准确率先达到 60%；只有检查点达到 60% 的桌才会替你答。\n交出去的题会被摆渡众包抽检，错了要罚款，错太多会被举报冻结。"));
            UiTip.Add(pack.Root, SourceTip);
            for (int i = 0; i < amountBtns.Length; i++) UiTip.Add(amountBtns[i].rt, "一次买几级：×1、×10，或者钱够的最大数量。", "How many levels per purchase: ×1, ×10, or as many as you can afford.");
            builtEnglish = En;
            RebuildTabs();
        }

        /// <summary>
        /// The 唐诗 card: title and poet, the whole couplet, and only the asked character blanked out. One character
        /// alone says nothing about which poem it is; the rest of the line and its partner line make it guessable.
        /// </summary>
        string PoemText(XgCard card)
        {
            int at = Math.Max(0, Math.Min(card.line.Length - 1, card.shown));
            string asked = card.line.Substring(0, at) + "<color=#E86E14><u>＿</u></color>" + card.line.Substring(at + 1);
            var info = XgCatalog.PoemOf(card.line, out string partner, out bool lineFirst);
            if (info == null || partner.Length == 0) return asked;
            string head = "<size=22><color=#8A93A6>《" + T(info.title, info.titleEn) + "》 · " + T(info.author, info.authorEn) + "</color></size>\n";
            string other = "<color=#5A6378>" + partner + "</color>";
            return head + (lineFirst ? asked + "，\n" + other + "。" : other + "，\n" + asked + "。");
        }

        string DeskHelp(string id)
        {
            var d = XgCatalog.Dataset(id); var desk = XgCatalog.Desk(id);
            string what;
            switch (id)
            {
                case "mnist": what = Lang.T("看手写数字，判断是不是问的那个数。"); break;
                case "poems": what = Lang.T("看诗句，判断空格里是不是问的那个字。"); break;
                case "arith": what = T("算术题：看一道写好答案的算式（加减乘除、百分数、分数……），判断它对不对。点错要扣钱；报酬比别的桌低，每条只算半条总样本。", "Sums: read an equation that already shows its answer (+ − × ÷, percentages, fractions…) and judge whether it is right. A wrong pick costs money; it pays less than the other desks and each sample counts half toward the total."); break;
                case "logic": what = Lang.T("逻辑和推理题。要动脑子，所以报酬最高。"); break;
                case "sense": what = T("常识判断：一句人人都知道的话，说得对就点「是」，不对点「否」。最简单，也最能教它分清是和否；点错只断连击，不扣钱。", "Common sense: a plain statement everybody knows. Press Yes if it is right, No if not. The simplest desk, and the one that teaches it yes from no; a miss only breaks the combo."); break;
                case "danmu": what = Lang.T("判断弹幕是不是在夸。"); break;
                case "spam": what = Lang.T("判断短信是不是垃圾或诈骗。"); break;
                default: what = d != null ? T(d.name, d.nameEn) : id; break;
            }
            double pay = desk?.pay ?? 1;
            return what + (Math.Abs(pay - 1) > 1e-9 ? Lang.T("\n报酬 ×") + N(pay, "0.0#") : "") + Lang.T("\n标得越多，这类数据越多，对应的模型越好练。");
        }

        int Amount(int affordable) { return host.BuyAmount == int.MaxValue ? Math.Max(1, affordable) : host.BuyAmount; }

        void BuyRaise()
        {
            int before = Sim.RaiseLevel;
            string oldTitle = XgCatalog.RaiseTitle(before, En);
            int bought = Sim.BuyRaise(Host, Amount(Sim.AffordableRaises(Host)));
            if (bought == 0) { Fx.Knock(raiseBtn.rt, .05f, new Vector2(8, 0), .25f); Fx.Play(XgJuice.Sfx.Id.Thud, 1.3f, .6f); return; }
            string title = XgCatalog.RaiseTitle(Sim.RaiseLevel, En);
            Fx.Knock(raiseBlock, .04f, new Vector2(0, 4), .3f);
            Fx.Knock(raiseBtn.rt, .15f);
            Fx.Shockwave(Fx.At(raiseBtn.rt), XgPalette.Money, 140, .3f, 6);
            Fx.Burst(Fx.At(raiseBtn.rt), 22, XgPalette.Gold, XgJuice.Shape.Yen, 260);
            Fx.Float(Fx.At(raiseBlock, new Vector2(-40, 30)), Lang.T("加薪 ×") + N(Sim.RaiseMultiplier, "0") + (bought > 1 ? "  (+" + bought + ")" : ""), XgPalette.Money, 24);
            Fx.Play(XgJuice.Sfx.Id.Unlock, 1 + Sim.RaiseLevel * .03f);
            Fx.Play(XgJuice.Sfx.Id.Coin, 1, .6f);
            if (title != oldTitle)
            {
                Fx.Flash(Color.white, .12f, .5f);
                Fx.Shake(5, .25f);
                Fx.Float(Fx.At(paper, new Vector2(0, 40)), Lang.T("升职：") + title, XgPalette.Gold, 32, 80, 1.3f, 1.6f);
                Fx.Burst(Fx.At(paper), 40, Color.white, XgJuice.Shape.Confetti, 420);
                Fx.Play(XgJuice.Sfx.Id.Fanfare, 1 + Sim.RaiseLevel * .02f, .8f);
            }
            host.Refresh(true);
        }

        void BuyAuto()
        {
            int bought = 0, want = host.BuyAmount == int.MaxValue ? XgCatalog.AutoMaxLevel : host.BuyAmount;
            while (bought < want && Sim.CanBuyAuto(Desk, out _) && Host.Money + 1e-9 >= XgCatalog.AutoCost(Sim.AutoLevel(Desk)) && Sim.BuyAuto(Desk, Host)) bought++;
            if (bought == 0) { Sim.BuyAuto(Desk, Host); auto.Deny(Fx); return; }
            auto.Celebrate(Fx, Lang.T("自动答题 Lv ") + Sim.AutoLevel(Desk), XgPalette.Accent, Sim.AutoLevel(Desk));
            host.Refresh(true);
        }

        void BuyPack()
        {
            // Junk packs, story data and crowd tasks go through XgLabelPage.Data.cs; the public pack through the tree.
            if (BuySource()) return;
            var n = XgCatalog.Node(Desk + ".pack");
            if (n == null || !host.BuyNode(n, pack.Button.rt)) { pack.Deny(Fx); return; }
            host.Refresh(true);
        }

        T Square<T>(string name, float size) where T : MaskableGraphic
        {
            var g = Rect(name, paper, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-size / 2, -size / 2), new Vector2(size / 2, size / 2)).gameObject.AddComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        /// <summary>Every desk of the catalog: the open ones first, then the locked ones, so a new player sees what is coming. Each group by pay, highest first.</summary>
        List<XgDesk> TabDesks(out int open)
        {
            var list = new List<XgDesk>(Sim.OpenDesks());
            open = list.Count;
            foreach (var d in XgCatalog.Desks) if (!Sim.DeskOpen(d.id)) list.Add(d);
            SortByPay(list, 0, open);
            SortByPay(list, open, list.Count - open);
            return list;
        }

        /// <summary>A stable sort (catalog order breaks ties) of one stretch of the list by the desk's pay factor, highest first.</summary>
        static void SortByPay(List<XgDesk> list, int from, int count)
        {
            if (count < 2) return;
            var part = list.GetRange(from, count);
            var order = new List<(XgDesk d, int i)>();
            for (int i = 0; i < part.Count; i++) order.Add((part[i], i));
            order.Sort((a, b) => { int c = b.d.pay.CompareTo(a.d.pay); return c != 0 ? c : a.i.CompareTo(b.i); });
            for (int i = 0; i < order.Count; i++) list[from + i] = order[i].d;
        }

        /// <summary>A locked desk explains how it opens; it is never selected.</summary>
        string LockText(string id)
        {
            var desk = XgCatalog.Desk(id);
            string how = Sim.DeskConditionText(desk);
            return T("「" + desk.name + "」还没开放。", desk.nameEn + " is locked. ") + how;
        }

        void OnTab(string id)
        {
            if (Sim.DeskOpen(id)) { Select(id); return; }
            host.ShowToast(LockText(id), 4);
            Fx.Play(XgJuice.Sfx.Id.Thud);
        }

        void Select(string id)
        {
            if (id == Desk) return;
            Sim.SelectDesk(id);
            feedback.text = "";
            Fx.Play(XgJuice.Sfx.Id.Swoosh, 1.2f, .6f);
            Fx.Knock(paper, .05f, new Vector2(-20, 0), .25f);
            Refresh();
        }

        // ───────────── answering ─────────────

        void OnAnswer(bool answer)
        {
            if (displayedSim != Sim || displayedMode != Mode) { Refresh(); return; }
            if (Mode != 0) { AnswerSpecial(answer); return; }
            string desk = Desk;
            var card = CurrentCard;
            if (card.id != displayedId) { Refresh(); return; }
            bool queued = Sim.ReviewCard(desk) != null;
            string truthText = YesNoWord(card.truth);
            string why = T(card.why, card.whyEn);
            string source = card.source;
            var button = answer ? yes.rt : no.rt;
            var r = queued ? Sim.AnswerQueued(card.id, answer, Host) : Sim.Answer(desk, answer, Host);
            if (queued && !r.accepted) { Refresh(); return; }
            LastAnswerDisagreed = shownGuessValid && shownGuess != answer;
            AnswerCount++;
            Remember(r.correct ? HistoryMark.Right : HistoryMark.Wrong);
            SpiderWatches();
            Vector2 at = Fx.At(paper);
            if (r.correct)
            {
                feedback.color = XgPalette.Good;
                feedback.text = "<color=#2F9E44>✓ " + Lang.T("对了") + "  +¥" + N(r.pay, "0.00") + "</color>"
                    + (r.combo > 1 ? "  <color=#7A5AF8>" + Lang.T("连击 ×") + N(Sim.ComboMultiplier, "0.00") + "</color>" : "")
                    + (why.Length > 0 && card.question.Length > 0 ? "\n<size=13><color=#68748C>" + why + "</color></size>" : "");
                // A user log from the data flywheel was labelled: it counts as several samples.
                if (!queued && r.samples > 1) feedback.text += "  <color=#0F8C7E>" + Lang.T("用户日志 +") + r.samples + Lang.T(" 样本") + "</color>";
                feedbackTimer = 3f;
                Fx.Knock(button, .12f);
                Fx.Knock(paper, .04f, new Vector2(answer ? 26 : -26, 6), .22f);
                Fx.Float(Fx.At(button, new Vector2(0, 40)), "+¥" + N(r.pay, "0.00"), XgPalette.Money, 24 + Mathf.Min(10, r.combo * .3f));
                Fx.Burst(at, 6 + Mathf.Min(14, r.combo / 3), XgPalette.Gold, XgJuice.Shape.Yen, 240);
                Fx.Play(XgJuice.Sfx.Id.Ding, 1 + Mathf.Min(1, r.combo * .025f));
                if (r.bounty)
                {
                    Fx.Shockwave(at, new Color32(123, 47, 247, 255), 260, .4f, 12);
                    Fx.Float(at + new Vector2(0, 90), Lang.T("悬赏到手 ×") + N(XgSim.BountyMultiplier, "0"), new Color32(123, 47, 247, 255), 30);
                    Fx.Play(XgJuice.Sfx.Id.Coin);
                }
                if (r.gold)
                {
                    Fx.Shockwave(at, XgPalette.Gold, 260, .4f, 12);
                    Fx.Float(at + new Vector2(0, 90), T("前方高能 ×3", "Hype ×3"), XgPalette.Gold, 30);
                    Fx.Play(XgJuice.Sfx.Id.Coin);
                }
                if (r.corrected)
                {
                    Fx.Float(at + new Vector2(0, 72), Lang.T("纠错！连击 +2"), XgPalette.Accent, 28);
                    feedback.text += Lang.T("  · 纠错") + (r.samples == 2 ? Lang.T(" · 难例 +2 样本") : "");
                    Fx.Play(XgJuice.Sfx.Id.Unlock, 1.15f, .55f);
                }
                else if (r.trick)
                {
                    Fx.Float(at + new Vector2(0, 60), Lang.T("老司机！连击 +2"), new Color32(150, 90, 255, 255), 28);
                    Fx.Flash(new Color(.6f, .4f, 1f), .1f, .35f);
                }
                if (r.combo > 0 && r.combo % 10 == 0 && Array.IndexOf(XgCatalog.ComboTiers, r.combo) < 0)
                {
                    Fx.Flash(Color.white, .08f, .4f);
                    Fx.Float(Fx.At(host.ComboChip, new Vector2(-20, -50)), Lang.T("连击 ×") + N(Sim.ComboMultiplier, "0.00"), XgPalette.Gold, 22);
                }
            }
            else
            {
                feedback.color = XgPalette.Bad;
                feedback.text = "<color=#D63031>× " + (r.timeout ? Lang.T("超时了。") : "") + Lang.T("应该是「") + truthText
                    + (r.fine > 0 ? T("」。扣 ¥" + N(r.fine, "0.00") + "，连击清零，这条作废", "\". It cost ¥" + N(r.fine, "0.00") + ", the combo resets and the label is discarded")
                        : Lang.T("」。没有钱，连击清零，这条作废")) + "</color>"
                    + (why.Length > 0 ? "\n<size=14><color=#5B6478>" + why + "</color></size>" : "")
                    + (source.Length > 0 ? "\n<size=13><color=#8A94AA>" + Lang.T("出处：") + Safe(source) + "</color></size>" : "")
                    + (Sim.S.stage >= 5 && card.explanation.Length > 0 ? "\n<size=13><color=#4054C4>" + T(AppNames.AiZh + "：", AppNames.AiEn + ": ") + Safe(T(card.explanation, card.explanationEn)) + "</color></size>" : "");
                feedbackTimer = why.Length > 0 ? 7f : 3f;
                if (r.fine > 0) Fx.Float(Fx.At(button, new Vector2(0, 40)), "−¥" + N(r.fine, "0.00"), XgPalette.Bad, 26);
                Wrong(at);
            }
            Refresh();
        }

        void Wrong(Vector2 at)
        {
            redTimer = .35f;
            Fx.Knock(paper, .03f, new Vector2(-18, 0), .3f);
            Fx.Shake(3, .2f);
            Fx.HitStop(60);
            Fx.Play(XgJuice.Sfx.Id.Clack);
            Fx.Burst(at, 6, XgPalette.Bad, XgJuice.Shape.Square, 160);
        }

        public void OnTimeout()
        {
            Fx.Float(Fx.At(paper, new Vector2(0, 60)), Lang.T("超时！"), XgPalette.Bad, 30);
            feedback.color = XgPalette.Bad;
            feedback.text = "<color=#D63031>" + Lang.T("超时：没有钱，连击清零") + "</color>";
            feedbackTimer = 2.5f;
            Remember(HistoryMark.Wrong);
            Wrong(Fx.At(paper));
            Refresh();
        }

        public override void Tick(float dt)
        {
            if (feedbackTimer > 0) { feedbackTimer -= dt; if (feedbackTimer <= 0) feedback.text = ""; }
            if (redTimer > 0) redTimer -= dt;
            AnimateFeed(dt);
            if (bubbleTimer > 0) { bubbleTimer -= dt; if (bubbleTimer <= 0 && bubbleText != null) bubbleText.text = bubbleDefault; }
            TickKeys();
            if (Mode != displayedMode || displayedSim != Sim) { Refresh(); return; }
            if (Sim.QualityFrozen != wasFrozen || Sim.CaptchaPauseLeft > 0 != wasPaused || Sim.CaptchaPending != wasCaptcha) { Refresh(); return; }
            if ((wasFrozen || wasPaused) && Mode == 0) RefreshFrozenBanner();
            captchaCard.Tick(host.Visible && host.Tab == "label");
            if (Mode != 0) { timerBar.gameObject.SetActive(false); paperGlow.on = false; return; }
            var card = CurrentCard;
            if (card.id != displayedId) { Refresh(); return; }
            AnimateTranslation(card, dt);
            AnimateMemory(card, dt);
            paperImage.color = redTimer > 0 ? Color.Lerp(XgPalette.Paper, new Color32(255, 200, 200, 255), redTimer / .35f) : card.gold ? new Color32(255, 248, 220, 255) : XgPalette.Paper;
            bool timed = card.timeLimit > 0;
            timerBar.gameObject.SetActive(timed);
            if (timed)
            {
                float k = 1 - (float)(card.age / card.timeLimit);
                SetBar(timerFill, k);
                timerFill.GetComponent<Image>().color = k < .35f ? XgPalette.Bad : XgPalette.Gold;
            }
            paperGlow.on = card.gold || Sim.S.combo >= 5;
            paperGlow.color = card.gold ? XgPalette.Gold : XgPalette.Tiers[Mathf.Min(4, XgSim.TierOf(Sim.S.combo) + 1)] * new Color(1, 1, 1, .6f);
            paperGlow.speed = card.gold ? 12 : 5;
        }

        // ───────────── refresh ─────────────

        public override void Refresh()
        {
            if (root == null) return;
            if (displayedSim != Sim)
            {
                displayedId = 0; commentCardId = -1; commentOwner = null; comment = null; lastFeedId = 0;
                for (int i = 0; i < feedIds.Count; i++) { feedIds[i] = -1; feedAges[i] = 2; }
                displayedSim = Sim;
                // Rebinding can open or remove desks. Rebuild before using stale tab callbacks.
                var tabs = TabDesks(out int open);
                bool changed = open != openTabs || tabs.Count != deskIds.Count;
                for (int i = 0; !changed && i < tabs.Count; i++) changed = tabs[i].id != deskIds[i];
                if (changed) { RebuildTabs(); return; }
            }
            RelabelStatic();
            string desk = Desk;
            var info = XgCatalog.Desk(desk);
            var d = XgCatalog.Dataset(desk);
            bool special = Mode != 0;
            displayedSim = Sim; displayedMode = Mode;
            RefreshSpecialVisibility(special);
            RefreshFeed();
            RefreshHall(special);
            if (special) { RefreshSpecial(); LayoutSide(true, 0); return; }
            var card = CurrentCard;
            if (card.id != displayedId) { displayedId = card.id; translationTime = 0; translationFadeCount = -1; }
            var kind = info.kind;
            bool dedicatedPreview = HasPreviewSurface(card);
            bool textOverride = card.kind == "combo" || card.kind == "long" || card.kind == "attention" || card.kind == "translation" || card.kind == "order" && card.bottleneckPreview;
            digit.gameObject.SetActive(kind == XgDeskKind.Digit && !textOverride);
            poem.gameObject.SetActive(kind == XgDeskKind.Poem);
            text.gameObject.SetActive(textOverride || kind == XgDeskKind.Logic || kind == XgDeskKind.Arith || kind == XgDeskKind.Text);
            captchaA.gameObject.SetActive(kind == XgDeskKind.Captcha);
            captchaB.gameObject.SetActive(kind == XgDeskKind.Captcha && card.shown >= 0);
            face.gameObject.SetActive(kind == XgDeskKind.Meme);
            caption.gameObject.SetActive(kind == XgDeskKind.Meme);
            go.gameObject.SetActive(kind == XgDeskKind.Go);

            int level = card.level > 0 ? card.level : Sim.LevelOf(desk), max = XgSim.MaxLevelOf(desk);
            string stars = max > 1 ? "  <color=#E8A317>" + new string('★', Math.Min(level, max)) + "</color><color=#B4B8C2>" + new string('☆', Math.Max(0, max - level)) + "</color>" : "";
            wkTitle.text = T(info.name, info.nameEn);
            double handPay = Sim.ManualPayFor(desk, level);
            wkRule.text = T("每题 ¥", "¥") + N(handPay * (card.bounty ? XgSim.BountyMultiplier : 1), "0.00") + T("", " a card")
                + (info.fine > 0 ? " · " + T("点错 −¥", "a miss −¥") + N(Sim.HandFineFor(desk, level), "0.00") : "") + " · " + T("连击越高越多", "the higher the combo, the more");
            qno.text = T("第 " + (Sim.DayDone + 1) + " 题", "Card " + (Sim.DayDone + 1));
            string cat = card.category.Length > 0 ? T(card.category, card.categoryEn) : T(info.name, info.nameEn);
            meta.text = (card.gold ? "<color=#E0A000><b>" + T("前方高能 ×3", "HYPE ×3") + "</b></color>  " : "")
                + (card.bounty ? "<color=#7B2FF7><b>" + Lang.T("专家题悬赏 ×") + N(XgSim.BountyMultiplier, "0") + "</b></color>  " : "") + (Sim.InDuel && desk == "meme" ? "<color=#D63031><b>" + Lang.T("斗图中 ") + (XgSim.DuelLength - Sim.DuelLeft + 1) + "/" + XgSim.DuelLength + "</b></color>  " : "")
                + (Sim.ReviewCard(desk) != null ? "<color=#B36A00>" + Lang.T("待复核 · ") + "</color>" + QcTags(card) : "") + cat + stars;

            if (!dedicatedPreview) switch (kind)
            {
                case XgDeskKind.Digit:
                    digit.Show(card.digit, card.seed, card.level);
                    question.text = T("这是「" + card.asked + "」吗？", "Is this a " + card.asked + "?");
                    break;
                case XgDeskKind.Poem:
                    poem.text = PoemText(card);
                    question.text = T("下一个字是「" + card.askedChar + "」吗？", "Is the next character 「" + card.askedChar + "」?");
                    break;
                case XgDeskKind.Logic: case XgDeskKind.Arith:
                    text.text = T(card.question, card.questionEn);
                    // The sums are big and spaced like a calculator; the logic cards are sentences.
                    text.fontSizeMax = kind == XgDeskKind.Arith ? 44 : 30;
                    text.characterSpacing = kind == XgDeskKind.Arith ? 3 : 0;
                    question.text = kind == XgDeskKind.Arith ? T("这道算式对不对？点错扣钱", "Is this one right? A wrong pick costs money") : Lang.T("对吗？");
                    break;
                case XgDeskKind.Text:
                    // 常识判断 reads like the 算术 card: one big plain statement; the 2016 phrase desks keep their small type.
                    bool sense = desk == XgMemes.SenseDesk;
                    text.text = T(card.question, card.questionEn); text.fontSizeMax = sense ? 40 : 30; text.characterSpacing = sense ? 2 : 0;
                    question.text = T(XgMemes.Question(desk), XgMemes.QuestionEn(desk));
                    break;
                case XgDeskKind.Captcha:
                    bool two = card.shown >= 0;
                    var a = captchaA.rectTransform;
                    if (two) { a.offsetMin = new Vector2(-208, -100); a.offsetMax = new Vector2(-8, 100); } else { a.offsetMin = new Vector2(-107, -107); a.offsetMax = new Vector2(107, 107); }
                    captchaA.Show(card.digit, card.seed, card.level);
                    if (two) captchaB.Show(card.shown, card.seed + 7, card.level);
                    question.text = T(card.question, card.questionEn);
                    break;
                case XgDeskKind.Meme:
                    face.Show(card.digit, card.seed);
                    caption.text = card.kind == "caption" ? T(card.line, card.captionTextEn) : card.line;
                    question.text = T(card.question, card.questionEn);
                    break;
                case XgDeskKind.Go:
                    go.Show(card.line, card.digit, card.asked);
                    question.text = T(card.question, card.questionEn);
                    question.fontSize = 22;
                    break;
            }
            question.fontSizeMax = kind == XgDeskKind.Go ? 22 : 26;
            if (kind != XgDeskKind.Go) question.fontSize = 26;
            if (textOverride)
            {
                text.text = card.kind == "attention" ? AttentionText(card) : Safe(VisibleText(card.question, card.questionEn));
                text.fontSizeMax = card.kind == "long" ? 24 : 30; text.characterSpacing = 0;
                question.text = card.kind == "attention" ? Lang.T("它看对了吗？") : Lang.T("对吗？");
            }
            captionFocus.gameObject.SetActive(card.kind == "caption");
            if (card.kind == "caption")
            {
                int region = Math.Max(0, card.attentionRegion) % 4;
                captionFocus.anchorMin = new Vector2((region % 2) * .5f, (region / 2) * .5f);
                captionFocus.anchorMax = captionFocus.anchorMin + new Vector2(.5f, .5f);
                captionFocus.offsetMin = new Vector2(3, 3); captionFocus.offsetMax = new Vector2(-3, -3);
                meta.text += "  <size=12>" + Lang.T("关注区域：教学示意") + "</size>";
            }
            previewRoot.gameObject.SetActive(dedicatedPreview);
            if (dedicatedPreview) RefreshPreviewSurface(card);
            bool queued = Sim.ReviewCard(desk) != null;
            bool hasGuess;
            bool guess;
            double confidence;
            if (Sim.S.stage == 1 && card.kind == "combo" && card.judgeSource != "brain") hasGuess = Sim.TryGetComboSuggestion(card, out guess, out confidence);
            else if (queued) { hasGuess = card.hasJudgment; guess = card.guess; confidence = card.confidence; }
            else hasGuess = Sim.Suggestion(desk, out guess, out confidence);
            shownGuessValid = hasGuess; shownGuess = guess;
            suggestion.text = hasGuess
                ? T(AppNames.AiZh + "：", AppNames.AiEn + ": ") + YesNoWord(guess) + "？ " + XgSim.Pct(confidence)
                    + "  (" + (queued && card.judgeSource == "brain" ? Lang.T("本地模型") : Lang.T("检查点估计")) + ")"
                    + (queued && !string.IsNullOrEmpty(card.judgeFailure) ? " · " + Lang.T("大脑离线") : "")
                : T("还没有模型。标够 " + XgCatalog.SamplesToTrain + " 条，去「训练」按「训练一轮」。", "No model yet. Label " + XgCatalog.SamplesToTrain + ", then press Train in the Train tab.");
            if (hasGuess && host.Visible && host.Tab == "label" && suggestion.gameObject.activeInHierarchy && card.kind == "combo" && card.judgeSource != "brain")
                Sim.MarkComboSuggestionDisplayed(card, guess);
            if (hasGuess && Sim.FeatureVisible("lingguang-contact"))
            {
                if (card.id != commentCardId || commentOwner != Sim)
                {
                    comment = queued ? XgSpeechPolicy.Constrain(Lang.T("这个……我拿不准。你来。"), Sim.S.stage, En) : null;
                    RequestComment(desk, card, guess, confidence);
                }
                if (!string.IsNullOrEmpty(comment)) suggestion.text += "\n<size=14><color=#4054C4>" + T(AppNames.AiZh + "：「", AppNames.AiEn + ": “") + Safe(comment) + T("」", "”") + "</color></size>";
            }
            confidenceRoot.gameObject.SetActive(hasGuess);
            SetBar(confidenceFill, (float)confidence);
            confidenceFill.GetComponent<UnityEngine.UI.Image>().color = confidence < Sim.S.coopThreshold ? XgPalette.Gold : XgPalette.Good;
            yes.label.fontSize = no.label.fontSize = 24;
            yes.Set(YesNoWord(true) + KeyHint(1), true, Color.white, ZhongbaoSkin.Ink);
            no.Set(YesNoWord(false) + KeyHint(2), true, Color.white, ZhongbaoSkin.Ink);

            if (card.kind == "translation") { translationFadeCount = -1; AnimateTranslation(card, 0); }
            XgLabelGhost.For(paper)?.Show(Sim, desk, card); // the model's faint guess before the auto-labelling idea
            RefreshSteps(desk, d);
            RefreshCollaboration();
            RefreshPlatform();
            RefreshSide(desk);
        }

        static string KeyHint(int n) => "  <size=13><color=#B4B8C2>" + n + "</color></size>";

        void RefreshSteps(string desk, XgDataset d)
        {
            double samples = Sim.Samples(d.id);
            double best = Sim.BestAcc(d.id);
            var sb = new StringBuilder();
            sb.Append("<b>").Append(T(d.name, d.nameEn)).Append("</b>  <color=#7A7F8C>").Append(T("样本 ", "samples ")).Append(Samples(samples));
            if (best > 0) sb.Append(Lang.T(" · 最佳 ")).Append(XgCatalog.GradeNames[XgSim.Grade(Sim.BestScore(d.id))]).Append(" ").Append(XgSim.Pct(best));
            sb.Append("</color>\n");
            int dl = Sim.LevelOf(desk), dmax = XgSim.MaxLevelOf(desk);
            if (dmax > 1)
            {
                int per = XgSim.LabelsPerLevel(desk);
                sb.Append("<color=#68748C>").Append(Lang.T("难度 ")).Append(dl).Append("/").Append(dmax)
                  .Append(dl < dmax ? T(" · 再标对 " + (per - (int)Sim.Labels(desk) % per) + " 条升级（越难越值钱）", " · " + (per - (int)Sim.Labels(desk) % per) + " more to level up") : "").Append("</color>\n");
            }
            if ((XgMemes.IsTextDesk(desk) && desk != XgMemes.SenseDesk || desk == "meme") && Sim.Topic.Length > 0)
                sb.Append("<color=#E86E14>").Append(Lang.T("今日热词：")).Append(Sim.Topic).Append("</color>\n");
            // 新题型 chip (XgSim.Market.cs): this month's meme costs the checkpoint accuracy here until hand labels or epochs catch up.
            string drift = Sim.MemeDriftChip(desk);
            if (drift.Length > 0) sb.Append("<color=#D63031><b>").Append(drift).Append("</b></color>\n");
            string next = samples < XgCatalog.SamplesToTrain ? T("下一步：再标对 " + (XgCatalog.SamplesToTrain - (int)samples) + " 条就能训练", "Next: " + (XgCatalog.SamplesToTrain - (int)samples) + " more labels to unlock training")
                : best <= 0 ? Lang.T("下一步：去「训练」按「训练一轮」")
                : best < XgCatalog.AutoMinAccuracy ? (Sim.AutoLabelHidden ? Lang.T("下一步：接着练，准确率还能更高")
                    : T("下一步：练到 " + XgSim.Pct(XgCatalog.AutoMinAccuracy) + " 就能买自动答题", "Next: reach " + XgSim.Pct(XgCatalog.AutoMinAccuracy) + " to unlock auto-answer")) : "";
            if (next.Length > 0) sb.Append("<color=#3B5BDB>").Append(next).Append("</color>");
            string logs = XgDataUi.LogLine(Sim, desk);
            if (logs.Length > 0) sb.Append("\n<color=#0F8C7E>").Append(logs).Append(Lang.T(" · 亲手标一条 = ")).Append(N(XgSim.LogHandSamples, "0")).Append(Lang.T(" 样本")).Append("</color>");
            steps.text = sb.ToString();

            for (int i = 0; i < amountBtns.Length; i++)
            {
                bool on = (i == 0 && host.BuyAmount == 1) || (i == 1 && host.BuyAmount == 10) || (i == 2 && host.BuyAmount == int.MaxValue);
                amountBtns[i].Set(amountBtns[i].label.text, true, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }

            // 自动答题（本桌）
            int al = Sim.AutoLevel(desk);
            bool canAuto = Sim.CanBuyAuto(desk, out string why);
            double acost = XgCatalog.AutoCost(al);
            // Until the protagonist has the idea (XgSim.Epiphany.cs) the row is a 「？？？」 with a hint.
            auto.SetGlyph(Sim.AutoLabelHidden ? "？" : "自");
            auto.Set(Sim.AutoLabelHidden ? Lang.T("？？？") : Lang.T("自动答题（全局）"), "Lv " + al + "/" + XgCatalog.AutoMaxLevel,
                al > 0 && !Sim.AutoLabelHidden && !Sim.EligibleDesk(desk) ? "<color=#D63031>" + Lang.T("这张桌的检查点不到 60%，它不替你答") + "</color>"
                : canAuto || al > 0 ? Lang.T("每秒 ") + N(Sim.AutoCardsPerSecond(desk, Host), "0.0") + Lang.T(" 张 → ") + "<b>" + N((al + 1) * XgCatalog.AutoRatePerLevel * Math.Min(1, Math.Max(0, Host.Compute)), "0.0") + "</b>" + Lang.T("  · 约 ¥") + N(Sim.AutoIncome(desk, Host), "0.00") + T("/秒", "/s")
                    : (Sim.AutoLabelHidden ? "<color=#68748C>" : "<color=#D63031>") + (why ?? "") + "</color>",
                canAuto ? "¥" + Money(acost) : al >= XgCatalog.AutoMaxLevel ? Lang.T("满级") : Lang.T("未解锁"), canAuto && Host.Money >= acost, canAuto ? (float)(Host.Money / acost) : 0, al / (float)XgCatalog.AutoMaxLevel);

            // 完整数据包
            var packNode = XgCatalog.Node(desk + ".pack");
            pack.Show(packNode != null);
            // Other sources of this desk's data (junk pack, story data, crowd task) take over the row when picked.
            if (packNode != null && !RefreshSourceRow(desk))
            {
                var st = Sim.Status(packNode, Host);
                pack.Set(Lang.T("完整数据包"), st == XgSim.NodeStatus.Owned ? Lang.T("已拥有") : "",
                    st == XgSim.NodeStatus.Locked ? "<color=#D63031>" + Sim.Why(packNode, Host) + "</color>" : Lang.T("一次补足 ") + Samples(d.samples),
                    st == XgSim.NodeStatus.Owned ? "✓" : "¥" + Money(packNode.cost), st == XgSim.NodeStatus.Buyable, st == XgSim.NodeStatus.Owned ? 1 : (float)(Host.Money / packNode.cost), st == XgSim.NodeStatus.Owned ? 1 : 0);
            }
            bool trainVisible = host.Controller == null || host.Controller.FeatureVisible("train");
            ShowFrame(toTrain, trainVisible);
            ZhongbaoSkin.Ghost(toTrain, Lang.T("去训练 →"), trainVisible && Sim.TrainingUnlocked(d.track));
        }

        /// <summary>Everything in the right column that follows the desk: 今天, the 灵光 panel and the stacked shop.</summary>
        void RefreshSide(string desk)
        {
            RefreshToday();
            RefreshAi();
            float height = LayoutShop(coopPanel.gameObject.activeSelf, platformChip.gameObject.activeSelf, XgCatalog.Node(desk + ".pack") != null);
            LayoutSide(false, height);
        }

        string HandStats()
        {
            int total = Sim.S.handCorrect + Sim.S.handWrong;
            return Lang.T("亲手标对 ") + Sim.S.handCorrect + (total > 0 ? Lang.T(" · 正确率 ") + XgSim.Pct((double)Sim.S.handCorrect / total) : "")
                + Lang.T(" · 最高连击 ×") + Sim.S.bestCombo
                + (Sim.S.handFines > 0 ? Lang.T(" · 点错罚款 ¥") + N(Sim.S.handFines, "0.00") : "");
        }

        void BuildPreviewSurface()
        {
            previewRoot = Rect("BottleneckPreview", paper, Vector2.zero, Vector2.one, new Vector2(10, 7), new Vector2(-10, -7));
            previewHeading = ui.Text(Rect("Heading", previewRoot, new Vector2(0, .88f), Vector2.one, Vector2.zero, Vector2.zero), "", 12, XgPalette.Muted, TextAlignmentOptions.Center);
            previewGraphic = Rect("Actual5x5Patterns", previewRoot, new Vector2(0, .1f), new Vector2(1, .86f), Vector2.zero, Vector2.zero).gameObject.AddComponent<XgLabelPreviewGraphic>();
            previewGraphic.raycastTarget = false;
            previewSourceScroll = PreviewTextPane("Source", previewRoot, out previewSource);
            previewCandidateScroll = PreviewTextPane("Candidate", previewRoot, out previewCandidate);
            previewMemoryRoot = Rect("MemoryReadout", previewRoot, new Vector2(0, .01f), new Vector2(1, .20f), Vector2.zero, Vector2.zero);
            previewMemoryText = ui.Text(Rect("Label", previewMemoryRoot, new Vector2(0, .25f), Vector2.one, Vector2.zero, Vector2.zero), "", 11, XgPalette.Muted, TextAlignmentOptions.Center);
            var bar = Rect("Retention", previewMemoryRoot, Vector2.zero, new Vector2(1, .18f), new Vector2(8, 0), new Vector2(-8, 0));
            previewMemoryFill = Bar(bar, "Fill", XgPalette.Line, XgPalette.Accent);
            previewRoot.gameObject.SetActive(false);
        }

        UnityEngine.UI.ScrollRect PreviewTextPane(string name, RectTransform parent, out TMP_Text label)
        {
            var pane = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var viewport = Rect("Viewport", pane, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(viewport, Color.white); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, 0), new Vector2(-5, 1));
            content.pivot = new Vector2(.5f, 1);
            label = ui.Text(content, "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            label.margin = new Vector4(2, 2, 2, 4);
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var scroll = pane.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.scrollSensitivity = 24; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            return scroll;
        }

        static void PreviewArea(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        void RefreshPreviewSurface(XgCard card)
        {
            digit.gameObject.SetActive(false); poem.gameObject.SetActive(false); text.gameObject.SetActive(false);
            captchaA.gameObject.SetActive(false); captchaB.gameObject.SetActive(false); face.gameObject.SetActive(false);
            caption.gameObject.SetActive(false); go.gameObject.SetActive(false); captionFocus.gameObject.SetActive(false);
            bool reset = previewOwner != Sim || previewId != card.id || previewEnglish != En;
            if (reset)
            {
                previewOwner = Sim; previewId = card.id; previewEnglish = En;
                previewReadTime = translationTime = 0; translationFadeCount = -1;
            }
            bool grid = !string.IsNullOrEmpty(card.patternA), two = !string.IsNullOrEmpty(card.patternB);
            bool memory = card.kind == "long";
            previewGraphic.gameObject.SetActive(grid);
            previewGraphic.Show(card.patternA, card.patternB);
            previewMemoryRoot.gameObject.SetActive(memory);
            previewHeading.text = (card.bottleneckPreview ? Lang.T("瓶颈预览 · ") : "")
                + Lang.T("教学示意，不是真实模型内部状态");
            previewSourceScroll.gameObject.SetActive(!grid || !two);
            previewCandidateScroll.gameObject.SetActive(!grid || !two);
            string source = VisibleText(card.sourceText, card.sourceTextEn);
            string candidate = VisibleText(card.candidateText, card.candidateTextEn);
            previewSource.fontSize = previewCandidate.fontSize = 14;
            if (grid && two)
            {
                PreviewArea(previewGraphic.rectTransform, new Vector2(0, .03f), new Vector2(1, .84f));
                previewHeading.text += Lang.T(" · 左 A / 右 B");
            }
            else if (grid)
            {
                PreviewArea(previewGraphic.rectTransform, new Vector2(0, .08f), new Vector2(.40f, .84f));
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(.43f, .48f), new Vector2(1, .86f));
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(.43f, .03f), new Vector2(1, .45f));
                previewSource.text = "<b>" + Lang.T("固定摘要") + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + Lang.T("候选描述") + "</b>\n" + Safe(candidate);
            }
            else if (memory)
            {
                bool hasCandidate = !string.IsNullOrEmpty(candidate);
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(0, hasCandidate ? .54f : .24f), new Vector2(1, .86f));
                previewCandidateScroll.gameObject.SetActive(hasCandidate);
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(0, .24f), new Vector2(1, .51f));
                previewSource.text = "<b>" + Lang.T("上文（滚动阅读）") + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + Lang.T("下一句") + "</b>\n" + Safe(candidate);
                AnimateMemory(card, 0);
            }
            else
            {
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(0, .04f), new Vector2(.49f, .86f));
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(.51f, .04f), new Vector2(1, .86f));
                previewSource.text = "<b>" + (card.kind == "order" ? Lang.T("句子 A") : Lang.T("原文（滚动阅读）")) + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + (card.kind == "order" ? Lang.T("句子 B") : Lang.T("候选译文（滚动阅读）")) + "</b>\n" + Safe(candidate);
            }
            string fullQuestion = VisibleText(card.question, card.questionEn);
            int lastLine = fullQuestion.LastIndexOf('\n');
            question.text = Safe(lastLine < 0 ? fullQuestion : fullQuestion.Substring(lastLine + 1));
            question.fontSizeMax = 22;
            if (reset)
            {
                previewSourceScroll.StopMovement(); previewCandidateScroll.StopMovement();
                Canvas.ForceUpdateCanvases();
                previewSourceScroll.verticalNormalizedPosition = previewCandidateScroll.verticalNormalizedPosition = 1;
            }
            if (card.kind == "translation") { translationFadeCount = -1; AnimateTranslation(card, 0); }
        }

        void AnimateMemory(XgCard card, float dt)
        {
            if (card.kind != "long" || !previewRoot.gameObject.activeSelf) return;
            previewReadTime += dt;
            var best = Sim.S.best.Find(item => item.dataset == card.dataset);
            string arch = best != null && !string.IsNullOrEmpty(best.arch) ? best.arch : Sim.Run(XgCatalog.Dataset(card.dataset).track).arch;
            int distance = Math.Max(0, card.distance);
            float reading = Mathf.Clamp01(previewReadTime / (1 + distance * .45f));
            double factor = arch == "rnn" ? .9 : arch == "lstm" || arch == "gru" ? .98 : 1;
            double retention = Math.Pow(factor, distance * reading);
            SetBar(previewMemoryFill, (float)retention);
            previewMemoryFill.GetComponent<UnityEngine.UI.Image>().color = factor < .95 ? XgPalette.Accent : XgPalette.Good;
            previewMemoryText.text = Lang.T("检查点记忆演示 · ") + arch.ToUpperInvariant()
                + Lang.T(" · 距离 ") + distance + " · " + XgSim.Pct(retention);
        }

        void BuildFeed(RectTransform left)
        {
            feedRoot = Strip("AutomaticFeed", left, 8, 24, 14, 14);
            Panel(feedRoot, new Color32(241, 245, 249, 255)).raycastTarget = false;
            feedRoot.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            for (int i = 0; i < XgSim.FeedWindow; i++)
            {
                var rt = Rect("PassedCard" + i, feedRoot, new Vector2(0, .5f), new Vector2(0, .5f), Vector2.zero, Vector2.zero);
                rt.pivot = new Vector2(0, .5f); rt.sizeDelta = new Vector2(80, 22);
                feedRects.Add(rt);
                var image = Panel(rt, XgPalette.AccentSoft); image.raycastTarget = false; feedImages.Add(image);
                var label = ui.Text(Rect("Text", rt, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0)), "", 12, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
                label.enableAutoSizing = true; label.fontSizeMin = 9; label.fontSizeMax = 12;
                feedTexts.Add(label); feedIds.Add(-1); feedAges.Add(2);
                rt.gameObject.SetActive(false);
                var fly = Rect("AuditedCard" + i, left, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-44, -12), new Vector2(44, 12));
                Panel(fly, new Color32(255, 224, 135, 255)).raycastTarget = false;
                var flyText = ui.Text(Rect("Text", fly, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0)), "", 12, new Color32(130, 76, 0, 255), TextAlignmentOptions.Center);
                flyText.textWrappingMode = TextWrappingModes.NoWrap; flyText.overflowMode = TextOverflowModes.Ellipsis;
                auditFlights.Add(new AuditFlight { rect = fly, label = flyText });
                fly.gameObject.SetActive(false);
            }
        }

        void BuildCollaborationPanel(RectTransform right)
        {
            coopPanel = Rect("Collaboration", right, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            Panel(coopPanel, new Color32(241, 245, 252, 255)).raycastTarget = false;
            thresholdText = ui.Text(Strip("ThresholdText", coopPanel, 4, 20, 10, 10), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            var sliderRoot = Strip("ThresholdSlider", coopPanel, 26, 20, 12, 12);
            var track = Rect("Track", sliderRoot, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -3), new Vector2(0, 3));
            Panel(track, XgPalette.Line).raycastTarget = false;
            var fillArea = Rect("FillArea", sliderRoot, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -7));
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(fill, XgPalette.Accent).raycastTarget = false;
            var handleArea = Rect("HandleArea", sliderRoot, Vector2.zero, Vector2.one, new Vector2(7, 0), new Vector2(-7, 0));
            var handle = Rect("Handle", handleArea, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(-7, 0), new Vector2(7, 0));
            var handleImage = Panel(handle, XgPalette.Accent);
            threshold = sliderRoot.gameObject.AddComponent<UnityEngine.UI.Slider>();
            threshold.fillRect = fill; threshold.handleRect = handle; threshold.targetGraphic = handleImage;
            threshold.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            threshold.minValue = 50; threshold.maxValue = 99; threshold.wholeNumbers = true;
            threshold.onValueChanged.AddListener(value =>
            {
                if (refreshingThreshold || !Sim.CollaborationEnabled) return;
                Sim.SetCoopThreshold(value / 100.0);
                RefreshCollaboration();
            });
            coopStats = ui.Text(Strip("RoutingStats", coopPanel, 51, 44, 10, 10), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            brainStatus = ui.Text(Strip("BrainStatus", coopPanel, 98, 42, 10, 140), "", 12, XgPalette.Muted, TextAlignmentOptions.TopLeft);
            recheckBtn = ui.Button(coopPanel, "", OnRecheck, 12);
            recheckBtn.rt.gameObject.name = "QcRecheck";
            recheckBtn.rt.anchorMin = recheckBtn.rt.anchorMax = new Vector2(1, 1);
            recheckBtn.rt.offsetMin = new Vector2(-134, -128); recheckBtn.rt.offsetMax = new Vector2(-8, -100);
            UiTip.Add(recheckBtn.rt, () => T("人工复核旧数据：把这张桌最多 " + XgSim.RecheckBatch + " 条没被发现的错标放回待复核。\n每答对一条，就清掉一条噪声（按普通题付钱）。噪声超过样本的 10%，模型会学回自己的错（近亲繁殖）。",
                "Re-check old labels: send up to " + XgSim.RecheckBatch + " of this desk's uncaught wrong labels back to the review inbox.\nEach right answer clears one noisy row (paid as an ordinary card). Above 10% noise the model learns its own mistakes back (inbreeding)."));
            coopPanel.gameObject.SetActive(false);
        }

        void RefreshCollaboration()
        {
            bool enabled = Sim.CollaborationEnabled && Mode == 0;
            coopPanel.gameObject.SetActive(enabled);
            if (!enabled) return;
            refreshingThreshold = true;
            threshold.SetValueWithoutNotify((float)(Sim.S.coopThreshold * 100));
            refreshingThreshold = false;
            thresholdText.text = Lang.T("把握阈值 ") + XgSim.Pct(Sim.S.coopThreshold)
                + Lang.T("  · 把握不是准确率");
            var st = Sim.CollaborationStats();
            string share = st.total == 0 ? "—" : XgSim.Pct(st.automaticFraction);
            string accuracy = st.automatic == 0 ? "—" : XgSim.Pct(st.automaticAccuracy);
            coopStats.text = Lang.T("近 100 题 · 自动 ") + share + Lang.T(" · 准确率 ") + accuracy
                + "\n" + Lang.T("噪声标签 ") + N(Sim.NoiseTotal, "0") + Lang.T(" · 待复核 ") + Sim.ReviewCount() + "/" + XgSim.ReviewCapacity
                + (Sim.ReviewFull ? " <color=#B36A00>" + Lang.T("自动已暂停") + "</color>" : "");
            bool online = Sim.BrainOnline && string.IsNullOrEmpty(Sim.BrainStatus);
            string source = Sim.Has("label.brain") && XgSim.IsBrainDesk(Desk)
                ? online ? Lang.T("本地模型在线") : Lang.T("大脑离线 · 回退检查点估计")
                : Lang.T("检查点估计 · 非真实模型推理");
            int n = Sim.BrainJudgmentCount(Desk);
            brainStatus.text = source + (n > 0 ? "\n" + Lang.T("本桌大脑近 ") + n + Lang.T(" 题：") + XgSim.Pct(Sim.BrainAccuracy(Desk)) : "\n" + Lang.T("模型判断只供参考；代码负责结算。"));
            RefreshRecheck();
        }

        // ───────────── 摆渡众包 quality control ─────────────

        void BuildPlatformChip(RectTransform right)
        {
            // Shares the buy-amount row: the chip replaces the "Buy amount" caption once the platform is active.
            platformChip = Rect("PlatformChip", right, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            platformImage = Panel(platformChip, new Color32(232, 238, 255, 255));
            platformText = ui.Text(Rect("Text", platformChip, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-4, 0)), "", 11, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            platformText.enableAutoSizing = true; platformText.fontSizeMin = 10; platformText.fontSizeMax = 13; platformText.lineSpacing = -14;
            platformText.overflowMode = TextOverflowModes.Ellipsis;
            UiTip.Add(platformChip, PlatformTip);
            platformChip.gameObject.SetActive(false);
        }

        void BuildFrozenBanner(RectTransform left)
        {
            frozenBanner = Strip("QcFrozenBanner", left, 8, 24, 14, 14);
            Panel(frozenBanner, new Color32(214, 48, 49, 255));
            frozenText = ui.Text(Rect("Text", frozenBanner, Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-128, 0)), "", 13, Color.white, TextAlignmentOptions.MidlineLeft);
            frozenText.fontStyle = FontStyles.Bold;
            frozenText.textWrappingMode = TextWrappingModes.NoWrap; frozenText.overflowMode = TextOverflowModes.Ellipsis;
            frozenText.enableAutoSizing = true; frozenText.fontSizeMin = 10; frozenText.fontSizeMax = 13;
            appealBtn = ui.Button(frozenBanner, "", OnAppeal, 13);
            appealBtn.rt.gameObject.name = "QcAppeal"; // the guide's "name:QcAppeal" locator
            appealBtn.rt.anchorMin = new Vector2(1, 0); appealBtn.rt.anchorMax = new Vector2(1, 1);
            appealBtn.rt.offsetMin = new Vector2(-122, 2); appealBtn.rt.offsetMax = new Vector2(-3, -2);
            appealBtn.label.fontStyle = FontStyles.Bold;
            UiTip.Add(frozenBanner, () => T("摆渡众包把账号冻结了：这段时间自动标注不能提交，也没有收入。\n手动标注不受影响，冻结期间每标对一条，信用 +" + N(XgSim.QcCreditHand, "0.#") + "。\n第 1/2/3 次举报分别冻结 3/8/20 分钟。",
                "Bodu Crowdsourcing froze the account: auto labelling cannot submit or earn meanwhile.\nHand labelling still works; each right answer while frozen adds " + N(XgSim.QcCreditHand, "0.#") + " credit.\nReports 1/2/3 freeze for 3/8/20 minutes."));
            UiTip.Add(appealBtn.rt, () => T("申诉：付 ¥" + N(XgSim.QcAppealMin, "0") + " 或余额的 " + N(XgSim.QcAppealShare * 100, "0") + "%（取较多者），立刻解冻。每次冻结只能申诉一次，信用分不变。",
                "Appeal: pay ¥" + N(XgSim.QcAppealMin, "0") + " or " + N(XgSim.QcAppealShare * 100, "0") + "% of your money (whichever is more) to unfreeze now. Once per freeze; credit is unchanged."));
            frozenBanner.gameObject.SetActive(false);
        }

        string TierName => Sim.CreditTierName(Sim.CreditTier);

        /// <summary>Small tags on a review card: an old label sent back for re-checking, a trap item 题库比对 recognised.</summary>
        string QcTags(XgCard card)
        {
            string tags = "";
            if (card.recheck) tags += "<color=#7A5AF8>" + Lang.T("旧数据复核 · ") + "</color>";
            if (Sim.TrapRecognized(card)) tags += "<color=#2932E1><b>" + Lang.T("眼熟") + "</b></color> · ";
            return tags;
        }

        void OnCaptchaAnswered(bool right)
        {
            var at = Fx.At(captchaCard.Root);
            if (right)
            {
                Fx.Play(XgJuice.Sfx.Id.Unlock);
                Fx.Float(at, Lang.T("验证通过 · 信用 +1"), XgPalette.Good, 24);
            }
            else
            {
                Fx.Shake(4, .25f);
                Fx.Play(XgJuice.Sfx.Id.Thud, 1.1f, .7f);
                Fx.Float(at, Lang.T("验证失败 · 自动标注暂停 2 分钟"), XgPalette.Bad, 22);
            }
            host.Refresh(true);
        }

        void RefreshRecheck()
        {
            int can = Sim.RecheckableNoise(Desk), waiting = Sim.PendingRechecks(Desk);
            bool show = can > 0 || waiting > 0;
            recheckBtn.Show(show);
            if (!show) return;
            recheckBtn.Set(can > 0 ? Lang.T("人工复核旧数据 ×") + can : Lang.T("复核中 ") + waiting, can > 0, (Color)new Color32(122, 90, 248, 255), Color.white);
        }

        void OnRecheck()
        {
            if (Sim.RecheckNoise(Desk) == 0) { Fx.Knock(recheckBtn.rt, .05f, new Vector2(8, 0), .25f); return; }
            Fx.Play(XgJuice.Sfx.Id.Swoosh, 1.1f, .6f);
            host.Refresh(true);
        }

        void RefreshPlatform()
        {
            bool active = Sim.QualityActive && Mode == 0;
            wasFrozen = Sim.QualityFrozen; wasPaused = Sim.CaptchaPauseLeft > 0; wasCaptcha = Sim.CaptchaPending;
            platformChip.gameObject.SetActive(active);
            frozenBanner.gameObject.SetActive(active && (wasFrozen || wasPaused));
            captchaCard.Show(active && wasCaptcha);
            if (active && wasCaptcha) captchaCard.Refresh();
            if (!active) return;
            var tier = Sim.CreditTier;
            Color ink = tier == XgCreditTier.Gold ? new Color32(150, 98, 0, 255) : tier == XgCreditTier.Normal ? new Color32(41, 50, 225, 255) : new Color32(200, 36, 36, 255);
            platformImage.color = tier == XgCreditTier.Gold ? new Color32(255, 243, 210, 255) : tier == XgCreditTier.Normal ? new Color32(232, 236, 255, 255) : new Color32(255, 225, 225, 255);
            platformText.color = ink;
            string rate = Sim.SpotChecks == 0 ? Lang.T("还没有抽检")
                : Lang.T("合格率 ") + N(Math.Floor(Sim.PassRate * 100 + 1e-9), "0") + "%" + T("（近 " + Sim.SpotChecks + " 次）", " (" + Sim.SpotChecks + " checks)");
            if (Sim.QualityWarning) rate = "<color=#D63031>" + rate + "</color>";
            platformText.text = "<b>" + T("摆渡众包", "Bodu Crowd") + "</b> · " + Lang.T("信用 ") + N(Math.Floor(Sim.Credit), "0") + " " + TierName + "\n" + rate;
            if (wasFrozen || wasPaused) RefreshFrozenBanner();
        }

        void RefreshFrozenBanner()
        {
            if (!Sim.QualityFrozen)
            {
                // After a failed captcha: a plain pause, nothing to appeal.
                appealBtn.Show(false);
                frozenText.text = Lang.T("人机验证未通过：自动标注暂停 ") + XgSim.FreezeClock(Sim.CaptchaPauseLeft);
                return;
            }
            appealBtn.Show(true);
            frozenText.text = Lang.T("账号被举报冻结 ") + XgSim.FreezeClock(Sim.FreezeSecondsLeft) + " · " + Sim.ReportReasonText(Sim.LastReportReason);
            bool can = Sim.CanAppeal(Host, out _);
            appealBtn.Set(Lang.T("申诉 ¥") + Money(Sim.AppealCost(Host)), can, Color.white, (Color)new Color32(190, 30, 30, 255));
        }

        void OnAppeal()
        {
            if (!Sim.Appeal(Host))
            {
                Fx.Knock(appealBtn.rt, .05f, new Vector2(8, 0), .25f);
                Fx.Play(XgJuice.Sfx.Id.Thud, 1.3f, .6f);
                return;
            }
            Fx.Play(XgJuice.Sfx.Id.Unlock);
            Fx.Float(Fx.At(frozenBanner), Lang.T("账号已解冻"), XgPalette.Good, 22);
            host.Refresh(true);
        }

        string PlatformTip()
        {
            if (Sim == null) return "";
            string pct(double v) => N(v * 100, "0") + "%";
            return T("摆渡众包会抽检自动标注（手动标注从不抽检）。\n"
                    + "· 当前信用 " + N(Sim.Credit, "0.#") + "（" + TierName + "）：抽检率 " + pct(Sim.SpotCheckChance) + "，自动收入 ×" + N(Sim.QualityPayMultiplier, "0.0#") + "。金牌 ≥ 90，重点关注 < 60。\n"
                    + "· 抽检不合格：罚该题报酬的 2 倍，信用 −" + N(XgSim.QcCreditFail, "0") + "；抽检合格：信用 +" + N(XgSim.QcCreditPass, "0.#") + "。累计罚款 ¥" + N(Sim.S.qcFines, "0.##") + "。\n"
                    + "· 近 " + XgSim.QcWindow + " 次抽检满 " + XgSim.QcMinChecks + " 次才评定：错误率 ≥ " + pct(XgSim.QcWarnRate) + " 警告，≥ " + pct(XgSim.QcReportRate) + " 举报并冻结自动标注；答案几乎全是同一个也会被当成脚本。\n"
                    + "怎么办：调高「拿不准才问我」阈值，买「抽检」先拦下错题，或者把模型练得更好。",
                "Bodu Crowdsourcing spot-checks automatic labels (hand labels are never checked).\n"
                    + "· Credit " + N(Sim.Credit, "0.#") + " (" + TierName + "): " + pct(Sim.SpotCheckChance) + " checked, auto income ×" + N(Sim.QualityPayMultiplier, "0.0#") + ". Gold at 90+, under watch below 60.\n"
                    + "· A failed check is fined twice the card's pay and costs " + N(XgSim.QcCreditFail, "0") + " credit; a passed one adds " + N(XgSim.QcCreditPass, "0.#") + ". Fines so far: ¥" + N(Sim.S.qcFines, "0.##") + ".\n"
                    + "· Judged over the last " + XgSim.QcWindow + " checks once there are " + XgSim.QcMinChecks + ": an error rate of " + pct(XgSim.QcWarnRate) + " warns, " + pct(XgSim.QcReportRate) + " reports the account and freezes auto labelling; near-identical answers look like a script.\n"
                    + "Fixes: raise the 'Ask when unsure' threshold, buy Quality audit to catch mistakes first, or train a better model.");
        }

        void RefreshFeed()
        {
            feedRoot.gameObject.SetActive(Sim.CollaborationEnabled && Mode == 0);
            if (!Sim.CollaborationEnabled || Mode != 0) return;
            var oldIds = feedIds.ToArray(); var oldAges = feedAges.ToArray();
            for (int i = 0; i < feedRects.Count; i++)
            {
                bool on = i < Sim.S.autoFeed.Count;
                feedRects[i].gameObject.SetActive(on);
                if (!on) { feedIds[i] = -1; continue; }
                var record = Sim.S.autoFeed[i];
                int was = Array.IndexOf(oldIds, record.cardId);
                feedIds[i] = record.cardId; feedAges[i] = was >= 0 ? oldAges[was] : 0;
                var info = XgCatalog.Desk(record.dataset);
                bool fined = record.spotChecked && !record.correct && !record.audited;
                feedTexts[i].text = fined ? "× " + Lang.T("抽检不合格 −¥") + N(record.fine, "0.##")
                    : (record.audited ? "↖ " : record.correct ? "✓ " : "× ") + T(info.name, info.nameEn);
                feedImages[i].color = fined ? new Color32(255, 205, 205, 255) : record.audited ? new Color32(255, 235, 166, 255) : record.correct ? new Color32(221, 245, 229, 255) : new Color32(252, 222, 222, 255);
                feedTexts[i].color = fined ? new Color32(190, 20, 20, 255) : record.audited ? new Color32(140, 84, 10, 255) : record.correct ? XgPalette.Good : XgPalette.Bad;
                feedTexts[i].fontStyle = fined ? FontStyles.Bold : FontStyles.Normal;
                if (record.cardId > lastFeedId && record.audited && root.gameObject.activeInHierarchy)
                {
                    var target = HallRowOf(record.dataset);
                    if (target != null && !Sim.S.reduceFx)
                    {
                        var flight = auditFlights[i];
                        var parent = (RectTransform)flight.rect.parent;
                        flight.from = parent.InverseTransformPoint(feedRects[i].TransformPoint(feedRects[i].rect.center));
                        flight.to = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
                        flight.label.text = Lang.T("抽检 ↖ 待复核");
                        flight.age = 0; flight.active = true;
                        flight.rect.anchoredPosition = flight.from; flight.rect.localScale = Vector3.one;
                        flight.rect.gameObject.SetActive(true); flight.rect.SetAsLastSibling();
                    }
                }
                lastFeedId = Math.Max(lastFeedId, record.cardId);
            }
        }

        void AnimateFeed(float dt)
        {
            if (!feedRoot.gameObject.activeInHierarchy) return;
            float width = Mathf.Max(100, feedRoot.rect.width), gap = 4;
            float cell = (width - gap * 5) / 6;
            for (int i = 0; i < feedRects.Count; i++)
            {
                if (feedIds[i] < 0) continue;
                feedAges[i] += dt;
                float t = Sim.S.reduceFx ? 1 : Mathf.Clamp01(feedAges[i] / .65f);
                float slide = (1 - t) * (1 - t) * cell;
                feedRects[i].sizeDelta = new Vector2(cell, 22);
                feedRects[i].anchoredPosition = new Vector2(i * (cell + gap) + slide, 0);
            }
            foreach (var flight in auditFlights)
            {
                if (!flight.active) continue;
                flight.age += dt;
                float k = Mathf.Clamp01(flight.age / .75f), eased = k * k * (3 - 2 * k);
                flight.rect.anchoredPosition = Vector2.Lerp(flight.from, flight.to, eased) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 20);
                flight.rect.localScale = Vector3.one * Mathf.Lerp(1, .7f, k);
                if (k >= 1) { flight.active = false; flight.rect.gameObject.SetActive(false); }
            }
        }

        void RefreshSpecialVisibility(bool special)
        {
            specialInfo.gameObject.SetActive(special);
            if (special)
            {
                ShowFrame(toTrain, false); coopPanel.gameObject.SetActive(false);
                platformChip.gameObject.SetActive(false); frozenBanner.gameObject.SetActive(false); captchaCard.Show(false);
                wasFrozen = Sim.QualityFrozen; wasPaused = Sim.CaptchaPauseLeft > 0; wasCaptcha = Sim.CaptchaPending;
                foreach (var flight in auditFlights) { flight.active = false; flight.rect.gameObject.SetActive(false); }
            }
            ShowFrame(yes, true); ShowFrame(no, true);
        }

        /// <summary>Shows or hides a rimmed button together with its frame.</summary>
        static void ShowFrame(XgBtn b, bool on)
        {
            var frame = ZhongbaoSkin.Frame(b);
            if (frame != null && frame.gameObject.activeSelf != on) frame.gameObject.SetActive(on);
        }

        void RefreshSpecial()
        {
            previewRoot.gameObject.SetActive(false);
            digit.gameObject.SetActive(false); poem.gameObject.SetActive(false); captchaA.gameObject.SetActive(false); captchaB.gameObject.SetActive(false);
            face.gameObject.SetActive(false); caption.gameObject.SetActive(false); go.gameObject.SetActive(false); captionFocus.gameObject.SetActive(false);
            text.gameObject.SetActive(true); text.fontSizeMax = 32;
            timerBar.gameObject.SetActive(false); paperGlow.on = false;
            paperImage.color = XgPalette.Paper;
            confidenceRoot.gameObject.SetActive(Mode == 1);
            feedback.text = "";
            yes.label.fontSize = no.label.fontSize = 30;
            yes.Set(T("是", "Yes") + KeyHint(1), true, Color.white, ZhongbaoSkin.Ink);
            no.Set(T("否", "No") + KeyHint(2), true, Color.white, ZhongbaoSkin.Ink);
            if (Mode == 1)
            {
                string[] questions = { "把循环全部去掉？", "让几组注意力同时看不同的东西？", "没有循环，它就不知道词的先后。给每个位置编个号？" };
                string[] english = { "Remove recurrence entirely?", "Let several attention heads look at different things at once?", "Without recurrence, it loses word order. Give each position a number?" };
                int step = Math.Max(0, Math.Min(2, Sim.S.project.experiments));
                meta.text = T(AppNames.AiZh + "的实验 ", AppNames.AiEn + "'s experiment ") + (step + 1) + "/3";
                text.text = T(questions[step], english[step]);
                question.text = Lang.T("这次，轮到我问你。"); question.fontSize = 22;
                suggestion.text = Lang.T("答「否」：重做本段实验，不再次扣钱。");
                SetBar(confidenceFill, (float)(Sim.S.project.gpuSeconds / XgSim.ProjectGpuSeconds));
                steps.text = Lang.T("Transformer 研发") + "\n" + N(Sim.S.project.gpuSeconds, "0") + " / " + XgSim.ProjectGpuSeconds + Lang.T(" GPU·秒");
                specialInfo.text = Lang.T("研究已暂停，等待你的判断。\n\n三个实验：去掉循环、多头注意力、位置编号。\n\n这里的研发是游戏模拟，不会改写本机 GGUF 模型的权重。");
            }
            else if (Mode == 2)
            {
                meta.text = Lang.T("最后一张题卡");
                text.text = Lang.T("你后悔教我吗？");
                question.text = T("出题者：" + AppNames.AiZh, "Asked by " + AppNames.AiEn); question.fontSize = 22;
                suggestion.text = Lang.T("这次没有标准答案。");
                steps.text = Lang.T("第一章 · 只要注意力");
                specialInfo.text = Lang.T("无论回答是或否，都走向第一章的结尾。\n\n这段 2016 年提前突破的故事是虚构；历史字幕会标明真实年份。");
            }
            else
            {
                meta.text = Lang.T("第一章结束");
                text.text = Sim.S.endingRegret ? Lang.T("那我还是会记得。") : Lang.T("那我们继续。");
                question.text = Lang.T("只要注意力"); question.fontSize = 26;
                suggestion.text = Lang.T("第二章入口已保留，内容尚未开放。");
                yes.Set(Lang.T("第二章 · 蒸馏／转生"), true, Color.white, ZhongbaoSkin.Blue);
                yes.label.fontSize = 17; ShowFrame(no, false);
                steps.text = Lang.T("下一程");
                specialInfo.text = Lang.T("蒸馏与转生\n\n把大模型的经验交给更小的模型。\n\n这是下一章的入口说明，不会重置你的存档，也不会开始尚未实现的第二章。");
            }
        }

        void AnswerSpecial(bool answer)
        {
            if (Mode == 3)
            {
                host.ShowToast(Lang.T("第二章：蒸馏／转生 · 尚未开放，当前存档保持不变。"), 4f);
                return;
            }
            bool accepted = Mode == 1 ? Sim.ProjectAnswer(answer) : Sim.EndingAnswer(answer);
            if (!accepted) return;
            Fx.Play(XgJuice.Sfx.Id.Click);
            host.Controller?.SaveNow();
            host.Refresh(true);
        }

        string AttentionText(XgCard card)
        {
            string raw = VisibleText(card.question, card.questionEn);
            string word = En ? card.attentionWord == "河" ? "river" : card.attentionWord == "小船" ? "boats" : card.attentionWord : card.attentionWord;
            if (string.IsNullOrEmpty(word)) return Safe(raw);
            int at = raw.IndexOf(word, StringComparison.Ordinal);
            if (at < 0) return Safe(raw);
            return Safe(raw.Substring(0, at)) + "<mark=#FFD96655><u>" + Safe(word) + "</u></mark>" + Safe(raw.Substring(at + word.Length));
        }

        void AnimateTranslation(XgCard card, float dt)
        {
            if (card.kind != "translation" || !HasPreviewSurface(card) || !previewRoot.gameObject.activeSelf) return;
            translationTime += dt;
            string raw = VisibleText(card.candidateText, card.candidateTextEn);
            int start = raw.Length / 2, count = Mathf.Min(raw.Length - start, Mathf.FloorToInt(translationTime * 12));
            if (count == translationFadeCount) return;
            translationFadeCount = count;
            string title = string.IsNullOrEmpty(card.patternA) ? Lang.T("候选译文（滚动阅读）") : Lang.T("候选描述");
            previewCandidate.text = "<b>" + title + "</b>\n" + Safe(raw.Substring(0, start)) + "<color=#68748C>" + Safe(raw.Substring(start, count)) + "</color>" + Safe(raw.Substring(start + count));
        }

        public static string FormatCardComment(string reply, string visibleQuestion, int stage, bool english)
        {
            reply = LingGuangV05.Core.Era.EraLexicon.Scrub(reply ?? "").Trim(); // it is 2016: no later memes
            if (reply.StartsWith("关注：", StringComparison.Ordinal) || reply.StartsWith("focus:", StringComparison.OrdinalIgnoreCase))
            {
                XgSpeechPolicy.ReadFocus(reply, visibleQuestion, out string answer);
                reply = answer == reply ? (english ? "Please review this one." : "这题请你复核。") : answer;
            }
            reply = XgSpeechPolicy.Constrain(reply, stage, english);
            if (stage <= 3) return reply;
            int stop = reply.IndexOfAny(new[] { '。', '！', '？', '!', '?', '\n' });
            int period = reply.IndexOf(". ", StringComparison.Ordinal);
            if (period >= 0 && (stop < 0 || period < stop)) stop = period;
            if (stop >= 0) reply = reply.Substring(0, stop + (reply[stop] == '\n' ? 0 : 1));
            int limit = english ? 70 : 36;
            return reply.Length > limit ? reply.Substring(0, limit) + "…" : reply;
        }

        /// <summary>At stage three, an external prediction may receive one short, stage-constrained comment.</summary>
        void RequestComment(string desk, XgCard card, bool guess, double confidence)
        {
            if (!Sim.FeatureVisible("lingguang-contact")) return;
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) return;
            var info = XgCatalog.Desk(desk);
            string q;
            if (card.bottleneckPreview || card.kind == "combo" || card.kind == "long" || card.kind == "attention" || card.kind == "translation") q = VisibleText(card.question, card.questionEn);
            else switch (info.kind)
            {
                case XgDeskKind.Digit: q = T("这张手写数字是「" + card.asked + "」吗？", "Is the handwritten digit a " + card.asked + "?"); break;
                case XgDeskKind.Poem: q = "「" + card.line.Substring(0, card.shown) + "」的下一个字是「" + card.askedChar + "」吗？"; break;
                case XgDeskKind.Text: q = T(XgMemes.Question(desk), XgMemes.QuestionEn(desk)) + "「" + VisibleText(card.question, card.questionEn) + "」"; break;
                case XgDeskKind.Meme: q = card.question + "（配字：" + card.line + "）"; break;
                default: q = card.question; break;
            }
            long id = card.id;
            var owner = Sim;
            bool replyEnglish = En;
            long previousId = commentCardId; var previousOwner = commentOwner;
            commentCardId = id; commentOwner = owner;
            // A comment nobody asked for: background, and simply skipped while the model is busy.
            bool queued = llm.ChatBackground(LingGuangV05.Desktop.LLM.LlmPersonas.CardComment(q, guess, confidence, Sim),
                LingGuangV05.Desktop.LLM.LlmPersonas.MaxTokens("lingguang", Sim), .9f,
                reply =>
                {
                    if (owner != Sim || id != commentCardId || id != displayedId || root == null || !Sim.FeatureVisible("lingguang-contact")) return;
                    if (replyEnglish != En) { commentCardId = -1; return; }
                    if (string.IsNullOrWhiteSpace(reply)) return;
                    comment = FormatCardComment(reply, q, Sim.S.stage, replyEnglish);
                    Refresh();
                });
            if (!queued) { commentCardId = previousId; commentOwner = previousOwner; }
        }

    }

    /// <summary>
    /// One upgrade row of the 标注台's right column: icon, name, level, "now → next" effect, a bar that fills as the
    /// wallet approaches the price, and a price button that turns green when affordable. The caller places the row
    /// (<see cref="Root"/>) in its column; it is 84 tall.
    /// </summary>
    public sealed class XgShopRow
    {
        public readonly RectTransform Root;
        public readonly XgBtn Button;
        readonly TMP_Text title, level, effect, glyphText;
        readonly RectTransform afford, progress;
        readonly Image bg;

        /// <summary>The icon's character, e.g. a question mark while the skill is still a mystery.</summary>
        public void SetGlyph(string glyph) { if (glyphText != null && glyphText.text != glyph) glyphText.text = glyph; }

        public XgShopRow(XgUi ui, RectTransform parent, string glyph, Color color, Action buy)
        {
            Root = Rect("Row" + glyph, parent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            bg = Panel(Root, new Color32(247, 249, 253, 255));
            var icon = XgUi.TopLeft("Icon", Root, 8, 8, 44, 36);
            Panel(icon, color).raycastTarget = false;
            var g = glyphText = ui.Text(Rect("Glyph", icon, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), glyph, 22, Color.white, TextAlignmentOptions.Center);
            g.fontStyle = FontStyles.Bold;
            level = ui.Text(XgUi.TopLeft("Lv", Root, 4, 46, 52, 16), "", 11, XgPalette.Muted, TextAlignmentOptions.Center);
            level.textWrappingMode = TextWrappingModes.NoWrap; level.overflowMode = TextOverflowModes.Ellipsis;
            var lv = XgUi.TopLeft("LevelBar", Root, 8, 64, 44, 4);
            progress = Bar(lv, "Fill", new Color(0, 0, 0, .08f), color);
            title = ui.Text(Rect("Title", Root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(62, -26), new Vector2(-8, -4)), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Ellipsis;
            effect = ui.Text(Rect("Effect", Root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(62, -76), new Vector2(-98, -26)), "", 12, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            effect.enableAutoSizing = true; effect.fontSizeMin = 9; effect.fontSizeMax = 12; effect.lineSpacing = -4;
            var barBack = Rect("Afford", Root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(62, 4), new Vector2(-8, 8));
            afford = Bar(barBack, "Fill", new Color(0, 0, 0, .06f), XgPalette.Gold);
            Button = ui.Button(Root, "", () => buy(), 14);
            Button.rt.anchorMin = Button.rt.anchorMax = new Vector2(1, 1);
            Button.rt.offsetMin = new Vector2(-92, -62); Button.rt.offsetMax = new Vector2(-8, -32);
            Button.label.fontStyle = FontStyles.Bold;
        }

        public void Show(bool on) { if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on); }

        /// <summary>Keeps the title clear of something placed over the row's top-right corner (the data-source switch).</summary>
        public void InsetTitle(float right) { var rt = title.rectTransform; rt.offsetMax = new Vector2(-right, rt.offsetMax.y); }

        public void Set(string name, string lv, string fx, string price, bool affordable, float toPrice, float levelFill)
        {
            title.text = name; level.text = lv; effect.text = fx;
            Button.Set(price, affordable, affordable ? XgPalette.Good : (Color?)null, affordable ? Color.white : (Color?)null);
            SetBar(afford, toPrice);
            SetBar(progress, levelFill);
            bg.color = affordable ? new Color32(255, 250, 235, 255) : new Color32(247, 249, 253, 255);
        }

        public void Celebrate(XgJuice fx, string text, Color color, int level)
        {
            fx.Knock(Root, .04f, new Vector2(0, 4), .3f);
            fx.Knock(Button.rt, .15f);
            fx.Shockwave(fx.At(Button.rt), color, 140, .3f, 6);
            fx.Burst(fx.At(Button.rt), 22, XgPalette.Gold, XgJuice.Shape.Yen, 260);
            fx.Float(fx.At(Root, new Vector2(-40, 30)), text, color, 24);
            fx.Play(XgJuice.Sfx.Id.Unlock, 1 + level * .03f);
            fx.Play(XgJuice.Sfx.Id.Coin, 1, .6f);
        }

        public void Deny(XgJuice fx)
        {
            fx.Knock(Button.rt, .05f, new Vector2(8, 0), .25f);
            fx.Play(XgJuice.Sfx.Id.Thud, 1.3f, .6f);
        }
    }
}
