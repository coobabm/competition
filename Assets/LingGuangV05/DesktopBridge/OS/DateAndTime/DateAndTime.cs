using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One clock face or date label bound to the <see cref="DateAndTimeManager"/> (2026-10-19). Replaces DreamOS's
    /// DateAndTime: same GUID, serialized fields and public API.
    ///
    /// Depending on <see cref="objectType"/> it turns the hour/minute(/second) hands of an analog clock, writes
    /// "H:MM" (optionally ":SS" and, in 12-hour mode, " AM"/" PM") into a text, or writes the date as
    /// day/month/year in the chosen <see cref="DateFormat"/> order, every frame.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/Date & Time/Date & Time")]
    public class DateAndTime : MonoBehaviour
    {
        // Settings
        [SerializeField] private bool enableAmPmLabel;
        [SerializeField] private bool addSeconds;
        public ObjectType objectType;
        public DateFormat dateFormat;

        // Bindings (filled in by the scene; textObj falls back to this object's text)
        [HideInInspector] public Transform clockHourHand;
        [HideInInspector] public Transform clockMinuteHand;
        [HideInInspector] public Transform clockSecondHand;
        [HideInInspector] public TextMeshProUGUI textObj;

        public enum ObjectType
        {
            AnalogClock,
            DigitalClock,
            DigitalDate,
        }

        public enum DateFormat
        {
            DD_MM_YYYY,
            MM_DD_YYYY,
            YYYY_MM_DD
        }

        void Awake()
        {
            bool isText = objectType == ObjectType.DigitalClock || objectType == ObjectType.DigitalDate;
            if (isText && textObj == null) { textObj = GetComponent<TextMeshProUGUI>(); }
        }

        void Update()
        {
            if (objectType == ObjectType.AnalogClock) { AnalogClock(); }
            else if (objectType == ObjectType.DigitalClock) { DigitalClock(); }
            else if (objectType == ObjectType.DigitalDate) { DigitalDate(); }
        }

        public void AnalogClock()
        {
            DateAndTimeManager time = DateAndTimeManager.instance;

            // The hour hand turns 30 degrees per hour, the minute and second hands 6 degrees per unit, clockwise.
            clockHourHand.localRotation = Quaternion.Euler(0, 0, time.currentHour * -15 * 2);
            clockMinuteHand.localRotation = Quaternion.Euler(0, 0, time.currentMinute * -6);
            if (addSeconds) { clockSecondHand.localRotation = Quaternion.Euler(0, 0, time.currentSecond * -6); }
        }

        public void DigitalClock()
        {
            DateAndTimeManager time = DateAndTimeManager.instance;

            // The hour is never padded; a one-digit minute gets a leading zero.
            string minute = time.currentMinute.ToString();
            if (minute.Length == 1) { minute = "0" + minute; }

            string text = string.Format("{0}:{1}", time.currentHour, minute);
            if (addSeconds) { text += ":" + time.currentSecond.ToString("00"); }

            if (time.useShortTimeFormat && enableAmPmLabel) { text += time.isAm ? " AM" : " PM"; }

            textObj.text = text;
        }

        public void DigitalDate()
        {
            DateAndTimeManager time = DateAndTimeManager.instance;

            if (dateFormat == DateFormat.DD_MM_YYYY) { textObj.text = string.Format("{0}.{1}.{2}", time.currentDay, time.currentMonth, time.currentYear); }
            else if (dateFormat == DateFormat.MM_DD_YYYY) { textObj.text = string.Format("{0}.{1}.{2}", time.currentMonth, time.currentDay, time.currentYear); }
            else if (dateFormat == DateFormat.YYYY_MM_DD) { textObj.text = string.Format("{0}.{1}.{2}", time.currentYear, time.currentMonth, time.currentDay); }
        }
    }
}
