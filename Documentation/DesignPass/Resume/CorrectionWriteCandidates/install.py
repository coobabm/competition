from pathlib import Path
import difflib
import hashlib
import json

root = Path.cwd()
candidate_dir = root / 'Documentation/DesignPass/Resume/CorrectionWriteCandidates'
manifest = json.loads((candidate_dir / 'hashes.json').read_text(encoding='utf-8'))
pending = []
patches = []
for entry in manifest:
    source = root / entry['path']
    candidate = candidate_dir / source.name
    before = source.read_bytes()
    after = candidate.read_bytes()
    assert hashlib.sha256(before).hexdigest().upper() == entry['before'], 'Source changed: ' + str(source)
    assert hashlib.sha256(after).hexdigest().upper() == entry['candidate'], 'Candidate changed: ' + str(candidate)
    patches.extend(difflib.unified_diff(before.decode('utf-8-sig').splitlines(True), after.decode('utf-8-sig').splitlines(True),
        fromfile='a/' + entry['path'], tofile='b/' + entry['path']))
    pending.append((source, after, entry['candidate']))
(candidate_dir / 'changes.patch').write_text(''.join(patches), encoding='utf-8', newline='\n')
for source, after, digest in pending:
    source.write_bytes(after)
    assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == digest
    print(source.relative_to(root).as_posix() + ' ' + digest)
print('INSTALLED_AND_FROZEN: five guarded files; no Unity process launched.')
