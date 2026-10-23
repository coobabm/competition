# 群预设生命周期 v2（Claude batch-01）

状态：**纯文件修订的候选，没编译、没跑 NUnit / Editor 检查、没启动 Unity，也没做 git apply --check。** 生产工程 `competition-latest-20261016` 没有被改动。

- `candidate/Assets/...`：所有改动和新增文件的完整新版本。已有的生产文件以**当前生产内容**为底，叠加 v1 候选的改动和本轮修复。
- `group-preset-lifecycle-v2.patch`：对当前生产文件的统一 diff（a/ b/ 路径相对工程根，新文件用 /dev/null）。**手写，未做 apply-check。** hunk 行号和行数已按生产文件逐个核对，但空上下文行写成了空行（git/GNU patch 一般都接受）。如果 apply 失败，就以 `candidate/` 里的完整文件为准，再用 `git diff --no-index` 重新生成补丁。
- 已有文件的完整副本是手工转写的，没有逐字节 diff。我核对过行数（YYChatHub.cs 845→860、LaoZhouNewsChecks.cs 212→229、WorldYyGiftRoutingChecks.cs 122→187、GroupPresets.cs 129→145），也抽查了含特殊字符的行，结果一致。合并前请先 diff 一次。

## 相对 v1 的改动及原因

| # | 问题（v1） | v2 处理 |
|---|---|---|
| 1 | `WorldYyGiftRoutingChecks` 断言 `GroupPresetChoices` 恰好 3 个；新语义下有已发送记录或当天已发时会失败 | 新增 `CheckGroupPresetLifecycle`：资源当日可选数 ≥3（原断言隐含的前提，保留）；气泡 ≤3；新档当天未发时 = min(3, 可选数)；每个气泡都是真实可选预设，互不重复，不含已发 id；getter 连调两次结果不变，存档状态不变；显式冻结只生效一次，冻结结果等于预览；真实发送 1 个后消失、当天不补、该 id 离开可选池、同一文本不再算预设发送；翌日 ≤3 个且不含已发 id，冻结一次。双语和私密话题的内容检查移到前面，在完整池上跑，避免发送后检查范围变小 |
| 2 | 每次 BindSave 后第一帧 `observedGroupDay=-1`，导致 `Touch()`，非 TestMode 下每次读档都 MarkDirty | BindSave 里 `BindGroupPresetDay(sim)` 记下当天并冻结，**不标脏**（冻结结果完全由存档决定，没保存就退出、重读时结果相同）。`TickGroupPresetDate` 只在真实换日**并且**冻结状态确实变化时才 `Touch()`；绑定时没能观测到 sim 的首帧只调 `Changed`，不标脏 |
| 3 | `GroupPresetChoices`（读路径）改写 dailyIds 却不保存 | Core 层拆成两个接口：`Freeze`（唯一写路径，返回是否变化）和 `Visible`（纯读：已冻结就过滤；未冻结就预览 Freeze 会选什么，但不存）。Hub 层 `FreezeGroupPresetDay()` 是唯一显式刷新入口；`GroupPresetChoices`/`AvailableGroupPresets` 不再 Repair、导入或冻结 |
| 4 | **编译错误**：`YYChatHub.GroupLifecycle.cs` 用了 `GameText`（在 `LingGuangV05.Runtime`），却没写 `using` | 补上 `using LingGuangV05.Runtime;` |
| 5 | 吞作者回复：第 2/5 次自由输入如果正好触发未来名彩蛋（`YYGroup.FutureNameReply`，GPT/Claude/豆包等），或旧档里有待发的测验脚本回复（`quizReplies`），冷场会删掉 pending，作者台词就丢了 | `TryGroupColdTurn(text)` 遇到这两种情况直接放行，不计数，也不清 pending |
| 6 | 当天还没冻结时先 MarkSent，后续冻结按缩小后的池子重算，剩下的气泡会整体错位，等于变相补位 | `SendGroupPreset` 在 MarkSent 之前先 `FreezeGroupPresetDay()` |
| 7 | `StartGroupColdScenes` 内部调 `RepairGroupLifecycle`（会导入历史），会在 OnEnable 等地方产生副作用 | 改成只读判空 |
| 8 | 周边界用 `GameCalendar.Now`（新年后会跳到 2017，并混入墙钟） | 改用 `GameCalendar.DateOf(CurrentDay)`，和预设冻结用同一个“游戏日”；周一到周日为一周。新增 `WeekKeyForDay` 及测试 |
| 9 | 空安全 | Core 各写路径都接受 null 状态；`Visible` 对旧档 null 列表只读不修；导入历史时跳过 null 会话/预设；协程判断 `groupPresets?.pendingColdScenes`；协程恢复后确认队首仍是同一 scene 才播 |

JsonUtility 结构检查：`GroupPresetState` 只有 bool/int、`List<string>` 和 `List<GroupColdScene>`（只含平坦 int），没有嵌套 List 或 Dictionary，可以序列化。旧档缺这个字段时用字段初始化值（historyImported=false），下次绑定会按真实文本精确回填。

跨档隔离：BindSave 先停冷场协程（generation++），群回复 generation++，清空 pending/inFlight；协程每一步都检查 `ReferenceEquals(S, expected)`；`groupColdOwner` 参与 `GroupScriptPlaying` 判断；静态的只有资源表 `groupPresets`。

## 文件清单

修改（以当前生产为底）：
1. `Assets/LingGuangV05/YY2016/YYChatHub.cs`：YYState 新增 `groupPresets`；BindSave 里停旧冷场、Repair/回填、无脏冻结、恢复冷场；Send 里在预设之后接入冷场（贴纸不计）；TickReplies 加冷场 generation 守卫；Update 调 `TickGroupPresetDate`。
2. `Assets/LingGuangV05/YY2016/YYChatHub.GroupPresets.cs`：`LoadGroupPresets` 静态拆出；`AvailableGroupPresets` 改纯读并排除已发；`GroupPresetChoices` 改纯读（用 `Visible`）；`SendGroupPreset` 先冻结再 MarkSent；OnEnable/OnDisable 加冷场启停；`GroupScriptPlaying` 把冷场算进去。
3. `Assets/LocalBuild/Editor/LaoZhouNewsChecks.cs`：明确的 `PresetFixture`；新增断言：发出后当天不补位、预设发送不计入自由输入、rebind 后已发 id 和同日冻结结果不变；跨档的“新请求”改用另一条未发预设。
4. `Assets/LocalBuild/Editor/WorldYyGiftRoutingChecks.cs`：见上表第 1 条（**v1 漏改了这个文件**）。

新增（各带 .meta，guid 沿用 v1）：
5. `Assets/LingGuangV05/Core/GroupPresetLifecycle.cs`：纯策略加可序列化状态（Core asmdef 设了 noEngineReferences，本文件不依赖 Unity）。
6. `Assets/LingGuangV05/YY2016/YYChatHub.GroupLifecycle.cs`：回填、显式冻结、无脏绑定、换日刷新、冷场判定和持久化播放。
7. `Assets/LingGuangV05/Tests/Editor/GroupPresetLifecycleTests.cs`：12 个 NUnit 测试。

## 测试清单（都没跑过）

1. Unity 编译通过（重点看 `GroupLifecycle.cs` 的 using，以及 `ChapterOneSim` 和 `YYGroup.FutureNameReply` 能否解析）。
2. EditMode NUnit `LingGuangV05.Core.Tests` → `GroupPresetLifecycleTests`，预期 12/12 通过：SameDayFreezesThreeIdsAndSendingDoesNotRefill、VisibleIsPureAndPreviewsTheFreeze、NextGameDayRefreshesWithoutNeedingAPlayerSend、SentIdsNeverReturnAcrossDatesAndExhaustionDoesNotLoop、SameDayGateChangesOnlyRemoveChoicesUntilNextDate、OldMissingListsAreReadSafelyAndHistoryImportsOnce、OldSaveWithoutFieldLoadsAFreshUnimportedState、WeeklySilenceOccursOnSecondAndFifthRealFreeInputsOnly、MondayResetsCountAndSkippedWeeksNeverInventTurns、GameDayWeekUsesTheCalendarDate、JsonRoundtripPreservesChoicesSentIdsWeekAndPartialContinuation、BothLanguagesHaveAllPublicContinuationLines（再加上全部现有 Core 测试，不得回归）。
3. 菜单 `LingGuang/Checks/World interactions`（`WorldYyGiftRoutingChecks.RunInteractions`）：预期输出 `WorldYyGiftRoutingChecks PASS`。
4. 菜单 `Tools/Local Verification/Lao Zhou historical news consumers`（`LaoZhouNewsChecks.Run`）：预期输出 `LAO_ZHOU_NEWS_PASS n assertions`，n 应比原来多（每个 case×语言多 2 条，每次 rebind 多 2 条）。
5. PlayMode `LifeGroupPlaybackChecks.Frames`：预期仍输出 `LIFE_GROUP_PLAYBACK_PASS`（OnEnable 新增的 `StartGroupColdScenes` 在无冷场时直接返回）。
6. 回归 `RunPhotos` 和其他用到 `GroupScriptPlaying` 的检查（XgPublicDiscourseNotifier、XgLifeHostActivity、XgAppAcquisitionHost、XgEndingPreparationHost）。

尚未编写、需要补的集成检查：真实普通 Send 第 1/2/5 次（第 2 次 pending 被清、冷场 8s+3s 两行）；预设/贴纸/未来名彩蛋/待发测验回复不占名额；语言切换后冷场行的语言；禁用再启用、读档后只补剩下的一行；跨档后旧协程和旧模型回调都不投递；非 TestMode 下读档后不 MarkDirty（可用计数 MarkDirty 的 runtime 桩）；实机 UI 同日发送不补位、翌日无发送也刷新。

## 未验证项和风险

- JsonUtility 对缺失的嵌套字段是否保留初始化值：靠 `OldSaveWithoutFieldLoadsAFreshUnimportedState` 验证，未运行；即使是 null，Repair 也会兜底。
- 绑定时冻结不标脏：若当天先读档并冻结，之后在同一天、下次保存之前有新事件解锁（eraDelivered 变化），然后不保存就退出，重读时冻结会基于新的可选池，可能和上次看到的不同。可见性很低；如果要绝对稳定，就得在绑定时标脏，这又和第 2 条要求冲突。目前取“不标脏”。
- 冷场播放中途的等待进度不持久化，重开后重新等 8s/3s（v1 已说明）。
- 冷场两行之间如果有预设回复插进来（协程等的是 `presetReplyRunning`，但预设协程不等冷场），可能出现“冷场行1、预设回复、冷场行2”交错。没有加反向等待，因为那样会两边互等死锁。
- 冷场之后玩家很快再发第 3 条，模型回复可能夹在两条自聊之间。
- 冷场会丢弃上一条普通输入还没投递的模型回复（v1 的设计，用来保证“无人接话”）。
- 玩家手打一条“已发预设”的原文：现在按普通自由输入计数。手打“当天不在气泡里的可选预设”原文：仍按预设发送并标记已发（沿用原逻辑）。
- 旧档被 MaxHistory（200）裁掉的历史无法回填，不伪造。
- 手写补丁未 apply-check；完整副本未逐字节 diff。

## 需问制作人

1. 冷场台词（大伟/小刚“下班末班车”、阿杰/小刚“夜宵面馆”）仍是 v1 草稿，口吻和次数（每周固定 2 次）是否认可？长期游玩时每周重复同样两段是否可以接受，要不要做成按周轮换的台词池？
2. 冷场是否应该丢掉玩家上一条普通消息待发的回复（现行：丢掉）？
3. 未来名彩蛋（GPT/Claude 等）和测验回复不计入冷场次数，这是本轮为了“不吞作者回复”做的取舍，需要确认。
4. 预设耗尽后群里没有任何预设气泡（不循环、不补造），这一点是否接受，还是要提示“今天没什么可聊”？
