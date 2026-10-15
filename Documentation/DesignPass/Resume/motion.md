# 动效恢复复核

日期：2026-10-13（Asia/Singapore）。范围仅《动效策划.md》第 10 行全局三层反馈原则；本轮仅阅读原整合报告、当前实现及相关交付，不读后续策划。不标记全局原则完成。

## 已确认

- 当前 `XgButtonPressFeedback` 保留 Button 业务路由，私有 Label Knock 与宿主业务 Knock 分开；结束依赖 `HasActiveKnock`，无第二份动画计时。
- 当前 `XgHold` 位于 `XgUi.cs`，已有 Selectable 门控、持有指针归属、重复按下过滤；`XgTrainingSubmit` 已有 active/interactable 门控。此次不改 XgUi 或训练生产代码。
- `motion-018/result.md` 唯一确认的冗余分配（活动帧逐次创建完成列表）已经在当前 `XgJuice` 以惰性 `completedPunches` 缓存落实，处理后 Clear。没有证据需再次修补；该交付保留的分配/帧耗测量仍不是已完成项。
- 已查当前安装版 uGUI：`BaseInputModule.OnEnable` 重新获取本对象 EventSystem，`EventSystem.UpdateModules` 重新收集并过滤模块。上次 fixture 的显式生命周期初始化在 PlayMode 不会累积模块；随后 `enabled=false` 阻止设备轮询。同步过程无帧让出，Dispose 用 DestroyImmediate 结束各自临时夹具。
- 主聊天反馈 `LocalVerification/DesignPass/31-motion-resume.log`：实际空场景 PlayMode 中 MotionIntegrationChecks 全部同步检查通过，包含基础、003、008、布局、Reduced、训练输入及完成缓存检查。它仍是 PlayMode 内的同步方法验证，不等同真实输入或自然动画帧验收。此次无需修复已通过的 fixture。

## 新增真实帧入口

主聊天已同意接口：`Assets/LocalBuild/Editor/DesignWallpaperSmoke.Motion.cs` partial 的 `StartMotionFrameChecks()`。主文件由主聊天在 `MotionIntegrationChecks.Run()` 成功后调用，取代立即写 PASS/Finish。新方法用临时生产 `XgJuice` 作为协程宿主，将现有调度的 due 设为正无穷防重入；捕获逐步异常，写 result.txt，恢复原 timeScale，清理自己创建的对象后结束。无控制器，无存档 API。

新增检查只通过 Unity 自然 Update/LateUpdate 和普通 deferred Destroy 推进，不改 Punch.t，不反射调用生命周期，也不重写已有同步输入用例：

- 实际 Desktop CJK 字体、生产 XgUi.Button、深/浅色各普通/Reduced 四行。
- timeScale=0 下观察真实变形、自然结束、精确复位和私有栈停用，同时保留更长的宿主 Knock。
- 12 轮跨帧重触发/结束，确认单私有组件持续复用。
- 运行中开启 Reduced 后单调收回；宿主隐藏轮询、隐藏时输入拒绝、按钮关闭/重开；外部单轴位置写入后自然完成。
- 共享栈部分完成、帧末销毁目标、保留尚活动目标、同帧完成、缓存实例复用和引用清空。
- 每个自然等待按实际累计的 capped unscaled delta 检查寿命预算（最多允许一个封顶 .05 秒帧的完成余量），同时保留 5 秒独立墙钟硬超时。不会用固定帧数假定已过去足够的动画时间。

输出位于现有 `LocalVerification/DesignPass/WallpaperSmoke`：`motion-rest.png`、`motion-first-active.png`、`motion-settled.png`、`motion-final.png`、`motion-frame-samples.csv`、`motion-wait-samples.csv`，以及原 checks/result 文件。截图需人工查看字形、边界、对比、回位；单帧图片不能单独证明动画轨迹。

## 当前不足与状态

新增代码已用当前 Unity 的 Roslyn 与当前 `Assembly-CSharp-Editor.rsp` 全部 Editor 源列表完成独立编译，退出码 0；日志 `LocalVerification/DesignPass/ResumeMotionCompile/compile.log`，输出仅写独立验证目录，没有替换 Library 程序集。这不是 Unity 导入/运行或视觉验收。创建途中主聊天捕到的不存在调色板成员 Bg 已改为现有 Page 后再编译通过。

仍待主聊天 Unity 运行和截图复核；截至本记录写入，不给新入口 PASS。输入通过 ExecuteEvents 派发，未接实体鼠标/键盘/手柄；四行是生产按钮的隔离展示，不代表完整训练/购买页或其业务结果验收。没有进行 profiler A/B、GC.Alloc/本机内存/FPS 测量，缓存行为检查不能冒充性能提升。未触及引导、科技树、壁纸、主 DesignWallpaperSmoke.cs；未启动 Unity，未提交、推送、打包或创建聊天/子 agent。

## 34-motion-frames 失败与修正

主聊天运行 `34-motion-frames.log` 后失败于 `Shared late target finishes within its limit`。checks 中早目标完成、正常延迟销毁、只留下仍活动晚目标都通过；晚目标的固定帧数断言恰成功记录 120 次，第 121 次失败。原 CSV 在截图耗时帧之后，正常帧间隔均值约 3.02ms，最小 1.57ms。120 帧并不保证走完 .45 动画秒；这是原探针把帧数当时间限制造成的错误。34 次没有共享 Punch 年龄采样，因此不借此宣称晚目标已正确完成，也没有改生产代码来迎合测试。

修正仅位于本组 partial：原本固定 120 帧限制替换为 `MotionFrameBudget`，累计自然帧的 `min(Time.unscaledDeltaTime, .05)`，要求寿命加最多一封顶帧内完成；原 5 秒墙钟硬超时、必须自然结束、精确恢复、未完成目标隔离、缓存清空/复用等断言全部保留。共享晚目标的预算从首次 Knock 起计，包含早目标和销毁阶段。新增 `motion-wait-samples.csv` 记录等待帧数、墙钟、累计动效时间，以及只读 Punch.t/life，便于区分更新停滞、低/高帧率和真实寿命错误。不写生产时钟，不提高 FrameRate 或人为降低渲染速度。

34 次的原 samples/checks/result 已复制保留于 `LocalVerification/DesignPass/ResumeMotionCompile/34-motion-*`。修正后使用最新 Unity Editor 响应文件独立编译退出码 0，日志 `compile-34-fix-current.log`。第一次重编译使用了上次缓存响应文件，缺少其他组新 partial 而失败；随后更新源列表后通过。尚未重新启动 Unity，修正后的运行/视觉仍待主聊天，不给整套 PASS。
