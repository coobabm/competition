using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.DreamOS
{
    [AddComponentMenu("DreamOS/Events/Press Key Event")]
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