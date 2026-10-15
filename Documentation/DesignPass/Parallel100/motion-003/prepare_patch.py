"""Build a review-only candidate; never change production files."""
from pathlib import Path
import difflib
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
TARGET = 'Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs'
EXPECTED = '1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78'
raw = (ROOT / TARGET).read_bytes()
assert hashlib.sha256(raw).hexdigest().upper() == EXPECTED, 'Source changed: review before regenerating'
old = raw.decode('utf-8-sig').replace('\r\n', '\n')
new = old.replace(' : Button\n', ' : Button, ICancelHandler\n')
new = new.replace('        float elapsed, duration;\n', '''        float elapsed, duration;
        int? feedbackPointerId;
''')
new = new.replace('if (eventData.button == PointerEventData.InputButton.Left) PressLabel();',
                  'if (eventData.button == PointerEventData.InputButton.Left) PressLabel(eventData.pointerId);')
new = new.replace('        public override void OnSubmit', '''        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (feedbackPointerId == eventData.pointerId) ResetPress();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            // Input modules send Up before clearing eligibleForClick on drag takeover.
            // Do not implement IDragHandler: the parent ScrollRect must retain the drag.
            if (eventData.button == PointerEventData.InputButton.Left &&
                feedbackPointerId == eventData.pointerId && eventData.dragging &&
                eventData.pointerPress != eventData.pointerDrag)
                ResetPress();
            // Ordinary releases keep their rebound and the inherited click dispatch.
        }

        public void OnCancel(BaseEventData eventData)
        {
            // Navigation Cancel clears feedback only; it does not consume the event
            // or change the input module's independent pointer click eligibility.
            var pointer = eventData as PointerEventData;
            if (pointer == null || feedbackPointerId == pointer.pointerId) ResetPress();
        }

        public override void OnSubmit''')
new = new.replace('        void PressLabel()\n', '        void PressLabel(int? pointerId = null)\n')
new = new.replace('            elapsed = 0;\n', '''            elapsed = 0;
            // Only a successfully started press becomes the owner. Submit has no pointer.
            feedbackPointerId = pointerId;
''')
new = new.replace('        void ResetPress()\n        {\n', '''        void ResetPress()
        {
            feedbackPointerId = null;
''')
(HERE / 'XgButtonPressFeedback.candidate.cs').write_text(new, encoding='utf-8')
patch = ''.join(difflib.unified_diff(old.splitlines(True), new.splitlines(True),
                                   'a/' + TARGET, 'b/' + TARGET, n=5))
(HERE / 'changes.patch').write_text('# Base SHA256: ' + EXPECTED + '\n' + patch, encoding='utf-8')
paths = [TARGET, 'Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs',
         'Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs',
         'Assets/LocalBuild/Editor/MotionButtonChecks.cs',
         'Documentation/DesignPass/Agents/motion-task-001.md']
package = 'Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/'
paths += [package + p for p in ['UI/Core/Button.cs', 'UI/Core/Selectable.cs',
          'EventSystem/InputModules/PointerInputModule.cs',
          'EventSystem/InputModules/StandaloneInputModule.cs']]
paths += ['Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/Plugins/UI/InputSystemUIInputModule.cs']
(HERE / 'source-sha256.json').write_text(json.dumps({p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest().upper()
                                                   for p in paths}, indent=2), encoding='utf-8')
print('Review candidate and contextual patch generated; production unchanged.')
