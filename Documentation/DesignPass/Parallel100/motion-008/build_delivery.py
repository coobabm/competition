"""Build a review-only patch. Never writes production files."""
import difflib
import hashlib
import json
from pathlib import Path

out = Path(__file__).resolve().parent
root = out.parents[3]
target = 'Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs'
expected = '1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78'
paths = [target,
    'Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs',
    'Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs',
    'Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs',
    'Assets/LocalBuild/Editor/MotionButtonChecks.cs',
    'Documentation/DesignPass/Agents/motion-task-001.md',
    'Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Selectable.cs',
    'Library/PackageCache/com.unity.ugui@d8a2716f3013/Runtime/UGUI/UI/Core/Button.cs']
hashes = {p: hashlib.sha256((root / p).read_bytes()).hexdigest().upper() for p in paths}
assert hashes[target] == expected, 'Target changed: review again before generating patch.'
source = (root / target).read_text(encoding='utf-8-sig')
anchor = '        public override void OnSubmit(BaseEventData eventData)\n'
addition = '''        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var hold = GetComponent<XgHold>();
            if (hold == null || !hold.isActiveAndEnabled) return;

            // XgHold ends on exit and does not restart on re-entry. Release only
            // Selectable's visual state; do not dispatch Up or invoke business callbacks.
            // Do not inspect Held: either component may receive the exit first.
            base.OnPointerUp(eventData);
            ResetPress();
        }

'''
assert source.count(anchor) == 1
candidate = source.replace(anchor, addition + anchor)
patch = ''.join(difflib.unified_diff(source.splitlines(True), candidate.splitlines(True),
    fromfile='a/' + target, tofile='b/' + target))
(out / 'changes.patch').write_text('# Target: ' + target + '\n# Original SHA256: ' + expected + '\n' + patch, encoding='utf-8', newline='\n')
(out / 'XgButtonPressFeedback.candidate.cs').write_text(candidate, encoding='utf-8', newline='\n')
(out / 'source-hashes.json').write_text(json.dumps(hashes, indent=2) + '\n', encoding='utf-8')
print('Review-only patch and candidate generated; production files untouched.')
