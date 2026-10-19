"""Minimal reader for the Unity YAML subset used by .anim / .controller assets.

Values stay raw strings (numbers are NOT converted) so nothing is lost on read;
callers convert with float()/int() where they need numbers.
Mappings -> dict (insertion ordered), sequences -> list, flow {..}/[..] supported.
"""
import re

DOC_RE = re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$')


def split_documents(text):
    """Return list of (classID, fileID, body_lines) for a Unity YAML file."""
    docs = []
    cur = None
    for line in text.split('\n'):
        m = DOC_RE.match(line)
        if m:
            cur = (int(m.group(1)), int(m.group(2)), [])
            docs.append(cur)
        elif cur is not None:
            cur[2].append(line)
    return docs


def _split_flow(s):
    """Split a flow body on top-level commas."""
    parts, depth, cur = [], 0, ''
    for ch in s:
        if ch in '{[':
            depth += 1
        elif ch in '}]':
            depth -= 1
        if ch == ',' and depth == 0:
            parts.append(cur)
            cur = ''
        else:
            cur += ch
    if cur.strip():
        parts.append(cur)
    return parts


def parse_flow(s):
    s = s.strip()
    if s.startswith('{'):
        body = s[1:-1].strip()
        d = {}
        if not body:
            return d
        for part in _split_flow(body):
            k, _, v = part.partition(':')
            d[k.strip()] = parse_flow(v)
        return d
    if s.startswith('['):
        body = s[1:-1].strip()
        return [parse_flow(p) for p in _split_flow(body)] if body else []
    return s


def _scalar(v):
    v = v.strip()
    if v.startswith('{') or v.startswith('['):
        return parse_flow(v)
    if len(v) >= 2 and v[0] == v[-1] and v[0] in '"\'':
        return v[1:-1]
    return v


def parse_block(lines):
    """Parse the body of one Unity document into a nested dict."""
    items = []
    for ln in lines:
        if not ln.strip():
            continue
        indent = len(ln) - len(ln.lstrip(' '))
        items.append((indent, ln.strip()))
    pos = [0]

    def parse_node(indent):
        # decide sequence or mapping by first line at this indent
        _, first = items[pos[0]]
        if first.startswith('- ') or first == '-':
            return parse_seq(indent)
        return parse_map(indent)

    def parse_seq(indent):
        out = []
        while pos[0] < len(items):
            ind, txt = items[pos[0]]
            if ind != indent or not (txt.startswith('- ') or txt == '-'):
                break
            rest = txt[1:].strip()
            if rest == '':
                pos[0] += 1
                out.append(parse_node(items[pos[0]][0]))
                continue
            if ':' in rest and not rest.startswith('{') and not rest.startswith('['):
                # inline first key of a mapping; rewrite the line as a mapping line
                child_indent = indent + 2
                items[pos[0]] = (child_indent, rest)
                out.append(parse_map(child_indent))
            else:
                pos[0] += 1
                out.append(_scalar(rest))
        return out

    def parse_map(indent):
        d = {}
        while pos[0] < len(items):
            ind, txt = items[pos[0]]
            if ind != indent or txt.startswith('- '):
                break
            key, _, val = txt.partition(':')
            key = key.strip()
            val = val.strip()
            pos[0] += 1
            if val == '':
                # nested block (map, or seq at same/greater indent) or empty scalar
                if pos[0] < len(items):
                    nind, ntxt = items[pos[0]]
                    if ntxt.startswith('- ') and nind >= indent:
                        d[key] = parse_seq(nind)
                        continue
                    if nind > indent:
                        d[key] = parse_node(nind)
                        continue
                d[key] = ''
            else:
                d[key] = _scalar(val)
        return d

    if not items:
        return {}
    return parse_map(items[0][0])


def parse_file(path):
    """Return list of dicts: {classID, fileID, type, data} for each document."""
    with open(path, encoding='utf-8') as f:
        text = f.read()
    out = []
    for cid, fid, body in split_documents(text):
        data = parse_block(body)
        typ = next(iter(data.keys())) if data else ''
        out.append({'classID': cid, 'fileID': fid, 'type': typ, 'data': data[typ] if typ else {}})
    return out
