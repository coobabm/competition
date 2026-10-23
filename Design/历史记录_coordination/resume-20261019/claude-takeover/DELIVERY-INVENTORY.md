# Claude 交付盘点与接手清单

审计日期：2026-10-19。只读核对 `ClaudeReturn/`、`ClaudeReturnRound2/`、`ClaudeReturnRound3/` 和 `competition-latest-20261016` 当前生产源码、`Design/整合记录.md`、第三轮分工。未启动 Unity、未修改生产、未访问普通玩家档案。本文唯一写入是协调报告。

## 结论

1. C1/C2 已合已验，C3/D3 文稿已按生产审阅版整合，不应重复开发。D1 已合生产审阅版但剩余 16 个具名实窗分支未执行通过；D2 只合最小 Go 重复刷新修复。
2. **D7/D8 仍是未合候选**，可接手。D8 必须只取命名提示 hunk；候选整文件会恢复刚删除的“先碰一下”。
3. **Round3 E1/E2/E3 已有源码候选，不能称“Claude 尚未交付”**。但三个 REPORT.md 均不存在；E1 QUESTIONS.md 也不存在。代码交付和报告完整性需分开记。
4. D5 旧身份迁移、D6 按最终路线回溯旧计划已作废，不能重启。正式身份仍为 `DefaultCompany/competition`（ProjectSettings.asset:15–16）。
5. Round2 E1 是性格设计、Round2 E2 是大脑光效设计；与 Round3 E1 存档、E2 贴吧是不同任务。性格/脑光效仍未有 Unity 实现交付；brain-v2 是更新的 HTML 原型。
6. 当前真正需要产品决策的是“性格不影响结局”与现有 R3 按主见/玩心选路的冲突。其余多数 QUESTIONS 是技术处理选项或已有答复，不应重新阻塞全部任务。

## 每项状态与证据

| 项目 | 当前分类 | 已完成与证据 | 还欠什么 |
|---|---|---|---|
| C1 安装引导 | 已合已验（Editor 范围） | 整合记录：`SidebarLayout/20261019-104154-992/report.json` passed，38 记录/27 图；Guide 25/25 | 最终 Player 总验收仍属主控；不要重做引导 |
| C2 公共阅读通知输入 | 已合已验（Editor 范围） | 105613-794 诊断闭环；干净复验 110253-787 passed，65 记录/31 图，错误 0 | 不重做，不改通知规则；旧探针已移出生产留档 |
| C3 日志/说明 | 已合文稿，发布未完成 | 与 D3 合并审阅；生产正文保留，完成状态已纠偏 | 最终版本/链接/包检事实更新 |
| D1 私人结局剩余分支 | 已合审阅版、编译；实窗未过 | 111951-097 卡在初始稳定前台门禁，0 具名结果；生产保留 LP2-latest-plan | 串行实跑生产套件：8 分支×中英；不是再实现产品规则 |
| D2 贴吧优化 | 部分已合已验，剩余候选 | Go 后保存已绘制状态 + 7 测量点；110253-787 实窗通过；112234-894 三段 RAW 支持减少重复绘制，整体帧时间未改善 | 剩余拆分转 Round3 E2；不能宣称流畅/音频已解决 |
| D3 发行材料 | 已合审阅版 | 七文稿与六张原图已整合；DreamOS 授权用户已确认 | 最新 README/许可增量与实际包检；不得依新候选删除已审六图或公开购买凭证 |
| D5 旧存档迁移 | 作废 | REJECTED.txt、整合记录与第三轮分工均明确不接入 | 无；禁止扫描/搬迁旧公司名/产品名目录 |
| D6 旧计划回溯 | 作废 | REJECTED.txt、生产 `PlannedFact(state)` 取最后有效计划 | 无；禁止恢复最终路线回溯旧计划 |
| D7 DreamOS 数据路径 | 候选未合、未验 | 有 REPORT/QUESTIONS、DreamOSDataManager.cs、DesktopProfilePaths.cs + meta、DesktopProfilePathsTests.cs + meta。生产仍 `Application.dataPath.Replace(Application.productName + "_Data", "")`，DeleteDataCategory 未先初始化路径 | 审查最小路径补丁、隔离路径测试、Player 验证；README 同步 |
| D8 命名一次提示 | 候选未合、未验 | 有 REPORT/QUESTIONS、XgSetupOverlay.cs；新增 nameNote、下移后续行 20px、中英提示 | 在生产仅合 nameNote 小块；视觉/语言验证；不要覆盖新删台词 |
| Round2 E1 性格设计 | 设计已交、Unity 实现未交 | `性格模块_评估与加强设计.md`；第三轮前制作人答复部分批准 P0 和小毛病边界 | P0 表达修复、设计同步；结局冲突独立等待决策，勿借此停掉全部可独立实现内容 |
| Round2 E2 大脑光效 | 设计与原型已交、Unity 实现未交 | 初稿 `brain_window_prototype.html`；更新稿 `Round3/brain-v2/brain_window_v2.html` 最后写入 20:41:54 | 将已批准方案落实 Unity；原型假数不是游戏验证 |
| Round3 E1 最新存档/退出 | 代码/测试/README 候选已交，未合未验；报告缺失 | ChapterOneRuntime.cs、SaveFailurePropagationTests.cs+meta、WindowsReleaseBuild.cs、NamingChecks.cs、README 增量 | 定向审查、失败注入与路径隔离验证；补调用链/覆盖报告 |
| Round3 E2 贴吧生命周期/刷新 | 代码/检查/6 diff/QUESTIONS 已交，未合未验；REPORT 缺失 | TiebaView.cs、DelayedFloorKey/SideKey partial、两个 Editor Checks、patch-1…6 | 分块审查并合；语义实窗与同口径性能验证 |
| Round3 E3 终局 QA/状态映射 | runner/增量/QUESTIONS 已交，未合未验；REPORT 缺失 | EndingPrivateRemainingUiRunner.cs 只加前台诊断，`验收状态增量_E3_20261019.md` | 补独立分支矩阵/断言映射、实跑、发行材料事实更新；不替换产品结局规则 |

## 可立即接手的小块（建议顺序）

### 第一批：低耦合、意图明确

- **D8**：只加 `nameNote` 字段、构建行、Refresh 中英文；保留生产 homePending 单句。候选相对生产 diff 最后一段恢复中英“先碰一下/Touch a button”，必须排除。
- **E1 构建身份**：停用旧 `WindowsReleaseBuild.Build/VerifyAndBuild`，保留方法名直接抛出明确停用信息；`NamingChecks` 改为检查当前 company/product。制作人第三轮前已明确“停用”，无需再问。候选还把 Unicode 转义写为 char，语义等价但非必需，可不带入。
- **E2 patch-1**：具名 `MarkStale` 订阅，OnDestroy 解订阅 hub 与静态 GameText 事件。纯生命周期修复，不改变渲染规则。
- **E2 patch-2**：StartCoroutine 前检查 `gameObject.activeInHierarchy`。按防御性修复记账，不称已复现门禁故障。
- **E3 runner 诊断**：额外记录 phase、焦点窗口、isFocused、最小化、帧差、稳定时间、gameView behavior。保留 60 秒窗口/3 秒持续前台/真实门禁。搭配现有生产 checks 即可。

### 第二批：已交具体候选，需要边界审查

- **E1 保存传播**：StartOverAfterBankruptcy 在 restartChosen 标记保存失败时留在旧 Sim、撤销标记并返回 false；BeginNewGame 返回首次保存是否成功，外层显示“已开始但未落盘”的真实状态；memory/TestMode 不写 player-profile。
  - 候选同时更改 `SaveDirectoryOverride` 优先级，使实例 override 胜过命令行/Editor 验证根。应单独审查这项是否削弱显式验证隔离，不能因为保存失败修复而直接全收。
  - 新测试宣称“前五项预期基线失败”只是作者预期，尚无执行证据。
  - 测试 GoBankrupt 借现有破产路径，不应回退刚合的月租改动以迁就旧夹具。
- **D7**：Player DreamOS 记录迁至当前 persistentDataPath 下 DreamOS_UserData，验证覆盖保持优先，DeleteDataCategory 初始化路径。不扫描旧路径、不自动搬运旧记录、不额外清空重置数据。RECORDING_PREVIEW 保持与现有录制版游戏档相邻是实现选择，Editor 与模组目录不在本小块扩大范围。
- **E2 patch-3**：把 10 秒时间桶改为真实延时楼层位串，另补 requires flag 与 daily.surf 显示键；实际数据为 40/90/150 三个正延时。保留 0.5 秒轮询，检查晚回复/未读/跨档/无事件变化。
- **E2 patch-4**：Open 用 opening 标志避免 OnEnable 先画旧页；原生图标/任务栏重开仍 OnEnable 绘制。修复旧聊天可能被提前标已读，应按现有“实际看到才清未读”规则审查，属于有可观察差异的 bug 修复，不是新的剧情设定。
- **E2 patch-5**：侧栏 key 包含语言、高亮、全部板块 id/name/nameEn 与顺序，非仅数量缓存。检查 Destroy 延迟、同帧重建、外部失效及真实关闭重开。
- **E2 patch-6**：测量点只编译进 Editor/Development；按当前正式构建选项核对，后续性能采样须 Development Build。它是打包边界变更，不能记为已实测性能收益。
- **E1/E3 文档增量**：定稿“仅当前存档身份”、当前保存/恢复边界；补 Oxygen LGPL 源/许可位置。D7 未合前不得写其已生效；最终 Player 未验保持待验。

## 候选冲突、回退风险与过时记录

### D1 最新内容已纠正 D6，但仍不能整体替换

第三轮分工写“Round2 新 D1 使用 LP2-final-choice”，这是当时快照事实；**当前** `ClaudeReturnRound2/D1/REPORT.md` 和 checks 已回到 LP2-latest-plan/单参数 PlannedFact，与现行规则相同，不再是新设定冲突。

但直接 `git diff --no-index` 确认当前 D1 候选仍有两类生产回退：

1. SendLabel/CancelLabel/Notice/Heading 把 `L(en, zh, en)` 的中英参数调反；生产是已修正顺序。
2. 删除 Frames 的第 5 个 `captureFailure` 回调与 finally 中清理前失败截图；与生产 runner/Round3 E3 runner 五参数调用不兼容，也损失诊断。

所以 D1 当前无需整套合入；新增 `NeedLatestPlanProjection` 如需增强，单独审阅后再取。

### 唯一明确的新产品冲突：性格与 R3

`Round3/制作人答复_20261019_第三轮前.md`：性格主要趣味性、不影响结局，去掉 Round2 E1 F 项。生产 `XgEndingPreparation.cs:247–256` 仍读取 warmth/play/opinion，249 行按 opinion/play 阈值选 online/public_model/local_only。需要主控向用户明确替代选路规则或“仅新增性格玩法不影响结局”的范围。不可静默修改终局，也不能把旧设计 F/C/E 中的 R3 连动原样实现。

### 已有答复，不需重复询问

第三轮前答复已批准：阶段4–6保留话少、让性格体现在用词；小毛病仅噪声数据、每包最多一次、永远保留“就要练”、可给假桌面图标起外号；脑窗属 .exe 暗色风、幕布结束后补播、形态按真实接线、先8种签名、添加 additive shader+128px图集、国际象棋归属需纠正、区域叫“推理区”、脑小窗与壁纸都改侧视、是/否色统一。减少特效开关交实现方判断。

Round2 制作人旧答复里“购买凭证放包内”与当前明确隐私边界冲突，按最新整合规则只核对授权与所需署名/EULA，**不得公开购买凭证/账号**。其中 D5/D6 旧答复也已被后续明确否决。

## 未交付或未完成的确切内容

- Round3 顶层 README、E1/E2/E3 REPORT 缺失；E1 QUESTIONS 缺失。E3 增量反复引用“E3 REPORT §3”，目标不存在。
- E3 没有新 EndingPrivateRemainingUiChecks（可以是合理的“无需改”），但要求的独立 8×2 分支矩阵及逐断言映射没有完整报告；不能把缺文件等同缺产品实现。
- 性格设计仍为旧稿，尚未把第三轮前答复回写去掉 F/R3 关联；无 C# 实现候选。
- 大脑有 v2 HTML，未有 Unity 运行时代码、shader、图集、QA 交付；不得将浏览器演示称游戏已实现。
- D1/赌场剩余实窗、存档/退出 Player、模型实际推理、录音听验、最终字体/中英视觉、包检、发布属于主控剩余执行责任，并非 Claude 代码包缺失。
- 整合记录末尾已明确科普 +5% 单轮不叠加、月租 YY、删除开局台词，42/42 EditMode；本次不重复。现存 2026.10.19.1 包早于这些改动，不能当作本轮最终包。

## 验证口径

本文“已验”仅引用整合记录已有结果，没有本轮新运行结论。“候选未合”由当前文件存在性与对应生产 diff/源码确认。目录和文件随他方继续交付可能更新，整合前需重比具体 hunk；旧 handoff 时间戳/README 概括不优先于当前源码与最新用户决定。

主控补充的本地 Claude 会话核对：`AppData/Local/Claude-3p/local-agent-mode-sessions/a19396f5/00000000/ddf22f9b/audit.jsonl` 对应标题“AI进化游戏”；20:48 服务端 503 中断的是 brain-v2 说明，20:41 HTML 已落盘。20:07 用户最新方向是更快、更神秘、高级多层神经光效，接手脑窗时需以此校准旧设计。19:28 用户已明确正式版不保留测量点、测试版保留，E2 patch-6 无需再询问。R3 性格冲突已由主控异步询问用户。
