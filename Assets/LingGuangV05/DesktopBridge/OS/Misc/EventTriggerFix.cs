using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Works around a Unity (2021+) issue where an EventTrigger on a UI object keeps firing pointer events at the
    /// wrong time: the sibling EventTrigger is disabled and this component forwards enter, exit and click to it. The
    /// exit is delayed by 0.1 s and cancelled if the pointer re-enters. Replaces DreamOS's EventTriggerFix
    /// (2026-10-19): same GUID and behaviour. Needs an EventTrigger on the same object.
    /// </summary>
    public class EventTriggerFix : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        const float ExitDelay = 0.1f;

        EventTrigger tempTrigger;
        bool onEnter;

        void Awake()
        {
            tempTrigger = gameObject.GetComponent<EventTrigger>();
            tempTrigger.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onEnter = true;
            tempTrigger.OnPointerEnter(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onEnter = false;

            StopCoroutine(nameof(WaitForPointerExit));
            StartCoroutine(nameof(WaitForPointerExit), eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            tempTrigger.OnPointerClick(eventData);
        }

        IEnumerator WaitForPointerExit(PointerEventData eventData)
        {
            yield return new WaitForSeconds(ExitDelay);
            if (onEnter == false) { tempTrigger.OnPointerExit(eventData); }
        }
    }
}
