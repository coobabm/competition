# 正式 Windows 发布包验收准备（静态审计，未执行）

**root 最新决定：避免创建 Windows 账户。本文件第3节账户路线已暂停，不能直接执行第4节启动模板。现有独立静态候选位于 ../release-verification-dir-stage/：实现 `--verification-save-dir`，待 root 导入、编译和运行验证。按该候选 README 替换隔离前提后，其它入口审计、真实范围与证据模板仍可复用。**

本目录仅交执行清单与证据模板，不修改生产，不创建另一套运行框架。没有启动 Unity、编译、Player、模型或录音。当前普通发布包没有安全隔离参数，也没有每次模型请求/取消的完整 Player 遥测。因此本清单把可直接执行的最终包试玩与可复用的真实模型 Editor 验证分开；不能填一个总 PASS 掩盖缺口。

## 1. 现有入口及准确范围

路径均相对 `C:/Users/xomem/Documents/ChatGPT/games`。

| 入口 | 可复用内容 | 不覆盖的内容 |
|---|---|---|
| `competition-latest-20261016/Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs` / `IntegratedReleaseBuild.Run` | 两场景、Windows x64 Mono、D3D11、CPU 模型路径，build-result 与工作区 source-provenance | 构建成功不是实际试玩；没有额外 RECORDING_PREVIEW define；不设存档隔离参数 |
| `.../Assets/LocalBuild/Editor/WindowsReleaseBuild.cs` | 老发布设置的参考 | 旧版本号与保存身份 setter，不能代替当前发布入口 |
| `.coordination/integration-20261018/release-audit-stage/Player-WindowQa.ps1` | 已运行 Player 的 PID/HWND 检查、精确窗口截图、前景点击 | 不启动 Player，不自动走剧情，不输入文本；PrintWindow 对 GPU 窗口可能失败 |
| 同目录 `record_loopback.py` 与 `audio-env/Scripts/python.exe` | 指定时长的真实 WASAPI 系统输出 WAV、端点与样本指标 | 不是游戏独占音频，非零样本不证明音乐正确或无卡顿 |
| `.../Assets/LocalBuild/Editor/SerialNativeQa.cs` / `SerialNativeQa.Run` | 真实 Qwen native 调用、优先级、实际未完成 Task 上的取消、下一请求等实际 Task 结束、单槽峰值 | 空场景 Editor；不代表最终 Player 的 YY、窗口或存档行为 |
| `.../Assets/LocalBuild/Editor/SfxBackgroundLoadChecks.cs` / `Frames()` | 58 个生产 WAV 的加载与 6 类播放请求 isPlaying 观测 | Editor coroutine 不是 executeMethod；不能直接当作 Player/听音验收 |
| 同目录 `Package-VerifiedRelease.ps1` | 已构建包的模型 SHA256、原生库/许可与来源检查、独立打包 | 不试玩、不推理、不录音；完整模型 hash 属于后续串行打包 I/O |

只复用这些现有脚本，不运行旧 `RecordingPlayerSmoke`。

## 2. 已核实的旧假设

- `RecordingPlayerSmoke.cs:1` 被 `RECORDING_PREVIEW` 包住，普通 Release 不包含该入口。`--recording-smoke` 不会使普通包隔离存档。
- `ChapterOneRuntime.VerificationSaveDirectory` 也只存在于 `UNITY_EDITOR`。普通包的存档目录来自 `Application.persistentDataPath/LingGuangV05`。内部 `SaveDirectoryOverride` 不是现成 CLI 参数。
- `RecordingPlayerSmoke` 依赖已注入阶段 4、`Recording-Stage4` 保存路径、蜘蛛/旧壁纸，以及固定 25 秒等待。不能代表新包正常标题→新游戏路径。
- 关键过时点是 `crowd.Label.YesButton/NoButton`：当前 `ZhongbaoView.PageIds` 是 contracts/auto/subcontract/ledger/credit，`Build()` 没有创建 Label。`Open("label")` 已路由到灵光 `teach`。因此 `Label` 属性还在不等于可用；旧脚本会空引用/测错入口。
- 四个灵光 `NavIds`（teach/train/tree/home）目前仍成立，不应把这一条也误判为过时。私教 Yes/No 的具体题型假设需逐卡核实，不能沿用旧众包题目答案来判企业收入。
- 当前 `ProjectSettings.asset` 实际为 `DefaultCompany / competition`。`IntegratedReleaseBuild` 保留当前身份；旧 `WindowsReleaseBuild` 写入的是 `RippleLocalPreview / LingGuang-Playability-20261008`。最终以 **build-result 的 company/product** 为准，根任务须在发布前决定/核对身份；不能依据注释宣称旧存档迁移已通过。
- 审计时 `Build/` 只有 `MyComputerAlive-20261016-1dd68c6`。没有把它当作本轮最终构建。
- `release-audit-stage/最终Player验收工具审计.md` 中“缺 loopback 工具”已被后来的 `AUDIO-READY.md` 更新：隔离 venv 已安装且以前枚举确认 Realtek 输出与同 id loopback。但现在仍需重新 `--inspect`；旧设备枚举不是本次录音证据。

## 3. 不触碰玩家存档的执行前提

采用**独立 Windows 测试账户 + 完整发布目录测试副本**。不要在 xomem 的登录会话启动正式包做本清单，不移动、备份覆盖、清空或暂时替换玩家的 LocalLow/HKCU。测试账户应是未玩过此游戏的专用账户，交互式登录后执行。

原因：LocalLow 和 PlayerPrefs/HKCU 随 Windows 账户隔离；DreamOS 数据则跟发布目录走，需要完整副本。`DreamOSDataManager.InitializeProfile` 用 `Application.dataPath` 字符串拼接/替换，不能仅凭猜测认为数据总在根目录；产品名与 exe 名不同还会影响该拼接结果。整个可写测试副本放在测试账户自身目录，原包只作为复制源。若没有可用专用测试账户，本次 Player 验收保持 blocked，不改用伪造 USERPROFILE/APPDATA 环境变量或不存在的 CLI 参数。

在测试账户中先记录 `whoami /user`、`$env:USERPROFILE`，核对不是玩家账户；将完整最终发布包复制到该账户 Documents 下的新目录。证据另建新目录，不与发布副本/旧报告混用。使用最终 build-result/source-provenance/打包 hash，把所有报告绑定到该包。

不要改 company/product 来“隔离测试”后把测试 build 当正式包。不同账户运行同一份最终二进制才是本清单的正常路径。

## 4. Player 操作顺序与证据

待 root 结束其它 Unity/Player/模型负载后，串行执行。下面是执行模板，必须先把 `$copy`、`$evidence`、脚本路径替换为测试账户可访问的真实绝对路径；本任务没有运行这些命令。

```powershell
$copy = 'C:/Users/TEST_ACCOUNT/Documents/ReleaseSmoke/FINAL_BUILD_COPY'
$evidence = 'C:/Users/TEST_ACCOUNT/Documents/ReleaseSmoke/NEW_EVIDENCE'
$exe = Join-Path $copy 'MyComputerAlive.exe'
# 先核对账户与 build-result，再创建全新的证据目录。
New-Item -ItemType Directory -Path $evidence -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $copy 'build-result.json') -Destination $evidence
Copy-Item -LiteralPath (Join-Path $copy 'source-provenance.json') -Destination $evidence
whoami /user | Out-File (Join-Path $evidence 'test-account.txt')
# 需要可见交互窗口完成实际试玩，故显式 Normal；不是后台 helper。
$player = Start-Process -FilePath $exe -WorkingDirectory $copy -WindowStyle Normal -PassThru -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"'+(Join-Path $evidence 'player.log')+'"'))
# 复用原窗口工具，先 Inspect，再用它实际返回的 handle。
& $windowTool -TargetProcessId $player.Id -Action Inspect
```

`$windowTool` 指向现有 `Player-WindowQa.ps1` 的可访问副本。每次导航后重新确认画面，不预设坐标。它包含 Add-Type 原生声明，root 执行阶段才加载；本准备任务没有执行该脚本。

1. **标题**：记录进程路径/PID/启动时间/窗口大小；保存 `01-title.png`。用真实鼠标点击开始，不调用 menu.BeginGame。记录启动耗时、转场是否可见、是否报错。
2. **新档开局**：在测试账户新档按实际流程完成必要开场，记录选择与实际步骤；保存 `02-desktop.png`。不注入 stage、appInstallState，不截加载前桌面冒充完成开机。若需要高级阶段测试，应另列测试档来源与导入步骤，只导入测试账户，不将其说成自然推进。
3. **教**：用实际桌面图标打开灵光，点击教页，保存 `03-teach.png`。记录当前阶段、可见卡片、可点击按钮与锁定提示。低阶段被锁仅证明门槛画面，不算已验证解锁功能。
4. **练**：点击练页并关闭/重开灵光，保存 `04-train.png`。记录布局/文字/按钮/响应，不要求为了烟测开始重训练。
5. **YY**：从真实图标开 YY，选玩家 AI 会话，保存 `05-yy.png`；发送一条实际键入的短自由问题，保存输入、发送时间、完整回答、回答到达时间和 `06-yy-response.png`。避免网吧群、答题分支或已有固定问候。不要手填聊天气泡/改存档消息。
6. **模型真实性判定**：记录同次 player.log 的“本地模型就绪”、实际 Qwen 名称、CPU/native 架构与失败/超时日志。**仅就绪日志+气泡还不足以证明这条气泡来自模型**：`YYChatHub.Reply` 在 null/重复回答时会 fallback，且阶段1/2有语言约束。当前普通包没有逐请求输出来源遥测；若无额外真实 native 回调证据，这一项写 `unverified_native_origin`，不能填 PASS，也不能用 Editor 报告代替。根任务如要求最终包严格过此项，需先增加只读 opt-in 请求来源记录，再构建并重走本清单；这是明确的剩余仪器缺口。
7. **音乐/声音**：按下一节录真实样本并听音；同时记录打开前、播放、关闭窗口后继续播放、进度跳转、下一曲的窗口画面与动作时刻。暂停/切曲造成的安静区间不要当掉音。
8. **退出**：用正常关机回标题，保存 `07-return-title.png`，再退出；收集 Player exit code 和完整日志，检查异常/崩溃。保留测试账户的测试存档和整个证据目录，不向玩家账户复制任何数据。

截图示例：`& $windowTool -TargetProcessId $player.Id -WindowHandle $hwnd -Action Capture -OutputPath (Join-Path $evidence '01-title.png')`。PrintWindow 失败时不能换 Editor render target 充数；root 可另用已核实的 ffmpeg gdigrab 窗口采集，但须先检查本机 demuxer 支持的 hwnd/title 选项，绝不抓整桌面补位。

## 5. 真实模型单槽/取消：复用现有检查，范围单列

现有证据 `competition-latest-20261016/LocalVerification/SerialNative/20261018-075016-103/result.json` 为以前的成功运行：Qwen3-4B-Q4_K_M、llamalib_win-x64_avx2.dll、Cpu；maxNativeCalls=1、maxActualTasks=1，取消时 Task 未完成，下一请求在其完成后才启动，服务停止与 singleton 保留成功。这是历史 Editor 证据，本次没有重跑。

需要重新确认最终源码时，root 在现有独立 batch Editor 工作流调用 `-executeMethod SerialNativeQa.Run`，**不传 -quit**（runner 负责进/退 PlayMode 和退出），模型服务与其他 Unity/Player运行串行。复用其新产生的 result.json、passed.txt 和 owned-native 日志，绑定最终 provenance。该检查有4个调度短 chat、1个真实取消 judge、1个取消后 chat，不另建模型测试框架。空场景不会创建游戏 runtime/玩家存档。

通过必须检查：模型实际 Ready/Owning native service，非空真实回调且各一次，取消发生在实际未完成 Task，cancelled 回调一次，后续请求非空，下一请求 dispatch 晚于实际 cancelled Task 完成，两个峰值均为1，清理完成。仅 UI“取消”字样、PendingCount=0 或请求计数变0不足以证明 native 已结束。

**最终 Player 串行取消仍未被这个 Editor 检查覆盖。** 生产 `XgBrain.CancelPending` 会调用 `LocalLlm.CancelJudges(this)`，但关闭教/练窗口不保证走该取消路径；不要写“关窗口=取消通过”。要把 Player 此项由 unverified 改成 pass，必须有该包真实 native Task 生命周期/取消与后续 dispatch 证据。可沿用 SerialNativeQa 的观察方法，通过生产公开 `Judge/CancelJudges/Chat` 做 opt-in probe，记录实际 running Task 完成，不能把 StartEditorQa/JudgeForEditorQa 解开 #if 后直接常驻玩家程序。当前交付不引入该 probe，证据模板显式保留缺口。

## 6. 音频设备与真实样本

复用 `release-audit-stage/audio-env/Scripts/python.exe` 和 `record_loopback.py`，在交互测试会话可访问的位置使用；如果隔离账户无权访问该 venv，先由 root 准备可用工具副本/环境，不能默默切成麦克风。

```powershell
& $audioPython $audioScript --inspect | Out-File (Join-Path $evidence 'audio-device.json') -Encoding utf8
# root 决定开始采样时才执行；先关闭/静音无关应用。
& $audioPython $audioScript --record --seconds 10 --output (Join-Path $evidence 'music-playing.wav')
```

依次单独取样：打开前安静基线、正常播放、关闭音乐窗后的持续播放；按需要追加明确标记的拖进度/下一曲样本。每段1–30秒，脚本监督超时为时长+15秒，不无限录制。输出 WAV 和同名 JSON，记录默认输出id/name、匹配 loopback=true、48kHz/双声道、时钟、真实帧数、RMS/peak、非零/剪切计数、100ms块与静音区间、SHA256。失败记录 `.failed.json`，部分 WAV 不算通过。

听完原始 WAV 后填预期曲目是否可辨、是否有爆音/卡顿、静音区间是否对应操作/音乐休止、是否混入别的应用。设备可枚举≠输出可听，AudioSource.isPlaying≠录到声音，非零 PCM≠正确音乐，单次10秒样本≠长时间连续稳定。不得拼素材或静音轨补成“现场声音”；主角没有配音是设计要求，不是音频设备故障。

## 7. 判定与本轮交付

填写 `evidence-template.json` 的每个范围并附原始产物路径。最终状态必须区分 `passed / failed / not_run / unverified / blocked`。缺 Player native 来源/串行取消证据时整体不是全面通过；可分别报告 UI实际试玩、Editor native、WASAPI听音结果。此次交付只有静态准备，不包含任何新实测通过。
