# WASAPI loopback — ready, no recording performed

Isolated environment:
`C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/audio-env/Scripts/python.exe`

Installed only in this venv: SoundCard 0.4.5, NumPy 1.26.4, cffi 1.17.1, pycparser 2.22. Global environments unchanged. Initial sandbox ensurepip failed on temporary-file permissions; authorized isolated bootstrap/download succeeded with temp redirected to staging.

Script: `record_loopback.py`. Syntax compilation/import/API signature checks and `--help` passed. Read-only `--inspect` confirmed the default output is **扬声器 (Realtek(R) Audio)** and the capture endpoint has the exact same id with is_loopback=true. No recorder was opened and no audio was captured.

## Usage

No arguments or `--help`: display help, do not record.
`--inspect`: enumerate only the default output and matching loopback endpoint, do not record.
Only explicit `--record --seconds 10 --output C:/absolute/new-take.wav` starts capture. Duration must be 1–30 seconds; a supervisor terminates a stalled capture after duration+15 seconds. An internal worker option exists only for the supervisor. Do not invoke it directly.

```powershell
$audioPython='C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/audio-env/Scripts/python.exe'
$audioScript='C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/record_loopback.py'
& $audioPython $audioScript --inspect
# Run only when the main task starts an authorized take:
# & $audioPython $audioScript --record --seconds 10 --output 'C:/absolute/new-take.wav'
```

Output: real stereo 48 kHz PCM16 WAV; adjacent JSON contains output device id/name, sampling rate, monotonic/UTC timing, WAV SHA256, measured RMS/peak dBFS, clipped/nonzero sample counts, 100 ms windows, contiguous silence spans >=300 ms below -60 dBFS and capture warnings. Failed or timed-out captures have `.failed.json`; partial WAV is not a passing take. Existing WAV/JSON is never overwritten.

RMS/peak and silence spans describe actual captured samples; silence may be intentional musical rest, not a proven dropout. These metrics do not establish sound quality: listen to the WAV to verify the expected music, stutter/clicks and absence of protagonist voice. Loopback includes every app using that output; mute unrelated apps before the take. No synthetic audio or audio-clock substitute is generated.
