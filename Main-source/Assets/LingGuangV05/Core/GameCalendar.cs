using System;
using System.Collections.Generic;

namespace LingGuangV05.Core
{
    /// <summary>The day the calendar reached at a moment of play (GameState.eraMarks).</summary>
    [Serializable]
    public sealed class EraMark
    {
        public double at;
        public int day;
    }

    /// <summary>
    /// The game is set in 2016 (design v1.1 §11.7). The prologue is 24 May 2016; each lab stage is a month
    /// (stage 5 is October and November), and the ending is New Year's Eve. The date moves with progress, not
    /// with real time: inside a stage it runs linearly from the first to the last day of the stage's month as the
    /// lab progresses. The hours and minutes on the desktop clock still run at normal speed from 01:47.
    /// Dates only move forward; each change is recorded with the play time it happened at, so old chat lines
    /// keep the date they were written on. The economy's 120-second "day" is a billing period, not a calendar day.
    /// </summary>
    public static class GameCalendar
    {
        public static readonly DateTime Start = new DateTime(2016, 5, 24, 1, 47, 0);
        public static readonly DateTime Ending = new DateTime(2016, 12, 31);
        public const int LastStage = 6;

        static readonly DateTime[] First =
        {
            new DateTime(2016, 5, 24), new DateTime(2016, 6, 1), new DateTime(2016, 7, 1), new DateTime(2016, 8, 1),
            new DateTime(2016, 9, 1), new DateTime(2016, 10, 1), new DateTime(2016, 12, 1),
        };
        static readonly DateTime[] Last =
        {
            new DateTime(2016, 5, 31), new DateTime(2016, 6, 30), new DateTime(2016, 7, 31), new DateTime(2016, 8, 31),
            new DateTime(2016, 9, 30), new DateTime(2016, 11, 30), new DateTime(2016, 12, 30),
        };

        static int Clamp(int stage) => Math.Max(0, Math.Min(LastStage, stage));

        /// <summary>First day of a stage's month (stage 0 = the prologue).</summary>
        public static DateTime FirstDay(int stage) => First[Clamp(stage)];
        public static DateTime LastDay(int stage) => Last[Clamp(stage)];

        /// <summary>Days since the prologue (24 May = 0).</summary>
        public static int DayIndex(DateTime date) => (int)(date.Date - Start.Date).TotalDays;
        public static DateTime DateOf(int day) => Start.Date.AddDays(Math.Max(0, day));

        /// <summary>The day a stage has reached at this progress (0–1), linear over its month.</summary>
        public static int DayFor(int stage, double progress)
        {
            if (double.IsNaN(progress)) progress = 0;
            progress = Math.Max(0, Math.Min(1, progress));
            int first = DayIndex(FirstDay(stage)), last = DayIndex(LastDay(stage));
            return first + (int)Math.Floor(progress * (last - first) + 1e-9);
        }

        public static DateTime DateFor(int stage, double progress) => DateOf(DayFor(stage, progress));

        public static int Yyyymmdd(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        /// <summary>The calendar day of this save now (0 before any progress).</summary>
        public static int CurrentDay(GameState s)
        {
            if (s == null || s.eraMarks == null || s.eraMarks.Count == 0) return 0;
            return s.eraMarks[s.eraMarks.Count - 1].day;
        }

        /// <summary>The day the calendar showed at a past moment of play.</summary>
        public static int DayAt(GameState s, double gameSeconds)
        {
            if (s == null || s.eraMarks == null) return 0;
            int day = 0;
            foreach (var mark in s.eraMarks) { if (mark.at > gameSeconds + 1e-6) break; day = mark.day; }
            return day;
        }

        /// <summary>Moves the calendar to <paramref name="day"/> if that is later than today. Returns true if it moved.</summary>
        public static bool Advance(GameState s, int day)
        {
            if (s == null) return false;
            if (s.eraMarks == null) s.eraMarks = new List<EraMark>();
            day = Math.Min(day, DayIndex(Ending));
            if (day <= CurrentDay(s)) return false;
            double at = Math.Max(s.gameSeconds, s.eraMarks.Count > 0 ? s.eraMarks[s.eraMarks.Count - 1].at : 0);
            s.eraMarks.Add(new EraMark { at = at, day = day });
            return true;
        }

        /// <summary>Desktop clock now: today's date and the real-time hours and minutes.</summary>
        public static DateTime Now(GameState s) => ClockFor(s, s == null ? 0 : s.gameSeconds);

        public static readonly DateTime NewYearsDay = new DateTime(2017, 1, 1);

        /// <summary>The ending (design v1.1 §8): the clock jumps to 2017-01-01 00:00 and runs on from there. Once.</summary>
        public static bool NewYear(GameState s)
        {
            if (s == null || s.newYearAt > 0) return false;
            s.newYearAt = Math.Max(1e-3, s.gameSeconds);
            return true;
        }

        /// <summary>Desktop clock at a moment of play: the date the calendar showed then, the time of day from play time.</summary>
        public static DateTime ClockFor(GameState s, double gameSeconds)
        {
            if (double.IsNaN(gameSeconds) || gameSeconds < 0) gameSeconds = 0;
            if (s != null && s.newYearAt > 0 && gameSeconds >= s.newYearAt) return NewYearsDay.AddSeconds(Math.Min(gameSeconds - s.newYearAt, 3.0e8));
            double seconds = (Start.TimeOfDay.TotalSeconds + Math.Min(gameSeconds, 3.0e9)) % 86400;
            return DateOf(DayAt(s, gameSeconds)).AddSeconds(seconds);
        }

        /// <summary>Clock for a moment of play without a save (the prologue day).</summary>
        public static DateTime ClockFor(double gameSeconds) => ClockFor(null, gameSeconds);
    }
}
