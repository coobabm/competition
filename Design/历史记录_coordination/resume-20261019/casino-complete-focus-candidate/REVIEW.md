# Casino Gossip 完整焦点诊断候选

2026-10-19；仅修改 runner 候选。生产文件、产品代码、剧情门禁、等待时长、焦点判定未改。没有启动/重跑 Unity，没有编译；只执行文本不变量检查及暂存基线 `git apply --check`（退出码0）。另一路生产Editor的运行权保持不动。

## 已有证据与盲区

已读 `competition-latest-20261016/LocalVerification/CasinoScamActual/20261019-110632-556/report.json`：

- 状态 failed，失败于 `Gossip` 的 `foreground retained during actual gossip wait`（当时 runner:570）。12个具名featureChecks，3170次含逐帧守卫的断言调用；runtime/cleanup errors均空。不是赌场套件通过。
- `focusObservations` 实际只有 `zh-paid/closed-reload-idempotence/begin` 和 `/end` 两条，分别 frame524/546、elapsed0/0.522，均 focused=True、minimized=False、probePresent=False、GameView。它们只证明此前半秒观察的两个采样点。
- 当前代码在 Gossip 成功交付后才记录 `gossip-before-SaveSlot/after-SaveSlot/after-Bind/after-LoadSlot`。这次在真实等待期间已经失败，这组记录尚未执行，不能拿它定位失败前后的 Save/Bind。
- 原 Gossip/StaleGossip 两个 while 用裸 Need，异常里缺 elapsed/frame/Editor focusedWindow；已有 RequireForeground只用于WaitSeconds，因此记录覆盖不完整。

## 本候选内容

精确补丁 `casino-complete-focus.patch` 只涉及 `Assets/LocalBuild/Editor/CasinoScamActualUiRunner.cs`，基线SHA256 `165c8a5f185ac71a03e744f417829bdc65eae31b97ff3f8b8e034b6486211709`。完整候选、基线、生成脚本与哈希在相邻目录。

1. 复用现有 `FocusState`，增加 `unscaledTime`；保留 phase、language、UTC、realtime elapsed、frame/frameDelta、isFocused、minimized、minimizedProbePresent、editorPaused、playing、focusedEditorWindow。
2. `RequireForeground` 仍一次读取focused/minimized，仍调用一次 Need 执行同一严格谓词 `focused && !minimized`。失败字符串同时写入既有 report.focusObservations，再抛异常；现有 Tick catch 已负责 Save，新增诊断不逐帧写盘。
3. Gossip：记录OpenGroup前、真实等待开始/结束；用RequireForeground替换裸Need。中英阶段由原tag区分。elapsed原点取OpenGroup前，因此包含原来已有的窗口就绪/3秒稳定时间，不冒称纯while时长。
4. StaleGossip：记录Bind前、Bind后/OpenGroup前、等待开始/结束；同样替换裸Need。在原有跨档/旧deadline测试中得到阶段可比记录。
5. `VisibleUntil` 首尾及焦点失败也走同一记录方式，覆盖排队与消息抵达的辅助等待，避免只补40秒while后又在相邻等待留下同类盲区。成功帧不会逐帧追加记录，仅begin/end和失败点；不新增额外帧或Sleep。

严格保留：`scheduled-Time.unscaledTime>30`；`Time.unscaledTime<scheduled-.1f`；抵达后的10秒VisibleUntil；`Time.unscaledTime<oldAt+1`；读档后41秒去重观察；Foreground的60秒超时及连续3秒稳定；所有消息计数、旧/新owner和学习奖励断言。Bind/SaveSlot/LoadSlot/OpenGroup/Foreground方法逐字未变。没有给Focus/Show/BringToFront增加调用，没有注入EditorMinimizedProbe，没有推迟/缩短/跳过真实等待。

## 已知 runner API 的焦点影响审查

以下是静态调用依据，不是运行归因结论：

| 路径 | 已见行为 | 与此次等待的关系 |
|---|---|---|
| Runner.Run:83 | `EditorWindow.GetWindow`、Show、**Focus**、Repaint，并设置固定GameView分辨率、打开Main、EnterPlaymode | 明确会操作Editor窗口焦点，但位于启动阶段；等待循环不重调。 |
| Runner.Tick:188 | 查找GameView并Repaint | 只看到重绘请求，没有Focus/Show/Close/OS前台切换调用；不能从API名字推导运行时绝无编辑器侧效应。 |
| Runner.Foreground | 读取Application.isFocused、IsMinimized，等待连续3秒 | 不抓焦点、不伪造状态，也不在失败后重试整个套件。候选原样保留。 |
| Runner.OpenGroup → ChapterOneDesktopRouter.Open → app OpenApp / WindowManager | 打开游戏里的YY窗口；`WindowManager.OpenWindow:210`调用FocusToWindow | `FocusToWindow:322–325`仅`transform.SetAsLastSibling()`，是游戏内层级；不等同Windows前台或Unity EditorWindow.Focus。 |
| FilesWindowUiChecks.Click:76 | ForceUpdateCanvases、LCD raycast校验、OnPointerEnter/Down/Up | 可触发目标产品按钮行为；此helper未调用Editor.Focus/Show或OS前台API；等待while内也不重复点击。 |
| QaDesktopCapture.Save | 渲染纹理Blit、ReadPixels、PNG写盘，并还原RenderTexture.active | 未看到焦点API；Gossip交付成功后的截图尚未到达，故不能把它指定为本次while失败原因。 |
| SaveSlot / LoadArchive | 指定slot6读写、Saving/Changed事件、Attach恢复owner | 此实现没有文件选择对话框或Editor Focus；事件订阅可能改变游戏UI，不能将“同帧出现”误当OS焦点因果。已有前后诊断保留。 |
| SetupInput | AllDeviceInputAlwaysGoesToGameView、虚拟Mouse/Keyboard、mute外部Unity设备；backgroundBehavior沿原值 | 这是已有输入隔离，不会把Application.isFocused硬设为真；不能用它证明真实前台。没有改这些设置。 |
| Isolate / Cleanup / RestoreEditor | runInBackground=true；最后恢复输入、分辨率/场景，必要时Close新建GameView；退出Playmode | 这些可能影响Editor生命周期，但cleanup/Close是在失败/完成以后，不能倒推为失败前原因。 |

当前检查没有找到持续Gossip等待中显式切换操作系统/Editor窗口焦点的runner API，也没有证据证明失败由用户操作、窗口最小化、某插件或特定产品方法造成。保持原因未定。

`PlayerWindowState.IsMinimized` 在UNITY_EDITOR分支仅取 `EditorMinimizedProbe != null && probe()`。因此 probePresent=False 时的 minimized=False **不是Windows原生最小化实测**。候选保留probe存在性字段，不安装探针、不修改谓词。

## 集成与一次后续验收建议（本轮未执行）

- 等字体/其他串行Editor工作释放后，由主控合这个单文件patch；生产哈希若变化需重新查看hunks，勿整文件覆盖新的有效修改。
- 后续只按主控已有安排运行一次完整赌场验收；不因为此前失败反复重跑、不替换真实前台判定。
- 若再次失败，先看对应 `zh-paid/gossip-wait`、`en-paid/gossip-wait`、`stale-gossip/wait` 或 `visible-until/...` 的失败采样与begin记录；检查时间/帧差、focusedEditorWindow及probePresent，保留原failure和截图。该记录只能缩小阶段范围，不自动确定焦点丢失根因。
- 在全套完成前，不把补齐诊断、编译成功或单个分支成功当成赌场实窗通过。

未编译、未运行、未改生产。本包为可应用的诊断候选，非功能修复或通过证据。
