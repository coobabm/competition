# Claude 第 1 批（2026-10-19，已按用户要求暂停）

基准：生产工程 `competition-latest-20261016`（HEAD 3533141a，含未提交改动）。**生产树未修改**，全部为候选。
工具限制（事实）：本会话 Linux 命令环境启动失败（sessiondata.vhdx not found），只有文件读写；**未编译、未跑任何测试、patch 均为手写未 apply-check，未计算 hash**。合并时以 `candidate/` 下完整文件对照生产 diff 为准。

| 目录 | 内容 | 状态 |
|---|---|---|
| `用户确认_20261019_Claude会话.md` | 用户本日新确认 5 条（老周知情修订、终局以「待办_终局记忆与发布抉择」为准、敏感词综合、Qwen3-4B、888 骗局） | 需写入决策记录 |
| `group-preset-lifecycle-v2/` | 修 v1 三处风险（WorldYyGiftRoutingChecks 3 选断言、读档标脏、读路径改状态）+ 缺 using 编译错误、彩蛋/测验回复被冷场吞掉、周边界 | 未编译；README 有 12 个 NUnit 与检查预期、待审问题 |
| `ending-small-A/` | A1 卷首句；A2 老周问句贴吧兜底；A3 终局阶段老周主动找上玩家（新字段 finalApproachSent）；A4「因为你在 X 月 X 日……」；A5 摆渡旧信入口在新流程隐藏 | 未编译；A3 台词待制作人审；老周 LLM 提示词「不知道秘密应用」与 A3 的冲突未处理 |
| `casino-scam/` | 888 杀猪盘叙事、AI 按阶段评论、群友只评网站、+300 spam 样本、提示词一行、gamble_888 改真实事件触发（去关键词误判）、11 个新测试 | 未编译；阈值与新台词待审；未含特效 juice 与新卡 |

未开始（暂停时）：敏感词综合（以 XgEndingTextGuard 为主体）、代码注释 Qwen3.5→Qwen3-4B 统一、统一图鉴候选接入、已读新闻 UI 检查接入 SidebarLayoutChecks、桌游/私人结局/群通知 UI 复验（需 Unity 前台）、性能、开发日志。
需用户写/定：R3 是否「它选完自动执行」、Unpinned 红笔句、「早了六年」/R4 字幕文案、四张记忆卡偏版文字。
