# C2 返回审查：先合诊断，修复仍有条件

> 后续实跑更正：104714-080在AddComponent处被Unity明确拒绝“because it is an editor script”，本诊断补丁原始Editor探针位置不能运行。主控正最小迁移probe/meta至Assets/LocalBuild/Diagnostics并补空值断言。详见EDITOR-PROBE-CHECK.md顶部更正，保留本报告作为交付时快照；隔离编译通过不代表Unity挂载可用。此失败没有H1读值，条件fix仍未采用。

## 结论与交付

建议先合 `patch/c2-diagnostics-only.patch`，只增加 QA 诊断；暂不合 `ClaudeReturn/C2/fix-after-readings.diff`。本轮没有改生产、运行 Unity/Player 或执行 UI 测试。H1 有实际 InputSystem 1.19 源码支持，但还没有发生故障时的玩家阶段读值，仍是假设。不能采用返回报告“产品门禁无缺陷”“H5排除”的确定口吻。

补丁 SHA256：`4a808857855c9e0cecd212e4f248991cb5e711537eb4d0c3f01abd71d870a77c`。

5 路径：既有 `Assets/LocalBuild/Editor/PublicReadingInteractionUiChecks.cs`；新增 early/late 与 pre-notifier 两个探针和各自 meta。完整文件在 `candidate/`，生产原文件快照在 `baseline/`，逐文件 hash 在 `manifest.json`。无产品文件、共享宿主改动或条件修复混入。

## 基线及验证范围

- 当前生产 runner 与 1750/source 快照逐字节相同，SHA256 均为 `e586828c4d7ad0c6441064116420327b01b3d4ca8b56d99a4772664164a7f451`；三个原 held 断言布尔表达式保留，现有 HeldMouseFixture 的输入写法不变。生成并检查后生产 hash 仍相同。
- 普通 `git apply --check` 对本诊断补丁成功；对返回的条件修复 diff 也成功，但后者没有应用。返回文件已明说手写 hunk；本机检查成功不代表运行正确。
- 两个探针 GUID 在生产 Assets 的 meta 中没有现存匹配；没有覆盖旧探针。
- 候选三份 C# 源码的隔离 Roslyn 编译成功、0 error，5条既有旧API warning；使用已存在 Unity/Bee 引用及此前 Guide 审查程序集。日志见 `verification/compile.log`、`result.json`。这仅降低语法/API风险，不代表当前全工程 Unity 编译、脚本导入、Editor程序集探针可实际 AddComponent 或运行顺序已通过。
- 未扫描 Library；仅依交接明确给出的 `Library/PackageCache/com.unity.inputsystem@21a28c3a6c83` 路径读取指定 ButtonControl/InputManager/InputState 源码，并复用既有编译 rsp 引用。没有修改包或安装工具。

## 证据和不能推出的结论

已读原始 `LocalVerification/SidebarLayout/20261019-093453-647/report.json` 并审看失败图：可见“公开讨论”通知。鼠标/current 和 >=3 玩家帧的前置断言通过，随后 count/safe/quiet 合并断言失败；原报告仍没有当时三个数值或逐帧 InputBusy。

`XgPublicDiscourseNotifier.Update` 顺序是计算综合 held → Advance → held/焦点门禁 → 到时投递。`Advance` 在 importantEvent=true 时重置 quiet=30、不增加 safe。这个局部顺序未发现明显绕过 held 的分支；没有据此证明所有运行时输入、生命周期或投递行为都没有产品缺陷。

若同一 delivery 对象从夹具安装到首次投递始终未替换、且不足30秒，那么期间某次 held 重置应阻止那次首次投递。这是带前提的推论，不能把单张截图当作每帧 held=false 的采样记录。投递后 tray.IsBusy 自身还会令综合 held=true。H4 也只能缩小到“曾有某条通知路径运行”，不能证明每个预期 Update 都运行并使用预期 owner。

## H1：源码支持与待证部分

当前包版本确为1.19.0。`ButtonControl.cs:194–208` 的 isPressed 在 needsToCheckFramePress 后读取独立 player/editor 缓存；首次 wasPressedThisFrame 会在 :278–288 切换到该模式。`AeroLcdSourceBridge.cs:66` 在其条件满足时读取该属性。

`InputManager.cs:3977,4005` 按当前选中的 buffer 比较；:4013–4022 按显式 updateType 写目标 buffer；:4025 起只在比较有变化时更新 button 缓存。:3075 起在玩家循环恢复 Editor 后的状态/缓冲。因此在 Editor 回调里交叉写 Dynamic+Editor 的现有夹具，确有值得验证的缓存/缓冲错配机制。

仍待证：发生故障时 Refresh 属于哪个上下文、Bridge 是否可交互、具体鼠标是否仍 current、缓存模式何时开启、player raw/cache/InputBusy 是否在同一玩家阶段持续不一致。`needsFramePress=true` 不能间接证明 Bridge.CanInteract=true；其他输入消费者也能开启缓存模式。返回材料对默认 ReadValueCaching 的描述不作为当前运行时设置已测证据。

必须按 phase/frame/device/delivery identity 联合判读。全局 playerCacheMismatch 计数包括准备前、维护前等样本，不能只见计数非零就认定 H1。`pre-notifier` 不是通知器内部采样；默认顺序0的其他组件仍可能在它和通知器之间执行。

## 对返回诊断的取舍和本轮小修

保留：在 Set(true) 前建立 trace；Refresh/onAfterUpdate 前后及 early/pre/late 观察；不读 wasPressedThisFrame/wasReleasedThisFrame；有界头/尾/首次事件记录；using/finally清理、30秒探针自毁；失败消息携带读值。没有直接写焦点、时间、delivery、产品门禁，原输入维护不变。

本轮补充：直接记录已存在 bridge 的 enabled/CanInteract 和 PresentationHeld；记录 MonoImporter 导入顺序与声明 attribute（两者分别记录，不能仅以顺序标签认定先后）；反射成员缺失显式标识；构造中途异常销毁已建诊断对象；将 quiet->30 的推断标为 held-reason-unspecified，避免误认为必由鼠标造成。弹窗扫描缩到夹具安装和最终Editor观察，减少逐帧 UI 数组分配。

副作用仍在：isPressed/ReadValue 是观测 API，但输入库可能维护自己的读取缓存；反射、字符串/集合和少量UI查询增加时间与分配，不是零扰动。onAfterUpdate 采样异常被记录而不是改变门禁；可用性依赖私有字段名称。一次加日志后通过只说明未复现，不能当修复。探针超时销毁只保护探针对象，不能替代外层 heldMouse、设备和输入模块 finally 清理。

## 最小执行顺序与条件修复

1. 主控确认当前 runner hash/补丁适用性后合诊断，在自己串行 Unity 会话导入/编译。若探针 AddComponent 或生命周期报错，先修诊断；不要通过移走或关闭产品组件让断言通过。迁出 Editor 目录需要明确程序集依赖方向，不能直接挪一个引用 Editor trace 的组件。
2. 先执行一次现有入口 `PublicReadingInteractionUiChecks.Run`，保留 report、失败截图及完整 `Public reading held-pointer diagnostic (C2, read-only)` 日志。无需机械执行“至少三次”；先看该次读值能否区分原因。缺 phase/成员/顺序异常时先完善具体缺口；读值充分时进入对应修复。
3. H1成立所需组合：同一 delivery/鼠标、实际player阶段 raw pressed但player cache/isPressed false、InputBusy false、期间真实前台且绑定正确；并有对应Refresh上下文与safe增长/投递关联。若 pre-notifier正确但随后仍增长，不能继续排除产品路径/排序，应调查这段间隔。
4. H1得到支持后，再移植 `fix-after-readings.diff` 的最小夹具变更：Editor refresh只写Editor，玩家buffer在玩家onAfterUpdate写，release用pending保证送达。必须保留诊断，并检查释放是否真的抵达玩家状态、第二次按住、当前设备恢复与Dispose。它改变了Set后即时玩家状态：真实player press edge可能产生一次点击音；这是需验证的可观察行为，不是理由修改产品InputBusy。
5. 特别注意条件修复的“按下/松开各一次”描述不精确：实现仍在 held=true 的每个player update写状态，pending保证松开也写一次；Dispose仍交叉写两种buffer，紧接移除QA设备。应验证正常释放和失败清理，不能笼统断言缓存永远一致。现有手动LCD OnPointerUp发生在Set(false)同一Editor片段，可能早于玩家buffer释放；验收应确认实际路由与后续输入不被卡住。
6. 依据证据安排有针对性的重跑；最终保持全部原门禁：按住count0/safe不变/quiet30、>=3玩家帧、真实前台、native tray忙碌、自然30安全秒、按住目标不消失、松开准确跳转、生活事件优先。诊断状态稳定后还需无诊断路径的验收；次数服从新增证据，不照抄外部“三次即完成”的流程。

当前公共阅读尾项仍未通过。本次没有宣称真实模型、音频、最终Player、发行或其他历史任务完成。

## 无需Unity、可并行的Claude下一阶段

见 `NEXT-C2-FOR-CLAUDE.md`。建议在主控串行收集一份trace期间，Claude只做纯文本条件修复审查/状态时序表；取得trace后再按证据交独立小候选。不要与当前诊断runner同时各自覆盖同一文件。
