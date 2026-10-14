# 接线调度台 · 游戏内改造验收（2026-10-07）

## 交付

真实工程 `/Users/kk/competition`，Unity 6000.4.5f1。入口：灵光.exe → 接线；原解锁条件保留（阶段2或首个检查点）。

- 198×120 固定可读卡片，浅色工作台；背景可平移，支持缩放、适应、整理。
- 点击或拖拽端口接线。点线只选中，明确点击固定操作栏才断开。
- 工作集默认展示3项任务及其上游；全部/订单/标注/训练/生活过滤。隐藏不停止后台任务。
- 仓库列出模型、显卡、电源与任务，保留解锁原因和接通租赁的事前费率。
- 不另造HTML模拟器。使用现有订单、代标、训练、预训练与生活任务接口；视觉脉冲表示流量，成功/失败事件仅使用真实执行器事件。

## 底层必要修正

- 新纪录继承手动GPU位置/明确拔线状态，不误从仓库旧记录继承。
- 接入但未启动训练显示待命，而非虚假运行。
- 家庭欠费停电传播到资源图和任务；不把一般训练门禁误判为所有电源停电。
- 同卡训练/推理共享算力与显存；显存等待不会丢进度或相互死锁；同一大时间片不被串行任务重复消费。
- 争用影响实际epoch进度，结算不再次扣减学习量。

## 已运行验证

- Unity EditMode `XgWiringTests`：**23/23**。
- 完整 .NET：**596项，567通过，29失败**。这29项在修改前备份链接的隔离基线中全部同名失败；最终失败集合无新增。完整项目不能因此称为全绿。
- 原生LCD指针链：节点选择、线路选择、显式断线、点击/拖线接回、节点拖动、背景平移、缩放/适应、分类、仓库、租金可见、能力不匹配拒绝、右键拖线拒绝。见 `lcd-interactions.json`；原始观察在 `lcd-interactions-raw.json`。
- 最后布局：1074×611，工作集10个节点、73项文本，负尺寸0、无界溢出0、截断0。见 `layout-report.json`。
- Play使用正常Host执行，`TestMode=false`、`useDiskSave=false`；观察到实际自动标注完成事件、进度和收入，而不是禁用Host的假测试状态。
- 调试中发现并修复了 CanvasRenderer 缺失、标题被裁切、待处理自动适应覆盖手动缩放；最后使用全新 Play 会话重验。

## 安全与限制

- 未安装依赖、未调用付费模型、未改玩家存档、未手改场景或Prefab YAML。
- 已退出测试Play，恢复 `useDiskSave=true`、`tickEnabled=true`；恢复进入任务时的 ComputerCaseMenu 编辑场景。场景没有保存修改。
- 原Opus接线源文件和本轮触及的共享源文件备份：`/Users/kk/unity/model-wiring-prototype/unity-before-20261007/`。
- 仓库/世界状态沿用实际游戏规则。没有新增自定义优先级、离线收益或任意外部任务；新增任务仍应注册到现有Core任务系统。
- 这是有界交互与回归验收，不是完整Unity流程/所有存档/所有分辨率/长期性能认证。

## 主要文件

- `Assets/LingGuangV05/XingGuang/Desktop/XgWiringPage.cs`：工作台。
- `Assets/LingGuangV05/XingGuang/Desktop/XgWiringGraphic.cs`：渲染与原生事件。
- `Assets/LingGuangV05/XingGuang/Desktop/XgTreePage.cs`：仅新增 `XgPan.SetView`。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Wiring.cs`、`XgSim.EpochAction.cs`、`XgSim.cs`、`Desktop/XingGuangHost.cs`：资源与训练修正。
- `Assets/LingGuangV05/Editor/XgWiringWorkbenchQa.cs`：拒绝磁盘写入模式的Play探针。

截图：`before.png` / `after-final.png`。统计：`regression-summary.json`。
