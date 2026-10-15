"""Read-only production checks; does not apply the patch or launch Unity."""
from pathlib import Path
import difflib
import hashlib
import json

out = Path(__file__).resolve().parent
root = out.parents[3]
hashes = json.loads((out / 'source-hashes.json').read_text(encoding='utf-8'))
patch = []
for relative, expected in hashes.items():
    source = root / relative
    actual = hashlib.sha256(source.read_bytes()).hexdigest().upper()
    if actual != expected:
        raise SystemExit(f'DEPENDENCY CHANGED: {relative}\nExpected {expected}\nActual   {actual}')
    candidate = out / 'candidate' / source.name
    if candidate.exists():
        patch.append(f'# Target: {relative}\n# Original SHA256: {expected}\n')
        patch.extend(difflib.unified_diff(
            source.read_text(encoding='utf-8-sig').splitlines(True),
            candidate.read_text(encoding='utf-8').splitlines(True),
            fromfile='a/' + relative, tofile='b/' + relative, n=4))
assert ''.join(patch) == (out / 'changes.patch').read_text(encoding='utf-8'), 'Candidate/patch mismatch'
print('Static delivery verification: 5 source hashes unchanged; patch matches 3 candidate files.')
print('Unity runtime tests and visual acceptance: NOT RUN.')
