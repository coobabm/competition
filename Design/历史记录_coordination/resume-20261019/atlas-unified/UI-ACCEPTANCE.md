# 主控 Unity 验收清单（尚未执行）

2026-10-19 恢复更新：现在提供独立 `UnifiedAtlasActualUiRunner.Run`（菜单 `Tools/Local Verification/Unified atlas actual UI`）。只由主控单个Unity调用；不再需要接共享runner。输出 `LocalVerification/UnifiedAtlasActual/<UTC>/report.json`。它运行下列原始全量检查后，执行新 `RunBoundaries`：中英双入口未购/真实train.weights购买/能力1–2对stage6门禁，tiny与sequence的原生最小化及关闭，生产BindSave内存切档、封印收藏真实分页，以及真实StageCurtain的月份→演示→首句→解锁→淡出。均已编译、尚未执行；预计原28张+6张边界图+最终全帧，共35张成功截图，失败时数量可能不足。

独立runner保护保存目录、桌面Profile、场景、GameView、输入和全局减少动态偏好；模型禁用，时钟和ctl暂停。购买阶段短暂将TestMode关闭以启用真实有限钱包，仍保持tick/disk关闭并finally恢复。历史能力/收藏、足额钱包、stage6、stage4幕布队列是明确夹具。**不声称自然六阶段、实际玩家鼠标、磁盘换槽或完整剧情自然触发**。窗口重开使用产品Open；关闭/最小化和幕布Continue使用实际LCD映射点击。

剩余未编码/未运行的范围：真实滚轮长文、最大化尺寸、全部收藏项逐一布局、磁盘slot切换；以下旧列表中已新增代码的分支，也必须等此runner实际通过才算完成。最新证据见 `RESUME_REPORT.md`。

核心 NUnit 入口：`LingGuangV05.XingGuang.Tests.XgUnifiedAtlasChecks.UnityRoundTripAndReadOnlyReplay`。纯 .NET 同一断言函数已跑80项；Unity JsonUtility 往返需要本项再验。

实际 LCD 辅助：`UnifiedAtlasActualUiChecks.Run(ctl, language, actualLcdClick, capture)`，由现有隔离 runner 调度 IEnumerator。它不创建真实档；调用方需准备独立 TestMode fixture、暂停 `ctl.enabled=false`，已关闭其他弹窗/待播过场、无活动训练，能力1–6已实际授权于 fixture、viewer已购。知识卡/已答题/结构状态使用已声明的历史夹具，不冒称自然流程。普通分支应确保全局 ReduceMotionPref 为0。每次点击走 LcdActualUiProbe.WaitForClick 及调用方的真实LCD点击。

辅助覆盖：科技入口、全部6项能力选择和重新演示、中英×普通/减少特效（24视图）、两类收藏×中英（4视图）、名字页更多入口与同一实例、关闭、换页清理；每次重览比较完整存档JSON，检查无新奖励/样本/阶段进度；检查序列演示标题/说明/结果无TMP溢出。期望28张截图，当前0张。

必须补的交互分支：

1. 新教学档未购viewer：从名字页和科技进入均不能重览；封印可定位 train.weights，正常购买后两个入口随即开放已学能力。旧档兼容按原 TrainPart 权限，不加新收费。
2. S.stage=6但能力仅1–2：3–6显示未获得，无法重览。未收集知识卡、未答题、未购/未使用结构不露正文或结构图。
3. 让序列/小模型正在演示，然后通过真实 .exe 原生最小化和关闭按钮操作；恢复后无旧演示。换档后不保留另一档的目录选择或演示。
4. 打开图鉴后触发真实已授权升阶/独占故事：图鉴关闭，原StageCurtain月份→对应演示→首句→解锁→淡出顺序和字幕同步不变；不重复排队或领奖。
5. 用真实滚轮读长知识卡/结构说明，翻完所有分页；核对窗口正常/最大化尺寸，列表、正文和演示无裁切/重叠。新入口工具栏不挤压已有按钮。
6. 使用混合收藏旧档：确认原结构图鉴、现象、知识卡均保留，图鉴重览前后未读/金币/样本/已获能力/待播队列一致（打开科技页已有清未读行为在进入图鉴前取基准）。

这些分支若失败需修候选并保留失败证据；不能仅据静态检测称图鉴实际可玩验收完成。
