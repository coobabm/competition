using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 手写数字 as a drawing task (the desk's main view in 摆渡众包): the page asks for a digit, the player writes it with
    /// the mouse, and LingGuang reads the ink live in a speech bubble. Handing a drawing in labels it with the asked
    /// digit (XgSim.SubmitHandwriting), pays like a hand label and teaches the recogniser, whose recent accuracy is on
    /// show. The desk opens on the yes/no cards (2026-10-19); writing is one toggle away. The rules live in
    /// XgSim.Handwriting.cs; this file only presents them.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        const int SessionCells = 20;
        const float GuessPeriod = .2f;

        RectTransform drawRoot, padRect, guessBubble;
        XgHandwritingInk ink;
        XgHandwritingPad padInput;
        XgBtn drawClear, drawDone, drawToggle, classicToggle;
        TMP_Text drawPrompt, drawPay, padHint, guessLine, guessPct, timeLine, memoryTitle, memorySub, accuracyLabel, accuracyBig, accuracyTrend, accuracyNote,
            drawFeedback, sessionCaption;
        RectTransform guessFill, accuracyFill;
        Image guessBack;
        readonly Image[] oddsFill = new Image[3];
        readonly TMP_Text[] oddsDigit = new TMP_Text[3], oddsPct = new TMP_Text[3];
        readonly Image[] sessionCell = new Image[SessionCells];
        readonly TMP_Text[] sessionMark = new TMP_Text[SessionCells];
        XgCrawlerLayer drawAvatar;

        readonly bool[] raster = new bool[XgHandwriting.Grid * XgHandwriting.Grid];
        readonly List<int> bag = new List<int>();
        readonly List<(int asked, bool hit)> session = new List<(int, bool)>();
        XgHandwritingGuess liveGuess;
        XgSim handwritingOwner;
        Vector2 lastPoint;
        // 2026-10-19 (owner): the desk just asks 「这张手写数字是 X 吗？」 yes/no first; writing is the optional toggle.
        bool yesNoChosen = true, inkDirty, hasInk;
        int asked = -1, sessionRight, strokeBudget;
        double sessionIncome;
        float drawClock, guessTimer, rightSince = -1, resultAge = 99;
        string resultLine = "";
        bool handwritingEnglish;

        /// <summary>The writing view is on screen (the yes/no card underneath is not being answered).</summary>
        bool HandwritingVisible => drawRoot != null && drawRoot.gameObject.activeSelf;

        /// <summary>The desk can be written on right now (the player may still have picked the yes/no cards).</summary>
        bool HandwritingEligible()
        {
            if (WorkContext || drawRoot == null || Sim == null || view != null || Desk != XgSim.HandwritingDesk || !Sim.DeskOpen(XgSim.HandwritingDesk)) return false;
            if (Mode != 0 || Sim.CaptchaPending || Sim.CaptchaPauseLeft > 0 || Sim.ReviewCard(Desk) != null) return false;
            var card = CurrentCard;
            return !(card.family || card.law || card.kind == "shutdown" || card.bottleneckPreview || card.kind == "combo");
        }

        static void PlaceFrame(XgBtn b, float x, float y, float w, float h)
        {
            var f = ZhongbaoSkin.Frame(b);
            f.anchorMin = f.anchorMax = new Vector2(0, 1); f.offsetMin = new Vector2(x, -y - h); f.offsetMax = new Vector2(x + w, -y);
        }

        TMP_Text Label(RectTransform parent, string name, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = ui.Text(TopLeft(name, parent, x, y, w, h), "", size, color, align);
            // Auto-sized so a long line shrinks instead of vanishing: an ellipsis box shows nothing when one line is taller than the box.
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true; t.fontSizeMin = size * .7f; t.fontSizeMax = size;
            return t;
        }

        void BuildHandwriting()
        {
            // Below the auto-answer feed strip, over the card and its buttons.
            drawRoot = Rect("Handwriting", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -36));
            Panel(drawRoot, Color.white).raycastTarget = false;

            drawPrompt = Label(drawRoot, "Prompt", 14, 6, 300, 40, 30, ZhongbaoSkin.Ink);
            drawPrompt.fontStyle = FontStyles.Bold;
            drawPay = Label(drawRoot, "Pay", 14, 46, 316, 18, 12, ZhongbaoSkin.Mute);
            drawToggle = ZhongbaoSkin.Outlined(ui, drawRoot, "", () => { yesNoChosen = true; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 13, ZhongbaoSkin.Line);
            var tf = ZhongbaoSkin.Frame(drawToggle);
            tf.anchorMin = tf.anchorMax = Vector2.one; tf.offsetMin = new Vector2(-146, -34); tf.offsetMax = new Vector2(-14, -8);
            tf.name = "ToYesNo";
            UiTip.Add(tf, "不想画了就切回「对 / 错」卡片，照旧按题付钱。", "Not in the mood to draw? Switch back to the yes/no cards; they pay as before.");

            // The pad: a white sheet with a thin rim; the ink is a child graphic, the pointer handler sits on the sheet.
            var rim = ZhongbaoSkin.Box(ui, drawRoot, "PadBox", new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -372), new Vector2(314, -72));
            padRect = Rect("Pad", rim, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(padRect, new Color32(253, 253, 250, 255));
            padHint = ui.Text(Rect("Hint", padRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 30, new Color32(205, 209, 220, 255), TextAlignmentOptions.Center);
            var inkRect = Rect("Ink", padRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inkRect.gameObject.AddComponent<CanvasRenderer>();
            ink = inkRect.gameObject.AddComponent<XgHandwritingInk>();
            padInput = padRect.gameObject.AddComponent<XgHandwritingPad>();
            padInput.Focus = () => ui.window?.FocusToWindow();
            padInput.Active = () => HandwritingVisible && Sim != null && asked >= 0;
            padInput.Down = PenDown; padInput.Drag = PenMove; padInput.Up = PenUp;

            drawClear = ZhongbaoSkin.Outlined(ui, drawRoot, "", ClearPad, 16, ZhongbaoSkin.Line);
            PlaceFrame(drawClear, 14, 380, 120, 38);
            drawDone = ZhongbaoSkin.Outlined(ui, drawRoot, "", HandIn, 18, ZhongbaoSkin.Blue);
            PlaceFrame(drawDone, 144, 380, 170, 38);
            ZhongbaoSkin.Frame(drawDone).name = "HandwritingDone";
            UiTip.Add(ZhongbaoSkin.Frame(drawDone), () => T(("写完点这里：这张字按你被问的数字入库，算一条手标样本，灵光也会把它记住。" .Replace("灵光", Sim.AiDisplayName)),
                ("Press when you are done: the drawing is stored under the digit you were asked, counts as one hand-labelled sample, and LingGuang remembers it." .Replace("LingGuang", Sim.AiDisplayName))));

            drawFeedback = ui.Text(TopLeft("Feedback", drawRoot, 14, 424, 300, 58), "", 13, ZhongbaoSkin.Ink, TextAlignmentOptions.TopLeft);
            sessionCaption = Label(drawRoot, "SessionCaption", 14, 486, 300, 16, 12, ZhongbaoSkin.Mute);
            for (int i = 0; i < SessionCells; i++)
            {
                var cell = TopLeft("Result" + i, drawRoot, 14 + i * 15, 506, 14, 14);
                sessionCell[i] = Panel(cell, ZhongbaoSkin.Track); sessionCell[i].raycastTarget = false;
                sessionMark[i] = ui.Text(Rect("Mark", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 9, Color.white, TextAlignmentOptions.Center);
                sessionMark[i].textWrappingMode = TextWrappingModes.NoWrap;
            }

            // LingGuang on the right: its avatar, the speech bubble with the live guess, its three best candidates.
            var home = TopLeft("Avatar", drawRoot, 330, 74, 40, 40);
            drawAvatar = ZhongbaoSkin.Canvas(home, vh => DrawSpiderIcon(vh, home, false));
            guessBubble = TopLeft("Bubble", drawRoot, 378, 72, 194, 62);
            guessBack = Panel(guessBubble, ZhongbaoSkin.SpiderSoft); guessBack.raycastTarget = false;
            var bubbleRim = guessBubble.gameObject.AddComponent<UnityEngine.UI.Outline>();
            bubbleRim.effectColor = ZhongbaoSkin.SpiderLine; bubbleRim.effectDistance = new Vector2(1, -1);
            guessLine = Label(guessBubble, "Guess", 10, 4, 132, 36, 22, new Color32(75, 63, 168, 255));
            guessLine.fontStyle = FontStyles.Bold; guessLine.enableAutoSizing = true; guessLine.fontSizeMin = 12; guessLine.fontSizeMax = 22;
            guessPct = Label(guessBubble, "Confidence", 140, 4, 46, 36, 16, new Color32(75, 63, 168, 255), TextAlignmentOptions.MidlineRight);
            var bar = TopLeft("Sure", guessBubble, 10, 46, 174, 6);
            guessFill = Bar(bar, "Fill", new Color(0, 0, 0, .08f), ZhongbaoSkin.Spider);
            UiTip.Add(guessBubble, () => T(("灵光边看边猜：数字是它此刻的读法，百分比是它有几分把握。它只认得内置字模和你写过的字。" .Replace("灵光", Sim.AiDisplayName)),
                ("LingGuang guesses as you write: the digit is what it reads right now, the percentage how sure it is. It only knows its built-in letterforms and the drawings you handed in." .Replace("LingGuang", Sim.AiDisplayName))));
            for (int i = 0; i < 3; i++)
            {
                float y = 144 + i * 20;
                oddsDigit[i] = Label(drawRoot, "OddsDigit" + i, 330, y, 18, 18, 13, ZhongbaoSkin.Ink);
                oddsDigit[i].fontStyle = FontStyles.Bold;
                var back = TopLeft("OddsBar" + i, drawRoot, 352, y + 5, 170, 8);
                Panel(back, ZhongbaoSkin.Track).raycastTarget = false;
                var fill = Rect("Fill", back, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
                oddsFill[i] = Panel(fill, ZhongbaoSkin.Spider); oddsFill[i].raycastTarget = false;
                oddsPct[i] = Label(drawRoot, "OddsPct" + i, 526, y, 46, 18, 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            }
            timeLine = ui.Text(TopLeft("Time", drawRoot, 330, 208, 242, 40), "", 13, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);

            // What it has learned, as numbers.
            var stats = ZhongbaoSkin.Box(ui, drawRoot, "Learned", new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, -440), new Vector2(572, -254));
            memoryTitle = Label(stats, "Memory", 12, 8, 218, 22, 15, ZhongbaoSkin.Ink);
            memoryTitle.fontStyle = FontStyles.Bold;
            memorySub = Label(stats, "MemorySub", 12, 30, 218, 16, 11, ZhongbaoSkin.Mute);
            ZhongbaoSkin.Hairline(TopLeft("Rule", stats, 0, 54, 242, 2), 10, false);
            accuracyLabel = Label(stats, "AccuracyLabel", 12, 62, 218, 18, 12, ZhongbaoSkin.Mute);
            accuracyBig = Label(stats, "Accuracy", 12, 80, 110, 38, 30, ZhongbaoSkin.Green);
            accuracyBig.fontStyle = FontStyles.Bold;
            accuracyTrend = Label(stats, "Trend", 104, 84, 126, 30, 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            accuracyTrend.textWrappingMode = TextWrappingModes.Normal;
            var accBar = TopLeft("AccuracyBar", stats, 12, 122, 218, 8);
            accuracyFill = Bar(accBar, "Fill", ZhongbaoSkin.Track, ZhongbaoSkin.Green);
            accuracyNote = ui.Text(TopLeft("Note", stats, 12, 138, 218, 44), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
            UiTip.Add(stats, "它读对的次数：每张图在它学这张之前先读一遍，读对才算。刚开始只认内置字模，所以很不准；每交一张，它就多认识你的一种写法。",
                "How often it read you right: each drawing is read once before it learns it, and only a right reading counts. At first it knows only its built-in letterforms, so it is poor; every drawing you hand in teaches it one more way you write.");

            // The way back from the yes/no cards: bottom left of the card area.
            classicToggle = ZhongbaoSkin.Outlined(ui, body, "", () => { yesNoChosen = false; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 13, ZhongbaoSkin.Line);
            var cf = ZhongbaoSkin.Frame(classicToggle);
            cf.anchorMin = cf.anchorMax = new Vector2(0, 0); cf.offsetMin = new Vector2(14, 8); cf.offsetMax = new Vector2(154, 34);
            cf.name = "ToHandwriting";
            UiTip.Add(cf, () => T(("回到写字：照被问的数字写一个，灵光边看边猜。" .Replace("灵光", Sim.AiDisplayName)), ("Back to writing: write the digit you are asked for while LingGuang guesses." .Replace("LingGuang", Sim.AiDisplayName))));
            ShowFrame(classicToggle, false);
            drawRoot.gameObject.SetActive(false);
        }

        // ───────────── showing and hiding ─────────────

        /// <summary>Follows the desk: shows the writing view (or the toggle back to it) and fills in its texts. Cheap; run on every refresh.</summary>
        void RefreshHandwriting()
        {
            if (drawRoot == null) return;
            bool eligible = HandwritingEligible();
            bool show = eligible && !yesNoChosen;
            if (!ReferenceEquals(handwritingOwner, Sim))
            {
                handwritingOwner = Sim; session.Clear(); sessionRight = 0; sessionIncome = 0; asked = -1; resultLine = ""; resultAge = 99;
            }
            if (show != drawRoot.gameObject.activeSelf)
            {
                drawRoot.gameObject.SetActive(show);
                if (show) { if (asked < 0) NewRound(); }
            }
            ShowFrame(classicToggle, eligible && yesNoChosen);
            if (handwritingEnglish != En) { handwritingEnglish = En; drawFeedback.text = ""; resultLine = ""; } // event texts are not translated after the fact
            if (!show) return;
            if (Sim != null) Sim.MiniGameSurfaceCheck = () => HandwritingVisible; // the card timer is not running while someone writes
            RefreshHandwritingTexts();
        }

        void RefreshHandwritingTexts()
        {
            double pay = Sim.ManualPayFor(Desk, Sim.LevelOf(Desk));
            drawPrompt.text = T("写一个 ", "Write a ") + "<color=#" + ColorUtility.ToHtmlStringRGB(ZhongbaoSkin.Blue) + ">" + asked + "</color>";
            drawPay.text = T("每张 ¥" + N(pay, "0.00") + " · " + N(XgSim.HandwritingFastSeconds, "0") + " 秒内被读对，另有速度奖励 · 不扣钱",
                "¥" + N(pay, "0.00") + " a drawing · a speed bonus if it reads it right within " + N(XgSim.HandwritingFastSeconds, "0") + " s · no fines");
            drawToggle.Set(T("改答是非题", "Yes/No cards"), true, Color.white, ZhongbaoSkin.Ink);
            ZhongbaoSkin.Ghost(drawClear, T("清空", "Clear"), true);
            ZhongbaoSkin.Primary(drawDone, T("写好了", "Done"), true);
            classicToggle.Set(T("← 改回手写", "← Back to writing"), true, Color.white, ZhongbaoSkin.Blue);
            padHint.text = hasInk ? "" : T("在这里写", "Write here");

            int memory = Sim.HandwritingMemory;
            memoryTitle.text = T("它记住了 " + memory + " 张你的字", "It remembers " + memory + " of your drawings");
            memorySub.text = T("内置字模 " + XgHandwriting.TemplatesPerDigit * 10 + " 个 + 你的字 " + memory + "/" + Sim.HandwritingCapacity,
                XgHandwriting.TemplatesPerDigit * 10 + " built-in letterforms + yours " + memory + "/" + Sim.HandwritingCapacity);
            double accuracy = Sim.HandwritingAccuracy(out int n);
            var recent = Sim.HandwritingRecent();
            int right = 0; foreach (bool r in recent) if (r) right++;
            accuracyLabel.text = n == 0 ? T("最近 20 张的命中率", "Hit rate, last 20") : T("最近 " + n + " 张里它读对了 " + right + " 张", "Read right in the last " + n + ": " + right);
            accuracyBig.text = n == 0 ? "—" : Math.Round(accuracy * 100) + "%";
            SetBar(accuracyFill, (float)accuracy);
            if (n >= 12)
            {
                int half = n / 2, a = 0, b = 0;
                for (int i = 0; i < half; i++) if (recent[i]) a++;
                for (int i = n - half; i < n; i++) if (recent[i]) b++;
                accuracyTrend.text = T("前 " + half + " 张 " + Math.Round(100.0 * a / half) + "% → 后 " + half + " 张 " + Math.Round(100.0 * b / half) + "%",
                    "first " + half + ": " + Math.Round(100.0 * a / half) + "% → last " + half + ": " + Math.Round(100.0 * b / half) + "%");
            }
            else accuracyTrend.text = "";
            accuracyNote.text = Sim.S.handwritingDrawn == 0
                ? T("它现在只认得内置字模，还不认识你的字。多写几张，它会越读越准。", "It only knows its built-in letterforms and not your hand yet. Write a few and it reads better.")
                : T("累计交了 " + Sim.S.handwritingDrawn + " 张，读对 " + Sim.S.handwritingRight + " 张（" + Math.Round(100.0 * Sim.S.handwritingRight / Sim.S.handwritingDrawn) + "%）。每张写完它都会记住。",
                    Sim.S.handwritingDrawn + " handed in, " + Sim.S.handwritingRight + " read right (" + Math.Round(100.0 * Sim.S.handwritingRight / Sim.S.handwritingDrawn) + "%). It remembers each one.");

            sessionCaption.text = session.Count == 0 ? T("这一次的字", "This session")
                : T("这一次 " + session.Count + " 张 · 读对 " + sessionRight + " · 收入 ¥" + N(sessionIncome, "0.00"), "This session: " + session.Count + " · read right " + sessionRight + " · earned ¥" + N(sessionIncome, "0.00"));
            for (int i = 0; i < SessionCells; i++)
            {
                int at = Math.Max(0, session.Count - SessionCells) + i;
                bool has = at >= 0 && at < session.Count;
                sessionCell[i].color = !has ? ZhongbaoSkin.Track : session[at].hit ? ZhongbaoSkin.Green : ZhongbaoSkin.Red;
                sessionMark[i].text = has ? session[at].asked.ToString() : "";
            }
            RefreshGuessView();
            RefreshTimeLine();
        }

        // ───────────── rounds, pen and live guess ─────────────

        void NewRound()
        {
            // Every digit once per bag, so the drawings it learns from cover all ten.
            if (bag.Count == 0)
            {
                for (int d = 0; d < 10; d++) bag.Add(d);
                var rng = new System.Random(unchecked(Environment.TickCount * 31 + session.Count));
                for (int i = bag.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (bag[i], bag[j]) = (bag[j], bag[i]); }
                if (bag[bag.Count - 1] == asked) { int j = rng.Next(bag.Count - 1); (bag[bag.Count - 1], bag[j]) = (bag[j], bag[bag.Count - 1]); }
            }
            asked = bag[bag.Count - 1]; bag.RemoveAt(bag.Count - 1);
            ResetPad();
        }

        void ResetPad()
        {
            Array.Clear(raster, 0, raster.Length);
            if (ink != null) ink.Clear();
            hasInk = false; inkDirty = false; drawClock = 0; rightSince = -1; guessTimer = 0; strokeBudget = XgHandwritingInk.MaxSegments;
            liveGuess = default;
        }

        void ClearPad()
        {
            if (!HandwritingVisible) return;
            ResetPad();
            drawFeedback.text = "";
            Fx.Play(XgJuice.Sfx.Id.Click);
            RefreshHandwritingTexts();
        }

        void PenDown(Vector2 p)
        {
            if (strokeBudget <= 0) { drawFeedback.text = "<color=#E33E33>" + T("笔迹太多了：点「写好了」，或者清空重写。", "That is a lot of ink: press Done, or clear and start over.") + "</color>"; return; }
            if (!hasInk) { hasInk = true; drawClock = 0; rightSince = -1; padHint.text = ""; drawFeedback.text = ""; resultAge = 99; }
            lastPoint = p;
            Stroke(p, p);
        }

        void PenMove(Vector2 p)
        {
            if (strokeBudget <= 0) return;
            if ((p - lastPoint).sqrMagnitude < 1e-6f) return;
            Stroke(lastPoint, p); lastPoint = p;
        }

        void PenUp() { guessTimer = 0; }

        void Stroke(Vector2 a, Vector2 b)
        {
            const float margin = 1.5f;
            float g = XgHandwriting.Grid - 1 - margin * 2;
            XgHandwriting.Line(raster, margin + a.x * g, margin + a.y * g, margin + b.x * g, margin + b.y * g);
            if (ink.Add(a, b)) strokeBudget--;
            inkDirty = true;
        }

        void TickHandwriting(float dt)
        {
            if (!HandwritingVisible) return;
            if (Sim != null) Sim.MiniGameSurfaceCheck = () => HandwritingVisible;
            if (resultAge < 99) resultAge += dt;
            if (!hasInk) { if (resultAge > 6 && resultLine.Length > 0) { resultLine = ""; RefreshGuessView(); } return; }
            drawClock += dt;
            guessTimer -= dt;
            if (inkDirty && guessTimer <= 0) { guessTimer = GuessPeriod; ReadInk(); }
            RefreshTimeLine();
        }

        /// <summary>Asks the recogniser what the ink says now and notes when it started to read the asked digit.</summary>
        void ReadInk()
        {
            inkDirty = false;
            liveGuess = Sim.ReadHandwriting(raster);
            bool right = liveGuess.valid && liveGuess.digit == asked;
            if (right && rightSince < 0) rightSince = drawClock;
            else if (!right) rightSince = -1;
            RefreshGuessView();
            RefreshTimeLine();
        }

        void RefreshTimeLine()
        {
            string fast = N(XgSim.HandwritingFastSeconds, "0");
            if (!hasInk) timeLine.text = T(fast + " 秒内被读对有速度奖励。", "Read right within " + fast + " s for a speed bonus.");
            else if (rightSince >= 0)
            {
                bool quick = rightSince <= XgSim.HandwritingFastSeconds;
                timeLine.text = "<color=#2F9E44>" + T("读对了 · 用了 " + N(rightSince, "0.0") + " 秒", "Read right · took " + N(rightSince, "0.0") + " s") + "</color>"
                    + (quick ? "  <color=#E86E14>" + T("有速度奖励", "speed bonus") + "</color>" : "");
            }
            else timeLine.text = T("还没读成 " + asked + " · 已写 " + N(drawClock, "0.0") + " 秒", "Not reading it as " + asked + " yet · " + N(drawClock, "0.0") + " s in");
        }

        void RefreshGuessView()
        {
            if (guessLine == null) return;
            if (!hasInk)
            {
                guessLine.text = resultLine.Length > 0 ? resultLine : T("等你下笔……", "Waiting for your pen…");
                guessPct.text = ""; SetBar(guessFill, 0); guessBack.color = ZhongbaoSkin.SpiderSoft;
                ShowOdds(null);
                return;
            }
            if (!liveGuess.valid)
            {
                guessLine.text = T("再写一点……", "A bit more…"); guessPct.text = ""; SetBar(guessFill, 0); guessBack.color = ZhongbaoSkin.SpiderSoft;
                ShowOdds(null);
                return;
            }
            int d = liveGuess.digit; double c = liveGuess.confidence;
            guessLine.text = c >= .6 ? T("是 " + d + "！", "It's " + d + "!") : c >= .35 ? T("是 " + d + "？", d + "?") : T("像 " + d + "……", "Looks like " + d + "…");
            guessPct.text = Math.Round(c * 100) + "%";
            SetBar(guessFill, (float)c);
            guessBack.color = d == asked ? new Color32(236, 248, 238, 255) : ZhongbaoSkin.SpiderSoft;
            ShowOdds(liveGuess);
        }

        void ShowOdds(XgHandwritingGuess? guess)
        {
            var order = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            double scale = 1;
            if (guess.HasValue)
            {
                var g = guess.Value;
                Array.Sort(order, (x, y) => g.odds[y].CompareTo(g.odds[x]));
                scale = g.odds[g.digit] > 0 ? g.confidence / g.odds[g.digit] : 1;
            }
            for (int i = 0; i < 3; i++)
            {
                bool on = guess.HasValue;
                double p = on ? guess.Value.odds[order[i]] * scale : 0;
                oddsDigit[i].text = on ? order[i].ToString() : "";
                oddsPct[i].text = on ? Math.Round(p * 100) + "%" : "";
                var rt = (RectTransform)oddsFill[i].transform;
                SetBar(rt, (float)p);
                oddsFill[i].color = on && order[i] == asked ? ZhongbaoSkin.Green : ZhongbaoSkin.Spider;
            }
        }

        // ───────────── handing in ─────────────

        void HandIn()
        {
            if (!HandwritingVisible || asked < 0) return;
            if (hasInk) ReadInk();
            double seconds = rightSince >= 0 ? rightSince : drawClock;
            var r = Sim.SubmitHandwriting(asked, raster, seconds, Host);
            if (r.rejected)
            {
                drawFeedback.text = "<color=#E33E33>" + T("还没写出一个数字：把它写完整一点，再点「写好了」。这张不算钱。", "That is not a digit yet: write the whole thing, then press Done. This one is not paid.") + "</color>";
                Fx.Knock(drawDone.rt, .05f, new Vector2(8, 0), .25f);
                Fx.Play(XgJuice.Sfx.Id.Thud, 1.3f, .6f);
                return;
            }
            double total = r.pay + r.bonus;
            AnswerCount++;
            session.Add((asked, r.hit)); if (r.hit) sessionRight++; sessionIncome += total;
            while (session.Count > 99) session.RemoveAt(0);
            string money = "+¥" + N(total, "0.00");
            if (r.hit)
            {
                drawFeedback.text = "<color=#2F9E44>✓ " + T("读对了 · 用了 " + N(r.seconds, "0.0") + " 秒", "Read right · took " + N(r.seconds, "0.0") + " s") + "  " + money + "</color>"
                    + (r.bonus > 0 ? "  <color=#E86E14>" + T("含速度奖励 ¥", "incl. speed bonus ¥") + N(r.bonus, "0.00") + "</color>" : "")
                    + "\n<color=#68748C>" + T("它又记住了一种你写的 " + asked + "。", "It remembered one more way you write " + asked + ".") + "</color>";
                resultLine = T("读对了 " + asked + "！", "Got " + asked + "!");
                Fx.Float(Fx.At(padRect, new Vector2(0, 60)), money, XgPalette.Money, 26);
                Fx.Burst(Fx.At(padRect), 8, XgPalette.Gold, XgJuice.Shape.Yen, 240);
                Fx.Play(XgJuice.Sfx.Id.Ding);
            }
            else
            {
                drawFeedback.text = "<color=#B36A00>" + T("它读成了 " + r.guess + "（你写的是 " + asked + "）", "It read " + r.guess + " (you wrote " + asked + ")") + "  " + money + "</color>"
                    + "\n<color=#68748C>" + T("不扣钱。它记住了这张，下次再看到这样的 " + asked + " 就认得了。", "No fine. It remembered this one, so it will know a " + asked + " like this next time.") + "</color>";
                resultLine = T("读成 " + r.guess + "，记住了", "Read " + r.guess + ", noted");
                Fx.Float(Fx.At(padRect, new Vector2(0, 60)), money, XgPalette.Money, 22);
                Fx.Play(XgJuice.Sfx.Id.Tick, 1.1f, .8f);
            }
            resultAge = 0;
            Fx.Knock(padRect, .03f, new Vector2(0, 8), .2f);
            NewRound();
            Refresh();
        }
    }

    /// <summary>The sheet's pointer handler: reports the mouse as 0–1 coordinates of the sheet (top left is the origin).</summary>
    public sealed class XgHandwritingPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> Down, Drag;
        public Action Up, Focus;
        public Func<bool> Active;
        bool held;

        Vector2 Point(PointerEventData e)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var p);
            var r = rect.rect;
            return new Vector2(Mathf.Clamp01((p.x - r.xMin) / r.width), Mathf.Clamp01((r.yMax - p.y) / r.height));
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            Focus?.Invoke();
            if (Active != null && !Active()) return;
            held = true; Down?.Invoke(Point(e));
        }
        public void OnDrag(PointerEventData e) { if (held) Drag?.Invoke(Point(e)); }
        public void OnPointerUp(PointerEventData e) { if (!held) return; held = false; Up?.Invoke(); }
        void OnDisable() { held = false; }
    }

    /// <summary>The pen strokes of the sheet, drawn as round-capped segments. Never takes input.</summary>
    public sealed class XgHandwritingInk : MaskableGraphic
    {
        /// <summary>Keeps one mesh well under the vertex limit.</summary>
        public const int MaxSegments = 1800;
        /// <summary>Pen width as a share of the sheet; the raster pen is 3 of 64 cells, drawn a hair fatter to read well.</summary>
        const float PenShare = 3.2f / 64f;
        readonly List<Vector4> segments = new List<Vector4>();
        static readonly Color Pen = new Color32(30, 34, 52, 255);

        protected override void Awake() { base.Awake(); raycastTarget = false; }
        public int Count => segments.Count;

        public bool Add(Vector2 a, Vector2 b)
        {
            if (segments.Count >= MaxSegments) return false;
            segments.Add(new Vector4(a.x, a.y, b.x, b.y));
            SetVerticesDirty();
            return true;
        }

        public void Clear() { segments.Clear(); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            float width = r.width * PenShare;
            foreach (var s in segments)
            {
                var a = new Vector2(r.xMin + s.x * r.width, r.yMax - s.y * r.height);
                var b = new Vector2(r.xMin + s.z * r.width, r.yMax - s.w * r.height);
                XgDraw.Seg(vh, a, b, width, Pen);
                XgDraw.Disc(vh, b, width * .5f, Pen, 8);
            }
        }
    }
}
