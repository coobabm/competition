using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// On Main's Event System. When the title (ComputerCaseMenu) preloads Main additively, the title keeps its own EventSystem
    /// and this one must never enable: a second enabled EventSystem logs "There can be only one active Event System" from its
    /// OnEnable, before CaseMenuMainDisplay can switch it off. Runs before that OnEnable; Main opened directly keeps its own.
    /// </summary>
    [DefaultExecutionOrder(-32000), DisallowMultipleComponent]
    public sealed class SingleEventSystemGuard : MonoBehaviour
    {
        void Awake()
        {
            var own = GetComponent<EventSystem>();
            // current lists only EventSystems that have already enabled, i.e. another scene's.
            if (own == null || EventSystem.current == null || EventSystem.current == own) return;
            own.enabled = false;
            foreach (var module in GetComponents<BaseInputModule>()) module.enabled = false;
        }
    }
}
