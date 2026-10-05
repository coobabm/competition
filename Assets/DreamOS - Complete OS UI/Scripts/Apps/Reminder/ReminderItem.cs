using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    public class ReminderItem : MonoBehaviour
    {
        [Header("Resources")]
        public ButtonManager mainButton;
        public SwitchManager switchManager;
        [SerializeField] private TextMeshProUGUI titleObject;
        [SerializeField] private TextMeshProUGUI timeObject;
        [SerializeField] private GameObject onceObject;
        [SerializeField] private GameObject dailyObject;

        // Helpers
        [HideInInspector] public string reminderID;
        [HideInInspector] public bool isAM;
        [HideInInspector] public ReminderManager manager;

        public void SetTitle(string text)
        {
            titleObject.text = text;
        }

        public void SetTime(string text)
        {
            timeObject.text = text;
        }

        public void SetOnce()
        {
            onceObject.SetActive(true);
            dailyObject.SetActive(false);
        }

        public void SetDaily()
        {
            onceObject.SetActive(false);
            dailyObject.SetActive(true);
        }

        public void DeleteReminder()
        {
            manager.DeleteReminder(reminderID, true);
            Destroy(gameObject);
        }

        public void InitializeWindow(ModalWindowManager modal, TMP_InputField title, HorizontalSelector type, HorizontalSelector minute, HorizontalSelector hour, HorizontalSelector meridiem)
        {
            int hourIndex = 7;
            int minuteIndex = 0;

            // Set events
            modal.onConfirm.RemoveAllListeners();
            modal.onConfirm.AddListener(delegate 
            {
                modal.CloseWindow();
                manager.UpdateReminderData(reminderID, title.text, type.index, minute.index, hour.index + 1, meridiem.index);

                for (int i = 0; i < DateAndTimeManager.instance.timedEvents.Count; i++)
                {
                    if (DateAndTimeManager.instance.timedEvents[i].eventID == reminderID)
                    {
                        if (meridiem.index == 0) { DateAndTimeManager.instance.timedEvents[i].meridiemFormat = DateAndTimeManager.DefaultShortTime.AM; }
                        else { DateAndTimeManager.instance.timedEvents[i].meridiemFormat = DateAndTimeManager.DefaultShortTime.PM; }

                        if (type.index == 0) { DateAndTimeManager.instance.timedEvents[i].eventType = DateAndTimeManager.TimedEventType.Once; }
                        else if (type.index == 1) { DateAndTimeManager.instance.timedEvents[i].eventType = DateAndTimeManager.TimedEventType.Daily; }

                        DateAndTimeManager.instance.timedEvents[i].eventTitle = title.text;
                        DateAndTimeManager.instance.timedEvents[i].eventHour = hour.index + 1;
                        DateAndTimeManager.instance.timedEvents[i].eventMinute = minute.index;

                        break;
                    }
                }

                SetTitle(title.text);
                if (minute.index < 10) { SetTime(string.Format("{0}:0{1} {2}", hour.items[hour.index].itemTitle, minute.items[minute.index].itemTitle, meridiem.items[meridiem.index].itemTitle)); }
                else { SetTime(string.Format("{0}:{1} {2}", hour.items[hour.index].itemTitle, minute.items[minute.index].itemTitle, meridiem.items[meridiem.index].itemTitle)); }

                if (type.index == 0) { SetOnce(); }
                else if (type.index == 1) { SetDaily(); }

                if (meridiem.index == 0) { isAM = true; }
                else { isAM = false; }
            });

            // Prepare selectors - we're adding +1 for the hour as there's no 0
            hour.items.Clear();
            minute.items.Clear();
            for (int i = 0; i < 12; i++) { hour.CreateNewItem((i + 1).ToString()); }
            for (int i = 0; i < 59; i++) { minute.CreateNewItem(i.ToString()); }
            hour.index = hourIndex;
            minute.index = minuteIndex;

            // Set title UI
            title.text = titleObject.text;

            // Update selector UI
            hour.UpdateUI();
            minute.UpdateUI();

            // Update type UI
            if (onceObject.activeInHierarchy) { type.index = 0; type.UpdateUI(); }
            else if (dailyObject.activeInHierarchy) { type.index = 1; type.UpdateUI(); }

            // Update meridiem UI
            if (isAM) { meridiem.index = 0; meridiem.UpdateUI(); }
            else { meridiem.index = 1; meridiem.UpdateUI(); }

            // Open the window
            modal.OpenWindow();
        }
    }
}