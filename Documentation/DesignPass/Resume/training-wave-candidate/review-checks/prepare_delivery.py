from pathlib import Path
import difflib
import hashlib
import json

candidate = Path(__file__).resolve().parent.parent
baseline = candidate / 'review-baseline'
transition = candidate / 'XgConceptTransition.cs'
original_comment = '    /// <summary>Transient evidence of an actual creation or R4 removal, never a saved concept.</summary>'
source = transition.read_text(encoding='utf-8-sig')
start = source.index('    /// <summary>')
end = source.index('    public sealed class XgConceptTransition')
baseline_transition = source[:start] + original_comment + '\n' + source[end:]
(baseline / transition.name).write_text(baseline_transition, encoding='utf-8', newline='\n')

patches = []
manifest = {'scope': 'Review additions only; baseline is the original Assets-outside candidate.', 'files': {}}
for name in ['XgBoard.cs', 'XgWallpaperTraining.cs', 'XgConceptTransition.cs']:
    before = (baseline / name).read_bytes()
    after = (candidate / name).read_bytes()
    relative = 'Documentation/DesignPass/Resume/training-wave-candidate/' + name
    patches.extend(difflib.unified_diff(before.decode('utf-8-sig').splitlines(keepends=True),
        after.decode('utf-8-sig').splitlines(keepends=True), fromfile='a/' + relative, tofile='b/' + relative))
    manifest['files'][name] = {'baseline': hashlib.sha256(before).hexdigest().upper(),
        'candidate': hashlib.sha256(after).hexdigest().upper()}
(candidate / 'review.patch').write_bytes(''.join(patches).encode('utf-8'))
(candidate / 'review-hashes.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(json.dumps(manifest, indent=2))
