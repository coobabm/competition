#!/usr/bin/env python3
"""Self check for generated animation assets.

usage: selfcheck.py ORIG_ROOT NEW_ROOT [PROJECT_ASSETS_DIR]
  ORIG_ROOT : copy of the original .anim/.controller files (backup)
  NEW_ROOT  : folder with the regenerated files
  PROJECT_ASSETS_DIR : (optional) Assets folder, scanned for Animator.Play/CrossFade/SetTrigger/SetBool names

Checks
  * every file re-parses with our parser; the parsed spec of the new file equals the spec of the original on the
    whole contract: names, lengths, loop/wrap/sample rate, bindings (path/attribute/class/script/type), start and end
    values, waypoint timing, events; controllers: layers, parameters, states, motions, transitions
  * the new clip's genericBindings equal the original's as a set (path/attribute hashes, script, typeID, customType)
  * m_EditorCurves is consistent with m_FloatCurves / m_EulerCurves / m_ScaleCurves
  * curves are numerically well behaved (keys inside the clip length, values stay inside the waypoint range)
  * controller object references resolve, default state exists, motion GUIDs exist among the clips
  * code string names still exist
"""
import glob
import json
import os
import re
import sys

import animlib
from animlib import f32, num, same, crc
from unityyaml import parse_file


NOTES = []


def dangling_guids(path, guid_to_clip):
    out = set()
    for d in parse_file(path):
        if d['type'] == 'AnimatorState':
            m = d['data']['m_Motion']
            if int(m['fileID']) and m.get('guid') not in guid_to_clip:
                out.add(m.get('guid'))
    return out


class Report:
    def __init__(self):
        self.errors = []
        self.n = 0

    def check(self, cond, msg):
        self.n += 1
        if not cond:
            self.errors.append(msg)


def hermite(keys, t):
    """Evaluate a parsed float curve (list of dict(time,value,in,out)) at t, honouring Unity's cubic Hermite."""
    if t <= keys[0][0]:
        return keys[0][1]
    for a, b in zip(keys, keys[1:]):
        if a[0] <= t <= b[0]:
            if b[0] == a[0]:
                return b[1]
            if a[3] == animlib.INF:
                return a[1]
            return animlib.Seg(a[0], a[1], a[3], b[0], b[1], b[2]).eval(t)
    return keys[-1][1]


def norm_curve(cv):
    out = dict(cv)
    for k in ('bindIndex', 'origKeys'):
        out.pop(k, None)
    return out


def key_of(cv):
    s = cv.get('script')
    return (cv['type'], cv['path'], cv['attribute'], cv['classID'], json.dumps(s, sort_keys=True))


def wp_equal(a, b):
    return len(a) == len(b) and all(same(x[0], y[0], 1e-6) and same(x[1], y[1], 1e-5) for x, y in zip(a, b))


def vec_equal(a, b):
    return all(same(x, y, 1e-5) for x, y in zip(a, b))


def compare_clip(rep, rel, old, new):
    p = rel
    for k in ('name', 'loop', 'wrapMode', 'legacy', 'flags', 'bounds', 'clipSettings', 'events'):
        rep.check(old[k] == new[k], '%s: %s differs' % (p, k))
    rep.check(same(old['length'], new['length']), '%s: length %s != %s' % (p, old['length'], new['length']))
    rep.check(same(old['sampleRate'], new['sampleRate']), '%s: sample rate' % p)
    ok = {key_of(c): c for c in old['curves']}
    nk = {key_of(c): c for c in new['curves']}
    rep.check(len(ok) == len(old['curves']), '%s: duplicate bindings in original' % p)
    rep.check(set(ok) == set(nk), '%s: binding set differs: only-old=%s only-new=%s' % (
        p, sorted(set(ok) - set(nk)), sorted(set(nk) - set(ok))))
    for k, o in ok.items():
        n = nk.get(k)
        if not n:
            continue
        rep.check(o['typeID'] == n['typeID'] and o['customType'] == n['customType'], '%s: %s type ids' % (p, k))
        if o['type'] in animlib.VEC_INFO:
            rep.check(vec_equal(o['start'], n['start']) and vec_equal(o['end'], n['end']),
                      '%s: %s start/end' % (p, k))
            rep.check(all(wp_equal(a, b) for a, b in zip(o['axes'], n['axes'])), '%s: %s waypoints' % (p, k))
        elif o['type'] == 'float':
            rep.check(same(o['start'], n['start'], 1e-5) and same(o['end'], n['end'], 1e-5),
                      '%s: %s start/end %s,%s vs %s,%s' % (p, k, o['start'], o['end'], n['start'], n['end']))
            rep.check(wp_equal(o['waypoints'], n['waypoints']), '%s: %s waypoints' % (p, k))
            rep.check(o['stepped'] == n['stepped'], '%s: %s stepped' % (p, k))
        else:
            rep.check(o['keys'] == n['keys'], '%s: %s sprite keys' % (p, k))
        rep.check(same(o['startTime'], n['startTime']) and same(o['endTime'], n['endTime']),
                  '%s: %s time range' % (p, k))


def check_clip_file(rep, rel, orig_path, new_path):
    """Structure checks that need the raw files."""
    oc = parse_file(orig_path)[0]['data']
    nc = parse_file(new_path)[0]['data']
    # bindings as a set
    def bset(c):
        return sorted((g['path'], g['attribute'], json.dumps(g['script'], sort_keys=True), g['typeID'],
                       g['customType'], g['isPPtrCurve']) for g in c['m_ClipBindingConstant']['genericBindings'])
    rep.check(bset(oc) == bset(nc), '%s: genericBindings differ' % rel)
    rep.check(oc['m_ClipBindingConstant']['pptrCurveMapping'] == nc['m_ClipBindingConstant']['pptrCurveMapping'],
              '%s: pptrCurveMapping' % rel)
    # every binding corresponds to a curve (count)
    ncurves = (len(nc['m_FloatCurves']) + len(nc['m_EulerCurves']) + len(nc['m_ScaleCurves']) +
               len(nc['m_PositionCurves']) + len(nc['m_PPtrCurves']))
    rep.check(len(nc['m_ClipBindingConstant']['genericBindings']) == ncurves, '%s: binding count' % rel)
    # editor curves consistent
    ed = {(e['path'], e['attribute'], e['classID']): e['curve']['m_Curve'] for e in nc['m_EditorCurves']}
    for e in nc['m_FloatCurves']:
        ks = ed.get((e['path'], e['attribute'], e['classID']))
        rep.check(ks == e['curve']['m_Curve'], '%s: editor curve != float curve for %s/%s' % (rel, e['path'], e['attribute']))
    for blk, vi in (('m_EulerCurves', 'euler'), ('m_ScaleCurves', 'scale'), ('m_PositionCurves', 'position')):
        for e in nc[blk]:
            for ax in animlib.AXES:
                ks = ed.get((e['path'], '%s.%s' % (animlib.VEC_INFO[vi]['edit'], ax), None)) if False else None
                found = [v for (pth, at, cl), v in ed.items()
                         if pth == e['path'] and at == '%s.%s' % (animlib.VEC_INFO[vi]['edit'], ax)]
                rep.check(len(found) == 1, '%s: editor curve missing %s %s.%s' % (rel, vi, e['path'], ax))
                if found:
                    f = found[0]
                    v3 = e['curve']['m_Curve']
                    rep.check(len(f) == len(v3) and all(
                        f32(num(a['value'])) == f32(num(b['value'][ax])) and f32(num(a['time'])) == f32(num(b['time']))
                        and f32(num(a['inSlope'])) == f32(num(b['inSlope'][ax]))
                        and f32(num(a['outSlope'])) == f32(num(b['outSlope'][ax])) for a, b in zip(f, v3)),
                        '%s: editor %s.%s differs from vector curve' % (rel, e['path'], ax))
    # numeric behaviour of every float curve
    length = num(nc['m_AnimationClipSettings']['m_StopTime'])
    for e in nc['m_FloatCurves']:
        ks = [(num(k['time']), num(k['value']), num(k['inSlope']), num(k['outSlope'])) for k in e['curve']['m_Curve']]
        ts = [k[0] for k in ks]
        rep.check(ts == sorted(ts) and len(set(ts)) == len(ts), '%s: key times not strictly increasing' % rel)
        rep.check(max(ts) <= length + 1e-5, '%s: key beyond clip length' % rel)
        stepped = any(k[2] == animlib.INF for k in ks)
        if not stepped and len(ks) > 1:
            lo, hi = min(k[1] for k in ks), max(k[1] for k in ks)
            n = 200
            bad = False
            for i in range(n + 1):
                t = ts[0] + (ts[-1] - ts[0]) * i / n
                v = hermite(ks, t)
                if v < lo - 1e-4 * max(1, abs(hi - lo)) or v > hi + 1e-4 * max(1, abs(hi - lo)):
                    bad = True
            rep.check(not bad, '%s: %s/%s leaves its range' % (rel, e['path'], e['attribute']))
        ok = [(num(k['time']), num(k['value'])) for k in
              [x for x in oc['m_FloatCurves'] if x['path'] == e['path'] and x['attribute'] == e['attribute']][0]['curve']['m_Curve']]
        rep.check(same(ks[0][1], ok[0][1], 1e-5) and same(ks[-1][1], ok[-1][1], 1e-5),
                  '%s: %s/%s start/end value differ from original' % (rel, e['path'], e['attribute']))
    for blk in ('m_EulerCurves', 'm_ScaleCurves', 'm_PositionCurves'):
        for e in nc[blk]:
            ts = [num(k['time']) for k in e['curve']['m_Curve']]
            rep.check(ts == sorted(ts) and len(set(ts)) == len(ts) and max(ts) <= length + 1e-5, '%s: %s times' % (rel, blk))


def compare_controller(rep, rel, old, new):
    p = rel
    o = dict(old); n = dict(new)
    o.pop('droppedOrphans', None); n.pop('droppedOrphans', None)
    o.pop('guid', None); n.pop('guid', None)
    rep.check(o == n, '%s: controller contract differs' % p)
    if o != n:
        for k in o:
            if o[k] != n.get(k):
                rep.errors.append('   %s: differing key %s' % (p, k))


def check_controller_file(rep, rel, path, guid_to_clip, dangling_ok=()):
    docs = parse_file(path)
    ids = [d['fileID'] for d in docs]
    rep.check(len(ids) == len(set(ids)), '%s: duplicate fileIDs' % rel)
    rep.check(ids == sorted(ids), '%s: docs not sorted by fileID' % rel)
    idset = set(ids) | {0}
    refs = []
    def walk(x):
        if isinstance(x, dict):
            if 'fileID' in x and 'guid' not in x:
                refs.append(int(x['fileID']))
            for v in x.values():
                walk(v)
        elif isinstance(x, list):
            for v in x:
                walk(v)
    for d in docs:
        walk(d['data'])
        if d['type'] == 'AnimatorState':
            m = d['data']['m_Motion']
            if int(m['fileID']):
                rep.check((m.get('guid') in guid_to_clip or m.get('guid') in dangling_ok) and m['fileID'] == '7400000',
                          '%s: motion guid %s not a clip' % (rel, m.get('guid')))
                if m.get('guid') not in guid_to_clip:
                    NOTES.append('%s: state "%s" points at clip GUID %s that does not exist in the project '
                                 '(already dangling in the original; kept as is)' % (rel, d['data']['m_Name'], m.get('guid')))
    rep.check(all(r in idset for r in refs), '%s: dangling fileID reference' % rel)
    ctrl = [d for d in docs if d['type'] == 'AnimatorController']
    rep.check(len(ctrl) == 1 and ctrl[0]['fileID'] == 9100000, '%s: controller root' % rel)
    for d in docs:
        if d['type'] == 'AnimatorStateMachine':
            if d['data']['m_ChildStates']:
                dflt = int(d['data']['m_DefaultState']['fileID'])
                rep.check(dflt in ids, '%s: default state missing' % rel)
    text = open(path, encoding='utf-8').read()
    rep.check(text.endswith('\n') and text.startswith('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'), '%s: header/EOL' % rel)


CALL_RE = re.compile(r'\.(Play|CrossFade|CrossFadeInFixedTime|PlayInFixedTime|SetTrigger|ResetTrigger|SetBool|'
                     r'SetInteger|SetFloat|GetBool|GetInteger|GetFloat|StringToHash)\(\s*"([^"]+)"')


def code_names(assets_dir):
    found = {}
    for p in glob.glob(os.path.join(assets_dir, '**', '*.cs'), recursive=True):
        try:
            t = open(p, encoding='utf-8', errors='ignore').read()
        except OSError:
            continue
        for m in CALL_RE.finditer(t):
            found.setdefault((m.group(1), m.group(2)), set()).add(os.path.basename(p))
    return found


def main():
    orig_root, new_root = sys.argv[1], sys.argv[2]
    assets = sys.argv[3] if len(sys.argv) > 3 else None
    rep = Report()
    # GUID of every clip (meta files of the project, read only)
    proj_anim = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'Assets',
                             'LingGuangV05/DesktopBridge/OSArt', 'Animations')
    guid_to_clip = {}
    for p in glob.glob(os.path.join(proj_anim, '**', '*.anim.meta'), recursive=True):
        m = re.search(r'^guid: (\w+)', open(p, encoding='utf-8').read(), re.M)
        guid_to_clip[m.group(1)] = p
    nclip = nctrl = 0
    old_states, new_states = set(), set()
    for d, _, fs in sorted(os.walk(orig_root)):
        for f in sorted(fs):
            op = os.path.join(d, f)
            rel = os.path.relpath(op, orig_root)
            np_ = os.path.join(new_root, rel)
            if not os.path.exists(np_):
                rep.check(False, '%s: missing in new root' % rel)
                continue
            if f.endswith('.anim'):
                nclip += 1
                old = animlib.parse_clip(op)
                new = animlib.parse_clip(np_)
                compare_clip(rep, rel, old, new)
                check_clip_file(rep, rel, op, np_)
                text = open(np_, encoding='utf-8').read()
                rep.check(text.endswith('\n') and text.startswith('%YAML 1.1\n'), '%s: header/EOL' % rel)
            elif f.endswith('.controller'):
                nctrl += 1
                old = animlib.parse_controller(op)
                new = animlib.parse_controller(np_)
                compare_controller(rep, rel, old, new)
                check_controller_file(rep, rel, np_, guid_to_clip, dangling_guids(op, guid_to_clip))
                for src, dst in ((old, old_states), (new, new_states)):
                    for l in src['layers']:
                        for s in l['stateMachine']['states']:
                            dst.add(s['name'])
                        dst.add(l['name'])
                    for pr in src['parameters']:
                        dst.add(pr['name'])
    print('clips checked: %d, controllers checked: %d, assertions: %d' % (nclip, nctrl, rep.n))
    if assets:
        names = code_names(assets)
        animcalls = [(c, n, f) for (c, n), f in names.items()
                     if c in ('Play', 'CrossFade', 'CrossFadeInFixedTime', 'PlayInFixedTime', 'SetTrigger', 'ResetTrigger',
                              'SetBool', 'SetInteger', 'StringToHash')
                     or (c in ('SetFloat', 'GetFloat', 'GetBool', 'GetInteger') and n in new_states)]
        missing_new, missing_old = [], []
        for c, n, f in sorted(animcalls):
            nm = n.split('.', 1)[1] if n.startswith('Base Layer.') else n
            if nm not in old_states:
                missing_old.append((c, n))   # not an animator name originally (e.g. a shader/other API)
            elif nm not in new_states:
                missing_new.append((c, n, sorted(f)))
        print('code animator-style string calls: %d (%d resolve to controller names, %d are other APIs)' % (
            len(animcalls), len(animcalls) - len(missing_old), len(missing_old)))
        rep.check(not missing_new, 'code names that existed in the originals but vanished: %s' % missing_new)
        if missing_old:
            print('  not controller names in the originals either (other APIs / dynamic):',
                  sorted(set(n for _, n in missing_old)))
    for n in NOTES:
        print('NOTE', n)
    if rep.errors:
        print('FAILED: %d problems' % len(rep.errors))
        for e in rep.errors[:60]:
            print('  ', e)
        sys.exit(1)
    print('ALL CHECKS PASSED')


if __name__ == '__main__':
    main()
