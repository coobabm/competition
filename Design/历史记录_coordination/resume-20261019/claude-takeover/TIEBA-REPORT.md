# E2 接管与最小整合报告

2026-10-19。生产：`competition-latest-20261016`。已读 E2 QUESTIONS、第三轮分工、六份候选 diff，并逐段对照生产；没有整文件覆盖生产、没有启动 Unity、没有修改 Hub／其它 agent 所有文件／普通存档／PlayerPrefs。

## 基线与文件

生产 TiebaView.cs 原始 SHA256：`bca45ba812cf6d300e7b5a5e9783d6a46258cd1d7abfcf51e6369543d3f7f308`，与 E2 声明基线一致。原件留在 `tieba-before/TiebaView.cs`；回撤必须按该差异审核，不能用 Git HEAD 覆盖原本已有 Go 和测量点改动。

已改：`Assets/LingGuangV05/DesktopBridge/Tieba/TiebaView.cs`。
新增及各自 .meta：`TiebaView.DelayedFloorKey.cs`、`TiebaView.SideKey.cs`（同目录）；`Assets/LocalBuild/Editor/TiebaDelayedFloorKeyChecks.cs`、`TiebaSideKeyChecks.cs`。
新 helper 和独立检查基于 E2 候选完整文件；生产主文件仅 apply_patch 最小 hunk。

## 已整合与审阅调整

1. **订阅／销毁解绑**：具名 MarkStale、Rename；Install 和 Start 共享幂等 BindEvents，重复 Install 不累积委托；换 hub 先解绑旧 hub；OnDestroy 使用 ReferenceEquals 处理 Unity fake-null。候选仅在 Start 捕获 hub，会留下重复 Install/rebind 边界，故未原样照搬。
2. **未激活协程 guard**：仅在 activeInHierarchy 时启动 ScrollToEnd。这是防御修复，未声称当前贴吧被门禁拦截，也未声称已实窗复现错误。协程同时捕获本次 ScrollRect，避免 chat → 其它页后旧协程把新页滚到底。
3. **延时刷新键**：用正数 afterSeconds 楼层阈值位替换无限十秒桶；当前 JSON 是三处数值 40/90/150，第四处是 note 文字。保留 .5 秒轮询；补 requires flags、DailyDataLinkHighlight，保留原有 DataRequestKey、未读／消息、typing、choices、语言、阶段、公开讨论 Stamp。额外保留独立 helpPosted 位（初次发帖到首楼回复前也须失效），加入 InPrologue，日期包含年份。
4. **Open／OnEnable**：opening try/finally 只压制显式 Open 期间的 OnEnable，随后 Go 一次到请求目标。原生图标／任务栏重新打开仍由 OnEnable 恢复当前页。保留 Go/DrawChat 可见性门禁；不会先为旧 chat 清未读。首次 root 尚未构建时仍走原有 Start→Update 绘制路径。
5. **侧栏复用**：按语言、高亮 board、全部 board id/name/nameEn 顺序生成键，再用 childCount 作结构保护；该键还进入 RefreshKey，因此同数量改名／改序即便没有 Changed，也会触发绘制。点击回调捕获渲染时的 boardId，避免替换同值 Board 对象后仍引用旧可变对象。
6. **测量点编译范围**：主控确认 Claude 目标会话用户在 `2026-10-19T11:28:55Z` 明确“正式版不保留，测试版留着”。7 Marker 名称和位置保留；使用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 直接包住 import、字段、所有 Auto 调用。正式版预处理后为 0 声明／0 测量调用，未引入候选 Measure 空包装器。Player 性能复验必须用 Development Build。
7. **草稿跨档边界**：同 forum-state + simulation 的消息／语言重绘保留草稿和焦点；加载另一档后不把上档未发送内容复制到新输入框。旧 Send/Choice 回调的 StillCurrent 检查完全保留。Go 主动导航的清草稿语义、chat 底部／其它页顶部的滚动意图保留。

## QUESTIONS 结论

- Q1：按当前“真正可见会话才已读”的源码规则修复 Open 冗余旧页，无需新增剧情设定。
- Q2：flags／高亮在 .5 秒轮询内与现有真实状态一致；不改变任何旗标生成／剧情计时条件。无需新设定。
- Q3：已核对 XgPublicDiscourseCatalog.Visible/Available：native forum 仅检查其自身 forum 安装，而 TiebaHub.Instance.View 存在时已加入；公开事件集合由 SetReleaseProjection 按不可变 revision 更新，Stamp 包含 revision/binding/stage/date/route/artifact。不增加重复 Native 查询。未来若允许不提升 revision 原位改 projection，需重新审查，但当前接口拒绝旧／相同 revision。
- Q4：已按用户明确决定整合条件编译；具体发行构建器 Development 标志由主控统一验证。
- Q5：InPrologue 是现有 CanPostHelp／ZhouChoices 显示输入，已加入键；仅补刷新，无剧情规则变化。
- Q6：Count 压缩碰撞保留；现实消息／清未读操作仍有 Changed，不扩改 Hub。
- Q7：六补丁按功能独立审阅；不是整套盲合。以上未发现需要用户选择的新设定冲突。

## 已运行证据

隔离 .NET 10 harness：`.coordination/resume-20261019/claude-takeover/tieba-checks/TiebaChecks.csproj`。
实际链接生产 Forum.cs、StoryJson.cs、两个新增 helper 和两个独立检查；仅 GameCalendar.Start、LaoZhouQaState 空壳及 Unity Application/Debug/MenuItem 使用 stub。

- `TIEBA_DELAYED_FLOOR_KEY_PASS: 6435 checks`：原始 forum.json 解析、40/90/150 边界、无发帖、固定其它条件的可见性变化对照、所有 requires flag。
- `TIEBA_SIDE_KEY_PASS: 13 checks`：语言、高亮、同数量 id／中英文名替换、改序、对象替换、字段边界、空列表。
- Roslyn 以 C#9 解析全部 TiebaView*.cs，Editor、Development、Release 三种预处理均无语法错误；Editor/Development 各 7 个 ProfilerMarker 实例化、7 个 Auto 调用位置，Release 均 0。
- `git diff --check -- Assets/LingGuangV05/DesktopBridge/Tieba/TiebaView.cs` 通过。

这证明数据／键逻辑和预处理语法；**不是 Unity 语义编译、真实 UI、Player、性能或音频验收**。没有收益数字或流畅性声明。

复跑：`dotnet run --project .coordination/resume-20261019/claude-takeover/tieba-checks -- C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016/Assets`。

## 主控待验清单

1. 单 Unity Editor 编译并调用 `TiebaDelayedFloorKeyChecks.Run`／`TiebaSideKeyChecks.Run`（菜单 Tools/Local Verification 下也可运行）。
2. 原生开关／任务栏最小化恢复／显式 Open(home/chat/其它页)，确认每次目标只绘制一次；旧 chat 不被误标已读，实际可见 chat 正常清未读。
3. 活跃／inactive 生命周期、销毁后静态语言事件、重复 Install 与换 hub 的订阅数量；不把防御 guard 当已复现 bug。
4. 发求助帖即刻／40/90/150 到达、qc.trap、daily.surf 高亮、序章结束、阶段／日期变化；中英和所有 tab 高亮。
5. 同档新消息重绘保留半输入草稿／焦点，跨档草稿清除，旧按钮不跨档发送；新消息后底部滚动；chat→其它页的次帧不被旧协程滚到底。
6. 侧栏节点复用真实对象 ID 与改名／改序失效；静态键检查不能替代实际 Unity 节点验收。
7. 同口径 Development Player 三段采样及正式版构建标志；尚未测到性能改善，不宣称音乐或卡顿解决。