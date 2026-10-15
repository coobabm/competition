using LingGuangV05.Runtime;
using LingGuangV05.CaseMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.Desktop
{
    /// <summary>Both shutdown entry points leave Main through the same save and scene transition.</summary>
    public static class GameExit
    {
        public const string TitleScene = "ComputerCaseMenu";
        public static void ReturnToTitle()
        {
            var menu = Object.FindAnyObjectByType<CaseMenuController>();
            if (menu != null && (menu.IsReturningToTitle || menu.IsTransitioning)) return;
            var runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime != null && runtime.useDiskSave && !runtime.TestMode && runtime.Sim != null && !runtime.Sim.InPrologue)
            {
                if (!runtime.SaveNow())
                {
                    Object.FindAnyObjectByType<LingGuangV05.Desktop.Story.StoryDesktopPresenter>()?.ShowToast("尚未关机：" + runtime.SaveStatus + "。进度仍在，请重试保存。", null);
                    return;
                }
            }
            Time.timeScale = 1;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (menu != null && menu.TryReturnToTitle()) return;
            if (Application.CanStreamedLevelBeLoaded(TitleScene)) SceneManager.LoadScene(TitleScene);
            else
            {
                Debug.LogWarning("Title scene unavailable; quitting instead of leaving a black screen.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
