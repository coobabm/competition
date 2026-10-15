"""Read-only dependency verification. Does not compile or run the game."""
from pathlib import Path
import hashlib
import json

out = Path(__file__).resolve().parent
root = out.parents[3]
expected = json.loads((out / 'source-sha256.json').read_text(encoding='utf-8'))
changed = []
for name, sha in expected.items():
    actual = hashlib.sha256((root / name).read_bytes()).hexdigest().upper()
    if actual != sha:
        changed.append(name)
if changed:
    raise SystemExit('Source changed; re-review patch before integration:\n' + '\n'.join(changed))
print('Static hash verification: dependencies unchanged. Unity/runtime NOT executed.')
