using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core.Story;

namespace LingGuangV05.Core.Era
{
    /// <summary>One dated 2016 event (design v1.1 §14.1): a news item, a forum thread, a group chat line, a shop listing or a tray bubble.</summary>
    public sealed class EraEvent
    {
        public string id = "", channel = "";
        public DateTime date;
        /// <summary>Tray bubbles that come back (the Windows 10 offer) until this date.</summary>
        public DateTime until;
        public double repeatMinutes;
        public string who = "", whoEn = "", title = "", titleEn = "", text = "", textEn = "";
        /// <summary>One-line headline only (politics, deaths): no body, no comment.</summary>
        public bool brief;
        public double price;
        public int Yyyymmdd => GameCalendar.Yyyymmdd(date);
        public bool Repeats => repeatMinutes > 0 && until > date;
    }

    /// <summary>
    /// era_events.json. Push channels (<see cref="YY"/>, <see cref="Tray"/>) are delivered when the calendar reaches
    /// their date, and only within the current month: a save that jumps months does not get a flood of old chat.
    /// Pull channels (news, forum, shop) are lists the apps read; nothing dated after today is ever shown.
    /// </summary>
    public sealed class EraEvents
    {
        public const string News = "news", Tieba = "tieba", YY = "yy", Taobao = "taobao", Tray = "tray";
        public static readonly string[] Channels = { News, Tieba, YY, Taobao, Tray };
        public static bool IsPush(string channel) => channel == YY || channel == Tray;

        readonly List<EraEvent> all = new List<EraEvent>();
        public IReadOnlyList<EraEvent> All => all;

        public static EraEvents Parse(string json)
        {
            var result = new EraEvents();
            var root = StoryJson.Parse(json) as Dictionary<string, object>;
            if (root == null || !(root.TryGetValue("events", out var list) && list is List<object> items)) throw new FormatException("era_events: no events list");
            var ids = new HashSet<string>();
            foreach (var item in items)
            {
                var o = item as Dictionary<string, object>;
                if (o == null) throw new FormatException("era_events: an event is not an object");
                var e = new EraEvent
                {
                    id = Str(o, "id"), channel = Str(o, "channel"), date = Date(o, "date"),
                    who = Str(o, "who"), whoEn = Str(o, "whoEn"), title = Str(o, "title"), titleEn = Str(o, "titleEn"),
                    text = Str(o, "text"), textEn = Str(o, "textEn"), brief = o.TryGetValue("brief", out var b) && b is bool flag && flag,
                    repeatMinutes = Num(o, "repeatMinutes"), price = Num(o, "price"),
                };
                e.until = o.ContainsKey("until") ? Date(o, "until") : e.date;
                if (e.id.Length == 0 || !ids.Add(e.id)) throw new FormatException("era_events: missing or repeated id " + e.id);
                if (Array.IndexOf(Channels, e.channel) < 0) throw new FormatException("era_events: unknown channel " + e.channel + " in " + e.id);
                if (e.date.Year != 2015 && e.date.Year != 2016 || e.date > GameCalendar.Ending) throw new FormatException("era_events: " + e.id + " is not dated in 2015–2016");
                if (e.title.Length == 0 && e.text.Length == 0) throw new FormatException("era_events: " + e.id + " has no text");
                if (e.title.Length > 0 != e.titleEn.Length > 0 || e.text.Length > 0 != e.textEn.Length > 0 || e.who.Length > 0 != e.whoEn.Length > 0)
                    throw new FormatException("era_events: " + e.id + " needs an English line for every Chinese line");
                if (e.brief && e.text.Length > 0) throw new FormatException("era_events: " + e.id + " is a one-line headline");
                if (IsPush(e.channel) && e.text.Length == 0) throw new FormatException("era_events: " + e.id + " needs a message");
                result.all.Add(e);
            }
            // Stable: events of the same day keep their file order (a chat reply follows its message).
            var sorted = new List<EraEvent>(System.Linq.Enumerable.OrderBy(result.all, x => x.date));
            result.all.Clear(); result.all.AddRange(sorted);
            return result;
        }

        static string Str(Dictionary<string, object> o, string key) => o.TryGetValue(key, out var v) && v is string s ? s : "";
        static double Num(Dictionary<string, object> o, string key) => o.TryGetValue(key, out var v) && v is double d ? d : 0;
        static DateTime Date(Dictionary<string, object> o, string key)
        {
            if (DateTime.TryParseExact(Str(o, key), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
            throw new FormatException("era_events: bad " + key + " in " + Str(o, "id"));
        }

        public EraEvent Get(string id) { foreach (var e in all) if (e.id == id) return e; return null; }

        /// <summary>What a pull channel shows today: everything dated on or before today, newest first.</summary>
        public List<EraEvent> Visible(string channel, DateTime today)
        {
            var list = new List<EraEvent>();
            foreach (var e in all) if (e.channel == channel && e.date <= today.Date) list.Add(e);
            list.Reverse();
            return list;
        }

        /// <summary>Events of a channel in one calendar month (the 摆渡 front page, the month card).</summary>
        public List<EraEvent> InMonth(string channel, int year, int month)
        {
            var list = new List<EraEvent>();
            foreach (var e in all) if (e.channel == channel && e.date.Year == year && e.date.Month == month) list.Add(e);
            return list;
        }

        /// <summary>
        /// Push events to deliver now: dated from the first of today's month to today, not delivered yet, oldest first.
        /// Repeating bubbles are due while today is inside their window; their repeats are the caller's timing.
        /// </summary>
        public List<EraEvent> Due(string channel, DateTime today, ICollection<string> delivered)
        {
            var list = new List<EraEvent>();
            var monthStart = new DateTime(today.Year, today.Month, 1);
            foreach (var e in all)
            {
                if (e.channel != channel || delivered != null && delivered.Contains(e.id)) continue;
                if (e.date > today.Date) continue;
                if (e.Repeats ? today.Date <= e.until : e.date >= monthStart) list.Add(e);
            }
            return list;
        }

        /// <summary>A repeating bubble still inside its window today.</summary>
        public List<EraEvent> Repeating(string channel, DateTime today)
        {
            var list = new List<EraEvent>();
            foreach (var e in all) if (e.channel == channel && e.Repeats && e.date <= today.Date && today.Date <= e.until) list.Add(e);
            return list;
        }
    }
}
