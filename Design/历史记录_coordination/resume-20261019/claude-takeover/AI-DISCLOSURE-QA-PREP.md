# AI disclosure actual UI QA extension

Added only `Assets/LocalBuild/Editor/BrainWindowPlayQa.AiDisclosure.cs`. Parent will enqueue `EnqueueAiDisclosure()` after persona archive steps. No Unity/game was launched; compilation and resulting screenshots are pending parent execution.

Uses the actual runtime YYChatHub, actual YYChatView and production ChapterOneDesktopRouter.Open for LingGuang and Lao Zhou. Sets only `S.lingguangUnlocked = true` as an explicitly reported contact-availability fixture. No Send/Receive call or model-generated/fabricated response is supplied. Existing scoped GameText memory locale is reused. Both LocalLlm and LocalEmbedder must remain disabled.

Eight queued steps produce four actual rendered-desktop PNGs and matching JSON diagnostics:

- `ai-disclosure-lingguang-zh`: `AI 合成内容 · 生成回复仅供参考`
- `ai-disclosure-lingguang-en`: `AI-generated replies · For reference only`
- `ai-disclosure-npc-en`: `Some replies are AI-generated · For reference only`
- `ai-disclosure-npc-zh`: `部分回复为 AI 合成内容，仅供参考`

Checks actual contact selection and window visibility; exact localized fixed-header label; active TMP and visible glyphs; CanvasRenderer cull/inherited alpha; no TMP overflow or preferred-size overflow; label inside its bar; bar inside conversation; bar between header and message viewport without geometric overlap. Does not resize the default YY window. QaDesktopCapture.Save reads the actual rendered LCD texture and verifies a nonblank PNG. Screenshots still require visual review for other desktop windows occluding the notice; geometric checks do not claim full desktop occlusion detection.

Each JSON clearly labels actual UI / contact fixture / local models disabled / no message sent or generated inference claim. This verifies disclosure presentation only. Tieba, title/help and other product surfaces are not included in this minimal extension.
