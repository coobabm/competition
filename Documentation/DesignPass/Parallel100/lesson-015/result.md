# lesson-015 · LessonTaught 订阅生命周期

结论：交付可复用辅助类与精确接入合同，尚未整合生产。当前生产代码没有壁纸 LessonTaught 订阅，因此不能声称已发现既有订阅泄漏，也不制造删除既有订阅的补丁。缺少的同步重绑通知及视觉接收器已明确列为接口依赖。只覆盖获授权的第 13 行订阅生命周期，不代表该条或整份策划完成。

## 原文件证据

- `Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs:40,54`：实例事件，成功学习后同步发布原卡和结果。
- `Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs:87–108,121–128`：BindSave 每次创建新 XgSim；OnRuntimeChanged 发现 ChapterOneSim 替换后重绑。当前没有对外 SimRebound 通知。`:214–218` 的销毁处理只解 runtime 和语言事件。
- `Assets/LingGuangV05/Runtime/ChapterOneRuntime.cs:105–114,217–227,254–258` 与 `ChapterOneRuntime.Slots.cs:55–67`：换档和重新开始进入 Attach，替换 ChapterOneSim 并同步 Changed。仅静态阅读这些入口，未执行任何存档操作。
- `Assets/LingGuangV05/XingGuang/Desktop/XgBrainWallpaper.cs:65–80,116–128`：Install 先 AddComponent 后赋依赖；Observe/Update 观察存档但未订阅 LessonTaught，亦无相关 OnDisable/OnDestroy 解绑。
- `Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs:58–64`：已有成功事件参数与学习后触发测试，未覆盖组件生命周期。
- 已核对 `Documentation/DesignPass/Agents/issues-task-001.md` 和当前已到达交付中的 LessonTaught 记录；未读取新策划正文。现有交接明确将壁纸接入与换档解绑留给主聊天。

## 交付

- `XgLessonSubscription.cs`：按 XgSim 引用幂等绑定、先解绑旧实例、null 暂停、永久 Dispose；每次绑定分配独立委托，阻止已捕获旧调用列表在 A→B→A、禁用恢复或销毁后继续回调；不移除其他订阅方。
- `integration-contract.md`：控制器 SimRebound 接口与具体插入上下文；壁纸 OnEnable/OnDisable/OnDestroy 接入片段、Install 初始化顺序、即时新实例绑定和清理旧视觉状态合同。
- `SubscriptionChecks.cs`、`check.ps1`：独立内存事件夹具。夹具定义同名 XgSim/XgCard/XgAnswer，**不可加入 Assets**。
- `source-hashes.json`：依赖文件完整路径（相对工程）及 SHA256，供结束复核和整合前比对。没有 changes.patch；所交付的是独立辅助代码和接口合同，不覆盖不断变化的生产文件。

## 验证状态

- 静态检查：已核对事件声明、换档/重开调用链与辅助类接口；未改 Assets、ProjectSettings 或其他任务目录。
- 实际运行：在独立 PowerShell 进程中 Add-Type 编译并运行辅助类与模拟事件，17 项断言通过，输出 `Standalone event ownership checks passed: 17 assertions (fixtures, not Unity).`。覆盖实际 multicast 调用过程中切换、旧委托调用、重复绑定、其他订阅保留、payload 透传、禁用恢复、Dispose 重复和禁止销毁后重绑。
- 未运行：生产 XgSim、Unity 编译、Unity 生命周期与 DesignFirstLessonChecks；没有这些项目的 PASS。
- 未做视觉验收：壁纸闪光效果、延迟和截图由主聊天统一负责。

## 待整合与依赖

整合方先比对 source-hashes，再采纳辅助类和同步通知接口；将真实视觉回调接入合同，并处理换实例/禁用时已开始的闪光清理。仅加入辅助类不会自动接通壁纸。完整接入前不应标为完成。后续生产编译与场景边界详见 integration-contract.md。

结束复核结果另见 `dependency-recheck.txt`。本任务未向其他聊天发消息、未创建聊天或 agent、未启动 Unity/游戏、未提交/推送/打包、未读写真实玩家存档。

