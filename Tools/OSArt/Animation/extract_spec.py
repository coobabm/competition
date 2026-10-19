#!/usr/bin/env python3
"""Extract the animation data contract of every .anim / .controller under a root folder into one JSON spec.

usage: extract_spec.py ROOT [OUT.json]
  ROOT : folder holding the originals (e.g. the backup copy of .../LingGuangV05/DesktopBridge/OSArt/Animations)
  OUT  : defaults to spec/os_anim_spec.json next to this script
.meta files are only READ (to record each clip's GUID); they are never written.
"""
import json
import os
import re
import sys

import animlib


def guid_of(path):
    meta = path + '.meta'
    if not os.path.exists(meta):
        return None
    m = re.search(r'^guid: (\w+)', open(meta, encoding='utf-8').read(), re.M)
    return m.group(1) if m else None


def extract(root, meta_root=None):
    meta_root = meta_root or root
    spec = {'format': 1, 'clips': {}, 'controllers': {}}
    for d, _, fs in sorted(os.walk(root)):
        for f in sorted(fs):
            p = os.path.join(d, f)
            rel = os.path.relpath(p, root).replace(os.sep, '/')
            mp = os.path.join(meta_root, rel)
            if f.endswith('.anim'):
                spec['clips'][rel] = animlib.parse_clip(p, guid_of(mp))
            elif f.endswith('.controller'):
                c = animlib.parse_controller(p)
                c['guid'] = guid_of(mp)
                spec['controllers'][rel] = c
    return spec


if __name__ == '__main__':
    root = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                                             'spec', 'os_anim_spec.json')
    meta_root = sys.argv[3] if len(sys.argv) > 3 else None
    spec = extract(root, meta_root)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', encoding='utf-8') as fh:
        json.dump(spec, fh, indent=1, ensure_ascii=False, sort_keys=False)
    print('clips: %d  controllers: %d -> %s' % (len(spec['clips']), len(spec['controllers']), out))
