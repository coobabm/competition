using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Attaches the story system to any scene that has a <see cref="ChapterOneRuntime"/>, without editing the
    /// scene file: director next to the runtime (same lifetime as the save), presenter next to the router.
    /// Scenes without the runtime are untouched.
    /// </summary>
    public static class StoryBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            var runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null) return;
            var go = runtime.gameObject;
            var director = go.GetComponent<StoryDirector>();
            if (director == null) director = go.AddComponent<StoryDirector>();
            director.runtime = runtime;
            var presenter = go.GetComponent<StoryDesktopPresenter>();
            if (presenter == null) presenter = go.AddComponent<StoryDesktopPresenter>();
            presenter.director = director;
            presenter.router = go.GetComponent<ChapterOneDesktopRouter>() ?? Object.FindAnyObjectByType<ChapterOneDesktopRouter>();
            var clock = go.GetComponent<GameEraClock>();
            if (clock == null) clock = go.AddComponent<GameEraClock>();
            clock.runtime = runtime;
            var install = go.GetComponent<LingGuangInstallFlow>();
            if (install == null) install = go.AddComponent<LingGuangInstallFlow>();
            install.runtime = runtime;
            install.router = presenter.router;
        }
    }
}
