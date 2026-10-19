#!/usr/bin/env python3
"""Round-trip test for features the DreamOS originals do not use: sprite (PPtr) curves, events, transitions."""
import copy
import json
import os
import tempfile

import animlib

sprite = lambda n: {'fileID': str(21300000 + n), 'guid': 'a' * 32, 'type': '3'}
clip = dict(
    name='Synthetic_Loop', guid=None, length=0.5, startTime=0, loop=True, wrapMode=0, sampleRate=60, legacy=0,
    easing='loop', clipSettings={'serializedVersion': '2'}, flags=dict(compressed=0, useHighQualityCurve=1,
                                                                        hasGenericRootTransform=0, hasMotionFloatCurves=0),
    bounds=dict(center={'x': '0', 'y': '0', 'z': '0'}, extent={'x': '0', 'y': '0', 'z': '0'}),
    curves=[
        dict(type='pptr', path='Icon', attribute='m_Sprite', classID=114,
             script={'fileID': '-765806418', 'guid': 'f70555f144d8491a825f0804e09c671c', 'type': '3'},
             typeID=114, customType=0, bindIndex=None, start=sprite(0), end=sprite(2), startTime=0, endTime=0.5,
             keys=[dict(time=0, value=sprite(0)), dict(time=0.25, value=sprite(1)), dict(time=0.5, value=sprite(2))]),
        dict(type='float', path='Icon', attribute='m_Color.a', classID=114,
             script={'fileID': '-765806418', 'guid': 'f70555f144d8491a825f0804e09c671c', 'type': '3'},
             typeID=114, customType=0, bindIndex=None, stepped=False, start=0, end=0, startTime=0, endTime=0.5,
             waypoints=[[0, 0], [0.25, 1], [0.5, 0]]),
        dict(type='position', path='', attribute='m_LocalPosition', classID=224, typeID=4, customType=0, bindIndex=None,
             start=[0, 0, 0], end=[1, 2, 0], startTime=0, endTime=0.5,
             axes=[[[0, 0], [0.5, 1]], [[0, 0], [0.25, 2], [0.5, 2]], [[0, 0], [0.5, 0]]]),
    ],
    events=[dict(time=0.25, functionName='OnTick', data='x', objectReferenceParameter={'fileID': '0'},
                 floatParameter=1.5, intParameter=3, messageOptions=0)])
ctrl = dict(
    name='Synth', parameters=[dict(name='Go', type=9, defaultFloat=0, defaultInt=0, defaultBool=0),
                              dict(name='Speed', type=1, defaultFloat=1, defaultInt=0, defaultBool=0)],
    layers=[dict(name='Base Layer', defaultWeight=0, blendingMode=0, syncedLayerIndex=-1, iKPass=0,
                 syncedLayerAffectsTiming=0, mask={'fileID': '0'},
                 stateMachine=dict(name='Base Layer', defaultState='A', states=[
                     dict(name=n, motion={'fileID': '7400000', 'guid': 'b' * 32, 'type': '2'}, speed=1, cycleOffset=0,
                          writeDefaults=1, mirror=0, ikOnFeet=0, tag='', speedParameter='Speed', mirrorParameter='',
                          cycleOffsetParameter='', timeParameter='', speedParameterActive=1, mirrorParameterActive=0,
                          cycleOffsetParameterActive=0, timeParameterActive=0,
                          transitions=[dict(kind='state', name='', conditions=[dict(mode=1, event='Go', threshold=0)],
                                            dst='B' if n == 'A' else 'A', dstStateMachine=None, solo=0, mute=0,
                                            isExit=0, duration=0.25, offset=0, exitTime=0.75, hasExitTime=1,
                                            hasFixedDuration=1, interruptionSource=0, orderedInterruption=1,
                                            canTransitionToSelf=1)]) for n in 'AB'],
                     anyStateTransitions=[dict(kind='state', name='', conditions=[], dst='B', dstStateMachine=None,
                                               solo=0, mute=0, isExit=0, duration=0.1, offset=0, exitTime=0,
                                               hasExitTime=0, hasFixedDuration=1, interruptionSource=0,
                                               orderedInterruption=1, canTransitionToSelf=0)],
                     entryTransitions=[dict(kind='entry', name='', conditions=[], dst='A', dstStateMachine=None,
                                            solo=0, mute=0, isExit=0)]))],
    droppedOrphans=[])

d = tempfile.mkdtemp()
cp, kp = os.path.join(d, 's.anim'), os.path.join(d, 's.controller')
open(cp, 'w').write(animlib.emit_clip(clip))
open(kp, 'w').write(animlib.emit_controller(ctrl, 'synthetic'))
c2 = animlib.parse_clip(cp)
for k in ('name', 'loop', 'length', 'events'):
    assert c2[k] == clip[k], k
by = {c['type']: c for c in c2['curves']}
assert [k['value'] for k in by['pptr']['keys']] == [sprite(0), sprite(1), sprite(2)]
assert [k['time'] for k in by['pptr']['keys']] == [0, 0.25, 0.5]
assert by['float']['waypoints'] == [[0, 0], [0.25, 1], [0.5, 0]]
assert by['position']['end'] == [1, 2, 0]
k2 = animlib.parse_controller(kp)
a, b = copy.deepcopy(ctrl), copy.deepcopy(k2)
a.pop('droppedOrphans'); b.pop('droppedOrphans')
assert a == b, json.dumps(b, indent=1)
print('synthetic round trip OK:', d)
