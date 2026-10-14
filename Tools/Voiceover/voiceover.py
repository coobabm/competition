"""Offline, resumable VoiceStudio production. No third-party Python dependencies."""
import argparse
import array
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess
import sys
import time
import wave

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / 'Assets/LingGuangV05'
OUT = ROOT / 'Documentation/Voiceover/2026-10-12'
RESOURCE = GAME / 'Resources/LingGuangV05/Voice'
PAUSE = re.compile(r'\[pause(?: [\d.]+(?:ms|s))?\]')
STRING = r'"(?:\\.|[^"\\])*"'


def key(text):
    return ''.join(c for c in re.sub(r'<[^>]*>', '', text) if c.isalpha() or c.isdecimal())


def clip_id(text):
    return 'v_' + hashlib.sha256(key(text).encode()).hexdigest()[:16]


def is_dynamic(text):
    return bool(re.search(r'\{[^{}]+\}', text))


def decode(s):
    return json.loads(s)


def direct(text, mood):
    text = re.sub(r'<[^>]*>', '', text).replace('「是。否。」', '「是、否」')
    pause = 550 if mood in ('weary', 'sad', 'reflective') else 320
    text = re.sub(r'…{1,}|\.{3,}', f'[pause {pause}ms]', text)
    text = re.sub(r'([。！？；])(?=.)', r'\1[pause ' + str(pause - 100) + 'ms]', text)
    return text


def performance(text, source):
    if text.startswith('和好了。'):
        return 'wry', 'm_ignored', .95
    if any(w in text for w in ('对不起', '没交上', '群消息', '暗了', '吵架')):
        return 'sad', 'm_left', .90
    if any(w in text for w in ('手腕', '上班', '电费', '烧烤', '眼睛', '鼠标左键', '包浆', '一晚上', '机器', '睡', '闭上眼')):
        return 'weary', 'm_ignored', .94
    if any(w in text for w in ('病毒', '谁让', '喂——', '扣钱', '举报', '什么玩意', '怎么能')):
        return 'alarmed', 'm_hijack', 1.04
    if any(w in text for w in ('学会', '答对', '挣钱', '高兴', '见面', '可行', '还比我快', '和好了')):
        return 'pleased', 'income_2', 1.0
    if any(w in text for w in ('糊弄', '包工头', '谁学', '哦，', '没说错', '准时', '跟我')):
        return 'wry', 'yy_learn_2', .98
    return 'reflective', 'm_left', .97


def inventory():
    entries = {}
    dynamic = []
    def add(text, source, category):
        if not isinstance(text, str): return
        text = text.replace('{exe}', '灵光.exe')
        if is_dynamic(text):
            dynamic.append({'template': text, 'source': source, 'category': category})
            return
        if not key(text): return
        k = key(text)
        if k in entries:
            if source not in entries[k]['sources']: entries[k]['sources'].append(source)
            return
        mood, reference, speed = performance(text, source)
        entries[k] = {'text': text, 'clip': clip_id(text), 'sources': [source], 'category': category,
                      'mood': mood, 'reference': reference, 'speed': speed, 'script': direct(text, mood).replace('调参', 'TIAO2CAN1').replace('.exe', '点 E X E')}
    def clean(source):
        # Preserve strings while removing comments, including commented-out example calls.
        return re.sub(STRING + r'|//[^\n]*|/\*[\s\S]*?\*/',
                      lambda m: m[0] if m[0].startswith('"') else '', source)
    for p in GAME.rglob('*.cs'):
        if any(x in p.parts for x in ('Editor', 'Tests')): continue
        text = clean(p.read_text())
        for m in re.finditer(r'InnerVoice\.(?:Say|SayAndWait)\(\s*(' + STRING + r')\s*([,+])', text):
            if m[2] == ',': add(decode(m[1]), str(p.relative_to(ROOT)), 'direct')
        if p.name == 'XgInnerMonologue.cs':
            for m in re.finditer(r'\bL\(\w+,\s*(' + STRING + r'),\s*(' + STRING + ')', text):
                add(decode(m[2]), f'{p.relative_to(ROOT)}:{decode(m[1])}', 'labelling')
        if p.name == 'XgSim.Afterthoughts.cs':
            for m in re.finditer(r'\{\s*(' + STRING + r'),\s*new\[\]\s*\{\s*(' + STRING + ')', text):
                for line in decode(m[2]).split('|'): add(line, f'{p.relative_to(ROOT)}:{decode(m[1])}', 'afterthought')
        if p.name == 'XgSim.SenseVoice.cs':
            for m in re.finditer(r'new XgVoiceLine\((' + STRING + ')', text): add(decode(m[1]), str(p.relative_to(ROOT)), 'sense')
        if p.name == 'GirlfriendRules.Proactive.cs':
            part = text[text.index('EpilogueLine('):]
            for m in re.finditer(r'return\s*\((' + STRING + ')', part): add(decode(m[1]), str(p.relative_to(ROOT)), 'epilogue')
        if p.name == 'XgBoardPage.Cortex.cs':
            part = text[text.index('Say("……这是什么？"'):text.index('static void Say')]
            for s in re.findall(STRING, part):
                v = decode(s)
                if re.search('[\u4e00-\u9fff]', v): add(v, str(p.relative_to(ROOT)), 'cortex')
    prologue = GAME / 'Resources/LingGuangV05/Prologue/prologue_lines.json'
    for name, pair in json.loads(prologue.read_text())['lines'].items():
        if name.startswith('m_'): add(pair['zh'], f'{prologue.relative_to(ROOT)}:{name}', 'prologue')
    story = GAME / 'Resources/LingGuangV05/Story'
    texts = {}
    for p in story.glob('*.json'):
        texts.update(json.loads(p.read_text()).get('lines', {}))
    def steps(obj, source):
        if isinstance(obj, dict):
            if obj.get('op') in ('think', 'narrate'):
                raw = texts.get(obj.get('key'), obj.get('text', ''))
                for line in raw if isinstance(raw, list) else [raw]: add(line, source + ':' + obj.get('key','inline'), obj['op'])
            for value in obj.values(): steps(value, source)
        elif isinstance(obj, list):
            for value in obj: steps(value, source)
    for p in story.glob('*story.json'): steps(json.loads(p.read_text()), str(p.relative_to(ROOT)))
    # Resolved story placeholders: finite authored branches, not arbitrary generated dialogue.
    bridge = GAME / 'XingGuang/Desktop/XgEraAdvantageBridge.cs'
    for line in bridge.read_text().splitlines():
        if any('case "'+name+'"' in line for name in ('eraContextOutcome', 'eraDeliverySummary')):
            for value in re.findall(STRING, line)[1:]:
                if re.search('[\u4e00-\u9fff]', decode(value)):
                    add(decode(value), str(bridge.relative_to(ROOT)), 'resolved-story')
        if 'case "eraIdentityReply"' in line:
            for value in ('就我一个。', '我自己慢慢做的。', '我还没有回答。'):
                add('刚才发出去的是：「'+value+'」', str(bridge.relative_to(ROOT)), 'resolved-story')
    add('还没有新的验收记录。', str(bridge.relative_to(ROOT)), 'resolved-story')
    portions = json.loads(Path(__file__).with_name('dynamic_portions.json').read_text())
    for portion in portions:
        if portion['spoken']:
            add(portion['spoken'], 'dynamic_portions.json:'+portion['template'], 'dynamic-fixed')
            portion['clip'] = clip_id(portion['spoken'])
    # Preserve existing direction and recordings; do not re-render already authored performances.
    legacy = (GAME / 'DesktopBridge/Story/VoiceLines.cs').read_text()
    for m in re.finditer(r'\{\s*(' + STRING + r'),\s*(' + STRING + r')\s*\}', legacy):
        line, clip = decode(m[1]), decode(m[2])
        add(line, 'VoiceLines.cs:existing', 'existing')
        if key(line) in entries: entries[key(line)]['existing'] = clip
    return {'entries': list(entries.values()), 'dynamic': dynamic, 'portions': portions}


def stats(path):
    with wave.open(str(path)) as f:
        channels, rate, width, count = f.getnchannels(), f.getframerate(), f.getsampwidth(), f.getnframes()
        if width != 2 or channels != 1 or rate != 24000: raise ValueError('Expected 24 kHz PCM16 mono')
        samples = array.array('h', f.readframes(count))
    if sys.byteorder != 'little': samples.byteswap()
    if not samples: raise ValueError('Empty waveform')
    if len(samples) != count: raise ValueError('Truncated WAV payload')
    peak = max(abs(n) for n in samples) / 32768
    rms = math.sqrt(sum(n*n for n in samples) / len(samples)) / 32768
    if peak < .001 or count / rate < .12: raise ValueError('Silent or truncated take')
    return {'seconds': count/rate, 'peak': peak, 'rms': rms, 'samples': count,
            'clipped_fraction': sum(abs(n) >= 32760 for n in samples) / len(samples)}


def read_proof(path):
    try:
        data = json.loads(path.read_text())
        return data if isinstance(data, dict) else {}
    except (OSError, ValueError):
        return {}


def atomic_json(path, data):
    temp = path.with_suffix('.tmp')
    temp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')
    temp.replace(path)


def render(limit):
    data = json.loads((OUT / 'manifest.json').read_text())
    takes = OUT / 'takes'; takes.mkdir(exist_ok=True)
    made = 0
    for i, row in enumerate(data['entries']):
        if row.get('existing'): continue
        dest = takes / (row['clip'] + '.wav')
        proof = dest.with_suffix('.json')
        signature = hashlib.sha256(json.dumps(row, ensure_ascii=False, sort_keys=True).encode()).hexdigest()
        if dest.exists() and proof.exists() and read_proof(proof).get('signature') == signature:
            stats(dest); continue
        if limit and made >= limit: break
        temp = dest.with_suffix('.part.wav'); headers = dest.with_suffix('.headers')
        reference = RESOURCE / 'me' / (row['reference'] + '.wav')
        original = {'m_left':'……就这么走了？', 'm_ignored':'……算了。360 天天报毒，八成又是误报。',
                    'm_hijack':'我去，什么玩意儿在动我电脑？不会中病毒了吧？！',
                    'income_2':'躺着也能挣钱？', 'yy_learn_2':'哦，跟我。'}[row['reference']]
        cmd = ['curl','--noproxy','*','--fail-with-body','--max-time','420','-sS','-D',str(headers),'-o',str(temp)]
        fields = {'text':row['script'],'language':'Chinese','engine':'omnivoice','num_step':'32',
                  'seed':str(int(row['clip'][2:10], 16) % 2147483647),'speed':str(row['speed']), 'postprocess_output':'false',
                  'ref_audio':'@'+str(reference),'ref_text':original}
        for name,value in fields.items():
            cmd += ['-F' if name == 'ref_audio' else '--form-string', name+'='+value]
        print(f'[{i+1}/{len(data["entries"])}] {row["mood"]} {row["text"]}', flush=True)
        start = time.monotonic()
        subprocess.run(cmd + ['http://127.0.0.1:3900/generate'], check=True)
        if re.search(r'x-omnivoice-dropped-chunks:\s*[1-9]',headers.read_text(), re.I):
            raise RuntimeError('Dropped speech chunk: '+row['clip'])
        evidence = stats(temp)
        if evidence['clipped_fraction'] > .001: raise ValueError('Clipping requires review')
        temp.replace(dest)
        evidence.update(signature=signature, elapsed=time.monotonic()-start, engine='omnivoice',
                        subjective_review='not_listened', text=row['text'])
        atomic_json(proof, evidence)
        made += 1
        print(f'  OK {evidence["seconds"]:.2f}s, generated {evidence["elapsed"]:.1f}s', flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['inventory','render'])
    parser.add_argument('--limit', type=int, default=0)
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    if args.action == 'inventory':
        data = inventory()
        (OUT / 'manifest.json').write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n')
        print(json.dumps({'lines':len(data['entries']), 'existing':sum(bool(r.get('existing')) for r in data['entries']), 'dynamic':len(data['dynamic'])}))
    else: render(args.limit)

if __name__ == '__main__': main()
