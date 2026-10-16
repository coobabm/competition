"""Synthesizes the 888 casino's sound effects as 44.1 kHz mono 16-bit WAVs (no samples, no network)."""
import sys, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(888)
out = sys.argv[1]


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, attack, decay):
    """Attack in samples (linear), then exponential decay with time constant `decay` seconds."""
    e = np.exp(-np.arange(n) / (SR * decay))
    a = max(1, int(attack))
    e[:a] *= np.linspace(0, 1, a)
    return e


def onepole_lp(x, fc):
    a = np.exp(-2 * np.pi * fc / SR)
    y = np.empty_like(x); s = 0.0
    for i, v in enumerate(x):
        s = (1 - a) * v + a * s; y[i] = s
    return y


def lp3(x, fc):
    return onepole_lp(onepole_lp(onepole_lp(x, fc), fc), fc)


def bandpass(x, lo, hi):
    """Three-pole low-pass at hi minus three-pole low-pass at lo: a band without the hiss."""
    y = lp3(x, hi)
    return y - lp3(y, lo)


def place(buf, snd, at, gain=1.0):
    i = int(at * SR)
    if i >= len(buf): return
    end = min(len(buf), i + len(snd))
    buf[i:end] += snd[: end - i] * gain


def clack(freqs=(3150, 4870, 7300), decay=.018, noise=.35):
    """A ceramic chip hitting another: a few inharmonic partials and a click of noise."""
    n = int(SR * .09)
    x = np.zeros(n)
    tt = np.arange(n) / SR
    for k, f in enumerate(freqs):
        f *= 1 + rng.uniform(-.03, .03)
        x += np.sin(2 * np.pi * f * tt) * np.exp(-tt / (decay * (1 - .2 * k))) / (1 + k * .6)
    x += bandpass(rng.standard_normal(n), 1800, 9000) * env(n, 2, .006) * noise
    return x


def thump(f=110, decay=.09, n_sec=.3):
    tt = t(n_sec)
    sweep = f * (1 + 1.2 * np.exp(-tt / .015))
    return np.sin(2 * np.pi * np.cumsum(sweep) / SR) * env(len(tt), 20, decay)


def bell(f, sec=1.0, decay=.35, bright=2.0):
    """FM bell."""
    tt = t(sec)
    mod = np.sin(2 * np.pi * f * 3.5 * tt) * bright * np.exp(-tt / (decay * .5))
    return np.sin(2 * np.pi * f * tt + mod) * env(len(tt), 30, decay)


def save(name, x, peak=.85):
    x = x - np.mean(x)
    m = np.max(np.abs(x)) or 1
    x = x / m * peak
    fade = min(len(x), int(SR * .01))
    x[-fade:] *= np.linspace(1, 0, fade)
    with wave.open(f"{out}/{name}.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
    print(name, f"{len(x) / SR:.2f}s")


# Chip placed: two chips settling on each other.
b = np.zeros(int(SR * .25)); place(b, clack(), 0); place(b, clack((3400, 5200, 7900)), .045, .55)
save("casino_chip", b)

# All in: a stack pushed onto the felt, chips tumbling.
b = np.zeros(int(SR * .9)); at = 0.0
for k in range(11):
    place(b, clack((2900 + rng.uniform(0, 900), 4700 + rng.uniform(0, 800), 7000)), at, 1 - k * .06)
    at += rng.uniform(.035, .075)
place(b, bandpass(rng.standard_normal(int(SR * .5)), 300, 2500) * env(int(SR * .5), 400, .12) * .25, .02)
save("casino_allin", b)

# Card dealt: a swish of air and a soft slap on the felt.
n = int(SR * .22)
sw = bandpass(rng.standard_normal(n), 900, 6000) * np.sin(np.linspace(0, np.pi, n)) ** 2 * .7
b = np.zeros(int(SR * .3)); place(b, sw, 0)
place(b, bandpass(rng.standard_normal(int(SR * .05)), 400, 3000) * env(int(SR * .05), 3, .01), .17, .9)
save("casino_deal", b)

# Card turned over: a short snap.
n = int(SR * .08)
save("casino_flip", bandpass(rng.standard_normal(n), 1500, 8000) * env(n, 2, .008) + clack((2200, 3600, 5100), .006, .1) [:n] * .3)

# Dice: shaken in the cup, rolled out, settled by the time the result shows (1.2 s).
b = np.zeros(int(SR * 1.3)); at = 0.0
while at < .4:
    place(b, clack((900 + rng.uniform(0, 500), 1900 + rng.uniform(0, 600), 3300), .012, .5), at, rng.uniform(.4, .8)); at += rng.uniform(.025, .06)
at = .48; gap = .045
for k in range(9):
    place(b, clack((1100 + rng.uniform(0, 400), 2300, 3900), .02, .3), at, .9 - k * .08); at += gap; gap *= 1.15
save("casino_dice", b)

# Slot reels spinning for the 1.2 s reveal: a fast ratchet over a whirr.
sec = 1.15; n = int(SR * sec)
whirr = bandpass(rng.standard_normal(n), 200, 1600) * .18 * (1 + .3 * np.sin(2 * np.pi * 6 * t(sec)))
b = whirr.copy()
for k in range(int(sec * 20)):
    place(b, clack((2600, 4100, 6000), .006, .2), k / 20, .35)
save("casino_reel_spin", b, .6)

# A reel stops: a mechanical clunk.
b = np.zeros(int(SR * .35)); place(b, thump(90, .07), 0); place(b, clack((1800, 2900, 4500), .02, .5), 0, .7)
save("casino_reel_stop", b)

# Roulette: the ball runs round the rim, clatters through the frets and drops into a pocket at ~1.15 s.
sec = 1.5; b = np.zeros(int(SR * sec))
rim = bandpass(rng.standard_normal(int(SR * .8)), 1200, 5000) * np.linspace(.3, .06, int(SR * .8))
place(b, rim, 0)
at = .55; gap = .05
while at < 1.08:
    place(b, clack((2400 + rng.uniform(0, 1500), 4200, 6500), .01, .4), at, .55); at += gap; gap *= 1.12
place(b, clack((1500, 2600, 4100), .03, .3), 1.14, 1.0)
save("casino_roulette", b)

# Win: a bright rising arpeggio.
b = np.zeros(int(SR * 1.1))
for k, f in enumerate([523.25, 659.25, 783.99, 1046.5]):
    place(b, bell(f, .9, .3, 1.4), k * .07, .8)
save("casino_win", b)

# Big win: a coin shower under a major chord fanfare.
sec = 2.4; b = np.zeros(int(SR * sec))
for k, f in enumerate([392.0, 523.25, 659.25, 783.99, 1046.5, 1318.5]):
    place(b, bell(f, 1.6, .55, 1.8), k * .06, .6)
for f in [523.25, 659.25, 783.99]:
    place(b, bell(f, 1.8, .9, .8), .45, .45)
for k in range(46):
    place(b, clack((4200 + rng.uniform(0, 2500), 6800 + rng.uniform(0, 2000), 9000), .015, .2), .15 + rng.uniform(0, 1.6), rng.uniform(.15, .4))
save("casino_bigwin", b)

# Lose: two soft falling notes, muted.
b = np.zeros(int(SR * .8))
for k, f in enumerate([392.0, 311.13]):
    tt = t(.45)
    tone = (np.sin(2 * np.pi * f * tt) + .3 * np.sin(4 * np.pi * f * tt)) * env(len(tt), 200, .16)
    place(b, onepole_lp(tone, 1400), k * .16, .7)
save("casino_lose", b, .6)
