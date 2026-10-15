"""Read-only static delivery checks. Does not compile C# or start Unity."""
import hashlib
import json
import subprocess
from pathlib import Path

out = Path(__file__).resolve().parent
root = out.parents[3]
hashes = json.loads((out / "source-sha256.json").read_text(encoding="utf-8"))
changed = [p for p, expected in hashes.items()
           if hashlib.sha256((root / p).read_bytes()).hexdigest() != expected]
if changed:
    raise SystemExit("Dependencies changed; re-review required:\n" + "\n".join(changed))
print("STATIC: all recorded dependencies unchanged")
for name in ("XgWallpaperConcepts.candidate.cs", "XgWallpaperConcepts.Lesson.cs"):
    lines = (out / name).read_text(encoding="utf-8").splitlines()
    assert all(line == line.rstrip() for line in lines), name + ": trailing whitespace"
result = subprocess.run(["git", "-c", "core.whitespace=cr-at-eol", "apply", "--check", "--ignore-space-change",
                         str(out / "changes.patch")], cwd=root, capture_output=True, text=True)
if result.returncode:
    raise SystemExit(result.stderr)
print("STATIC: patch context accepted; no production file changed")
print("NOT RUN: Unity compilation, event/lifecycle execution and visual acceptance")
