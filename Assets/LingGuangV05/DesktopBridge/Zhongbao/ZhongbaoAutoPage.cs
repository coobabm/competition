using System.Collections.Generic;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
using Row = LingGuangV05.Desktop.Zhongbao.ZhongbaoRows;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>
    /// 自动提交: what 自动答题 is doing on the platform right now (XgSim.AutoPlan.cs). A summary strip (state, level, cards and
    /// ¥ per second, the pause button), a table with one row per open desk (its icon, whether the model answers it and
    /// why not, and which model: the deployed checkpoint's architecture, depth and accuracy, or 灵光's own brain for
    /// text desks), and the latest submissions with the model that made each one.
    /// </summary>
    public sealed class ZhongbaoAutoPage : XgPage
    {
        const float TopH = 92, RightW = 360, LineH = 60;

        TMP_Text state, detail, deskTitle, deskNote, feedTitle, feedNote, empty;
        XgBtn pause;
        RectTransform deskList, feedList;
        Row feed;
        readonly List<DeskLine> lines = new List<DeskLine>();

        sealed class DeskLine
        {
            public RectTransform rt;
            public Image icon, dot;
            public TMP_Text name, model, speed, recent;
            public string iconId;
        }

        public override void Build(RectTransform area)
        {
            root = Rect("auto", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── the summary strip ──
            var top = ZhongbaoSkin.Box(ui, root, "Summary", new Vector2(0, 1), Vector2.one, new Vector2(0, -TopH), Vector2.zero);
            state = ui.Text(Rect("State", top, new Vector2(0, 1), Vector2.one, new Vector2(18, -46), new Vector2(-170, -12)), "", 20, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
            state.fontStyle = FontStyles.Bold; state.textWrappingMode = TextWrappingModes.NoWrap;
            detail = ui.Text(Rect("Detail", top, Vector2.zero, new Vector2(1, 1), new Vector2(18, 8), new Vector2(-170, -48)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
            pause = ui.Button(top, "", () => { if (Sim == null) return; Sim.AutoPaused = !Sim.AutoPaused; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 14);
            pause.rt.anchorMin = pause.rt.anchorMax = new Vector2(1, .5f);
            pause.rt.offsetMin = new Vector2(-150, -20); pause.rt.offsetMax = new Vector2(-18, 20);
            UiTip.Add(top, "自动答题：模型替你的账号交题。检查点不到 60% 的桌它不碰；交出去的题平台会抽检，错的罚钱。",
                "Auto-answer: the model hands in labels under your account. It never touches a desk whose checkpoint is below 60%; the platform spot-checks what it submits and fines wrong ones.");

            // ── left: one row per open desk ──
            var desks = ZhongbaoSkin.Box(ui, root, "Desks", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-RightW - 12, -TopH - 12));
            ZhongbaoSkin.Header(ui, desks, "每类题由谁来答", "Who answers each kind of card", out deskTitle, out deskNote);
            ZhongbaoUi.TipOnHeader(desks, "每张开着的标注桌：自动答题是否在替你交、用的是哪个模型（训练页评估出的最好检查点，或第四阶段起文字题用灵光自己的大脑）。",
                "Every open labelling desk: whether auto-answer is submitting for you, and which model does it (the best checkpoint from the training page, or from stage 4 灵光's own brain for text cards).");
            deskList = ZhongbaoUi.BoxScroll(desks, "Viewport");
            empty = ui.Text(Rect("Empty", desks, Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -(ZhongbaoSkin.HeaderHeight + 24))), "", 14, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);

            // ── right: the latest submissions ──
            var right = ZhongbaoSkin.Box(ui, root, "Feed", new Vector2(1, 0), Vector2.one, new Vector2(-RightW, 0), new Vector2(0, -TopH - 12));
            ZhongbaoSkin.Header(ui, right, "最近交出去的题", "Latest submissions", out feedTitle, out feedNote);
            feedList = ZhongbaoUi.BoxScroll(right, "Viewport");
            feed = new Row(ui, feedList);
        }

        DeskLine Line(int i)
        {
            while (lines.Count <= i)
            {
                var l = new DeskLine { rt = Rect("Desk", deskList, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero) };
                l.rt.pivot = new Vector2(.5f, 1);
                Panel(l.rt, Color.clear).raycastTarget = false;
                var iconRt = Rect("Icon", l.rt, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -20), new Vector2(54, 20));
                l.icon = Panel(iconRt, Color.white); l.icon.preserveAspect = true; l.icon.raycastTarget = false;
                l.dot = Panel(Rect("Dot", iconRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, -2), new Vector2(2, 10)), ZhongbaoSkin.Green); l.dot.raycastTarget = false;
                l.name = ui.Text(Rect("Name", l.rt, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(66, 0), new Vector2(-200, -6)), "", 14, ZhongbaoSkin.Ink, TextAlignmentOptions.BottomLeft);
                l.name.textWrappingMode = TextWrappingModes.NoWrap; l.name.overflowMode = TextOverflowModes.Ellipsis;
                l.model = ui.Text(Rect("Model", l.rt, Vector2.zero, new Vector2(1, .5f), new Vector2(66, 6), new Vector2(-200, -2)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
                l.model.textWrappingMode = TextWrappingModes.NoWrap; l.model.overflowMode = TextOverflowModes.Ellipsis;
                l.speed = ui.Text(Rect("Speed", l.rt, new Vector2(1, .5f), new Vector2(1, 1), new Vector2(-196, 0), new Vector2(-14, -6)), "", 13, ZhongbaoSkin.Ink, TextAlignmentOptions.BottomRight);
                l.speed.textWrappingMode = TextWrappingModes.NoWrap; l.speed.fontStyle = FontStyles.Bold;
                l.recent = ui.Text(Rect("Recent", l.rt, new Vector2(1, 0), new Vector2(1, .5f), new Vector2(-196, 6), new Vector2(-14, -2)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.TopRight);
                l.recent.textWrappingMode = TextWrappingModes.NoWrap;
                var rule = Rect("Rule", l.rt, Vector2.zero, new Vector2(1, 0), new Vector2(12, 0), new Vector2(-12, 1));
                ZhongbaoSkin.Canvas(rule, vh => { var r = rule.rect; XgDraw.Dashed(vh, new Vector2(r.xMin, r.center.y), new Vector2(r.xMax, r.center.y), 1, 3, ZhongbaoSkin.Line); });
                lines.Add(l);
            }
            return lines[i];
        }

        static string Hex(Color c) => ZhongbaoSkin.Hex(c);

        string StateText(XgAutoDeskPlan p, out Color ink)
        {
            ink = ZhongbaoSkin.Orange;
            switch (p.state)
            {
                case XgAutoDeskState.Running: ink = ZhongbaoSkin.Green; return T("自动提交中", "Submitting");
                case XgAutoDeskState.Off: ink = ZhongbaoSkin.Mute; return T("自动答题没开", "Auto-answer off");
                case XgAutoDeskState.NoCheckpoint: ink = ZhongbaoSkin.Mute; return T("没有检查点 · 你自己标", "No checkpoint · you label it");
                case XgAutoDeskState.BelowBar: ink = ZhongbaoSkin.Red; return T("检查点 " + ZhongbaoUi.Pct(p.accuracy) + " < 60% · 不替你答", "Checkpoint " + ZhongbaoUi.Pct(p.accuracy) + " < 60% · won't answer");
                default: return HoldText(p.state);
            }
        }

        string HoldText(XgAutoDeskState s)
        {
            switch (s)
            {
                case XgAutoDeskState.PausedByYou: return T("你暂停了", "Paused by you");
                case XgAutoDeskState.Held:
                    return Sim.QualityFrozen ? T("账号冻结中", "Account frozen")
                        : Sim.CaptchaPending ? T("等你在标注台输验证码", "Waiting for the captcha on the label desk")
                        : T("验证没过，暂停 " + XgSim.FreezeClock(Sim.CaptchaPauseLeft), "Captcha failed, paused " + XgSim.FreezeClock(Sim.CaptchaPauseLeft));
                case XgAutoDeskState.InboxFull: return T("待复核满 " + XgSim.ReviewCapacity + " 题，先去复核", "Review inbox full (" + XgSim.ReviewCapacity + "), review first");
                case XgAutoDeskState.Project: return T("研发占着 GPU", "Research has the GPU");
                case XgAutoDeskState.NoPower: return Host != null && !string.IsNullOrEmpty(Host.Blocker) ? Host.Blocker : T("显卡没在算", "The GPU is not running");
                default: return "";
            }
        }

        static string BrainName()
        {
            var llm = LocalLlm.Instance;
            return llm != null && !string.IsNullOrEmpty(llm.ModelName) ? llm.ModelName : "";
        }

        string CheckpointText(XgAutoDeskPlan p)
        {
            if (p.best == null) return T("还没有检查点：先在训练页练这一类，评估出准确率", "No checkpoint yet: train this kind on the training page and assess it");
            string arch = p.arch != null ? T(p.arch.name, p.arch.nameEn) : p.best.arch;
            string text = T("模型 ", "Model ") + "<b>" + arch + "</b> · " + T(p.depth + " 层", p.depth + (p.depth == 1 ? " layer" : " layers"));
            if (p.model != null) text += " · " + T("检查点 ", "checkpoint ") + (string.IsNullOrEmpty(p.model.name) ? "#" + p.model.id : "「" + p.model.name + "」");
            return text + " · " + T("准确率 ", "accuracy ") + ZhongbaoUi.Pct(p.accuracy);
        }

        string ModelText(XgAutoDeskPlan p)
        {
            // Without a 60% checkpoint nothing answers this desk, the brain included.
            if (p.state == XgAutoDeskState.NoCheckpoint || p.state == XgAutoDeskState.BelowBar)
                return CheckpointText(p) + (p.usesBrain || p.brainFallback ? ZhongbaoUi.Muted(T(" · 到 60% 以后文字题交给灵光的大脑", " · from 60% on, 灵光's brain judges its text cards")) : "");
            if (p.usesBrain)
            {
                string brain = BrainName();
                string text = T("模型 <b>灵光的大脑</b>（本机语言模型" + (brain.Length > 0 ? " " + brain : "") + "）· 用自己的话判断",
                    "Model <b>灵光's brain</b> (local language model" + (brain.Length > 0 ? " " + brain : "") + ") · judges in its own words");
                if (p.brainJudgments > 0) text += T(" · 近 " + p.brainJudgments + " 题对 ", " · " + ZhongbaoUi.Pct(p.brainAccuracy) + " right of the last " + p.brainJudgments) + (Sim.English ? "" : ZhongbaoUi.Pct(p.brainAccuracy));
                return text;
            }
            string checkpoint = CheckpointText(p);
            return p.brainFallback ? checkpoint + ZhongbaoUi.Muted(T(" · 大脑不在线，先用检查点", " · brain offline, the checkpoint answers")) : checkpoint;
        }

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            var global = Sim.AutoGlobalState(Host);
            var plans = Sim.AutoDeskPlans(Host);
            double rate = 0, income = 0; int running = 0;
            foreach (var p in plans) if (p.Running) { running++; rate += p.rate; income += p.income; }

            // ── summary ──
            if (Sim.AutoLabelHidden)
            {
                state.text = "<color=" + Hex(ZhongbaoSkin.Mute) + ">？？？</color>";
                detail.text = T("也许有更省力的办法……先在标注台多标几张。", "Maybe there is an easier way… label a few more cards on the desk first.");
            }
            else if (global == XgAutoDeskState.Off)
            {
                state.text = "<color=" + Hex(ZhongbaoSkin.Mute) + ">" + T("自动答题还没买", "Auto-answer not bought yet") + "</color>";
                detail.text = T("在标注台右边的商店里买「自动答题（全局）」，有一张桌的检查点到 60% 就能买。", "Buy 「Auto labelling (global)」 in the shop beside the label desk; it needs one desk with a 60% checkpoint.");
            }
            else
            {
                bool on = global == XgAutoDeskState.Running;
                state.text = "<color=" + Hex(on ? ZhongbaoSkin.Green : ZhongbaoSkin.Orange) + ">● " + (on ? T("自动提交中", "Submitting") : HoldText(global)) + "</color>"
                    + "  <size=14><color=" + Hex(ZhongbaoSkin.Ink) + ">" + T("自动答题 Lv ", "Auto-answer Lv ") + Sim.GlobalAutoLevel + "/" + XgCatalog.AutoMaxLevel + "</color></size>";
                string d = T(running + " 类题在自动交 · 每秒 " + N(rate, "0.##") + " 题 · 约 +" + ZhongbaoUi.Yuan(income) + "/秒",
                    running + " kind" + (running == 1 ? "" : "s") + " submitting · " + N(rate, "0.##") + " cards/s · about +" + ZhongbaoUi.Yuan(income) + "/s");
                d += "\n" + (Sim.CollaborationEnabled
                    ? T("把握低于 " + ZhongbaoUi.Pct(Sim.S.coopThreshold) + " 的题交给你复核（待复核 " + Sim.ReviewCount() + "/" + XgSim.ReviewCapacity + "）",
                        "Cards it is less than " + ZhongbaoUi.Pct(Sim.S.coopThreshold) + " sure of go to your review inbox (" + Sim.ReviewCount() + "/" + XgSim.ReviewCapacity + ")")
                    : T("它答什么就交什么；买「拿不准才问我」后，没把握的题会留给你。", "It submits whatever it answers; with 「Ask when unsure」 the unsure ones wait for you."));
                if (Sim.QualityActive) d += T(" · 平台抽检 ", " · spot checks ") + ZhongbaoUi.Pct(Sim.SpotCheckChance);
                detail.text = d;
            }
            bool canPause = Sim.GlobalAutoLevel > 0 && !Sim.AutoLabelHidden;
            pause.rt.gameObject.SetActive(canPause);
            if (canPause)
            {
                if (Sim.AutoPaused) ZhongbaoSkin.Primary(pause, T("继续自动答题", "Resume"), true);
                else ZhongbaoSkin.Ghost(pause, T("暂停自动答题", "Pause"), true);
            }

            // ── desks ──
            deskTitle.text = T("每类题由谁来答", "Who answers each kind of card");
            deskNote.text = T(plans.Count + " 张桌开着", plans.Count + " desk" + (plans.Count == 1 ? "" : "s") + " open");
            bool hidden = Sim.AutoLabelHidden;
            empty.gameObject.SetActive(hidden || plans.Count == 0);
            if (hidden) empty.text = T("？？？", "???");
            else if (plans.Count == 0) empty.text = T("还没有开着的标注桌。", "No labelling desk is open yet.");
            int shown = hidden ? 0 : plans.Count;
            for (int i = 0; i < shown; i++)
            {
                var p = plans[i]; var l = Line(i);
                if (!l.rt.gameObject.activeSelf) l.rt.gameObject.SetActive(true);
                l.rt.offsetMin = new Vector2(0, -(i + 1) * LineH); l.rt.offsetMax = new Vector2(0, -i * LineH);
                if (l.iconId != p.desk.id)
                {
                    l.iconId = p.desk.id;
                    var sprite = XgIcons.Get("desk", p.desk.id);
                    l.icon.sprite = sprite; l.icon.color = sprite != null ? Color.white : ZhongbaoSkin.Blue3;
                }
                string status = StateText(p, out var ink);
                l.dot.color = ink;
                l.icon.color = l.icon.sprite == null ? ZhongbaoSkin.Blue3 : p.Running ? Color.white : new Color(1, 1, 1, .55f);
                l.name.text = "<b>" + T(p.desk.name, p.desk.nameEn) + "</b>  <size=12><color=" + Hex(ink) + ">" + status + "</color></size>";
                l.model.text = ModelText(p);
                l.speed.text = p.Running ? N(p.rate, "0.##") + T(" 题/秒", " cards/s") + "  <color=" + Hex(ZhongbaoSkin.Green) + ">+" + ZhongbaoUi.Yuan(p.income) + T("/秒", "/s") + "</color>" : "<color=" + Hex(ZhongbaoSkin.Dim) + ">—</color>";
                var r = p.recent;
                l.recent.text = r.automatic > 0 ? T("近 " + r.automatic + " 题自动交 · 对 " + ZhongbaoUi.Pct(r.automaticAccuracy), r.automatic + " auto · " + ZhongbaoUi.Pct(r.automaticAccuracy) + " right") : "";
            }
            for (int i = shown; i < lines.Count; i++) if (lines[i].rt.gameObject.activeSelf) lines[i].rt.gameObject.SetActive(false);
            deskList.sizeDelta = new Vector2(0, shown * LineH + 6);

            // ── the latest submissions, with the model behind each ──
            feedTitle.text = T("最近交出去的题", "Latest submissions");
            feedNote.text = T("最近 " + XgSim.FeedWindow + " 题", "last " + XgSim.FeedWindow);
            var items = new List<Row.Item>();
            var feedRecords = Sim.S.autoFeed;
            if (feedRecords.Count == 0) items.Add(Row.Note(T("还没有。自动答题交出去的题会出现在这里，标明是哪个模型答的。", "None yet. Labels auto-answer hands in show here, with the model that answered each.")));
            for (int i = feedRecords.Count - 1; i >= 0; i--)
            {
                var rec = feedRecords[i];
                var desk = XgCatalog.Desk(rec.dataset);
                string name = desk != null ? T(desk.name, desk.nameEn) : rec.dataset;
                string mark, value; Color valueInk;
                if (rec.audited) { mark = "<color=#8C540A>↖</color>"; value = T("自查拦下", "self-check caught it"); valueInk = ZhongbaoSkin.Orange; }
                else if (rec.spotChecked && !rec.correct) { mark = "<color=#BE1414>×</color>"; value = "−¥" + N(rec.fine, "0.##"); valueInk = ZhongbaoSkin.Red; }
                else if (rec.correct) { mark = "<color=#2F9E44>✓</color>"; value = "+¥" + N(rec.pay, "0.##"); valueInk = ZhongbaoSkin.Green; }
                else { mark = "<color=#D63031>×</color>"; value = T("错了 · 成了噪声", "wrong · now noise"); valueInk = ZhongbaoSkin.Mute; }
                items.Add(Row.Row(mark + " " + name, value, valueInk));
                string who;
                if (rec.judgeSource == "brain") who = T("灵光的大脑", "灵光's brain");
                else
                {
                    var best = Sim.Best(rec.dataset);
                    var arch = best != null ? XgCatalog.Arch(best.arch) : null;
                    who = T("检查点 ", "checkpoint ") + (arch != null ? T(arch.name, arch.nameEn) : best != null && !string.IsNullOrEmpty(best.arch) ? best.arch : T("模型", "model"));
                }
                string question = Sim.English || string.IsNullOrEmpty(rec.question) ? "" : "「" + Clip(rec.question, 16) + "」 ";
                items.Add(Row.Sub(question + T("答「", "answered “") + (rec.guess ? T("是", "yes") : T("否", "no")) + T("」", "”") + " · " + T("把握 ", "sure ") + ZhongbaoUi.Pct(rec.confidence) + " · " + who));
            }
            feedList.sizeDelta = new Vector2(0, feed.Set(items));
        }

        static string Clip(string text, int max)
        {
            text = text.Replace('\n', ' ');
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }

        public override void Tick(float dt) { }
    }
}
