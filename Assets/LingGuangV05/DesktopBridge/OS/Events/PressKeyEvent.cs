using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Invokes a UnityEvent when a hotkey (an Input System action set in the inspector) is triggered. Replaces
    /// DreamOS's PressKeyEvent (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    [AddComponentMenu("LingGuang/OS/Events/Press Key Event")]
    public class PressKeyEvent : MonoBehaviour
    {
        // Resources
        [SerializeField] private InputAction hotkey;

        // Events
        public UnityEvent pressAction;

        void Start()
        {
            hotkey.Enable();
        }

        void Update()
        {
            if (hotkey.triggered)
            {
                pressAction.Invoke();
            }
        }
    }
}
