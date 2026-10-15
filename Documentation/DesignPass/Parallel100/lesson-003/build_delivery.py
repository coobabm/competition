from pathlib import Path
import hashlib, difflib, json

ROOT = Path(__file__).resolve().parents[4]
OUT = Path(__file__).resolve().parent
PAGE = 'Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs'
KEYS = 'Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.Spider.cs'
HELPER = 'Assets/LingGuangV05/XingGuang/Desktop/XgLessonAnswerInput.cs'
dependencies = [PAGE, KEYS, 'Assets/LingGuangV05/XingGuang/Core/XgSim.cs',
    'Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs',
    'Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs',
    'Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs']
raw = {p: (ROOT / p).read_bytes() for p in dependencies}
original = {p: b.decode('utf-8-sig').replace('\r\n', '\n') for p,b in raw.items()}
changed = {}
def replace(text, before, after):
    assert text.count(before) == 1, before
    return text.replace(before, after)

s = original[PAGE]
s = replace(s, '        bool shownGuessValid, shownGuess, shownTeachingEvaluation;', '''        bool shownGuessValid, shownGuess, shownTeachingEvaluation;
        XgSim lessonPresentedSim;
        long lessonPresentedId;
        bool lessonPresented, lessonPresentedGuess;
        int lessonPresentedFrame = -1;''')
s = replace(s, '"", () => OnAnswer(true), 24,', '"", null, 24,')
s = replace(s, '"", () => OnAnswer(false), 24,', '"", null, 24,')
anchor = '            no = ZhongbaoSkin.Outlined(ui, body, "", null, 24, new Color32(203, 210, 230, 255));'
s = replace(s, anchor, anchor + '''
            yes.button.gameObject.AddComponent<XgLessonAnswerInput>().Bind(yes.button, () => CaptureAnswer(true));
            no.button.gameObject.AddComponent<XgLessonAnswerInput>().Bind(no.button, () => CaptureAnswer(false));''')
s = replace(s, '        void OnAnswer(bool answer)\n        {', '''        // Mouse input belongs to the view at pointer-down, even if a refresh occurs before release.
        Action CaptureAnswer(bool answer)
        {
            var owner = displayedSim;
            int mode = displayedMode;
            long id = displayedId;
            string desk = Desk;
            bool teaching = shownTeachingEvaluation, guess = shownGuess, valid = shownGuessValid;
            bool wasPresented = !teaching || Time.frameCount > lessonPresentedFrame;
            return () =>
            {
                if (!wasPresented || (teaching || shownTeachingEvaluation) &&
                    (owner != displayedSim || mode != displayedMode || id != displayedId || desk != Desk ||
                     teaching != shownTeachingEvaluation || guess != shownGuess || valid != shownGuessValid))
                { Refresh(); return; }
                OnAnswer(answer);
            };
        }

        void OnAnswer(bool answer)
        {
            // Refresh updates labels synchronously; they have not rendered until the next frame.
            if (shownTeachingEvaluation && Time.frameCount <= lessonPresentedFrame) { Refresh(); return; }''')
s = replace(s, '            if (special) { RefreshSpecial(); LayoutSide(true, 0); return; }',
    '            if (special) { lessonPresented = false; RefreshSpecial(); LayoutSide(true, 0); return; }')
s = replace(s, '            shownTeachingEvaluation = teaching;', '''            if (teaching && (!lessonPresented || lessonPresentedSim != Sim || lessonPresentedId != card.id || lessonPresentedGuess != guess))
                lessonPresentedFrame = Time.frameCount;
            lessonPresented = teaching; lessonPresentedSim = Sim;
            lessonPresentedId = card.id; lessonPresentedGuess = guess;
            shownTeachingEvaluation = teaching;''')
changed[PAGE] = s
s = original[KEYS]
s = replace(s, 'yes.button.onClick.Invoke();', 'OnAnswer(true);')
s = replace(s, 'no.button.onClick.Invoke();', 'OnAnswer(false);')
changed[KEYS] = s
assert not (ROOT / HELPER).exists(), 'Helper now exists; re-review before integration'
changed[HELPER] = '''using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // Button retains its visuals/navigation; this component owns answer dispatch.
    // No callback runs on pointer-down, and canceled presses cannot submit a lesson.
    public sealed class XgLessonAnswerInput : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISubmitHandler
    {
        Button button;
        Func<Action> capture;
        Action pending;
        int pointerId;

        public void Bind(Button target, Func<Action> snapshot) { button = target; capture = snapshot; }
        bool Ready => button != null && button.IsActive() && button.IsInteractable() && capture != null;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            pending = null;
            if (!Ready) return;
            pointerId = data.pointerId;
            pending = capture();
        }

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || data.pointerId != pointerId) return;
            var submit = pending;
            pending = null; // Consume before dispatch, including reentrant callbacks.
            if (Ready) submit?.Invoke();
        }

        public void OnSubmit(BaseEventData data)
        {
            if (Ready) capture()?.Invoke();
        }

        void OnDisable() { pending = null; }
    }
}
'''
diff = []
for p,s in changed.items():
    diff.extend(difflib.unified_diff(original.get(p, '').splitlines(True), s.splitlines(True),
        fromfile='a/' + p if p in original else '/dev/null', tofile='b/' + p, n=4))
(OUT / 'changes.patch').write_text(''.join(diff), encoding='utf-8', newline='\n')
(OUT / 'source-sha256.json').write_text(json.dumps({p: hashlib.sha256(b).hexdigest().upper() for p,b in raw.items()}, indent=2) + '\n', encoding='utf-8')
(OUT / 'XgLessonAnswerInput.cs').write_text(changed[HELPER], encoding='utf-8')
assert all((ROOT / p).read_bytes() == b for p,b in raw.items()), 'Source changed during generation'
print('Static delivery generation complete; production bytes unchanged. Unity/runtime NOT executed.')
