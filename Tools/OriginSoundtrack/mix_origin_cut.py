"""Re-time the origin film's SFX and ambience stems (161 s film time) onto the 88 s cut (XgOriginCut) and mix under the emergency cut."""
import wave, sys, numpy as np
STEMS = "/Users/kk/unity/evolution-video/out/stems/"
MUSIC = "/Users/kk/unity/competition/Documentation/LingGuang-Incremental/起源片_新配乐_剪辑.wav"
OUT = sys.argv[1]
SR = 44100
BAR = 60.0 / 143.91 * 4
DUR = 88.0
B = [None, 50.84, 73.55, 93.5, 114.48, 137.5]
SHOTS = [(0,2,0,3.5),(2,4,7,10.5),(4,6,10.5,14),(6,8,14,17),(8,10,17,22.5),(10,12,22.5,25.5),(12,14,28.5,31.5),(14,16,34.5,37.5),
 (16,18,40,43),(18,20,46,49),(20,21,49,B[1]),(21,22,B[1],52),(22,24,55,58),(24,26,58,61),(26,28,61,64.5),(28,29,71,B[2]),(29,30,B[2],74.5),
 (30,32,88,91),(32,34,B[3],96.5),(34,36,111,B[4]),(36,38,B[4],117.5),(38,41,127.5,131),(41,43,B[5],141),(43,49,145,152.6),(49,51,152.6,157.4),(51,DUR/BAR,157.4,161)]
SHOTS = [(a*BAR, min(b*BAR, DUR), f0, f1) for a,b,f0,f1 in SHOTS]

def read(p):
    w = wave.open(p); n = w.getnchannels(); assert w.getframerate() == SR and w.getsampwidth() == 2
    x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).reshape(-1, n).astype(np.float32) / 32768
    return x if n == 2 else np.repeat(x, 2, 1)

sfx, amb, oldmus = read(STEMS+"sfx.wav"), read(STEMS+"ambience.wav"), read(STEMS+"music.wav")
mus = read(MUSIC)
N = int(DUR * SR)
out_sfx = np.zeros((N, 2), np.float32); out_amb = np.zeros((N, 2), np.float32)

def fade(n, k):
    k = min(k, n // 2); r = np.ones(n, np.float32)
    if k > 0: r[:k] = np.linspace(0, 1, k); r[-k:] = np.minimum(r[-k:], np.linspace(1, 0, k))
    return r[:, None]

# SFX: split into sound events at silences, place each at the cut time showing its onset, natural speed (no pitch change).
env = np.abs(sfx).max(1); hop = 220
blk = np.array([env[i:i+hop].max() for i in range(0, len(env), hop)]) > 0.002
events = []; i = 0
while i < len(blk):
    if blk[i]:
        j = i
        while j < len(blk) and (blk[j] or blk[j:j+6].any()): j += 1   # bridge gaps under ~30 ms
        events.append((i*hop, min(j*hop + hop, len(sfx)))); i = j
    else: i += 1
placed = dropped = 0
for s0, s1 in events:
    tf = s0 / SR
    for c0, c1, f0, f1 in SHOTS:
        if f0 - 0.02 <= tf < f1:
            c = c0 + (tf - f0) / (f1 - f0) * (c1 - c0)
            end = min(s1, s0 + int((c1 + 0.35 - c) * SR))          # sounds from a cut-away moment do not ring long past the cut
            seg = sfx[s0:end] * fade(end - s0, int(0.004 * SR))
            if end < s1:  # fade the tail that got cut short
                k = min(len(seg), int(0.12 * SR)); seg[-k:] *= np.linspace(1, 0, k)[:, None]
            a = int(c * SR); b = min(N, a + len(seg)); out_sfx[a:b] += seg[:b-a]; placed += 1
            break
    else: dropped += 1

# Ambience: each shot's stretch of the bed at natural speed, 40 ms crossfades.
for c0, c1, f0, f1 in SHOTS:
    a, b = int(c0 * SR), int(c1 * SR); n = b - a + int(0.04 * SR)
    s = int(f0 * SR); seg = amb[s:s+n]
    if len(seg) < n: seg = np.pad(seg, ((0, n - len(seg)), (0, 0)))
    seg = seg * fade(n, int(0.04 * SR)); e = min(N, a + n); out_amb[a:e] += seg[:e-a]

# Levels: the film's premaster was 0.455 music + 0.501 sfx + 0.0034 ambience (least squares on the stems); keep that balance
# against the new music, scaled to its loudness.
rms = lambda x: float(np.sqrt((x ** 2).mean()) + 1e-9)
g = rms(mus) / (0.455 * rms(oldmus))
m = np.zeros((N, 2), np.float32); m[:min(N, len(mus))] = mus[:N]
out_sfx *= 0.501 * g; out_amb *= 0.0034 * g
mix = m + out_sfx + out_amb
peak = np.abs(mix).max()
if peak > 0.97: mix *= 0.97 / peak
w = wave.open(OUT, "wb"); w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
w.writeframes((np.clip(mix, -1, 1) * 32767).astype(np.int16).tobytes()); w.close()
print(f"events {len(events)} placed {placed} dropped {dropped}; gain {g:.2f}; peak {peak:.2f}; rms music {rms(mus):.3f} sfx {rms(g*out_sfx):.3f} amb {rms(g*out_amb):.3f}")
