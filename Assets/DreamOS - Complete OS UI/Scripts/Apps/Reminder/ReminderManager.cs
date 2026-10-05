using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Michsky.DreamOS
{
    public class ReminderManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private GameObject reminderPreset;
        [SerializeField] private Transform reminderParent;
        [SerializeField] private ModalWindowManager reminderModal;
        [SerializeField] private TMP_InputField eventTitleObject;
        [SerializeField] private HorizontalSelector typeSelector;
        [SerializeField] private HorizontalSelector hourSelector;
        [SerializeField] private HorizontalSelector minuteSelector;
        [SerializeField] private HorizontalSelector meridiemSelector;

        // Settings
        int reminderLimit = 10;

        // Helpers
        List<ReminderItem> activeItems = new List<ReminderItem>();
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.DateAndTime;

        void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (DateAndTimeManager.instance == null)
                return;

            // Get current item count
            int itemCount = GetCurrentItemCount();

            // Delete each cached item
            foreach (Transform child in reminderParent) { Destroy(child.gameObject); }

            // Search for items
            for (int i = 0; i < reminderLimit; i++)
            {
                if (DreamOSDataManager.ContainsJsonKey(dataCat, "ReminderItem#" + i.ToString() + "_IsEnabled"))
                {
                    // Create the object
                    GameObject rObject = Instantiate(reminderPreset, new Vector3(0, 0, 0), Quaternion.identity);
                    rObject.name = "ReminderItem#" + i.ToString();
                    rObject.transform.SetParent(reminderParent, false);

                    // Get the component
                    ReminderItem rItem = rObject.GetComponent<ReminderItem>();
                    rItem.manager = this;
                    rItem.reminderID = rObject.name;
                    rItem.mainButton.onClick.AddListener(delegate { rItem.InitializeWindow(reminderModal, eventTitleObject, typeSelector, minuteSelector, hourSelector, meridiemSelector); });
                    rItem.switchManager.onEvents.AddListener(delegate { EnableReminder(rItem.reminderID, true, false); });
                    rItem.switchManager.offEvents.AddListener(delegate { EnableReminder(rItem.reminderID, false, false); });
                    activeItems.Add(rItem);

                    // Create the timed event item
                    DateAndTimeManager.TimedEvent tEvent = new DateAndTimeManager.TimedEvent();
                    tEvent.eventID = rObject.name;
                    tEvent.eventMinute = DreamOSDataManager.ReadIntData(dataCat, rObject.name + "_Minute");
                    tEvent.eventHour = DreamOSDataManager.ReadIntData(dataCat, rObject.name + "_Hour");
                    if (DreamOSDataManager.ReadIntData(dataCat, rObject.name + "_IsPM") == 0) { tEvent.meridiemFormat = DateAndTimeManager.DefaultShortTime.AM; rItem.isAM = true; }
                    else { tEvent.meridiemFormat = DateAndTimeManager.DefaultShortTime.PM; rItem.isAM = false; }
                    tEvent.eventTitle = DreamOSDataManager.ReadStringData(dataCat, rObject.name + "_Title");
                    tEvent.isReminderItem = true;
                    tEvent.isEnabled = DreamOSDataManager.ReadBooleanData(dataCat, rObject.name + "_IsEnabled");

                    // Set event type
                    if (DreamOSDataManager.ReadIntData(dataCat, rObject.name + "_Type") == 0) 
                    {
                        rItem.SetOnce();
                        tEvent.eventType = DateAndTimeManager.TimedEventType.Once;
                    }

                    else if (DreamOSDataManager.ReadIntData(dataCat, rObject.name + "_Type") == 1)
                    {
                        rItem.SetDaily();
                        tEvent.eventType = DateAndTimeManager.TimedEventType.Daily;
                    }

                    // Check for enable state
                    if (tEvent.isEnabled) { rItem.switchManager.isOn = true; rItem.switchManager.UpdateUI(); }
                    else if (!tEvent.isEnabled) { rItem.switchManager.isOn = false; rItem.switchManager.UpdateUI(); }

                    // Set UI
                    rItem.SetTitle(DreamOSDataManager.ReadStringData(dataCat, rObject.name + "_Title"));
                    if (tEvent.eventMinute < 10) { rItem.SetTime(string.Format("{0}:0{1} {2}", tEvent.eventHour, tEvent.eventMinute, tEvent.meridiemFormat)); }
                    else { rItem.SetTime(string.Format("{0}:{1} {2}", tEvent.eventHour, tEvent.eventMinute, tEvent.meridiemFormat)); }

                    // Add items
                    activeItems.Add(rItem);
                    DateAndTimeManager.instance.timedEvents.Add(tEvent);
                }
            }
        }

        public void EnableReminder(string itemID, bool value, bool updateSwitch = true)
        {
            // Cache an item
            ReminderItem tempItem = null;

            // Search for itemID
            for (int i = 0; i < activeItems.Count; i++)
            {
                if (activeItems[i].reminderID == itemID)
                {
                    tempItem = activeItems[i];
                    break;
                }
            }

            // Don't go further if the item cannot be found
            if (tempItem == null)
                return;

            // Update timedEvents[] value
            for (int i = 0; i < DateAndTimeManager.instance.timedEvents.Count; i++)
            {
                if (DateAndTimeManager.instance.timedEvents[i].eventID == itemID)
                {
                    DateAndTimeManager.instance.timedEvents[i].isEnabled = value;
                    break;
                }
            }

            // Set data and update the interface
            DreamOSDataManager.WriteBooleanData(dataCat, tempItem.reminderID + "_IsEnabled", value);
            if (updateSwitch) { tempItem.switchManager.isOn = value; tempItem.switchManager.UpdateUI(); }
        }

        public void DeleteReminder(string itemID, bool disableBefore = true)
        {
            // Disable before deleting
            if (disableBefore) { EnableReminder(itemID, false, false); }

            // Delete data
            DreamOSDataManager.DeleteData(dataCat, itemID + "_IsEnabled");
            DreamOSDataManager.DeleteData(dataCat, itemID + "_Title");
            DreamOSDataManager.DeleteData(dataCat, itemID + "_Type");
            DreamOSDataManager.DeleteData(dataCat, itemID + "_Minute");
            DreamOSDataManager.DeleteData(dataCat, itemID + "_Hour");
            DreamOSDataManager.DeleteData(dataCat, itemID + "_IsPM");

            // Get current item count
            int itemCount = GetCurrentItemCount();

            // Set new item count
            DreamOSDataManager.WriteIntData(dataCat, "ReminderItemCount", itemCount - 1);
        }

        public void CreateReminder()
        {
            // Get current item count
            int itemCount = GetCurrentItemCount();

            if (itemCount == reminderLimit)
                return;

            // Set new item count
            DreamOSDataManager.WriteIntData(dataCat, "ReminderItemCount", itemCount + 1);

            // Create the object
            GameObject rObject = Instantiate(reminderPreset, new Vector3(0, 0, 0), Quaternion.identity);
            rObject.name = "ReminderItem#" + itemCount.ToString();
            rObject.transform.SetParent(reminderParent, false);

            // Get the component
            ReminderItem rItem = rObject.GetComponent<ReminderItem>();
            rItem.manager = this;
            rItem.isAM = true;
            rItem.reminderID = rObject.name;
            rItem.mainButton.onClick.AddListener(delegate { rItem.InitializeWindow(reminderModal, eventTitleObject, typeSelector, minuteSelector, hourSelector, meridiemSelector); });
            rItem.switchManager.onEvents.AddListener(delegate { EnableReminder(rItem.reminderID, true, false); });
            rItem.switchManager.offEvents.AddListener(delegate { EnableReminder(rItem.reminderID, false, false); });

            // Create the timed event item
            DateAndTimeManager.TimedEvent tEvent = new DateAndTimeManager.TimedEvent();
            tEvent.eventID = rObject.name;
            tEvent.eventMinute = 0;
            tEvent.eventHour = 8;
            tEvent.meridiemFormat = DateAndTimeManager.DefaultShortTime.AM;
            tEvent.eventTitle = "Reminder " + (itemCount + 1).ToString();
            tEvent.isReminderItem = true;
            tEvent.isEnabled = true;

            // Set data
            DreamOSDataManager.WriteBooleanData(dataCat, rObject.name + "_IsEnabled", true);
            DreamOSDataManager.WriteStringData(dataCat, rObject.name + "_Title", "Reminder " + (itemCount + 1).ToString());
            DreamOSDataManager.WriteIntData(dataCat, rObject.name + "_Type", 0);
            DreamOSDataManager.WriteIntData(dataCat, rObject.name + "_Minute", 0);
            DreamOSDataManager.WriteIntData(dataCat, rObject.name + "_Hour", 8);
            DreamOSDataManager.WriteIntData(dataCat, rObject.name + "_IsPM", 0);

            // Set UI
            rItem.SetOnce();
            rItem.SetTitle("Reminder " + (itemCount + 1).ToString());
            if (tEvent.eventMinute < 10) { rItem.SetTime(string.Format("{0}:0{1} {2}", tEvent.eventHour, tEvent.eventMinute, tEvent.meridiemFormat)); }
            else { rItem.SetTime(string.Format("{0}:{1} {2}", tEvent.eventHour, tEvent.eventMinute, tEvent.meridiemFormat)); }

            // Update switch UI
            rItem.switchManager.isOn = true;
            rItem.switchManager.UpdateUI();

            // Add items
            activeItems.Add(rItem);
            DateAndTimeManager.instance.timedEvents.Add(tEvent);
        }

        public void UpdateReminderData(string itemID, string itemTitle, int itemType, int itemMinute, int itemHour, int isPM)
        {
            DreamOSDataManager.WriteIntData(dataCat, itemID + "_Type", itemType);
            DreamOSDataManager.WriteStringData(dataCat, itemID + "_Title", itemTitle);
            DreamOSDataManager.WriteIntData(dataCat, itemID + "_Minute", itemMinute);
            DreamOSDataManager.WriteIntData(dataCat, itemID + "_Hour", itemHour);
            DreamOSDataManager.WriteIntData(dataCat, itemID + "_IsPM", isPM);
        }

        int GetCurrentItemCount()
        {
            int itemCount = 0;
            if (DreamOSDataManager.ContainsJsonKey(dataCat, "ReminderItemCount")) { itemCount = DreamOSDataManager.ReadIntData(dataCat, "ReminderItemCount"); }
            return itemCount;
        }
    }
}