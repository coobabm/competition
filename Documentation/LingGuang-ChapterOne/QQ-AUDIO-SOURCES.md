# QQ-era audio source check

## Conclusion / evidence boundary

- No Tencent-hosted QQ2010 sound file, QQ2010 installer extraction manifest, or original-file hash was verified in this research.
- The two accessible WAV candidates below are from a third-party QQ imitation. They must **not** be described as authenticated QQ2010 originals.
- This research subagent did not download or execute installers, archives, or audio files. Audio endpoints were checked with HTTP HEAD only; no listening or waveform comparison was performed by this subagent. The parent task may perform separate asset validation.
- Public download availability is not a reuse license. The repository's MIT license does not establish that its author owns or can license Tencent-origin sound recordings. Original-sound use in a local prototype is not proof of permission to redistribute it in a released game.

## Accessible candidates, pinned to a commit

Source repository: <https://github.com/sxei/myqq>

Pinned commit: `e603e4a0027cc787e0549fa6727da6630610288e`.

| Candidate | Direct URL | Observed response | Provenance / scope |
|---|---|---|---|
| `msg.wav` | <https://raw.githubusercontent.com/sxei/myqq/e603e4a0027cc787e0549fa6727da6630610288e/msg.wav> | HTTP 200, `audio/wav` | Repository includes it as a message sound. Not an authenticated Tencent original. |
| `system.wav` | <https://raw.githubusercontent.com/sxei/myqq/e603e4a0027cc787e0549fa6727da6630610288e/system.wav> | HTTP 200, `audio/wav` | Repository includes a system-sound playback helper. Its audible identity and QQ2010 event mapping remain unverified. |

HTTP response date observed during the check: `Fri, 02 Oct 2026 22:50:52 GMT` (October 3 in Asia/Seoul). Response ETags are not represented here as verified local content hashes.

## What the primary repository actually establishes

- The author's README says the project was written in 2012 and particularly imitates the QQ2011 login interface. This is evidence of the author's stated project era, **not** proof that either WAV came from QQ2010.
  - <https://github.com/sxei/myqq/blob/e603e4a0027cc787e0549fa6727da6630610288e/README.md>
- `src/com/MyTools.java`, lines 156–176, implements `playMsgSound()` using `msg.wav` and `playSystemSound()` using `system.wav`.
  - <https://github.com/sxei/myqq/blob/e603e4a0027cc787e0549fa6727da6630610288e/src/com/MyTools.java#L156-L176>
- `src/client/socket/S_TCP.java`, lines 112–119, plays the message cue for an incoming message when the chat window is not visible. `CS_TCP.java`, lines 182–186, uses it for unread offline-message notification.
  - <https://github.com/sxei/myqq/blob/e603e4a0027cc787e0549fa6727da6630610288e/src/client/socket/S_TCP.java#L112-L119>
  - <https://github.com/sxei/myqq/blob/e603e4a0027cc787e0549fa6727da6630610288e/src/client/socket/CS_TCP.java#L182-L186>

These are mappings in the imitation project; they do not establish original QQ2010 behavior.

## Contemporary historical corroboration (secondary sources)

These articles predate QQ2010 and are more useful for historical mapping than a modern nostalgia upload. They are not Tencent documentation and cannot prove that a particular WAV matches a particular QQ2010 build.

| Source | What it establishes | Limitation |
|---|---|---|
| Sina / Zhongguancun Online, December 21, 2005: <https://tech.sina.com.cn/s/2005-12-21/1127798457.shtml> | Lists `msg.wav` as message notification, `system.wav` as system notification, and `Global.wav` as friend-online notification. | Historical third-party tutorial, not QQ2010-specific. |
| ZDNet / Zhiding, January 14, 2008: <https://net.zhiding.cn/network_security_zone/2008/0114/707079.shtml> | Describes incoming contact messages, system notifications, new-online, and group sound categories. Explicitly gives a friend-add request requiring verification as a system-notification example. | Reprinted tutorial; its mention of an OICQ installation path suggests some material may predate publication. |
| ZDNet / Zhiding, July 23, 2008: <https://net.zhiding.cn/network_security_zone/2008/0723/1007147.shtml> | Independently lists the `msg.wav` / `system.wav` / `Global.wav` event mapping. | Corroborates classic-era naming, not a QQ2010 asset hash. |

**Working mapping:** incoming message → `msg.wav`; verification/system notice → `system.wav`; friend online → `Global.wav`, not `system.wav`. The familiar “cough” label is commonly associated with the system cue, but the two repository WAV files still require audition to verify audible identity. Do not assign a cough to friend-online as though that mapping were authenticated.

**Sent messages:** None of the above historical category lists establishes a separate outgoing-message default sound. Silence is the conservative implementation choice; absence from these lists is not definitive proof that every QQ2010 build and configuration was silent.

## Integration guidance

1. **Incoming message:** `msg.wav` is the strongest candidate for audition/comparison. Before labeling it a classic original, compare it with a lawfully obtained QQ2010 installation's sound file and record source/version/hash.
2. **Friend online versus cough/system notification:** Keep these as separate events. Contemporary sources support `Global.wav` for online and `system.wav` for system/verification notices. No accessible `Global.wav` with sufficient provenance was verified here; leave that slot empty rather than misusing the system cue.
3. **Sent message / generic button clicks:** No authentic QQ2010 default cue was established. Prefer silence to an invented sound advertised as original. Any custom subtle feedback must be labeled a design addition.
4. **Nudge / file transfer:** No verified source or original mapping was established. Do not expand the UI solely to justify additional cues. Keep a muted/unassigned slot until the UI actually supports the event and a source is validated.
5. **Fallback:** An original procedural approximation can preserve a period feel, but label it “QQ-era inspired / recreated,” never “QQ2010 original.” Keep it separate from any third-party recording.

## Research limitations

The available repository supplies accessible candidates and source-code mapping, and contemporary secondary articles corroborate classic-era event names. They do not supply extraction provenance, Tencent authorization, or a QQ2010 comparison baseline. Official/primary proof for the exact historical sound set remains missing. Search-result or nostalgia-upload labels alone are insufficient.
