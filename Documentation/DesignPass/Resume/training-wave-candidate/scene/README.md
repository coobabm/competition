# 1:084 真实 Main 场景检查候选

状态：Assets 外候选。Core、Assembly-CSharp 和完整 Editor 三个程序集的独立编译通过；未启动 Unity，尚无 Main 机制或视觉 PASS。主任务仍需安装并运行验收。

## 接入

1. 先由主任务集成已审查的 training-wave-candidate 根目录生产候选：Core 的 XgBoard / XgConceptTransition；Desktop 的 XgBrainWallpaper / XgWallpaperConcepts / XgWallpaperTraining。不要覆盖期间出现的无关生产修改。
2. 将本目录 `DesignWallpaperSmoke.TrainingScene.cs` 加入 `Assets/LocalBuild/Editor`。
3. 应用 `dispatcher.patch`，它只在 case 11 增加 training-scene 分支。**不要复制 `DesignWallpaperSmoke.compile-only.cs` 覆盖 dispatcher。**
4. 入口为 `DesignWallpaperSmoke.RunTrainingScene`，仍用现有 Main 独立存档 smoke 运行方式。该模式走真实 Main.unity。

依赖已完成 67/68 验收的 `DesignWallpaperSmoke.WiringScene.cs` 共享 partial helper：真实源画布坐标反算 / round-trip / raycast / LCD click、通知关闭、完整 source 和 GameView 捕捉、私有字段只读观察。真实训练按钮的命中记录会沿用 `wiring-scene-input.txt`，不是接线业务调用。

## 玩法路径与边界夹具

- 真实 controller、Host、有限 starter GPU、正常 UiFitScale 窗口。最初四张教学只调用真实教学判断流程准备样本；随后通过真实 LCD 输入命中真实 TrainButton（其 XgHold.Down 触发实际 BeginEpoch）。不调用业务 onClick，不直接引发事件。
- 先冻结模拟推进，记录真实开始事件，只让显示走自然帧：.2s 截图，验证 .6s 光波自然到期；之后短暂恢复真实 controller Update，完成刚才那轮真实训练。开始效果不能伪造完成的 epoch / cards。
- Reduced / Quiet / Time.timeScale=0：真实 BeginEpoch 和实际 R1 创建，在不修改效果年龄的情况下显示并自然过期。检查底层格子不被 Reduced 隐藏、亮度不超过真实 Quiet LightLevel。
- R2 / R4 边界明确使用近阈值 Core 夹具：两真实父概念、达阈值链接、普通临界弱概念、seed、pinned；执行实际 Board.Train。第二次同帧 Train 再生成 R1 概念，在 EOF 检查概念映射等于最终 Board ID 集，出生队列 / opacity 索引匹配真实事件 ID，死亡位置保留原 ID 的最后可见位置。
- Idle 夹具的目标在训练 R4 后仍然存在，通过真实 Idle(64) 删除；.36s 捕捉已经变灰的残影，再检查自然清理与不额外衰减。
- 容量边界在同一个渲染帧内执行 5 次真实 BeginEpoch，使用有界 Core Tick 完成每一轮，检查只留 4 个波；再由实际 Board.Train 一张有 160 个不同 raw features 的显式夹具卡，检查只留最新 128 个 life 和 birth ID。该段推进的是明确标注的 Core 夹具时间，不作为自然训练速度证据，也不修改 Unity 动画时间。
- 禁用／重新启用显示组件、替换实际 Board、禁用 runtime、通过 SetSimulationForTests 更换独立 ChapterOneSim；确认旧来源不能再驱动效果，新来源只接收新发生的真实事件。

所有渲染证据来自真实帧末 CanvasRenderer mesh；与同帧 OnPopulateMesh 逐顶点、逐颜色比较，检查有限坐标及 4096 顶点上界。测试不会强制刷新网格来掩盖漏刷，也不会写动画计时或伪造 life / wave 集合。

只读断言比较 action 完成后的完整 XgState、household GameState、额外非序列化 link 数据和真实钱包，等待动画期间保持 runtime tick / controller tick 关闭。唯一恢复 controller 的段落是明确的自然训练完成。磁盘保存始终关闭，并复用 runner 的 Save / DesktopProfile 隔离目录。

## 输出和限制

输出仍在 `LocalVerification/DesignPass/WallpaperSmoke`：checks.txt / result.txt，training-scene-cases.txt 夹具说明，training-scene-frames.csv 自然帧记录。8 组每组包括完整 source `.png` 与 GameView `-full.png`：

| 前缀 | 场景 |
| --- | --- |
| training-window-rest | 默认训练窗口静止 |
| training-wave-desktop | 真实 LCD 开始后的自然光波 |
| training-epoch-completed | 真实 controller 完成训练 |
| training-quiet-reduced-paused | Quiet、Reduced 和暂停组合 |
| training-r1-r2-r4-same-frame | 真正规则触发的生灭与同帧末态 |
| training-idle-r4-grey | Idle 实际移除后的灰点残影 |
| training-capacity-final-state | 容量上限与最终概念位置 |
| training-save-swap-clean | 换档后的自然清理 |

120 秒协程总期限，普通断言失败由最外层捕获记录 FAIL、退出码 1 并结束隔离 Play。finally 恢复模块、组件开关、Quiet / Reduced、timeScale、TestMode 和原独立 household 引用。合成物理 PointerEventData 经过真实 LCD 链路，不声称验证了操作系统鼠标设备。

编译工具 `prepare_compile.py` 仅使用 Unity 随附 dotnet / csc，重定向所有输出到 `LocalVerification/DesignPass/TrainingSceneCompile`；不启动 Unity。它依赖当前 Bee response files，每次从最新 dispatcher 生成最小补丁与编译专用副本，hashes.json 记录验证基线及所有候选哈希。

完整画面仍须主任务人工判断。若自然帧或窗口最小化较慢导致 .2s 截图赶不上 .6s 波，检查会失败并保留帧日志，不会延长波或伪造年龄来取得截图。
