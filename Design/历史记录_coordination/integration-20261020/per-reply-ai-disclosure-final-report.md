# Per-reply AI disclosure — final main-surface freeze

Supersedes the earlier blanket-footer scope. Exact shared text stays: AI 合成内容 · 仅供参考 / AI-generated content · For reference only. No model/prompt/history/save data changed.

Implemented per non-player reply:
- YY BuildMessages: every non-system NPC text/photo/sticker/file/transfer/link row has a separate 22px label below its rendered row; player and system rows excluded. Original fixed footer remains as a compact overview. Learning drawer and Lao Zhou choices still reserve footer space.
- Tieba: shown authored floors, discussion/private messages, latest-reply preview, era post/reply and board-club post. Player, deleted/folded/system rows excluded. Blanket window footer removed.
- YY/Tieba public-discussion archives: individual discussion excerpts labelled; kind=historical source summaries excluded.
- Reading: dedicated notice immediately after the character reaction inside scrolling content, only visible with nonempty reaction. Material/source excerpt not labelled. Blanket footer removed.
- Teaching: actual AI prediction/comment/family-question rendering gets an independent small final text line. No-model/system directions not labelled. Removed the generic workspace/TeachCompanion instruction footers and restored their geometry.
- Desktop notifications: explicit transient aiReply flag passed only by YY received character messages, book-completion character messages and Tieba replies. TrayPopups reserves24px below preview only for flagged replies; downloads/file-received/system/overflow notices keep default false. No saved schema changes; same notification scheduling/callbacks/budget.

Shared APIs remain Runtime/AiContentNotice.Footer, CreateFooter and BindFooter; AiContentFooter performs UI-only language refresh. Parent added Runtime assembly TMP/UI refs.

Verification: parent reports Unity compilation and isolated BrainWindowPlayQa.RunAiDisclosure passed, actual group screenshot already sent to user from Evidence/BrainWindow/20261019-171643-941. Existing game-provided netbar history contains multiple NPC replies plus player text; no generated fake screenshot or fabricated model reply. QA asserts individual notice count matches real non-player/non-system history, player excluded, zh/en labels. Latest follow-up (Reading/public archives/notifications/Teach) statically parsed and git diff --check passed; parent still needs final Unity compile/Play coverage. BookLibraryUiChecks.ReadAll now checks reaction-specific label directly follows reaction, localization, fit and no raycast after real chapter completion.

Other ownership to include in final acceptance: parent covered Taohuo Wangwang bubbles, Miaoyu buyer/discussion replies, Zhongbao subcontract messages with scroll, GameHub AI comments, DeskGame speech, Bodu Lao Zhou/news/discussion replies. Design agent covers narrative/ending/finale/Love/letters/Spider character replies. Old native YY adapter is disabled/replaced by current YYChatView in Main (OS audit); intentionally not expanded.

Formal source freeze complete. No Unity launches or commits by this agent.
