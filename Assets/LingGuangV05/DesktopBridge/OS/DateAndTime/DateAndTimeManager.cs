using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Apps;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The desktop's clock and calendar (2026-10-19). Replaces DreamOS's DateAndTimeManager: same GUID, serialized
    /// fields and public API, so the taskbar clock, the settings/commander/reminder apps and the story's 2016 era clock
    /// (which drives the public time fields on top of it) keep working unchanged.
    ///
    /// Time either follows the PC clock (<see cref="useSystemTime"/>) or is simulated: seconds advance by
    /// <c>deltaTime * timeMultiplier</c> and roll over into minutes, hours, days and months from
    /// <see cref="monthData"/>, in 12-hour (AM/PM) or 24-hour form, saving each unit to the "DateAndTime" data when
    /// <see cref="saveTimeData"/> is on. Once a minute it checks <see cref="timedEvents"/> (one-off and daily alarms,
    /// including the reminders the Reminder app registers) and raises a notification for each that fires.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("LingGuang/OS/Date & Time/Date & Time Manager")]
    public class DateAndTimeManager : MonoBehaviour
    {
        public static DateAndTimeManager instance;

        // Time variables
        [Range(0.1f, 10000)] public float timeMultiplier = 1;
        [Range(1, 31)] public int currentDay = 1;
        [Range(1, 12)] public int currentMonth = 1;
        [Range(0, 24)] public int currentHour;
        [Range(0, 60)] public int currentMinute;
        [Range(0, 60)] public float currentSecond;
        public int currentYear = 2019;

        // Resources
        [SerializeField] private Sprite notificationIcon;

        // Settings
        public bool useSystemTime = false;
        public bool saveTimeData = true;
        public bool useShortTimeFormat = true;
        [SerializeField] private bool enableTimedEvents = true;
        [SerializeField] private DefaultShortTime defaultTimeFormat;

        public MonthData[] monthData = new MonthData[12];

        public List<TimedEvent> timedEvents = new List<TimedEvent>();

        // Clock constants (kept public for the analog clock and the settings app)
        public float rotationDegreesPerDay = 360;
        public float hoursPerDay = 24;
        public float minutesPerHour = 60;
        public float secondsPerMinute = 60;
        public bool isAm;
        [HideInInspector] public ReminderApp reminderManager;

        int currentMonthDayCount = 30;
        const DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.DateAndTime;

        [System.Serializable]
        public class TimedEvent
        {
            public string eventTitle;
            public TimedEventType eventType;
            [Range(0, 12)] public int eventHour;
            [Range(0, 59)] public int eventMinute;
            public DefaultShortTime meridiemFormat;
            public UnityEvent onEventTime = new UnityEvent();
            [HideInInspector] public string eventID;
            [HideInInspector] public int latestDay = -1;
            [HideInInspector] public bool isEnabled = true;
            [HideInInspector] public bool isReminderItem = false;
        }

        [System.Serializable]
        public class MonthData
        {
            public string monthName = "Name";
            [Range(1, 31)] public int dayCount = 30;
        }

        public enum DefaultShortTime { AM, PM }
        public enum TimedEventType { Once, Daily }

        void Awake()
        {
            instance = this;
        }

        void OnEnable()
        {
            UpdateTimeData();
        }

        void Update()
        {
            UpdateValues();
        }

        void UpdateValues()
        {
            if (useSystemTime)
            {
                currentSecond = GetCurrentSystemSecond();
                currentMinute = GetCurrentSystemMinute();
                currentHour = useShortTimeFormat ? GetCurrentSystemHourShort() : GetCurrentSystemHour();
                currentDay = GetCurrentSystemDay();
                currentMonth = GetCurrentSystemMonth();
                currentYear = GetCurrentSystemYear();
                return;
            }

            currentSecond += Time.deltaTime * timeMultiplier;
            if (currentSecond < 59) { return; }

            currentSecond = 0;
            currentMinute += 1;

            if (currentMinute >= 60)
            {
                currentHour += 1;
                currentMinute = 0;

                CheckForClockFormat();

                if (!useShortTimeFormat && currentHour >= 24)
                {
                    currentDay += 1;
                    currentHour = 0;
                    if (currentDay == 0) { currentDay = 1; }
                }
                else if (useShortTimeFormat && currentHour >= 13) { currentHour = 1; }
                else if (useShortTimeFormat && isAm && currentHour >= 12)
                {
                    currentDay += 1;
                    SaveInt("CurrentDay", currentDay);
                }

                if (currentDay > currentMonthDayCount)
                {
                    currentDay = 1;
                    currentMonth += 1;

                    if (currentMonth == 13)
                    {
                        currentMonth = 1;
                        currentYear += 1;
                        SaveInt("CurrentYear", currentYear);
                    }

                    currentMonthDayCount = monthData[currentMonth - 1].dayCount;

                    SaveInt("CurrentDay", currentDay);
                    SaveInt("CurrentMonth", currentMonth);
                }

                SaveInt("CurrentHour", currentHour);
            }

            SaveInt("CurrentMinute", currentMinute);
            if (enableTimedEvents) { CheckForTimedEvents(); }
        }

        void SaveInt(string key, int value)
        {
            if (saveTimeData) { DreamOSDataManager.WriteIntData(dataCat, key, value); }
        }

        public void UpdateTimeData(bool readAfter = true)
        {
            if (!saveTimeData || useSystemTime) { return; }

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "ShortTimeFormat")) { DreamOSDataManager.WriteBooleanData(dataCat, "ShortTimeFormat", useShortTimeFormat); }
            else { useShortTimeFormat = DreamOSDataManager.ReadBooleanData(dataCat, "ShortTimeFormat"); }

            // First run: seed the saved clock from the inspector values.
            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentMinute"))
            {
                isAm = defaultTimeFormat == DefaultShortTime.AM;
                DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", isAm);
                DreamOSDataManager.WriteIntData(dataCat, "CurrentMinute", currentMinute);
                DreamOSDataManager.WriteIntData(dataCat, "CurrentHour", currentHour);
                DreamOSDataManager.WriteIntData(dataCat, "CurrentDay", currentDay);
                DreamOSDataManager.WriteIntData(dataCat, "CurrentMonth", currentMonth);
                DreamOSDataManager.WriteIntData(dataCat, "CurrentYear", currentYear);
            }

            if (readAfter)
            {
                isAm = DreamOSDataManager.ReadBooleanData(dataCat, "IsAM");
                currentMinute = DreamOSDataManager.ReadIntData(dataCat, "CurrentMinute");
                currentHour = DreamOSDataManager.ReadIntData(dataCat, "CurrentHour");
                currentDay = DreamOSDataManager.ReadIntData(dataCat, "CurrentDay");
                currentMonth = DreamOSDataManager.ReadIntData(dataCat, "CurrentMonth");
                currentYear = DreamOSDataManager.ReadIntData(dataCat, "CurrentYear");
            }

            currentMonthDayCount = monthData[currentMonth - 1].dayCount;
            if (currentDay == 0) { currentDay = 1; }
        }

        public void CheckForTimedEvents()
        {
            for (int i = 0; i < timedEvents.Count; i++)
            {
                TimedEvent timedEvent = timedEvents[i];
                if (!timedEvent.isEnabled) { continue; }

                bool isMeridiemCorrect = (timedEvent.meridiemFormat == DefaultShortTime.AM && isAm) || (timedEvent.meridiemFormat == DefaultShortTime.PM && !isAm);
                bool isEventTime = timedEvent.eventHour == currentHour && timedEvent.eventMinute == currentMinute && isMeridiemCorrect;

                if (timedEvent.eventType == TimedEventType.Once && isEventTime)
                {
                    timedEvent.isEnabled = false;
                    timedEvent.onEventTime.Invoke();

                    if (timedEvent.isReminderItem) { reminderManager.EnableReminder(timedEvent.eventID, false); }
                    NotifyTimedEvent(timedEvent);
                }
                else if (timedEvent.eventType == TimedEventType.Daily && timedEvent.latestDay != currentDay && isEventTime)
                {
                    timedEvent.latestDay = currentDay;
                    timedEvent.onEventTime.Invoke();

                    NotifyTimedEvent(timedEvent);
                }
            }
        }

        void NotifyTimedEvent(TimedEvent timedEvent)
        {
            if (NotificationManager.instance != null) { NotificationManager.instance.CreateNotification(notificationIcon, "Timed Event", timedEvent.eventTitle); }
        }

        public void CreateTimedEvent(string title, int hour, int minute, DefaultShortTime meridiem, TimedEventType type = TimedEventType.Once, UnityEvent events = null)
        {
            TimedEvent timedEvent = new TimedEvent();
            timedEvent.eventTitle = title;
            timedEvent.eventMinute = minute;
            timedEvent.eventHour = hour;
            timedEvent.meridiemFormat = meridiem;
            timedEvent.eventType = type;
            if (events != null) { timedEvent.onEventTime.AddListener(events.Invoke); } // DreamOS threw here for the default null
            timedEvent.isReminderItem = false;
            timedEvent.isEnabled = true;
            timedEvents.Add(timedEvent);
        }

        /// <summary>Flips AM/PM when the hour passes 12. (The 24-hour branch writes inverted values to the save; kept as DreamOS had it.)</summary>
        void CheckForClockFormat()
        {
            if (useShortTimeFormat)
            {
                if (currentHour == 12 && !isAm) { SetMeridiem(true, true); }
                else if (currentHour == 12 && isAm) { SetMeridiem(false, false); }
            }
            else
            {
                if (currentHour >= 24 && !isAm) { SetMeridiem(true, false); }
                else if (currentHour >= 12 && isAm) { SetMeridiem(false, true); }
            }
        }

        void SetMeridiem(bool am, bool savedValue)
        {
            isAm = am;
            DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", savedValue);
        }

        public void ShortTimeFormat(bool value)
        {
            useShortTimeFormat = value;

            DreamOSDataManager.WriteBooleanData(dataCat, "ShortTimeFormat", useShortTimeFormat);
            UpdateTimeFormat();
        }

        /// <summary>Converts the current hour between 24-hour and 12-hour form after the format was switched.</summary>
        void UpdateTimeFormat()
        {
            if (useShortTimeFormat && currentHour >= 12)
            {
                isAm = false;
                DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", false);

                if (currentHour >= 13 && currentHour <= 23) { currentHour -= 12; }
            }
            else if (!useShortTimeFormat && !isAm)
            {
                if (currentHour >= 1 && currentHour <= 11) { currentHour += 12; }
                else if (currentHour == 12) { currentHour = 0; }
            }
        }

        public void DeleteSavedData()
        {
            DreamOSDataManager.DeleteDataCategory(dataCat);
        }

        public static int GetCurrentSystemSecond() { return int.Parse(DateTime.Now.ToString("ss")); }
        public static int GetCurrentSystemMinute() { return int.Parse(DateTime.Now.ToString("mm")); }
        public static int GetCurrentSystemHour() { return int.Parse(DateTime.Now.ToString("HH")); }
        public static int GetCurrentSystemHourShort() { return int.Parse(DateTime.Now.ToString("hh")); }
        public static int GetCurrentSystemDay() { return int.Parse(DateTime.Now.ToString("dd")); }
        public static int GetCurrentSystemMonth() { return int.Parse(DateTime.Now.ToString("MM")); }
        public static int GetCurrentSystemYear() { return int.Parse(DateTime.Now.ToString("yyyy")); }
    }
}
