using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    public struct XgLearningTime
    {
        public int date;
        public double secondsOfDay;
        public XgLearningTime(int date, double secondsOfDay) { this.date = date; this.secondsOfDay = secondsOfDay; }
    }
    public sealed class XgLearningQuery
    {
        public string sourceKind = "", sourceId = "", text = "";
        public int offset, limit = 50;
        public bool retestableOnly, textOnly;
    }
    public sealed class XgLearningPage
    {
        public readonly List<XgAskedEntry> records = new List<XgAskedEntry>();
        public int total, offset;
        public bool HasMore => offset + records.Count < total;
    }
    public sealed class XgTeachingNotesPage
    {
        public readonly List<XgTeachingNote> records = new List<XgTeachingNote>();
        public int total, offset;
        public bool HasMore => offset + records.Count < total;
    }
    /// <summary>Only model input identifiers; deliberately contains no label, correction, explanation or old answer.</summary>
    public sealed class XgLearningRetest
    {
        public long recordId, teachingNoteId;
        public string question, questionEn;
        public bool requiresImage;
    }
    public sealed partial class XgState
    {
        public long learningRecordSerial;
    }
    public sealed partial class XgSim
    {
        /// <summary>Host supplies the game's calendar. Missing clock means unknown, never the real computer's date.</summary>
        public Func<XgLearningTime> LearningClock;
        XgState learningRecordState;
        List<XgAskedEntry> learningRecordList, learningSorted;
        int learningRecordCount = -1;
        readonly Dictionary<long, XgAskedEntry> learningById = new Dictionary<long, XgAskedEntry>();
        readonly Dictionary<string, XgAskedEntry> learningByEvent = new Dictionary<string, XgAskedEntry>(StringComparer.Ordinal);
        List<XgTeachingNote> learningNotes, learningNotesNewest;
        int learningNotesCount = -1;
        readonly Dictionary<long, XgTeachingNote> learningNotesById = new Dictionary<long, XgTeachingNote>();

        void RepairLearningNotes()
        {
            if (S.teachingNotebook == null) S.teachingNotebook = new List<XgTeachingNote>();
            if (ReferenceEquals(learningNotes, S.teachingNotebook) && learningNotesCount == S.teachingNotebook.Count) return;
            learningNotes = S.teachingNotebook; learningNotesCount = learningNotes.Count; learningNotesById.Clear(); learningNotesNewest = null;
            foreach (var note in learningNotes) if (note != null && note.id > S.teachingNoteSerial) S.teachingNoteSerial = note.id;
            foreach (var note in learningNotes)
            {
                if (note == null) continue;
                if (note.id <= 0 || learningNotesById.ContainsKey(note.id)) note.id = checked(++S.teachingNoteSerial);
                learningNotesById.Add(note.id, note);
            }
        }
        long NextLearningNoteId() { RepairLearningNotes(); return checked(++S.teachingNoteSerial); }
        public XgTeachingNote LearningTeachingNote(long id)
        {
            RepairLearningNotes(); return learningNotesById.TryGetValue(id, out var note) ? note : null;
        }
        int CompareLearningNotes(XgTeachingNote a, XgTeachingNote b)
        {
            var first = LearningRecord(a.learningRecordId); var second = LearningRecord(b.learningRecordId);
            if (first != null && second != null) return CompareLearning(first, second);
            if (first != null && first.gameDate > 0) return -1;
            if (second != null && second.gameDate > 0) return 1;
            return b.id.CompareTo(a.id);
        }
        public IEnumerable<XgTeachingNote> TeachingNotesNewest()
        {
            RepairLearningNotes();
            if (learningNotesNewest == null) { learningNotesNewest = new List<XgTeachingNote>(learningNotesById.Values); learningNotesNewest.Sort(CompareLearningNotes); }
            foreach (var note in learningNotesNewest) yield return note;
        }
        public XgTeachingNotesPage QueryTeachingNotes(int offset = 0, int limit = 50, Func<XgTeachingNote, bool> filter = null)
        {
            var page = new XgTeachingNotesPage { offset = Math.Max(0, offset) }; limit = Math.Max(1, Math.Min(200, limit));
            foreach (var note in TeachingNotesNewest())
            {
                if (filter != null && !filter(note)) continue;
                if (page.total++ < page.offset) continue;
                if (page.records.Count < limit) page.records.Add(note);
            }
            return page;
        }

        public void RepairLearningRecords()
        {
            if (S.askedLog == null) S.askedLog = new List<XgAskedEntry>();
            if (ReferenceEquals(learningRecordState, S) && ReferenceEquals(learningRecordList, S.askedLog) && learningRecordCount == S.askedLog.Count) return;
            learningRecordState = S; learningRecordList = S.askedLog; learningSorted = null;
            learningById.Clear(); learningByEvent.Clear();
            long largest = Math.Max(0, S.learningRecordSerial);
            foreach (var record in S.askedLog) if (record != null && record.id > largest) largest = record.id;
            foreach (var record in S.askedLog)
            {
                if (record == null) continue;
                if (record.id <= 0 || learningById.ContainsKey(record.id)) record.id = checked(++largest);
                learningById.Add(record.id, record);
                if (string.IsNullOrEmpty(record.sourceKind)) record.sourceKind = (record.bankId ?? "").StartsWith("train.", StringComparison.Ordinal) ? "train" : "question";
                // Legacy 'at' is an independent elapsed clock, not a calendar timestamp. Never invent a date from it.
                if (!ValidLearningTime(record.gameDate, record.gameSecondsOfDay)) { record.gameDate = 0; record.gameSecondsOfDay = -1; }
                if (!string.IsNullOrEmpty(record.eventKey) && !learningByEvent.ContainsKey(record.eventKey)) learningByEvent.Add(record.eventKey, record);
            }
            S.learningRecordSerial = largest; learningRecordCount = S.askedLog.Count;
        }
        static bool ValidLearningTime(int date, double seconds)
        {
            if (date <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds >= 86400) return false;
            try { new DateTime(date / 10000, date / 100 % 100, date % 100); return true; }
            catch (ArgumentOutOfRangeException) { return false; }
        }
        static int CompareLearning(XgAskedEntry a, XgAskedEntry b)
        {
            int order = b.gameDate.CompareTo(a.gameDate);
            if (order == 0) order = b.gameSecondsOfDay.CompareTo(a.gameSecondsOfDay);
            return order != 0 ? order : b.id.CompareTo(a.id);
        }
        List<XgAskedEntry> OrderedLearning()
        {
            RepairLearningRecords();
            if (learningSorted == null)
            {
                learningSorted = new List<XgAskedEntry>(learningById.Values);
                learningSorted.Sort(CompareLearning);
            }
            return learningSorted;
        }
        XgAskedEntry AppendLearning(XgAskedEntry record)
        {
            RepairLearningRecords();
            if (!string.IsNullOrEmpty(record.eventKey) && learningByEvent.TryGetValue(record.eventKey, out var existing)) return existing;
            record.id = checked(S.learningRecordSerial + 1);
            record.learningStage = Math.Max(1, Math.Min(6, S.stage));
            var when = LearningClock != null ? LearningClock() : new XgLearningTime(0, -1);
            if (ValidLearningTime(when.date, when.secondsOfDay)) { record.gameDate = when.date; record.gameSecondsOfDay = when.secondsOfDay; }
            else { record.gameDate = 0; record.gameSecondsOfDay = -1; }
            S.learningRecordSerial = record.id; S.askedLog.Add(record); learningRecordCount = S.askedLog.Count;
            learningById.Add(record.id, record);
            if (!string.IsNullOrEmpty(record.eventKey)) learningByEvent.Add(record.eventKey, record);
            if (learningSorted != null)
            {
                int lo = 0, hi = learningSorted.Count;
                while (lo < hi) { int mid = lo + (hi - lo) / 2; if (CompareLearning(learningSorted[mid], record) < 0) lo = mid + 1; else hi = mid; }
                learningSorted.Insert(lo, record);
            }
            return record;
        }
        public XgAskedEntry LearningRecord(long id) { RepairLearningRecords(); return learningById.TryGetValue(id, out var record) ? record : null; }
        public IEnumerable<XgAskedEntry> LearningRecordsNewest() { foreach (var record in OrderedLearning()) yield return record; }
        public XgLearningPage QueryLearningRecords(XgLearningQuery query = null)
        {
            query = query ?? new XgLearningQuery();
            var page = new XgLearningPage { offset = Math.Max(0, query.offset) };
            int limit = Math.Max(1, Math.Min(200, query.limit));
            foreach (var record in OrderedLearning())
            {
                if (!string.IsNullOrEmpty(query.sourceKind) && record.sourceKind != query.sourceKind) continue;
                if (!string.IsNullOrEmpty(query.sourceId) && record.bankId != query.sourceId) continue;
                if (query.retestableOnly && !RetestableRecord(record)) continue;
                if (query.textOnly && LearningRecordNeedsImage(record)) continue;
                if (!string.IsNullOrEmpty(query.text) && !LearningContains(record.question, query.text) && !LearningContains(record.questionEn, query.text)
                    && !LearningContains(record.aiAnswer, query.text) && !LearningContains(record.playerAnswer, query.text) && !LearningContains(record.correction, query.text)) continue;
                if (page.total++ < page.offset) continue;
                if (page.records.Count < limit) page.records.Add(record);
            }
            return page;
        }
        static bool LearningContains(string text, string query) => (text ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        static bool RetestableRecord(XgAskedEntry record) => record != null && !string.IsNullOrWhiteSpace(record.question)
            && (record.sourceKind == "question" || record.sourceKind == "train" || record.sourceKind == "retest" || record.sourceKind == "correction" || record.sourceKind == AiActionSource);
        public string LearningTimestamp(XgAskedEntry record)
        {
            if (record == null || !ValidLearningTime(record.gameDate, record.gameSecondsOfDay)) return T("未记录", "Not recorded");
            int seconds = (int)record.gameSecondsOfDay;
            return string.Format(CultureInfo.InvariantCulture, "{0:0000}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}", record.gameDate / 10000, record.gameDate / 100 % 100, record.gameDate % 100, seconds / 3600, seconds / 60 % 60, seconds % 60);
        }
        public long RecordBookCompletion(string bookId, string title, string titleEn, string completionKey)
        {
            if (string.IsNullOrWhiteSpace(bookId) || string.IsNullOrWhiteSpace(completionKey)) return 0;
            return AppendLearning(new XgAskedEntry { sourceKind = "book", bankId = bookId, question = title ?? "", questionEn = titleEn ?? "", eventKey = "book:" + completionKey }).id;
        }
        void RecordDeliveredLearningReply(int replyId, string question, string answer)
        {
            AppendLearning(new XgAskedEntry { sourceKind = "chat", bankId = "yy", question = question, aiAnswer = answer, eventKey = "yy:" + replyId });
        }
        public long RecordLearningRetest(long originalId, string deliveredAnswer, string deliveryKey)
        {
            var original = LearningRecord(originalId);
            if (!RetestableRecord(original) || string.IsNullOrWhiteSpace(deliveredAnswer) || string.IsNullOrWhiteSpace(deliveryKey)) return 0;
            return AppendLearning(new XgAskedEntry { sourceKind = "retest", bankId = original.bankId, originalId = original.id,
                question = original.question, questionEn = original.questionEn, playerAnswer = original.playerAnswer, answerSource = original.answerSource,
                aiAnswer = deliveredAnswer, teachingNoteId = original.teachingNoteId, eventKey = "retest:" + deliveryKey }).id;
        }
        public long RecordLearningCorrection(long originalId, string correction, string deliveryKey)
        {
            var original = LearningRecord(originalId);
            if (!RetestableRecord(original) || string.IsNullOrWhiteSpace(correction) || string.IsNullOrWhiteSpace(deliveryKey)) return 0;
            return AppendLearning(new XgAskedEntry { sourceKind = "correction", answerSource = PlayerAnswerSource, bankId = original.bankId, originalId = original.id,
                question = original.question, questionEn = original.questionEn, aiAnswer = original.aiAnswer, playerAnswer = correction,
                correction = correction, teachingNoteId = original.teachingNoteId, eventKey = "correction:" + deliveryKey }).id;
        }
        public XgLearningRetest LearningRetestInput(long recordId)
        {
            var record = LearningRecord(recordId); if (!RetestableRecord(record)) return null;
            return new XgLearningRetest { recordId = record.id, teachingNoteId = record.teachingNoteId, question = record.question, questionEn = record.questionEn, requiresImage = LearningRecordNeedsImage(record) };
        }
        bool LearningRecordNeedsImage(XgAskedEntry record)
        {
            var note = LearningTeachingNote(record.teachingNoteId);
            return note != null && (!string.IsNullOrEmpty(note.teachingImageKind) || !string.IsNullOrEmpty(note.patternA) || note.dataset == "mnist");
        }
        void RecordTeachingAsked(XgTeachingNote note)
        {
            learningNotesById[note.id] = note; learningNotesCount = S.teachingNotebook.Count;
            var record = AppendLearning(new XgAskedEntry { sourceKind = "question", answerSource = note.answerSource, bankId = note.dataset, question = T(note.text, note.textEn), questionEn = note.textEn,
                playerAnswer = string.IsNullOrEmpty(note.answerText) ? T(note.playerAnswer ? "是" : "否", note.playerAnswer ? "Yes" : "No") : note.answerText,
                aiAnswer = note.aiAnswer, correction = note.correction, teachingNoteId = note.id, eventKey = "teaching:" + note.id });
            note.learningRecordId = record.id;
            if (learningNotesNewest != null)
            {
                int lo = 0, hi = learningNotesNewest.Count;
                while (lo < hi) { int mid = lo + (hi - lo) / 2; if (CompareLearningNotes(learningNotesNewest[mid], note) < 0) lo = mid + 1; else hi = mid; }
                learningNotesNewest.Insert(lo, note);
            }
        }
    }
}
