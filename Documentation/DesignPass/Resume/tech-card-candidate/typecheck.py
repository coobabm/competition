"""Use Unity Roslyn without starting Unity; all outputs remain in candidate-only storage."""
from pathlib import Path
import re
import subprocess

root = Path(__file__).resolve().parents[4]
candidate = Path(__file__).resolve().parent
out = candidate / 'compile'
out.mkdir(exist_ok=True)
unity = Path('C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data')
for assembly in ['Assembly-CSharp', 'Assembly-CSharp-Editor']:
    text = (root / 'Library/Bee/artifacts/1900b0aE.dag' / (assembly + '.rsp')).read_text(encoding='utf-8-sig')
    text = re.sub(r'(?m)^-out:.*$', '-out:"' + (out / (assembly + '.dll')).as_posix() + '"', text)
    text = re.sub(r'(?m)^-refout:.*$', '-refout:"' + (out / (assembly + '.ref.dll')).as_posix() + '"', text)
    additions = []
    if assembly == 'Assembly-CSharp':
        source = 'Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs'
        text = text.replace('"' + source + '"', '"' + (candidate / 'XgTechTreePage.cs').as_posix() + '"')
        additions = [(candidate / name).as_posix() for name in ['XgTechTreePage.Cards.cs', 'XgTechCardCopy.cs']]
        additions.append('Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.Growth.cs')
    else:
        text = text.replace('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.ref.dll', (out / 'Assembly-CSharp.ref.dll').as_posix())
        text = text.replace('"Assets/LocalBuild/Editor/DesignWallpaperSmoke.cs"', '"' + (candidate / 'DesignWallpaperSmoke.cs').as_posix() + '"')
        additions = ['Assets/LocalBuild/Editor/DesignWallpaperSmoke.Growth.cs', 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.GrowthGuide.cs']
        checks = candidate / 'DesignWallpaperSmoke.TechCards.cs'
        if checks.exists(): additions.append(checks.as_posix())
    for addition in additions:
        if addition not in text: text += '\n"' + addition + '"\n'
    response = out / (assembly + '.rsp'); response.write_text(text, encoding='utf-8')
    result = subprocess.run([str(unity / 'NetCoreRuntime/dotnet.exe'), str(unity / 'DotNetSdkRoslyn/csc.dll'), '@' + str(response)], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (out / (assembly + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(assembly, result.returncode, flush=True)
    if result.returncode:
        print(result.stdout + result.stderr); raise SystemExit(result.returncode)
