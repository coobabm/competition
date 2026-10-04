using System;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Drives the desktop clock (taskbar, lock screen): the save's 2016 calendar day (GameCalendar), and 01:47 plus seconds played for the time of day. Turns off DreamOS time saving on this scene's manager only, so the shared
    /// DreamOS user data never stores a 2016 date. Respects the native 12/24-hour setting.
    /// </summary>
    public sealed class GameEraClock : MonoBehaviour
    {
        public ChapterOneRuntime runtime;
        private DateAndTimeManager time;

        private void LateUpdate()
        {
            // The static instance is lost on a script hot reload; fall back to the scene object.
            if (time == null) time = DateAndTimeManager.instance != null ? DateAndTimeManager.instance : FindAnyObjectByType<DateAndTimeManager>(FindObjectsInactive.Include);
            if (time != null && DateAndTimeManager.instance == null) DateAndTimeManager.instance = time; // taskbar labels read the static
            if (time == null || runtime == null || runtime.Sim == null) return;
            var sim = runtime.Sim;
            DateTime now = GameCalendar.Now(sim.S);
            time.saveTimeData = false;
            time.useSystemTime = false;
            time.currentYear = now.Year; time.currentMonth = now.Month; time.currentDay = now.Day;
            time.currentMinute = now.Minute; time.currentSecond = now.Second;
            time.isAm = now.Hour < 12;
            time.currentHour = time.useShortTimeFormat ? (now.Hour % 12 == 0 ? 12 : now.Hour % 12) : now.Hour;
        }
    }
}
