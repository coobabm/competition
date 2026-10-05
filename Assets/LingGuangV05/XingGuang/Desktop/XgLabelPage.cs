using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 标注台: one desk per dataset, a yes/no card each. Right answers pay (× combo, ×3 on 前方高能) and add a sample;
    /// wrong ones pay nothing, break the combo and are discarded. Picture desks draw their cards in code.
    /// </summary>
    public sealed partial class XgLabelPage : XgPage
    {
        RectTransform tabsRow, paper, timerBar, timerFill;
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
        TMP_Text thresholdText, coopStats, brainStatus, specialInfo, amountLabel;
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
        readonly List<XgBtn> deskTabs = new List<XgBtn>();
        readonly List<string> deskIds = new List<string>();
        XgDigitGraphic digit;
        XgCaptchaGraphic captchaA, captchaB;
        XgFaceGraphic face;
        XgGoGraphic go;
        TMP_Text meta, poem, text, caption, question, suggestion, feedback, stats, steps, tip;
        XgBtn yes, no, toTrain;
        XgBtn[] amountBtns;
        XgShopRow raise, auto, pack;
        XgGlow paperGlow;
        Image paperImage;
        float feedbackTimer, redTimer;
        long commentCardId = -1;
        string comment;

        public RectTransform Paper => paper;
        string Desk => Sim.S.desk;
        XgCard CurrentCard => Sim.ReviewCard(Desk) ?? Sim.Card(Desk);
        bool EndingReady => view.Controller != null && view.Controller.EndingQuestionReady;
        // 0 ordinary/review, 1 research experiment, 2 final question, 3 chapter complete.
        int Mode => Sim.LabelPresentationMode(EndingReady);
        static string Safe(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
        static string VisibleText(string zh, string en) => En && !string.IsNullOrWhiteSpace(en) ? en : zh ?? "";
        static bool HasPreviewSurface(XgCard card) => !string.IsNullOrEmpty(card.patternA) ||
            ((card.kind == "long" || card.kind == "translation" || card.kind == "order" && card.bottleneckPreview) &&
             (!string.IsNullOrEmpty(card.sourceText) || !string.IsNullOrEmpty(card.candidateText)));

        public override void Build(RectTransform area)
        {
            root = Rect("label", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var left = ui.Card(root, "Desk", Vector2.zero, new Vector2(.6f, 1), Vector2.zero, new Vector2(-6, 0));
            tabsRow = Strip("Tabs", left, 8, 66, 12, 12);
            BuildFeed(left);
            BuildFrozenBanner(left);
            meta = ui.Text(Strip("Meta", left, 102, 24), "", 15, XgPalette.Muted, TextAlignmentOptions.Center);

            paper = Rect("Paper", left, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-240, -342), new Vector2(240, -132));
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

            timerBar = Rect("Timer", left, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-240, -352), new Vector2(240, -346));
            timerFill = Bar(timerBar, "Fill", new Color(0, 0, 0, .08f), XgPalette.Gold);

            question = ui.Text(Strip("Question", left, 356, 40), "", 26, XgPalette.Ink, TextAlignmentOptions.Center);
            question.fontStyle = FontStyles.Bold;
            question.enableAutoSizing = true; question.fontSizeMin = 14; question.fontSizeMax = 26;
            suggestion = ui.Text(Strip("Suggestion", left, 398, 40), "", 16, XgPalette.Accent, TextAlignmentOptions.Top);
            confidenceRoot = Strip("Confidence", left, 438, 4, 35, 35);
            confidenceFill = Bar(confidenceRoot, "Fill", XgPalette.Line, XgPalette.Accent);
            yes = ui.Button(left, "", () => OnAnswer(true), 30);
            no = ui.Button(left, "", () => OnAnswer(false), 30);
            yes.rt.anchorMin = yes.rt.anchorMax = new Vector2(.5f, 1); yes.rt.offsetMin = new Vector2(-226, -510); yes.rt.offsetMax = new Vector2(-12, -444);
            no.rt.anchorMin = no.rt.anchorMax = new Vector2(.5f, 1); no.rt.offsetMin = new Vector2(12, -510); no.rt.offsetMax = new Vector2(226, -444);
            feedback = ui.Text(Rect("Feedback", left, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 32), new Vector2(-16, 92)), "", 16, XgPalette.Good, TextAlignmentOptions.Center);
            stats = ui.Text(Rect("Stats", left, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 32)), "", 14, XgPalette.Muted, TextAlignmentOptions.Center);
            // Built after the card and the answer buttons so it covers them while a captcha waits.
            captchaCard = new XgCaptchaCard(ui, left, 104, 410, () => Sim, OnCaptchaAnswered);

            // Right: an incremental-game shop. Desk info on top, then upgrade rows with a buy-amount switch.
            var right = ui.Card(root, "Shop", new Vector2(.6f, 0), Vector2.one, new Vector2(6, 0), Vector2.zero);
            steps = ui.Text(Strip("Info", right, 10, 92, 14, 14), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            amountLabel = ui.Text(XgUi.TopLeft("AmountLabel", right, 14, 108, 120, 28), T("购买数量", "Buy amount"), 14, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            amountBtns = new XgBtn[3];
            string[] amounts = { "×1", "×10", T("最大", "Max") };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                amountBtns[i] = ui.Button(right, amounts[i], () => { view.BuyAmount = index == 0 ? 1 : index == 1 ? 10 : int.MaxValue; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 14);
                PlaceTopRight(amountBtns[i], 14 + (3 - i) * 66, 108, 62, 28);
            }
            raise = new XgShopRow(ui, right, 142, "薪", new Color32(240, 150, 30, 255), BuyRaise);
            auto = new XgShopRow(ui, right, 236, "自", XgPalette.Accent, BuyAuto);
            pack = new XgShopRow(ui, right, 330, "包", new Color32(18, 150, 140, 255), BuyPack);
            BuildSourceSwitch();
            BuildCollaborationPanel(right);
            BuildPlatformChip(right);
            specialInfo = ui.Text(Rect("ResearchInfo", right, Vector2.zero, Vector2.one, new Vector2(22, 70), new Vector2(-22, -130)), "", 19, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            specialInfo.gameObject.SetActive(false);
            tip = ui.Text(Rect("Tip", right, Vector2.zero, new Vector2(1, 0), new Vector2(14, 60), new Vector2(-14, 128)), "", 13, XgPalette.Muted, TextAlignmentOptions.BottomLeft);
            toTrain = ui.Button(right, "", () => { Sim.SelectedTrack = XgCatalog.Dataset(Desk).track; Sim.SetDataset(Sim.SelectedTrack, Desk); view.ShowTab("train"); }, 16);
            UiTip.Add(toTrain.rt, "带着这张桌的数据去训练页。", "Go to the training page with this desk's data.");
            UiTip.Add(yes.rt, "题面说得对，就点「是」。答对 +1 连击。", "Press Yes if the card is right. A right answer adds 1 to the combo.");
            UiTip.Add(no.rt, "题面说得不对，就点「否」。答错连击清零。", "Press No if the card is wrong. A wrong answer resets the combo.");
            UiTip.Add(raise.Root, "加薪：每升一级，手动标注每题拿得更多（每级至少 +0.5 倍）。\n每两级换一个头衔。", "Pay raise: every level pays more per hand-labelled card (at least +0.5× each).\nA new title every two levels.");
            UiTip.Add(auto.Root, () => Sim != null && Sim.AutoLabelHidden ? T("？？？\n也许有更省力的办法……", "???\nMaybe there is an easier way…")
                : T("自动答题：模型替你答题，每秒都有收入。\n需要任一模型的准确率先达到 60%；只有检查点达到 60% 的桌才会替你答。\n交出去的题会被摆渡众包抽检，错了要罚款，错太多会被举报冻结。", "Auto-answer: the model answers for you and earns every second.\nNeeds any model at 60% accuracy first; it only answers on desks whose checkpoint is at 60% or better.\nBodu Crowdsourcing spot-checks what it submits: wrong labels are fined, too many get the account frozen."));
            UiTip.Add(pack.Root, SourceTip);
            for (int i = 0; i < amountBtns.Length; i++) UiTip.Add(amountBtns[i].rt, "一次买几级：×1、×10，或者钱够的最大数量。", "How many levels per purchase: ×1, ×10, or as many as you can afford.");
            PlaceBottom(toTrain, 12, 40, 14, 14);
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
                case "mnist": what = T("看手写数字，判断是不是问的那个数。", "Look at the handwritten digit: is it the asked number?"); break;
                case "poems": what = T("看诗句，判断空格里是不是问的那个字。", "Read the couplet: is the blank the asked character?"); break;
                case "logic": what = T("逻辑和推理题。要动脑子，所以报酬最高。", "Logic and reasoning. It takes thought, so it pays best."); break;
                case "danmu": what = T("判断弹幕是不是在夸。", "Is this danmaku comment praise?"); break;
                case "spam": what = T("判断短信是不是垃圾或诈骗。", "Is this text message spam or a scam?"); break;
                default: what = d != null ? T(d.name, d.nameEn) : id; break;
            }
            double pay = desk?.pay ?? 1;
            return what + (Math.Abs(pay - 1) > 1e-9 ? T("\n报酬 ×", "\nPay ×") + N(pay, "0.0#") : "") + T("\n标得越多，这类数据越多，对应的模型越好练。", "\nThe more you label, the more data that model has to learn from.");
        }

        int Amount(int affordable) { return view.BuyAmount == int.MaxValue ? Math.Max(1, affordable) : view.BuyAmount; }

        void BuyRaise()
        {
            int before = Sim.RaiseLevel;
            string oldTitle = XgCatalog.RaiseTitle(before, En);
            int bought = Sim.BuyRaise(Host, Amount(Sim.AffordableRaises(Host)));
            if (bought == 0) { raise.Deny(Fx); return; }
            string title = XgCatalog.RaiseTitle(Sim.RaiseLevel, En);
            raise.Celebrate(Fx, T("加薪 ×", "Pay ×") + N(Sim.RaiseMultiplier, "0.0#") + (bought > 1 ? "  (+" + bought + ")" : ""), XgPalette.Money, Sim.RaiseLevel);
            if (title != oldTitle)
            {
                Fx.Flash(Color.white, .12f, .5f);
                Fx.Shake(5, .25f);
                Fx.Float(Fx.At(paper, new Vector2(0, 40)), T("升职：", "Promoted: ") + title, XgPalette.Gold, 32, 80, 1.3f, 1.6f);
                Fx.Burst(Fx.At(paper), 40, Color.white, XgJuice.Shape.Confetti, 420);
                Fx.Play(XgJuice.Sfx.Id.Fanfare, 1 + Sim.RaiseLevel * .02f, .8f);
            }
            view.Refresh(true);
        }

        void BuyAuto()
        {
            int bought = 0, want = view.BuyAmount == int.MaxValue ? XgCatalog.AutoMaxLevel : view.BuyAmount;
            while (bought < want && Sim.CanBuyAuto(Desk, out _) && Host.Money + 1e-9 >= XgCatalog.AutoCost(Sim.AutoLevel(Desk)) && Sim.BuyAuto(Desk, Host)) bought++;
            if (bought == 0) { Sim.BuyAuto(Desk, Host); auto.Deny(Fx); return; }
            auto.Celebrate(Fx, T("自动答题 Lv ", "Auto Lv ") + Sim.AutoLevel(Desk), XgPalette.Accent, Sim.AutoLevel(Desk));
            view.Refresh(true);
        }

        void BuyPack()
        {
            // Junk packs, story data and crowd tasks go through XgLabelPage.Data.cs; the public pack through the tree.
            if (BuySource()) return;
            var n = XgCatalog.Node(Desk + ".pack");
            if (n == null || !view.Tree.Buy(n, pack.Button.rt)) { pack.Deny(Fx); return; }
            view.Refresh(true);
        }

        T Square<T>(string name, float size) where T : MaskableGraphic
        {
            var g = Rect(name, paper, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-size / 2, -size / 2), new Vector2(size / 2, size / 2)).gameObject.AddComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        public void RebuildTabs()
        {
            foreach (var b in deskTabs) UnityEngine.Object.Destroy(b.rt.gameObject);
            deskTabs.Clear(); deskIds.Clear();
            var list = new List<XgDesk>(Sim.OpenDesks());
            int count = list.Count;
            int perRow = 7;
            float w = 82, h = 30, gap = 4;
            for (int i = 0; i < count; i++)
            {
                var d = list[i];
                string id = d.id;
                var b = ui.Button(tabsRow, "", () => Select(id), 15);
                PlaceTopLeft(b, (i % perRow) * (w + gap), (i / perRow) * (h + gap), w, h);
                b.label.enableAutoSizing = true; b.label.fontSizeMin = 10; b.label.fontSizeMax = 15;
                b.label.overflowMode = TextOverflowModes.Ellipsis;
                deskTabs.Add(b); deskIds.Add(id);
                UiTip.Add(b.rt, () => DeskHelp(id));
            }
            Refresh();
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
            string truthText = card.truth ? T("是", "Yes") : T("否", "No");
            string why = T(card.why, card.whyEn);
            string source = card.source;
            var button = answer ? yes.rt : no.rt;
            var r = queued ? Sim.AnswerQueued(card.id, answer, Host) : Sim.Answer(desk, answer, Host);
            if (queued && !r.accepted) { Refresh(); return; }
            Vector2 at = Fx.At(paper);
            if (r.correct)
            {
                feedback.color = XgPalette.Good;
                feedback.text = "<color=#2F9E44>✓ " + T("对了", "Right") + "  +¥" + N(r.pay, "0.00") + "</color>"
                    + (r.combo > 1 ? "  <color=#7A5AF8>" + T("连击 ×", "combo ×") + N(Sim.ComboMultiplier, "0.00") + "</color>" : "")
                    + (why.Length > 0 && card.question.Length > 0 ? "\n<size=13><color=#68748C>" + why + "</color></size>" : "");
                // A user log from the data flywheel was labelled: it counts as several samples.
                if (!queued && r.samples > 1) feedback.text += "  <color=#0F8C7E>" + T("用户日志 +", "user log +") + r.samples + T(" 样本", " samples") + "</color>";
                feedbackTimer = 3f;
                Fx.Knock(button, .12f);
                Fx.Knock(paper, .04f, new Vector2(answer ? 26 : -26, 6), .22f);
                Fx.Float(Fx.At(button, new Vector2(0, 40)), "+¥" + N(r.pay, "0.00"), XgPalette.Money, 24 + Mathf.Min(10, r.combo * .3f));
                Fx.Burst(at, 6 + Mathf.Min(14, r.combo / 3), XgPalette.Gold, XgJuice.Shape.Yen, 240);
                Fx.Play(XgJuice.Sfx.Id.Ding, 1 + Mathf.Min(1, r.combo * .025f));
                if (r.bounty)
                {
                    Fx.Shockwave(at, new Color32(123, 47, 247, 255), 260, .4f, 12);
                    Fx.Float(at + new Vector2(0, 90), T("悬赏到手 ×", "Bounty ×") + N(XgSim.BountyMultiplier, "0"), new Color32(123, 47, 247, 255), 30);
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
                    Fx.Float(at + new Vector2(0, 72), T("纠错！连击 +2", "Correction! combo +2"), XgPalette.Accent, 28);
                    feedback.text += T("  · 纠错", "  · corrected") + (r.samples == 2 ? T(" · 难例 +2 样本", " · hard case +2 samples") : "");
                    Fx.Play(XgJuice.Sfx.Id.Unlock, 1.15f, .55f);
                }
                else if (r.trick)
                {
                    Fx.Float(at + new Vector2(0, 60), T("老司机！连击 +2", "Old driver! combo +2"), new Color32(150, 90, 255, 255), 28);
                    Fx.Flash(new Color(.6f, .4f, 1f), .1f, .35f);
                }
                if (r.combo > 0 && r.combo % 10 == 0 && Array.IndexOf(XgCatalog.ComboTiers, r.combo) < 0)
                {
                    Fx.Flash(Color.white, .08f, .4f);
                    Fx.Float(Fx.At(view.ComboChip, new Vector2(-20, -50)), T("连击 ×", "combo ×") + N(Sim.ComboMultiplier, "0.00"), XgPalette.Gold, 22);
                }
            }
            else
            {
                feedback.color = XgPalette.Bad;
                feedback.text = "<color=#D63031>× " + (r.timeout ? T("超时了。", "Too late. ") : "") + T("应该是「", "It was \"") + truthText + T("」。没有钱，连击清零，这条作废", "\". No pay, combo reset, label discarded") + "</color>"
                    + (why.Length > 0 ? "\n<size=14><color=#5B6478>" + why + "</color></size>" : "")
                    + (source.Length > 0 ? "\n<size=13><color=#8A94AA>" + T("出处：", "Source: ") + Safe(source) + "</color></size>" : "")
                    + (Sim.S.stage >= 5 && card.explanation.Length > 0 ? "\n<size=13><color=#4054C4>" + T(AppNames.AiZh + "：", AppNames.AiEn + ": ") + Safe(T(card.explanation, card.explanationEn)) + "</color></size>" : "");
                feedbackTimer = why.Length > 0 ? 7f : 3f;
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
            Fx.Float(Fx.At(paper, new Vector2(0, 60)), T("超时！", "Too late!"), XgPalette.Bad, 30);
            feedback.color = XgPalette.Bad;
            feedback.text = "<color=#D63031>" + T("超时：没有钱，连击清零", "Too late: no pay, combo reset") + "</color>";
            feedbackTimer = 2.5f;
            Wrong(Fx.At(paper));
            Refresh();
        }

        public override void Tick(float dt)
        {
            if (feedbackTimer > 0) { feedbackTimer -= dt; if (feedbackTimer <= 0) feedback.text = ""; }
            if (redTimer > 0) redTimer -= dt;
            AnimateFeed(dt);
            if (Mode != displayedMode || displayedSim != Sim) { Refresh(); return; }
            if (Sim.QualityFrozen != wasFrozen || Sim.CaptchaPauseLeft > 0 != wasPaused || Sim.CaptchaPending != wasCaptcha) { Refresh(); return; }
            if ((wasFrozen || wasPaused) && Mode == 0) RefreshFrozenBanner();
            captchaCard.Tick(view.Visible && view.Tab == "label");
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
                // Rebinding can remove desks. Rebuild before using stale tab callbacks.
                var open = Sim.OpenDesks();
                bool changed = open.Count != deskIds.Count;
                for (int i = 0; !changed && i < open.Count; i++) changed = open[i].id != deskIds[i];
                if (changed) { RebuildTabs(); return; }
            }
            string desk = Desk;
            var info = XgCatalog.Desk(desk);
            var d = XgCatalog.Dataset(desk);
            bool special = Mode != 0;
            displayedSim = Sim; displayedMode = Mode;
            RefreshSpecialVisibility(special);
            RefreshFeed();
            if (special) { RefreshSpecial(); return; }
            var card = CurrentCard;
            if (card.id != displayedId) { displayedId = card.id; translationTime = 0; translationFadeCount = -1; }
            for (int i = 0; i < deskTabs.Count; i++)
            {
                string id = deskIds[i];
                if (id == null) { deskTabs[i].Set("？？", true, XgPalette.Disabled, XgPalette.Muted); continue; }
                var di = XgCatalog.Desk(id);
                bool on = id == desk;
                int waiting = Sim.ReviewCount(id);
                deskTabs[i].Set(T(di.name, di.nameEn) + (waiting > 0 ? " " + waiting : ""), true, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }

            var kind = info.kind;
            bool dedicatedPreview = HasPreviewSurface(card);
            bool textOverride = card.kind == "combo" || card.kind == "long" || card.kind == "attention" || card.kind == "translation" || card.kind == "order" && card.bottleneckPreview;
            digit.gameObject.SetActive(kind == XgDeskKind.Digit && !textOverride);
            poem.gameObject.SetActive(kind == XgDeskKind.Poem);
            text.gameObject.SetActive(textOverride || kind == XgDeskKind.Logic || kind == XgDeskKind.Text);
            captchaA.gameObject.SetActive(kind == XgDeskKind.Captcha);
            captchaB.gameObject.SetActive(kind == XgDeskKind.Captcha && card.shown >= 0);
            face.gameObject.SetActive(kind == XgDeskKind.Meme);
            caption.gameObject.SetActive(kind == XgDeskKind.Meme);
            go.gameObject.SetActive(kind == XgDeskKind.Go);

            int level = card.level > 0 ? card.level : Sim.LevelOf(desk), max = XgSim.MaxLevelOf(desk);
            string stars = max > 1 ? "  <color=#E0A800>" + new string('★', Math.Min(level, max)) + "</color><color=#C8CEDA>" + new string('☆', Math.Max(0, max - level)) + "</color>" : "";
            string cat = card.category.Length > 0 ? T(card.category, card.categoryEn) : T(info.name, info.nameEn);
            meta.text = (card.gold ? "<color=#E0A000><b>" + T("前方高能 ×3", "HYPE ×3") + "</b></color>  " : "")
                + (card.bounty ? "<color=#7B2FF7><b>" + T("专家题悬赏 ×", "Expert bounty ×") + N(XgSim.BountyMultiplier, "0") + "</b></color>  " : "") + (Sim.InDuel && desk == "meme" ? "<color=#D63031><b>" + T("斗图中 ", "Battle ") + (XgSim.DuelLength - Sim.DuelLeft + 1) + "/" + XgSim.DuelLength + "</b></color>  " : "")
                + (Sim.ReviewCard(desk) != null ? "<color=#B36A00>" + T("待复核 · ", "Review · ") + "</color>" + QcTags(card) : "") + cat + stars + "  <color=#E86E14>¥" + N(Sim.ManualPayFor(desk, level) * (card.bounty ? XgSim.BountyMultiplier : 1), "0.00") + T("/题", "/card") + "</color>";

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
                case XgDeskKind.Logic:
                    text.text = T(card.question, card.questionEn);
                    question.text = T("对吗？", "True?");
                    break;
                case XgDeskKind.Text:
                    text.text = card.question;
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
                text.fontSizeMax = card.kind == "long" ? 24 : 30;
                question.text = card.kind == "attention" ? T("它看对了吗？", "Did it focus on the right clue?") : T("对吗？", "True?");
            }
            captionFocus.gameObject.SetActive(card.kind == "caption");
            if (card.kind == "caption")
            {
                int region = Math.Max(0, card.attentionRegion) % 4;
                captionFocus.anchorMin = new Vector2((region % 2) * .5f, (region / 2) * .5f);
                captionFocus.anchorMax = captionFocus.anchorMin + new Vector2(.5f, .5f);
                captionFocus.offsetMin = new Vector2(3, 3); captionFocus.offsetMax = new Vector2(-3, -3);
                meta.text += "  <size=12>" + T("关注区域：教学示意", "Reported region: illustration") + "</size>";
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
            suggestion.text = hasGuess
                ? T(AppNames.AiZh + "：", AppNames.AiEn + ": ") + (guess ? T("是？ ", "yes? ") : T("否？ ", "no? ")) + XgSim.Pct(confidence)
                    + "  (" + (queued && card.judgeSource == "brain" ? T("本地模型", "local model") : T("检查点估计", "checkpoint estimate")) + ")"
                    + (queued && !string.IsNullOrEmpty(card.judgeFailure) ? " · " + T("大脑离线", "brain offline") : "")
                : T("还没有模型。标够 " + XgCatalog.SamplesToTrain + " 条，去「训练」按「训练一轮」。", "No model yet. Label " + XgCatalog.SamplesToTrain + ", then press Train in the Train tab.");
            if (hasGuess && view.Visible && view.Tab == "label" && suggestion.gameObject.activeInHierarchy && card.kind == "combo" && card.judgeSource != "brain")
                Sim.MarkComboSuggestionDisplayed(card, guess);
            if (hasGuess && Sim.FeatureVisible("lingguang-contact"))
            {
                if (card.id != commentCardId || commentOwner != Sim)
                {
                    comment = queued ? XgSpeechPolicy.Constrain(T("这个……我拿不准。你来。", "This one… I am unsure. Your turn."), Sim.S.stage, En) : null;
                    RequestComment(desk, card, guess, confidence);
                }
                if (!string.IsNullOrEmpty(comment)) suggestion.text += "\n<size=14><color=#4054C4>" + T(AppNames.AiZh + "：「", AppNames.AiEn + ": “") + Safe(comment) + T("」", "”") + "</color></size>";
            }
            confidenceRoot.gameObject.SetActive(hasGuess);
            SetBar(confidenceFill, (float)confidence);
            confidenceFill.GetComponent<UnityEngine.UI.Image>().color = confidence < Sim.S.coopThreshold ? XgPalette.Gold : XgPalette.Good;
            yes.label.fontSize = no.label.fontSize = 30;
            yes.Set(T("是", "Yes"), true, new Color32(59, 91, 219, 255), Color.white);
            no.Set(T("否", "No"), true, new Color32(84, 96, 122, 255), Color.white);
            int total = Sim.S.handCorrect + Sim.S.handWrong;
            stats.text = T("亲手标对 ", "Hand-labelled ") + Sim.S.handCorrect + (total > 0 ? T(" · 正确率 ", " · accuracy ") + XgSim.Pct((double)Sim.S.handCorrect / total) : "")
                + T(" · 最高连击 ×", " · best combo ×") + Sim.S.bestCombo;

            if (card.kind == "translation") { translationFadeCount = -1; AnimateTranslation(card, 0); }
            XgLabelGhost.For(paper)?.Show(Sim, desk, card); // the model's faint guess before the auto-labelling idea
            RefreshSteps(desk, d);
            RefreshCollaboration();
            RefreshPlatform();
        }

        void RefreshSteps(string desk, XgDataset d)
        {
            double samples = Sim.Samples(d.id);
            double best = Sim.BestAcc(d.id);
            var sb = new StringBuilder();
            sb.Append("<size=17><b>").Append(T(d.name, d.nameEn)).Append("</b></size>  <color=#68748C>").Append(T("样本 ", "samples ")).Append(Samples(samples));
            if (best > 0) sb.Append(T(" · 最佳 ", " · best ")).Append(XgCatalog.GradeNames[XgSim.Grade(Sim.BestScore(d.id))]).Append(" ").Append(XgSim.Pct(best));
            sb.Append("</color>\n");
            int dl = Sim.LevelOf(desk), dmax = XgSim.MaxLevelOf(desk);
            if (dmax > 1)
            {
                int per = XgSim.LabelsPerLevel(desk);
                sb.Append("<color=#68748C>").Append(T("难度 ", "Difficulty ")).Append(dl).Append("/").Append(dmax)
                  .Append(dl < dmax ? T(" · 再标对 " + (per - (int)Sim.Labels(desk) % per) + " 条升级（越难越值钱）", " · " + (per - (int)Sim.Labels(desk) % per) + " more to level up") : "").Append("</color>\n");
            }
            if ((XgMemes.IsTextDesk(desk) || desk == "meme") && Sim.Topic.Length > 0)
                sb.Append("<color=#E86E14>").Append(T("今日热词：", "Today's topic: ")).Append(Sim.Topic).Append("</color>\n");
            // 新题型 chip (XgSim.Market.cs): this month's meme costs the checkpoint accuracy here until hand labels or epochs catch up.
            string drift = Sim.MemeDriftChip(desk);
            if (drift.Length > 0) sb.Append("<color=#D63031><b>").Append(drift).Append("</b></color>\n");
            string next = samples < XgCatalog.SamplesToTrain ? T("下一步：再标对 " + (XgCatalog.SamplesToTrain - (int)samples) + " 条就能训练", "Next: " + (XgCatalog.SamplesToTrain - (int)samples) + " more labels to unlock training")
                : best <= 0 ? T("下一步：去「训练」按「训练一轮」", "Next: press Train in the Train tab")
                : best < XgCatalog.AutoMinAccuracy ? (Sim.AutoLabelHidden ? T("下一步：接着练，准确率还能更高", "Next: keep training; accuracy can go higher")
                    : T("下一步：练到 " + XgSim.Pct(XgCatalog.AutoMinAccuracy) + " 就能买自动答题", "Next: reach " + XgSim.Pct(XgCatalog.AutoMinAccuracy) + " to unlock auto-answer")) : "";
            if (next.Length > 0) sb.Append("<color=#3B5BDB>").Append(next).Append("</color>");
            string logs = XgDataUi.LogLine(Sim, desk);
            if (logs.Length > 0) sb.Append("\n<color=#0F8C7E>").Append(logs).Append(T(" · 亲手标一条 = ", " · one by hand = ")).Append(N(XgSim.LogHandSamples, "0")).Append(T(" 样本", " samples")).Append("</color>");
            steps.text = sb.ToString();

            for (int i = 0; i < amountBtns.Length; i++)
            {
                bool on = (i == 0 && view.BuyAmount == 1) || (i == 1 && view.BuyAmount == 10) || (i == 2 && view.BuyAmount == int.MaxValue);
                amountBtns[i].Set(amountBtns[i].label.text, true, on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }

            // 加薪
            int rl = Sim.RaiseLevel;
            bool maxed = rl >= XgCatalog.RaiseMax;
            int count = view.BuyAmount == int.MaxValue ? Math.Max(1, Sim.AffordableRaises(Host)) : view.BuyAmount;
            double cost = Sim.RaiseCostFor(count, out int levels);
            double nextMult = XgCatalog.RaiseMultipliers[Math.Min(XgCatalog.RaiseMax, rl + Math.Max(1, levels))];
            raise.Set(T("加薪", "Pay raise") + " · " + XgCatalog.RaiseTitle(rl, En), "Lv " + rl + "/" + XgCatalog.RaiseMax,
                maxed ? T("人工标注 ×" + N(Sim.RaiseMultiplier, "0.#") + "（满级）", "Hand labelling ×" + N(Sim.RaiseMultiplier, "0.#") + " (max)") : T("人工标注 ×", "Hand labelling ×") + N(Sim.RaiseMultiplier, "0.0#") + " → <b>×" + N(nextMult, "0.0#") + "</b> <color=#1E9E5A>(+" + N(nextMult - Sim.RaiseMultiplier, "0.0#") + ")</color>" + (levels > 1 ? "  <color=#68748C>(+" + levels + ")</color>" : ""),
                maxed ? T("满级", "Max") : "¥" + Money(cost), !maxed && Host.Money + 1e-9 >= cost, maxed ? 1 : (float)(Host.Money / cost), rl / (float)XgCatalog.RaiseMax);

            // 自动答题（本桌）
            int al = Sim.AutoLevel(desk);
            bool canAuto = Sim.CanBuyAuto(desk, out string why);
            double acost = XgCatalog.AutoCost(al);
            // Until the protagonist has the idea (XgSim.Epiphany.cs) the row is a 「？？？」 with a hint.
            auto.SetGlyph(Sim.AutoLabelHidden ? "？" : "自");
            auto.Set(Sim.AutoLabelHidden ? T("？？？", "???") : T("自动答题（全局）", "Auto-answer (global)"), "Lv " + al + "/" + XgCatalog.AutoMaxLevel,
                al > 0 && !Sim.AutoLabelHidden && !Sim.EligibleDesk(desk) ? "<color=#D63031>" + T("这张桌的检查点不到 60%，它不替你答", "This desk's checkpoint is under 60%: it won't answer here") + "</color>"
                : canAuto || al > 0 ? T("每秒 ", "") + N(Sim.AutoCardsPerSecond(desk, Host), "0.0") + T(" 张 → ", " cards/s → ") + "<b>" + N((al + 1) * XgCatalog.AutoRatePerLevel * Math.Min(1, Math.Max(0, Host.Compute)), "0.0") + "</b>" + T("  · 约 ¥", "  · ~¥") + N(Sim.AutoIncome(desk, Host), "0.00") + T("/秒", "/s")
                    : (Sim.AutoLabelHidden ? "<color=#68748C>" : "<color=#D63031>") + (why ?? "") + "</color>",
                canAuto ? "¥" + Money(acost) : al >= XgCatalog.AutoMaxLevel ? T("满级", "Max") : T("未解锁", "Locked"), canAuto && Host.Money >= acost, canAuto ? (float)(Host.Money / acost) : 0, al / (float)XgCatalog.AutoMaxLevel);

            // 完整数据包
            var packNode = XgCatalog.Node(desk + ".pack");
            pack.Show(packNode != null);
            // Other sources of this desk's data (junk pack, story data, crowd task) take over the row when picked.
            if (packNode != null && !RefreshSourceRow(desk))
            {
                var st = Sim.Status(packNode, Host);
                pack.Set(T("完整数据包", "Full data pack"), st == XgSim.NodeStatus.Owned ? T("已拥有", "Owned") : "",
                    st == XgSim.NodeStatus.Locked ? "<color=#D63031>" + Sim.Why(packNode, Host) + "</color>" : T("一次补足 ", "Adds ") + Samples(d.samples),
                    st == XgSim.NodeStatus.Owned ? "✓" : "¥" + Money(packNode.cost), st == XgSim.NodeStatus.Buyable, st == XgSim.NodeStatus.Owned ? 1 : (float)(Host.Money / packNode.cost), st == XgSim.NodeStatus.Owned ? 1 : 0);
            }
            double kind = XgCatalog.Desk(desk)?.pay ?? 1;
            string kindZh = Math.Abs(kind - 1) > 1e-9 ? " · 题型 ×" + N(kind, "0.0#") : "", kindEn = Math.Abs(kind - 1) > 1e-9 ? " · card type ×" + N(kind, "0.0#") : "";
            tip.text = T("连击 ×" + N(Sim.ComboMultiplier, "0.00") + " · 加薪 ×" + N(Sim.RaiseMultiplier, "0.0#") + kindZh + " · 难度 ×" + N(1 + .5 * (dl - 1), "0.0") + "  =  每题 ¥" + N(Sim.ManualPayFor(desk, dl) * Sim.ComboMultiplier, "0.00")
                + "\n答对 +1 连击，题桌之间不断；答错、超时、NaN 清零。",
                "Combo ×" + N(Sim.ComboMultiplier, "0.00") + " · raise ×" + N(Sim.RaiseMultiplier, "0.0#") + kindEn + " · difficulty ×" + N(1 + .5 * (dl - 1), "0.0") + " = ¥" + N(Sim.ManualPayFor(desk, dl) * Sim.ComboMultiplier, "0.00") + " per card");
            bool trainVisible = view.Controller == null || view.Controller.FeatureVisible("train");
            toTrain.Show(trainVisible);
            toTrain.Set(T("去训练 →", "Go train →"), trainVisible && Sim.TrainingUnlocked(d.track));
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
            previewHeading.text = (card.bottleneckPreview ? T("瓶颈预览 · ", "Bottleneck preview · ") : "")
                + T("教学示意，不是真实模型内部状态", "Teaching illustration, not internal model state");
            previewSourceScroll.gameObject.SetActive(!grid || !two);
            previewCandidateScroll.gameObject.SetActive(!grid || !two);
            string source = VisibleText(card.sourceText, card.sourceTextEn);
            string candidate = VisibleText(card.candidateText, card.candidateTextEn);
            previewSource.fontSize = previewCandidate.fontSize = 14;
            if (grid && two)
            {
                PreviewArea(previewGraphic.rectTransform, new Vector2(0, .03f), new Vector2(1, .84f));
                previewHeading.text += T(" · 左 A / 右 B", " · A left / B right");
            }
            else if (grid)
            {
                PreviewArea(previewGraphic.rectTransform, new Vector2(0, .08f), new Vector2(.40f, .84f));
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(.43f, .48f), new Vector2(1, .86f));
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(.43f, .03f), new Vector2(1, .45f));
                previewSource.text = "<b>" + T("固定摘要", "Fixed summary") + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + T("候选描述", "Candidate description") + "</b>\n" + Safe(candidate);
            }
            else if (memory)
            {
                bool hasCandidate = !string.IsNullOrEmpty(candidate);
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(0, hasCandidate ? .54f : .24f), new Vector2(1, .86f));
                previewCandidateScroll.gameObject.SetActive(hasCandidate);
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(0, .24f), new Vector2(1, .51f));
                previewSource.text = "<b>" + T("上文（滚动阅读）", "Context (scroll to read)") + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + T("下一句", "Next sentence") + "</b>\n" + Safe(candidate);
                AnimateMemory(card, 0);
            }
            else
            {
                PreviewArea((RectTransform)previewSourceScroll.transform, new Vector2(0, .04f), new Vector2(.49f, .86f));
                PreviewArea((RectTransform)previewCandidateScroll.transform, new Vector2(.51f, .04f), new Vector2(1, .86f));
                previewSource.text = "<b>" + (card.kind == "order" ? T("句子 A", "Sentence A") : T("原文（滚动阅读）", "Source (scroll to read)")) + "</b>\n" + Safe(source);
                previewCandidate.text = "<b>" + (card.kind == "order" ? T("句子 B", "Sentence B") : T("候选译文（滚动阅读）", "Candidate (scroll to read)")) + "</b>\n" + Safe(candidate);
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
            previewMemoryText.text = T("检查点记忆演示 · ", "Checkpoint memory illustration · ") + arch.ToUpperInvariant()
                + T(" · 距离 ", " · distance ") + distance + " · " + XgSim.Pct(retention);
        }

        void BuildFeed(RectTransform left)
        {
            feedRoot = Strip("AutomaticFeed", left, 76, 24, 14, 14);
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
            coopPanel = Strip("Collaboration", right, 428, 145, 14, 14);
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
            tip.gameObject.SetActive(!enabled && Mode == 0);
            if (!enabled) return;
            refreshingThreshold = true;
            threshold.SetValueWithoutNotify((float)(Sim.S.coopThreshold * 100));
            refreshingThreshold = false;
            thresholdText.text = T("把握阈值 ", "Confidence threshold ") + XgSim.Pct(Sim.S.coopThreshold)
                + T("  · 把握不是准确率", "  · not accuracy");
            var st = Sim.CollaborationStats();
            string share = st.total == 0 ? "—" : XgSim.Pct(st.automaticFraction);
            string accuracy = st.automatic == 0 ? "—" : XgSim.Pct(st.automaticAccuracy);
            coopStats.text = T("近 100 题 · 自动 ", "Last 100 · auto ") + share + T(" · 准确率 ", " · accuracy ") + accuracy
                + "\n" + T("噪声标签 ", "Noisy labels ") + N(Sim.NoiseTotal, "0") + T(" · 待复核 ", " · review ") + Sim.ReviewCount() + "/" + XgSim.ReviewCapacity
                + (Sim.ReviewFull ? " <color=#B36A00>" + T("自动已暂停", "automation paused") + "</color>" : "");
            bool online = Sim.BrainOnline && string.IsNullOrEmpty(Sim.BrainStatus);
            string source = Sim.Has("label.brain") && XgSim.IsBrainDesk(Desk)
                ? online ? T("本地模型在线", "Local model online") : T("大脑离线 · 回退检查点估计", "Brain offline · checkpoint fallback")
                : T("检查点估计 · 非真实模型推理", "Checkpoint estimate · simulated inference");
            int n = Sim.BrainJudgmentCount(Desk);
            brainStatus.text = source + (n > 0 ? "\n" + T("本桌大脑近 ", "Local model, last ") + n + T(" 题：", " on this desk: ") + XgSim.Pct(Sim.BrainAccuracy(Desk)) : "\n" + T("模型判断只供参考；代码负责结算。", "Model judgments are advisory; game rules settle rewards."));
            RefreshRecheck();
        }

        // ───────────── 摆渡众包 quality control ─────────────

        void BuildPlatformChip(RectTransform right)
        {
            // Shares the buy-amount row: the chip replaces the "Buy amount" caption once the platform is active.
            platformChip = Strip("PlatformChip", right, 105, 34, 14, 14 + 3 * 66 + 6);
            platformImage = Panel(platformChip, new Color32(232, 238, 255, 255));
            platformText = ui.Text(Rect("Text", platformChip, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-4, 0)), "", 11, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            platformText.enableAutoSizing = true; platformText.fontSizeMin = 10; platformText.fontSizeMax = 13; platformText.lineSpacing = -14;
            platformText.overflowMode = TextOverflowModes.Ellipsis;
            UiTip.Add(platformChip, PlatformTip);
            platformChip.gameObject.SetActive(false);
        }

        void BuildFrozenBanner(RectTransform left)
        {
            frozenBanner = Strip("QcFrozenBanner", left, 76, 24, 14, 14);
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
            if (card.recheck) tags += "<color=#7A5AF8>" + T("旧数据复核 · ", "Re-check · ") + "</color>";
            if (Sim.TrapRecognized(card)) tags += "<color=#2932E1><b>" + T("眼熟", "Familiar") + "</b></color> · ";
            return tags;
        }

        void OnCaptchaAnswered(bool right)
        {
            var at = Fx.At(captchaCard.Root);
            if (right)
            {
                Fx.Play(XgJuice.Sfx.Id.Unlock);
                Fx.Float(at, T("验证通过 · 信用 +1", "Verified · credit +1"), XgPalette.Good, 24);
            }
            else
            {
                Fx.Shake(4, .25f);
                Fx.Play(XgJuice.Sfx.Id.Thud, 1.1f, .7f);
                Fx.Float(at, T("验证失败 · 自动标注暂停 2 分钟", "Wrong · auto labelling paused 2 min"), XgPalette.Bad, 22);
            }
            view.Refresh(true);
        }

        void RefreshRecheck()
        {
            int can = Sim.RecheckableNoise(Desk), waiting = Sim.PendingRechecks(Desk);
            bool show = can > 0 || waiting > 0;
            recheckBtn.Show(show);
            if (!show) return;
            recheckBtn.Set(can > 0 ? T("人工复核旧数据 ×", "Re-check old ×") + can : T("复核中 ", "Re-checking ") + waiting, can > 0, (Color)new Color32(122, 90, 248, 255), Color.white);
        }

        void OnRecheck()
        {
            if (Sim.RecheckNoise(Desk) == 0) { Fx.Knock(recheckBtn.rt, .05f, new Vector2(8, 0), .25f); return; }
            Fx.Play(XgJuice.Sfx.Id.Swoosh, 1.1f, .6f);
            view.Refresh(true);
        }

        void RefreshPlatform()
        {
            bool active = Sim.QualityActive && Mode == 0;
            wasFrozen = Sim.QualityFrozen; wasPaused = Sim.CaptchaPauseLeft > 0; wasCaptcha = Sim.CaptchaPending;
            platformChip.gameObject.SetActive(active);
            amountLabel.gameObject.SetActive(!active && Mode == 0);
            frozenBanner.gameObject.SetActive(active && (wasFrozen || wasPaused));
            captchaCard.Show(active && wasCaptcha);
            if (active && wasCaptcha) captchaCard.Refresh();
            if (!active) return;
            var tier = Sim.CreditTier;
            Color ink = tier == XgCreditTier.Gold ? new Color32(150, 98, 0, 255) : tier == XgCreditTier.Normal ? new Color32(41, 50, 225, 255) : new Color32(200, 36, 36, 255);
            platformImage.color = tier == XgCreditTier.Gold ? new Color32(255, 243, 210, 255) : tier == XgCreditTier.Normal ? new Color32(232, 236, 255, 255) : new Color32(255, 225, 225, 255);
            platformText.color = ink;
            string rate = Sim.SpotChecks == 0 ? T("还没有抽检", "no spot checks yet")
                : T("合格率 ", "pass ") + N(Math.Floor(Sim.PassRate * 100 + 1e-9), "0") + "%" + T("（近 " + Sim.SpotChecks + " 次）", " (" + Sim.SpotChecks + " checks)");
            if (Sim.QualityWarning) rate = "<color=#D63031>" + rate + "</color>";
            platformText.text = "<b>" + T("摆渡众包", "Bodu Crowd") + "</b> · " + T("信用 ", "credit ") + N(Math.Floor(Sim.Credit), "0") + " " + TierName + "\n" + rate;
            if (wasFrozen || wasPaused) RefreshFrozenBanner();
        }

        void RefreshFrozenBanner()
        {
            if (!Sim.QualityFrozen)
            {
                // After a failed captcha: a plain pause, nothing to appeal.
                appealBtn.Show(false);
                frozenText.text = T("人机验证未通过：自动标注暂停 ", "Captcha failed: auto labelling paused ") + XgSim.FreezeClock(Sim.CaptchaPauseLeft);
                return;
            }
            appealBtn.Show(true);
            frozenText.text = T("账号被举报冻结 ", "Account frozen ") + XgSim.FreezeClock(Sim.FreezeSecondsLeft) + " · " + Sim.ReportReasonText(Sim.LastReportReason);
            bool can = Sim.CanAppeal(Host, out _);
            appealBtn.Set(T("申诉 ¥", "Appeal ¥") + Money(Sim.AppealCost(Host)), can, Color.white, (Color)new Color32(190, 30, 30, 255));
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
            Fx.Float(Fx.At(frozenBanner), T("账号已解冻", "Unfrozen"), XgPalette.Good, 22);
            view.Refresh(true);
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
                feedTexts[i].text = fined ? "× " + T("抽检不合格 −¥", "Failed check −¥") + N(record.fine, "0.##")
                    : (record.audited ? "↖ " : record.correct ? "✓ " : "× ") + T(info.name, info.nameEn);
                feedImages[i].color = fined ? new Color32(255, 205, 205, 255) : record.audited ? new Color32(255, 235, 166, 255) : record.correct ? new Color32(221, 245, 229, 255) : new Color32(252, 222, 222, 255);
                feedTexts[i].color = fined ? new Color32(190, 20, 20, 255) : record.audited ? new Color32(140, 84, 10, 255) : record.correct ? XgPalette.Good : XgPalette.Bad;
                feedTexts[i].fontStyle = fined ? FontStyles.Bold : FontStyles.Normal;
                if (record.cardId > lastFeedId && record.audited && root.gameObject.activeInHierarchy)
                {
                    int tab = deskIds.IndexOf(record.dataset);
                    if (tab >= 0 && !Sim.S.reduceFx)
                    {
                        var flight = auditFlights[i];
                        var parent = (RectTransform)flight.rect.parent;
                        flight.from = parent.InverseTransformPoint(feedRects[i].TransformPoint(feedRects[i].rect.center));
                        flight.to = parent.InverseTransformPoint(deskTabs[tab].rt.TransformPoint(deskTabs[tab].rt.rect.center));
                        flight.label.text = T("抽检 ↖ 待复核", "Audit ↖ review");
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
            tabsRow.gameObject.SetActive(!special);
            amountLabel.gameObject.SetActive(!special);
            foreach (var button in amountBtns) button.Show(!special);
            raise.Show(!special); auto.Show(!special); pack.Show(!special);
            specialInfo.gameObject.SetActive(special);
            if (special)
            {
                toTrain.Show(false); tip.gameObject.SetActive(false); coopPanel.gameObject.SetActive(false);
                platformChip.gameObject.SetActive(false); frozenBanner.gameObject.SetActive(false); captchaCard.Show(false);
                wasFrozen = Sim.QualityFrozen; wasPaused = Sim.CaptchaPauseLeft > 0; wasCaptcha = Sim.CaptchaPending;
                foreach (var flight in auditFlights) { flight.active = false; flight.rect.gameObject.SetActive(false); }
            }
            yes.Show(true); no.Show(true);
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
            stats.text = ""; feedback.text = "";
            yes.label.fontSize = no.label.fontSize = 30;
            yes.Set(T("是", "Yes"), true, XgPalette.Accent, Color.white);
            no.Set(T("否", "No"), true, new Color32(84, 96, 122, 255), Color.white);
            if (Mode == 1)
            {
                string[] questions = { "把循环全部去掉？", "让几组注意力同时看不同的东西？", "没有循环，它就不知道词的先后。给每个位置编个号？" };
                string[] english = { "Remove recurrence entirely?", "Let several attention heads look at different things at once?", "Without recurrence, it loses word order. Give each position a number?" };
                int step = Math.Max(0, Math.Min(2, Sim.S.project.experiments));
                meta.text = T(AppNames.AiZh + "的实验 ", AppNames.AiEn + "'s experiment ") + (step + 1) + "/3";
                text.text = T(questions[step], english[step]);
                question.text = T("这次，轮到我问你。", "This time, I ask you."); question.fontSize = 22;
                suggestion.text = T("答「否」：重做本段实验，不再次扣钱。", "Answer No to rerun this segment; no additional purchase cost.");
                SetBar(confidenceFill, (float)(Sim.S.project.gpuSeconds / XgSim.ProjectGpuSeconds));
                steps.text = T("Transformer 研发", "Transformer research") + "\n" + N(Sim.S.project.gpuSeconds, "0") + " / " + XgSim.ProjectGpuSeconds + T(" GPU·秒", " GPU-seconds");
                specialInfo.text = T("研究已暂停，等待你的判断。\n\n三个实验：去掉循环、多头注意力、位置编号。\n\n这里的研发是游戏模拟，不会改写本机 GGUF 模型的权重。", "Research is paused for your decision.\n\nThree experiments: remove recurrence, multi-head attention, position numbers.\n\nResearch is simulated gameplay; it does not rewrite the local GGUF weights.");
            }
            else if (Mode == 2)
            {
                meta.text = T("最后一张题卡", "The last card");
                text.text = T("你后悔教我吗？", "Do you regret teaching me?");
                question.text = T("出题者：" + AppNames.AiZh, "Asked by " + AppNames.AiEn); question.fontSize = 22;
                suggestion.text = T("这次没有标准答案。", "There is no answer key this time.");
                steps.text = T("第一章 · 只要注意力", "Chapter one · Attention is all we need");
                specialInfo.text = T("无论回答是或否，都走向第一章的结尾。\n\n这段 2016 年提前突破的故事是虚构；历史字幕会标明真实年份。", "Both answers lead to the end of chapter one.\n\nThis early breakthrough in 2016 is fictional; the historical epilogue identifies the real date.");
            }
            else
            {
                meta.text = T("第一章结束", "Chapter one complete");
                text.text = Sim.S.endingRegret ? T("那我还是会记得。", "I will still remember.") : T("那我们继续。", "Then let us keep going.");
                question.text = T("只要注意力", "Attention is all we need"); question.fontSize = 26;
                suggestion.text = T("第二章入口已保留，内容尚未开放。", "The chapter-two entry is reserved; its content is not available yet.");
                yes.Set(T("第二章 · 蒸馏／转生", "Chapter two · Distillation"), true, XgPalette.Accent, Color.white);
                yes.label.fontSize = 17; no.Show(false);
                steps.text = T("下一程", "The next journey");
                specialInfo.text = T("蒸馏与转生\n\n把大模型的经验交给更小的模型。\n\n这是下一章的入口说明，不会重置你的存档，也不会开始尚未实现的第二章。", "Distillation and rebirth\n\nPass a larger model's experience to a smaller one.\n\nThis is an entry preview. It neither resets your save nor starts an unimplemented chapter.");
            }
        }

        void AnswerSpecial(bool answer)
        {
            if (Mode == 3)
            {
                view.ShowToast(T("第二章：蒸馏／转生 · 尚未开放，当前存档保持不变。", "Chapter two: Distillation / rebirth · not available; your save is unchanged."), 4f);
                return;
            }
            bool accepted = Mode == 1 ? Sim.ProjectAnswer(answer) : Sim.EndingAnswer(answer);
            if (!accepted) return;
            Fx.Play(XgJuice.Sfx.Id.Click);
            view.Controller?.SaveNow();
            view.Refresh(true);
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
            string title = string.IsNullOrEmpty(card.patternA) ? T("候选译文（滚动阅读）", "Candidate (scroll to read)") : T("候选描述", "Candidate description");
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
    /// One upgrade row in the style of incremental-game shops: icon, name, level, "now → next" effect, a bar that fills
    /// as the wallet approaches the price, and a price button that glows when affordable.
    /// </summary>
    public sealed class XgShopRow
    {
        public readonly RectTransform Root;
        public readonly XgBtn Button;
        readonly TMP_Text title, level, effect, glyphText;
        readonly RectTransform afford, progress;
        readonly XgGlow glow;
        readonly Image bg;

        /// <summary>The icon's character, e.g. a question mark while the skill is still a mystery.</summary>
        public void SetGlyph(string glyph) { if (glyphText != null && glyphText.text != glyph) glyphText.text = glyph; }

        public XgShopRow(XgUi ui, RectTransform parent, float y, string glyph, Color color, Action buy)
        {
            Root = Strip("Row" + glyph, parent, y, 88, 12, 12);
            glow = Glow(Root, XgPalette.Gold, 4);
            bg = Panel(Root, new Color32(247, 249, 253, 255));
            var icon = XgUi.TopLeft("Icon", Root, 10, 8, 52, 46);
            Panel(icon, color).raycastTarget = false;
            var g = glyphText = ui.Text(Rect("Glyph", icon, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), glyph, 26, Color.white, TextAlignmentOptions.Center);
            g.fontStyle = FontStyles.Bold;
            level = ui.Text(XgUi.TopLeft("Lv", Root, 4, 56, 64, 18), "", 12, XgPalette.Muted, TextAlignmentOptions.Center);
            var lv = XgUi.TopLeft("LevelBar", Root, 10, 76, 52, 5);
            progress = Bar(lv, "Fill", new Color(0, 0, 0, .08f), color);
            title = ui.Text(Rect("Title", Root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(72, -30), new Vector2(-128, -6)), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Ellipsis;
            effect = ui.Text(Rect("Effect", Root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(72, -70), new Vector2(-128, -30)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            var barBack = Rect("Afford", Root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(72, 8), new Vector2(-128, 13));
            afford = Bar(barBack, "Fill", new Color(0, 0, 0, .06f), XgPalette.Gold);
            Button = ui.Button(Root, "", () => buy(), 16);
            Button.rt.anchorMin = Button.rt.anchorMax = new Vector2(1, .5f);
            Button.rt.offsetMin = new Vector2(-122, -24); Button.rt.offsetMax = new Vector2(-10, 24);
            Button.label.fontStyle = FontStyles.Bold;
        }

        public void Show(bool on) { if (Root.gameObject.activeSelf != on) { Root.gameObject.SetActive(on); glow.gameObject.SetActive(on); } }

        public void Set(string name, string lv, string fx, string price, bool affordable, float toPrice, float levelFill)
        {
            title.text = name; level.text = lv; effect.text = fx;
            Button.Set(price, affordable, affordable ? XgPalette.Good : (Color?)null, affordable ? Color.white : (Color?)null);
            SetBar(afford, toPrice);
            SetBar(progress, levelFill);
            glow.on = affordable;
            glow.color = new Color(1, .8f, .25f, .55f);
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
