using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgPersonaDay
    {
        public int day;
        public double warmth, play, opinion;
    }
    [Serializable] public sealed class XgPersonaReason
    {
        public int day;
        public string source = "", element = "";
        public bool positive;
        public double[] before = new double[3], after = new double[3];
    }
    public sealed partial class XgState
    {
        public int personaRatedReplies, personaArchiveRevision;
        public List<XgPersonaDay> personaDays = new List<XgPersonaDay>();
        public List<XgPersonaReason> personaReasons = new List<XgPersonaReason>();
    }
    public sealed partial class XgSim
    {
        public const string PersonaArchiveNode = "persona.archive";
        public const int PersonaArchivePrice = 300, PersonaArchiveDayLimit = 14, PersonaArchiveReasonLimit = 64;
        public int PersonaRatingCount
        {
            get { int retained = 0; foreach (var line in S.chat) if (line != null && line.from == "ai" && line.rating != 0) retained++;
                return Math.Max(S.personaRatedReplies, retained); }
        }
        public bool PersonaArchiveUnlocked => S.unlocked.Contains(PersonaArchiveNode);
        public bool PersonaArchiveEligible => S.stage >= 2 && (PersonaRatingCount >= 15 || PhenomenonSeen("drift"));
        public string PersonaArchiveBlocker()
        {
            if (PersonaArchiveUnlocked) return null;
            if (S.stage < 2) return T("第 2 阶段开放性格档案。", "Personality archive opens in stage 2.");
            return PersonaArchiveEligible ? null : T("累计赞/踩 15 次，或发现性格漂移后解锁（", "Unlock after 15 ratings or observed personality drift (") + PersonaRatingCount + T("/15）。", "/15).");
        }
        public double[] PersonaAxes() => new[] { ActualAxis(0), ActualAxis(1), ActualAxis(2) };
        static bool PersonaDate(int day, out DateTime date) => DateTime.TryParseExact(day.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        void TrimPersonaArchive()
        {
            if (S.personaDays == null) S.personaDays = new List<XgPersonaDay>();
            if (S.personaReasons == null) S.personaReasons = new List<XgPersonaReason>();
            S.personaDays.RemoveAll(x => x == null);
            S.personaReasons.RemoveAll(x => x == null);
            if (TodayKnown && PersonaDate(Today, out var today))
            {
                int oldest = int.Parse(today.AddDays(-13).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                S.personaDays.RemoveAll(x => x.day < oldest);
                S.personaReasons.RemoveAll(x => x.day > 0 && x.day < oldest);
            }
            S.personaDays.Sort((a,b) => a.day.CompareTo(b.day));
            if (S.personaDays.Count > PersonaArchiveDayLimit) S.personaDays.RemoveRange(0, S.personaDays.Count - PersonaArchiveDayLimit);
            if (S.personaReasons.Count > PersonaArchiveReasonLimit) S.personaReasons.RemoveRange(0, S.personaReasons.Count - PersonaArchiveReasonLimit);
        }
        public IReadOnlyList<XgPersonaDay> PersonaArchiveDays { get { TrimPersonaArchive(); return S.personaDays; } }
        public IReadOnlyList<XgPersonaReason> PersonaArchiveReasons { get { TrimPersonaArchive(); return S.personaReasons; } }
        // Called only around a real tone-board training operation; opening the archive never invents history.
        void RecordPersonaChange(double[] before, string element, bool positive, string source)
        {
            var after = PersonaAxes(); bool changed = false;
            for (int axis = 0; axis < 3; axis++) if (before[axis] != after[axis]) changed = true;
            if (!changed) return;
            TrimPersonaArchive();
            int day = TodayKnown && PersonaDate(Today, out _) ? Today : 0;
            S.personaReasons.Add(new XgPersonaReason { day = day, source = source, element = element, positive = positive, before = before, after = after });
            if (day != 0)
            {
                var row = S.personaDays.Find(x => x.day == day);
                if (row == null) { row = new XgPersonaDay { day = day }; S.personaDays.Add(row); }
                row.warmth = after[0]; row.play = after[1]; row.opinion = after[2];
            }
            S.personaArchiveRevision++;
            TrimPersonaArchive();
        }
        public string PersonaReasonText(XgPersonaReason reason)
        {
            if (reason == null) return "";
            string source = reason.source == "like" ? T("赞了回复", "Liked a reply") : reason.source == "dislike" ? T("踩了回复", "Disliked a reply")
                : reason.source == "tag" ? T("给回复贴标签", "Tagged a reply") : reason.source == "life" ? T("生活事件选择", "Life-event choice")
                : reason.source == "setup" ? T("开局性格播种", "Opening personality seed") : T("语气训练", "Tone training");
            var changes = new List<string>();
            for (int axis = 0; axis < 3; axis++)
                if (reason.before != null && reason.after != null && reason.before.Length > axis && reason.after.Length > axis && reason.before[axis] != reason.after[axis])
                    changes.Add(T(AxisNames[axis], AxisNamesEn[axis]) + " " + reason.before[axis].ToString("0", CultureInfo.InvariantCulture) + " → " + reason.after[axis].ToString("0", CultureInfo.InvariantCulture));
            return source + " · " + string.Join(" / ", changes);
        }
    }
}
