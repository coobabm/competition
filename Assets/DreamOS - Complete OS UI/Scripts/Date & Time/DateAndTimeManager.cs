using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.DreamOS
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("DreamOS/Date & Time/Date & Time Manager")]
    public class DateAndTimeManager : MonoBehaviour
    {
        // Static Instance
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

        // Month Thing
        public MonthData[] monthData = new MonthData[12];

        // Events
        public List<TimedEvent> timedEvents = new List<TimedEvent>();

        // Helpers
        public float rotationDegreesPerDay = 360;
        public float hoursPerDay = 24;
        public float minutesPerHour = 60;
        public float secondsPerMinute = 60;
        public bool isAm;
        int currentMonthDayCount = 30;
        [HideInInspector] public ReminderManager reminderManager;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.DateAndTime;

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
            if (!useSystemTime)
            {
                currentSecond += Time.deltaTime * timeMultiplier;

                if (currentSecond >= 59)
                {
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
                            if (saveTimeData) { DreamOSDataManager.WriteIntData(dataCat, "CurrentDay", currentDay); }
                        }

                        if (currentDay > currentMonthDayCount)
                        {
                            currentDay = 1;
                            currentMonth += 1;

                            if (currentMonth == 13)
                            {
                                currentMonth = 1;
                                currentYear += 1;
                                if (saveTimeData) { DreamOSDataManager.WriteIntData(dataCat, "CurrentYear", currentYear); }              
                            }

                            currentMonthDayCount = monthData[currentMonth - 1].dayCount;

                            if (saveTimeData)
                            {
                                DreamOSDataManager.WriteIntData(dataCat, "CurrentDay", currentDay);
                                DreamOSDataManager.WriteIntData(dataCat, "CurrentMonth", currentMonth);
                            }
                        }

                        if (saveTimeData) { DreamOSDataManager.WriteIntData(dataCat, "CurrentHour", currentHour); }
                    }

                    if (saveTimeData) { DreamOSDataManager.WriteIntData(dataCat, "CurrentMinute", currentMinute); }
                    if (enableTimedEvents) { CheckForTimedEvents(); }
                }
            }

            else
            {
                currentSecond = GetCurrentSystemSecond();
                currentMinute = GetCurrentSystemMinute();
                if (useShortTimeFormat) { currentHour = GetCurrentSystemHourShort(); }
                else { currentHour = GetCurrentSystemHour(); }
                currentDay = GetCurrentSystemDay();
                currentMonth = GetCurrentSystemMonth();
                currentYear = GetCurrentSystemYear();
            }
        }

        public void UpdateTimeData(bool readAfter = true)
        {
            if (!saveTimeData || useSystemTime)
                return;

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "ShortTimeFormat")) { DreamOSDataManager.WriteBooleanData(dataCat, "ShortTimeFormat", useShortTimeFormat); }
            else if (DreamOSDataManager.ReadBooleanData(dataCat, "ShortTimeFormat")) { useShortTimeFormat = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCat, "ShortTimeFormat")) { useShortTimeFormat = false; }

            if (!DreamOSDataManager.ContainsJsonKey(dataCat, "CurrentMinute"))
            {
                if (defaultTimeFormat == DefaultShortTime.AM) { isAm = true; }
                else if (defaultTimeFormat == DefaultShortTime.PM) { isAm = false; }
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
                if (!timedEvents[i].isEnabled)
                    continue;

                // Check for meridiem format
                bool isMeridiemCorrect = false;
                if ((timedEvents[i].meridiemFormat == DefaultShortTime.AM && isAm) || (timedEvents[i].meridiemFormat == DefaultShortTime.PM && !isAm)) { isMeridiemCorrect = true; }

                if (timedEvents[i].eventType == TimedEventType.Once && timedEvents[i].eventHour == currentHour && timedEvents[i].eventMinute == currentMinute && isMeridiemCorrect)
                {
                    timedEvents[i].isEnabled = false;
                    timedEvents[i].onEventTime.Invoke();

                    if (timedEvents[i].isReminderItem) { reminderManager.EnableReminder(timedEvents[i].eventID, false); }
                    if (NotificationManager.instance != null) { NotificationManager.instance.CreateNotification(notificationIcon, "Timed Event", timedEvents[i].eventTitle); }
                }

                else if (timedEvents[i].eventType == TimedEventType.Daily && timedEvents[i].latestDay != currentDay && timedEvents[i].eventHour == currentHour && timedEvents[i].eventMinute == currentMinute && isMeridiemCorrect)
                {
                    timedEvents[i].latestDay = currentDay;
                    timedEvents[i].onEventTime.Invoke();

                    if (NotificationManager.instance != null) { NotificationManager.instance.CreateNotification(notificationIcon, "Timed Event", timedEvents[i].eventTitle); }
                }
            }
        }

        public void CreateTimedEvent(string title, int hour, int minute, DefaultShortTime meridiem, TimedEventType type = TimedEventType.Once, UnityEvent events = null)
        {
            TimedEvent tEvent = new TimedEvent();
            tEvent.eventTitle = title;
            tEvent.eventMinute = minute;
            tEvent.eventHour = hour;
            tEvent.meridiemFormat = meridiem;
            tEvent.eventType = type;
            tEvent.onEventTime.AddListener(events.Invoke);
            tEvent.isReminderItem = false;
            tEvent.isEnabled = true;
            timedEvents.Add(tEvent);
        }

        private void CheckForClockFormat()
        {
            if (useShortTimeFormat)
            {
                if (currentHour == 12 && !isAm) { isAm = true; DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", true); }
                else if (currentHour == 12 && isAm) { isAm = false; DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", false); }
            }

            else
            {
                if (currentHour >= 24 && !isAm) { isAm = true; DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", false); }
                else if (currentHour >= 12 && isAm) { isAm = false; DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", true); }
            }
        }

        public void ShortTimeFormat(bool value)
        {
            if (value == true) { useShortTimeFormat = true; }
            else { useShortTimeFormat = false; }

            DreamOSDataManager.WriteBooleanData(dataCat, "ShortTimeFormat", useShortTimeFormat);
            UpdateTimeFormat();
        }

        void UpdateTimeFormat()
        {
            if (useShortTimeFormat && currentHour >= 12)
            {
                isAm = false;
                DreamOSDataManager.WriteBooleanData(dataCat, "IsAM", false);

                if (currentHour == 13) { currentHour = 1; }
                else if (currentHour == 14) { currentHour = 2; }
                else if (currentHour == 15) { currentHour = 3; }
                else if (currentHour == 16) { currentHour = 4; }
                else if (currentHour == 17) { currentHour = 5; }
                else if (currentHour == 18) { currentHour = 6; }
                else if (currentHour == 19) { currentHour = 7; }
                else if (currentHour == 20) { currentHour = 8; }
                else if (currentHour == 21) { currentHour = 9; }
                else if (currentHour == 22) { currentHour = 10; }
                else if (currentHour == 23) { currentHour = 11; }
                else if (currentHour == 0) { currentHour = 12; }
            }

            else if (!useShortTimeFormat && !isAm)
            {
                if (currentHour == 1) { currentHour = 13; }
                else if (currentHour == 2) { currentHour = 14; }
                else if (currentHour == 3) { currentHour = 15; }
                else if (currentHour == 4) { currentHour = 16; }
                else if (currentHour == 5) { currentHour = 17; }
                else if (currentHour == 6) { currentHour = 18; }
                else if (currentHour == 7) { currentHour = 19; }
                else if (currentHour == 8) { currentHour = 20; }
                else if (currentHour == 9) { currentHour = 21; }
                else if (currentHour == 10) { currentHour = 22; }
                else if (currentHour == 11) { currentHour = 23; }
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