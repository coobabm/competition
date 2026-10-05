# Desktop media and notifications

Goal: implement the requested casino ad copy/midgame gating/colourful artwork, meaningful YY avatars, generated supporting artwork in current Main apps, and audible visible incoming-message reminders. Preserve existing DreamOS shell, narrative IDs, wallet and save behavior. No automatic GitHub push (not requested this turn).

Execution: independent implementation tasks under subagent-driven-development, shared Art API agreed below. No workers control Unity/scene or commit; parent owns image generation, import and live validation. Work in /Users/kk/competition; preserve concurrent edits.

## Asset and API contract (parent ownership)
- Native image_gen produces actual raster assets, not coded stand-ins. Store in Resources/LingGuangV05/Media.
- `LingGuangV05.Desktop.Media.DesktopMedia.Avatar(string id)` returns Sprite from a 4x4 atlas. Accepted keys: me, laozhou, cousin, qingwen, ajie, xiaogang, dawei, netbar, zhou_now, dad, mom, aunt, wang, liu, xiaolu, afang. Alias/group-prefix helpers belong here.
- AI's established 8x8 zero remains for lingguang: narrative invariant, not replaced by a human portrait.
- `DesktopMedia.Picture(string key)` returns Sprite. Keys: casino (standalone), ai, gpu, desk, esports, space, phone, cinema, shopping, flowers, plush, headset, data, go, news, books, laptop (4x4 content atlas).
- `DesktopMedia.Paint(RectTransform parent,string key,bool cover=true)` attaches non-raycasting image child, preserves aspect ratio/crops via container mask. Missing art must fail gracefully without covering existing controls.
- No image processing in Python; atlas slicing happens through Unity Sprite.Create regions.

## Tasks
1. Parent: generate 3 artwork assets (casino background, portrait atlas, app content atlas), persist outputs/prompts/provenance, implement caching service and key tests. Ad exact copy: 缺钱吗？\n来两把试试手气; footer 游戏内广告 only; English equivalent; stage>=3 and 120 active eligible seconds preserved.
2. Notifications worker: YY, Tieba private messages, Juxin, mail/native notifications and completed-message actions; visible reminder + sound, no own-message/old-save replay, no popup storm, respect mute/cutscenes. Reuse existing tray/Sfx architecture; add targeted tests.
3. Media views worker: all YY people/self/group sender avatars (not just contact list), Tieba/ Bodu illustration rows, Juxin portraits, Taohuo/Miaoyu product pictures/gifts/dataset cards. Reuse existing useful imagery; generated assets replace letter-only/flat empty illustrations. Never fabricate clickable actions or alter game rules.
4. Parent: integrate/independent review, compile + focused tests, actual Play evidence where editor available, restore original test settings/player saves.

Review focus: missing asset fallback; image raycast/input interception; sprite crop geometry; stable identity mapping; queued notifications across save swap; excessive repeated notification audio; read conversation vs unread badges; simultaneous casino/tray overlap; old AI avatar preserved.

Progress: generated five PNGs (casino backdrop, 16-portrait atlas, 16-illustration atlas, Lili portrait, period-correct iPhone gift art); resources copied into project with provenance. AI 8x8 zero retained. Independent review caught bilingual group aliases/Lili, gift mismatch, narrow-news layout and casino love/hologram gate; these were fixed. Notifications worker delivered 10/10 focused budget tests and 20/20 chat regression; no live Unity claim yet.

Validation checkpoint: current Unity compilation clean; isolated media component probes 3/3, .NET chat 31/31, casino regression 33/33. All source-review findings fixed, including specific suppression of AI-joins scripted receipt notifications while its own scene shows/dings them. Full Main UI/audio Play pass awaits permission to restart the existing shared test Play; do not claim whole-game visual/audio acceptance from these checks. See Evidence/DesktopMedia/verification.md.
