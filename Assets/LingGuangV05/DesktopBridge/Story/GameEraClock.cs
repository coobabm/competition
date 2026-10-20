using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.OS;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Drives taskbar and lock-screen labels from the save calendar, or the current epilogue presentation.
    /// The normal clock keeps the real hours and minutes. Neither display mode writes DreamOS time data
    /// or changes simulation time. Respects the native 12/24-hour setting.
    /// </summary>
    public sealed class GameEraClock : MonoBehaviour
    {
        /// <summary>The desktop shows the real time of day; only the date is the story's 2016.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void UseRealTimeOfDay() { GameCalendar.WallClock = () => DateTime.Now; }

        public ChapterOneRuntime runtime;
        private DateAndTimeManager time;

        /// <summary>Shared label clock. Historical records and gameplay must continue to use GameCalendar.</summary>
        public static DateTime DisplayNow(ChapterOneRuntime runtime)
        {
            var sim = runtime == null ? null : runtime.Sim;
            DateTime now = GameCalendar.Now(sim == null ? null : sim.S);
            if (runtime == null || sim == null) return now;
            var epilogue = runtime.GetComponent<XgEpilogueDirector>();
            return epilogue == null ? now : epilogue.DesktopDisplayTime(sim, now);
        }

        private void LateUpdate()
        {
            // The static instance is lost on a script hot reload; fall back to the scene object.
            if (time == null) time = DateAndTimeManager.instance != null ? DateAndTimeManager.instance : FindAnyObjectByType<DateAndTimeManager>(FindObjectsInactive.Include);
            if (time != null && DateAndTimeManager.instance == null) DateAndTimeManager.instance = time; // taskbar labels read the static
            if (time == null || runtime == null || runtime.Sim == null) return;
            if (GameCalendar.WallClock == null) UseRealTimeOfDay(); // a script reload during Play clears statics
            DateTime now = DisplayNow(runtime);
            time.saveTimeData = false;
            time.useSystemTime = false;
            time.currentYear = now.Year; time.currentMonth = now.Month; time.currentDay = now.Day;
            time.currentMinute = now.Minute; time.currentSecond = now.Second;
            time.isAm = now.Hour < 12;
            time.currentHour = time.useShortTimeFormat ? (now.Hour % 12 == 0 ? 12 : now.Hour % 12) : now.Hour;
        }
    }
}
