# 本轮小范围修复只读审查

审查对象：当前工作区的 `GameHubView.cs`（sampleNote、commentary 双语、AiDisplayName）、`GameHubView.BoardReader.cs`（标题/帮助）、`YYChatView.cs`（nativeStatus）、`XgEndingPreparationHost.Narrative.cs`（RefreshSeedLanguage）。仅阅读这些改动及直接相关的重绘/存档绑定调用；未修改生产代码、运行测试或启动 Unity。未检查 Claude 终局或 888 交付。本报告与此前 legacy 联系人问题独立。

## P2：双语状态已保存，但语言切换未触发棋盘重绘

- 位置：`Assets/LingGuangV05/DesktopBridge/Games/GameHubView.cs:117-121`、`:263-280`。
- 本轮 `sampleNote` 改成标记、commentary 保留中英文，只有调用 `LabLines()` 时才按当前语言渲染。`GameText.Changed` 注册的是 `Relabel()`，该方法更新导航和首页卡片，没有对当前棋盘调用 `Refresh()`。
- 触发：中文完成飞行棋后，保持当前棋盘页面，切为英文并返回窗口。`LudoPanel.Update()` 第98行在 `game.Over` 时直接返回，故状态区仍保留完成时的中文；完成提示的新英文分支也不会执行。只有切页再进入、重新开局或其它真实重绘后更新。阶段6评论在等待玩家回合时同样可能残留旧语言。
- 证据：`LudoPanel.cs:98-106,121-123`；正常切页会重绘的路径是 `GameHubView.cs:258`。仅在测试夹具额外调用 `Refresh()` 不能证明玩家原地换语言生效。
- 最小建议：在语言切换的显示刷新路径中对当前棋盘执行一次纯显示 `Refresh()`；不要用 `RestartRound()`，也不要产生新完成回执。验证已结束/等待玩家两种状态原地换语言，棋局、回执数量保持不变。

## 相关既有隔离缺口：部分棋盘的完成提示仍会跨档保留

**不是本轮新增回归，列在此处是因为本轮明确审查 sampleNote 的跨档隔离。**

- 位置：`Assets/LingGuangV05/DesktopBridge/Games/HubGame.BoardSessions.cs:29-33`；`GomokuPanel.cs:66-70`、`GoPanel.cs:75`、`ChessPanel.cs:225-234`。
- 触发：在存档 A 完成五子棋、围棋或国际象棋，使 `sampleNote` 非空；切换到存档 B。`SyncBoardOwner()` 调用对应 `RestartRound()`，这些重开路径清除 commentary、棋局与本局记录，但没有清空 sampleNote。因此 B 的全新棋局也可显示 A 留下的“对局已结束”提示。
- 风险限于显示：未发现本轮标记改动将 A 的实际完成收据或奖励写入 B。飞行棋、跳棋、大富翁的重开实现已显式清空 sampleNote。
- 最小建议：仅在 owner 改变时统一清除完成提示与评论，再启动新 owner 的棋局；保留同档普通重开时有意保留历史提示的规则。此项可单独后续处理。

## 未发现新增问题的部分

- `AiName` 改为 `lab.AiDisplayName` 与现有名字策略一致，Lab 缺失仍有后备显示；新增 commentaryEn 在实际产生评论时与中文同时赋值，中文为空时不渲染旧英文。没有新增存档字段或进度写入。
- Board reader 标题、帮助和边距为显示改动，英文保留“本站标准 240×240 / 15×15、黑棋下一手”的能力边界。现有读取回调仍校验 owner 和 state；没有新增空引用路径。半屏文字是否实际溢出需主进程当前 UI 验收判断，本审查不作视觉通过声明。
- `YYChatView.nativeStatus` 安装时保存原生状态标签引用，`OnLanguage()` 判空后刷新，未增加消息/已读/收据写入；销毁时仍取消语言事件订阅。未发现新增跨档数据依赖。
- `RefreshSeedLanguage()` 只更新既有 TMP 标签和本地语言缓存，不调用答案提交、记忆发现或冷却写入；按钮继续使用原 prompt、选项索引与捕获的存档绑定。`ObserveSave()` 在 `PollNarrative()` 前运行，换档时 `ResetNarrative()` / `CloseSeed()` 清空本轮新增引用。完整创建后的标签与 options 数量匹配，未发现正常生命周期下的新增空引用。

边界：只读静态审查，不代表第二轮 BoardGamesUiPlayQa 或其它运行验收已通过。报告写完后停止。
