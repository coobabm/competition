# D1 固定快照审查（尚非完整交付验收）

## 后续更新：完整交付与修补候选已审

Claude 的 REPORT/QUESTIONS/sidebar-optional-hunk 后续到齐并已读，源代码 SHA 未变；本文“尚未收到 REPORT”是初审时状态。新文档没有改变三项 QA 发现。已按 root 授权在 `candidate/` 制作修补、精确补丁和 4 份文件，一次隔离编译 exit 0；详见 `candidate/README.md`。未改生产，未跑 Unity，不接 Sidebar optional。

审查日期：2026-10-19。仅写本目录；未修改生产工程、未启动 Unity、未跑实际 UI。两份源码均已完整阅读。快照取自 `ClaudeReturnRound2/D1`；当时仅收到两份源码、两份 meta 和 QUESTIONS，尚无 REPORT，因此不能判定整包交付完成。原始字节保留在 `snapshot/`。

结论：**暂不进入实际 UI 验收，先修正 QA 的四处中英参数反序。** 两份候选的隔离 Roslyn 编译通过（exit 0，4 个 FindObjectsByType 弃用警告），只证明当前引用下的类型/API 可用，不证明 Unity 导入、夹具运行或 UI 通过。另有两项 runner 证据完整性问题，建议在独立 QA 内修正。

## 可执行发现

### 1. 必修：四个本地化 helper 参数顺序反了

位置：`EndingPrivateRemainingUiChecks.cs:65–68`；调用处 `:254`、`:258`、`:267`、`:393`。

`L(bool en, string zh, string english)` 的第一个文本参数为中文，但 `SendLabel`、`CancelLabel`、`Notice`、`Heading` 均传入英文在前、中文在后。生产 `Assets/LingGuangV05/YY2016/YYChatView.EndingPrivate.cs:44–61` 用的是正确的中文在前。故正常 zh 首个 LP1 预览就会在 :254 的 heading/notice 检查失败；即使绕开该断言，取消和发送查找也会使用错误语言。en 同样反序。

最小修正仅交换这四个 QA helper 的两个文本参数。不要改产品文案、语言系统或降低断言。这是静态确定的问题，尚未以 Unity 运行复现。

### 2. runner 退出清理失败时，passed 文件与实际结果可能相矛盾

位置：`EndingPrivateRemainingUiRunner.cs:157–160,323–329,336–349`。

`Finish` 在退出 PlayMode 前写 `report.status=passed` 和 `passed.txt`；进入 EditMode 后才调用 `RestoreEditor`。若场景/GameView/配置恢复抛错，catch 只改局部 `passed=false` 并写 `cleanup-errors.txt`，既未把 report 改成 failed，也未撤销 passed.txt。读取 report 或 marker 的汇总会误判，而进程退出码为 1。

建议在 EditMode 恢复成功后最终确认通过；或所有晚期清理错误都同步写回 report 并移除本次 marker。至少父级实际验收必须同时检查退出码、cleanup-errors、report 与 marker。这是异常路径的静态发现，不代表当前已发生清理故障。

### 3. 当前失败截图在夹具复原后拍摄，无法保证呈现失败状态

位置：`EndingPrivateRemainingUiRunner.cs:197–198`；Checks 的 finally `:507–531`。

catch 先 `DisposeRoutines`，嵌套 Checks 的 finally 会隐藏 epilogue、关闭 YY/mail、重绑 original，再进入 `CaptureFailure`。因此失败日志虽准确，截图可能已经是关闭/恢复后的桌面，而不是出错的预览/信件。建议先保存失败时可见画面，再按 finally 保证清理；或明确把该图标为清理后截图并补充错误前的控件/状态文本证据。不应把现有截图称为失败瞬间证据。

## 范围、runner 与隔离核对

- 独立 static Editor runner，无新增 Editor MonoBehaviour 动态挂载问题；不改旧 EndingPrivate/EndingSmallA 套件、不改终局产品。当前引用中 `EndingSmallAActualUiChecks.Foreground/Fixture`、`EndingLetterPresenter.Root/Session/Snapshot/Visible` 等均通过编译。
- `BeforeSceneLoad` 在本次 Root 有效时将主保存重定向到 `LocalVerification/EndingPrivateRemaining/<timestamp>/Save`，DreamOS profile 重定向到同次 DesktopProfile；写盘前 GuardIsolated 再校验路径。真实 `SaveNow → AtomicSaveStore.Load` 检查主文件、回执、原话和他人会话；真实 `SaveSlot(6)/LoadArchive(6)` 检查重绑。
- release/night/public-scene 条件用 detached XgEpilogueFlow + 内存 saveNow 构造，Scope 和检查记录明确标为 simulated snapshot。没有直接赋值“私人消息已发送”来冒充按钮发送。
- TestMode 在 bootstrap 阶段用于隔离；每个分支 Bind 后反射恢复 false，再显式暂停 tick、默认关盘。需要写盘时才开 useDiskSave。没有发现把 TestMode=true 的保存返回当作真实写盘的路径。
- 每次点击执行真实 Application.isFocused/非最小化检查和 LCD 命中校验；截图前调用已存在的 3 秒连续前台守卫。StoryFocus 的两个 false 回调是叙事夹具隔离，不是伪造 Application focus。
- 虚拟输入设备、原输入设置、先前 current 设备均有恢复；locale 和 StoryFocus 有清理。保存/profile 覆盖、FPS、VSync、background、ReduceMotionPref、GameView、scene setup 有恢复。这里只核实显式管理项；尚未运行验证整个生命周期，也不据此宣称所有 PlayerPrefs 都被隔离。
- Scope 如实声明无物理 OS 输入、模型推理、自然通关、R3、最终完成结局、Player 或性能结论。

## 可见覆盖矩阵（源码意图，全部 UI 未运行）

| 分支 | 夹具/触发 | 控件与预期收件人 | 信件/持久化 | 旧覆盖关系 |
|---|---|---|---|---|
| LP1 zh/en | stage6 + SI read + pinning online | 老周；计划预览/取消/发送；独立抄录的计划原话 | zhou_open_door 9 段 + 精确 LP1；主文件及 slot6 | 补旧套件 LP4 以外的计划 |
| LP1-mismatch zh/en | 已实际发送 online 计划，随后 public_model 夹具 | 不自动发送 route | zhou_copies 无附段 | 新增错配负例 |
| LP2 zh/en | asked 无 route；先 online 后 public_model | 老周；两次实际发送，public_model 含取消 | zhou_copies 9 段 + LP2；主文件及 slot6 | 补旧套件计划分支 |
| LP2-latest-plan zh/en | 上述两次发送后 online 夹具 | 最后发送的 public_model 计划仍占优 | zhou_open_door 无 LP1 | 新增最新计划规则负例 |
| cross-save zh/en | A 已开预览，绑定 B | 旧按钮仍可命中才实点；否则记录不可点；另有明确标注的 synthetic stale preview 调用 | A/B 均无新增消息/回执 | 跨档回调负例 |
| R1-before-night zh/en | online 已 commit，无 night receipt | Hub 无 offer；这一项不是可见按钮点击验收 | 不建立私人回执 | 新增阶段门槛 |
| R1 zh/en | online 玩家决定 + completed/power_cut；no_stop/stop | route 实际预览/取消/发送，随后独立 shutdown 预览/取消/发送；只给老周 | 主文件及 slot6；zhou_open_door 无附段 | 补旧套件 R3 以外路线 |
| R2 zh/en | public_model 玩家决定；unpinned/no_stop | 同上，只给老周 | 主文件及 slot6；zhou_copies 无附段 | 补旧套件 R3 以外路线 |

所有发送检查其他 YY/Tieba 会话不变、老周收到原话、其他人物 prompt 不含该原话。动态 AI 名是合法的“小砚”/“Quill”；断言固定私人文案不擅自插入这些名字，并非对名字插槽的替换功能验收。

重复点击是条件性覆盖：同帧 send 仍可命中时才第二次实点；否则报告“重复不可能”。stale choice 检查旧按钮被销毁，并未强行直接调用其回调。这些源码记录是诚实的；最终报告不要概括为所有情况下都执行了第二次点击。

R1/R2 replies 和 shutdown reply 的预期值来自生产 `EndingPrivateDisclosure.RouteReplies/ShutdownReply`，信件预期来自生产资源：可验证 UI/回执/磁盘传输一致性，不能独立发现源文案本身被改错。计划原话则在 QA 单独抄录。最终说明应区分这两种 oracle。

QUESTIONS 的“最后实际发送计划优先”与生产 PlannedFact 逆序扫描一致，无须发起设定变更；未发现必须询问用户的冲突。Planned 夹具未像 EndingSmallA 清除全部开场字段，当前没有证据证明这会阻塞本套件，因此只列为运行时待观察，不定为产品/夹具 bug。

## 验证与下一步

1. 交换四个 helper 的 zh/en 参数；按以上建议完善 runner 证据输出。
2. 收齐 Claude REPORT，核对基线 SHA、完整分支矩阵、未执行声明，并确认最终返回源码是否继续变化。
3. 父级串行 Unity owner 再做导入/编译与独立实际 UI；需要全部 16 个 zh/en namedResults、运行错误 0、清理错误 0、截图可读、真实隔离盘证据、最终退出码成功。不能用断言调用次数当功能数。
4. 旧套件无需为了此静态审查重复跑；若运行暴露产品问题，另交证据和最小补丁，不放宽本检查。

固定源码 SHA256：

- Checks: `3855829bb32ff22d5be2609267371c18e2305dae7f7724ea5c695f95ef65a7c6`
- Runner: `ae807ee648236637c0dbf184331b6770730b6d85a9e404f33644d44fbd5e14ec`

编译命令及结果：`verify.py`、`verification/isolated.rsp`、`verification/compile.log`、`verification/result.json`。未输出或应用产品补丁。
