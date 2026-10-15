# motion-018：通用按钮反馈性能审查

日期：2026-10-13（Asia/Singapore）。结论：确认活动动画每帧创建临时列表，交付一个最小候选补丁；组件重复创建、粒子/音频初始化方面无需改动。生产代码保持只读。完成的是静态审查及补丁只读应用检查，未编译本候选、未启动 Unity/游戏、未进行运行或视觉验收，不提供 FPS 数值，不标记“三层反馈”全局原则完成。

## 已确认的问题与候选修正

`Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs:292–306` 在 punches 非空的每一次 Update 中创建 `new List<RectTransform>()`，即使本帧没有任何完成项也分配 List 对象；完成帧首次 Add 还分配后备数组。新通用按钮首次按压后启用独立 XgJuice，因此也会承担这个已有引擎路径的分配。这是源码可确认的冗余分配，不等于已测出卡顿。

`changes.patch` 只针对这个共享文件：新增惰性 scratch List 字段，重复使用同一列表，在移除完成项后 Clear 以释放目标引用并保留容量。未开始过 Knock 的实例不新增 List 对象；第一次活动更新仍有缓存创建成本，首次完成仍可能扩容。缓存容量足够后，该处不再为每帧创建列表。迭代/移除顺序、缓动计算、普通/Reduced 参数、旧业务动效和取消行为均不改。没有添加对象池、全局服务或新组件。

这是一个托管临时分配换少量常驻容量的补丁；共享业务栈也会使用它，需按 measurement.md 覆盖多目标/目标销毁场景。补丁没有解决每个新动画周期创建 Punch、Button 本体每帧 LateUpdate 调度、TMP/Canvas 重建或任意 Visible 委托成本，不能宣称按钮“零分配”。

## 当前实现证据与无需改动项

行号对应 source-sha256.json 所记录的当前版本，路径以工程根目录为基准。

| 检查项 | 代码依据与结论 |
| --- | --- |
| Button 数量 | `XgUi.cs:192` 直接 AddComponent<XgButtonPressFeedback>，它继承 Button；没有先创建一个普通 Button 再叠加第二个反馈组件。Label 在 210 行绑定。 |
| 私有组件数量 | `XgButtonPressFeedback.cs:33–44` 先检查活跃、交互、Label、宿主状态，然后只在 pressFx==null 时添加一个 XgJuice。固定 Label 下未用按钮为 0，已用按钮最多 1；结束/取消停用后保留用于下一次。无证据需要每次销毁/重建或预热所有按钮。 |
| 首次分配 | `XgJuice.cs:35–42` 初始化 3 个 List、1 个 Dictionary、4 个 Stack 包装对象；`Knock:104` 首个条目创建 Punch 并分配字典存储。容量/委托/JIT/本机大小受运行环境影响，不给臆测字节数。新按钮仅调用 Knock，不调用 `Init:44–61`，所以不会由这条路径创建 Fx/Flash 或初始化声音池。 |
| 后续分配 | 动画中重复 Knock 复用 Punch；动画结束或 Clear 后条目被移除，下一次仍会 new Punch。缓存列表补丁只去掉逐帧临时 List，不去掉 Punch。 |
| 空闲成本 | `XgButtonPressFeedback.LateUpdate:47–56` 对尚未使用或已停用 pressFx 立即返回；仍有每个启用 Button 的 LateUpdate 消息调度。不能停用 Button 来省调度，因为会影响交互。没有实测证据要求引入集中调度器。 |
| 可见性 | 每次 PressLabel 从父层查询宿主；开始前与活动 LateUpdate 调用宿主 Visible。父链查询随层级成本变化；缓存父链可能破坏重挂场景，未在无测量时更改。宿主 delegate 是否昂贵由调用者决定。不可见活动效果在 LateUpdate 清理，Update 先于 LateUpdate，切换当帧可能多做一次效果更新；不宣称立即零开销。 |
| 清理 | `ResetPress:70–75` 调用私有 Clear 后停用组件；`XgJuice.Clear:202–210` 调用 OnDisable 恢复 punches 并清空，随后停用组件还可能触发一次空 OnDisable。泛用实例其他集合为空且只有一个 Punch，清理规模受限，没有已证明值得更改的清理热点。空实例集合是具体集合 foreach，无源码层面必需的每次临时 List。 |
| 可见性的界限 | Label/父 GameObject 不活跃由 Unity 停止对应消息，父窗口逻辑可见性由 host.Visible 负责。CanvasGroup.alpha=0、离屏或遮挡本身不被本类检测；没有宿主时也没有窗口可见性判断。这些不能自动归类为已确认性能故障。 |

## 去重与整合

已核对既有 `Documentation/DesignPass/Agents/motion-task-001.md`、`Assets/LocalBuild/Editor/MotionButtonChecks.cs` 和已到达相关任务记录。任务 001 与 motion-007 已识别“有临时 List 分配”，但没有修复/测量交付；现有 MotionButtonChecks 仅同步登记和清理检查，不应当作真实帧性能证据。本次不重复输入/连点行为实现。

已到达的 motion-009/changes.patch 同样触及 XgJuice.Update，其工作是布局跟踪，保留了 `var done = new List<RectTransform>();`。两份补丁不能盲目顺序应用：整合者应把本次字段、惰性复用与末尾 Clear 三处修改合入最终版本，保留另一补丁经独立审查后认可的布局逻辑，再统一运行。这里只说明相交位置，不替其他任务验收或采纳其改动。

交付文件：

- `changes.patch`：目标文件、原文件 SHA256、上下文均随补丁附带；待整合，没有应用到 Assets。
- `MotionButtonPerfProbe.cs`：独立编辑器测量候选，使用生产方法隔离首次输入、重触发、活动更新、取消、重开、空闲与隐藏输入的分配；尚未编译/运行。
- `measurement.md`：可复现 A/B 步骤、探针局限、真实帧/本机成本测量、多目标/销毁边界及视觉验收方法。
- `source-sha256.json`：本次生产依赖和已有任务记录指纹。

没有依赖缺失需等待；探针接口合同见 measurement.md。统一编译、真实帧性能采样与截图由主聊天负责。只写本目录，没有改 Assets、ProjectSettings、全局清单或其他目录，没有创建聊天/子 agent、跨聊天发消息、提交/推送/打包或接触真实存档；未阅读新策划正文。

## 已执行检查与待验证

- 已执行静态源码核对；`git apply --check --ignore-space-change` 退出码 0，仅验证补丁上下文可以应用，未写生产文件，不是编译/运行通过。
- 待统一编译候选和测量脚本；探针依赖当前私有方法名称，生产实现变化时先核对合同。
- 待运行基线/候选 A/B；确认 List 分配移除、首次/重开成本没有被隐藏、组件数稳定、恢复和宿主业务效果不变。
- 待验证共享 XgJuice 多目标完成、部分完成、销毁目标和多次清理；待实景视觉验收。未运行项一律无 PASS 结论。

结束前依赖指纹复核结果见下方追加记录。

交付末尾已重新计算 source-sha256.json 中全部 5 个依赖的 SHA256，与首次快照一致。补丁目标仍为 `2ABCA8BD6A93D03CF679A786F1A8B6EB2B50A2505A68CE2AA5F890844CF567EC`；本复核只证明静态证据未漂移，不代表 Unity 编译或实际运行通过。
