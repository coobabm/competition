# 群预设生命周期：恢复交付报告

状态：**独立候选完成；4个程序集隔离编译通过、9个纯核心NUnit测试通过、生产git apply --check通过；未应用生产，未启动Unity。** 2026-10-19。

## 交付与基线

- 目录：`games/.coordination/resume-20261019/group-preset-lifecycle/`。
- `group-preset-lifecycle.patch`：相对当前生产工作树的增量，SHA256 `db578cb66993c97f559f7d710c7cb988d68d2420c3ea6e1b625fd5a6e2505653`。
- `candidate/`：12个候选文件；其中4个修改文件、8个新增文件（4组CS/meta）。`NEW_FILES/`单独提供全部新增文件；完整patch也包含它们，**两者不应重复应用**。
- `manifest.json`：生产HEAD `3533141a406940d571e064f3797328c7d214235a`、4个原文件SHA256及12个候选SHA256。打包时逐字节确认生产4文件仍等于baseline。
- `baseline/`：开始接手的生产4文件快照。`original-v1/`与`README.original-v1.md`保存此前草稿，仅作历史。
- 只读审阅并吸收 `../../claude-batch-01/group-preset-lifecycle-v2/`；没有修改Claude交付目录。不得再叠加v1或v2补丁。

## 最终行为

1. 每个游戏日显式冻结最多3个ID；getter纯读，语言切换共用ID；已发永久排除、当日不补位、耗尽不循环。绑定不标脏；真实换日冻结并通知/标脏。日期资格、日快照、周计数统一基于 `GameCalendar.DateOf(CurrentDay)`，不混入墙钟/新年演出日期。
2. 旧档null字段修复；仅真实玩家、非AI代发、非贴纸、中英文逐字精确的尚存预设历史导入；不从旧groupPresetTurn猜历史或自由输入次数。最大200条裁剪掉的旧历史无法恢复，不伪造。
3. 每周一重置普通自由输入次数；第2与第5次无接话，延迟8秒/3秒公共自聊。引导/已处理脚本/预设/贴纸/未来名彩蛋/待发旧测验不占计数；未完成场景及nextLine持久化。重载重新等待该行完整延时，不宣称剩余秒数恢复。
4. 冷场等待StoryFocus、Intro、作者队列、群引导脚本；不新增模型请求，不使用玩家AI/训练/后台进度作群友知情依据。
5. 进一步修复Claude v2遗漏：未来名彩蛋即时进入现有作者回复队列，避免“彩蛋→下一条恰逢冷场”丢作者回复；预设进入队列时也取消旧通用请求的owner generation；旧测验回复保留。彩蛋中的莉莉发言仍须已入群。
6. 换档首先停并清旧作者队列和冷场；同档禁用保持原作者队列恢复逻辑。作者与冷场均用state owner/generation，旧迭代器finally不清理新档队列。普通模型的最终交付回调亦检查generation。

## 文件与职责

修改：
- `Assets/LingGuangV05/YY2016/YYChatHub.cs`：状态、绑定/换档、Send分流、通用回复generation守卫、换日tick。
- `Assets/LingGuangV05/YY2016/YYChatHub.GroupPresets.cs`：日冻结读路径、发送去重、共用作者队列。
- `Assets/LocalBuild/Editor/LaoZhouNewsChecks.cs`：旧内容消费者夹具适配、预设不计自由输入、JsonUtility重绑保持冻结。
- `Assets/LocalBuild/Editor/WorldYyGiftRoutingChecks.cs`：最多三项/纯读/同日无补位/翌日排除已发断言，保留原资源/隐私检查。

新增（均含meta）：
- `Assets/LingGuangV05/Core/GroupPresetLifecycle.cs`
- `Assets/LingGuangV05/YY2016/YYChatHub.GroupLifecycle.cs`
- `Assets/LingGuangV05/Tests/Editor/GroupPresetLifecycleTests.cs`
- `Assets/LocalBuild/Editor/GroupPresetRuntimeChecks.cs`

## 实际执行证据

只使用本候选的 `verification/` 输出，生产文件/Library不写入。生成项目读取当前Unity csproj引用，将候选源文件覆盖到隔离编译列表；DLL全部输出本目录。

|检查|命令/日志|结果|
|---|---|---|
|Core编译|`dotnet build verification/LingGuangV05.Core.csproj --nologo -v:q -m:2`；`verification/compile-core.log`|0错误、1警告|
|桌面编译|`dotnet build verification/Assembly-CSharp.csproj --nologo -v:q -m:2`；`verification/compile-desktop.log`|0错误、538警告（包含既有工程警告）|
|Editor编译|`dotnet build verification/Assembly-CSharp-Editor.csproj --nologo -v:q -m:2`；`verification/compile-editor.log`|0错误、241警告（包含既有工程警告）|
|测试程序集编译|`dotnet build verification/LingGuangV05.Core.Tests.csproj --nologo -v:q -m:2`；`verification/compile-tests.log`|0错误、1字段反射赋值警告|
|真实已编译NUnit方法|`dotnet run --project verification/CoreRunner.csproj --nologo`；`verification/core-run.log`/`core-results.json`|9通过、0失败、3明确跳过（需Unity原生JsonUtility）|
|补丁校验|`python -X utf8 verification/package.py`内执行生产目录`git apply --check`；`verification/apply-check.log`|exit 0；未应用|

最初生成patch漏写新文件mode，apply-check失败于`dev/null`；已修复格式并通过，原失败日志保留为`apply-check-initial-format-failure.log`。没有以候选已写或编译通过宣称Unity/UI/Player验证。

## 主控下一步：精确入口

1. Unity EditMode filter：`LingGuangV05.Tests.GroupPresetLifecycleTests`，程序集 `LingGuangV05.Core.Tests`，**12个测试**。三项原生JsonUtility测试必须实际跑：`OldMissingListsAreReadSafelyAndHistoryImportsOnce`、`OldSaveWithoutFieldRepairsAFreshUnimportedState`、`JsonRoundtripPreservesChoicesSentIdsWeekAndPartialContinuation`。
2. 隔离EditMode入口：`GroupPresetRuntimeChecks.Run`，成功标记 `GROUP_PRESET_RUNTIME_PASS`。检查真实Send/Receive、双语彩蛋后冷场、预设取消旧通用请求、贴纸/已处理脚本/测验不计数、精确中英历史回填/拒绝AI和近似文本、跨档、StoryFocus/作者队列/脚本等待、保存重绑后仅发剩余第二行及当前语言。它手动推进真实迭代器，**尚未运行**，不是物理UI/磁盘或真实延时验证。
3. 回归入口：`LaoZhouNewsChecks.Run`、`WorldYyGiftRoutingChecks.RunInteractions`；原 `LifeGroupPlaybackChecks.Frames`（PlayMode）验证同档禁用/启用作者队列与旧finally保护。
4. 最低实机UI：同日三气泡→点击一项剩两项/不补；切语言；翌日无需发送也刷新；普通自由输入第2/5次真实8秒+3秒；预设/引导优先；冷场第一行后真实磁盘保存/加载仅续第二行；最小化/组件禁用再启用；换档与迟到旧模型回调不得落新档。全游戏最小化时是否沿用既有YY Realtime等待需验，不在本候选冒称暂停计时。
5. 检查通知/故事消费方 `GroupScriptPlaying` 回归，尤其PublicReadingInteraction和应用获取引导。冷场串行让位作者回复，可能出现“自聊第一行→作者队列→自聊第二行”，这是优先保留作者台词的行为；没有反向互等死锁。

本候选未改生产、共享Design文档、用户存档，未开Unity/Player/模型，未提交/push。
