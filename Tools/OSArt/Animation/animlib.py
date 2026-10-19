"""Shared library for the OSArt animation tools.

* parse_clip / parse_controller   : Unity YAML  -> JSON-able "spec" dicts (the data contract)
* emit_clip / emit_controller     : spec        -> Unity YAML text (our own curves / our own layout)

The spec only carries what code and scenes depend on: bindings, lengths, loop flags, start/end values,
the timing "waypoints" of every curve (hold edges and turning points), events, controller layout-independent
data (layers, parameters, states, motions, transitions).  Everything between the waypoints is authored here
with our own easing and key tangents.
"""
import hashlib
import math
import os
import re
import struct
import zlib
from decimal import Decimal

from unityyaml import parse_file

INF = float('inf')

# --------------------------------------------------------------------------- numbers


def f32(x):
    x = float(x)
    if math.isinf(x) or math.isnan(x):
        return x
    return struct.unpack('<f', struct.pack('<f', x))[0]


def short(x):
    """Shortest decimal string that survives a float32 round trip (Unity style)."""
    x = float(x)
    if x == INF:
        return 'Infinity'
    if x == -INF:
        return '-Infinity'
    x = f32(x)
    if x == 0:
        return '0'
    s = None
    for n in range(1, 10):
        s = '%.*g' % (n, x)
        if f32(float(s)) == x:
            break
    if 'e' in s or 'E' in s:
        s = format(Decimal(s), 'f')
    return s


def num(s):
    """Parse a raw YAML number (supports Infinity)."""
    s = str(s).strip()
    if s in ('Infinity', 'inf'):
        return INF
    if s in ('-Infinity', '-inf'):
        return -INF
    return float(s)


def jnum(x):
    """Number for the JSON spec: float32-shortest, ints stay ints when integral."""
    v = float(short(x)) if not math.isinf(x) else x
    if v == int(v) and abs(v) < 1e9:
        return int(v)
    return v


def same(a, b, eps=1e-6):
    return abs(f32(a) - f32(b)) <= eps


# --------------------------------------------------------------------------- hashing / bindings


def crc(s):
    return zlib.crc32(s.encode('utf-8')) if s else 0


# (type -> (typeID, customType, attribute-hash)) for transform-level curves
VEC_INFO = {
    'euler': dict(typeID=4, customType=4, attrHash=4, edit='localEulerAnglesRaw', key='m_EulerCurves'),
    'scale': dict(typeID=4, customType=0, attrHash=3, edit='m_LocalScale', key='m_ScaleCurves'),
    'position': dict(typeID=4, customType=0, attrHash=1, edit='m_LocalPosition', key='m_PositionCurves'),
}
VEC_BY_ATTR = {v['edit']: k for k, v in VEC_INFO.items()}
AXES = ('x', 'y', 'z')


# --------------------------------------------------------------------------- waypoint reduction


def reduce_waypoints(keys):
    """keys: [(t, v)] -> only hold edges and turning points (plus first/last).

    Monotone interior keys carry no contract (start/end/timing) so they are dropped; the span is re-eased."""
    n = len(keys)
    if n <= 2:
        return [list(k) for k in keys]
    out = []
    for i, (t, v) in enumerate(keys):
        if 0 < i < n - 1 and same(keys[i - 1][1], v) and same(v, keys[i + 1][1]):
            continue  # interior of a hold
        out.append((t, v))
    res = [out[0]]
    for i in range(1, len(out) - 1):
        a, b, c = out[i - 1][1], out[i][1], out[i + 1][1]
        if (b > a + 1e-6 and c > b + 1e-6) or (b < a - 1e-6 and c < b - 1e-6):
            continue
        res.append(out[i])
    res.append(out[-1])
    return [list(r) for r in res]


# --------------------------------------------------------------------------- clip parsing


def _vec(d):
    return [num(d['x']), num(d['y']), num(d['z'])]


def _script(d):
    if isinstance(d, dict) and d.get('fileID', '0') != '0' or (isinstance(d, dict) and 'guid' in d):
        return dict(d)
    return None


def infer_easing(name, loop):
    if loop:
        return 'loop'
    tail = name.split('_', 1)[-1]
    if re.search(r'(Out|Hide|ToClosed|ToInactive|Disabled)', tail):
        return 'out'
    if re.search(r'(ToHighlighted|Idle|Off)', tail):
        return 'neutral'
    return 'in'


def parse_clip(path, guid=None):
    """Parse an .anim file into a spec dict."""
    docs = parse_file(path)
    assert len(docs) == 1 and docs[0]['type'] == 'AnimationClip', path
    c = docs[0]['data']
    st = c['m_AnimationClipSettings']
    loop = st['m_LoopTime'] == '1'
    name = c['m_Name']

    # binding table from the original constant (matched by hash) to keep typeID/customType/order
    bind = {}
    for i, g in enumerate(c['m_ClipBindingConstant']['genericBindings']):
        bind.setdefault((int(g['path']), int(g['attribute']), g['typeID']), (i, g))

    # editor class IDs for transform curves
    ed_cls = {}
    for e in c['m_EditorCurves']:
        a = e['attribute']
        base = a.rsplit('.', 1)[0]
        if base in VEC_BY_ATTR:
            ed_cls[(e['path'], base)] = e['classID']
        if base == 'localEulerAnglesRaw':
            ed_cls[(e['path'], base)] = e['classID']

    curves = []
    for kind in ('euler', 'position', 'scale'):
        vi = VEC_INFO[kind]
        for e in c[vi['key']]:
            ks = e['curve']['m_Curve']
            axes = [[], [], []]
            for k in ks:
                t = num(k['time'])
                for ai, ax in enumerate(AXES):
                    axes[ai].append((t, num(k['value'][ax])))
            wps = [reduce_waypoints(a) for a in axes]
            b = bind.get((crc(e['path']), vi['attrHash'], '4'))
            curves.append(dict(
                type=kind, path=e['path'], attribute=vi['edit'],
                classID=int(ed_cls.get((e['path'], vi['edit']), 224)),
                typeID=int(b[1]['typeID']) if b else vi['typeID'],
                customType=int(b[1]['customType']) if b else vi['customType'],
                bindIndex=b[0] if b else None,
                start=[jnum(a[0][1]) for a in axes], end=[jnum(a[-1][1]) for a in axes],
                startTime=jnum(num(ks[0]['time'])), endTime=jnum(num(ks[-1]['time'])),
                origKeys=len(ks),
                axes=[[[jnum(t), jnum(v)] for t, v in w] for w in wps]))
    for e in c['m_FloatCurves']:
        ks = e['curve']['m_Curve']
        stepped = any(k['tangentMode'] == '103' or 'nfinity' in str(k['outSlope']) for k in ks)
        pts = [(num(k['time']), num(k['value'])) for k in ks]
        wp = [[t, v] for t, v in pts] if stepped else reduce_waypoints(pts)
        b = bind.get((crc(e['path']), crc(e['attribute']), e['classID']))
        curves.append(dict(
            type='float', path=e['path'], attribute=e['attribute'], classID=int(e['classID']),
            script=_script(e['script']),
            typeID=int(b[1]['typeID']) if b else int(e['classID']),
            customType=int(b[1]['customType']) if b else (28 if e['classID'] == '224' else 0),
            bindIndex=b[0] if b else None,
            stepped=stepped, start=jnum(pts[0][1]), end=jnum(pts[-1][1]),
            startTime=jnum(pts[0][0]), endTime=jnum(pts[-1][0]), origKeys=len(ks),
            waypoints=[[jnum(t), jnum(v)] for t, v in wp]))
    for e in c['m_PPtrCurves']:
        ks = e['curve']
        b = bind.get((crc(e['path']), crc(e['attribute']), e['classID']))
        curves.append(dict(
            type='pptr', path=e['path'], attribute=e['attribute'], classID=int(e['classID']),
            script=_script(e['script']),
            typeID=int(b[1]['typeID']) if b else int(e['classID']),
            customType=int(b[1]['customType']) if b else 0,
            bindIndex=b[0] if b else None,
            start=dict(ks[0]['value']), end=dict(ks[-1]['value']),
            startTime=jnum(num(ks[0]['time'])), endTime=jnum(num(ks[-1]['time'])),
            keys=[dict(time=jnum(num(k['time'])), value=dict(k['value'])) for k in ks]))
    events = []
    for ev in c['m_Events']:
        events.append(dict(
            time=jnum(num(ev['time'])), functionName=ev['functionName'], data=ev.get('data', ''),
            objectReferenceParameter=dict(ev.get('objectReferenceParameter', {'fileID': '0'})),
            floatParameter=jnum(num(ev.get('floatParameter', '0'))),
            intParameter=int(ev.get('intParameter', '0')),
            messageOptions=int(ev.get('messageOptions', '0'))))
    spec = dict(
        name=name,
        guid=guid,
        length=jnum(num(st['m_StopTime'])),
        startTime=jnum(num(st['m_StartTime'])),
        loop=loop,
        wrapMode=int(c['m_WrapMode']),
        sampleRate=jnum(num(c['m_SampleRate'])),
        legacy=int(c['m_Legacy']),
        easing=infer_easing(name, loop),
        clipSettings={k: v for k, v in st.items() if k not in ('m_StopTime', 'm_StartTime')},
        flags=dict(compressed=int(c['m_Compressed']), useHighQualityCurve=int(c['m_UseHighQualityCurve']),
                   hasGenericRootTransform=int(c['m_HasGenericRootTransform']),
                   hasMotionFloatCurves=int(c['m_HasMotionFloatCurves'])),
        bounds=dict(center=dict(c['m_Bounds']['m_Center']), extent=dict(c['m_Bounds']['m_Extent'])),
        curves=curves,
        events=events)
    return spec


# --------------------------------------------------------------------------- our curves

# easing = (start slope factor, end slope factor); slope = factor * delta / duration
EASE = {
    'in': (3.0, 0.0),            # ease-out cubic: fast arrival, soft landing
    'out': (0.0, 3.0),           # ease-in cubic: soft departure, fast exit
    'neutral': (math.pi / 2, 0.0),  # ease-out sine
    'loop': (0.0, 0.0),          # sine-like in-out (zero slope at every extreme)
    'settle': (0.0, 0.0),
}


class Seg:
    """One cubic Hermite span of a curve."""
    __slots__ = ('t0', 'v0', 'm0', 't1', 'v1', 'm1')

    def __init__(self, t0, v0, m0, t1, v1, m1):
        self.t0, self.v0, self.m0, self.t1, self.v1, self.m1 = t0, v0, m0, t1, v1, m1

    def eval(self, t):
        dt = self.t1 - self.t0
        if dt <= 0:
            return self.v1
        u = (t - self.t0) / dt
        u2, u3 = u * u, u * u * u
        return ((2 * u3 - 3 * u2 + 1) * self.v0 + (u3 - 2 * u2 + u) * dt * self.m0 +
                (-2 * u3 + 3 * u2) * self.v1 + (u3 - u2) * dt * self.m1)

    def deriv(self, t):
        dt = self.t1 - self.t0
        if dt <= 0:
            return 0.0
        u = (t - self.t0) / dt
        u2 = u * u
        return ((6 * u2 - 6 * u) * self.v0 / dt + (3 * u2 - 4 * u + 1) * self.m0 +
                (-6 * u2 + 6 * u) * self.v1 / dt + (3 * u2 - 2 * u) * self.m1)


def build_segments(wp, easing, loop):
    """Waypoints [(t, v)] -> list of Seg authored with our own easing."""
    n = len(wp)

    def turn(i):
        if i <= 0 or i >= n - 1:
            return False
        a, b, c = wp[i - 1][1], wp[i][1], wp[i + 1][1]
        return (b > a + 1e-6 and b > c + 1e-6) or (b < a - 1e-6 and b < c - 1e-6)

    deltas = [wp[i + 1][1] - wp[i][1] for i in range(n - 1)]
    nz = [d for d in deltas if abs(d) > 1e-9]
    monotone = not nz or all(d > 0 for d in nz) or all(d < 0 for d in nz)

    # loops that return to their start value: carry the slope smoothly through the wrap point
    wrap = 0.0
    if easing == 'loop' and not monotone and n >= 3 and same(wp[0][1], wp[-1][1]):
        v0, vb, va = wp[0][1], wp[-2][1], wp[1][1]
        if (vb < v0 - 1e-6 and va > v0 + 1e-6) or (vb > v0 + 1e-6 and va < v0 - 1e-6):
            ta = wp[1][0] - wp[0][0]
            tb = wp[-1][0] - wp[-2][0]
            if ta > 0 and tb > 0:
                avg = (va - vb) / (ta + tb)
                lim = 3 * min(abs((va - v0) / ta), abs((v0 - vb) / tb))
                wrap = math.copysign(min(abs(avg), lim), avg)

    segs = []
    for i in range(n - 1):
        (t0, v0), (t1, v1) = wp[i], wp[i + 1]
        d = v1 - v0
        dt = t1 - t0
        if abs(d) < 1e-9 or dt <= 0:
            segs.append(Seg(t0, v0, 0.0, t1, v1, 0.0))
            continue
        if easing == 'loop' and monotone:
            m0 = m1 = d / dt                   # steady spin / drift
        elif easing == 'loop':
            m0 = wrap if i == 0 else 0.0       # sine-like: zero slope at every extreme / hold edge
            m1 = wrap if i == n - 2 else 0.0
        else:
            s0, s1 = EASE[easing]
            if turn(i):
                s0 = 0.0
            if turn(i + 1):
                s1 = 0.0
            m0, m1 = s0 * d / dt, s1 * d / dt
        segs.append(Seg(t0, v0, m0, t1, v1, m1))
    return segs


def _key_dict(t, v, m_in, m_out):
    return dict(time=t, value=v, inSlope=m_in, outSlope=m_out)


def float_keys(cv, easing, loop):
    """Keys for a float curve: list of dict(time, value, inSlope, outSlope, tangentMode, ...)."""
    wp = [tuple(p) for p in cv['waypoints']]
    if cv.get('stepped'):
        return [dict(time=t, value=v, inSlope=INF, outSlope=INF, tangentMode=103, inWeight=0.0, outWeight=0.0)
                for t, v in wp]
    if len(wp) == 1:
        t, v = wp[0]
        return [dict(time=t, value=v, inSlope=0.0, outSlope=0.0, tangentMode=0, inWeight=1 / 3, outWeight=1 / 3)]
    segs = build_segments(wp, easing, loop)
    keys = []
    for i, (t, v) in enumerate(wp):
        m_in = segs[i - 1].m1 if i > 0 else segs[0].m0
        m_out = segs[i].m0 if i < len(segs) else segs[-1].m1
        keys.append(dict(time=t, value=v, inSlope=m_in, outSlope=m_out,
                         tangentMode=0 if same(m_in, m_out, 1e-5) else 1, inWeight=1 / 3, outWeight=1 / 3))
    return keys


def vector_keys(cv, easing, loop):
    """Keys for a Vector3 curve (shared times, per axis values/slopes)."""
    axsegs = []
    times = set()
    for ax in cv['axes']:
        wp = [tuple(p) for p in ax]
        times.update(f32(t) for t, _ in wp)
        axsegs.append((wp, build_segments(wp, easing, loop) if len(wp) > 1 else None))
    times = sorted(times)
    keys = []
    for u in times:
        val, sin, sout = [], [], []
        for wp, segs in axsegs:
            if segs is None:
                val.append(wp[0][1]); sin.append(0.0); sout.append(0.0)
                continue
            idx = next((i for i, (t, _) in enumerate(wp) if same(t, u, 1e-7)), None)
            if idx is not None:
                val.append(wp[idx][1])
                sin.append(segs[idx - 1].m1 if idx > 0 else segs[0].m0)
                sout.append(segs[idx].m0 if idx < len(segs) else segs[-1].m1)
            else:
                s = next(s for s in segs if s.t0 <= u <= s.t1)
                val.append(s.eval(u)); d = s.deriv(u); sin.append(d); sout.append(d)
        broken = any(not same(a, b, 1e-5) for a, b in zip(sin, sout))
        keys.append(dict(time=u, value=val, inSlope=sin, outSlope=sout, tangentMode=1 if broken else 0,
                         inWeight=[1 / 3] * 3, outWeight=[1 / 3] * 3))
    return keys


# --------------------------------------------------------------------------- clip emitting


def flow(d):
    if isinstance(d, dict):
        return '{' + ', '.join('%s: %s' % (k, flow(v)) for k, v in d.items()) + '}'
    return str(d)


def _emit_float_curve(L, cv, keys, with_class=True):
    L.append('  - curve:')
    L.append('      serializedVersion: 2')
    if keys:
        L.append('      m_Curve:')
        for k in keys:
            L.append('      - serializedVersion: 3')
            L.append('        time: %s' % short(k['time']))
            L.append('        value: %s' % short(k['value']))
            L.append('        inSlope: %s' % short(k['inSlope']))
            L.append('        outSlope: %s' % short(k['outSlope']))
            L.append('        tangentMode: %d' % k['tangentMode'])
            L.append('        weightedMode: 0')
            L.append('        inWeight: %s' % short(k['inWeight']))
            L.append('        outWeight: %s' % short(k['outWeight']))
    else:
        L.append('      m_Curve: []')
    L.append('      m_PreInfinity: 2')
    L.append('      m_PostInfinity: 2')
    L.append('      m_RotationOrder: 4')


def _v3(vals):
    return '{x: %s, y: %s, z: %s}' % tuple(short(v) for v in vals)


def _emit_vec_curve(L, cv, keys):
    L.append('  - curve:')
    L.append('      serializedVersion: 2')
    L.append('      m_Curve:')
    for k in keys:
        L.append('      - serializedVersion: 3')
        L.append('        time: %s' % short(k['time']))
        L.append('        value: %s' % _v3(k['value']))
        L.append('        inSlope: %s' % _v3(k['inSlope']))
        L.append('        outSlope: %s' % _v3(k['outSlope']))
        L.append('        tangentMode: %d' % k['tangentMode'])
        L.append('        weightedMode: 0')
        L.append('        inWeight: %s' % _v3(k['inWeight']))
        L.append('        outWeight: %s' % _v3(k['outWeight']))
    L.append('      m_PreInfinity: 2')
    L.append('      m_PostInfinity: 2')
    L.append('      m_RotationOrder: 4')


def _script_str(s):
    return flow(s) if s else '{fileID: 0}'


def emit_clip(spec):
    easing = spec['easing']
    loop = spec['loop']
    curves = spec['curves']
    vec = {k: [] for k in VEC_INFO}
    floats, pptrs = [], []
    for cv in curves:
        if cv['type'] in VEC_INFO:
            vec[cv['type']].append((cv, vector_keys(cv, easing, loop)))
        elif cv['type'] == 'float':
            floats.append((cv, float_keys(cv, easing, loop)))
        elif cv['type'] == 'pptr':
            pptrs.append(cv)
        else:
            raise ValueError(cv['type'])

    L = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!74 &7400000', 'AnimationClip:',
         '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
         '  m_PrefabAsset: {fileID: 0}', '  m_Name: %s' % spec['name'], '  serializedVersion: 6',
         '  m_Legacy: %d' % spec['legacy'], '  m_Compressed: %d' % spec['flags']['compressed'],
         '  m_UseHighQualityCurve: %d' % spec['flags']['useHighQualityCurve'],
         '  m_RotationCurves: []', '  m_CompressedRotationCurves: []']

    def block(name, items, fn):
        if not items:
            L.append('  %s: []' % name)
            return
        L.append('  %s:' % name)
        for it in items:
            fn(it)

    def vec_entry(it):
        cv, keys = it
        _emit_vec_curve(L, cv, keys)
        L.append('    path: %s' % cv['path'])

    block('m_EulerCurves', vec['euler'], vec_entry)
    block('m_PositionCurves', vec['position'], vec_entry)
    block('m_ScaleCurves', vec['scale'], vec_entry)

    def float_entry(it):
        cv, keys = it
        _emit_float_curve(L, cv, keys)
        L.append('    attribute: %s' % cv['attribute'])
        L.append('    path: %s' % cv['path'])
        L.append('    classID: %d' % cv['classID'])
        L.append('    script: %s' % _script_str(cv.get('script')))

    block('m_FloatCurves', floats, float_entry)

    def pptr_entry(cv):
        L.append('  - curve:')
        for k in cv['keys']:
            L.append('    - time: %s' % short(k['time']))
            L.append('      value: %s' % flow(k['value']))
        L.append('    attribute: %s' % cv['attribute'])
        L.append('    path: %s' % cv['path'])
        L.append('    classID: %d' % cv['classID'])
        L.append('    script: %s' % _script_str(cv.get('script')))

    block('m_PPtrCurves', pptrs, pptr_entry)
    L.append('  m_SampleRate: %s' % short(spec['sampleRate']))
    L.append('  m_WrapMode: %d' % spec['wrapMode'])
    L.append('  m_Bounds:')
    L.append('    m_Center: %s' % flow(spec['bounds']['center']))
    L.append('    m_Extent: %s' % flow(spec['bounds']['extent']))

    # bindings: one per transform curve / float curve / pptr curve, in Unity's own observed order
    binds = []
    for ci, cv in enumerate(curves):
        if cv['type'] in VEC_INFO:
            vi = VEC_INFO[cv['type']]
            binds.append((cv, dict(path=crc(cv['path']), attribute=vi['attrHash'], script='{fileID: 0}',
                                   typeID=cv.get('typeID', vi['typeID']),
                                   customType=cv.get('customType', vi['customType']), pptr=0)))
        elif cv['type'] == 'float':
            binds.append((cv, dict(path=crc(cv['path']), attribute=crc(cv['attribute']),
                                   script=_script_str(cv.get('script')), typeID=cv['typeID'],
                                   customType=cv['customType'], pptr=0)))
        else:
            binds.append((cv, dict(path=crc(cv['path']), attribute=crc(cv['attribute']),
                                   script=_script_str(cv.get('script')), typeID=cv['typeID'],
                                   customType=cv['customType'], pptr=1)))
    order = sorted(range(len(binds)), key=lambda i: (binds[i][0].get('bindIndex') is None,
                                                     binds[i][0].get('bindIndex') or 0, i))
    L.append('  m_ClipBindingConstant:')
    if binds:
        L.append('    genericBindings:')
        for i in order:
            b = binds[i][1]
            L.append('    - serializedVersion: 2')
            L.append('      path: %d' % b['path'])
            L.append('      attribute: %d' % b['attribute'])
            L.append('      script: %s' % b['script'])
            L.append('      typeID: %d' % b['typeID'])
            L.append('      customType: %d' % b['customType'])
            L.append('      isPPtrCurve: %d' % b['pptr'])
    else:
        L.append('    genericBindings: []')
    if pptrs:
        L.append('    pptrCurveMapping:')
        for cv in pptrs:
            for k in cv['keys']:
                L.append('    - %s' % flow(k['value']))
    else:
        L.append('    pptrCurveMapping: []')

    cs = dict(spec['clipSettings'])
    L.append('  m_AnimationClipSettings:')
    L.append('    serializedVersion: %s' % cs.get('serializedVersion', '2'))
    L.append('    m_AdditiveReferencePoseClip: %s' % flow(cs.get('m_AdditiveReferencePoseClip', {'fileID': '0'})))
    L.append('    m_AdditiveReferencePoseTime: %s' % cs.get('m_AdditiveReferencePoseTime', '0'))
    L.append('    m_StartTime: %s' % short(spec['startTime']))
    L.append('    m_StopTime: %s' % short(spec['length']))
    for key in ('m_OrientationOffsetY', 'm_Level', 'm_CycleOffset', 'm_HasAdditiveReferencePose'):
        L.append('    %s: %s' % (key, cs.get(key, '0')))
    L.append('    m_LoopTime: %d' % (1 if loop else 0))
    for key in ('m_LoopBlend', 'm_LoopBlendOrientation', 'm_LoopBlendPositionY', 'm_LoopBlendPositionXZ',
                'm_KeepOriginalOrientation', 'm_KeepOriginalPositionY', 'm_KeepOriginalPositionXZ',
                'm_HeightFromFeet', 'm_Mirror'):
        L.append('    %s: %s' % (key, cs.get(key, '0')))

    # editor curves: floats first, then the per-axis expansion of euler / position / scale
    ed = []
    for cv, keys in floats:
        ed.append((cv, keys, None))
    for kind in ('euler', 'position', 'scale'):
        for cv, keys in vec[kind]:
            for ai, ax in enumerate(AXES):
                ek = [dict(time=k['time'], value=k['value'][ai], inSlope=k['inSlope'][ai],
                           outSlope=k['outSlope'][ai], tangentMode=k['tangentMode'],
                           inWeight=k['inWeight'][ai], outWeight=k['outWeight'][ai]) for k in keys]
                ed.append((dict(cv, attribute='%s.%s' % (VEC_INFO[kind]['edit'], ax), script=None), ek, ax))
    if ed:
        L.append('  m_EditorCurves:')
        for cv, keys, _ in ed:
            _emit_float_curve(L, cv, keys)
            L.append('    attribute: %s' % cv['attribute'])
            L.append('    path: %s' % cv['path'])
            L.append('    classID: %d' % cv['classID'])
            L.append('    script: %s' % _script_str(cv.get('script')))
    else:
        L.append('  m_EditorCurves: []')
    eul = []
    for cv, _ in vec['euler']:
        for ax in AXES:
            eul.append((cv, ax))
    if eul:
        L.append('  m_EulerEditorCurves:')
        for cv, ax in eul:
            _emit_float_curve(L, cv, [])
            L.append('    attribute: m_LocalEulerAngles.%s' % ax)
            L.append('    path: %s' % cv['path'])
            L.append('    classID: %d' % cv['classID'])
            L.append('    script: {fileID: 0}')
    else:
        L.append('  m_EulerEditorCurves: []')
    L.append('  m_HasGenericRootTransform: %d' % spec['flags']['hasGenericRootTransform'])
    L.append('  m_HasMotionFloatCurves: %d' % spec['flags']['hasMotionFloatCurves'])
    if spec['events']:
        L.append('  m_Events:')
        for ev in spec['events']:
            L.append('  - time: %s' % short(ev['time']))
            L.append('    functionName: %s' % ev['functionName'])
            L.append('    data: %s' % ev['data'])
            L.append('    objectReferenceParameter: %s' % flow(ev['objectReferenceParameter']))
            L.append('    floatParameter: %s' % short(ev['floatParameter']))
            L.append('    intParameter: %d' % ev['intParameter'])
            L.append('    messageOptions: %d' % ev['messageOptions'])
    else:
        L.append('  m_Events: []')
    return '\n'.join(L) + '\n'


# --------------------------------------------------------------------------- controllers


def _ref_id(d):
    return int(d['fileID'])


def parse_controller(path):
    """Parse a .controller into a spec dict (reachable data only; orphans are reported)."""
    docs = parse_file(path)
    by = {d['fileID']: d for d in docs}
    ctrl = next(d for d in docs if d['type'] == 'AnimatorController')
    cd = ctrl['data']
    params = []
    for p in cd['m_AnimatorParameters']:
        params.append(dict(name=p['m_Name'], type=int(p['m_Type']), defaultFloat=jnum(num(p['m_DefaultFloat'])),
                           defaultInt=int(p['m_DefaultInt']), defaultBool=int(p['m_DefaultBool'])))
    used = set()
    layers = []

    def trans(tid):
        t = by[tid]
        used.add(tid)
        td = t['data']
        dst = _ref_id(td['m_DstState'])
        out = dict(
            name=td['m_Name'],
            conditions=[dict(mode=int(c['m_ConditionMode']), event=c['m_ConditionEvent'],
                             threshold=jnum(num(c['m_EventTreshold']))) for c in td['m_Conditions']],
            dst=by[dst]['data']['m_Name'] if dst else None,
            dstStateMachine=None,
            solo=int(td['m_Solo']), mute=int(td['m_Mute']), isExit=int(td['m_IsExit']))
        assert _ref_id(td['m_DstStateMachine']) == 0, 'sub state machines not supported'
        if t['type'] == 'AnimatorStateTransition':
            out.update(
                kind='state', duration=jnum(num(td['m_TransitionDuration'])),
                offset=jnum(num(td['m_TransitionOffset'])), exitTime=jnum(num(td['m_ExitTime'])),
                hasExitTime=int(td['m_HasExitTime']), hasFixedDuration=int(td['m_HasFixedDuration']),
                interruptionSource=int(td['m_InterruptionSource']),
                orderedInterruption=int(td['m_OrderedInterruption']),
                canTransitionToSelf=int(td['m_CanTransitionToSelf']))
        else:
            out.update(kind='entry')
        return out

    for l in cd['m_AnimatorLayers']:
        sid = _ref_id(l['m_StateMachine'])
        sm = by[sid]['data']
        used.add(sid)
        assert not sm['m_ChildStateMachines'], 'child state machines not supported: ' + path
        assert not sm['m_StateMachineBehaviours'] and not l['m_Behaviours'], 'behaviours: ' + path
        states = []
        for cs in sm['m_ChildStates']:
            stid = _ref_id(cs['m_State'])
            st = by[stid]['data']
            used.add(stid)
            assert not st['m_StateMachineBehaviours']
            mot = st['m_Motion']
            states.append(dict(
                name=st['m_Name'],
                motion=dict(mot) if _ref_id(mot) else None,
                speed=jnum(num(st['m_Speed'])), cycleOffset=jnum(num(st['m_CycleOffset'])),
                writeDefaults=int(st['m_WriteDefaultValues']), mirror=int(st['m_Mirror']),
                ikOnFeet=int(st['m_IKOnFeet']), tag=st['m_Tag'],
                speedParameter=st['m_SpeedParameter'], mirrorParameter=st['m_MirrorParameter'],
                cycleOffsetParameter=st['m_CycleOffsetParameter'], timeParameter=st['m_TimeParameter'],
                speedParameterActive=int(st['m_SpeedParameterActive']),
                mirrorParameterActive=int(st['m_MirrorParameterActive']),
                cycleOffsetParameterActive=int(st['m_CycleOffsetParameterActive']),
                timeParameterActive=int(st['m_TimeParameterActive']),
                transitions=[trans(_ref_id(t)) for t in st['m_Transitions']]))
        dflt = _ref_id(sm['m_DefaultState'])
        layers.append(dict(
            name=l['m_Name'], defaultWeight=jnum(num(l['m_DefaultWeight'])),
            blendingMode=int(l['m_BlendingMode']), syncedLayerIndex=int(l['m_SyncedLayerIndex']),
            iKPass=int(l['m_IKPass']), syncedLayerAffectsTiming=int(l['m_SyncedLayerAffectsTiming']),
            mask=dict(l['m_Mask']),
            stateMachine=dict(
                name=sm['m_Name'],
                defaultState=by[dflt]['data']['m_Name'] if dflt else None,
                states=states,
                anyStateTransitions=[trans(_ref_id(t)) for t in sm['m_AnyStateTransitions']],
                entryTransitions=[trans(_ref_id(t)) for t in sm['m_EntryTransitions']])))
    orphans = [d['type'] + ':' + (d['data'].get('m_Name', '') or str(d['fileID'])) for d in docs
               if d['type'] in ('AnimatorState', 'AnimatorStateTransition', 'AnimatorTransition',
                                'AnimatorStateMachine') and d['fileID'] not in used]
    return dict(name=cd['m_Name'], parameters=params, layers=layers, droppedOrphans=orphans)


def _sid(ctrl_key, kind, key):
    h = hashlib.md5(('%s|%s|%s' % (ctrl_key, kind, key)).encode('utf-8')).digest()
    v = struct.unpack('<q', h[:8])[0]
    if v in (0, 9100000):
        v += 1
    return v


def emit_controller(spec, ctrl_key):
    """Emit a controller with our own object IDs and a clean grid layout."""
    ids = {}

    def sid(kind, key):
        i = _sid(ctrl_key, kind, key)
        ids[(kind, key)] = i
        return i

    docs = []  # (fileID, classID, [lines])

    def hdr(typ, name, extra_ver=None):
        return [typ + ':', '  m_ObjectHideFlags: 1' if typ != 'AnimatorController' else '  m_ObjectHideFlags: 0',
                '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
                '  m_PrefabAsset: {fileID: 0}']

    def emit_transition(tr, owner):
        tid = sid('trans', '%s|%s|%d' % (owner, tr.get('dst'), len(docs)))
        dst = ids_state[(layer_key, tr['dst'])] if tr['dst'] else 0
        isstate = tr['kind'] == 'state'
        L = hdr('AnimatorStateTransition' if isstate else 'AnimatorTransition', tr['name'])
        L.append('  m_Name: %s' % tr['name'])
        if tr['conditions']:
            L.append('  m_Conditions:')
            for c in tr['conditions']:
                L.append('  - m_ConditionMode: %d' % c['mode'])
                L.append('    m_ConditionEvent: %s' % c['event'])
                L.append('    m_EventTreshold: %s' % short(c['threshold']))
        else:
            L.append('  m_Conditions: []')
        L.append('  m_DstStateMachine: {fileID: 0}')
        L.append('  m_DstState: {fileID: %d}' % dst)
        L.append('  m_Solo: %d' % tr['solo'])
        L.append('  m_Mute: %d' % tr['mute'])
        L.append('  m_IsExit: %d' % tr['isExit'])
        L.append('  serializedVersion: %d' % (3 if isstate else 1))
        if isstate:
            L.append('  m_TransitionDuration: %s' % short(tr['duration']))
            L.append('  m_TransitionOffset: %s' % short(tr['offset']))
            L.append('  m_ExitTime: %s' % short(tr['exitTime']))
            L.append('  m_HasExitTime: %d' % tr['hasExitTime'])
            L.append('  m_HasFixedDuration: %d' % tr['hasFixedDuration'])
            L.append('  m_InterruptionSource: %d' % tr['interruptionSource'])
            L.append('  m_OrderedInterruption: %d' % tr['orderedInterruption'])
            L.append('  m_CanTransitionToSelf: %d' % tr['canTransitionToSelf'])
        docs.append((tid, 1101 if isstate else 1109, L))
        return tid

    ids_state = {}
    layer_docs = []
    for li, layer in enumerate(spec['layers']):
        layer_key = '%d:%s' % (li, layer['name'])
        sm = layer['stateMachine']
        for st in sm['states']:
            ids_state[(layer_key, st['name'])] = sid('state', '%s|%s' % (layer_key, st['name']))
        sm_id = sid('sm', layer_key)
        layer_docs.append((layer_key, sm_id))
        # states
        child = []
        # layout: default state first, then declaration order; columns of 6
        order = sorted(range(len(sm['states'])), key=lambda i: (sm['states'][i]['name'] != sm['defaultState'], i))
        pos = {}
        for rank, i in enumerate(order):
            col, row = divmod(rank, 6)
            pos[i] = (300 + 260 * col, 40 + 70 * row)
        for i, st in enumerate(sm['states']):
            stid = ids_state[(layer_key, st['name'])]
            trs = [emit_transition(t, 'S|%s|%s' % (layer_key, st['name'])) for t in st['transitions']]
            L = ['AnimatorState:', '  serializedVersion: 6', '  m_ObjectHideFlags: 1',
                 '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
                 '  m_PrefabAsset: {fileID: 0}', '  m_Name: %s' % st['name'], '  m_Speed: %s' % short(st['speed']),
                 '  m_CycleOffset: %s' % short(st['cycleOffset'])]
            if trs:
                L.append('  m_Transitions:')
                L += ['  - {fileID: %d}' % t for t in trs]
            else:
                L.append('  m_Transitions: []')
            L += ['  m_StateMachineBehaviours: []', '  m_Position: {x: 50, y: 50, z: 0}',
                  '  m_IKOnFeet: %d' % st['ikOnFeet'], '  m_WriteDefaultValues: %d' % st['writeDefaults'],
                  '  m_Mirror: %d' % st['mirror'],
                  '  m_SpeedParameterActive: %d' % st['speedParameterActive'],
                  '  m_MirrorParameterActive: %d' % st['mirrorParameterActive'],
                  '  m_CycleOffsetParameterActive: %d' % st['cycleOffsetParameterActive'],
                  '  m_TimeParameterActive: %d' % st['timeParameterActive'],
                  '  m_Motion: %s' % (flow(st['motion']) if st['motion'] else '{fileID: 0}'),
                  '  m_Tag: %s' % st['tag'], '  m_SpeedParameter: %s' % st['speedParameter'],
                  '  m_MirrorParameter: %s' % st['mirrorParameter'],
                  '  m_CycleOffsetParameter: %s' % st['cycleOffsetParameter'],
                  '  m_TimeParameter: %s' % st['timeParameter']]
            docs.append((stid, 1102, L))
            child.append((stid, pos[i]))
        anys = [emit_transition(t, 'A|%s' % layer_key) for t in sm['anyStateTransitions']]
        entries = [emit_transition(t, 'E|%s' % layer_key) for t in sm['entryTransitions']]
        L = ['AnimatorStateMachine:', '  serializedVersion: 6', '  m_ObjectHideFlags: 1',
             '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
             '  m_PrefabAsset: {fileID: 0}', '  m_Name: %s' % sm['name']]
        if child:
            L.append('  m_ChildStates:')
            for stid, (x, y) in child:
                L += ['  - serializedVersion: 1', '    m_State: {fileID: %d}' % stid,
                      '    m_Position: {x: %d, y: %d, z: 0}' % (x, y)]
        else:
            L.append('  m_ChildStates: []')
        L.append('  m_ChildStateMachines: []')
        if anys:
            L.append('  m_AnyStateTransitions:')
            L += ['  - {fileID: %d}' % t for t in anys]
        else:
            L.append('  m_AnyStateTransitions: []')
        if entries:
            L.append('  m_EntryTransitions:')
            L += ['  - {fileID: %d}' % t for t in entries]
        else:
            L.append('  m_EntryTransitions: []')
        dflt = ids_state[(layer_key, sm['defaultState'])] if sm['defaultState'] else 0
        L += ['  m_StateMachineTransitions: {}', '  m_StateMachineBehaviours: []',
              '  m_AnyStatePosition: {x: 40, y: -30, z: 0}', '  m_EntryPosition: {x: 40, y: 40, z: 0}',
              '  m_ExitPosition: {x: 40, y: 110, z: 0}', '  m_ParentStateMachinePosition: {x: 40, y: 180, z: 0}',
              '  m_DefaultState: {fileID: %d}' % dflt]
        docs.append((sm_id, 1107, L))

    L = ['AnimatorController:', '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}',
         '  m_PrefabInstance: {fileID: 0}', '  m_PrefabAsset: {fileID: 0}', '  m_Name: %s' % spec['name'],
         '  serializedVersion: 5']
    if spec['parameters']:
        L.append('  m_AnimatorParameters:')
        for p in spec['parameters']:
            L += ['  - m_Name: %s' % p['name'], '    m_Type: %d' % p['type'],
                  '    m_DefaultFloat: %s' % short(p['defaultFloat']), '    m_DefaultInt: %d' % p['defaultInt'],
                  '    m_DefaultBool: %d' % p['defaultBool'], '    m_Controller: {fileID: 9100000}']
    else:
        L.append('  m_AnimatorParameters: []')
    L.append('  m_AnimatorLayers:')
    for layer, (lk, sm_id) in zip(spec['layers'], layer_docs):
        L += ['  - serializedVersion: 5', '    m_Name: %s' % layer['name'], '    m_StateMachine: {fileID: %d}' % sm_id,
              '    m_Mask: %s' % flow(layer['mask']), '    m_Motions: []', '    m_Behaviours: []',
              '    m_BlendingMode: %d' % layer['blendingMode'], '    m_SyncedLayerIndex: %d' % layer['syncedLayerIndex'],
              '    m_DefaultWeight: %s' % short(layer['defaultWeight']), '    m_IKPass: %d' % layer['iKPass'],
              '    m_SyncedLayerAffectsTiming: %d' % layer['syncedLayerAffectsTiming'],
              '    m_Controller: {fileID: 9100000}']
    docs.append((9100000, 91, L))
    docs.sort(key=lambda d: d[0])
    out = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:']
    for fid, cid, lines in docs:
        out.append('--- !u!%d &%d' % (cid, fid))
        out += lines
    return '\n'.join(out) + '\n'
