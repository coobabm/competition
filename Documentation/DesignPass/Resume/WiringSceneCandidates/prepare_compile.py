from pathlib import Path
import hashlib
import json
import difflib

root = Path.cwd()
candidate = root / 'Documentation/DesignPass/Resume/WiringSceneCandidates'
out = root / 'LocalVerification/DesignPass/WiringSceneCompile'
dispatcher = root / 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs'
source = dispatcher.read_bytes().decode('utf-8-sig')
original = source
anchor = '                    if (mode == "answer-correction") StartCorrectionChecks(controller, brain);'
assert source.count(anchor) == 1
after_anchor = source[source.index(anchor) + len(anchor):]
newline = '\r\n' if after_anchor.startswith('\r\n') else '\n'
source = source.replace(anchor, anchor + newline + '                    if (mode == "wiring-scene") StartWiringSceneChecks(controller, runtime);')
(candidate / 'DesignWallpaperSmoke.cs').write_bytes(source.encode('utf-8'))
patch = ''.join(difflib.unified_diff(original.splitlines(keepends=True), source.splitlines(keepends=True),
    fromfile='a/Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs',
    tofile='b/Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs', n=3))
(candidate / 'dispatcher.patch').write_bytes(patch.encode('utf-8'))
rsp = (root / 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp').read_text(encoding='utf-8-sig')
lines = []
for line in rsp.splitlines():
    if line.startswith('-out:'):
        line = '-out:"LocalVerification/DesignPass/WiringSceneCompile/Assembly-CSharp-Editor.dll"'
    elif line.startswith('-refout:'):
        line = '-refout:"LocalVerification/DesignPass/WiringSceneCompile/Assembly-CSharp-Editor.ref.dll"'
    elif line == '"Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs"':
        line = '"Documentation/DesignPass/Resume/WiringSceneCandidates/DesignWallpaperSmoke.cs"'
    lines.append(line)
lines.append('"Documentation/DesignPass/Resume/WiringSceneCandidates/DesignWallpaperSmoke.WiringScene.cs"')
(out / 'editor.rsp').write_text('\n'.join(lines) + '\n', encoding='utf-8')
manifest = {'dispatcher_base': hashlib.sha256(dispatcher.read_bytes()).hexdigest().upper(),
    'candidate': hashlib.sha256((candidate / 'DesignWallpaperSmoke.WiringScene.cs').read_bytes()).hexdigest().upper(),
    'note': 'Do not replace the production dispatcher wholesale; apply dispatcher.patch only.'}
(candidate / 'hashes.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
