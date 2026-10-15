using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 学习率 strip over the train button (XgSim.Rates.cs): 自动, then five steps around the model's safe rate, each with
    /// what it does to speed and the chance it 炸. A pick holds until the player picks 自动 again; a round that blows up
    /// gets the full treatment over the curve (<see cref="Exploded"/>).
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const float RateStripHeight = 44, RateStripGap = 6;
        RectTransform rateStrip;
        readonly XgBtn[] rateBtns = new XgBtn[1 + 5];

        void BuildRates()
        {
            rateStrip = Rect("RateStrip", trainRow, new Vector2(0, 1), Vector2.one, new Vector2(0, -RateStripHeight), Vector2.zero);
            Panel(rateStrip, new Color32(10, 17, 24, 255));
            var title = ui.Text(Rect("Title", rateStrip, Vector2.zero, new Vector2(0, 1), new Vector2(10, 0), new Vector2(76, 0)), "", 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            title.text = "<b>" + T("学习率", "Rate") + "</b>\n<size=10><color=#E24B4A>" + T("险", "risk") + "</color><color=#6F95A5>←→</color><color=#5DCAA5>" + T("稳", "safe") + "</color></size>";
            var row = Rect("Steps", rateStrip, Vector2.zero, Vector2.one, new Vector2(80, 4), new Vector2(-4, -4));
            for (int i = 0; i < rateBtns.Length; i++)
            {
                int slot = i;
                var b = ui.Button(row, "", () => PickRate(slot), 14);
                b.rt.anchorMin = new Vector2(i / (float)rateBtns.Length, 0); b.rt.anchorMax = new Vector2((i + 1) / (float)rateBtns.Length, 1);
                b.rt.offsetMin = new Vector2(2, 0); b.rt.offsetMax = new Vector2(-2, 0);
                b.rt.gameObject.name = i == 0 ? "RateAuto" : "RateStep" + XgSim.RateNotches[i - 1];
                b.label.richText = true;
                b.label.alignment = TextAlignmentOptions.Center;
                b.label.enableAutoSizing = true; b.label.fontSizeMin = 9; b.label.fontSizeMax = 14;
                b.label.rectTransform.offsetMin = new Vector2(3, 1); b.label.rectTransform.offsetMax = new Vector2(-3, -1);
                rateBtns[i] = b;
                UiTip.Add(b.rt, () => RateSlotTip(slot));
            }
            UiTip.Add(title.rectTransform, () => RateTip());
        }

        static int NotchOf(int slot) => slot <= 0 ? 0 : XgSim.RateNotches[slot - 1];

        void PickRate(int slot)
        {
            var run = Sim.Run(Track);
            if (run.epochActive || !Sim.SetRate(Track, NotchOf(slot), slot == 0))
            {
                Fx.Play(XgJuice.Sfx.Id.Thud, 1, .5f);
                view.ShowToast(T("这一轮练完才能换学习率。", "Change the rate after this round."), 2.5f);
                return;
            }
            var at = Fx.At(rateBtns[slot].rt);
            double tear = Sim.TearChance(run);
            if (tear > 0)
            {
                Fx.Play(XgJuice.Sfx.Id.Charge, 1.3f, .45f);
                Fx.Float(at, T("炸的概率 ", "Blow-up chance ") + N(tear * 100, "0") + "%", XgDark.Bad, 16, 40, 1.1f);
            }
            else
            {
                Fx.Play(XgJuice.Sfx.Id.Clack, 1, .6f);
                Fx.Float(at, slot == 0 ? T("自动：最大的不炸步子", "Auto: the biggest safe step") : NotchOf(slot) < 0 ? T("稳，但慢", "Steady, but slow") : T("刚好不炸", "Just short of tearing"), XgDark.Good, 14, 36, .9f);
            }
            Fx.Knock(rateBtns[slot].rt, .08f);
            Refresh();
        }

        void RefreshRates(XgRun run, bool unlocked)
        {
            if (rateStrip == null) return;
            rateStrip.gameObject.SetActive(unlocked);
            if (!unlocked) return;
            int chosen = run.autoLr ? 0 : 1 + System.Array.IndexOf(XgSim.RateNotches, Sim.RateNotch(run));
            bool locked = run.epochActive;
            for (int i = 0; i < rateBtns.Length; i++)
            {
                int notch = NotchOf(i);
                double tear = i == 0 ? 0 : Sim.TearChanceAt(notch);
                Color tone = i == 0 ? XgDark.Accent : tear >= .5 ? XgDark.Bad : tear > 0 ? XgDark.Money : notch == 0 ? XgDark.Good : XgDark.Params;
                bool on = i == chosen;
                string head = i == 0 ? T("自动", "Auto") : Sim.RateShownText(run, notch);
                string sub = i == 0 ? T("不会炸 ×1", "never blows up ×1")
                    : Sim.RateTierName(notch) + " ×" + N(XgSim.RateGainAt(notch), "0.0#")
                      + (tear > 0 ? " · " + T("炸 ", "blow-up ") + N(tear * 100, "0") + "%" : notch == 0 ? T(" · 不炸", " · no blow-up") : XgSim.RateSteadyAt(notch) <= 0 ? T(" · 不退步", " · never drops") : T(" · 少退步", " · fewer drops"));
                rateBtns[i].Set("<b>" + head + "</b>\n<size=70%>" + sub + "</size>", !locked || on,
                    on ? Color.Lerp(XgDark.Card, tone, .62f) : Color.Lerp(XgDark.Card, tone, .12f),
                    on ? Color.white : Color.Lerp(XgDark.Ink, tone, .45f));
            }
        }

        string RateSlotTip(int slot)
        {
            var run = Sim.Run(Track);
            int notch = NotchOf(slot);
            if (slot == 0) return T("自动：每轮用这个模型「刚好不炸」的学习率（" + Sim.RateShownText(run, 0) + "）。永远不会炸。", "Auto: each round uses this model's largest safe rate (" + Sim.RateShownText(run, 0) + "). It never blows up.");
            double tear = Sim.TearChanceAt(notch);
            string s = T("学习率 ", "Rate ") + Sim.RateShownText(run, notch) + "\n" + T("学得快慢：×", "Learning speed: ×") + N(XgSim.RateGainAt(notch), "0.#");
            if (tear > 0) s += "\n" + T("每轮炸的概率：", "Chance each round blows up: ") + N(tear * 100, "0") + "%" + T("。炸了这一轮白练，模型退回一截，连击清零。", ". A blow-up wastes the round, sets the model back and ends the combo.");
            else if (notch < 0) s += "\n" + (XgSim.RateSteadyAt(notch) <= 0 ? T("这一档不会退步：稳稳地慢慢涨。", "This step never drops: slow and steady.") : T("退步的概率降到 ", "Drop chance down to ") + N(XgSim.RateSteadyAt(notch) * 100, "0") + "%");
            s += "\n" + T(notch > 0 ? "高风险高回报。" : notch < 0 ? "低风险低回报。" : "", notch > 0 ? "High risk, high return." : notch < 0 ? "Low risk, low return." : "");
            return s;
        }

        string RateTip() => T(
            "学习率是每练一张卡、权重改多大一步。越往左风险越高、回报越高；越往右越稳、涨得越慢。\n步子大：学得快，但一张错得很离谱的卡会把权重推到无穷大（NaN），这一轮就炸了。\n步子小：稳，但慢。\n中间那格「刚好不炸」是这个模型能用的最大步子；模型越深越容易炸，梯度裁剪、预热、BatchNorm 能让它更扛得住。",
            "The learning rate is how far the weights move per card. Further left is higher risk and higher return; further right is steadier and slower.\nBig steps learn fast, but one badly wrong card can push the weights to infinity (NaN): the round blows up.\nSmall steps are steady but slow.\nThe middle one is the biggest step this model can take safely; deeper models blow up sooner, and gradient clipping, warm-up and BatchNorm let it take more.");

        /// <summary>A round blew up (the player's rate was too big): over the curve, loud.</summary>
        public void Exploded(int track)
        {
            if (!view.Visible || view.Tab != "train" || (XgTrack)track != Track || chartArea == null) return;
            var at = Fx.At(chartArea);
            Fx.Shockwave(at, XgDark.Bad, 280, .55f, 14);
            Fx.Burst(at, 42, XgDark.Bad, XgJuice.Shape.Spark, 520, 900, .7f);
            Fx.Float(at, T("炸了 · NaN", "Blew up · NaN"), XgDark.Bad, 40, 70, 1.6f, 1.2f);
            Fx.Play(XgJuice.Sfx.Id.Break, .9f, 1);
        }
    }
}
