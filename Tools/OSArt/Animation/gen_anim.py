#!/usr/bin/env python3
"""Re-emit every .anim / .controller from the JSON spec with our own curves and our own controller layout.

usage: gen_anim.py [--spec spec/os_anim_spec.json] OUT_ROOT
  OUT_ROOT : folder to write into, same relative paths as in the spec.  For the project this is
             Assets/LingGuangV05/DesktopBridge/OSArt/Animations (files are overwritten in place, .meta files are never touched).
"""
import argparse
import json
import os

import animlib

HERE = os.path.dirname(os.path.abspath(__file__))


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--spec', default=os.path.join(HERE, 'spec', 'os_anim_spec.json'))
    ap.add_argument('out_root')
    a = ap.parse_args()
    spec = json.load(open(a.spec, encoding='utf-8'))
    for rel, clip in spec['clips'].items():
        write(os.path.join(a.out_root, rel), animlib.emit_clip(clip))
    for rel, ctrl in spec['controllers'].items():
        write(os.path.join(a.out_root, rel), animlib.emit_controller(ctrl, rel))
    print('wrote %d clips, %d controllers to %s' % (len(spec['clips']), len(spec['controllers']), a.out_root))


if __name__ == '__main__':
    main()
