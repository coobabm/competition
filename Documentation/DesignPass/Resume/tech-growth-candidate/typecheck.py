"""Use Unity's bundled C# compiler only; never launch Unity or write Library outputs."""
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parents[4]
candidate = Path(__file__).resolve().parent
out = root / 'LocalVerification/DesignPass/GrowthCompile'
out.mkdir(parents=True, exist_ok=True)
unity = Path('C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data')
deployed = '--deployed' in sys.argv

for assembly in ['Assembly-CSharp', 'Assembly-CSharp-Editor']:
    original = root / 'Library/Bee/artifacts/1900b0aE.dag' / (assembly + '.rsp')
    text = original.read_text(encoding='utf-8-sig')
    text = re.sub(r'(?m)^-out:.*$', '-out:"' + (out / (assembly + '.dll')).as_posix() + '"', text)
    text = re.sub(r'(?m)^-refout:.*$', '-refout:"' + (out / (assembly + '.ref.dll')).as_posix() + '"', text)
    if assembly == 'Assembly-CSharp':
        main = 'Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs'
        growth = 'Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.Growth.cs'
        if not deployed:
            text = text.replace('"' + main + '"', '"' + (candidate / 'XgTechTreePage.cs').as_posix() + '"')
            growth = (candidate / 'XgTechTreePage.Growth.cs').as_posix()
        if growth not in text:
            text += '\n"' + growth + '"\n'
    else:
        text = text.replace('Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.ref.dll', (out / 'Assembly-CSharp.ref.dll').as_posix())
        growth = 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.Growth.cs' if deployed else (candidate / 'DesignWallpaperSmoke.Growth.cs').as_posix()
        if growth not in text:
            text += '\n"' + growth + '"\n'
        guide = 'Assets/LocalBuild/Editor/DesignWallpaperSmoke.GrowthGuide.cs'
        if deployed and (root / guide).exists() and guide not in text:
            text += '\n"' + guide + '"\n'
    response = out / (assembly + '.rsp')
    response.write_text(text, encoding='utf-8')
    result = subprocess.run([str(unity / 'NetCoreRuntime/dotnet.exe'), str(unity / 'DotNetSdkRoslyn/csc.dll'), '@' + str(response)],
                            cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (out / (assembly + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(assembly, result.returncode, flush=True)
    if result.returncode:
        print(result.stdout + result.stderr)
        raise SystemExit(result.returncode)
