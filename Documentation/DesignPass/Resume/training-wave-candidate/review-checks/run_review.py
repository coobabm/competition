"""Bounded, hidden, protected pure-Core review. No Unity, real saves or apphost exe."""
from pathlib import Path
import json
import os
import re
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
OUT = ROOT / 'LocalVerification/DesignPass/TrainingWaveReview'
DOTNET = Path('C:/Program Files/dotnet/dotnet.exe')

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    environment = os.environ.copy()
    environment.update(DOTNET_CLI_HOME=str(OUT / 'dotnet-home'), DOTNET_CLI_TELEMETRY_OPTOUT='1',
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1')
    results = []
    def run(name, args, expected=0, seconds=60):
        logfile = OUT / (name + '.log')
        with logfile.open('wb') as stream:
            try:
                process = subprocess.run([str(DOTNET), *args], cwd=HERE, env=environment,
                    stdout=stream, stderr=subprocess.STDOUT, timeout=seconds,
                    creationflags=subprocess.CREATE_NO_WINDOW)
                code = process.returncode
            except subprocess.TimeoutExpired:
                code = 124
        results.append({'name': name, 'exit_code': code, 'expected': expected, 'log': str(logfile)})
        (OUT / 'exit-codes.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
        print(f'{name}: exit={code}, expected={expected}', flush=True)
        if code != expected:
            raise RuntimeError(f'{name} failed; see {logfile}')
        return logfile.read_text(encoding='utf-8-sig', errors='replace')

    digests = []
    for variant, option in [('Baseline', 'false'), ('Candidate', 'true')]:
        run('build-' + variant.lower(), ['build', 'TrainingWaveChecks.csproj', '-c', 'Release',
            '--nologo', '--verbosity', 'minimal', '--disable-build-servers',
            '--configfile', 'NuGet.Config', '-p:TrainingCandidate=' + option])
        assembly = HERE / 'bin' / variant / 'net10.0/TrainingWaveChecks.dll'
        # Prove the exact binary handles its deliberate failure before its real test workload.
        failure = run('protected-failure-' + variant.lower(), [str(assembly), '--force-fail'], expected=1, seconds=15)
        if 'CAUGHT_TEST_FAILURE:' not in failure or 'INTENTIONAL_PROTECTED_FAILURE' not in failure:
            raise RuntimeError('The exact test binary did not demonstrate its protected failure handler')
        output = run('run-' + variant.lower(), [str(assembly)], seconds=45)
        if 'TRAINING_WAVE_CORE_PASS' not in output:
            raise RuntimeError('Missing explicit test completion marker')
        match = re.search(r'^EQUIVALENCE_SHA256=([A-F0-9]+)$', output, re.MULTILINE)
        if not match:
            raise RuntimeError('Missing state/result/wallet equivalence digest')
        digests.append(match.group(1))
    if digests[0] != digests[1]:
        raise RuntimeError('Production and subscribed-candidate equivalence digests differ')
    (OUT / 'result.txt').write_text('PASS\nEQUIVALENCE_SHA256=' + digests[0] + '\n', encoding='utf-8')
    print('PASS: production and subscribed candidate have identical state/result/wallet digests', flush=True)
    return 0

if __name__ == '__main__':
    try:
        sys.exit(main())
    except Exception as error:
        OUT.mkdir(parents=True, exist_ok=True)
        (OUT / 'result.txt').write_text('FAIL: ' + str(error) + '\n', encoding='utf-8')
        print('CAUGHT_REVIEW_FAILURE: ' + str(error), file=sys.stderr)
        sys.exit(1)
