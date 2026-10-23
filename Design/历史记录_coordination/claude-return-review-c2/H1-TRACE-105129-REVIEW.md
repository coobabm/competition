# C2 105129 实测诊断：H1成立，修复仍待运行确认

## 后续110253干净版通过（追加）

已独立读取 `LocalVerification/SidebarLayout/20261019-110253-787/report.json`：passed，65条记录、31图，runtimeErrors=[]、editorInfrastructureErrors=[]。root 确认本次已移除临时 trace/probes，且包含 D2 缩减补丁与字体 setter 修复；因此前一节“干净版待复验”现在已由此新结果取代。范围仍是明确夹具/合成输入的 Editor 实际 LCD UI 验收，不能外推物理输入、自然游玩、模型或最终 Player。

## 后续105613修后结果（追加，旧失败保留）

已独立读取 `LocalVerification/SidebarLayout/20261019-105613-794/report.json`：passed，65条记录、31图，runtimeErrors=0、editorInfrastructureErrors=0；4730为含逐帧守卫的断言调用数。具名记录明确包含native文本焦点、合成按住、真实tray忙碌、安全时间、自然quiet30、跨帧LCD按下/松开、准确浏览器路由，以及life-open/result-pending和优先通知释放。

`claude-c2-phase-correct-input-20261019.log:1730–1731` 首段修后trace有30玩家帧；preNotifierInputBusyFalse=0、playerCacheMismatch=0、playerForeignCurrent=0、unfocusedSamples=0；delivered=0、notHeldSafeGain=0、quiet首次重置后保持。对照旧失败直接支持本次QA时序修复有效。此结果仍是带临时诊断、明确夹具与合成InputSystem状态的Editor验收，不是物理鼠标/自然游玩/最终Player。主控将移除临时trace与probe再复验；本报告未提前把无诊断复验计为通过。

以下为105129原失败诊断及修复审查，原样保留。

来源：`.coordination/integration-20261018/claude-c2-probe-runtime-location-20261019.log` 2090起；生产 `LocalVerification/SidebarLayout/20261019-105129-919/report.json`。本agent只读审查并写此报告，没有修改生产或运行Unity。

## 判断

H1在本次合成输入QA复现中已得到直接证据支持，足以进入最小夹具修复。它不再只是从截图猜测；但没有据此证明真实物理鼠标也有同样故障，也不将产品所有输入/通知路径称为无缺陷。首次开启缓存的具体消费者仍未被追踪，不能仅因Bridge可交互就断言一定是Bridge最先开启；这不影响已观测到的Editor/玩家缓存错配诊断。

| 时刻 | 直接读值与变化 |
|---|---|
| frame2424，Set前，Editor | current/qa=18/18，raw=0、isPressed=false，needsFramePress=true，两套cache均false |
| 同frame Refresh后，Editor | raw=1、isPressed=true、cacheEditor=true；cachePlayer仍false，玩家pressedStep仍uint.MaxValue |
| frame2425，Dynamic input-before及input-after | raw=1、prev=1、cachePlayer=false、isPressed=false；重复Apply没有修正缓存 |
| frame2425，pre-notifier | InputBusy=false，current/qa仍18/18，focus=true、minimized=false、bound/deliverySame=true、CanInteract=true、presentationHeld=false、life=false/false、trayBusy=false |
| 同frame late-update | safe从600增加到600.02510899864137，quiet仍0；证实门禁看到非按住状态并推进调度 |
| frame2446 late-update | count从0变1，safe=600.367407001555，trayBusy=true；真实投递发生 |
| frame2447 late-update | quiet变30且trayBusy=true。此时的重置不能算鼠标按住门禁成功 |
| frame2452 Editor最终 | Editor侧isPressed=true，但玩家缓存仍false；count1、safe增长0.367407001555、quiet30 |

汇总：28玩家帧、28次pre-notifier InputBusy=false、140个玩家阶段raw/cache不一致样本；foreignCurrent=0、unfocusedSamples=0。五种玩家阶段每帧均出现，既有顺序值与声明顺序已记录；head/tail省略中段的完整逐行文本，但汇总保留了所有采样计数。H2设备替换、H3失焦，以及绑定错对象，不是这一段已观测失败的解释。

源码机制与轨迹吻合：现有Refresh在Editor上下文先写Dynamic，使player raw提前变1，但ButtonControl缓存更新读取当前Editor buffer；随后Editor写入将Editor缓存设true。进入Dynamic时player raw已经1，重复写1没有变化，因而player缓存维持false。通知器按isPressed得到false，按其现有综合门禁和计时顺序推进。这是已证明的QA维护时序问题；无依据改产品InputBusy读取规则。

## Claude条件diff及主控已实施变体

建议的必要变动仅在HeldMouseFixture：Refresh只写Editor；玩家onAfterUpdate在当前Dynamic/Fixed阶段写玩家状态；Set设置playerPending，使false状态也能送到下一次玩家更新；held期间继续每次玩家输入后重申QA设备。保留现有Trace钩子、全部严格断言及已迁出的两个探针。

Claude原diff在Dispose里仍写Dynamic+Editor，不宜保留。主控最新源码已采用更小且符合当前所有权的清理：取消onAfterUpdate订阅，清held/playerPending；外层finally随即RemoveDevice并恢复priorMouse，没有yield。此路径不需要再次写QA设备的两套buffer，也避免结束时重新引入错配。本agent已只读看到该实现，未重复生成或应用补丁。原1750基线diff不要直接重套到当前已合源。

## 释放、再次按压及清理审查

- **正常释放**：Set(false)立即更新Editor，playerPending允许下一次Dynamic/Fixed Apply(false)，随后清pending。它不会被原来的if(!held)return提前跳过；这是条件修复必要的一部分。直到该玩家回调前，player缓存可能暂仍true，符合明确的阶段延迟，不能用Editor读值冒充player释放已完成。
- **第一段释放**：后续实际tray生命周期及自然30安全秒需要玩家阶段持续推进；如果QA鼠标卡在true，这段将无法完成，既有验收可发现。诊断只包第一段按住，不能把该trace声称为释放轨迹；修后完整runner仍必须走完。
- **第二次按压**：它发生在真实tray及自然安静期之后，已有许多玩家帧供前次release送达；Set(true)再次置pending且重置PlayerFrames。保留第二段>=3玩家帧、目标始终可点击、mouseup前不路由及mouseup后精确页面检查。一个新真实按下边沿可能使Bridge播一次点击音，不能因此禁用Bridge或改变产品输入。
- **同Editor片段的false→true**：单个bool pending会合并尚未送达的释放。当前runner两段之间有实际等待，不触发这个风险；不为假设用途扩成事件队列。以后若复用fixture做零玩家帧的快速连点，必须另行定义并验证边沿语义。
- **手动LCD松开**：第二段Set(false)之后同步OnPointerUp，玩家buffer释放可能尚未发生。实际LCD路由由PointerEventData/桥接处理，原runner仍断言精确路由，下一玩家阶段负责解除合成按钮状态。不要用直接调用浏览器打开来替代这条验收。
- **失败清理**：取消订阅后立即移除设备、恢复原鼠标，再恢复input modules等。当前Dispose的安全性依赖这个owner约定；若未来独立复用Dispose但保留设备，必须另定释放策略。当前不应在Dispose跨buffer写false。探针自身清理和输入设备清理是两件事，均须保留。

## 当前验收边界

105129依然是failed报告，证明了根因而非修复成功。主控已启动修后 `claude-c2-phase-correct-input-20261019`，结果尚未由本报告核对。先看该次真实读值/完整结果：玩家维护后raw/cache/isPressed一致、InputBusy=true、count0/safe不变/quiet30，再确认tray、自然安静30秒、第二次按住/松开和生活事件优先全部通过。不要仅凭首段成功就宣布公共阅读尾项完成；无需为旧假设再机械重跑三次。
