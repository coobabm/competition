using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One reminder row (replaces DreamOS's ReminderItem; same GUID and fields). Clicking it opens the edit dialog,
    /// whose OK writes the reminder back to the saved data, the clock's timed event and this row.
    /// </summary>
    public class ReminderRow : MonoBehaviour
    {
        public ButtonManager mainButton;
        public SwitchManager switchManager;
        [SerializeField] TextMeshProUGUI titleObject;
        [SerializeField] TextMeshProUGUI timeObject;
        [SerializeField] GameObject onceObject;
        [SerializeField] GameObject dailyObject;

        [HideInInspector] public string reminderID;
        [HideInInspector] public bool isAM;
        [HideInInspector] public ReminderApp manager;

        public void SetTitle(string text) => titleObject.text = text;

        public void SetTime(string text) => timeObject.text = text;

        public void SetOnce() => SetDailyState(false);

        public void SetDaily() => SetDailyState(true);

        void SetDailyState(bool daily)
        {
            onceObject.SetActive(!daily);
            dailyObject.SetActive(daily);
        }

        public void DeleteReminder()
        {
            manager.DeleteReminder(reminderID, true);
            Destroy(gameObject);
        }

        /// <summary>Fills the shared edit dialog with this reminder and opens it (hours 1–12, minutes 0–58).</summary>
        public void InitializeWindow(ModalWindowManager modal, TMP_InputField title, HorizontalSelector type, HorizontalSelector minute, HorizontalSelector hour, HorizontalSelector meridiem)
        {
            modal.onConfirm.RemoveAllListeners();
            modal.onConfirm.AddListener(() => Apply(modal, title, type, minute, hour, meridiem));

            hour.items.Clear();
            minute.items.Clear();
            for (int i = 0; i < 12; i++) hour.CreateNewItem((i + 1).ToString());
            for (int i = 0; i < 59; i++) minute.CreateNewItem(i.ToString());
            // The dialog always opens at 8:00, whatever the reminder's time.
            hour.index = 7;
            minute.index = 0;

            title.text = titleObject.text;
            hour.UpdateUI();
            minute.UpdateUI();

            if (onceObject.activeInHierarchy) { type.index = 0; type.UpdateUI(); }
            else if (dailyObject.activeInHierarchy) { type.index = 1; type.UpdateUI(); }

            meridiem.index = isAM ? 0 : 1;
            meridiem.UpdateUI();
            modal.OpenWindow();
        }

        void Apply(ModalWindowManager modal, TMP_InputField title, HorizontalSelector type, HorizontalSelector minute, HorizontalSelector hour, HorizontalSelector meridiem)
        {
            modal.CloseWindow();
            manager.UpdateReminderData(reminderID, title.text, type.index, minute.index, hour.index + 1, meridiem.index);

            var timed = DateAndTimeManager.instance.timedEvents.Find(e => e.eventID == reminderID);
            if (timed != null)
            {
                timed.meridiemFormat = meridiem.index == 0 ? DateAndTimeManager.DefaultShortTime.AM : DateAndTimeManager.DefaultShortTime.PM;
                if (type.index == 0) timed.eventType = DateAndTimeManager.TimedEventType.Once;
                else if (type.index == 1) timed.eventType = DateAndTimeManager.TimedEventType.Daily;
                timed.eventTitle = title.text;
                timed.eventHour = hour.index + 1;
                timed.eventMinute = minute.index;
            }

            SetTitle(title.text);
            SetTime(string.Format(minute.index < 10 ? "{0}:0{1} {2}" : "{0}:{1} {2}",
                hour.items[hour.index].itemTitle, minute.items[minute.index].itemTitle, meridiem.items[meridiem.index].itemTitle));
            if (type.index == 0) SetOnce();
            else if (type.index == 1) SetDaily();
            isAM = meridiem.index == 0;
        }
    }
}
