# motion-015：实际训练按钮两层反馈核对

日期：2026-10-13（Asia/Singapore）

## 结论

当前源码中，训练页实际 `TrainButton` 的有效手动训练已具备「手感＋结果」两层反馈，无需新增效果或补丁。结论限于静态调用链核对；未运行 Unity、游戏或检查入口，未进行视觉验收，也不表示《动效策划》第 10 行全局完成。

本任务只读生产源码与既有任务记录，没有阅读新策划正文或后续条目；只写本目录。未改 Assets、ProjectSettings、全局清单或玩家存档，未提交、推送、打包、创建聊天或发送其他聊天消息。

## 已有记录与范围

- 已核对 `Documentation/DesignPass/Agents/motion-task-001.md`：通用按钮只提供手感，明确未覆盖 `XgHold` 独立业务。本次补的是实际训练入口到业务结果的审查证据，不重复实现通用按压。
- 已核对 `Assets/LocalBuild/Editor/MotionButtonChecks.cs`：它验证通用组件，不建立训练页，不证明训练事件链和真实画面；本次未运行它，亦不沿用其日志中的 PASS 作为本任务结果。
- 实际目标为训练页 `TrainButton`。`XgRealTrainPanel` 是自动排队的真实小网络演示，只有跳过／关闭按钮，不能用演示的误差曲线替代主训练按钮证据。顺带确认其跳过按钮由 `ui.Button` 创建，`Skip` 启动淡出，属于通用手感＋关闭结果，未改动。

## 原文件证据

以下路径均相对工程根目录；行号按本次核对版本记录。

| 环节 | 源码位置 | 证据与反馈层 |
| --- | --- | --- |
| 实际输入接线 | `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs:118–134`、`:232–237`、`:712–716` | `train = ui.Button(..., null, 28)`；左键 `XgHold.Down` 与独立 Submit 均调用 `Press`；`Press` 调用 `Sim.BeginEpoch(..., true)`。不能只查看空的 onClick 判断没有业务。 |
| 通用手感 | `Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs:188–211`；`XgButtonPressFeedback.cs:17–47`（同目录） | 安装反馈 Button；左键按下／Submit 压缩与微移 Label。Label 私有效果不覆盖按钮本体业务 Knock。 |
| 业务事件真实接通 | `Assets/LingGuangV05/XingGuang/Core/XgSim.EpochAction.cs:13–26`；`Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.cs:188–200`、`:267`、`:286` | BeginEpoch 设置进行中状态并发出 EpochStarted；视图订阅开始、完成事件并转发训练页。 |
| 每次有效手动开始 | `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs:258–275` | `Fx.Knock(train.rt, .1f, new Vector2(0,-4))` ＋风扇音效提供独立手感；保留 12 个火花。Refresh 更新禁用状态；不依赖通用 Label 动画在立即禁用后继续可见。 |
| 进行中结果 | 同文件 `:303–318`、`:485–503`；`XgSim.EpochAction.cs:29–49` | epochFill 每帧显示 epochProgress；进行中禁用训练，等待算力时 hint 显示等待原因。可见状态反馈属于结果层，不把多个装饰效果重复计算为多层。 |
| 普通完成／没有增益 | `XgTrainPage.cs:277–299`、`:463–478`；`Assets/LingGuangV05/XingGuang/Core/XgSim.cs:677` | 完成事件显示「第 N 轮 · +分」或「涨不动了」；曲线末段 Pulse，轮次、准确率和曲线刷新。即使未出新纪录卡，也有结果层。 |
| 退步／回滚 | `XgTrainPage.cs:283–295` | 分别显示退步分数及原因、检查点撤销退步；保留原抖动、音效、冲击波，结果可辨识。 |
| 条件变化导致拒绝 | `XgSim.EpochAction.cs:19–20`；`XingGuangView.cs:257`；`XgTrainPage.cs:232–237`、`:486–503` | blocker 经 Say→Message→Toast；Press 失败时按钮横向 Knock＋Thud 并刷新原因。未解锁路径虽先返回 false，但 Refresh 的「先标够样本」仍给出状态；常态预先禁用并显示原因。 |
| 新纪录强化 | `XgTrainPage.cs:332–365`、`:397–435` | 手动新评级保留成绩滚动、盖章、奖金、震屏、粒子等原有动效；满足至少两层并不要求每轮触发这些奖励效果。 |

无需将火花认定为「世界层」才能成立：按钮回弹是手感层，训练进度及明确的结果呈现是结果层。未增加世界反馈，也未削弱原帅气动效。

## 边界与待统一验证

以下是主聊天实际运行／视觉验收清单，全部未运行、未验收：

1. 在隔离测试状态中分别对视觉、序列轨道执行左键和已配置 Submit。一次输入只开始一轮；开始时按钮本体反馈可见，进度推进，完成出现结果。不要只观察通用 Label，因为开始后同步 Refresh 会禁用它并清理该通用动画。
2. 覆盖普通增益且无新纪录、增益不足 0.5 分、退步、检查点回滚。每类都必须有可读结果，不能以新纪录盖章代替普通完成验证。
3. 两轨道争用资源、训练中失去资源、开始前条件变化：分别确认进度／等待原因、拒绝提示和反馈；训练进行中快速点击不得开始第二轮或额外结算。
4. 静音、减少动效、快速连点、训练时切页／隐藏后返回：确认仍能靠可见状态判断结果，按钮不留残余缩放。当前事件处理在隐藏／非训练页／非当前轨道时主动不播效果；不据此判定缺层。
5. 输入隔离需单独核实：`XgHold.OnPointerDown` 只检查左键，`XgTrainingSubmit.OnSubmit` 直接调用回调，没有复用 Button 的 IsInteractable 检查。这是已有输入边界，不能把 MotionButtonChecks 的通用禁用测试当作实际训练输入测试。核心仍检查正在训练／未解锁／blocker。父 CanvasGroup 禁用、评估卡覆盖时的业务输入隔离交统一输入验证，本任务不以该观察扩展修改共用输入组件。
6. 如验收真实训练演示的跳过按钮，观察按下和 0.35 秒淡出；淡出期间重复按下不会重新开始关闭计时。该演示检查不替代上列主按钮验证。

## 待整合

仅整合此份静态核对结论和验收清单，无 changes.patch、C# 片段或新增测试入口。无需改生产源码；不重复添加已有的 Knock、结果飘字或强庆祝效果。统一编译、实际训练场景和截图由主聊天负责。

依赖 SHA256 与结束前复核结果见同目录 `source-hashes.json`、`dependency-recheck.md`。后续源文件变化时，应重新核对上述接线与反馈分支再采纳结论。
