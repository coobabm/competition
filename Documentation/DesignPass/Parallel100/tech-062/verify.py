"""Read production; write/run only this task's isolated arithmetic harness. No Unity/save access."""
from pathlib import Path
import difflib, hashlib, json, subprocess, sys, os

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
manifest = json.loads((HERE/'source-sha256.json').read_text(encoding='utf-8'))
target = next(iter(manifest))
proposed = (HERE/'proposed-XgTechTreePage.cs').read_text(encoding='utf-8')
original = (ROOT/target).read_text(encoding='utf-8-sig')

def section(text, start, end):
    return text[text.index(start):text.index(end, text.index(start))]

def check(condition, detail):
    if not condition:
        raise AssertionError(detail)

drift = [p for p, sha in manifest.items() if hashlib.sha256((ROOT/p).read_bytes()).hexdigest() != sha]
print('Dependency drift: ' + (', '.join(drift) if drift else 'none'), flush=True)
check(not drift, 'Review changed dependencies; do not silently apply stale patch')
expected = ''.join(difflib.unified_diff(original.splitlines(True), proposed.splitlines(True), fromfile='a/'+target, tofile='b/'+target, n=5))
patch = (HERE/'changes.patch').read_text(encoding='utf-8')
check(patch[patch.index('--- a/'):]==expected, 'Patch and tested proposal differ')
apply_check = subprocess.run(['git', 'apply', '--check', '--whitespace=error-all', str(HERE/'changes.patch')],
                             cwd=ROOT, text=True, encoding='utf-8', errors='replace', stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
check(apply_check.returncode == 0, 'Read-only patch application check failed: '+apply_check.stdout)
print('Patch/source parity and read-only git apply check: passed.', flush=True)
# Protect copy and simulation behavior. We test the actual proposed source, not a duplicate layout model.
for start, end in [('        void FillTooltip(', '        XgTechIcon.Kind IconOfShown('),
                   ('        public void Click(', '        // ───────────── tooltip'),
                   ('        string Ladder(', '        // ───────────── navigation')]:
    check(section(original, start, end) == section(proposed, start, end), 'Out-of-scope change: '+start)
for needed in ['TextWrappingModes.Normal;', 'nameLayout.minWidth = 0;',
               'tipName.GetPreferredValues(', 'tipCost.GetPreferredValues(',
               'float h = Mathf.Min(tipContent.rect.height, bounds.height - 8);',
               'tipScroll.movementType = ScrollRect.MovementType.Clamped;',
               'clip.gameObject.AddComponent<RectMask2D>();',
               'tipScroll.verticalScrollbar = scrollbar;',
               'Time.unscaledTime + .35f', 'EventTriggerType.PointerEnter',
               'tooltip.anchoredPosition = new Vector2(x, y) - root.rect.center;',
               'Screen.safeArea', 'tipScroll.verticalNormalizedPosition = 1;']:
    check(needed in proposed, 'Missing layout contract: '+needed)
check('tooltip.DOShake' not in proposed, 'Tooltip animation can cross clamped bounds')
check('已满级 · MAX' in proposed and 'body.Append(XgTechTreeCopy.Note(shownNode, Sim))' in proposed,
      'Missing max-level/copy source')
print('Static scope/layout contract checks: passed (not a Unity compile or render).', flush=True)

helper = section(proposed, '    internal static class XgTechTooltipGeometry',
                 '    /// <summary>A label that keeps')
tests = r'''
internal static class Program
{
    static int count;
    static void Check(bool ok, string message) { count++; if (!ok) throw new Exception(message); }
    static void Near(float a, float b, string m) => Check(Math.Abs(a-b) < .002f, m);
    static void Main()
    {
        Near(XgTechTooltipGeometry.Width(1000, 312), 312, "normal width");
        Near(XgTechTooltipGeometry.Width(240, 312), 232, "narrow page must reserve both margins");
        Near(XgTechTooltipGeometry.Width(320, 312), 312, "exact-fit width");
        Near(XgTechTooltipGeometry.Width(9, 312), 1, "small positive bounds");
        XgTechTooltipGeometry.Place(-400,-300,400,300,312,100,0,0,-90,out var x,out var y);
        Near(x,0,"centred node"); Near(y,110,"prefer above with 10-unit gap");
        XgTechTooltipGeometry.Place(-400,-300,400,300,312,100,0,250,160,out x,out y);
        Near(y,156,"fall below when top has no room");
        // Independent containment properties, off-page nodes, every edge/corner,
        // full-height content, fractional DPI-like dimensions, non-centred root pivots.
        var random = new Random(62013);
        for (int i=0; i<4000; ++i)
        {
            float left=(float)(random.NextDouble()*2000-1000);
            float bottom=(float)(random.NextDouble()*2000-1000);
            float bw=(float)(9+random.NextDouble()*1591), bh=(float)(9+random.NextDouble()*1191);
            float w=XgTechTooltipGeometry.Width(bw,312);
            float h=i%3==0 ? bh-8 : (float)(1+random.NextDouble()*(bh-9));
            float nx=left+(float)(random.NextDouble()*bw*3-bw);
            float nt=bottom+(float)(random.NextDouble()*bh*3-bh);
            XgTechTooltipGeometry.Place(left,bottom,left+bw,bottom+bh,w,h,nx,nt,nt-118,out x,out y);
            Check(w>0 && w<=bw-8+.001f,"width fits");
            Check(x-w/2>=left+4-.002f && x+w/2<=left+bw-4+.002f,"horizontal containment");
            Check(y-h>=bottom+4-.002f && y<=bottom+bh-4+.002f,"vertical containment");
            const float dx=137.25f,dy=-73.5f;
            XgTechTooltipGeometry.Place(left+dx,bottom+dy,left+bw+dx,bottom+bh+dy,w,h,nx+dx,nt+dy,nt-118+dy,out var x2,out var y2);
            Near(x2,x+dx,"translation x"); Near(y2,y+dy,"translation y");
        }
        Console.WriteLine($"Executed C# geometry assertions: {count} passed.");
        Console.WriteLine("Unity compile: NOT RUN. TMP glyph layout/input: NOT RUN. Visual acceptance: NOT RUN.");
    }
}
'''
work = HERE/'verification-work'
work.mkdir(exist_ok=True)
(work/'Program.cs').write_text('using System;\n'+helper+tests, encoding='utf-8')
version = subprocess.check_output(['dotnet', '--version'], text=True).strip()
major = int(version.split('.')[0])
check(major >= 8, 'Requires .NET SDK 8 or later')
(work/'Geometry.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net{major}.0</TargetFramework><EnableDefaultCompileItems>true</EnableDefaultCompileItems></PropertyGroup></Project>\n', encoding='utf-8')
(work/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>\n', encoding='utf-8')
env = dict(os.environ, DOTNET_CLI_HOME=str(work), DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1')
result = subprocess.run(['dotnet', 'run', '--project', str(work/'Geometry.csproj'), '--configuration', 'Release'],
                        cwd=work, env=env, text=True, encoding='utf-8', errors='replace', stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
print(result.stdout, end='')
(HERE/'verification-output.txt').write_text('Dependency drift: none\nPatch/source parity and read-only git apply check: passed\nStatic scope/layout contract checks: passed\n'+result.stdout, encoding='utf-8')
sys.exit(result.returncode)
