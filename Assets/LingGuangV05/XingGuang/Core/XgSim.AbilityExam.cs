using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>One question of a sat 能力考: what was asked, what it answered, and why it was wrong.</summary>
    [Serializable]
    public sealed class XgExamItem
    {
        public string id = "", qZh = "", qEn = "", rightZh = "", rightEn = "", wrongZh = "", wrongEn = "";
        public bool brain, right, personal;
        /// <summary>Fixed difficulty: right while the readiness of its bar puts the chance above it.</summary>
        public double u;
        public string reasonZh = "", reasonEn = "";
        public string Question(bool english) => english ? qEn : qZh;
        /// <summary>What it answered.</summary>
        public string Said(bool english) => right ? (english ? rightEn : rightZh) : (english ? wrongEn : wrongZh);
    }

    /// <summary>One sitting of the 能力考 for an ability.</summary>
    [Serializable]
    public sealed class XgExamSheet
    {
        /// <summary>0 = none.</summary>
        public int ability, attempt, rightCount;
        /// <summary>The page has shown every answer (until then nothing tells the result: a replay shows the same sitting).</summary>
        public bool revealed;
        public bool passed, claimed;
        public double paramsReady, samplesReady;
        public List<XgExamItem> items = new List<XgExamItem>();
    }

    public sealed partial class XgState
    {
        /// <summary>The last 能力考 sat (ability 0 = none), and the failed ones before it (for the before/after).</summary>
        public XgExamSheet exam = new XgExamSheet();
        public List<XgExamSheet> examHistory = new List<XgExamSheet>();
        /// <summary>A failed exam can be sat again once <see cref="stageEpochs"/> reaches this.</summary>
        public int examRetryEpoch;
    }

    /// <summary>
    /// 能力考 (训练页_答卷诊断能力考_设计.md §三): the next chapter opens with an exam instead of a button. It can be sat
    /// once both bars are at <see cref="ExamOpenShare"/> of the threshold: <see cref="AbilityExamQuestions"/> questions, each
    /// leaning on one bar (要脑子 → parameters, 要见过 → samples) with a fixed difficulty, answered right while that
    /// bar's readiness puts the chance above it (about half at 80%, most at 100%, all at 120%). <see cref="AbilityExamPass"/>
    /// right passes and the ability emerges as before (<see cref="ClaimExamPass"/>). A wrong answer is the habit of
    /// the chapter before (是 / 否, an option letter, words in a heap, a sentence missing its start), and says which
    /// bar is short. A fail can be sat again after <see cref="ExamRetryRounds"/> more rounds, at no cost.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int AbilityExamQuestions = 8, AbilityExamPass = 6, ExamPersonal = 2, ExamRetryRounds = 3, ExamHistoryKept = 8;
        public const double ExamOpenShare = .8;

        public double ExamParamsReady => NextAbility <= 0 ? 0 : TrainedParamsK / Math.Max(1e-9, ParamsThreshold(NextAbility));
        public double ExamSamplesReady => NextAbility <= 0 ? 0 : TrainedSamples / Math.Max(1e-9, SamplesThreshold(NextAbility));

        /// <summary>The exam can be sat now (both bars at 80%, a round trained this chapter, no fail waiting out its rounds).</summary>
        public bool ExamReady => NextAbility > 0 && S.stageEpochs > 0 && !ExamPassPending
            && ExamParamsReady + 1e-9 >= ExamOpenShare && ExamSamplesReady + 1e-9 >= ExamOpenShare
            && S.stageEpochs >= S.examRetryEpoch;

        /// <summary>Both bars are at 80%, but a failed exam still waits for more rounds.</summary>
        public int ExamRoundsToRetry => Math.Max(0, S.examRetryEpoch - S.stageEpochs);

        /// <summary>The exam for the next ability was passed and the chapter has not turned yet.</summary>
        public bool ExamPassPending => S.exam != null && S.exam.revealed && S.exam.passed && !S.exam.claimed && S.exam.ability == NextAbility && NextAbility > 0;

        /// <summary>A sitting the page has not finished showing (the game closed mid-exam): <see cref="TakeExam"/> returns it again.</summary>
        bool ExamUnrevealed => S.exam != null && !S.exam.revealed && S.exam.ability == NextAbility && NextAbility > 0 && S.exam.items != null && S.exam.items.Count > 0;

        /// <summary>The chance one question is answered right at a bar's readiness: 0 at 60%, ½ at 80%, 0.85 at 100%, 1 at 120%.</summary>
        public static double ExamChance(double ready)
        {
            if (ready <= .6) return 0;
            if (ready <= .8) return (ready - .6) / .2 * .5;
            if (ready <= 1) return .5 + (ready - .8) / .2 * .35;
            if (ready <= 1.2) return .85 + (ready - 1) / .2 * .15;
            return 1;
        }

        /// <summary>Sits the exam for the next ability. Every answer is settled now; the page plays them one by one.</summary>
        public XgExamSheet TakeExam()
        {
            if (ExamUnrevealed) return S.exam;
            if (!ExamReady) return null;
            int ability = NextAbility;
            int attempt = 1;
            if (S.examHistory != null) foreach (var h in S.examHistory) if (h.ability == ability) attempt++;
            var sheet = new XgExamSheet { ability = ability, attempt = attempt, paramsReady = ExamParamsReady, samplesReady = ExamSamplesReady };
            var rng = new Random(StableHash("exam." + ability) ^ S.dataSalt * 131 ^ attempt * 7919);
            foreach (var item in PersonalQuestions(ability, rng)) sheet.items.Add(item);
            var bank = new List<XgExamQuestion>();
            foreach (var q in XgExamBank.All) if (ability == AbilityCount ? q.ability >= 2 && q.ability < AbilityCount : q.ability == ability) bank.Add(q);
            while (sheet.items.Count < AbilityExamQuestions && bank.Count > 0)
            {
                int i = rng.Next(bank.Count);
                var q = bank[i]; bank.RemoveAt(i);
                sheet.items.Add(new XgExamItem
                {
                    id = q.id, qZh = q.zh, qEn = q.en, rightZh = q.rightZh, rightEn = q.rightEn, brain = q.brain,
                    wrongZh = ExamWrongAnswer(q.ability, q.kind, q.rightZh, false, rng), wrongEn = ExamWrongAnswer(q.ability, q.kind, q.rightEn, true, rng),
                });
            }
            // Shuffled, so the personal questions are not always first.
            for (int i = sheet.items.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = sheet.items[i]; sheet.items[i] = sheet.items[j]; sheet.items[j] = t; }
            foreach (var item in sheet.items)
            {
                item.u = rng.NextDouble();
                double ready = item.brain ? sheet.paramsReady : sheet.samplesReady;
                item.right = item.u < ExamChance(ready);
                if (item.right) continue;
                item.reasonZh = item.brain ? "参数不够：这题要把几件事连起来想（参数到了门槛的 " + F(ready * 100, "0") + "%）"
                                           : "没见过：这种题它见得太少（样本到了门槛的 " + F(ready * 100, "0") + "%）";
                item.reasonEn = item.brain ? "Not enough parameters: this one needs several things put together (parameters at " + F(ready * 100, "0") + "% of the line)"
                                           : "Not seen enough: it has met too few like this (samples at " + F(ready * 100, "0") + "% of the line)";
            }
            foreach (var item in sheet.items) if (item.right) sheet.rightCount++;
            sheet.passed = sheet.rightCount >= AbilityExamPass;
            S.exam = sheet;
            return sheet;
        }

        /// <summary>The page has shown every answer: a pass waits for 「继续」, a fail waits <see cref="ExamRetryRounds"/> rounds.</summary>
        public void RevealExam()
        {
            var sheet = S.exam;
            if (sheet == null || sheet.revealed || sheet.ability == 0) return;
            sheet.revealed = true;
            if (sheet.passed) return;
            S.examRetryEpoch = S.stageEpochs + ExamRetryRounds;
            if (S.examHistory == null) S.examHistory = new List<XgExamSheet>();
            S.examHistory.Add(sheet);
            while (S.examHistory.Count > ExamHistoryKept) S.examHistory.RemoveAt(0);
        }

        /// <summary>After a passed exam: the ability emerges and the chapter turns (as the old chapter button did).</summary>
        public bool ClaimExamPass()
        {
            if (!ExamPassPending) return false;
            S.exam.claimed = true;
            S.examRetryEpoch = 0;
            EmergeAbility(S.exam.ability);
            return true;
        }

        // ───────────── questions ─────────────

        /// <summary>Two questions from the player's own game: the name it was given, how to call the player, a card taught by hand, words from the chat.</summary>
        List<XgExamItem> PersonalQuestions(int ability, Random rng)
        {
            var list = new List<XgExamItem>();
            string[] order;
            switch (ability)
            {
                case 2: order = new[] { "hand", "name" }; break;
                case 3: order = new[] { "name", "hand" }; break;
                case 4: order = new[] { "callme", "name" }; break;
                case 5: order = new[] { "memory", "callme" }; break;
                default: order = new[] { "memory", "name" }; break;
            }
            foreach (var kind in order)
            {
                var item = Personal(kind, ability, rng);
                if (item != null) list.Add(item);
                if (list.Count >= ExamPersonal) break;
            }
            return list;
        }

        XgExamItem Personal(string kind, int ability, Random rng)
        {
            var p = Profile;
            string own = ability == 5 || ability == AbilityCount ? "memory" : "";
            switch (kind)
            {
                case "name":
                    if (p == null || string.IsNullOrEmpty(p.name)) return null;
                    return PersonalItem("name", ability, false, "你给我起的名字是什么？", "What name did you give me?", p.name + "。", p.name + ".", own, rng);
                case "callme":
                    if (p == null || string.IsNullOrEmpty(p.callMe) || p.callMe == "你") return null;
                    return PersonalItem("callme", ability, false, "你让我怎么称呼你？", "What did you ask me to call you?", p.callMe + "。", p.callMe + ".", own, rng);
                case "hand":
                    if (S.sheetHand == null || S.sheetHand.Count == 0) return null;
                    var c = S.sheetHand[S.sheetHand.Count - 1 - rng.Next(Math.Min(3, S.sheetHand.Count))];
                    XgBoardCard b;
                    try { b = XgBoardData.FromCard(c); } catch (Exception) { return null; }
                    string zh = CardTextZh(b, c.dataset), en = CardTextEn(b, c.dataset);
                    if (string.IsNullOrEmpty(zh)) return null;
                    // A card it was taught: the wrong answer is simply the other one.
                    return new XgExamItem
                    {
                        id = "me.hand", personal = true, brain = false,
                        qZh = "你亲手教过我这道：「" + zh + "」", qEn = "You taught me this one yourself: \"" + en + "\"",
                        rightZh = c.truth ? "是。" : "否。", rightEn = c.truth ? "Yes." : "No.",
                        wrongZh = c.truth ? "否。" : "是。", wrongEn = c.truth ? "No." : "Yes.",
                    };
                case "memory":
                    var pair = MemoryBeforeAfter();
                    if (pair == null || !pair.real) return null;
                    return new XgExamItem
                    {
                        id = "me.memory", personal = true, brain = true,
                        qZh = pair.Question(false), qEn = pair.Question(true),
                        rightZh = pair.After(false), rightEn = pair.After(true), wrongZh = pair.Before(false), wrongEn = pair.Before(true),
                    };
            }
            return null;
        }

        XgExamItem PersonalItem(string id, int ability, bool brain, string qZh, string qEn, string rightZh, string rightEn, string kind, Random rng)
            => new XgExamItem
            {
                id = "me." + id, personal = true, brain = brain, qZh = qZh, qEn = qEn, rightZh = rightZh, rightEn = rightEn,
                wrongZh = ExamWrongAnswer(ability == AbilityCount ? 5 : ability, kind, rightZh, false, rng),
                wrongEn = ExamWrongAnswer(ability == AbilityCount ? 5 : ability, kind, rightEn, true, rng),
            };

        /// <summary>
        /// What it answers before it has the ability: the habit of the chapter before. 2 只会是 / 否; 3 picks an option
        /// letter; 4 says the right words in a heap; 5 loses the start (a memory question: who?; a translation: the
        /// first half is gone).
        /// </summary>
        public static string ExamWrongAnswer(int ability, string kind, string right, bool english, Random rng)
        {
            switch (ability)
            {
                case 2:
                    bool yes = rng.Next(2) == 0;
                    return english ? (yes ? "Yes." : "No.") : (yes ? "是。" : "否。");
                case 3:
                    string letter = "ABCD".Substring(rng.Next(4), 1);
                    return english ? letter + "." : "选 " + letter + "。";
                case 4:
                    return Heap(right, english, rng);
                default:
                    if (kind == "translate") return LostStart(right);
                    return english ? "…Who are you talking about?" : "……你说的是谁？";
            }
        }

        static bool Punct(char ch) => "。，？！、.,?!\"「」 ".IndexOf(ch) >= 0;

        /// <summary>The right words, in a heap: characters (or English words) shuffled so the order is never the right one.</summary>
        static string Heap(string right, bool english, Random rng)
        {
            var parts = new List<string>();
            if (english) { foreach (var w in right.Split(' ')) { string t = w.Trim('.', ',', '?', '!').ToLowerInvariant(); if (t.Length > 0) parts.Add(t); } }
            else foreach (char ch in right) if (!Punct(ch)) parts.Add(ch.ToString());
            if (parts.Count < 2) return english ? "… " + string.Join(" ", parts) + " …" : "……" + string.Join("", parts) + "……";
            var heap = new List<string>(parts);
            for (int tries = 0; tries < 6; tries++)
            {
                for (int i = heap.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = heap[i]; heap[i] = heap[j]; heap[j] = t; }
                if (!Same(heap, parts)) break;
            }
            if (Same(heap, parts)) heap.Reverse();
            return string.Join(english ? "  " : "　", heap);
        }

        static bool Same(List<string> a, List<string> b) { for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false; return true; }

        /// <summary>A sentence with its first half gone (定长瓶颈: the start did not make it through).</summary>
        static string LostStart(string right)
        {
            if (right.IndexOf(' ') >= 0)
            {
                var words = right.Split(' ');
                var sb = new StringBuilder("…");
                for (int i = words.Length / 2; i < words.Length; i++) sb.Append(i > words.Length / 2 ? " " : "").Append(words[i]);
                return sb.ToString();
            }
            return "……" + right.Substring(right.Length / 2);
        }

        // ───────────── the learned moment ─────────────

        /// <summary>
        /// The before/after of the emergence from the exam just passed: a question it really got wrong in a failed
        /// sitting and gets right now (real), else a question it got right with the old habit as the before.
        /// </summary>
        XgBeforeAfter ExamBeforeAfter(int ability)
        {
            var e = S.exam;
            if (e == null || e.ability != ability || !e.passed) return null;
            if (S.examHistory != null)
                for (int h = S.examHistory.Count - 1; h >= 0; h--)
                {
                    var old = S.examHistory[h];
                    if (old.ability != ability) continue;
                    foreach (var now in e.items)
                    {
                        if (!now.right) continue;
                        foreach (var then in old.items)
                            if (then.id == now.id && !then.right && then.wrongZh != now.rightZh)
                                return new XgBeforeAfter(now.qZh, now.qEn, then.wrongZh, then.wrongEn, now.rightZh, now.rightEn, true);
                    }
                }
            XgExamItem pick = null;
            foreach (var now in e.items) if (now.right && now.wrongZh != now.rightZh && (pick == null || pick.personal && !now.personal)) pick = now;
            return pick == null ? null : new XgBeforeAfter(pick.qZh, pick.qEn, pick.wrongZh, pick.wrongEn, pick.rightZh, pick.rightEn);
        }
    }
}
