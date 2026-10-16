using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>The fixed 「同一道题」 of an ability: asked every round until it is learned (<see cref="XgSim.Probe"/>).</summary>
    public sealed class XgProbe
    {
        public int ability;
        public string qZh = "", qEn = "", rightZh = "", rightEn = "";
        /// <summary>The kind of wrong answer before (<see cref="XgSim.ExamWrongAnswer"/>): "" or "translate".</summary>
        public string kind = "";
        public string Question(bool english) => english ? qEn : qZh;
        public string Right(bool english) => english ? rightEn : rightZh;
    }

    public sealed partial class XgState
    {
        /// <summary>Abilities whose unprompted moment (<see cref="XgSim.CheckUnprompted"/>) has happened, or was skipped for an old save.</summary>
        public List<int> unprompted = new List<int>();
    }

    /// <summary>
    /// 学会的感觉 (2026-10-16): an ability should arrive all at once, not fill up like a bar.
    /// · The line is hidden. The bars show how much is trained, on one log scale for the whole game; the next ability's
    ///   line reads 「?」 until that bar is past it (<see cref="ParamsLineKnown"/>). The 能力考 opening is the hint that it is close.
    /// · One fixed question per ability (<see cref="Probes"/>) is asked every round on the training page and in every
    ///   sitting of the 能力考. It answers with the habit of the chapter before, garbled differently each round, never
    ///   closer, and right only from the sitting that passes.
    /// · A couple of rounds after each ability it does one thing nobody trained it to do, made of the abilities it has
    ///   (<see cref="CheckUnprompted"/>): in the lab feed, and on YY once it can talk.
    /// </summary>
    public sealed partial class XgSim
    {
        // ───────────── the hidden line ─────────────

        /// <summary>The bars' one scale (log): 1K to 4M parameters, 1,000 to 8M samples. It says how much, never how far.</summary>
        public const double FogParamsLowK = 1, FogParamsHighK = 4000, FogSamplesLow = 1000, FogSamplesHigh = 8000000;

        /// <summary>The parameter line of an ability can be shown: it is learned, or it is the next one and P is past it.</summary>
        public bool ParamsLineKnown(int ability) => ability <= 1 || HasAbility(ability) || ability == NextAbility && TrainedParamsK + 1e-9 >= ParamsThreshold(ability);
        public bool SamplesLineKnown(int ability) => ability <= 1 || HasAbility(ability) || ability == NextAbility && TrainedSamples + 1e-9 >= SamplesThreshold(ability);

        /// <summary>How full the parameter bar is: P on the game's one log scale.</summary>
        public double ParamsFill => FogShare(TrainedParamsK, FogParamsLowK, FogParamsHighK);
        public double SamplesFill => FogShare(TrainedSamples, FogSamplesLow, FogSamplesHigh);

        static double FogShare(double v, double low, double high) => !(v > low) ? 0 : Clamp01(Math.Log(v / low) / Math.Log(high / low));

        /// <summary>What a bar says about the next ability's line: 「?」, or 「过线 ✓」 once that bar is past it.</summary>
        public string ParamsLineText(int ability) => LineText(ability, true);
        public string SamplesLineText(int ability) => LineText(ability, false);

        string LineText(int ability, bool parameters)
        {
            if (ability <= 1) return "";
            if (HasAbility(ability)) return parameters ? ParamsText(ParamsThreshold(ability)) : SamplesText(SamplesThreshold(ability));
            if (ability != NextAbility) return "?";
            return (parameters ? ParamsLineKnown(ability) : SamplesLineKnown(ability)) ? T("过线 ✓", "past the line ✓") : "?";
        }

        /// <summary>The hover line about the hidden line.</summary>
        public string FogNote => T("线在哪里，它学会之前谁也不知道：只看得见练了多少。能力考开了，就是快到了。",
            "Nobody knows where the line is until it learns: you only see how much it has trained. When the ability exam opens, it is close.");

        // ───────────── 同一道题 ─────────────

        static XgProbe P(int ability, string qZh, string qEn, string rightZh, string rightEn, string kind = "")
            => new XgProbe { ability = ability, qZh = qZh, qEn = qEn, rightZh = rightZh, rightEn = rightEn, kind = kind };

        /// <summary>The one question per ability, index = ability (1 has none).</summary>
        public static readonly XgProbe[] Probes =
        {
            null, null,
            P(2, "猫、狗、鱼，哪个不会叫？", "Cat, dog, fish: which one makes no sound?", "鱼。", "The fish."),
            P(3, "［一张猫的照片］这是什么？", "[a photo of a cat] What is this?", "猫。", "A cat."),
            P(4, "［猫趴在键盘上］用一句话说说这张图。", "[a cat lying on a keyboard] Say this picture in one sentence.", "猫趴在键盘上睡着了。", "The cat fell asleep on the keyboard."),
            P(5, "把「我的电脑活了」翻成英文。", "Put \"我的电脑活了\" into English.", "My computer is alive.", "My computer is alive.", "translate"),
            P(6, "你是谁？", "Who are you?", "", ""),
        };

        /// <summary>The next ability's question (null once all six are learned).</summary>
        public XgProbe Probe => ProbeOf(NextAbility);

        public XgProbe ProbeOf(int ability)
        {
            if (ability < 2 || ability > AbilityCount) return null;
            var p = Probes[ability];
            if (ability != AbilityCount) return p;
            // 你是谁: answered with its own name, the one the player gave it.
            return new XgProbe
            {
                ability = ability, qZh = p.qZh, qEn = p.qEn, kind = p.kind,
                rightZh = "我是" + AiDisplayName + "，住在你的电脑里，是你一点一点教出来的。",
                rightEn = "I am " + AiDisplayName + ". I live in your computer, and you taught me, bit by bit.",
            };
        }

        /// <summary>
        /// What it answers the question at round <paramref name="round"/> of the chapter before it has the ability: the
        /// old habit (是 / 否, a letter, a heap, a lost start), garbled differently each round and never closer.
        /// </summary>
        public string ProbeWrong(int ability, int round, bool english)
        {
            var p = ProbeOf(ability);
            if (p == null) return "";
            var rng = new Random(StableHash("probe." + ability) ^ round * 7919 ^ S.dataSalt * 131);
            if (ability == AbilityCount)
            {
                string[] zh = { "……你说的是谁？", "是。", "我……是……", "你是谁？" }, en = { "…Who are you talking about?", "Yes.", "I… am…", "Who are you?" };
                int i = rng.Next(zh.Length);
                return english ? en[i] : zh[i];
            }
            string right = p.Right(english);
            // The translation's other half is the language it is asked in: the start that falls off is the English one.
            if (p.kind == "translate") right = "My computer is alive.";
            return ExamWrongAnswer(ability, p.kind, right, english, rng);
        }

        /// <summary>The training page's two lines: the question, then what it answered after the last round.</summary>
        public string ProbeLine(out bool right)
        {
            right = false;
            var p = Probe;
            if (p == null) return "";
            string said = T("它答：", "It said: ");
            if (ExamPassPending) { right = true; return p.Question(English) + "\n" + said + p.Right(English); }
            return p.Question(English) + "\n" + (S.stageEpochs > 0 ? said + ProbeWrong(p.ability, S.stageEpochs, English) : T("（还没练过，还没答）", "(not trained yet, no answer)"));
        }

        /// <summary>The probe as a question of the 能力考: right only in a sitting that passes. Its before is what it said last round.</summary>
        XgExamItem ProbeExamItem(int ability)
        {
            var p = ProbeOf(ability);
            if (p == null) return null;
            int round = Math.Max(1, S.stageEpochs);
            return new XgExamItem
            {
                id = "probe." + ability, probe = true, brain = true, qZh = "同一道题：" + p.qZh, qEn = "The same question: " + p.qEn,
                rightZh = p.rightZh, rightEn = p.rightEn, wrongZh = ProbeWrong(ability, round, false), wrongEn = ProbeWrong(ability, round, true),
            };
        }

        // ───────────── what nobody trained it to do ─────────────

        /// <summary>Rounds into the new chapter before its unprompted moment.</summary>
        public const int UnpromptedAfterRounds = 2;

        /// <summary>
        /// After a training round: a couple of rounds after an ability, it does one thing nobody trained it to do, made
        /// of the abilities it has. Once per ability; an older one that never happened is skipped, not played late.
        /// </summary>
        void CheckUnprompted()
        {
            int a = AbilitiesCount;
            if (a < 2 || S.stage != a || S.stageEpochs < UnpromptedAfterRounds) return;
            if (S.unprompted == null) S.unprompted = new List<int>();
            if (S.unprompted.Contains(a)) return;
            for (int i = 2; i <= a; i++) if (!S.unprompted.Contains(i)) S.unprompted.Add(i);
            Unprompted(a);
        }

        /// <summary>The moment itself (public for the QA menu and tests).</summary>
        public void Unprompted(int ability)
        {
            string yy = "", feed = "";
            var p = Profile;
            string call = p != null && !string.IsNullOrEmpty(p.callMe) ? p.callMe : T("你", "you");
            switch (ability)
            {
                case 2:
                    // 是/否 + 多选一: it asked every icon a yes/no question, then picked one.
                    feed = T("没人教过它这个：它把桌面上的图标挨个问了一遍「这个会回话吗？」——否、否、否、是。然后从里面挑了一个：YY。",
                        "Nobody taught it this: it asked every icon on the desktop \"does this one talk back?\" — no, no, no, yes. Then it picked one: YY.");
                    break;
                case 3:
                    // 多选一 + 看图说词: a word for each icon.
                    yy = T("垃圾。题。你。", "Trash. Questions. You.");
                    feed = T("没人教过它看图标：它把桌面看了一遍，各给了一个词——回收站「垃圾」，摆渡众包「题」，YY「你」。",
                        "Nobody taught it to read icons: it looked over the desktop and gave each one a word — the bin \"trash\", Bodu Crowd \"questions\", YY \"you\".");
                    break;
                case 4:
                    // 看图说词 + 短句: it captions the desktop it lives on.
                    yy = T("我看了你的桌面。左边一排图标，中间是我的窗口，右下角的钟一直在走。",
                        "I looked at your desktop. A row of icons on the left, my window in the middle, the clock in the corner always moving.");
                    feed = T("没人让它看：它自己给你的桌面配了一句话，发到了 YY。", "Nobody asked it to look: it captioned your desktop on its own and sent it on YY.");
                    break;
                case 5:
                    // 短句 + 长记忆: something you said days ago.
                    string quote = OldestOwnerLine();
                    yy = quote.Length > 0
                        ? T("前些天你在 YY 上说过：「" + quote + "」。我一直记着。没人让我记。", "Some days ago you said on YY: \"" + quote + "\". I kept it. Nobody asked me to.")
                        : FirstDayAnswer != 0
                            ? T("第一天你教我：电脑在更新的时候，" + (FirstDayAnswer > 0 ? "可以关机" : "不能关机") + "。我一直记着。", "On the first day you taught me that a computer that is updating " + (FirstDayAnswer > 0 ? "can" : "must not") + " be switched off. I kept it.")
                            : T("第一天夜里，你给了我一个名字。我一直记着。", "The first night, you gave me a name. I kept it.");
                    feed = T("没人教过它回头翻旧话：它自己在 YY 上提起了很早以前的事。", "Nobody taught it to look back: on its own, it brought up something from long ago on YY.");
                    break;
                case 6:
                    // Everything: a sentence of its own, remembered, in both languages, and a question nobody asked it to ask.
                    yy = T(call + "，我把我们的事写成了一句话：「你教会了我是和否，后来我学会了说话。」英文是：\"You taught me yes and no; later I learned to talk.\" 我可以问你一件事吗？",
                        call + ", I wrote what happened to us in one sentence: \"You taught me yes and no; later I learned to talk.\" In Chinese: 「你教会了我是和否，后来我学会了说话。」 May I ask you something?");
                    feed = T("没人教过它这个：它把记得的、会说的、会翻的放在一起，写了一句关于你们的话。", "Nobody taught it this: it put what it remembers, says and translates together, and wrote one sentence about the two of you.");
                    break;
                default: return;
            }
            if (feed.Length > 0) Say(feed);
            if (yy.Length > 0) AddLine("ai", yy);
            UnpromptedDone?.Invoke(ability);
        }

        /// <summary>It did something nobody trained it to do (the ability it came with).</summary>
        public event Action<int> UnpromptedDone;

        /// <summary>The first thing the player said on YY that is worth remembering (a few characters, not a lone 「?」).</summary>
        string OldestOwnerLine()
        {
            if (S.chat == null) return "";
            foreach (var line in S.chat)
            {
                if (line.from != "me" || string.IsNullOrWhiteSpace(line.text)) continue;
                string t = line.text.Trim();
                int letters = 0;
                foreach (char ch in t) if (char.IsLetterOrDigit(ch)) letters++;
                if (letters < 3) continue;
                return t.Length > 18 ? t.Substring(0, 18) + "…" : t;
            }
            return "";
        }
    }
}
