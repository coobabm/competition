using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    public sealed class DesktopLocalizedClock : MonoBehaviour
    {
        public TMP_Text target;
        public bool date, seconds, amPm;
        private string previous;
        private void OnEnable() { GameText.Changed += Render; Render(); }
        private void OnDisable() { GameText.Changed -= Render; }
        private void Update() { Render(); }
        private void Render()
        {
            var time = DateAndTimeManager.instance;
            if (time == null || target == null) return;
            string value = date ? GameText.F("{0}年{1}月{2}日", "{0:D4}-{1:D2}-{2:D2}", time.currentYear, time.currentMonth, time.currentDay)
                : time.currentHour.ToString("00") + ":" + time.currentMinute.ToString("00")
                + (seconds ? ":" + ((int)time.currentSecond).ToString("00") : "")
                + (amPm && time.useShortTimeFormat ? GameText.T(time.isAm ? " 上午" : " 下午", time.isAm ? " AM" : " PM") : "");
            if (value == previous) return;
            previous = value; target.text = value;
        }
    }
}
