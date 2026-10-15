"""
灵光 sound set: natural, deep, "sinking" UI sounds (felt piano, wood, glass, water, air, embers, sub drops),
rendered offline with real reverb. Writes Assets/LingGuangV05/Resources/XgSfx/<Id>_v<n>.wav for every
XgJuice.Sfx.Id (3 variants each) and a preview reel.

    cd Tools/XingGuang/sfx && python3 -m venv .venv && .venv/bin/pip install numpy scipy
    .venv/bin/python make_sfx.py [--preview out.wav]

Everything is deterministic (seeded), so a re-run gives the same files.
"""
import argparse
import os
import wave

import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR = 48000
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "LingGuangV05", "Resources", "XgSfx")
IDS = ["Click", "Ding", "Clack", "Whoosh", "Fan", "Tick", "Stamp", "Fanfare", "Buzz", "Thud", "Charge", "Unlock", "Coin", "Break", "Tier", "Swoosh"]


# ───────────── building blocks ─────────────

def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def env_exp(n, decay, attack=0.003):
    """Fast attack, exponential decay (decay = seconds to -60 dB)."""
    t = np.arange(n) / SR
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    return a * np.exp(-6.9 * t / decay)


def glide(f0, f1, seconds, curve=3.0):
    """Phase of a sine whose pitch falls (or rises) exponentially from f0 to f1."""
    t = t_axis(seconds)
    k = 1 - np.exp(-curve * t / seconds)
    k /= k[-1] if k[-1] != 0 else 1
    f = f0 * (f1 / f0) ** k
    return 2 * np.pi * np.cumsum(f) / SR


def lp(x, hz, order=2):
    return sosfilt(butter(order, hz, "low", fs=SR, output="sos"), x)


def hp(x, hz, order=2):
    return sosfilt(butter(order, hz, "high", fs=SR, output="sos"), x)


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def pad(x, seconds):
    n = int(SR * seconds)
    return np.pad(x, (0, max(0, n - len(x))))[:n] if x.ndim == 1 else np.pad(x, ((0, max(0, n - x.shape[0])), (0, 0)))[:n]


def place(buf, x, at):
    i = int(SR * at)
    end = min(len(buf), i + len(x))
    buf[i:end] += x[: end - i]
    return buf


def felt_note(freq, seconds, rng, bright=1.0, decay=1.6):
    """A soft felt-piano / celesta tone: slightly inharmonic partials, higher ones die sooner, a felt thump on top."""
    t = t_axis(seconds)
    n = len(t)
    out = np.zeros(n)
    for k, amp in [(1, 1.0), (2, 0.42 * bright), (3, 0.18 * bright), (4, 0.09 * bright), (5, 0.05 * bright)]:
        f = freq * k * (1 + 0.0004 * k * k)
        out += amp * np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * env_exp(n, decay / (0.6 + 0.4 * k), 0.004)
    thump = lp(rng.standard_normal(n), 900) * env_exp(n, 0.04, 0.001) * 0.25
    return out + thump


def bell(freq, seconds, rng, decay=1.2):
    """Glass / small bell: the partial ratios of a struck bar."""
    t = t_axis(seconds)
    n = len(t)
    out = np.zeros(n)
    for ratio, amp, d in [(1, 1.0, 1.0), (2.756, 0.45, 0.55), (5.404, 0.22, 0.3), (8.933, 0.1, 0.18)]:
        out += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6.28)) * env_exp(n, decay * d, 0.0015)
    return out


def wood(freqs, seconds, rng, decay=0.09):
    """A wooden knock: a short noise strike ringing a few body modes."""
    n = int(SR * seconds)
    strike = rng.standard_normal(n) * env_exp(n, 0.006, 0.0005)
    out = np.zeros(n)
    for i, f in enumerate(freqs):
        out += bp(strike, f * 0.94, f * 1.06, 2) * (1.0 / (1 + i * 0.6))
    t = np.arange(n) / SR
    for i, f in enumerate(freqs):
        out += 0.35 / (1 + i) * np.sin(2 * np.pi * f * t) * env_exp(n, decay / (1 + i * 0.5), 0.0008)
    return out


def sub_drop(f0, f1, seconds, drive=2.5):
    """The 'sinking' boom: a sine falling in pitch, with a saturated copy so laptop speakers still hear it."""
    ph = glide(f0, f1, seconds, 3.2)
    n = len(ph)
    e = env_exp(n, seconds * 1.1, 0.006)
    s = np.sin(ph) * e
    harm = lp(np.tanh(drive * s) - np.tanh(drive) * s / max(1, drive), 420)
    return s + 0.55 * harm


def noise_sweep(f_from, f_to, seconds, rng, width=0.6, shape=None):
    """Air moving: band-passed noise whose centre glides; processed in short blocks."""
    n = int(SR * seconds)
    x = rng.standard_normal(n)
    out = np.zeros(n)
    block = 512
    zi_like = None
    centres = f_from * (f_to / f_from) ** (np.arange(0, n, block) / n)
    for b, c in zip(range(0, n, block), centres):
        lo, hi = max(40, c * (1 - width / 2)), min(SR / 2 - 100, c * (1 + width / 2))
        seg = x[max(0, b - 2048): b + block]
        y = bp(seg, lo, hi, 2)[-min(block, n - b):]
        out[b: b + len(y)] = y
    env = shape if shape is not None else np.sin(np.linspace(0, np.pi, n)) ** 1.5
    return out * env


def bubble(freq, seconds, rng):
    """A water bubble: a sine whose pitch rises fast as it pops, gone in a few tens of ms."""
    n = int(SR * seconds)
    t = np.arange(n) / SR
    f = freq * (1 + 2.2 * t / seconds)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph) * env_exp(n, seconds * 0.9, 0.002)


def crackle(seconds, rng, density=60, decay_to=0.0):
    """Embers: sparse clicks of random size, thinning out."""
    n = int(SR * seconds)
    out = np.zeros(n)
    count = int(density * seconds)
    for _ in range(count):
        i = int(rng.uniform(0, 1) ** 1.6 * n)
        if i >= n - 200:
            continue
        L = int(rng.uniform(40, 160))
        out[i: i + L] += rng.standard_normal(L) * np.exp(-np.arange(L) / (L / 4)) * rng.uniform(0.2, 1)
    return bp(out, 1200, 9000)


# ───────────── space ─────────────

_ir_cache = {}


def reverb_ir(rt60, seed=7, predelay=0.012, dark=4500):
    key = (rt60, seed, predelay, dark)
    if key in _ir_cache:
        return _ir_cache[key]
    rng = np.random.default_rng(seed)
    n = int(SR * rt60 * 1.1)
    t = np.arange(n) / SR
    decay = np.exp(-6.9 * t / rt60)
    irs = []
    for ch in range(2):
        x = rng.standard_normal(n) * decay
        # Darker as it decays: blend a low-passed copy in over time.
        x = x * (1 - t / t[-1]) ** 2 + lp(x, dark * 0.35) * (1 - (1 - t / t[-1]) ** 2)
        x = hp(x, 120)
        x = np.concatenate([np.zeros(int(SR * predelay)), x])
        irs.append(x / np.sqrt(np.sum(x ** 2)))
    ir = np.stack(irs, 1)
    _ir_cache[key] = ir
    return ir


def space(dry, rt60=1.2, wet=0.25, width=1.0):
    """Mono dry → stereo with a reverb tail."""
    ir = reverb_ir(rt60)
    tail = np.stack([fftconvolve(dry, ir[:, c])[: len(dry) + len(ir)] for c in range(2)], 1)
    out = np.zeros_like(tail)
    out[: len(dry), 0] += dry
    out[: len(dry), 1] += dry
    mid = tail.mean(1, keepdims=True)
    tail = mid + (tail - mid) * width
    return out * (1 - wet * 0.5) + tail * wet * 2.2


def pan(st, p):
    """p −1 (left) … 1 (right), constant power, per sample when p is an array."""
    p = np.asarray(p)
    a = (p + 1) * np.pi / 4
    st = st.copy()
    st[:, 0] *= np.cos(a) * 1.414
    st[:, 1] *= np.sin(a) * 1.414
    return st


def finish(st, peak, seconds=None):
    st = np.atleast_2d(st)
    if st.shape[1] != 2:
        st = np.stack([st[:, 0], st[:, 0]], 1) if st.shape[0] > st.shape[1] else np.stack([st[0], st[0]], 1)
    for c in range(2):
        st[:, c] = hp(st[:, c], 28, 2)
    # Trim silence at the tail, then fade the last 30 ms.
    level = np.abs(st).max(1)
    above = np.where(level > 1e-3 * max(1e-9, level.max()))[0]
    end = above[-1] + 1 if len(above) else len(st)
    if seconds:
        end = min(end, int(SR * seconds))
    st = st[:end]
    fade = min(len(st), int(SR * 0.03))
    st[-fade:] *= np.linspace(1, 0, fade)[:, None]
    st = np.tanh(st / max(1e-9, np.abs(st).max()) * 1.15) / np.tanh(1.15)
    return st * peak


# ───────────── the sounds ─────────────
# v: variant 0–2 (small pitch / timing changes so repeats do not sound mechanical). Peaks are set by role:
# frequent small sounds sit low, rewards and big moments sit high.

def s_click(v, rng):
    p = [1.0, 1.04, 0.97][v]
    x = wood([1050 * p, 2350 * p, 3900 * p], 0.12, rng, 0.05) * 0.5
    x += np.sin(glide(190 * p, 130 * p, 0.12)) * env_exp(int(SR * 0.12), 0.06, 0.001) * 0.6
    return finish(space(x, 0.35, 0.10, 0.6), 0.42)


def s_tick(v, rng):
    p = [1.0, 1.06, 0.95][v]
    x = bell(3100 * p, 0.09, rng, 0.06) * 0.6 + hp(rng.standard_normal(int(SR * 0.09)), 4000) * env_exp(int(SR * 0.09), 0.006, 0.0003) * 0.3
    return finish(space(x, 0.4, 0.12, 0.8), 0.22)


def s_thud(v, rng):
    p = [1.0, 0.95, 1.05][v]
    n = int(SR * 0.55)
    x = sub_drop(100 * p, 46 * p, 0.55, 2.2)
    x += lp(rng.standard_normal(n), 380) * env_exp(n, 0.07, 0.001) * 0.7
    x += wood([210 * p, 470 * p], 0.55, rng, 0.12) * 0.25
    return finish(space(x, 0.6, 0.12, 0.5), 0.62)


def s_ding(v, rng):
    note = [659.25, 739.99, 587.33][v]  # E5, F#5, D5
    x = felt_note(note, 1.6, rng, 0.8, 1.8) * 0.8 + bell(note * 2, 1.6, rng, 0.8) * 0.12
    return finish(space(x, 1.8, 0.28, 1.0), 0.5)


def s_unlock(v, rng):
    root = [392.0, 415.3, 369.99][v]  # G4 / G#4 / F#4
    x = np.zeros(int(SR * 2.0))
    place(x, sub_drop(105, 38, 1.3, 2.6) * 0.75, 0)
    place(x, felt_note(root, 1.8, rng, 0.9, 2.0) * 0.55, 0.0)
    place(x, felt_note(root * 1.5, 1.8, rng, 0.8, 2.0) * 0.45, 0.045)
    place(x, felt_note(root * 2, 1.7, rng, 0.6, 1.8) * 0.3, 0.09)
    place(x, bell(root * 4, 1.2, rng, 0.9) * 0.06, 0.1)
    return finish(space(x, 2.2, 0.3, 1.0), 0.8)


def s_coin(v, rng):
    a, b = [(987.77, 1318.5), (1046.5, 1396.9), (932.33, 1244.5)][v]
    x = np.zeros(int(SR * 1.0))
    place(x, bell(a, 0.9, rng, 0.7) * 0.5, 0)
    place(x, bell(b, 0.9, rng, 0.8) * 0.55, 0.07)
    return finish(space(x, 1.4, 0.25, 1.0), 0.45)


def s_fanfare(v, rng):
    roots = [261.63, 293.66, 246.94]  # C, D, B
    r = roots[v]
    chord = [1, 1.25, 1.5, 1.875, 2.25]  # maj9, just-ish
    x = np.zeros(int(SR * 3.0))
    place(x, sub_drop(80, 34, 1.8, 2.4) * 0.8, 0)
    for i, c in enumerate(chord):
        place(x, felt_note(r * c, 2.6, rng, 0.75, 2.6) * (0.5 - i * 0.05), 0.02 + i * 0.065)
    # A soft pad swelling under the roll.
    n = int(SR * 2.6)
    t = np.arange(n) / SR
    padv = sum(np.sin(2 * np.pi * r * c * t * (1 + d)) for c in (1, 1.5, 2) for d in (-0.003, 0.003)) / 6
    padv = lp(padv, 1800) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.25
    place(x, padv, 0.15)
    place(x, bell(r * 4.5, 1.6, rng, 1.0) * 0.07, 0.36)
    return finish(space(x, 2.8, 0.32, 1.0), 0.85)


def s_whoosh(v, rng):
    d = [0.5, 0.46, 0.55][v]
    n = int(SR * d)
    x = noise_sweep(2600, 420, d, rng, 0.7) * 0.9
    x += np.sin(glide(70, 45, d)) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.35
    st = space(x, 0.8, 0.15, 1.0)
    st = pan(st, np.linspace(-0.6, 0.6, len(st)))
    return finish(st, 0.4)


def s_swoosh(v, rng):
    d = [0.42, 0.4, 0.46][v]
    n = int(SR * d)
    shape = np.linspace(0, 1, n) ** 2 * (1 - np.linspace(0, 1, n) ** 8)
    x = noise_sweep(380, 3200, d, rng, 0.6, shape) * 0.8
    x += np.sin(glide(330, 880, d, -1.5)) * shape * 0.12
    st = space(x, 0.8, 0.15, 1.0)
    st = pan(st, np.linspace(0.5, -0.5, len(st)))
    return finish(st, 0.36)


def s_clack(v, rng):
    p = [1.0, 0.93, 1.07][v]
    x = wood([640 * p, 1520 * p, 2880 * p], 0.25, rng, 0.11)
    return finish(space(x, 0.5, 0.14, 0.7), 0.42)


def s_stamp(v, rng):
    p = [1.0, 0.96, 1.04][v]
    n = int(SR * 0.9)
    x = pad(sub_drop(120 * p, 36 * p, 0.85, 3.0), 0.9) * 0.9
    x += bp(rng.standard_normal(n), 700, 3500) * env_exp(n, 0.05, 0.0008) * 0.55
    x += wood([180 * p, 410 * p], 0.9, rng, 0.15) * 0.3
    return finish(space(x, 0.9, 0.16, 0.6), 0.72)


def s_buzz(v, rng):
    p = [1.0, 0.97, 1.03][v]
    x = np.zeros(int(SR * 0.7))
    for i, at in enumerate((0, 0.11)):
        n = int(SR * 0.35)
        k = np.sin(glide(92 * p, 58 * p, 0.35)) * env_exp(n, 0.22, 0.002) + lp(rng.standard_normal(n), 300) * env_exp(n, 0.04, 0.001) * 0.4
        place(x, k * (1 - i * 0.2), at)
    # A dull minor second on top: "no", not a siren.
    place(x, felt_note(220 * p, 0.6, rng, 0.4, 0.5) * 0.18 + felt_note(233.08 * p, 0.6, rng, 0.4, 0.5) * 0.18, 0.0)
    return finish(space(x, 0.6, 0.12, 0.6), 0.55)


def s_charge(v, rng):
    d = [1.0, 0.95, 1.08][v]
    n = int(SR * d)
    shape = np.linspace(0, 1, n) ** 1.6
    x = noise_sweep(300, 4200, d, rng, 0.5, shape) * 0.45
    ph = glide(180, 720, d, -2.0)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * np.arange(n) / SR)
    x += np.sin(ph * vib) * shape * 0.25
    buf = np.zeros(int(SR * (d + 1.0)))
    place(buf, x, 0)
    place(buf, bell(1046.5, 1.0, rng, 0.9) * 0.3, d - 0.02)
    return finish(space(buf, 1.4, 0.25, 1.0), 0.6)


def s_tier(v, rng):
    base = [523.25, 587.33, 493.88][v]
    x = np.zeros(int(SR * 1.0))
    for i, m in enumerate((1, 1.25, 1.5)):
        place(x, bubble(base * m * 0.5, 0.05, rng) * 0.5, i * 0.07)
        place(x, felt_note(base * m, 0.9, rng, 0.5, 0.9) * 0.28, i * 0.07 + 0.01)
    place(x, sub_drop(70, 40, 0.6, 2.0) * 0.4, 0)
    return finish(space(x, 1.2, 0.24, 1.0), 0.6)


def s_break(v, rng):
    p = [1.0, 0.94, 1.06][v]
    x = np.zeros(int(SR * 1.5))
    place(x, sub_drop(160 * p, 34 * p, 1.2, 2.8) * 0.9, 0)
    place(x, crackle(1.3, rng, 70) * 0.35, 0.02)
    place(x, felt_note(146.83 * p, 1.0, rng, 0.5, 0.9) * 0.2, 0)
    return finish(space(x, 1.2, 0.2, 1.0), 0.66)


def s_fan(v, rng):
    d = [0.7, 0.65, 0.75][v]
    n = int(SR * d)
    x = lp(rng.standard_normal(n), 900) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.6
    x += np.sin(2 * np.pi * 118 * np.arange(n) / SR) * np.sin(np.linspace(0, np.pi, n)) ** 2 * 0.08
    return finish(space(x, 0.6, 0.15, 1.0), 0.3)


SOUNDS = {
    "Click": s_click, "Ding": s_ding, "Clack": s_clack, "Whoosh": s_whoosh, "Fan": s_fan, "Tick": s_tick,
    "Stamp": s_stamp, "Fanfare": s_fanfare, "Buzz": s_buzz, "Thud": s_thud, "Charge": s_charge,
    "Unlock": s_unlock, "Coin": s_coin, "Break": s_break, "Tier": s_tier, "Swoosh": s_swoosh,
}


def write_wav(path, st):
    data = (np.clip(st, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview", help="also write a reel of every sound (variant 0) to this path")
    ap.add_argument("--out", default=OUT)
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    reel = []
    for i, name in enumerate(IDS):
        lengths = set()
        for v in range(3):
            rng = np.random.default_rng(1000 + i * 17 + v)
            st = SOUNDS[name](v, rng)
            # Variants always differ in length (XgTrainingPresentationQa checks it); a few ms of silence is inaudible.
            while len(st) in lengths:
                st = np.concatenate([st, np.zeros((64, 2))])
            lengths.add(len(st))
            write_wav(os.path.join(args.out, f"{name}_v{v}.wav"), st)
            if v == 0:
                reel.append(st)
                reel.append(np.zeros((int(SR * 0.45), 2)))
        print(f"{name:8s} {len(st) / SR:4.2f}s peak {np.abs(st).max():.2f}")
    if args.preview:
        write_wav(args.preview, np.concatenate(reel))


if __name__ == "__main__":
    main()
