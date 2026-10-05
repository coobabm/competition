using System;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace HongmengOS.Aero2010
{
    public sealed class AeroShellClock : MonoBehaviour
    {
        public TMP_Text timeLabel;
        public TMP_Text dateLabel;
        public DateAndTimeManager desktopClock;
        private float nextUpdate;
        private void OnEnable() { nextUpdate = 0; Refresh(); }
        private void Update() { if (Time.unscaledTime >= nextUpdate) Refresh(); }
        private void Refresh()
        {
            nextUpdate = Time.unscaledTime + 1;
            var clock = desktopClock;
            if (clock != null)
            {
                if (timeLabel != null) timeLabel.text = clock.currentHour.ToString("00") + ":" + clock.currentMinute.ToString("00") + (clock.useShortTimeFormat ? (clock.isAm ? " AM" : " PM") : string.Empty);
                if (dateLabel != null) dateLabel.text = clock.currentMonth + "/" + clock.currentDay + "/" + clock.currentYear;
            }
            else
            {
                DateTime now = DateTime.Now;
                if (timeLabel != null) timeLabel.text = now.ToString("HH:mm");
                if (dateLabel != null) dateLabel.text = now.ToString("M/d/yyyy");
            }
        }
    }
}
