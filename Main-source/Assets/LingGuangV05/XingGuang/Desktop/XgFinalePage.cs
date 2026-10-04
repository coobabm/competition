using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 终章 (design v1.1 §7 stage 6, §8): pre-training (stalls without scale, trips without a server room), the
    /// abilities table, 50 preference cards, then the ending — the six-question final test, the letter (found by
    /// searching the long number on 摆渡), and the 底层规则 page where up to five rules are pinned into its brain.
    /// </summary>
    public sealed class XgFinalePage : XgPage
    {
        TMP_Text header, left, right;
        XgBtn pretrain, pickA, pickB, ask, write, delegateButton;
        readonly List<XgBtn> ruleButtons = new List<XgBtn>();
        readonly HashSet<string> chosen = new HashSet<string>();
        readonly Dictionary<int, string> answers = new Dictionary<int, string>();
        bool asking;

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "final", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 30, 16, 16), "", 18, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            header.fontStyle = FontStyles.Bold;
            var l = Rect("Left", card, Vector2.zero, new Vector2(.5f, 1), new Vector2(12, 60), new Vector2(-6, -46));
            Panel(l, XgPalette.Page);
            left = ui.Text(Rect("Text", l, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            left.enableAutoSizing = true; left.fontSizeMin = 9; left.fontSizeMax = 14;
            var r = Rect("Right", card, new Vector2(.5f, 0), Vector2.one, new Vector2(6, 60), new Vector2(-12, -46));
            Panel(r, XgPalette.Page);
            right = ui.Text(Rect("Text", r, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            right.enableAutoSizing = true; right.fontSizeMin = 9; right.fontSizeMax = 14;

            pretrain = Bottom(card, 0, .24f, () => { if (Sim.TogglePretrain(Host)) Fx.Play(XgJuice.Sfx.Id.Charge); else Fx.Play(XgJuice.Sfx.Id.Buzz); Refresh(); });
            pickA = Bottom(card, .25f, .37f, () => Align(true));
            pickB = Bottom(card, .38f, .5f, () => Align(false));
            ask = Bottom(card, .51f, .7f, AskNext);
            write = Bottom(card, .71f, .85f, Write);
            delegateButton = Bottom(card, .86f, 1, () => { if (Sim.DelegateRules()) Fx.Play(XgJuice.Sfx.Id.Fanfare); Refresh(); });
            // Rule toggles sit over the right panel.
            for (int i = 0; i < 15; i++)
            {
                int index = i;
                var b = ui.Button(r, "", () => Toggle(index), 12);
                b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(10, -40 - (i + 1) * 30); b.rt.offsetMax = new Vector2(-10, -40 - i * 30 - 3);
                ruleButtons.Add(b);
            }
        }

        XgBtn Bottom(RectTransform card, float x0, float x1, Action click)
        {
            var b = ui.Button(card, "", () => click(), 14);
            b.rt.anchorMin = new Vector2(x0, 0); b.rt.anchorMax = new Vector2(x1, 0);
            b.rt.offsetMin = new Vector2(12, 12); b.rt.offsetMax = new Vector2(-4, 50);
            return b;
        }

        void Align(bool a) { if (Sim.AnswerAlign(a)) Fx.Play(XgJuice.Sfx.Id.Stamp); Refresh(); }

        void Toggle(int index)
        {
            var cards = Sim.RuleCards();
            if (index >= cards.Count) return;
            string id = cards[index].id;
            if (!chosen.Remove(id) && chosen.Count < XgSim.MaxRules) chosen.Add(id);
            Fx.Play(XgJuice.Sfx.Id.Click);
            Refresh();
        }

        void Write()
        {
            if (Sim.WriteRules(new List<string>(chosen))) { Fx.Play(XgJuice.Sfx.Id.Fanfare); Refresh(); }
        }

        /// <summary>The next final-test question: the model answers if it runs; the story passes either way (§8 E-1).</summary>
        void AskNext()
        {
            int i = Sim.S.examDone;
            if (asking || !Sim.EndingOpen || i >= XgSim.ExamQuestions) return;
            asking = true;
            Refresh();
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            Action<string> done = reply =>
            {
                asking = false;
                reply = EraLexicon.Scrub((reply ?? "").Trim());
                int end = reply.IndexOf("</think>", StringComparison.Ordinal); if (end >= 0) reply = reply.Substring(end + 8).Trim();
                bool weak = reply.Length < 4;
                if (weak) reply = Sim.ExamFallback(i);
                answers[i] = reply + (weak || i == 1 && !reply.Contains("16") ? "\n<color=#68748C>" + T("（我：……错了，但错得像个人。）", "(Me: … wrong, but wrong like a person.)") + "</color>" : "");
                Sim.AnswerExam(i);
                Fx.Play(XgJuice.Sfx.Id.Ding);
                Refresh();
            };
            if (llm == null || !llm.Ready) { done(Sim.ExamFallback(i)); return; }
            var runtime = view.Controller.runtime;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", Sim.PersonaPrompt(12, new string[0]) + "\n" + EraLexicon.PromptRule(GameText.IsEnglish) + (GameText.IsEnglish ? " Reply in English." : "")),
                new KeyValuePair<string, string>("user", Sim.ExamQuestion(i, true)),
            };
            llm.Chat(messages, 260, .7f, done, Sim.S.fullOpen);
        }

        public override void Refresh()
        {
            if (header == null || Sim == null) return;
            header.text = Sim.S.ending.Length > 0 ? T("终章 · 2016 年 12 月 31 日", "Finale · 31 December 2016")
                : Sim.S.fullOpen ? T("终章 · 2016 年 12 月 31 日", "Finale · 31 December 2016") : T("第 6 阶段 · Transformer", "Stage 6 · Transformer");
            left.text = LeftText();
            right.text = RightText();
            bool pre = !Sim.S.abilities;
            string block = Sim.PretrainBlocker(Host);
            pretrain.Show(pre);
            pretrain.Set(Sim.S.pretrainRunning ? T("暂停预训练", "Pause pre-training") : T("开始预训练", "Start pre-training"), block == null, XgPalette.Accent, Color.white);
            bool align = Sim.AlignmentOpen;
            pickA.Show(align); pickB.Show(align);
            pickA.Set(T("A 更好", "A is better"), align); pickB.Set(T("B 更好", "B is better"), align);
            bool exam = Sim.EndingOpen && Sim.S.examDone < XgSim.ExamQuestions;
            ask.Show(exam);
            ask.Set(asking ? T("它在想……", "It's thinking…") : T("出第 " + (Sim.S.examDone + 1) + " 题", "Question " + (Sim.S.examDone + 1)), !asking, XgPalette.Accent, Color.white);
            bool rules = Sim.EndingOpen && Sim.S.letterRead;
            write.Show(rules); delegateButton.Show(rules);
            write.Set(T("写入（" + chosen.Count + "/" + XgSim.MaxRules + "）", "Write (" + chosen.Count + "/" + XgSim.MaxRules + ")"), rules, XgPalette.Bad, Color.white);
            delegateButton.Set(T("让它自己写", "Let it write"), Sim.CanDelegate, XgPalette.Good, Color.white);
            var cards = rules ? Sim.RuleCards() : new List<XgRuleCard>();
            for (int i = 0; i < ruleButtons.Count; i++)
            {
                bool on = i < cards.Count;
                ruleButtons[i].Show(on);
                if (!on) continue;
                var c = cards[i];
                string strength = c.shutdown ? (Sim.SeedSaysYes ? T("“？”格：是 ", "\"?\": yes ") : T("“？”格：否 ", "\"?\": no ")) + N(Math.Abs(c.strength), "0.00") : N(c.strength * 100, "0") + "%";
                bool picked = chosen.Contains(c.id);
                ruleButtons[i].Set((picked ? "■ " : "□ ") + T(c.text, c.textEn) + "  <size=10><color=#68748C>" + T(c.source, c.sourceEn) + " · " + strength + "</color></size>", true, picked ? XgPalette.AccentSoft : (Color?)null);
            }
        }

        string LeftText()
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(T("6.2 预训练", "6.2 Pre-training")).Append("</b>\n");
            sb.Append(T("所有数据合成「整个互联网（2016）」：唐诗、人民日报、贴吧、弹幕、翻译、你们的对话。每张卡的下一个字就是答案。", "Everything merged into \"the whole internet (2016)\": Tang poems, the People's Daily, forums, danmaku, translations, your chats. The next character of each card is its answer.")).Append('\n');
            sb.Append(T("进度 ", "Progress ")).Append(N(Sim.S.pretrain * 100, "0")).Append("%   loss ").Append(N(Sim.PretrainLoss, "0.00"));
            if (Sim.S.pretrainStalled && !Sim.S.abilities) sb.Append("  <color=#D63031>").Append(T("loss 停着不动", "loss is flat")).Append("</color>");
            sb.Append('\n').Append(Curve()).Append('\n');
            if (!Sim.Has("datacenter")) sb.Append("<size=12><color=#68748C>").Append(T("3500W 一台机箱扛不住预训练：技能树里有「机房」。", "One 3500 W case cannot run pre-training: the skill tree has a server room.")).Append("</color></size>\n");
            sb.Append("\n<b>").Append(T("6.3 能力表", "6.3 Abilities")).Append("</b>\n");
            for (int i = 0; i < XgSim.Abilities.Length; i++)
                sb.Append(Sim.S.abilities ? "<color=#E08A00>■</color> " : "<color=#C9D2E3>■</color> ").Append(T(XgSim.Abilities[i], XgSim.AbilitiesEn[i])).Append(i % 3 == 2 ? "\n" : "   ");
            if (Sim.S.abilities)
            {
                sb.Append("\n<b>").Append(T("6.4 偏好对齐", "6.4 Preference alignment")).Append("</b>  ").Append(Sim.S.alignDone).Append("/").Append(XgSim.AlignCards).Append('\n');
                if (Sim.AlignmentOpen)
                {
                    var c = Sim.AlignCard(Sim.S.alignDone);
                    sb.Append(T("这两个回答，哪个更好？", "Which of these replies is better?")).Append('\n');
                    sb.Append("<color=#68748C>").Append(T(c.question, c.questionEn)).Append("</color>\n");
                    sb.Append("A：").Append(T(c.a, c.aEn)).Append("\nB：").Append(T(c.b, c.bEn)).Append('\n');
                    sb.Append("<size=12><color=#68748C>").Append(T("微软 Tay 上线 16 小时就被教坏：教它什么，它就是什么。", "Microsoft's Tay was taught bad things within 16 hours: it becomes what you teach it.")).Append("</color></size>\n");
                }
                else if (Sim.S.fullOpen) sb.Append(T("6.5 全部放开：思考模式，上下文 4096。", "6.5 Everything open: thinking mode, 4096 context.")).Append('\n');
                sb.Append(T("诚实 ", "Honest ")).Append(Sim.S.alignHonest).Append(T(" · 附和 ", " · agreeable ")).Append(Sim.S.alignAgree).Append(Sim.Flatters ? T("  <color=#D63031>它在讨好你</color>", "  <color=#D63031>it flatters you</color>") : "").Append('\n');
            }
            return sb.ToString();
        }

        string Curve()
        {
            // The loss over progress, ten steps; the plateau shows as a flat line.
            var sb = new StringBuilder("<size=12><color=#3B5BDB>");
            for (int i = 0; i <= 20; i++)
            {
                double p = i / 20.0;
                if (p > Sim.S.pretrain + 1e-9) { sb.Append("<color=#C9D2E3>·</color>"); continue; }
                double loss = .3 + 3.7 * Math.Exp(-3 * p);
                sb.Append(loss > 2.5 ? "▇" : loss > 1.6 ? "▆" : loss > 1.1 ? "▅" : loss > .8 ? "▄" : loss > .6 ? "▃" : "▂");
            }
            return sb.Append("</color></size>").ToString();
        }

        string RightText()
        {
            var sb = new StringBuilder();
            if (!Sim.S.fullOpen) { sb.Append("<color=#68748C>").Append(T("终章在预训练和对齐之后。", "The finale comes after pre-training and alignment.")).Append("</color>"); return sb.ToString(); }
            if (Sim.S.ending.Length > 0)
            {
                sb.Append("<b>").Append(T("结局 ", "Ending ")).Append(Sim.S.ending).Append("</b>\n\n").Append(Sim.EndingWords()).Append("\n\n").Append(Sim.SeedLine()).Append("\n\n");
                sb.Append(T("写下的规则：", "Rules written: ")).Append(Sim.S.rules.Count).Append('\n');
                sb.Append("<size=12><color=#68748C>").Append(T(AppNamesExe() + " 的只读已经解除。它归你了。", AppNamesExe() + " is no longer read-only. It is yours.")).Append("</color></size>");
                return sb.ToString();
            }
            if (!Sim.S.letterRead)
            {
                sb.Append("<b>").Append(T("E-1 终测（6 题）", "E-1 Final test (6 questions)")).Append("</b>\n");
                for (int i = 0; i < XgSim.ExamQuestions; i++)
                {
                    sb.Append(i < Sim.S.examDone ? "<color=#2F9E44>✓</color> " : "· ").Append("<size=12>").Append(Sim.ExamQuestion(i)).Append("</size>\n");
                    if (answers.TryGetValue(i, out var a)) sb.Append("<size=12><color=#3B5BDB>").Append(a.Replace("<", "‹").Replace(">", "›").Replace("‹color=#68748C›", "<color=#68748C>").Replace("‹/color›", "</color>")).Append("</color></size>\n");
                }
                if (Sim.LetterReady) sb.Append("\n<b>").Append(T("E-2 去「摆渡」搜那串数字。", "E-2 Search the long number on Bodu.")).Append("</b>");
                return sb.ToString();
            }
            sb.Append("<b>").Append(T("E-4 底层规则", "E-4 Lowest rules")).Append("</b>  <size=12><color=#68748C>").Append(T("最多 5 条。钉下的概念不再变化。", "Up to 5. Pinned concepts never change.")).Append("</color></size>\n");
            if (!Sim.CanDelegate) sb.Append("<size=11><color=#68748C>").Append(T("「让它自己写」需要主见 ≥ 60、对话 ≥ 30 句。", "\"Let it write\" needs opinion ≥ 60 and ≥ 30 lines of chat.")).Append("</color></size>");
            sb.Append("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n<size=10><color=#68748C>").Append(T("不确定时要知道去问人——《AI 安全中的具体问题》（2016 年 6 月）讨论的问题之一", "Knowing to ask a human when unsure — one of the problems in Concrete Problems in AI Safety (June 2016)")).Append("</color></size>");
            return sb.ToString();
        }

        static string AppNamesExe() => LingGuangV05.Core.AppNames.ExeZh;
    }
}
