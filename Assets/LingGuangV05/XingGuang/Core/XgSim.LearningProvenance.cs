namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        public const string PlayerAnswerSource = "player", AiActionSource = "ai_action";

        /// <summary>An automatic action is evidence of model behaviour, never a player's answer or a lesson.</summary>
        public long RecordAiAction(string dataset, string question, string questionEn, string action)
        {
            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(action)) return 0;
            return AppendLearning(new XgAskedEntry { sourceKind = AiActionSource, answerSource = AiActionSource,
                bankId = dataset, question = question, questionEn = questionEn, playerAnswer = "", aiAnswer = action }).id;
        }
        public static bool HasPlayerAnswer(XgAskedEntry record) => record != null
            && record.answerSource == PlayerAnswerSource && record.sourceKind != AiActionSource
            && !string.IsNullOrWhiteSpace(record.playerAnswer);
        public static bool HasPlayerTeachingAnswer(XgAskedEntry record, XgTeachingNote note)
            => HasPlayerAnswer(record) && record.sourceKind == "question" && note != null
            && note.answerSource == PlayerAnswerSource && note.id > 0 && record.id > 0
            && record.teachingNoteId == note.id && note.learningRecordId == record.id;

        public string LearningAnswerSummary(XgAskedEntry record)
        {
            if (record == null) return "";
            // Retests inherit answer provenance; replay never turns an AI action or old unknown into supervision.
            if (record.answerSource == AiActionSource || record.sourceKind == AiActionSource)
                return T("AI 作答（非玩家判断）：", "AI response (not a player judgment): ") + (record.aiAnswer ?? "");
            string prefix = HasPlayerAnswer(record) ? T("你：", "You: ")
                : record.answerSource == "dataset_label" ? T("题库标签：", "Dataset label: ")
                : T("记录答案（来源不明）：", "Recorded answer (source unknown): ");
            return prefix + (record.playerAnswer ?? "")
                + (string.IsNullOrEmpty(record.aiAnswer) ? "" : " · " + AiDisplayName + ": " + record.aiAnswer);
        }
        public string TeachingAnswerSummary(XgTeachingNote note)
        {
            if (note == null) return "";
            return (note.answerSource == PlayerAnswerSource ? T("你的答案：", "Your answer: ")
                : T("记录答案（来源不明）：", "Recorded answer (source unknown): "))
                + (string.IsNullOrEmpty(note.answerText) ? T(note.playerAnswer ? "是" : "否", note.playerAnswer ? "Yes" : "No") : note.answerText);
        }
    }
}
