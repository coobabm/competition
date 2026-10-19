#!/usr/bin/env python3
"""Synthesizes every DreamOS-slot sound for the desktop UI from scratch (numpy + stdlib wave only).

Usage:  python3 make_audio.py [--out <DreamOS Audio folder>] [--only name,name]
Everything is deterministic (fixed seeds). Files are overwritten in place, same names as before.
"""
import argparse, os, wave
import numpy as np

DEFAULT_OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
    "../../../Assets/LingGuangV05/DesktopBridge/OSArt/Audio"))

# ----------------------------------------------------------------- helpers
def t_axis(n, sr): return np.arange(n) / sr

def expdec(n, sr, tau, attack=0.0004):
    t = t_axis(n, sr)
    e = np.exp(-t / tau)
    if attack > 0: e *= np.minimum(1.0, t / attack)
    return e

def fft_filter(x, sr, lo=None, hi=None, order=3):
    """Zero-phase Butterworth-shaped band/low/high pass applied in the frequency domain."""
    n = len(x); X = np.fft.rfft(x); f = np.fft.rfftfreq(n, 1 / sr); f[0] = 1e-9
    g = np.ones_like(f)
    if lo: g *= 1 / np.sqrt(1 + (lo / f) ** (2 * order))
    if hi: g *= 1 / np.sqrt(1 + (f / hi) ** (2 * order))
    return np.fft.irfft(X * g, n)

def sweep_bandpass(x, sr, f0, f1, q=3.0, fc=None):
    """Resonant band-pass whose centre glides f0->f1 (or follows the array fc); state-variable filter, sample loop."""
    n = len(x); fc = np.geomspace(f0, f1, n) if fc is None else fc
    low = band = 0.0; out = np.empty(n)
    for i in range(n):
        f = 2 * np.sin(np.pi * fc[i] / sr)
        low += f * band
        high = x[i] - low - band / q
        band += f * high
        out[i] = band
    return out

def reverb(stereo, sr, rt=1.2, wet=0.25, seed=7, circular=False, damp=5000):
    """Synthetic-IR reverb (decaying filtered noise, decorrelated per channel) via FFT convolution."""
    rng = np.random.default_rng(seed)
    L = int(rt * 1.6 * sr)
    t = t_axis(L, sr)
    out = np.zeros_like(stereo)
    n = stereo.shape[1]
    N = n if circular else n + L
    res = np.zeros((2, N))
    for c in range(2):
        ir = rng.standard_normal(L) * np.exp(-6.9 * t / rt)
        ir = fft_filter(ir, sr, lo=150, hi=damp, order=2)
        ir[: int(0.012 * sr)] *= np.linspace(0, 1, int(0.012 * sr))   # soft pre-delay onset
        ir /= np.sqrt((ir ** 2).sum()) + 1e-12
        if circular:
            irp = np.zeros(N); irp[:min(L, N)] = ir[:N]
            y = np.fft.irfft(np.fft.rfft(stereo[c]) * np.fft.rfft(irp), N)
        else:
            y = np.fft.irfft(np.fft.rfft(stereo[c], N) * np.fft.rfft(ir, N), N)
        res[c] = y
    if circular: return stereo * (1 - wet * 0.5) + res * wet
    dry = np.zeros((2, N)); dry[:, :n] = stereo
    return dry + res * wet

def bell(sr, n, f, tau, partials=((1, 1.0, 1.0), (2.0, 0.35, 0.5), (3.01, 0.15, 0.3), (4.2, 0.06, 0.2)), attack=0.002):
    t = t_axis(n, sr); y = np.zeros(n)
    for mult, amp, tmul in partials:
        y += amp * np.sin(2 * np.pi * f * mult * t) * np.exp(-t / (tau * tmul))
    return y * np.minimum(1.0, t / attack)

def place(buf, sig, start_s, sr, gain=1.0, pan=0.0):
    """Mix mono/stereo sig into stereo buf at start_s (clipped to buffer)."""
    i = int(start_s * sr)
    if sig.ndim == 1:
        l = np.sqrt(0.5 * (1 - pan)) * sig; r = np.sqrt(0.5 * (1 + pan)) * sig
        sig = np.stack([l, r])
    m = min(sig.shape[1], buf.shape[1] - i)
    if m > 0: buf[:, i:i + m] += gain * sig[:, :m]

def finish(path, st, sr, channels, peak_db, fade_ms=12.0, trim_lead=True):
    """Trim leading silence, short fade-out, peak-normalise, write 16-bit PCM WAV."""
    st = np.asarray(st, dtype=np.float64)
    if st.ndim == 1: st = np.stack([st, st])
    if trim_lead:
        env = np.abs(st).max(axis=0); idx = np.argmax(env > 0.002 * env.max())
        st = st[:, idx:]
    n = st.shape[1]; fn = min(n // 2, int(fade_ms / 1000 * sr))
    st[:, n - fn:] *= np.linspace(1, 0, fn) ** 2
    fi = int(0.0002 * sr); st[:, :fi] *= np.linspace(0, 1, fi)   # 0.2 ms anti-click, still instant
    st -= st.mean(axis=1, keepdims=True) * 0.0
    pk = np.abs(st).max()
    st = st / pk * (10 ** (peak_db / 20))
    pcm = (np.clip(st, -1, 1) * 32767).astype("<i2")
    if channels == 1: pcm = pcm[:1] if True else pcm
    if channels == 1: pcm = ((st[0] + st[1]) * 0.5 * 32767).astype("<i2")[None, :]
    data = pcm.T.copy().tobytes()
    with wave.open(path, "wb") as w:
        w.setnchannels(channels); w.setsampwidth(2); w.setframerate(sr); w.writeframes(data)
    return n / sr

# ----------------------------------------------------------------- sounds
def keystroke(seed, length_s, f_thump, f_noise, tau_n, up_gain, up_at, rattle=0.0, sr=48000):
    rng = np.random.default_rng(seed)
    n = int(length_s * sr); st = np.zeros((2, n))
    def tap(gain, f_th, f_ns, tn, tt):
        m = int(0.25 * sr)
        nz = rng.standard_normal((2, m))
        nz = np.stack([fft_filter(x, sr, lo=f_ns * 0.55, hi=f_ns * 1.7, order=3) for x in nz])
        nz *= expdec(m, sr, tn)
        tt_ = t_axis(m, sr)
        th = np.sin(2 * np.pi * (f_th * (1 + 0.5 * np.exp(-tt_ / 0.012))) * tt_) * expdec(m, sr, tt)
        body = np.sin(2 * np.pi * f_ns * 0.28 * tt_) * expdec(m, sr, 0.010) * 0.25
        return (nz * 1.0 + (th + body)[None, :] * 0.9) * gain
    place(st, tap(1.0, f_thump, f_noise, tau_n, 0.018), 0.0, sr)
    place(st, tap(up_gain, f_thump * 1.5, f_noise * 1.25, tau_n * 0.7, 0.010), up_at, sr)
    if rattle > 0:                                           # little spring/plastic rattle tail
        for k in range(3):
            place(st, tap(rattle * 0.5 ** k, f_thump * 2.2, f_noise * 0.9, 0.003, 0.008), up_at + 0.045 * (k + 1), sr)
    return st

def make_keys(out):
    spec = [  # seed, length, thump Hz, noise centre Hz, noise tau, key-up gain, key-up time, rattle
        (11, 0.21, 190, 3200, 0.0060, 0.35, 0.095, 0.0),
        (12, 0.35, 120, 2200, 0.0090, 0.45, 0.170, 0.18),   # space-bar-ish
        (13, 0.22, 210, 3800, 0.0050, 0.30, 0.100, 0.0),
        (14, 0.31, 150, 2600, 0.0080, 0.40, 0.140, 0.10),   # enter-ish
        (15, 0.46, 105, 1900, 0.0110, 0.50, 0.230, 0.22),   # big key
        (16, 0.22, 170, 3000, 0.0065, 0.32, 0.105, 0.0),
    ]
    for i, s in enumerate(spec, 1):
        st = keystroke(*s)
        st = reverb(st, 48000, rt=0.08, wet=0.06, seed=100 + i)[:, : int(s[1] * 48000)]
        finish(os.path.join(out, "SFX", f"Keyboard_Keystroke{i}.wav"), st, 48000, 2, -2.5, fade_ms=25)

def make_hover(out):
    sr = 48000; n = int(0.16 * sr); t = t_axis(n, sr)
    y = (np.sin(2 * np.pi * 2100 * t) + 0.35 * np.sin(2 * np.pi * 3150 * t)) * expdec(n, sr, 0.018, attack=0.0015)
    st = np.stack([y, y * 0.96])
    finish(os.path.join(out, "SFX", "Hover.wav"), st, sr, 2, -26.5, fade_ms=30)

def make_click(out):
    # The theme's clickSound: the same crisp "哒" as Resources/DesktopSfx/Click (see the desktop UI section below).
    sr = 44100
    write_click(os.path.join(out, "SFX", "Click.wav"), da(sr, int(0.03 * sr)), sr, LK_CLICK, fade_ms=10)

def make_mouse_click(out):
    # Plain click on any clickable control (played on release): the 哒, then a faint, higher, single-contact
    # release "t" 40 ms later, so it reads as one tight mechanical click rather than a flam.
    sr = SFX_SR; y = np.zeros(int(0.075 * sr))
    at(y, da(sr, int(0.03 * sr)), 0.0, sr)
    at(y, snap(int(0.02 * sr), sr, scale=1.22, air=0.8, phases=DA_PH2, seed=DA_SEED + 2), 0.040, sr, 0.26)
    write_click(os.path.join(out, "SFX", "Mouse_Click1.wav"), y, sr, LK_CLICK, fade_ms=12)

def make_notification(out):
    sr = 48000; n = int(3.2 * sr); st = np.zeros((2, n))
    place(st, bell(sr, n, 784.0, 0.55), 0.0, sr, gain=0.9, pan=-0.1)      # G5
    place(st, bell(sr, n, 1046.5, 0.90), 0.20, sr, gain=1.0, pan=0.1)     # C6
    st = reverb(st, sr, rt=1.5, wet=0.30, seed=31)[:, :n]
    finish(os.path.join(out, "SFX", "Notification.wav"), st, sr, 2, -0.3, fade_ms=700)

def make_received(out):
    sr = 48000; n = int(1.2 * sr); st = np.zeros((2, n))
    part = ((1, 1.0, 1.0), (2.76, 0.25, 0.4), (5.4, 0.08, 0.2))           # glassier, softer than the notification
    place(st, bell(sr, n, 987.8, 0.30, part), 0.0, sr, gain=0.9, pan=-0.15)    # B5
    place(st, bell(sr, n, 740.0, 0.45, part), 0.13, sr, gain=0.85, pan=0.15)   # F#5
    st = reverb(st, sr, rt=0.7, wet=0.3, seed=32)[:, :n]
    finish(os.path.join(out, "SFX", "ReceivedMessage.wav"), st, sr, 2, -3.0, fade_ms=200)

def make_sent(out):
    sr = 48000; n = int(0.38 * sr); rng = np.random.default_rng(41); st = np.zeros((2, n))
    m = int(0.24 * sr); t = t_axis(m, sr)
    env = np.sin(np.pi * np.clip(t / 0.24, 0, 1)) ** 1.5
    nz = sweep_bandpass(rng.standard_normal(m), sr, 500, 3200, q=2.2) * env
    nz /= np.abs(nz).max() + 1e-9
    place(st, nz, 0.0, sr, gain=0.55)
    mp = int(0.14 * sr); tp = t_axis(mp, sr)
    f = 620 + 520 * (1 - np.exp(-tp / 0.035))
    pop = np.sin(2 * np.pi * np.cumsum(f) / sr) * expdec(mp, sr, 0.045, 0.0008)
    place(st, pop, 0.11, sr, gain=0.8, pan=0.1)
    finish(os.path.join(out, "SFX", "SentMessage.wav"), st, sr, 2, -3.0, fade_ms=60)

def make_error(out):
    sr = 44100; n = int(1.62 * sr); st = np.zeros((2, n))
    def buzz(f0, f1, dur):
        m = int(dur * sr); t = t_axis(m, sr)
        f = np.linspace(f0, f1, m)
        ph = 2 * np.pi * np.cumsum(f) / sr
        saw = sum(np.sin(ph * k) / k for k in range(1, 12)) * (2 / np.pi)       # band-limited saw
        saw = fft_filter(saw, sr, hi=900, order=3)
        am = 0.78 + 0.22 * np.sin(2 * np.pi * 26 * t)                           # gentle 26 Hz wobble
        e = np.minimum(1, t / 0.012) * np.exp(-t / 0.20)
        return saw * am * e
    place(st, buzz(207, 196, 0.5), 0.0, sr, gain=1.0, pan=-0.05)
    place(st, buzz(165, 150, 0.7), 0.26, sr, gain=1.0, pan=0.05)
    st = reverb(st, sr, rt=0.55, wet=0.22, seed=51, damp=2500)[:, :n]
    finish(os.path.join(out, "SFX", "Error.wav"), st, sr, 2, 0.0, fade_ms=200)

# ----------------------------------------------------------------- music
NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}
def mtof(name):
    p, o = name[:-1], int(name[-1]); return 440.0 * 2 ** ((NOTE[p] + 12 * (o + 1) - 69) / 12)

def make_music(out):
    """'Pocket Lantern': 16 bars, 82 BPM, Cmaj7 - Am7 - Dm7 - G7 lo-fi loop. Circular rendering => seamless loop."""
    sr = 44100; bpm = 82.0; beat = 60.0 / bpm; bar = 4 * beat; bars = 16
    n = int(round(bar * bars * sr)); st = np.zeros((2, n)); rng = np.random.default_rng(61)
    def wrap_add(ch, sig, start):
        i = int(start * sr) % n; m = len(sig)
        if i + m <= n: st[ch, i:i + m] += sig
        else: k = n - i; st[ch, i:] += sig[:k]; st[ch, :m - k] += sig[k:]
    def epiano(f, dur, vel):
        m = int((dur + 0.9) * sr); t = t_axis(m, sr)
        mod = np.sin(2 * np.pi * f * 2.0 * t) * 1.3 * np.exp(-t / 0.35)       # tine
        y = np.sin(2 * np.pi * f * t + mod) * np.exp(-t / 1.1) + 0.2 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t / 0.12)
        rel = np.minimum(1.0, np.maximum(0.0, (dur + 0.5 - t) / 0.5))
        return y * np.minimum(1, t / 0.004) * rel * vel
    def pluck(f, dur, vel):
        m = int((dur + 0.5) * sr); t = t_axis(m, sr)
        y = (np.sign(np.sin(2 * np.pi * f * t)) * 0.3 + np.sin(2 * np.pi * f * t)) * np.exp(-t / 0.28)   # soft square-ish chiptune
        return fft_filter(y, sr, hi=3200, order=2) * np.minimum(1, t / 0.003) * vel
    def bass(f, dur, vel):
        m = int((dur + 0.2) * sr); t = t_axis(m, sr)
        y = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * f * 2 * t)
        return y * np.minimum(1, t / 0.006) * np.minimum(1, np.maximum(0, (dur + 0.15 - t) / 0.15)) * vel
    def kick(vel):
        m = int(0.35 * sr); t = t_axis(m, sr)
        return np.sin(2 * np.pi * np.cumsum(48 + 110 * np.exp(-t / 0.03)) / sr) * np.exp(-t / 0.14) * vel
    def snare(vel):
        m = int(0.25 * sr); t = t_axis(m, sr)
        nz = fft_filter(rng.standard_normal(m), sr, lo=1200, hi=6000) * np.exp(-t / 0.07)
        return (nz * 0.8 + np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.05) * 0.5) * vel
    def hat(vel):
        m = int(0.07 * sr); t = t_axis(m, sr)
        return fft_filter(rng.standard_normal(m), sr, lo=6500) * np.exp(-t / 0.018) * vel

    chords = [  # voicing (rootless-ish), bass root
        (["E3", "G3", "B3", "D4"], "C2"),   # Cmaj7
        (["E3", "G3", "C4", "A3"], "A1"),   # Am7
        (["F3", "A3", "C4", "D4"], "D2"),   # Dm7
        (["F3", "B3", "D4", "G3"], "G1"),   # G7
    ]
    # melody per 4-bar phrase: (beat offset within phrase, note, length in beats); three variations
    A = [(0, "E5", 1), (1.5, "G5", .5), (2, "A5", 1.5), (4, "G5", 1), (5.5, "E5", .5), (6, "D5", 2),
         (8, "C5", 1), (9.5, "D5", .5), (10, "F5", 1.5), (12, "E5", 1), (13, "D5", 1), (14, "B4", 2)]
    B = [(0, "G5", 1.5), (2, "E5", .5), (2.5, "D5", .5), (3, "E5", 1), (4, "A5", 1.5), (6, "G5", 1), (7, "E5", 1),
         (8, "F5", 1), (9, "A5", 1), (10, "G5", 1), (11, "F5", 1), (12, "D5", 2), (14, "B4", 1), (15, "D5", 1)]
    quiet = []     # phrase with no melody (just keys + drums)
    phrases = [quiet, A, B, A]
    for b in range(bars):
        voicing, root = chords[b % 4]; t0 = b * bar
        for j, nm in enumerate(voicing):                       # comped chord: beat 1 and the "and" of 2, softly rolled
            f = mtof(nm); roll = j * 0.018
            for off, v, d in ((0.0, 0.5, 1.6 * beat), (1.5 * beat, 0.32, 1.0 * beat)):
                y = epiano(f, d, v)
                wrap_add(0, y * 0.95, t0 + off + roll); wrap_add(1, y * 0.95, t0 + off + roll + 0.008)
        fb = mtof(root)
        for off, d, v in ((0, 1.4, 0.62), (2.5, 0.8, 0.4), (3.25, 0.6, 0.35)):
            y = bass(fb, d * beat, v); wrap_add(0, y, t0 + off * beat); wrap_add(1, y, t0 + off * beat)
        # drums: lazy swung boom-bap
        sw = 0.12 * beat
        for off, v in ((0, 0.95), (2.5, 0.7)):
            y = kick(v); wrap_add(0, y, t0 + off * beat); wrap_add(1, y, t0 + off * beat)
        if b % 4 == 3: kk = kick(0.55); wrap_add(0, kk, t0 + 3.5 * beat); wrap_add(1, kk, t0 + 3.5 * beat)
        for off in (1, 3):
            y = snare(0.38); wrap_add(0, y, t0 + off * beat); wrap_add(1, y * 0.9, t0 + off * beat + 0.004)
        for k in range(8):
            off = k * 0.5 * beat + (sw if k % 2 else 0)
            v = (0.22 if k % 2 == 0 else 0.13) * (0.8 + 0.4 * rng.random())
            y = hat(v); p = -0.3 if k % 2 else 0.3
            wrap_add(0, y * (1 - p) * .6, t0 + off); wrap_add(1, y * (1 + p) * .6, t0 + off)
    for p_i, ph in enumerate(phrases * 1):
        t0 = p_i * 4 * bar
        for off, nm, ln in ph:
            y = pluck(mtof(nm), ln * beat, 0.34); ts = t0 + off * beat * 1.0
            wrap_add(0, y, ts); wrap_add(1, np.roll(y, int(0.011 * sr)), ts)   # tiny Haas widening
    # glue: tape-ish warmth (circular low-pass), reverb, vinyl dust
    st = np.stack([fft_filter(x, sr, lo=40, hi=7500, order=2) for x in st])
    st = reverb(st, sr, rt=1.4, wet=0.22, seed=62, circular=True)
    dust = np.zeros(n)
    idx = rng.integers(0, n, int(n / sr * 9)); dust[idx] = rng.standard_normal(len(idx)) * 0.02
    hiss = fft_filter(rng.standard_normal(n), sr, lo=3000, hi=9000) * 0.0015
    st += (fft_filter(dust, sr, lo=800, hi=9000) + hiss)[None, :]
    pk = np.abs(st).max()
    st = np.tanh(st / pk * 1.35) / np.tanh(1.35)              # gentle soft clip so it stays musical
    st = st / np.abs(st).max() * (10 ** (-1.0 / 20))
    pcm = (np.clip(st, -1, 1) * 32767).astype("<i2").T.copy().tobytes()
    with wave.open(os.path.join(out, "Placeholder", "Music_JackTheLumberer.wav"), "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr); w.writeframes(pcm)
    return n / sr

# ----------------------------------------------------------------- desktop UI sounds (2026-10-19, round 3)
# Clicks: Audio/SFX/Click + Audio/SFX/Mouse_Click1. Action sounds: Resources/DesktopSfx/<DesktopSound name>, played by
# LingGuangV05.Desktop.OS.DesktopSfx instead of the click (exactly one sound per mouse release).
#
# What "清脆" means here (owner: a dry, short, bright "哒"; not a "咚", not a "叮"):
#   1. the attack is an edge, not a ramp: full level within 0.1 ms and nothing in front of it (every filter that
#      touches a transient is causal, so there is no pre-ringing);
#   2. the body is a cluster of short INHARMONIC modes from 1.5 to 9.6 kHz that are all gone within ~2 ms; their
#      bandwidths overlap, so it has a colour (哒) but no pitch (叮), and nothing below 1 kHz (咚);
#   3. about a fifth of the energy is above 6 kHz, but only for a fraction of a millisecond: that reads as edge and
#      air, not as fizz;
#   4. a second, smaller contact 1.4 ms after the first (the switch bottoming out) makes it a tight mechanism
#      instead of a synthetic blip.
# Every action sound starts with the same 哒 and then makes one gesture. Open/close, on/off and maximize/minimize
# are mirror images (same parts, pitch direction reversed). Loudness is matched by K-weighted energy (LK, about
# momentary LUFS for clips under 400 ms), not by peak; actions sit at or below the click.

SFX_SR = 48000
LK_CLICK = -30.5          # Click, Mouse_Click1 (peak ≈ -4 dBFS); everything else is set relative to this

def at(buf, sig, start_s, sr, gain=1.0):
    i = int(start_s * sr); m = min(len(sig), len(buf) - i)
    if m > 0: buf[i:i + m] += sig[:m] * gain

def biquad(x, sr, kind, f0, q=0.707):
    """Causal RBJ biquad ("lp", "hp" or "bp"), sample loop: nothing leaks in front of a transient."""
    w0 = 2 * np.pi * min(f0, 0.45 * sr) / sr; al = np.sin(w0) / (2 * q); c = np.cos(w0)
    b = {"lp": ((1 - c) / 2, 1 - c, (1 - c) / 2), "hp": ((1 + c) / 2, -(1 + c), (1 + c) / 2), "bp": (al, 0.0, -al)}[kind]
    a0 = 1 + al; b0, b1, b2 = (v / a0 for v in b); a1 = -2 * c / a0; a2 = (1 - al) / a0
    out = [0.0] * len(x); x1 = x2 = y1 = y2 = 0.0
    for i, xi in enumerate(np.asarray(x, dtype=float).tolist()):
        yi = b0 * xi + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, xi, y1, yi; out[i] = yi
    return np.array(out)

def k_weight_gain(f):
    """Magnitude of the BS.1770 K filter (+4 dB shelf above ~1.5 kHz, high-pass ~38 Hz), analytic approximation."""
    f = np.maximum(np.asarray(f, dtype=float), 1e-9)
    return (1 + (10 ** (4 / 20) - 1) / np.sqrt(1 + (1681.0 / f) ** 2)) / np.sqrt(1 + (38.0 / f) ** 4)

def loudness_k(y, sr):
    """K-weighted energy of the clip spread over a 400 ms window, in dB (≈ momentary LUFS for clips < 400 ms)."""
    nfft = 1 << int(np.ceil(np.log2(max(len(y), 2048)) + 1))
    X = np.fft.rfft(y, nfft); f = np.fft.rfftfreq(nfft, 1 / sr)
    e = 2 * (np.abs(X) ** 2 * k_weight_gain(f) ** 2).sum() / nfft
    return -0.691 + 10 * np.log10(e / (0.4 * sr) + 1e-20)

def write_click(path, y, sr, lk, fade_ms=10, peak_max=-3.5):
    """Write a mono 16-bit UI sound at loudness lk (K-weighted, see loudness_k) with its peak at most peak_max.
    Unlike finish() there is no fade-in (the sounds bring their own 0.08 ms onset; a longer ramp blunts the edge),
    a causal 40 Hz high-pass removes DC without smearing the attack, and the tail gets a squared fade."""
    y = biquad(np.asarray(y, dtype=float), sr, "hp", 40)
    i = np.argmax(np.abs(y) > 1e-4 * np.abs(y).max()); y = y[max(0, i - 1):]          # one quiet sample in front
    n = len(y); fn = min(n // 2, int(fade_ms / 1000 * sr)); y[n - fn:] *= np.linspace(1, 0, fn) ** 2
    g = 10 ** ((lk - loudness_k(y, sr)) / 20); pk = 20 * np.log10(np.abs(y).max() * g)
    if pk > peak_max:
        print(f"  {os.path.basename(path)}: peak {pk:.1f} dBFS capped at {peak_max} (LK {lk + peak_max - pk:.1f})")
        g *= 10 ** ((peak_max - pk) / 20)
    pcm = np.round(np.clip(y * g, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes(pcm.tobytes())

# (frequency Hz, decay s, amplitude): a hard little plastic snap. Bandwidths 1/(pi*tau) = 200-800 Hz overlap: no pitch.
DA_MODES = ((1500, 0.0016, 0.24), (2350, 0.0015, 0.55), (3250, 0.0013, 1.00), (4300, 0.0010, 0.85),
            (5650, 0.0008, 0.70), (7400, 0.00055, 0.65), (9600, 0.0004, 0.45))
# Start phases of the modes (first contact, bottom-out contact) and the edge-noise seed. Phases decide how the modes
# add up in the first millisecond, i.e. the crest factor and the band balance; these were picked out of 1500 random
# draws for crest <= 27.5 dB (peak minus LK) with 10-15 % at 1-2.5 kHz and 14-20 % at 6-12 kHz.
DA_PH1 = (4.33, 0.90, 0.96, 5.95, 0.92, 2.90, 0.51)
DA_PH2 = (4.68, 5.71, 3.01, 0.59, 1.74, 0.95, 2.62)
DA_EDGE = 1.3             # peak of the 0.2 ms edge burst relative to the loudest mode
DA_SEED = 656

def snap(n, sr, scale=1.0, air=1.0, damp=1.0, phases=DA_PH1, seed=DA_SEED):
    """One contact: the modal cluster (pitched up by scale, decays times damp) plus a 0.2 ms band-limited edge burst;
    air scales everything above 6 kHz. 0.08 ms raised-cosine onset: an edge, but no digital click."""
    t = t_axis(n, sr); y = np.zeros(n)
    for (f, tau, a), p in zip(DA_MODES, phases):
        f *= scale
        if f > 0.42 * sr: continue
        y += a * (air if f > 6000 else 1.0) * np.sin(2 * np.pi * f * t + p) * np.exp(-t / (tau * damp))
    m = min(n, int(0.003 * sr))
    e = np.random.default_rng(seed).standard_normal(m) * np.exp(-t[:m] / 0.00018)
    e = biquad(biquad(e, sr, "hp", 2200 * scale), sr, "lp", 11500 * scale)
    y[:m] += DA_EDGE * air * e / (np.abs(e).max() + 1e-12)
    k = max(2, int(0.00008 * sr)); y[:k] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(k) / k)
    return y

def da(sr, n, scale=1.0, air=1.0, second=0.38, gap=0.0014):
    """The dry "哒": a snap and, gap later, a smaller, slightly higher bottom-out contact."""
    y = snap(n, sr, scale, air)
    if second: at(y, snap(n, sr, scale * 1.07, air * 0.8, damp=0.8, phases=DA_PH2, seed=DA_SEED + 1), gap, sr, second)
    return y

def glide_path(n, sr, f0, f1, glide):
    """Pitch path f0 -> f1 that eases in like a real resonance settling (log-exponential, ~95 % there after glide s);
    a linear slide that stops dead sounds like a slide whistle."""
    k = np.exp(-3 * t_axis(n, sr) / glide)
    return np.exp(np.log(f1) + (np.log(f0) - np.log(f1)) * k)

def glass(n, sr, f0, f1, glide, tau, attack=0.004, partials=((1, 1.0), (2.0, 0.16), (2.76, 0.07))):
    """A light glassy tone gliding f0 -> f1 (see glide_path), soft attack, exponential decay."""
    ph = 2 * np.pi * np.cumsum(glide_path(n, sr, f0, f1, glide)) / sr
    return sum(a * np.sin(m * ph) for m, a in partials) * expdec(n, sr, tau, attack)

def swish(n, sr, rng, f0, f1, glide, attack, tau, q=2.2):
    """Airy band-passed noise whose centre moves f0 -> f1; low-passed at 7 kHz so it never fizzes."""
    y = sweep_bandpass(rng.standard_normal(n), sr, f0, f1, q=q, fc=glide_path(n, sr, f0, f1, glide))
    y = biquad(y, sr, "lp", 7000) * expdec(n, sr, tau, attack)
    return y / (np.abs(y).max() + 1e-9)

def sfx_out(out):
    d = os.path.normpath(os.path.join(out, "..", "Resources", "DesktopSfx")); os.makedirs(d, exist_ok=True); return d

def write_sfx(out, name, y, lk, fade_ms=10):
    write_click(os.path.join(sfx_out(out), name + ".wav"), y, SFX_SR, lk, fade_ms=fade_ms)

def gesture(sr, length_s, click, parts):
    """An action sound: the click at 0, then each (signal, start s, dB) part scaled so its loudness sits that many dB
    relative to the click. The click keeps the attack; the parts carry the meaning."""
    y = np.zeros(int(length_s * sr)); at(y, click, 0, sr); ref = loudness_k(click, sr)
    for sig, start, rel in parts: at(y, sig, start, sr, 10 ** ((ref + rel - loudness_k(sig, sr)) / 20))
    return y

def make_desktop_sfx(out):
    sr = SFX_SR; rng = np.random.default_rng(31); N = lambda s: int(s * sr)
    C = lambda **kw: da(sr, N(0.03), **kw)

    # Click on a button: the reference 哒.
    write_sfx(out, "Click", C(), LK_CLICK)

    # Click on empty space / text: a smaller, softer, single-contact tick.
    write_sfx(out, "Tap", da(sr, N(0.025), scale=1.12, air=0.55, second=0.22), LK_CLICK - 7.5, fade_ms=8)

    # Stepping a selector / choosing an item: a higher 哒 with a tiny glint of glass on top ("哒i").
    y = gesture(sr, 0.06, C(scale=1.2), [(glass(N(0.05), sr, 2650, 3300, 0.008, 0.009, attack=0.0015,
                                                 partials=((1, 1.0), (2.0, 0.1))), 0.002, -8)])
    write_sfx(out, "Select", y, LK_CLICK - 2.0)

    # Switching tabs / pages: two dry contacts 24 ms apart, low then high, "嗒哒". No tone.
    y = gesture(sr, 0.06, C(scale=0.9, second=0.3), [(C(scale=1.18), 0.024, +3)])
    write_sfx(out, "Tab", y, LK_CLICK - 1.0)

    # Switch on: 哒 + a short glass chirp up a fifth. Switch off: the same chirp down a fifth, a touch softer.
    for name, sc, f0, f1, lk in (("ToggleOn", 1.05, 1500, 2250, -1.0), ("ToggleOff", 0.95, 2250, 1500, -1.5)):
        y = gesture(sr, 0.1, C(scale=sc), [(glass(N(0.095), sr, f0, f1, 0.03, 0.022, attack=0.003), 0.002, -4)])
        write_sfx(out, name, y, LK_CLICK + lk, fade_ms=20)

    # Menu / start menu / context menu: 哒 + a quick airy flick upward ("哒-嘶"), no tone, no second tick.
    y = gesture(sr, 0.075, C(scale=1.1), [(swish(N(0.07), sr, rng, 1800, 4800, 0.04, 0.006, 0.02, q=3.0), 0.0, -4)])
    write_sfx(out, "MenuOpen", y, LK_CLICK - 1.0, fade_ms=15)

    # Maximize / restore: 哒 + a short air swoosh up that lands on a small high contact ("哒-嗖-嗒").
    # Minimize: the mirror, swoosh down onto a lower contact. Pure air, no glass: that belongs to open/close.
    for name, sc, a0, a1, land in (("WindowMaximize", 1.05, 1200, 4000, 1.25), ("WindowMinimize", 0.95, 4000, 1200, 0.85)):
        y = gesture(sr, 0.12, C(scale=sc), [(swish(N(0.09), sr, rng, a0, a1, 0.06, 0.008, 0.025, q=2.2), 0.0, -2),
                                            (da(sr, N(0.03), scale=land, air=0.8, second=0.3), 0.068, -8)])
        write_sfx(out, name, y, LK_CLICK - 1.0, fade_ms=20)

    # Window opens: 哒 + a glass tone that blooms up an octave and settles, with air rising behind it.
    # Window closes: the mirror image, falling, a little shorter.
    for name, sc, g0, g1, a0, a1, L, tau, lk in (("WindowOpen", 1.05, 900, 1800, 1400, 4500, 0.22, 0.055, -1.0),
                                                 ("WindowClose", 0.95, 1800, 900, 4500, 1400, 0.20, 0.045, -1.5)):
        y = gesture(sr, L, C(scale=sc), [(glass(N(L), sr, g0, g1, 0.07, tau, attack=0.005), 0.002, -2),
                                         (swish(N(L), sr, rng, a0, a1, 0.1, 0.02, 0.05), 0.0, -7)])
        write_sfx(out, name, y, LK_CLICK + lk, fade_ms=45)

ALL = {"keys": make_keys, "hover": make_hover, "click": make_click, "mouse": make_mouse_click,
       "notification": make_notification, "received": make_received, "sent": make_sent,
       "error": make_error, "music": make_music, "desktop": make_desktop_sfx}

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--out", default=DEFAULT_OUT); ap.add_argument("--only", default="")
    a = ap.parse_args()
    for k, fn in ALL.items():
        if a.only and k not in a.only.split(","): continue
        fn(a.out); print("wrote", k)
