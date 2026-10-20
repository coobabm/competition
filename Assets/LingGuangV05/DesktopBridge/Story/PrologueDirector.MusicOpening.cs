using System.Collections;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    public sealed partial class PrologueDirector
    {
        // Only Run's new-game prologue enters this sequence. Loaded games past setup
        // retain their normal desktop and music state; no real cursor is moved.
        IEnumerator ApproachMusicBeforeVirus()
        {
            SetStep("music-intent");
            Hijack(true);
            Think(T("打开电脑的第一件事当然是听音乐...", "Of course, the first thing to do after turning on the computer is listen to music..."), 2);
            yield return PrologueDesk.Wait(1.1f);

            var music = desk.icons != null ? desk.icons.Find("Music Player") as RectTransform : null;
            if (music == null || !music.gameObject.activeInHierarchy)
            {
                // An altered desktop must not strand a new game behind an input lock.
                Debug.LogWarning("[Prologue] Music shortcut unavailable; continuing to the virus alert");
                Release();
                yield break;
            }
            SetStep("music-approach");
            yield return desk.Move(desk.LocalOf(music), 1.15f);
            SetStep("music-interrupted");
            // Let the arrival be visible, but never dispatch a click/open/play event.
            // VirusAlert constructs its window before returning control to the player.
            yield return PrologueDesk.Wait(.16f);
        }
    }
}
