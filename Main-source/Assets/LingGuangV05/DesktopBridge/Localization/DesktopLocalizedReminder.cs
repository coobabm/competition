using System.Globalization;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Formats the time of an existing reminder; never changes its title, schedule or saved data.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReminderItem))]
    public sealed class DesktopLocalizedReminder : MonoBehaviour
    {
        public ReminderItem item;

        private void Awake()
        {
            if (item == null) item = GetComponent<ReminderItem>();
        }

        private void OnEnable()
        {
            GameText.Changed += Refresh;
            Refresh();
        }

        // Native CreateReminder assigns reminderID and appends its event after Instantiate/OnEnable.
        private void Start() { Refresh(); }
        private void OnDisable() { GameText.Changed -= Refresh; }

        public void Refresh()
        {
            if (item == null) item = GetComponent<ReminderItem>();
            var time = DateAndTimeManager.instance;
            if (item == null || string.IsNullOrEmpty(item.reminderID) || time == null || time.timedEvents == null) return;
            foreach (var scheduled in time.timedEvents)
            {
                if (scheduled == null || scheduled.eventID != item.reminderID) continue;
                bool morning = scheduled.meridiemFormat == DateAndTimeManager.DefaultShortTime.AM;
                string suffix = morning ? GameText.T("上午", "AM") : GameText.T("下午", "PM");
                item.SetTime(scheduled.eventHour.ToString(CultureInfo.InvariantCulture) + ":" +
                    scheduled.eventMinute.ToString("00", CultureInfo.InvariantCulture) + " " + suffix);
                return;
            }
        }
    }
}
