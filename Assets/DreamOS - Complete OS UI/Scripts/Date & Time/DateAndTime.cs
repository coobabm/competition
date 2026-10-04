using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Date & Time/Date & Time")]
    public class DateAndTime : MonoBehaviour
    {
        // Settings
        [SerializeField] private bool enableAmPmLabel;
        [SerializeField] private bool addSeconds;
        public ObjectType objectType;
        public DateFormat dateFormat;

        // Helpers
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
            if (objectType == ObjectType.DigitalClock && textObj == null) { textObj = gameObject.GetComponent<TextMeshProUGUI>(); }
            else if (objectType == ObjectType.DigitalDate && textObj == null) { textObj = gameObject.GetComponent<TextMeshProUGUI>(); }
        }

        void Update()
        {
            if (objectType == ObjectType.AnalogClock) { AnalogClock(); }
            else if (objectType == ObjectType.DigitalClock) { DigitalClock(); }
            else if (objectType == ObjectType.DigitalDate) { DigitalDate(); }
        }

        public void AnalogClock()
        {
            clockHourHand.localRotation = Quaternion.Euler(0, 0, DateAndTimeManager.instance.currentHour * -15 * 2);
            clockMinuteHand.localRotation = Quaternion.Euler(0, 0, DateAndTimeManager.instance.currentMinute * -6);
            if (addSeconds == true) { clockSecondHand.localRotation = Quaternion.Euler(0, 0, DateAndTimeManager.instance.currentSecond * -6); }
        }

        public void DigitalClock()
        {
            if (DateAndTimeManager.instance.currentHour.ToString().Length != 1 && DateAndTimeManager.instance.currentMinute.ToString().Length == 1) { textObj.text = string.Format("{0}:0{1}", DateAndTimeManager.instance.currentHour, DateAndTimeManager.instance.currentMinute); }
            else if (DateAndTimeManager.instance.currentHour.ToString().Length == 1 && DateAndTimeManager.instance.currentMinute.ToString().Length == 1) { textObj.text = string.Format("{0}:0{1}", DateAndTimeManager.instance.currentHour, DateAndTimeManager.instance.currentMinute); }
            else if (DateAndTimeManager.instance.currentHour.ToString().Length == 1 && DateAndTimeManager.instance.currentMinute.ToString().Length != 1) { textObj.text = string.Format("{0}:{1}", DateAndTimeManager.instance.currentHour, DateAndTimeManager.instance.currentMinute); }
            else { textObj.text = string.Format("{0}:{1}", DateAndTimeManager.instance.currentHour, DateAndTimeManager.instance.currentMinute); }

            if (addSeconds) { textObj.text = textObj.text + ":" + DateAndTimeManager.instance.currentSecond.ToString("00"); }
            if (!DateAndTimeManager.instance.useShortTimeFormat) { return; }

            if (DateAndTimeManager.instance.isAm && enableAmPmLabel) { textObj.text = textObj.text + " AM"; }
            else if (!DateAndTimeManager.instance.isAm && enableAmPmLabel) { textObj.text = textObj.text + " PM"; }
        }

        public void DigitalDate()
        {
            if (dateFormat == DateFormat.DD_MM_YYYY) { textObj.text = string.Format("{0}.{1}.{2}", DateAndTimeManager.instance.currentDay, DateAndTimeManager.instance.currentMonth, DateAndTimeManager.instance.currentYear); }
            else if (dateFormat == DateFormat.MM_DD_YYYY) { textObj.text = string.Format("{0}.{1}.{2}", DateAndTimeManager.instance.currentMonth, DateAndTimeManager.instance.currentDay, DateAndTimeManager.instance.currentYear); }
            else if (dateFormat == DateFormat.YYYY_MM_DD) { textObj.text = string.Format("{0}.{1}.{2}", DateAndTimeManager.instance.currentYear, DateAndTimeManager.instance.currentMonth, DateAndTimeManager.instance.currentDay); }
        }
    }
}