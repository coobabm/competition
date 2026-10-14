"""Read-only offline ASR spot/check pass. Run with VoiceStudio's existing Python venv."""
import argparse
from difflib import SequenceMatcher
import hashlib
import json
from pathlib import Path
import re
import sys

from voiceover import OUT, key


def comparable(text):
    return key(text).translate(str.maketrans({'她':'他','它':'他','得':'的','地':'的','7':'七'}))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--model', choices=['base','large-v3'], default='base')
    parser.add_argument('--only', nargs='*')
    args = parser.parse_args()
    # Existing package and cached weights only. Never silently download an ASR model.
    from faster_whisper import WhisperModel
    model = WhisperModel(args.model, device='cpu', compute_type='int8', cpu_threads=4, local_files_only=True)
    data = json.loads((OUT/'manifest.json').read_text())
    path = OUT/('asr-'+args.model+'.json')
    results = json.loads(path.read_text()) if path.exists() else {}
    for row in data['entries']:
        name = row['clip']
        if args.only and name not in args.only: continue
        wav = OUT/'takes'/(name+'.wav')
        if not wav.exists(): continue
        digest = hashlib.sha256(wav.read_bytes()).hexdigest()
        if results.get(name,{}).get('sha256') == digest: continue
        segments, info = model.transcribe(str(wav), language='zh', beam_size=3, condition_on_previous_text=False)
        heard = ''.join(s.text for s in segments)
        score = SequenceMatcher(None, comparable(row['text']), comparable(heard)).ratio()
        results[name] = {'expected':row['text'],'heard':heard,'similarity':score,
                         'sha256':digest,'model':args.model,'review': 'needs_text_review' if score < .8 else 'asr_match'}
        temp = path.with_suffix('.tmp'); temp.write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n'); temp.replace(path)
        print(f'{name} {score:.2f} {heard}',flush=True)
    print(f'Checked {len(results)} takes; ASR does not certify performance quality.',flush=True)

if __name__ == '__main__': main()
