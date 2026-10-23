# 性能、音频与普通 Player 验收增量审查

2026-10-19，独立只读审查。没有启动 Unity、Player、模型或录音，没有编译或改生产；未发现足以制作候选修复的新产品 bug。当前源 HEAD 为 `3533141a406940d571e064f3797328c7d214235a`，工作树含未提交整合。这里不替代主控当前 Unity 验收。

## 历史数据实际说明什么

最新已有完整捕获：`competition-latest-20261016/LocalVerification/IntegrationProfiler/20261018-160520-537-47187a`，对应原始数据导出 `raw-analysis-20261018-160826-187`，两者 complete.txt 存在。其记录的源为 `c67a7db50ace0b9df6356b7c3b79b75c404e95e0` 加 dirtyFiles，不能归属于当前最终整合或即将生成的 Player。

|阶段|渲染帧样本数|完成动作|帧间隔均值 / p95 / 最大 ms|RAW 主线程最大 ms|
|---|---:|---:|---|---:|
|teach-continuous|1741|81 / 81|17.223 / 22.955 / 44.820|44.590|
|train-auto|1758|16 / 29 请求|17.052 / 21.527 / 59.385|58.979|
|yy-tieba-switch|1610|20 / 20|18.626 / 29.284 / 80.441|100.528|

每阶段预热3秒、测量30秒。教题调用真实按钮回调但没有物理点击；训练直接定时请求 BeginEpoch，不是自然购买后的自动训练；禁用 LocalLlm。均值不证明稳定60fps，两个时间口径也不能混用。

- YY/贴吧 RAW 帧1288总100.528ms，其中 `EditorApplication.update: IntegrationProfilerCapture.Tick` inclusive65.663ms / self13.941ms；帧982总94.809ms，Tick inclusive60.950ms / self12.518ms。验收回调会主动切窗，包含产品调用和自身工作，不能整段扣除，也不能全算为产品日常 Update。inclusive父子不能相加。
- 训练帧126的 `Xg.Controller.SimTick` 38.589ms，内部有Epoch/phenomena工作及JIT，不能从旧报告泛化到当前源。
- 训练帧1204：`File.Read` 16.895ms、`AsyncReadManager.SyncRequest` 17.024ms；路径为 `XingGuangView.Update > Loading.ReadObject > Loading.LoadFileHeaders > AsyncReadManager.SyncRequest > File.Read`。这是实际同步资源读证据，不是已识别的音频加载调用。旧121139捕获“无>=2ms同步读”的结论不能覆盖此新捕获。
- 训练的 `Flush DSP connections` 与YY/贴吧的 `Audio.Thread` recorder均值为负，raw异常表已判无效。保留原数据，禁止转零或用其证明音频耗时正常。
- 旧脑窗结果 `BrainWindowPerformance/20261018-070251-936/result.json`：status passed，41项，1项单独列出的 Editor SearchDatabase 基础设施错误；夹具绕过涌现资格、直接调用知识通知，没有物理输入。不要报“零错误”或本轮重新通过。

附带 `historical-profiler-summary.json` 从上述既有JSON/TSV提取并记录输入SHA256；脚本不运行Unity、不修改原捕获。

## 当前代码与工具边界

1. `IntegrationProfilerCapture.Run` 是独立Editor诊断：隔离存档、可见性/字形验证、固定夹具、禁用模型。complete.txt 表示采集完成，没有帧预算或音频听感PASS阈值。超时实际240秒，错误文字仍写180秒；这是诊断文案小问题，不阻挡采证。采用新进程单次运行，不依赖静态状态重复复用。捕获不要传 `-quit`；入口自行退出。
2. `ProfilerRawAudit.Run -profilerArchiveRoot <精确捕获目录>` 是串行离线导出，另开已关闭Play/Profiler的Unity执行，带 `-quit`。必须显式路径，避免工具的“最近完成”默认选到旧批次。分析会清Editor Profiler缓冲，因此不能与捕获同跑。
3. YY `Update` 在 `!built || !IsOpen` 时停止主要UI刷新；此前的 `PollEndingPrivatePreview` 仅在预览存在时处理失效/关闭。贴吧在实际窗口不可见时返回，显示时0.5秒检查。`DesktopNotifications.IsWindowVisible`检查active/isOn/CanvasGroup alpha，覆盖最小化；它不判断被其他窗口遮挡。不要宣称所有遮挡窗口零更新。
4. 脑窗隐藏时停止ResizeDrawing与反馈；0.25秒Observe仍负责绑定、购买状态等。旧QA只证明一次隐藏区间 MeshBuilds不变。主控制器模拟、生活时钟/训练等隐藏继续是明确产品选择，不能为提升分数统一停掉。
5. GameMusic把AudioSource移到常驻对象，保持关窗播放；`externalPlaylistAdvance=true`交给一个常驻更新者推进曲目。显式Pause/Stop清PlaybackRequested，自然结束按其下降沿推进；本次仅源码复核。`isPlaying`、采样率、音频计数都不能替代录音实听。最终Player需覆盖关窗/重开和曲目边界。
6. LocalLlm.Pump 在 `active.Count>0 || NativeCallsInFlight>0` 返回，每次派发后立即返回。native计数在实际Task结束continuation中减少，取消UI释放后仍阻止另一原生调用。三context slots不是三请求并发。真实并发峰值/来源/取消顺序只能由最终二进制trace证明；普通单次成功不证明取消路径。当前无embedding GGUF，不能把聊天单请求推广为所有可能服务。
7. `Summarize-NativeTrace.ps1`是只读汇总，不是自动验收判定器：structurallyValid可以在空事件或未完成任务时仍为true。必须另外要求真实同ID YY链、native/native_filtered来源、零unfinished、无metadataIssues、对照界面UTC/文本。取消若未发生，标未测试。

## 本轮交付

- `SERIAL-ENTRYPOINTS.md`：给主控的最短入口与最终Player采证顺序。
- `collect_profiler_evidence.py`：只读既有导出的复用汇总器，必须显式指定capture/raw目录，新输出放源捕获之外。
- `historical-profiler-summary.json`：本轮执行该汇总器所得，历史证据，不冒充新测量。
- `source-and-tool-hashes.json`、`static-checks.json`：本次阅读源/沿用脚本的哈希与静态检查；不是运行时通过。

不建议因本审查重跑已过且未变的功能套件或无证据改性能。主控若仍需要当前源的性能基线，按串行入口做一次；最终普通Player、逐请求真实来源和录音实听证据目前仍待主控生成。
