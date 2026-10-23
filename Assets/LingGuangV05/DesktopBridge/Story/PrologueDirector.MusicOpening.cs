using System.Collections;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    public sealed partial class PrologueDirector
    {
        // Only Run's new-game prologue enters this sequence. Loaded games past setup
        // retain their normal desktop and music state; no real cursor is moved.
        // The song is already playing (GameMusic starts it when the desktop shows); it goes to change it and never gets there.
        IEnumerator ApproachMusicBeforeVirus()
        {
            SetStep("music-intent");
            Hijack(true);
            // The background music now plays from the moment the desktop shows (玩家反馈 F2), so it goes to change the song.
            Think(T("先把歌切了……", "First, let me change the song…"), 2);
            yield return W(1.1f);

            var music = desk.icons != null ? desk.icons.Find("Music Player") as RectTransform : null;
            if (music == null || !music.gameObject.activeInHierarchy)
            {
                // An altered desktop must not strand a new game behind an input lock.
                Debug.LogWarning("[Prologue] Music shortcut unavailable; continuing to the virus alert");
                Release();
                yield break;
            }
            SetStep("music-approach");
            yield return Mv(desk.LocalOf(music), 1.15f);
            SetStep("music-interrupted");
            // Let the arrival be visible, but never dispatch a click/open/play event.
            // VirusAlert constructs its window before returning control to the player.
            yield return W(.16f);
        }
    }
}
