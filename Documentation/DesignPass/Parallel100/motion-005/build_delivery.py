from pathlib import Path
import hashlib
import difflib

root = Path(__file__).resolve().parents[4]
out = Path(__file__).resolve().parent
target = 'Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs'
source = root / target
expected = '1F284BD333A1049AA0A8463BF9B3A3913D6C359AF0FEAA22794A66BCFAAABE78'
assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == expected, 'Source changed; re-review first'
old = source.read_text(encoding='utf-8-sig')
new = old.replace('using UnityEngine;', 'using System.Collections.Generic;\nusing UnityEngine;', 1)
new = new.replace('        float elapsed, duration;\n', '''        float elapsed, duration;

        struct PointerGesture
        {
            public int id;
            public bool cancelled, released;
        }
        List<PointerGesture> pointerGestures;

        int FindGesture(int id)
        {
            if (pointerGestures != null)
                for (int i = 0; i < pointerGestures.Count; i++)
                    if (pointerGestures[i].id == id) return i;
            return -1;
        }
''', 1)
new = new.replace('''            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) PressLabel();''', '''            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (pointerGestures == null) pointerGestures = new List<PointerGesture>();
            int previous = FindGesture(eventData.pointerId);
            if (previous >= 0) pointerGestures.RemoveAt(previous);
            pointerGestures.Add(new PointerGesture {
                id = eventData.pointerId, cancelled = !IsActive() || !IsInteractable()
            });
            // Track before selection callbacks, which can change ancestor groups.
            base.OnPointerDown(eventData);
            int current = FindGesture(eventData.pointerId);
            if (current < 0 || pointerGestures[current].cancelled)
            {
                base.OnPointerUp(eventData);
                return;
            }
            PressLabel();''', 1)
anchor = '        public override void OnSubmit(BaseEventData eventData)'
new = new.replace(anchor, '''        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int index = FindGesture(eventData.pointerId);
            if (index < 0) return;
            var gesture = pointerGestures[index];
            gesture.released = true;
            pointerGestures[index] = gesture;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            int index = FindGesture(eventData.pointerId);
            if (index >= 0 && pointerGestures[index].cancelled) return;
            base.OnPointerClick(eventData);
        }

        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            // Follow Selectable's effective inheritance, including ignoreParentGroups.
            if (IsInteractable() || pointerGestures == null) return;
            bool held = false;
            for (int i = 0; i < pointerGestures.Count; i++)
            {
                var gesture = pointerGestures[i];
                gesture.cancelled = true;
                held |= !gesture.released;
                pointerGestures[i] = gesture;
            }
            // Clear Selectable's private pressed flag without changing hover/selection.
            if (held) base.OnPointerUp(new PointerEventData(null));
        }

''' + anchor, 1)
new = new.replace('''        void LateUpdate()
        {
''', '''        void LateUpdate()
        {
            // Both project input modules deliver Up and Click synchronously before LateUpdate.
            // Keep cancellation through Click, then discard releases (including off-target ones).
            if (pointerGestures != null)
                for (int i = pointerGestures.Count - 1; i >= 0; i--)
                    if (pointerGestures[i].released) pointerGestures.RemoveAt(i);
''', 1)
new = new.replace('''        protected override void OnDisable()
        {
''', '''        protected override void OnDisable()
        {
            if (pointerGestures != null) pointerGestures.Clear();
''', 1)
(out / 'XgButtonPressFeedback.candidate.cs').write_text(new, encoding='utf-8', newline='\n')
patch = ''.join(difflib.unified_diff(old.splitlines(True), new.splitlines(True), 'a/' + target, 'b/' + target))
(out / 'changes.patch').write_text(patch, encoding='utf-8', newline='\n')
print('Candidate and context patch generated; production unchanged. Source SHA256:', expected)
