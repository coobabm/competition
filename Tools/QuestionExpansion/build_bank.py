"""Builds XgQuestionBank.cs from data/*.json, after validating every item.

python -I build_bank.py <Unity project root (for duplicate checks)>
Writes ./XgQuestionBank.cs next to this script and prints a report. Exits 1 on any error.
"""
import json, os, re, sys, datetime
HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, 'data')
errors, report = [], []
def err(msg): errors.append(msg)
def load(name):
    p = os.path.join(DATA, name)
    if not os.path.exists(p): err('missing data/' + name); return None
    data = json.load(open(p, encoding='utf-8'))
    if isinstance(data, list):
        for i, row in enumerate(data):
            if isinstance(row, dict) and row.get('since'):
                try:
                    datetime.datetime.strptime(str(row['since']), '%Y%m%d')
                except (ValueError, TypeError):
                    err(f'{name}[{i}]: invalid since date')
    return data
def cs(s):
    return '"' + s.replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n').replace('\r', '') + '"'
def arr1(items): return '{' + ','.join(cs(x) for x in items) + '}'
def ints(items): return '{' + ','.join(str(int(x)) for x in items) + '}'
def jag(rows): return '{' + ',\n            '.join('new[]' + arr1(r) for r in rows) + '}'
def jagi(rows): return '{' + ','.join('new[]' + ints(r) for r in rows) + '}'
def cjk(s): return sum(1 for ch in s if '\u4e00' <= ch <= '\u9fff')

root = sys.argv[1] if len(sys.argv) > 1 else None
PACK_FILES = {
    'sentence': ['Desktop/MiniGames/XgStageLessonGames.cs'], 'translation': ['Desktop/MiniGames/XgStageLessonGames.cs'], 'compare': ['Desktop/MiniGames/XgStageLessonGames.cs'],
    'odd': ['Desktop/MiniGames/XgPackPuzzles.cs'], 'chinglish': ['Desktop/MiniGames/XgPackContentGames.cs'], 'five': ['Desktop/MiniGames/XgPackContentGames.cs'],
    'seven': ['Desktop/MiniGames/XgPackContentGames.cs'], 'quote': ['Core/XgTeachingPoetry.cs'], 'undercover': ['Desktop/MiniGames/XgSocialGames.Deduction.cs'],
    'danmu': ['Core/XgTeachingDanmu.cs'], 'spam.json': ['Core/XgMemes.cs'], 'review.json': ['Core/XgMemes.cs'], 'headline.json': ['Core/XgMemes.cs']}
patch_text = open(os.path.join(HERE, 'apply_question_expansion.py'), encoding='utf-8').read()
_cache = {}
def pack_text(where):
    key = re.split(r'\[', where)[0]
    if key not in _cache:
        t = patch_text
        for f in PACK_FILES.get(key, []):
            if root: t += open(os.path.join(root, 'Assets/LingGuangV05/XingGuang', f), encoding='utf-8-sig').read()
        _cache[key] = t
    return _cache[key]
existing = ''.join(open(os.path.join(root, 'Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgPackPuzzles.cs'), encoding='utf-8-sig').read() for _ in [0] if root)
def dup(text, where):
    if text and cs(text)[1:-1] in pack_text(where): err(f'{where}: already in this pack: {text}')
def uniq(keys, where):
    seen = set()
    for k in keys:
        if k in seen: err(f'{where}: duplicate {k}')
        seen.add(k)

out = []
# ── sentence ──
d = load('sentence.json') or []
for i, x in enumerate(d):
    w = f'sentence[{i}]'
    zh, en, b = x['zh'], x['en'], x['bad']
    if not (2 <= len(zh) <= 4 and 2 <= len(en) <= 4): err(w + ' token count')
    if not (-1 <= b < min(len(zh), len(en))): err(w + ' bad index')
    if (b == -1) != (x['fix'] == '没错') or (b == -1) != (x['fixEn'] == 'No mistake'): err(w + ' fix/no-mistake mismatch')
    if any(len(t) > 7 for t in zh) or any(len(t) > 22 for t in en): err(w + ' token too long')
    dup(''.join(zh), w)
uniq([''.join(x['zh']) for x in d], 'sentence')
out.append('        public static readonly string[][] SentenceZh=' + jag([x['zh'] for x in d]) + ';')
out.append('        public static readonly string[][] SentenceEn=' + jag([x['en'] for x in d]) + ';')
out.append('        public static readonly int[] SentenceBad=' + ints([x['bad'] for x in d]) + ';')
out.append('        public static readonly string[] SentenceFix=' + arr1([x['fix'] for x in d]) + ',SentenceFixEn=' + arr1([x['fixEn'] for x in d]) + ';')
out.append('        public static readonly int[] SentenceSince=' + ints([x['since'] for x in d]) + ';')
report.append(f"短句找错字 +{len(d)}  (on 0901: +{sum(1 for x in d if x['since'] <= 20160901)}, correct {sum(1 for x in d if x['bad'] == -1)})")

# ── odd one out ──
d = load('odd.json') or []
for i, x in enumerate(d):
    w = f'odd[{i}]'
    if len(x['zh']) != 4 or len(x['en']) != 4 or len(set(x['zh'])) != 4: err(w + ' needs 4 distinct options')
    if any(len(t) > 12 for t in x['zh']) or any(len(t) > 28 for t in x['en']): err(w + ' option too long')
uniq([tuple(sorted(x['zh'])) for x in d], 'odd')
out.append('        public static readonly string[][] OddZh=' + jag([x['zh'] for x in d]) + ';')
out.append('        public static readonly string[][] OddEn=' + jag([x['en'] for x in d]) + ';')
out.append('        public static readonly int[] OddSince=' + ints([x['since'] for x in d]) + ';')
report.append(f"找不同 +{len(d)}  (on 0701: +{sum(1 for x in d if x['since'] <= 20160701)})")

# ── chinglish ──
d = load('chinglish.json') or []
for i, x in enumerate(d):
    w = f'chinglish[{i}]'
    if ' '.join(x['spans']) != x['wrong']: err(w + ' spans do not join to wrong')
    if not (2 <= len(x['spans']) <= 4) or any(len(s) > 16 for s in x['spans']): err(w + ' spans count/length')
    if not x['errorSpans'] or any(not (0 <= e < len(x['spans'])) for e in x['errorSpans']): err(w + ' errorSpans')
    opts = [x['correct'], x['wrong1'], x['wrong2'], x['wrong3']]
    if len(set(opts)) != 4: err(w + ' rewrites not distinct')
    dup(x['wrong'], w)
uniq([x['wrong'].lower() for x in d], 'chinglish')
out.append('        public static readonly string[][] EnglishZh=' + jag([[x['wrong'], x['meaningZh'], x['correct'], x['wrong1'], x['wrong2'], x['wrong3']] for x in d]) + ';')
out.append('        public static readonly string[][] EnglishSpans=' + jag([x['spans'] for x in d]) + ';')
out.append('        public static readonly int[][] EnglishErrorSpans=' + jagi([x['errorSpans'] for x in d]) + ';')
out.append('        public static readonly string[] EnglishContextEn=' + arr1([x['contextEn'] for x in d]) + ';')
out.append('        public static readonly string[] EnglishReasonZh=' + arr1([x['reasonZh'] for x in d]) + ';')
out.append('        public static readonly string[] EnglishReasonEn=' + arr1([x['reasonEn'] for x in d]) + ';')
report.append(f'中式英语改错 +{len(d)}')

# ── translation ──
d = load('translation.json') or []
for i, x in enumerate(d):
    w = f'translation[{i}]'; words = x['en'].split(' ')
    if x['grade'] not in (0, 1, 2): err(w + ' grade')
    if x['grade'] == 1 and not (0 <= x['wrongWord'] < len(words) <= 10): err(w + ' wrongWord/words')
    if x['grade'] != 1 and x['wrongWord'] != -1: err(w + ' wrongWord must be -1')
    dup(x['zh'], w)
uniq([x['zh'] for x in d], 'translation')
out.append('        public static readonly string[] TranslationSources=' + arr1([x['zh'] for x in d]) + ';')
out.append('        public static readonly string[] TranslationCandidates=' + arr1([x['en'] for x in d]) + ';')
out.append('        public static readonly int[] TranslationGrades=' + ints([x['grade'] for x in d]) + ',TranslationWrongWords=' + ints([x['wrongWord'] for x in d]) + ';')
report.append(f'翻译打分 +{len(d)}  grades ' + str([sum(1 for x in d if x["grade"] == g) for g in (0, 1, 2)]))

# ── compare ──
d = load('compare.json') or []
out.append('        public static readonly int[] CompareSince=' + ints([x.get('since', 0) for x in d]) + ';')
for i, x in enumerate(d):
    w = f'compare[{i}]'
    if any(not x[k] for k in ('q', 'qEn', 'good', 'goodEn', 'bad', 'badEn')): err(w + ' empty field')
    if cjk(x['good']) > 30 or cjk(x['bad']) > 30: err(w + ' answer too long')
    dup(x['q'], w)
uniq([x['q'] for x in d], 'compare')
for k, n in (('q', 'Q'), ('qEn', 'QEn'), ('good', 'Good'), ('goodEn', 'GoodEn'), ('bad', 'Bad'), ('badEn', 'BadEn')):
    out.append(f'        public static readonly string[] Compare{n}=' + arr1([x[k] for x in d]) + ';')
report.append(f'挑好答案 +{len(d)}')

# ── undercover ──
d = load('undercover.json') or []
for i, x in enumerate(d):
    w = f'undercover[{i}]'
    if len(x['zh']) != 7 or len(x['en']) != 7: err(w + ' needs 7+7 fields')
    elif x['zh'][0] in x['zh'][2] or x['zh'][1] in x['zh'][2]: err(w + ' common mentions a word')
    dup('new[]{' + cs(x['zh'][0]) + ',' + cs(x['zh'][1]), w)
uniq([w for x in d for w in x['zh'][:2]], 'undercover words')
out.append('        public static readonly string[][] Pairs=' + jag([x['zh'] for x in d]) + ';')
out.append('        public static readonly string[][] PairsEn=' + jag([x['en'] for x in d]) + ';')
report.append(f'谁是卧底 +{len(d)}')

# ── poems ──
d = load('poem_lines.json') or {'five': [], 'seven': []}
for bank, n in (('five', 5), ('seven', 7)):
    for i, x in enumerate(d[bank]):
        if len(x['line']) != n or cjk(x['line']) != n: err(f'{bank}[{i}] not {n} chars: {x["line"]}')
        dup(x['line'], f'{bank}[{i}]')
    uniq([x['line'] for x in d[bank]], bank)
out.append('        public static readonly string[] Five=' + arr1([x['line'] for x in d['five']]) + ';')
out.append('        public static readonly string[] Seven=' + arr1([x['line'] for x in d['seven']]) + ';')
report.append(f"唐诗下一字 五言 +{len(d['five'])} 七言 +{len(d['seven'])}")

d = load('poem_quotes.json') or []
for i, x in enumerate(d):
    w = f'quote[{i}]'
    if x['original'] and x['line'] != x['originalLine']: err(w + ' original differs from originalLine')
    if not x['original'] and x['line'] == x['originalLine']: err(w + ' parody equals original')
    if x['line'].split('，')[0] != x['originalLine'].split('，')[0]: err(w + ' first half changed')
    dup(x['line'], w)
uniq([x['line'] for x in d], 'quotes')
out.append('        public static readonly XgPoemQuote[] Quotes={\n            ' + ',\n            '.join(
    f"new XgPoemQuote({cs(x['line'])},{cs(x['lineEn'])},{'true' if x['original'] else 'false'},{cs(x['originalLine'])},{cs(x['originalLineEn'])},{cs(x['source'])},{cs(x['sourceEn'])})" for x in d) + '};')
report.append(f"原诗还是改编 +{len(d)} (original {sum(1 for x in d if x['original'])})")

# ── danmu ──
d = load('danmu.json') or []
for i, x in enumerate(d):
    if x['tone'] not in (0, 1, 2): err(f'danmu[{i}] tone')
    dup(x['text'], f'danmu[{i}]')
uniq([x['text'] for x in d], 'danmu')
out.append('        public static readonly XgDatedDanmu[] Danmu={\n            ' + ',\n            '.join(
    f"new XgDatedDanmu({cs(x['text'])},{cs(x['textEn'])},{x['tone']},{x['since'] or 20160101})" for x in d) + '};')
report.append(f"弹幕 +{len(d)} (on 0701: +{sum(1 for x in d if (x['since'] or 20160101) <= 20160701)}) tones " + str([sum(1 for x in d if x['tone'] == t) for t in (0, 1, 2)]))

# ── phrase banks ──
def phrases(name, field, opening):
    d = load(name) or []
    for i, x in enumerate(d):
        if not isinstance(x['yes'], bool): err(f'{name}[{i}] yes')
        dup(x['text'], f'{name}[{i}]')
    uniq([x['text'] for x in d], name)
    def one(x):
        trick = bool(x.get('trick')); level = 3 if trick else int(x.get('level', 1))
        return (f"new XgPhrase{{text={cs(x['text'])},textEn={cs(x.get('textEn', ''))},yes={'true' if x['yes'] else 'false'},why={cs(x['why'])},whyEn={cs(x.get('whyEn', ''))},"
                f"source={cs(x.get('source', ''))},since={x.get('since') or 20150101},level={level},trick={'true' if trick else 'false'}}}")
    out.append(f'        public static readonly XgPhrase[] {field}={{\n            ' + ',\n            '.join(one(x) for x in d) + '};')
    report.append(f"{field} +{len(d)} (on {opening}: +{sum(1 for x in d if (x.get('since') or 20150101) <= opening)}, yes {sum(1 for x in d if x['yes'])})")
phrases('spam.json', 'Spam', 20160601)
phrases('review.json', 'Review', 20160901)
phrases('headline.json', 'Headline', 20160701)

# ── idioms (chain must stay closed) ──
d = (load('idioms.json') or {'words': []})['words']
m = re.search(r'class XgIdiomPack.*?Words=\{(.*?)\};', existing, re.S)
base = re.findall(r'"([^"]+)"', m.group(1)) if m else []
allw = base + d
for wd in d:
    if len(wd) != 4: err('idiom not 4 chars: ' + wd)
uniq(allw, 'idioms')
lonely = [wd for wd in allw if not any(o != wd and o[0] == wd[-1] for o in allw)]
if lonely: err('idioms without successor: ' + ' '.join(lonely))
out.append('        public static readonly string[] Idioms=' + arr1(d) + ';')
report.append(f'成语接龙 +{len(d)} (combined {len(allw)}, no-successor {len(lonely)})')

src = '''using System;
namespace LingGuangV05.XingGuang
{
    // Generated by 发布材料/题库扩充_20261021/build_bank.py from data/*.json. Edit the JSON and rebuild, not this file.
    // 2026-10-21: extra teaching questions so every text pack has ~100; each pack class appends these in its static constructor.
    public static class XgQuestionBank
    {
        public static T[] Join<T>(T[] a,T[] b){var r=new T[a.Length+b.Length];Array.Copy(a,r,a.Length);Array.Copy(b,0,r,a.Length,b.Length);return r;}
''' + '\n'.join(out) + '''
    }
}
'''
print('\n'.join(report))
if errors:
    print('\nERRORS (' + str(len(errors)) + '):\n  ' + '\n  '.join(errors[:80])); sys.exit(1)
open(os.path.join(HERE, 'XgQuestionBank.cs'), 'w', encoding='utf-8', newline='\n').write(src)
print('\nOK: XgQuestionBank.cs written')
