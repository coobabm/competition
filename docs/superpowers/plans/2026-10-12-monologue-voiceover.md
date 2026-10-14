# LingGuang offline monologue voiceover

> Execute inline with executing-plans; review the final scoped changes.

**Goal:** Voice all authored Chinese protagonist monologues using local VoiceStudio, with scene-appropriate reference delivery and deliberate pauses. User selected offline delivery: variable values remain subtitles.
**Architecture:** Reuse VoiceLines + InnerVoice. Keep existing recordings and source text. Add a generated resource manifest and offline WAV assets. Match dynamic templates only to explicitly curated fixed spoken portions; no network/runtime synthesis.
**Tech:** Python standard library + curl/ffmpeg already installed; VoiceStudio local /generate; Unity existing AudioSource/Resources.

## Constraints
- Active project /Users/kk/competition; preserve extensive pre-existing changes and saves.
- Do not install/download models, send scripts remotely, change global settings, or replace source dialogue.
- OmniVoice cannot promise explicit emotion; reference takes + punctuation + speed + pause markers are the actual controls.
- AI dialogue/chat messages are not protagonist monologues; narration coverage is audited separately.

## Tasks
- [x] Inventory direct InnerVoice, prologue, story think/narrate steps, afterthoughts, fatigue, yes/no and epilogue pools; record dynamic exclusions.
- [x] Test extraction/normalization and offline manifest lookup before implementation.
- [x] Pilot local synthesis; verify waveform and local ASR before sequential resumable batch.
- [x] Generate missing takes, validate audio integrity/dropped chunks, map every eligible line.
- [x] Integrate resource manifest; preserve skip/hold timing; Unity import, focused tests and playback probe without changing saves.
- [x] Review scoped changes, report exact coverage and subjective review limits.

## Review focus
Dynamic templates cannot shadow unrelated text; English stays silent; missing clips safely fall back; subtitles outlast clips; skips stop playback; no changes to existing unrelated work.

## Evidence and limits
9 Python + 7 Unity checks passed; 136 new offline takes,148 mappings,17 dynamic templates. Editor preview advanced and stopped. Full local ASR screening with4 corrected takes. No complete story traversal or human listening certification. See Documentation/Voiceover/2026-10-12/README.md.
