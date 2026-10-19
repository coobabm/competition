#!/usr/bin/env python3
"""Prints a measurement table for the desktop UI sounds made by make_audio.py (numpy + stdlib wave only).

Usage:  python3 measure_sfx.py [file.wav ...]      (default: the 13 click/desktop sounds in Assets)
        python3 measure_sfx.py --root <dir>        (the same 13 under <dir>, e.g. a make_audio.py --out <dir>/Audio render)

Columns
  len     clip length (ms)                    pk      sample peak (dBFS)
  st      loudest 10 ms RMS (dBFS)             LK      K-weighted energy of the whole clip over a 400 ms window
                                                       (≈ momentary LUFS; what to match for perceived loudness)
  cent    spectral centroid (Hz)               bands   energy share <1k | 1-2.5k | 2.5-6k | 6-12k | >12k (%)
  atk     first sample to 90% of the peak (ms) e90     time by which 90% of the energy has played (ms)
  d40     last time the 1 ms envelope is above -40 dB re its maximum (ms)
  dc      mean sample value, edge  |first sample| and max |x| in the last 1 ms (both ×1e3)
"""
import os, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.normpath(os.path.join(HERE, "../../../Assets/LingGuangV05/DesktopBridge/OSArt"))
DEFAULT = [os.path.join(ASSETS, "Audio/SFX", n + ".wav") for n in ("Click", "Mouse_Click1")] + \
          [os.path.join(ASSETS, "Resources/DesktopSfx", n + ".wav") for n in
           ("Tap", "Click", "Select", "Tab", "ToggleOn", "ToggleOff", "MenuOpen",
            "WindowMinimize", "WindowMaximize", "WindowClose", "WindowOpen")]

def load(path):
    with wave.open(path, "rb") as w:
        sr, ch, n = w.getframerate(), w.getnchannels(), w.getnframes()
        x = np.frombuffer(w.readframes(n), "<i2").astype(np.float64) / 32768.0
    return x.reshape(-1, ch).mean(axis=1), sr

def k_weight_gain(f):
    """Magnitude of the ITU-R BS.1770 K filter (high shelf +4 dB above ~1.5 kHz, high-pass ~38 Hz), analytic approx."""
    f = np.maximum(f, 1e-9)
    shelf = 1 + (10 ** (4 / 20) - 1) / (1 + (1681.0 / f) ** 2) ** 0.5
    hp = 1 / np.sqrt(1 + (38.0 / f) ** 4)
    return shelf * hp

def measure(x, sr):
    n = len(x); a = np.abs(x); pk = a.max() + 1e-12
    X = np.fft.rfft(x * 1.0, 1 << int(np.ceil(np.log2(max(n, 2048)) + 1)))
    f = np.fft.rfftfreq((len(X) - 1) * 2, 1 / sr); P = np.abs(X) ** 2
    cent = (f * P).sum() / P.sum()
    edges = [0, 1000, 2500, 6000, 12000, sr / 2 + 1]
    bands = [100 * P[(f >= lo) & (f < hi)].sum() / P.sum() for lo, hi in zip(edges[:-1], edges[1:])]
    nfft = (len(X) - 1) * 2; ek = 2 * (P * k_weight_gain(f) ** 2).sum() / nfft          # Parseval: K-weighted energy
    lk = -0.691 + 10 * np.log10(ek / (0.4 * sr) + 1e-20)
    w = max(1, int(0.010 * sr)); c = np.concatenate([[0], np.cumsum(x * x)])
    st = 10 * np.log10(((c[w:] - c[:-w]) / w).max() + 1e-20) if n > w else 10 * np.log10((x * x).mean())
    atk = np.argmax(a >= 0.9 * pk) / sr * 1000
    e = np.cumsum(x * x); e90 = np.argmax(e >= 0.9 * e[-1]) / sr * 1000
    w1 = max(1, int(0.001 * sr)); env = np.sqrt(np.convolve(x * x, np.ones(w1) / w1, "same"))
    above = np.nonzero(env > env.max() * 10 ** (-40 / 20))[0]; d40 = above[-1] / sr * 1000 if len(above) else 0
    tail = a[-max(1, int(0.001 * sr)):].max()
    return dict(len=n / sr * 1000, pk=20 * np.log10(pk), st=st, lk=lk, cent=cent, bands=bands, atk=atk, e90=e90,
                d40=d40, dc=x.mean() * 1e3, first=a[0] * 1e3, tail=tail * 1e3)

def table(paths):
    print(f"{'sound':<16}{'len':>5}{'pk':>6}{'st':>6}{'LK':>6}{'cent':>6}  {'<1k 1-2.5 2.5-6 6-12 >12k':<26}{'atk':>5}{'e90':>6}{'d40':>6}  dc/edge/tail")
    for p in paths:
        x, sr = load(p); m = measure(x, sr)
        name = ("SFX/" if "Audio/SFX" in p else "") + os.path.splitext(os.path.basename(p))[0]
        b = " ".join(f"{v:4.0f}" for v in m["bands"])
        print(f"{name[:16]:<16}{m['len']:5.0f}{m['pk']:6.1f}{m['st']:6.1f}{m['lk']:6.1f}{m['cent']:6.0f}  {b:<26}"
              f"{m['atk']:5.2f}{m['e90']:6.1f}{m['d40']:6.1f}  {m['dc']:.2f}/{m['first']:.2f}/{m['tail']:.2f}")

def spectro(x, sr, dur=0.22, hop=0.004, win=0.008, bands=20, lo=700, hi=14000, floor=-45):
    """Log-band spectrogram (dB re its own maximum, floored), fixed size: a rough picture of what the ear gets."""
    h, w = int(hop * sr), int(win * sr); edges = np.geomspace(lo, hi, bands + 1)
    f = np.fft.rfftfreq(w, 1 / sr); x = np.concatenate([x, np.zeros(int(dur * sr) + w)])
    S = np.empty((int(dur / hop), bands))
    for i in range(S.shape[0]):
        P = np.abs(np.fft.rfft(x[i * h:i * h + w] * np.hanning(w))) ** 2
        S[i] = [P[(f >= a) & (f < b)].sum() + 1e-20 for a, b in zip(edges[:-1], edges[1:])]
    S = 10 * np.log10(S); return np.maximum(S - S.max(), floor)

def pairs(paths):
    """Nearest neighbour of every sound by mean |dB| difference of the log-band spectrograms where either sound is
    active (higher = more distinct; below ~4 dB two sounds are hard to tell apart)."""
    names = [("SFX/" if "Audio/SFX" in p else "") + os.path.splitext(os.path.basename(p))[0] for p in paths]
    S = [spectro(*load(p)) for p in paths]
    print("\nnearest neighbour (mean |dB| over the cells where either is above -40 dB, 0.7-14 kHz; the two Click files and Mouse_Click1 are meant to match)")
    for i, a in enumerate(S):
        d = sorted((np.abs(a - b)[(a > -40) | (b > -40)].mean(), names[j]) for j, b in enumerate(S) if j != i)
        print(f"  {names[i]:<16} {d[0][1]:<16} {d[0][0]:5.1f}   next {d[1][1]:<16} {d[1][0]:5.1f}")

def from_root(root):
    """The same 13 files under another render root (make_audio.py --out <root>/Audio)."""
    return [p.replace(ASSETS, os.path.abspath(root)) for p in DEFAULT]

if __name__ == "__main__":
    args = sys.argv[1:]
    if len(args) == 2 and args[0] == "--root": args = from_root(args[1])
    table(args or DEFAULT); pairs(args or DEFAULT)
