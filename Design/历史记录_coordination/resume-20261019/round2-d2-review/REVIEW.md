# D2 read-only review — 2026-10-19

结论：不建议整包合入当前 D2。可单独合入 `tieba-d2-reviewed.patch`，它只修正源码明确的 Go 后强制重复重建，并增加原因/阶段测量点；没有性能收益或运行通过结论。未改生产、未启动 Unity、未编译。仅运行文本检查和暂存副本的 `git apply --check`，后者退出码 0。

## 交付完整性与基线

审查时 `ClaudeReturnRound2/D2/` 只有 `TiebaView.cs.diff`（18:52）和 `files/Assets/LingGuangV05/DesktopBridge/Tieba/TiebaView.cs`（18:51）。缺少分工要求的 `REPORT.md`、`QUESTIONS.md`，因此尚无作者的采样/推断分离、回归清单和明确未执行声明。这里的审查不能替代作者交付完成声明。

生产文件 SHA256 `a6cb1a284143e2f230fba310934f3e1e628a58d699ca7addab585f5a98464d7d`，与手写 diff 声明的快照完全一致。完整候选 SHA256 `8e49e7a6484a91c64f41993322b288d3af427f7a35ca579c459963e7347caa44`。所有哈希、静态断言和未运行状态记入 `static-review.json`。

原 diff 自称 hunk 行数未经工具校验，不能直接当成可靠可应用 patch。已从完整候选生成 `submitted-normalized.patch` 供审阅；**它不是建议合入的补丁**。实际建议合入的是另一个缩减文件 `tieba-d2-reviewed.patch`，只涉及允许范围中的 TiebaView.cs。

## 证据与路径

采样证明：`performance-audio-reviewed/CURRENT-FINDINGS.md` 和对应 `current-20261019-092802-summary.json` 记录 Editor RAW 切换帧 1041 的 TiebaView.Update 47.745ms inclusive / 5.900ms self，以及帧 1186 的 39.696 / 5.314ms。没有足够子调用数据把这两个峰值分别归到侧栏、主页或销毁。包含子层的时间不能相加；Editor、模型关闭、旧源采样不代表当前 Player，也不能说明音乐连续性。

源码证明（以下行号针对当前生产基线）：

- TiebaView.cs:62–70 在 Start 建布局并订阅 Hub/GameText 的 Changed，将 signature 清空。
- :154–165 的 Go 立即 Redraw，但把 signature 留空。之后 :179–191 的可见窗口 Update 经过原有 0.5 秒节流，会计算非空 now，因此再次 Redraw，即使没有数据或语言变化。此重复是确定的控制流；不能据此声称它贡献了上述峰值的全部时间。
- :213–247 的 Redraw 更新导航、调用 DrawSide、保留聊天输入草稿/焦点、Destroy main 下全部子对象，然后重新 DrawChat 或 NewScroll + 对应页面。
- :249–265 的 DrawSide 每次 Destroy 所有侧栏子对象，再创建文字、Image/Button/图标。:267 起 NewScroll 创建 ScrollRect、遮罩、布局组、ContentSizeFitter；页面方法继续创建 Card 和 TMP。这条路径能解释为何值得测量构建和后续 UI 生命周期成本，不能证明具体毫秒占比。
- Unity 的 Destroy 为延迟销毁；ClearMarker 只包住排队调用，实际 TMP.OnDestroy、后续布局/网格/Canvas 工作可能落在其他 marker/后续帧。不能把 ClearMarker 当作总销毁成本。
- Hub.cs:63–79 换档 Bind 更换 S、清 pending、EndScript 并触发 Changed；:162 MarkSeen 有实际未读时清零并 Touch；这些入口与 View 的可见性判断、事件订阅在缩减补丁中保持原样。

## 候选取舍

| D2 内容 | 决定 | 依据 |
|---|---|---|
| 原刷新键提取为 RefreshKey；Go 绘制前记录该键 | 采纳，但采用生产原键逐字抽取 | 解除上述确定重复。绘制期间/之后的 Changed 仍能清空键，不吞新事件。Go 原有导航定位、MarkSeen、草稿清空不动。 |
| Go/Cleared/KeyChanged、RefreshKey、Side/Clear/Page markers | 采纳 | 当前 RAW 缺精细归因；名字含 Tieba，既有 IntegrationProfilerCapture.cs:245 筛选可收录。不保留原候选“未采样时零成本”绝对表述。 |
| GateKey 替换 `(int)(secondsSinceHelp / 10)` | 本轮不采纳 | 是刷新触发策略变化，包含新增 flag 扫描与定时 gate 缓存，超出本次“保持门禁/剧情/节拍”小修。当前 forum.json 的 40/90/150 秒阈值均为 10 的倍数，不能虚构一个现存 14 秒阈值回归。完整时间与 flag 场景未经运行，不能断言等价。 |
| RefreshKey 增加 DailyDataLinkHighlight | 本轮不采纳 | 这是额外刷新触发依赖，未见对应 UI 失效复现；保留现有行为以隔离本次性能修改。 |
| Open 提前赋目标，goCalls 检测 OnEnable 是否画过 | 暂不采纳 | 改变原生激活/恢复时序。OnEnable 画过不等于 EnsureShown 已完成可见性恢复，现有证据未验证隐藏打开、最小化、跨档组合。没有声称已复现回归；本轮保持 Open 逐字不变。 |
| DrawSide 缓存 language/selected board/Boards.Count | 暂不采纳 | 能减少明显重复创建，但 key 没有表达 Library/Board 对象与内容身份，回调捕获 board 对象。当前生产库通常固定，未发现同数换库的实际产品路径，故不是已证实缺陷；为满足严格生命周期范围，先测 Side 后再决定缓存。 |

缩减补丁保留可见性早退、原 0.5 秒节流、所有原 key 字段/10 秒桶、Open、OnEnable/Start/OnDestroy、DrawSide 与其后全部页面方法，以及 Hub/Core/论坛资源。没有额外静态缓存、存档字段、后台任务或异步回调。

## 仍需实测与已有回归入口

这些是下一步验收清单，不是本轮已执行结果：

1. 在相同 Editor fixture/禁模型/预热和采样时长下对照采样；定位同状态 Go 后应只有 Go 的 Redraw，下一轮 Update 无 Cleared/KeyChanged Redraw。发生真正变化时必须仍绘制。报告原因次数与各分区的 inclusive/self、分配和后续 Canvas/TMP，而非相加嵌套时间。
2. 原生关闭/再开、最小化/恢复、窗口切换和首次打开；确认隐藏时不绘制、不错误标已读，恢复后目标和未读正确。
3. 聊天新消息、typing、choice、已有草稿/焦点、输入发送、头像、首页/板块/帖子/收件箱/公开讨论；保持 Go 的顶部/聊天底部定位。已有事件导致的重绘仍有旧滚动行为，本补丁未承诺修复所有滚动重置。
4. 中英切换、日历/阶段变化、40/90/150 秒帮助回复、门禁与数据请求回复、动态 AI 名；对照旧代码相同 fixture 时刻，不改规则来让检查通过。
5. 换档后显示新状态，原旧消息按钮守卫继续拒绝旧 owner；不让记录、门禁或回调跨档混用。
6. `LaoZhouNewsChecks.Run`（历史新闻消费者核心）、`LaoZhouNewsCommentsUiChecks.Frames`（在现有独立 Sidebar/Main 宿主执行新闻分享实际 UI）、`PublicReadingInteractionUiChecks.Run`、`LaoZhouQuestionsUiChecks.Run`；公开讨论另参考 `PublicDiscourseUiChecks` 现有调用方。先确认各 runner 的隔离/串行前提，不把 Frames 方法当作独立菜单。
7. 最终 Player 与音频由主任务按原验收做；本次补丁不能给出音频卡顿已解决、稳定 60fps 或收益百分比。

## 文件使用

- `tieba-d2-reviewed.patch`：建议的精确小补丁，已在暂存基线 `git apply --check`。
- `files/Assets/.../TiebaView.cs`：对应完整审阅版本，仅用于核对；若生产再变化应重验 patch，勿盲目覆盖。
- `submitted-normalized.patch`：原作者完整候选的标准 diff，未批准整包合入。
- `baseline/TiebaView.cs`、`build_review_patch.py`、`static-review.json`：可复现文本审阅与哈希记录。

生产未修改；编译、Unity、运行回归、性能采样均未执行。
