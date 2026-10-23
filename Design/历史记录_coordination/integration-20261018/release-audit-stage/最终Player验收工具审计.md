# 最终 Player 实际验收工具审计

## 已核实

- 当前普通 Release 没有 --lingguang-smoke。旧 .coordination/standalone-result.json 属于 Builds/LingGuangV06，不能证明新包成功。
- ChapterOneRuntime.SaveDirectoryOverride 不支持普通 Release 命令行。--recording-smoke 仅 RECORDING_PREVIEW 编译时生效；IntegratedReleaseBuild 不设置该 define。不要声称启动参数已隔离存档。
- Assets/LocalBuild/RecordingPlayerSmoke.cs 仍断言旧众包标注、旧壁纸大脑和旧导航，不可用于本次最终验收。
- 工作区脚本搜索未找到可直接复用的外部 Win32 截图/点击脚本（GetWindowRect/PrintWindow/SetForegroundWindow/CopyFromScreen/mouse_event/SendInput）。不要拿 Editor 纹理截图当最终 Player 截图。
- ffmpeg 实际存在：C:/Users/xomem/AppData/Local/Microsoft/WinGet/Links/ffmpeg.exe。设备支持 gdigrab 与 dshow，不含 wasapi。当前设备枚举没有 audio-only DirectShow 输入，不能把视频录制假称带系统声音。
- 旧录制源码：C:/Users/xomem/Documents/ChatGPT/games/competition-20261011/LocalVerification/Recordings/2026-10-14-power-spider-01/Recording20261014-Continuous.cs，用 Unity ScreenCapture + OnAudioFilterRead 导出PCM，属于旧Editor注入方案，不是外部工具，不应注入本次Release。
- 同目录 postprocess_continuous.py 是 ffmpeg 音视频合成脚本，前提是已经有真实PCM，不能凭它获得系统声音。
- 常见 Program Files / Local Programs OBS 安装路径未找到 obs64.exe；系统有 OBS Virtual Camera 设备，但不等于已找到可运行OBS。

## 最低必要 Player 验收

1. 退出现有游戏。记录最终 build-result/source-provenance，完整备份当前 company/product 对应 LocalLow 游戏目录及持久化桌面配置。仅变更测试副本/新档；回滚时先保存本次测试产物，不能覆盖丢失玩家的新进度。
2. 启动最终 MyComputerAlive.exe，指定独立绝对 -logFile，窗口模式 1280×720。使用正常标题开机，截图标题、开机完成；不要重建/重写动画。
3. 通过真实桌面图标开喵鱼。新档阶段锁定应显示明确锁定页且不崩溃；第三阶段测试档应实际显示列表，切买/卖页并关闭重开。记录窗口活着和日志无未处理异常，不能只截锁定页宣称验证已开放商店。
4. 真实打开音乐应用：开之前静音；开后自然播放、拖进度不卡、关闭音乐窗口持续播放、当前曲结束或下一曲按钮能切换；通过系统实际听音/录制证据，日志不等于听见音频。主角始终无语音。
5. YY给玩家AI发送真实自由问题，等待实际Qwen响应，记录输入/完整回答、日志模型路径/Qwen3-4B与原生启动。不要用预设回复或 Editor SerialNative PASS 代替最终Player推理。
6. 正常关机回标题，结束进程后收集 -logFile、截图/录像、构建来源及结果。保存测试文件，再按完整备份恢复用户存档/配置。

仅画面录像可使用 ffmpeg gdigrab 抓指定游戏窗口（title必须先查真实标题），限制 30fps/短时长，输出独立验收目录。gdigrab 抓窗口可能包含遮挡，截图时确保游戏前景；不要抓整个桌面混入其他应用。现环境缺已核实系统loopback设备，因此声音验收目前优先人工实际听音；要录系统声音需进一步找到已有支持WASAPI loopback的工具，不能编造静音轨或拼接素材冒充游戏现场。
