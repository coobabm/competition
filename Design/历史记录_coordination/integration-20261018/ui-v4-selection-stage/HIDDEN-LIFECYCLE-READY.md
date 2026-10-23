# 隐藏页面首次刷新生命周期修复（候选）

`hidden-game-lifecycle.patch` 仅在 RefreshDeskGame 选择了游戏、准备 Open 前加 `!deskGame.Visible` 分支：停止残留会话并返回到普通刷新，不写 classicDesks。下一次页面真正可见时再 Open。此处刻意不用 Focused：后台可见页面仍应同步内容，真实输入门禁继续在 Focused/Settle/Tick。

保留现有可见时启动失败的经典卡回退，以及玩家主动选择经典模式的会话偏好。没有清空用户 classicDesks，没有对众包恢复教学，也不改玩法解锁规则。

`hidden-game-lifecycle-check.patch` 基于根已应用“合法经典卡表面检查”的最新检查器，补一个有正反例的生命周期测试：
1. 对单个 sense 桌设定默认模式夹具，隐藏真实 Teach root 并 Refresh：不得启动 game，亦不得写经典偏好。旧生产逻辑会在后一断言失败。
2. 恢复真实 root 再 Refresh：立即启动 sense 默认小游戏。
3. 通过真实小游戏经典按钮主动切卡，经历隐藏/恢复后必须保留经典偏好和正确题卡。
4. 点真实“改回小游戏”按钮，仍能恢复默认游戏。

检查器静态编译退出 0；两 patch 的 apply --check --ignore-space-change 通过。未修改主项目，未启动 Unity。请根串行应用后运行 SelectionIsolationInteractionChecks.Run。当前源完整候选 `XgLabelPage.DeskGame.lifecycle.cs` 只作参考；优先用 patch，避免覆盖其他修改。
