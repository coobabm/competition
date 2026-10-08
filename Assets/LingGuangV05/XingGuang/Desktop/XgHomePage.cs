using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 概览, the landing page (lingguang-redesign/index.html #home): the cortex thumbnail with the regions it has trained
    /// lit, its latest line, the two big cards toward the next ability (thresholds after the items owned), the six-cell
    /// ability strip, four quick actions (训练一轮, 买数据, 买道具, 接线) that go through the existing actions and pages,
    /// and the recent events (the sim's notices and the 接线 log's warnings, kept by the view).
    /// </summary>
    public sealed class XgHomePage : XgPage
    {
        const float LeftWidth = 330, CortexHeight = 300;

        XgCortexGraphic cortex;
        TMP_Text cortexCaption, sayWho, sayText;
        TMP_Text nextHead, nextName;
        Big paramsCard, dataCard;
        readonly Image[] cellRims = new Image[XgSim.AbilityCount];
        readonly Image[] cellFills = new Image[XgSim.AbilityCount];
        readonly TMP_Text[] cellNames = new TMP_Text[XgSim.AbilityCount], cellSubs = new TMP_Text[XgSim.AbilityCount];
        readonly RectTransform[] cellBars = new RectTransform[XgSim.AbilityCount];
        readonly Act[] acts = new Act[4];
        readonly List<TMP_Text> feedLines = new List<TMP_Text>();
        TMP_Text feedHead;
        float animTimer;
        bool cortexSet;

        sealed class Big { public Image rim; public TMP_Text head, tags, value, note; public RectTransform fill; }
        sealed class Act { public RectTransform rt; public TMP_Text title, sub; public Button button; }

        public override void Build(RectTransform area)
        {
            root = Rect("home", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── left: the cortex and its latest line
            var map = Rect("Cortex", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -CortexHeight), new Vector2(LeftWidth, 0));
            Panel(map, XgDark.Line);
            var inner = Rect("Inner", map, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            Panel(inner, new Color32(6, 11, 16, 255)).raycastTarget = false;
            var brain = Rect("Brain", inner, Vector2.zero, Vector2.one, new Vector2(2, 22), new Vector2(-2, -2));
            cortex = brain.gameObject.AddComponent<XgCortexGraphic>();
            cortex.raycastTarget = false;
            cortex.Font = ui.font;
            cortexCaption = ui.Text(Rect("Caption", inner, Vector2.zero, new Vector2(1, 0), new Vector2(10, 4), new Vector2(-10, 24)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            cortexCaption.textWrappingMode = TextWrappingModes.NoWrap; cortexCaption.overflowMode = TextOverflowModes.Ellipsis;
            var open = map.gameObject.AddComponent<Button>(); open.transition = Selectable.Transition.None; open.targetGraphic = map.GetComponent<Image>();
            open.onClick.AddListener(() => { view.ShowTab("abilities"); Fx.Play(XgJuice.Sfx.Id.Click); });
            UiTip.Add(map, () => T("它的大脑。练过的区会亮；点开看「能力」页。", "Its brain. Regions it has trained light up; click for the Abilities page."));

            var say = ui.Card(root, "Say", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -CortexHeight - 10 - 120), new Vector2(LeftWidth, -CortexHeight - 10));
            sayWho = ui.Text(Strip("Who", say, 8, 18, 12, 12), "", 12, XgDark.Good, TextAlignmentOptions.MidlineLeft);
            sayWho.textWrappingMode = TextWrappingModes.NoWrap;
            sayText = ui.Text(Rect("Text", say, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -30)), "", 14, XgDark.Ink, TextAlignmentOptions.TopLeft);
            sayText.lineSpacing = 12; sayText.overflowMode = TextOverflowModes.Ellipsis;
            var toChat = say.gameObject.AddComponent<Button>(); toChat.transition = Selectable.Transition.None; toChat.targetGraphic = say.GetComponent<Image>();
            // Talking to it happens in YY, which it joins right after its setup; until then the card is all there is.
            toChat.onClick.AddListener(() => { if (XgYyTalk.Open()) Fx.Play(XgJuice.Sfx.Id.Click); });
            UiTip.Add(say, () => XgYyTalk.InYY ? T("它最近说的话。点一下去 YY 和它聊。", "What it said last. Click to talk to it on YY.")
                : T("它最近说的话。它很快会进 YY，到时候在那儿和它聊。", "What it said last. It joins YY soon; you talk to it there."));

            // ── right: next ability, strip, actions, feed
            var right = Rect("Right", root, Vector2.zero, Vector2.one, new Vector2(LeftWidth + 14, 0), Vector2.zero);
            nextHead = ui.Text(Strip("NextHead", right, 0, 20, 0, 260), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            nextHead.characterSpacing = 2;
            nextName = ui.Text(Rect("NextName", right, new Vector2(1, 1), Vector2.one, new Vector2(-300, -20), Vector2.zero), "", 13, XgDark.Params, TextAlignmentOptions.MidlineRight);
            nextName.textWrappingMode = TextWrappingModes.NoWrap;
            paramsCard = BigCard(right, "Params", 0, .5f, XgDark.Params);
            dataCard = BigCard(right, "Samples", .5f, 1, XgDark.Data);
            UiTip.Add(paramsCard.rim.rectTransform, () => T("已训练参数量：评估到 C 级以上的模型里最大的那个。加宽、加深在训练页（宽度和层数的上限在科技树里买）。", "Trained parameters: the biggest model assessed at grade C or better. Widen and deepen on the training page (the caps are in the tech tree)."));
            UiTip.Add(dataCard.rim.rectTransform, () => T("样本：所有数据集的有效样本。标错的、杂包里的噪声按规矩打折。", "Samples: the effective samples of every dataset. Wrong labels and junk-pack noise count against."));

            const float StripTop = 20 + 8 + 150 + 12;
            var strip = Rect("Abilities", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -StripTop - 58), new Vector2(0, -StripTop));
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                int ability = i + 1;
                var cell = Rect("Ability" + ability, strip, new Vector2(i / 6f, 0), new Vector2((i + 1) / 6f, 1), new Vector2(i == 0 ? 0 : 3, 0), new Vector2(i == 5 ? 0 : -3, 0));
                cellRims[i] = Panel(cell, XgDark.Line);
                var fill = Rect("Fill", cell, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
                cellFills[i] = Panel(fill, XgDark.Card); cellFills[i].raycastTarget = false;
                cellNames[i] = ui.Text(Rect("Name", cell, new Vector2(0, .5f), Vector2.one, new Vector2(4, -2), new Vector2(-4, -4)), "", 13, XgDark.Ink, TextAlignmentOptions.Center);
                cellNames[i].textWrappingMode = TextWrappingModes.NoWrap; cellNames[i].overflowMode = TextOverflowModes.Ellipsis;
                cellSubs[i] = ui.Text(Rect("Sub", cell, Vector2.zero, new Vector2(1, .5f), new Vector2(4, 8), new Vector2(-4, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.Center);
                cellSubs[i].textWrappingMode = TextWrappingModes.NoWrap; cellSubs[i].overflowMode = TextOverflowModes.Ellipsis;
                cellBars[i] = Bar(Rect("Progress", cell, Vector2.zero, new Vector2(1, 0), new Vector2(8, 5), new Vector2(-8, 8)), "Fill", XgDark.Track, XgDark.Params);
                var go = cell.gameObject.AddComponent<Button>(); go.transition = Selectable.Transition.None; go.targetGraphic = cellRims[i];
                go.onClick.AddListener(() => { view.ShowTab("abilities"); Fx.Play(XgJuice.Sfx.Id.Click); });
                UiTip.Add(cell, () => T(XgSim.AbilityUnlocks[ability], XgSim.AbilityUnlocksEn[ability]));
            }

            const float ActsTop = StripTop + 58 + 12;
            var row = Rect("Actions", right, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -ActsTop - 56), new Vector2(0, -ActsTop));
            acts[0] = Action(row, 0, "Train", TrainOnce);
            acts[1] = Action(row, 1, "BuyData", () => Go("data", "label"));
            acts[2] = Action(row, 2, "BuyItems", () => Go("items", null));
            acts[3] = Action(row, 3, "Wiring", () => Go("wiring", null));

            const float FeedTop = ActsTop + 56 + 12;
            var feed = Rect("Feed", right, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -FeedTop));
            feedHead = ui.Text(Strip("Head", feed, 0, 20, 0, 0), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            feedHead.characterSpacing = 2;
            for (int i = 0; i < 9; i++)
            {
                var line = Strip("Line" + i, feed, 22 + i * 26, 26, 0, 0);
                Panel(Rect("Rule", line, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), XgDark.Hairline).raycastTarget = false;
                var t = ui.Text(Rect("Text", line, Vector2.zero, Vector2.one, new Vector2(0, 1), Vector2.zero), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
                feedLines.Add(t);
            }
        }

        Big BigCard(RectTransform parent, string name, float x0, float x1, Color accent)
        {
            var card = ui.Card(parent, name, new Vector2(x0, 1), new Vector2(x1, 1), new Vector2(x0 > 0 ? 6 : 0, -28 - 150), new Vector2(x1 < 1 ? -6 : 0, -28));
            var b = new Big { rim = card.GetComponent<Image>() };
            b.head = ui.Text(Strip("Head", card, 8, 18, 12, 12), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            b.head.textWrappingMode = TextWrappingModes.NoWrap;
            b.tags = ui.Text(Rect("Tags", card, new Vector2(.35f, 1), Vector2.one, new Vector2(0, -26), new Vector2(-12, -8)), "", 12, accent, TextAlignmentOptions.MidlineRight);
            b.tags.textWrappingMode = TextWrappingModes.NoWrap; b.tags.overflowMode = TextOverflowModes.Ellipsis;
            b.value = ui.Text(Strip("Value", card, 28, 32, 12, 12), "", 24, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            b.value.textWrappingMode = TextWrappingModes.NoWrap; b.value.overflowMode = TextOverflowModes.Ellipsis;
            b.fill = Bar(Strip("Track", card, 64, 12, 12, 12), "Fill", XgDark.Track, accent);
            b.note = ui.Text(Rect("Note", card, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -84)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            b.note.lineSpacing = 8; b.note.overflowMode = TextOverflowModes.Ellipsis;
            return b;
        }

        Act Action(RectTransform row, int i, string name, Action click)
        {
            var rt = Rect(name, row, new Vector2(i / 4f, 0), new Vector2((i + 1) / 4f, 1), new Vector2(i == 0 ? 0 : 4, 0), new Vector2(i == 3 ? 0 : -4, 0));
            var face = Panel(rt, XgDark.Card);
            var rim = rt.gameObject.AddComponent<Outline>(); rim.effectDistance = new Vector2(1, -1);
            var hover = rt.gameObject.AddComponent<XgHoverRim>(); hover.rim = rim; hover.Apply();
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = face; b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => click());
            if (ui.window != null) rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = ui.window;
            var a = new Act { rt = rt, button = b };
            a.title = ui.Text(Rect("Title", rt, new Vector2(0, .5f), Vector2.one, new Vector2(12, 0), new Vector2(-8, -6)), "", 15, XgDark.Ink, TextAlignmentOptions.BottomLeft);
            a.title.textWrappingMode = TextWrappingModes.NoWrap;
            a.sub = ui.Text(Rect("Sub", rt, Vector2.zero, new Vector2(1, .5f), new Vector2(12, 6), new Vector2(-8, -2)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            a.sub.textWrappingMode = TextWrappingModes.NoWrap; a.sub.overflowMode = TextOverflowModes.Ellipsis;
            return a;
        }

        // ───────────── actions ─────────────

        /// <summary>The selected training line if it can train, else the other one.</summary>
        XgTrack TrainTrack()
        {
            var t = Sim.SelectedTrack;
            if (Sim.TrainingUnlocked(t)) return t;
            var other = t == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision;
            return Sim.TrainingUnlocked(other) ? other : t;
        }

        /// <summary>训练一轮 through the training page's own action (a hand press); a blocker sends you to that page.</summary>
        void TrainOnce()
        {
            if (!view.FeatureOpen("train")) { Fx.Play(XgJuice.Sfx.Id.Thud); view.Controller.OpenCrowd("label"); return; }
            var track = TrainTrack();
            var run = Sim.Run(track);
            if (run.epochActive) { Fx.Knock(acts[0].rt, .04f, new Vector2(8, 0)); return; }
            if (!Sim.BeginEpoch(track, Host, true))
            {
                Fx.Knock(acts[0].rt, .04f, new Vector2(10, 0)); Fx.Play(XgJuice.Sfx.Id.Thud);
                string why = Sim.Blocker(run, Host);
                if (!string.IsNullOrEmpty(why)) view.ShowToast(why, 3);
                view.ShowTab("train");
                return;
            }
            Fx.Knock(acts[0].rt, .1f);
            Fx.Play(XgJuice.Sfx.Id.Click);
            view.Refresh(true);
        }

        /// <summary>Opens a page, or (when it is not open yet) the 摆渡众包 page that leads to it.</summary>
        void Go(string page, string crowd)
        {
            Fx.Play(XgJuice.Sfx.Id.Click);
            if (view.FeatureOpen(page)) view.ShowTab(page);
            else if (crowd != null) view.Controller.OpenCrowd(crowd);
            else Fx.Knock(page == "items" ? acts[2].rt : acts[3].rt, .04f, new Vector2(8, 0));
        }

        // ───────────── refresh ─────────────

        public override void Shown() { cortexSet = false; Refresh(); }

        public override void Tick(float dt)
        {
            if (cortex == null || Sim == null) return;
            animTimer -= dt;
            if (animTimer > 0) return;
            // The thumbnail breathes at about 15 frames a second (a full rebuild each frame is the 大脑 page's job).
            animTimer = Sim.S.reduceFx ? .25f : 1 / 15f;
            cortex.Animate();
        }

        public override void Refresh()
        {
            if (cortex == null || Sim == null) return;
            RefreshCortex();
            RefreshSay();
            RefreshNext();
            RefreshStrip();
            RefreshActions();
            RefreshFeed();
        }

        void RefreshCortex()
        {
            var board = Sim.Board;
            bool one = Sim.HasAbility(XgSim.AbilityCount);
            cortex.Reduced = Sim.S.reduceFx;
            cortex.UnifiedTitle = "";
            cortex.SetUnified(one, cortexSet);
            cortex.UnifiedCells.Clear();
            int lit = 0;
            for (int i = 0; i < cortex.Regions.Length; i++)
            {
                var r = cortex.Regions[i];
                var concepts = new List<XgConcept>(board.Concepts(r.id));
                concepts.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
                string arch = r.id == XgSim.ToneRegion ? (concepts.Count > 0 ? "tone" : "") : Sim.RegionWiringOrLast(r.id)?.id ?? "";
                cortex.SetArch(i, one ? "transformer" : arch, false);
                // A region it has trained (it holds concepts) is lit; the rest stay dark.
                r.training = concepts.Count > 0 || Sim.RegionTraining(r.id);
                if (r.training) lit++;
                r.empty = concepts.Count == 0;
                r.cells.Clear();
                for (int c = 0; c < Math.Min(XgBoardPage.CortexCells, concepts.Count); c++)
                {
                    var concept = concepts[c];
                    float strength = Mathf.Clamp01((float)(Math.Abs(concept.w) / 2));
                    r.cells.Add(concept.seed ? new XgCellLook(XgCellKind.Seed, strength) : concept.alt.Length > 0 ? new XgCellLook(XgCellKind.Superposed, strength)
                        : new XgCellLook(concept.w >= 0 ? XgCellKind.Yes : XgCellKind.No, strength));
                }
                for (int c = 0; c < 14 && cortex.UnifiedCells.Count < XgCortexGraphic.UnifiedCellCount; c++)
                    cortex.UnifiedCells.Add(c < r.cells.Count ? r.cells[c] : new XgCellLook(XgCellKind.Dim, 0));
                cortex.RegionNames[i] = XgSim.RegionName(r.id, Lang.English);
                r.chip = ""; r.chipTopo = ""; r.chipChange = "";
            }
            cortexSet = true;
            cortexCaption.text = T("皮层 · ", "Cortex · ") + XgSim.ParamsText(Sim.TrainedParamsK) + T(" 参数 · 亮着的区 ", " parameters · lit regions ") + lit + "/" + cortex.Regions.Length;
        }

        void RefreshSay()
        {
            var chat = Sim.S.chat;
            XgChatLine last = null;
            for (int i = chat.Count - 1; i >= 0; i--) if (chat[i].from == "ai") { last = chat[i]; break; }
            string name = view.AiName;
            if (last == null)
            {
                sayWho.text = name;
                sayText.text = "<color=#6F95A5>" + T("它还没和你说过话。", "It has not said anything to you yet.") + "</color>";
                return;
            }
            double ago = Math.Max(0, Sim.Clock - last.at);
            string when = ago < 120 ? T("刚才", "just now") : ago < 3600 ? T(N(ago / 60, "0") + " 分钟前", N(ago / 60, "0") + " min ago") : T(N(ago / 3600, "0") + " 小时前", N(ago / 3600, "0") + " h ago");
            sayWho.text = name + " · " + when + (XgYyTalk.InYY ? T("  ·  YY 里回它 ↗", "  ·  reply on YY ↗") : "");
            sayText.text = last.text;
        }

        void RefreshNext()
        {
            int next = Sim.NextAbility;
            double p = Sim.TrainedParamsK, d = Sim.TrainedSamples;
            nextHead.text = T("下一项能力", "NEXT ABILITY");
            if (next == 0)
            {
                nextName.text = T("六项都有了", "All six");
                FillBig(paramsCard, T("已训练参数量", "Trained parameters"), "", XgSim.ParamsText(p), "", 1, T("六项能力都有了。终章在「收藏」里。", "All six abilities. The finale is under Keep."));
                FillBig(dataCard, T("样本", "Samples"), "", XgSim.SamplesText(d), "", 1, "");
                return;
            }
            nextName.text = XgSim.AbilityName(next, Sim.English);
            double baseP = XgSim.AbilityParamsK[next], nowP = Sim.ParamsThreshold(next);
            double baseD = XgSim.AbilitySamples[next], nowD = Sim.SamplesThreshold(next);
            string pOf = " / " + XgSim.ParamsText(nowP) + (nowP < baseP - 1e-9 ? T("（原 ", " (was ") + XgSim.ParamsText(baseP) + T("）", ")") : "");
            string dOf = " / " + XgSim.SamplesText(nowD) + (nowD < baseD - 1e-9 ? T("（原 ", " (was ") + XgSim.SamplesText(baseD) + T("）", ")") : "");
            FillBig(paramsCard, T("已训练参数量", "Trained parameters"), DiscountTags(next, true), XgSim.ParamsText(p), pOf, (float)Sim.ParamsProgress(next), ParamsNote());
            FillBig(dataCard, T("样本", "Samples"), DiscountTags(next, false), XgSim.SamplesText(d), dOf, (float)Sim.SamplesProgress(next), SamplesNote());
        }

        static void FillBig(Big b, string head, string tags, string value, string of, float k, string note)
        {
            b.head.text = head;
            b.tags.text = tags;
            b.value.text = value + (of.Length > 0 ? " <size=13><color=#6F95A5>" + of + "</color></size>" : "");
            SetBar(b.fill, k);
            b.note.text = note;
        }

        /// <summary>The items that lower this ability's line: owned ones lit with their factor, the rest dim.</summary>
        string DiscountTags(int ability, bool parameters)
        {
            var sb = new StringBuilder();
            foreach (var dsc in XgSim.AbilityDiscounts)
            {
                if (dsc.ability != ability) continue;
                double f = parameters ? dsc.paramsFactor : dsc.samplesFactor;
                if (f >= 1) continue;
                bool owned = Sim.DiscountOwned(dsc);
                string name = dsc.item.Length > 0 ? Sim.NodeName(XgCatalog.Node(dsc.item)) : Sim.NodeName(XgCatalog.Node(dsc.itemsAny[0]));
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(owned ? "" : "<color=#3A5566>").Append("[").Append(name).Append(" ×").Append(N(f, "0.0#")).Append("]").Append(owned ? "" : "</color>");
            }
            return sb.ToString();
        }

        string ParamsNote()
        {
            double biggest = 0;
            foreach (var run in Sim.Runs) biggest = Math.Max(biggest, XgSim.ParamsK(run));
            double vram = Sim.Vram(Host);
            string fits = vram > 300 ? T("显存 " + N(vram / 1024, "0.#") + "G 最多放约 " + XgSim.ParamsText((vram - 300) / .016) + "。", "VRAM " + N(vram / 1024, "0.#") + " GB holds about " + XgSim.ParamsText((vram - 300) / .016) + ".") : T("还没有能训练的显卡。", "No card to train on yet.");
            return T("把模型加宽加深，再训练到 C 级以上才算数。", "Widen and deepen the model, then train it to grade C or better.")
                + T("现在最大的模型 ", " The biggest model now is ") + XgSim.ParamsText(biggest) + T("；", "; ") + fits;
        }

        string SamplesNote()
        {
            double raw = 0;
            // Weighted like the bar itself (算术 counts half), so only wrong labels show up as taken off.
            foreach (var ds in XgCatalog.Datasets) if (ds.id != "xor" && ds.id != "parallel") raw += Sim.Samples(ds.id) * ds.dataWeight;
            double lost = Math.Max(0, raw - Sim.TrainedSamples);
            return T("去摆渡众包做题，或者买数据包。标错的样本打折算", "Label on Bodu Crowd, or buy data packs. Wrong labels count against")
                + (lost >= 1 ? T("：现在扣掉 " + XgSim.SamplesText(lost) + " 条。", ": " + XgSim.SamplesText(lost) + " taken off now.") : T("。", "."));
        }

        void RefreshStrip()
        {
            int next = Sim.NextAbility;
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                int ability = i + 1;
                bool learned = Sim.HasAbility(ability), isNext = ability == next;
                cellRims[i].color = learned ? XgDark.Good : isNext ? XgDark.Params : XgDark.Line;
                cellFills[i].color = learned ? XgDark.OnFill : XgDark.Card;
                cellNames[i].text = XgSim.AbilityName(ability, Sim.English);
                cellNames[i].color = learned ? XgDark.Good : XgDark.Ink;
                cellSubs[i].text = learned ? T("已学会", "learned") : ability == 1 ? T("起始", "start")
                    : XgSim.ParamsText(Sim.ParamsThreshold(ability)) + " · " + XgSim.SamplesText(Sim.SamplesThreshold(ability));
                SetBar(cellBars[i], learned ? 1 : isNext ? (float)Sim.AbilityProgress(ability) : 0);
                cellBars[i].GetComponent<Image>().color = learned ? XgDark.Good : XgDark.Params;
            }
        }

        void RefreshActions()
        {
            // 训练一轮
            var a = acts[0];
            a.title.text = Lang.T("训练一轮");
            if (!view.FeatureOpen("train")) a.sub.text = T("先去摆渡众包标够样本", "Label enough samples on Bodu Crowd first");
            else
            {
                var run = Sim.Run(TrainTrack());
                string why = Sim.Blocker(run, Host);
                var ds = XgCatalog.Dataset(run.dataset);
                a.sub.text = run.epochActive ? T("这一轮正在练…", "Training this epoch…")
                    : !string.IsNullOrEmpty(why) ? "<color=#EF9F27>" + why + "</color>"
                    : (ds != null ? T("练「" + ds.name + "」", "Train '" + ds.nameEn + "'") : "") + T(" · 第 " + (run.epoch + 1) + " 轮", " · epoch " + (run.epoch + 1));
            }
            // 买数据
            a = acts[1];
            a.title.text = T("买数据", "Buy data");
            var offer = CheapestOffer();
            a.sub.text = !view.FeatureOpen("data") ? T("去摆渡众包做题攒样本", "Label on Bodu Crowd for samples")
                : offer != null ? "「" + offer.Name(Sim.English) + "」¥" + Money(offer.price) : T("现在没有能买的数据包", "No pack for sale right now");
            // 买道具
            a = acts[2];
            a.title.text = T("买道具", "Buy items");
            if (!view.FeatureOpen("items")) a.sub.text = T("评估出成绩以后开", "Opens after the first graded model");
            else
            {
                int buyable = view.Items != null ? view.Items.BuyableCount() : 0;
                string owned = OwnedDiscountName(Sim.NextAbility);
                a.sub.text = (owned.Length > 0 ? owned + T(" 已打折 · ", " discounting · ") : "") + (buyable > 0 ? T(buyable + " 件可买", buyable + " affordable") : T("暂时没有买得起的", "Nothing affordable yet"));
            }
            // 接线
            a = acts[3];
            a.title.text = T("接线", "Wiring");
            if (!view.FeatureOpen("wiring")) a.sub.text = T("有了「多选一」或第一个检查点以后开", "Opens with 'Pick one' or the first checkpoint");
            else
            {
                var w = Sim.Wiring;
                int bad = 0;
                if (w != null) foreach (var n in w.nodes) if (n.shown && n.bad) bad++;
                a.sub.text = T((w != null ? w.running : 0) + " 条在跑", (w != null ? w.running : 0) + " running") + (bad > 0 ? " · <color=#E24B4A>" + T(bad + " 条出错", bad + " failing") + "</color>" : "")
                    + (w != null && w.tripped ? " · <color=#E24B4A>" + T("跳闸", "tripped") + "</color>" : "");
            }
        }

        /// <summary>The cheapest pack on sale now (public, junk or story data), or null.</summary>
        XgDataOffer CheapestOffer()
        {
            XgDataOffer best = null;
            foreach (var def in XgSim.DataOfferCatalog)
            {
                if (def.source == XgDataSource.Crowd || def.source == XgDataSource.Crawl) continue;
                var o = Sim.Offer(def.id);
                if (o == null || o.owned || !o.available) continue;
                if (best == null || o.price < best.price) best = o;
            }
            return best;
        }

        string OwnedDiscountName(int ability)
        {
            if (ability <= 0) return "";
            foreach (var dsc in XgSim.AbilityDiscounts)
            {
                if (dsc.ability != ability || !Sim.DiscountOwned(dsc)) continue;
                if (dsc.item.Length > 0) return Sim.NodeName(XgCatalog.Node(dsc.item));
                foreach (var id in dsc.itemsAny) if (Sim.Has(id)) return Sim.NodeName(XgCatalog.Node(id));
            }
            return "";
        }

        void RefreshFeed()
        {
            feedHead.text = T("最近发生的事", "RECENT");
            var feed = view.Feed;
            for (int i = 0; i < feedLines.Count; i++)
            {
                if (i >= feed.Count) { feedLines[i].text = i == 0 ? "<color=#3A5566>" + T("还没有新消息。", "Nothing yet.") + "</color>" : ""; continue; }
                var f = feed[i];
                string color = f.tone >= 3 ? "#E07B4F" : "#6F95A5";
                feedLines[i].text = "<color=#3A5566>" + (f.at.Length > 0 ? f.at : "··:··") + "</color>   <color=" + color + ">" + Flatten(f.text) + "</color>";
            }
        }

        static string Flatten(string text) => text == null ? "" : text.Replace("\n", " ");
    }
}
