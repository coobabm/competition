using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Invokes a UnityEvent after a delay on scaled time, either automatically at Start or when told to. Replaces
    /// DreamOS's TimedEvent (2026-10-19): same GUID, serialized fields and public API (the method names
    /// StartIEnumerator / StopIEnumerator are wired in scenes).
    /// </summary>
    [AddComponentMenu("LingGuang/OS/Events/Timed Event")]
    public class TimedEvent : MonoBehaviour
    {
        // Settings
        public float timer = 4;
        public bool enableAtStart;

        // Events
        public UnityEvent timerAction;

        void Start()
        {
            if (enableAtStart == true)
                StartCoroutine(nameof(ProcessTimedEvent));
        }

        IEnumerator ProcessTimedEvent()
        {
            yield return new WaitForSeconds(timer);
            timerAction.Invoke();
            StopCoroutine(nameof(ProcessTimedEvent));
        }

        public void StartIEnumerator()
        {
            StartCoroutine(nameof(ProcessTimedEvent));
        }

        public void StopIEnumerator()
        {
            StopCoroutine(nameof(ProcessTimedEvent));
        }
    }
}
