from pathlib import Path

root = Path.cwd()
dest = 'Documentation/DesignPass/Resume/CorrectionWriteCandidates'
out = 'LocalVerification/DesignPass/CorrectionWriteCompile'
core = (root / 'Library/Bee/artifacts/1900b0aE.dag/LingGuangV05.XingGuang.Core.rsp').read_text(encoding='utf-8-sig')
lines = core.splitlines()
for i, line in enumerate(lines):
    if line.startswith('-out:'):
        lines[i] = f'-out:"{out}/LingGuangV05.XingGuang.Core.dll"'
    elif line.startswith('-refout:'):
        lines[i] = f'-refout:"{out}/LingGuangV05.XingGuang.Core.ref.dll"'
    else:
        for name in ['XgBoard.cs', 'XgSim.Board.cs', 'XgSim.AnswerTrace.cs', 'XgSim.cs']:
            if line == f'"Assets/LingGuangV05/XingGuang/Core/{name}"':
                lines[i] = f'"{dest}/{name}"'
(root / out / 'core.rsp').write_text('\n'.join(lines) + '\n', encoding='utf-8')
runner = (root / 'LocalVerification/DesignPass/AnswerTraceCompile/runner-managed.rsp').read_text(encoding='utf-8-sig')
runner = runner.replace('LocalVerification/DesignPass/AnswerTraceCompile/LingGuangV05.XingGuang.Core.dll', f'{out}/LingGuangV05.XingGuang.Core.dll')
runner = runner.replace('LocalVerification/DesignPass/AnswerTraceCompile/AnswerTraceRunner.dll', f'{out}/AnswerTraceRunner.dll')
runner = runner.replace('Documentation/DesignPass/Resume/AnswerTraceCandidates/AnswerTraceChecks.cs', f'{dest}/AnswerTraceChecks.cs')
runner += '\n"Assets/LocalBuild/Editor/CorrectionDropoutChecks.cs"\n'
(root / out / 'runner.rsp').write_text(runner, encoding='utf-8')
for source, target in [
    ('LocalVerification/DesignPass/AnswerTraceCompile/AnswerTraceRunner.runtimeconfig.json', 'AnswerTraceRunner.runtimeconfig.json'),
    ('Library/ScriptAssemblies/LingGuangV05.Core.dll', 'LingGuangV05.Core.dll'),
]:
    (root / out / target).write_bytes((root / source).read_bytes())
print('Prepared isolated production-core and managed runner compilation.')
