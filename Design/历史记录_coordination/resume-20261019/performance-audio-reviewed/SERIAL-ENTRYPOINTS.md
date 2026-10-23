# 主控串行入口清单

所有执行都由主控安排；本审查未运行这些Unity/Player入口。继续沿用完整清单：
`C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/release-final-audit/candidate/RELEASE-SMOKE-CHECKLIST.md`。
此文件仅聚焦性能/音频/模型来源，不能替代该清单的存档、偏好、发行许可要求。

## 1. 可选：当前最终源的Editor性能诊断

先结束其他Unity/Player/native工作，冻结本次被测源，记录源hash与dirty状态；不并行编译、模型hash或打包。运行一次（全程有音频设备的主控环境）：

```powershell
$project='C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016'
$unity='C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe'
# 使用全新日志名；不要加 -quit，捕获入口自行退出。
& $unity -batchmode -forceAudio -projectPath $project -executeMethod IntegrationProfilerCapture.Run -logFile 'C:/ABSOLUTE/NEW-profiler.log'
```

等待实际退出并检查日志所指的精确 `LocalVerification/IntegrationProfiler/<id>`：complete.txt、3份JSON/RAW、可见性截图、动作数。存在failure.txt或进程失败即保留失败，不换用旧目录。这里不测本地模型，也不代表声音已听到。

捕获停止后，离线原始分析：

```powershell
& $unity -batchmode -quit -projectPath $project -executeMethod ProfilerRawAudit.Run -profilerArchiveRoot 'C:/ABSOLUTE/EXACT-completed-capture' -logFile 'C:/ABSOLUTE/NEW-raw-audit.log'
```

要求当前日志RAW_AUDIT_PASS和新子目录complete.txt。检查真实`/Main Thread`、异常表、self/inclusive与工具开销。不要把inclusive父子相加。可用本包 `collect_profiler_evidence.py --capture <目录> --raw-export <该目录的新raw-analysis子目录> --output <新的外部JSON>`汇总；不需重新打开Unity。

脑窗旧41项证据在REVIEW.md注明；只有当前改动影响脑窗生命周期/绘制时再用`BrainWindowPerformanceQa.Run`，也是自行退出、不加`-quit`，不要用不受隔离启动保护的`Start`代替。现有失败/基础设施错误记录必须保留。

## 2. 构建唯一普通Player

功能验收完成且源冻结后：`IntegratedReleaseBuild.Run`（`-batchmode -quit`）。期望版本`2026.10.19.1`，Win64 Mono/D3D11、BuildOptions.None，身份从build-result.json读取。必须用本次日志`INTEGRATED_RELEASE_BUILD_PASS`指向的路径及本次source-provenance；不能只信latest-build.json。版本号不代表已经发布。

## 3. 隔离启动前

沿用工具目录：
`C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-smoke-current-stage`。

- 全新evidence目录；PlayerData留不存在或空目录，最终传两参数`--verification-save-dir <绝对目录>`。
- 先记录普通存档范围内原文件hash，并用`PlayerPrefs-Guard.ps1 -Action Snapshot`保存确切build-result身份的偏好；保留其输出和CLIXML。不修改用户正常进度。
- 记录exe/build-result/source-provenance/hash，按完整清单启动唯一可见Player。
- 先核对`VERIFICATION_SAVE_ROOT`、`.lingguang-verification-save`和`VERIFICATION_NATIVE_TRACE <确切路径>`，无隔离证明则停止测试，不能去掉隔离参数重试。
- `Player-WindowQa.ps1 -Action Inspect`固定PID、完整EXE路径、启动UTC及当前HWND。实际启动分辨率以当前窗口测量为准。无盲点循环、无注入存档、无隐藏测试开关。

## 4. 一次有界普通流程与真实声音

从标题物理机箱电源开始，按正常序章/名字/性格流程，完成可用桌面与实际存档。教学/训练当前锁定时如实截图，不造进度。等待模型正常就绪后，在可用私聊（建议晴雯）仅发送一次短句；记录联系人、发送/回复UTC、实际完整文本及截图。最多等待75秒，不重发。正常系统自己发起的setup/后台请求仍记录。

音乐采证复用现有回环工具：

```powershell
$audioPython='C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/audio-env/Scripts/python.exe'
$audioScript='C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-smoke-current-stage/record_loopback.py'
& $audioPython $audioScript --inspect
# 每段只写全新绝对WAV，1至30秒；需要同步点击时按完整清单Hidden后台启动，不能无限录音。
& $audioPython $audioScript --record --seconds 15 --output 'C:/ABSOLUTE/NEW-music-open.wav'
```

先打开音乐，确认可辨曲目/音量/进度两时刻；等首次旁白/fade结束采一段。第二段15秒覆盖真实关闭音乐窗口、等待、重开，不重启曲目，记操作UTC。自然顺序切曲用真实可见slider拖到曲末前约5秒，录15–20秒跨界，标“用户seek辅助自然曲末”，不能说完整听完或日常完成。若无可见seek则仅测实际Next并标自然跨界未测。

为直接审查音乐与单请求竞争，可把唯一有意私聊的发送安排在音乐稳定播放后的30秒录音内；记录回复是否落在录音范围，超出则不能声称已覆盖整个推理期。不得为了填满录音再发送。关闭/最小化YY或贴吧后重开，观察状态/未读/UI恢复；这只能作交互烟测，不能证实零CPU刷新。不要假定早期流程能购买脑窗。

每段要有completed=true的JSON、真实WAV、端点id/name、时间与hash，检查无.failed.json并实际听完。RMS/peak/静音区间只作线索；有意休止、ducking、seek、关机淡出不自动算掉音。其他应用可能混入回环，记录污染。原始WAV不得用音乐素材或静音轨替换。

## 5. 正常退出与判定

正常游戏关机/标题Quit，等待进程退出；保留完整日志、exit code、截图与隔离存档。按完整清单Compare→Restore（已退出PID）→Compare偏好，复核普通用户存档hash；最后Compare必须unchanged=true。脚本不会停止其他进程，因此主控先确认没有并行游戏。

对日志宣告的确切trace执行：
`Summarize-NativeTrace.ps1 -TracePath <确切JSONL> -PlayerLog <本次日志>`。

选中实际可见消息：同request_id完整成功native_dispatch/enter/return/task_completed/callback/delivered链；UTC/联系人吻合；native或native_filtered如实标注；无metadataIssues或trace写入失败，零unfinished，观察峰值不超过1。只看structurallyValid不足。取消没有发生就写未测；没有embedding模型就写BM25回退，不能假称所有服务并发已测。

只有实际录音听感、真实模型来源、最终Player路径分别满足，才分别填PASS。最终普通Player不自动覆盖后期图鉴/私人结局/群图全部流程。包装/大模型hash在Player/native完全退出之后再做，发布不由本审查触发。
