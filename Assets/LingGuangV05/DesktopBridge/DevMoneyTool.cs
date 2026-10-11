// Test tool: excluded entirely from non-development release builds.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
namespace LingGuangV05.Desktop
{
    public sealed class DevMoneyTool : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime != null && runtime.GetComponent<DevMoneyTool>() == null) runtime.gameObject.AddComponent<DevMoneyTool>();
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            var runtime = GetComponent<ChapterOneRuntime>();
            if (keyboard == null || !keyboard.f9Key.wasPressedThisFrame || runtime?.Sim == null) return;
            double delta = keyboard.shiftKey.isPressed ? -10000 : 10000;
            if (keyboard.ctrlKey.isPressed) runtime.Sim.S.money = 0;
            else runtime.Sim.S.money += delta;
            runtime.MarkDirty();
            PrologueDirector.Desk?.StoryPopup("[DEV]", "金币：¥" + runtime.Sim.S.money.ToString("0"), 3);
        }
    }
}
#endif
