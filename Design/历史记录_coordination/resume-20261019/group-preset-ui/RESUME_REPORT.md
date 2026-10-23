# 独立群预设实际 UI 候选

2026-10-19。状态：**已隔离编译，未启动Unity、未执行UI、未改生产；等待主控单实例运行。** 群预设核心补丁保持冻结，本包只新增一个Editor runner及meta。

- 入口：`GroupPresetActualUiRunner.Run`
- 菜单：`Tools/Local Verification/Group preset actual UI`
- 新文件：`candidate/Assets/LocalBuild/Editor/GroupPresetActualUiRunner.cs` 和 `.meta`
- 补丁：`group-preset-ui.patch`，SHA256 `d11b77ed4195941e4540b5153937e9165f0a33c9e2927b7e3393ef2359b61ea4`
- HEAD：`3533141a406940d571e064f3797328c7d214235a`；`manifest.json`记录候选与6项实际生产依赖哈希。
- 已执行 `git apply --check`（exit 0），未应用。最终 `dotnet build verification/GroupUiNarrow.csproj --nologo -v:q -m:2`：仅编译本runner，引用实际Unity/生产程序集，0错误、0警告；日志 `verification/compile-narrow.log`。早先完整Editor隔离编译通过；最后复编时主控已开始合888源码、对应Library DLL尚旧，触发8项无关的casinoAiSeq等缺字段错误，完整失败日志保留 `verification/full-editor-stale-888-dependency.log`。未修改888源码或库去绕过；最终本runner窄编译单独通过。

## 执行顺序

1. 仍在EditMode、未切场景时，顺序执行 `LaoZhouNewsChecks.Run` 和 `WorldYyGiftRoutingChecks.RunInteractions`。两者使用inactive GameObject + SetSimulationForTests，不读写正常存档；LZ自身恢复缓存/随机状态，runner额外保存/恢复World使用的新闻/预设缓存、RNG、WallClock。每个fixture在finally销毁。必须两者输出成功标记且无Error/Exception才进入UI。
2. 单独输出 `preflight-consumers.log`：LZ保留其实际断言数量，World保留它提供的PASS标记（该检查没有数量接口）；不将它们纳入UI计数。
3. 复用独立Main runner宿主；独立Save/DesktopProfile目录、1280×720 GameView夹具、输入设备隔离与还原、禁用模型/无经济Tick、错误捕获、截图、恢复原场景/设置。不接入或改动Sidebar共享runner。
4. 主控已定位OriginCurtain独立起源影片会挡UI；本runner在首次Bind前将该无关剧情driver同其它独立剧情一起冻结，保留真实LCD命中检查。通知host使用已有 `GetComponent<DesktopNotifications>()` 判空禁用，避免TestMode下Install返回null。支持 `-verificationExit`，非batch前台执行后也可退出。

## 实际操作与明确夹具

|检查|实际执行路径|夹具/边界|
|---|---|---|
|中英每日预设|实际YY窗口Choice按钮经 FilesWindowUiChecks.Click 的LCD坐标映射、精确射线命中和down/up，3→2，同日不补；语言互切仍保持相同ID|日期、阶段和空历史为显式前置；非自然游玩|
|次日刷新与不重发|显式GameCalendar.Advance一次，等待生产hub.Update刷新气泡，已发ID不再出现|日期推进是夹具，不伪称自然进度；不把当天未发内容一律永久删除|
|第二次自由输入|TMP字段明确赋值，再真实点击Send；自然等待第一行8秒、第二行3秒|周计数预置1，第一条没有实际发送；第二条走真实view→Send；不是物理键盘输入|
|自然延时|观察Time.realtimeSinceStartupAsDouble，至少7.8/2.8秒（0.2秒采样容差），不手动推进协程或修改时间|每语言独立真实等待；前台丢失直接失败而非伪造focus|
|作者优先|另一条真实第二次冷场开始6秒后，真实点击预设，等待原2–4秒作者队列自然播放，逐条校验均在冷场之前|无需伪造presetReplyRunning；使用真实资源原回复|
|部分续播/组件启停|第一条冷场已实际收到后禁用hub，等待3.4秒不继续；以生产SaveSlot(6)保存，切到另一独立档，恢复slot6后再启用hub，自然等3秒只发剩下第二行|保存/加载调用实际API，非菜单物理点击；文件严格限制在本轮Save/slots/6|
|旧回调跨档|保留旧气泡UnityEvent，在另一档显式Invoke并断言未新增消息/已发ID，另等3.4秒确认无旧冷场|明确是迟到回调夹具，不是旧按钮真实点击；不宣称实际本地模型迟到回调验证|

预计成功输出14张LCD截图和1张全帧截图；实际数量以报告为准。输出目录：`LocalVerification/GroupPresetActual/<UTC-run>/`，`report.json`、`passed.txt`、`preflight-consumers.log`、截图、隔离Save文件。真实运行未完成前这些文件不应被声称存在。

## 仍需主控实际运行

- 本包只编译过，首次Main初始化、布局/遮挡、实际焦点与截图仍可能揭示夹具或产品问题。不得绕过FilesWindowUiChecks的命中断言、直接调用产品Send来凑UI通过、伪造Application.isFocused或用计时夹具代替自然等待。
- Preflight失败会停止在EditMode，并保留该目录的`preflight-consumers.log`；不会进入UI或生成UI成功标记。
- 不测试新模型请求与原生迟到结果；本地模型始终禁用。核心generation守卫与另一个已通过的RuntimeChecks是独立证据。
- 不包含真实用户主动触发引导剧本UI、第五次冷场完整UI、所有33条耗尽UI，也不宣称Player/物理鼠标/自然整局验证；这些未覆盖边界按需要追加。
- 减少特效PlayerPrefs、GameView大小、源场景、输入设备/设置和故事探针在结束/失败时恢复；普通存档路径始终隔离。调试首次失败应保留原报告与截图。

