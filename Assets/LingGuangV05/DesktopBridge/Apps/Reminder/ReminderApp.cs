using System.Collections.Generic;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop reminders (2026-10-19). Replaces DreamOS's ReminderManager: same GUID, serialized fields, public
    /// methods and saved keys ("ReminderItem#n_Title" … in the DateAndTime data), so existing desktop profiles load.
    ///
    /// Up to ten reminders; each is a row with an on/off switch and a timed event on the desktop clock
    /// (DateAndTimeManager), which fires the reminder and switches a one-off reminder off.
    /// </summary>
    public class ReminderApp : MonoBehaviour
    {
        const int ReminderLimit = 10;
        const string CountKey = "ReminderItemCount";
        const DreamOSDataManager.DataCategory Category = DreamOSDataManager.DataCategory.DateAndTime;

        [SerializeField] GameObject reminderPreset;
        [SerializeField] Transform reminderParent;
        [SerializeField] ModalWindowManager reminderModal;
        [SerializeField] TMP_InputField eventTitleObject;
        [SerializeField] HorizontalSelector typeSelector;
        [SerializeField] HorizontalSelector hourSelector;
        [SerializeField] HorizontalSelector minuteSelector;
        [SerializeField] HorizontalSelector meridiemSelector;

        readonly List<ReminderRow> activeItems = new List<ReminderRow>();

        void Awake() => Initialize();

        /// <summary>Rebuilds the rows from the saved reminders and registers their timed events.</summary>
        public void Initialize()
        {
            if (DateAndTimeManager.instance == null) return;
            foreach (Transform child in reminderParent) Destroy(child.gameObject);

            for (int i = 0; i < ReminderLimit; i++)
            {
                string id = "ReminderItem#" + i;
                if (!DreamOSDataManager.ContainsJsonKey(Category, id + "_IsEnabled")) continue;

                var row = AddRow(id);
                var timed = new DateAndTimeManager.TimedEvent
                {
                    eventID = id,
                    eventMinute = DreamOSDataManager.ReadIntData(Category, id + "_Minute"),
                    eventHour = DreamOSDataManager.ReadIntData(Category, id + "_Hour"),
                    eventTitle = DreamOSDataManager.ReadStringData(Category, id + "_Title"),
                    isReminderItem = true,
                    isEnabled = DreamOSDataManager.ReadBooleanData(Category, id + "_IsEnabled")
                };
                row.isAM = DreamOSDataManager.ReadIntData(Category, id + "_IsPM") == 0;
                timed.meridiemFormat = row.isAM ? DateAndTimeManager.DefaultShortTime.AM : DateAndTimeManager.DefaultShortTime.PM;

                int type = DreamOSDataManager.ReadIntData(Category, id + "_Type");
                if (type == 0) { row.SetOnce(); timed.eventType = DateAndTimeManager.TimedEventType.Once; }
                else if (type == 1) { row.SetDaily(); timed.eventType = DateAndTimeManager.TimedEventType.Daily; }

                row.switchManager.isOn = timed.isEnabled;
                row.switchManager.UpdateUI();
                row.SetTitle(timed.eventTitle);
                row.SetTime(TimeLabel(timed));
                DateAndTimeManager.instance.timedEvents.Add(timed);
            }
        }

        ReminderRow AddRow(string id)
        {
            var row = AppSpawn.Row(reminderPreset, reminderParent, id).GetComponent<ReminderRow>();
            row.manager = this;
            row.reminderID = id;
            row.mainButton.onClick.AddListener(() =>
                row.InitializeWindow(reminderModal, eventTitleObject, typeSelector, minuteSelector, hourSelector, meridiemSelector));
            row.switchManager.onEvents.AddListener(() => EnableReminder(row.reminderID, true, false));
            row.switchManager.offEvents.AddListener(() => EnableReminder(row.reminderID, false, false));
            activeItems.Add(row);
            return row;
        }

        static string TimeLabel(DateAndTimeManager.TimedEvent e) =>
            string.Format(e.eventMinute < 10 ? "{0}:0{1} {2}" : "{0}:{1} {2}", e.eventHour, e.eventMinute, e.meridiemFormat);

        public void EnableReminder(string itemID, bool value, bool updateSwitch = true)
        {
            var row = activeItems.Find(r => r.reminderID == itemID);
            if (row == null) return;

            var timed = DateAndTimeManager.instance.timedEvents.Find(e => e.eventID == itemID);
            if (timed != null) timed.isEnabled = value;

            DreamOSDataManager.WriteBooleanData(Category, row.reminderID + "_IsEnabled", value);
            if (updateSwitch)
            {
                row.switchManager.isOn = value;
                row.switchManager.UpdateUI();
            }
        }

        public void DeleteReminder(string itemID, bool disableBefore = true)
        {
            if (disableBefore) EnableReminder(itemID, false, false);
            foreach (var field in new[] { "_IsEnabled", "_Title", "_Type", "_Minute", "_Hour", "_IsPM" })
                DreamOSDataManager.DeleteData(Category, itemID + field);
            DreamOSDataManager.WriteIntData(Category, CountKey, ItemCount() - 1);
        }

        /// <summary>The list's "+" button: a new one-off reminder at 8:00 AM, switched on.</summary>
        public void CreateReminder()
        {
            int count = ItemCount();
            if (count == ReminderLimit) return;
            DreamOSDataManager.WriteIntData(Category, CountKey, count + 1);

            string id = "ReminderItem#" + count;
            string title = "Reminder " + (count + 1);
            var row = AddRow(id);
            row.isAM = true;
            var timed = new DateAndTimeManager.TimedEvent
            {
                eventID = id,
                eventMinute = 0,
                eventHour = 8,
                meridiemFormat = DateAndTimeManager.DefaultShortTime.AM,
                eventTitle = title,
                isReminderItem = true,
                isEnabled = true
            };

            DreamOSDataManager.WriteBooleanData(Category, id + "_IsEnabled", true);
            DreamOSDataManager.WriteStringData(Category, id + "_Title", title);
            DreamOSDataManager.WriteIntData(Category, id + "_Type", 0);
            DreamOSDataManager.WriteIntData(Category, id + "_Minute", 0);
            DreamOSDataManager.WriteIntData(Category, id + "_Hour", 8);
            DreamOSDataManager.WriteIntData(Category, id + "_IsPM", 0);

            row.SetOnce();
            row.SetTitle(title);
            row.SetTime(TimeLabel(timed));
            row.switchManager.isOn = true;
            row.switchManager.UpdateUI();
            DateAndTimeManager.instance.timedEvents.Add(timed);
        }

        public void UpdateReminderData(string itemID, string itemTitle, int itemType, int itemMinute, int itemHour, int isPM)
        {
            DreamOSDataManager.WriteIntData(Category, itemID + "_Type", itemType);
            DreamOSDataManager.WriteStringData(Category, itemID + "_Title", itemTitle);
            DreamOSDataManager.WriteIntData(Category, itemID + "_Minute", itemMinute);
            DreamOSDataManager.WriteIntData(Category, itemID + "_Hour", itemHour);
            DreamOSDataManager.WriteIntData(Category, itemID + "_IsPM", isPM);
        }

        static int ItemCount() =>
            DreamOSDataManager.ContainsJsonKey(Category, CountKey) ? DreamOSDataManager.ReadIntData(Category, CountKey) : 0;
    }
}
