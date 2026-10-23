# 阅读通知输入夹具最小修复候选

只修改 `Assets/LocalBuild/Editor/PublicReadingInteractionUiChecks.cs`。没有修改生产文件，没有启动 Unity、模型或构建。候选不是运行通过声明。

## 失败证据与原因

生产报告 `LocalVerification/SidebarLayout/20261019-053542-858/report.json` 已完成 26 张双语群图截图与对应检查，随后在原第 275 行 `explicit synthetic held-mouse state still active` 失败。报告没有运行时错误或编辑器基础设施错误。

该断言是 `Mouse.current==mouse && mouse.leftButton.isPressed`，现有报告没有区分是哪一项失败，不能仅凭报告断言物理鼠标确实抢占了设备。

当前项目 InputSystem 1.19.0 源码与测试结构证实两个夹具缺口：

- `SidebarLayoutChecks` 在 `EditorApplication.update` 推进枚举器；`InputState.currentUpdateType` 的官方源码说明编辑器读取独立 Editor 缓冲。原测试不指定更新类型的 `InputState.Change` 使用 `InputManager.defaultUpdateType`，前台运行时默认通常是 player 更新类型；不能保证写入与随后读取同一缓冲。
- `InputManager` 的事件处理明确在收到有变化的原生输入事件后调用 `device.MakeCurrent()`；`InputState.Change` 的 `UpdateState` 本身没有调用 `MakeCurrent()`。原夹具只在创建鼠标时选中一次，后续编辑器 tick 的状态写入不保证生产 `Update` 看到它。

源码位置：`Library/PackageCache/com.unity.inputsystem@21a28c3a6c83/InputSystem/State/InputState.cs` 第 19–27、111–127 行；`InputSystem/InputManager.cs` 第 150–166、3554–3558、3879–3888、4013–4024 行。当前包在播放时不调用 Editor 类型的 `onAfterUpdate`，因此单独增加该回调也不能解决编辑器读值。

## 修复范围

新增一个仅供本测试使用的 `HeldMouseFixture`：

1. 按下、释放及编辑器等待时明确同步测试鼠标的 player/Editor 两套缓冲。
2. 在 `InputSystem.onAfterUpdate` 的 Dynamic/Fixed 更新后维持显式合成按住状态并选中该测试鼠标，让生产 notifier 的真实 `Update` 读取真实 InputSystem 状态。
3. 记录不同 `Time.frameCount`，在两段按住测试都增加至少 3 个实际 player 帧的要求。原来的编辑器循环计数和全部原有 `Need` 断言原文保留。
4. `finally` 先解除回调并释放合成鼠标，再移除测试设备和恢复原鼠标、UI modules。

真实 `Application.isFocused`、窗口最小化、TMP focus、notifier `InputBusy`、通知安全时间、tray 交互、LCD raycast ownership、跨帧 pointer-down/up 与精确浏览器目标检查均保留。未调用手动 `InputSystem.Update`，未直接执行 notifier，不禁用物理设备，不伪造应用焦点。图像渲染检查完全未变。

这是明确标注的合成输入夹具，仍不是物理 OS 鼠标自动化。Scope 已注明缓冲同步和 after-update 维护方式。

## 验证和接入

`prepare_and_verify.py` 已完成 16 项静态检查（包括保留全部原始 `Need` 断言，以及只读 `git apply --check` / `--stat`）。C# 编译和 Unity 实机验证未执行，由主代理继续。

补丁：`reading-input-fix.patch`

SHA256：`b37e1546d1feb572832a7ddda4508e3b75a7b143eb4b5324c78dfdb5ae36d364`

主代理可将单文件补丁应用于生产，然后重跑原完整 `PublicReadingInteractionUiChecks`。验收仍需原 26 图检查、输入/托盘/自然 quiet30、真实 LCD pointer-up 打开精确讨论页以及 life priority 全部通过。若新的实际 player 帧检查失败，应保留证据并检查前台/暂停和帧推进，不减小帧数或删除断言。
