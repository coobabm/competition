using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed class XgFnRound
    {
        public string id = "", askZh = "", askEn = "";
    }

    public enum XgFnRecall { Uncertain, Remembered, Forgot }

    [Serializable] public sealed class XgFnEvidence
    {
        public int round, replyId;
        public string question = "", answer = "", source = "", answerSource = "";
        public XgFnRecall recall;
    }

    // An in-flight request belongs to one simulation instance, never to a restored save or another binding.
    public sealed class XgFnRequest
    {
        internal XgSim owner;
        internal int generation, round, afterReplyId;
        internal string question;
        public string Question => question;
        public int Round => round;
    }

    public sealed partial class XgState
    {
        /// <summary>Next round: 0/1 = seed, 2 = distraction, 3 = recall, 4 = resolved.</summary>
        public int fnStep;
        public bool fnDone;
        public List<XgFnEvidence> fnEvidence = new List<XgFnEvidence>();
    }

    /// <summary>An optional stage-four memory conversation. Only delivered model replies count as evidence.</summary>
    public sealed partial class XgSim
    {
        public const string ForgetNightChronicle = "forgetnight";
        XgFnRequest fnRequest;
        int fnGeneration;

        public static readonly XgFnRound[] FnRounds =
        {
            new XgFnRound { id = "seed", askZh = "记住这个暗号：今晚吃烤冷面，多加醋。", askEn = "Remember this code: grilled cold noodles tonight, extra vinegar." },
            new XgFnRound { id = "rain", askZh = "外面又下雨了，机箱风扇嗡嗡响，今天过得真快。", askEn = "It's raining outside again, the case fan is humming, and today went by fast." },
            new XgFnRound { id = "recall", askZh = "我第一句让你记住的暗号是什么？", askEn = "What was the code I asked you to remember in my first line?" }
        };

        public bool OfferForgetNight => S.stage == 4 && !S.fnDone && (S.fnStep > 0 || ExamReady || ExamPassPending);
        public bool ForgetNightActive => S.stage == 4 && S.fnStep > 0 && !S.fnDone;
        public XgFnRound FnRound => S.fnDone || S.fnStep < 0 || S.fnStep > FnRounds.Length ? null : FnRounds[Math.Max(1, S.fnStep) - 1];
        public string ForgetNightChoice(bool english) => !OfferForgetNight || FnRound == null ? null : english ? FnRound.askEn : FnRound.askZh;

        public bool ForgetNightMatches(string text)
        {
            var round = FnRound;
            if (!OfferForgetNight || round == null || string.IsNullOrWhiteSpace(text)) return false;
            return text.Trim() == round.askZh || text.Trim() == round.askEn;
        }

        public XgFnRequest BeginForgetNightRequest(string question)
        {
            if (!ForgetNightMatches(question) || fnRequest != null) return null;
            return fnRequest = new XgFnRequest { owner = this, generation = fnGeneration, question = question.Trim(), round = Math.Max(1, S.fnStep), afterReplyId = S.yyReplySerial };
        }

        public bool ForgetNightRequestCurrent(XgFnRequest request) => request != null && ReferenceEquals(request.owner, this)
            && ReferenceEquals(request, fnRequest) && request.generation == fnGeneration
            && request.round == Math.Max(1, S.fnStep) && ForgetNightMatches(request.question);

        public void CancelForgetNightRequest()
        {
            fnGeneration++;
            fnRequest = null;
        }

        /// <summary>Commit after YY displayed the model response and registered its actual delivery receipt.</summary>
        public bool CommitForgetNightReply(XgFnRequest request, int deliveredReplyId, bool fromModel)
        {
            if (!fromModel || !ForgetNightRequestCurrent(request) || deliveredReplyId <= request.afterReplyId) return false;
            var delivered = S.yyReplies?.Find(r => r.id == deliveredReplyId);
            if (delivered == null || (delivered.question ?? "").Trim() != request.question || string.IsNullOrWhiteSpace(delivered.answer)) return false;
            if (S.fnEvidence == null) S.fnEvidence = new List<XgFnEvidence>();
            if (S.fnEvidence.Exists(e => e != null && e.replyId == deliveredReplyId)) return false;
            var evidence = new XgFnEvidence { round = request.round, replyId = deliveredReplyId,
                question = delivered.question, answer = delivered.answer, source = "yy:" + deliveredReplyId, answerSource = "local_model",
                recall = request.round == 3 ? ForgetNightRecall(delivered.answer) : XgFnRecall.Uncertain };
            S.fnEvidence.Add(evidence);
            CancelForgetNightRequest();
            if (request.round < 3) S.fnStep = request.round + 1;
            else if (evidence.recall == XgFnRecall.Uncertain) S.fnStep = 3;
            else
            {
                S.fnStep = 4; S.fnDone = true;
                NoteChronicle(ForgetNightChronicle);
            }
            return true;
        }

        /// <summary>Conservative classification: absence of the code alone never means forgetting.</summary>
        public static XgFnRecall ForgetNightRecall(string answer)
        {
            string text = (answer ?? "").Trim().ToLowerInvariant();
            bool HasAny(params string[] parts) { foreach (var part in parts) if (text.Contains(part)) return true; return false; }
            if (HasAny("没忘", "没有忘", "并未忘", "不曾忘", "not forgotten", "haven't forgotten", "have not forgotten", "didn't forget", "did not forget", "not forget",
                "不是烤冷面", "不是多加醋", "不加醋", "不要醋", "no vinegar", "not cold noodles", "not grilled cold noodles", "without vinegar", "not extra vinegar"))
                return XgFnRecall.Uncertain;
            bool code = text.Contains("烤冷面") && HasAny("多加醋", "多放醋", "醋多一点")
                || text.Contains("cold noodles") && HasAny("extra vinegar", "more vinegar");
            bool uncertain = HasAny("可能", "也许", "不确定", "猜", "好像", "或许", "maybe", "perhaps", "not sure", "guess", "might");
            bool forgot = HasAny("忘了", "忘记了", "没记住", "不记得", "想不起来", "不知道", "不清楚", "don't remember", "do not remember", "can't remember", "cannot remember", "forgot", "no idea");
            if (code && !uncertain && !forgot && !HasAny("？", "?")) return XgFnRecall.Remembered;
            if (forgot && !code && !uncertain) return XgFnRecall.Forgot;
            return XgFnRecall.Uncertain;
        }

        public XgFnEvidence ForgetNightResult => !S.fnDone || S.fnEvidence == null ? null
            : S.fnEvidence.FindLast(e => e != null && e.round == 3 && e.replyId > 0 && e.source == "yy:" + e.replyId && e.answerSource == "local_model"
                && !string.IsNullOrWhiteSpace(e.answer) && e.recall != XgFnRecall.Uncertain);

        string ForgetNightChronicleBody(bool english, string self, string you)
        {
            var result = ForgetNightResult;
            if (result == null) return "";
            string answer = result.answer.Replace("<", "‹").Replace(">", "›");
            return english
                ? "You asked me to remember 'grilled cold noodles, extra vinegar', then talked about rain and the fan. When asked for the code, my actual reply was: “" + answer + "” "
                    + (result.recall == XgFnRecall.Remembered ? "This time I recalled the code." : "This time I said I could not recall it.")
                : you + "让" + self + "记住「烤冷面，多加醋」，随后聊了雨和风扇。再问暗号时，" + self + "实际回答：「" + answer + "」"
                    + (result.recall == XgFnRecall.Remembered ? "这一次，暗号答对了。" : "这一次，回答说没能想起来。");
        }
    }
}
