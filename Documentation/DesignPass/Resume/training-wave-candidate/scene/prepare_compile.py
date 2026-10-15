"""Compile candidate assemblies outside Assets/Library; never launch Unity or the scene."""
from pathlib import Path
import difflib
import hashlib
import json
import subprocess
import sys

HERE = Path(__file__).resolve().parent
CANDIDATE = HERE.parent
ROOT = HERE.parents[4]
OUT = ROOT / 'LocalVerification/DesignPass/TrainingSceneCompile'
BEE = 'Library/Bee/artifacts/1900b0aE.dag'
DOTNET = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/NetCoreRuntime/dotnet.exe'
CSC = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/DotNetSdkRoslyn/csc.dll'

def relative(path):
    return path.relative_to(ROOT).as_posix()

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    dispatcher = ROOT / 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs'
    before = dispatcher.read_bytes().decode('utf-8-sig')
    anchor = '                    if (mode == "wiring-scene") StartWiringSceneChecks(controller, runtime);'
    assert before.count(anchor) == 1, 'Expected one current Wiring dispatcher anchor'
    tail = before[before.index(anchor) + len(anchor):]
    newline = '\r\n' if tail.startswith('\r\n') else '\n'
    after = before.replace(anchor, anchor + newline + '                    if (mode == "training-scene") StartTrainingSceneChecks(controller, runtime);')
    (HERE / 'DesignWallpaperSmoke.compile-only.cs').write_bytes(after.encode('utf-8'))
    patch = ''.join(difflib.unified_diff(before.splitlines(keepends=True), after.splitlines(keepends=True),
        fromfile='a/Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs', tofile='b/Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs'))
    (HERE / 'dispatcher.patch').write_bytes(patch.encode('utf-8'))
    replacements = {
        'Assets/LingGuangV05/XingGuang/Core/XgBoard.cs': CANDIDATE / 'XgBoard.cs',
        'Assets/LingGuangV05/XingGuang/Desktop/XgBrainWallpaper.cs': CANDIDATE / 'XgBrainWallpaper.cs',
        'Assets/LingGuangV05/XingGuang/Desktop/XgWallpaperConcepts.cs': CANDIDATE / 'XgWallpaperConcepts.cs',
        'Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs': HERE / 'DesignWallpaperSmoke.compile-only.cs',
    }
    additions = {
        'LingGuangV05.XingGuang.Core': [CANDIDATE / 'XgConceptTransition.cs'],
        'Assembly-CSharp': [CANDIDATE / 'XgWallpaperTraining.cs'],
        'Assembly-CSharp-Editor': [HERE / 'DesignWallpaperSmoke.TrainingScene.cs'],
    }
    outputs = []
    for assembly, extra in additions.items():
        source = (ROOT / BEE / (assembly + '.rsp')).read_text(encoding='utf-8-sig')
        lines = []
        for line in source.splitlines():
            if line.startswith('-out:'):
                line = '-out:"' + relative(OUT / (assembly + '.dll')) + '"'
            elif line.startswith('-refout:'):
                line = '-refout:"' + relative(OUT / (assembly + '.ref.dll')) + '"'
            else:
                for old, new in replacements.items():
                    if line == '"' + old + '"': line = '"' + relative(new) + '"'
                for dependency in ('LingGuangV05.XingGuang.Core', 'Assembly-CSharp'):
                    old = '-r:"' + BEE + '/' + dependency + '.ref.dll"'
                    if line == old: line = '-r:"' + relative(OUT / (dependency + '.ref.dll')) + '"'
            lines.append(line)
        for item in extra: lines.append('"' + relative(item) + '"')
        response = OUT / (assembly + '.rsp')
        response.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        log = OUT / (assembly + '.log')
        with log.open('wb') as stream:
            try:
                result = subprocess.run([DOTNET, CSC, '@' + relative(response)], cwd=ROOT,
                    stdout=stream, stderr=subprocess.STDOUT, timeout=60,
                    creationflags=subprocess.CREATE_NO_WINDOW)
                code = result.returncode
            except subprocess.TimeoutExpired:
                code = 124
        outputs.append({'assembly': assembly, 'exit_code': code, 'log': relative(log)})
        (OUT / 'exit-codes.json').write_text(json.dumps(outputs, indent=2), encoding='utf-8')
        print(assembly + ': exit=' + str(code), flush=True)
        if code != 0: raise RuntimeError('Compile failed; see ' + str(log))
    hashed = [dispatcher, HERE / 'DesignWallpaperSmoke.TrainingScene.cs', HERE / 'dispatcher.patch']
    hashed.extend(CANDIDATE / name for name in ['XgBoard.cs', 'XgConceptTransition.cs', 'XgBrainWallpaper.cs', 'XgWallpaperConcepts.cs', 'XgWallpaperTraining.cs'])
    (HERE / 'hashes.json').write_text(json.dumps({relative(item): hashlib.sha256(item.read_bytes()).hexdigest().upper()
        for item in hashed}, indent=2), encoding='utf-8')
    print('All candidate assemblies compile; no Unity process was launched.', flush=True)
    return 0

if __name__ == '__main__':
    try: sys.exit(main())
    except Exception as error:
        print('CAUGHT_COMPILE_FAILURE: ' + str(error), file=sys.stderr)
        sys.exit(1)
