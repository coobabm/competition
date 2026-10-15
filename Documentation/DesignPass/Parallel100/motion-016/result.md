# motion-016：数据／道具购买结果反馈核对

日期：2026-10-13（Asia/Singapore）。交付状态：静态审阅完成；一处候选最小修正，尚未整合、编译、运行或视觉验收。

## 结论与范围

仅核对已有数据与道具购买成功／失败反馈，不读取新增策划正文，不宣称《动效策划》第 10 行全局完成。已核对 motion-task-001.md；其通用按钮实现只负责手感，本交付补业务失败结果，不重复替换通用组件。生产源码均只读，未启动 Unity／游戏，未访问玩家存档，未写 Assets、ProjectSettings 或全局清单。

发现一处确定缺口：`XgItemsPage.Buy(Card)` 的非 Buyable 预检查直接播放 Knock／Thud 后返回，绕过 `XgSim.BuyNode` 的 Say(Why(...))。当按钮最后刷新时可买，但实际点击前余额已减少或条件变化时，会有拒绝音效却没有当次失败原因。视图按 refreshTimer 周期刷新，并非每次输入前自动刷新全部卡片，因此这个分支不应只按禁用按钮永远不可达处理。

候选 changes.patch 仅展开此分支，保留原 Knock(.05f, (8,0)) 与 Thud，复用 host.ShowToast(Sim.Why(...), 3.5f)，再 host.Refresh(true)。不调用购买接口、不推算余额、不播放成功粒子、不添加成功文案，也不替换既有金币、冲击波或通用 Label 动效。

补丁目标：`Assets/LingGuangV05/XingGuang/Desktop/XgItemsPage.cs`，原文第 345–350 行。
原文件 SHA256：`8361F777B746D8E0359EF9BB02E2CEFC48E9B0AF0023EF85819DA59E5662239A`。
其余依赖摘要见 source-hashes.json；补丁内也带目标与摘要。应用前须再比对，若不符应按当前源码重新审查，不能盲目覆盖。

## 原文件证据与无需修改的路径

- XgUi.cs 的 Button 安装 XgButtonPressFeedback；该组件只动画 Label。业务对按钮／卡片本体的 Knock 和原粒子仍独立保留。
- XgDataPage.cs:320–343：BuyDataOffer／AccelerateOffer 返回 false 后 Knock＋Thud＋刷新并立即 return；成功后才 Knock(.15)、金币（适用分支）、Coin、刷新。268–299 行从真实 Offer 显示下载、已拥有等状态。无需额外制造“下载完成”提示。
- XgSim.DataSources.cs:534–574：前置条件与扣款失败产生真实 Say；成功在实际登记拥有／下载后返回 true。购买与下载完成不是同一结果。
- XgItemsPage.cs:207–218：BuyConsumable 返回 true 才播放 Coin、卡片 Knock、金币和实际 pack 数量的 +数量飘字；失败仅 Knock＋Thud。XgSim.Consumables.cs:150–160 在库存超限与扣款失败时 Say 原因，成功在 stock 增加后 Say 已购数量。无需重复添加提示。
- XingGuangView.cs:193、257、306–314：订阅 Sim.Message，由 OnMessage 调用 ShowToast；因此上述业务 Say 已有结果层，不是仅日志记录。
- 道具节点成功路径 XgItemsPage.cs:348 → XingGuangView.cs:368 → XgTechTreePage.cs:628–634：只有 Sim.BuyNode 成功才播放强 Knock、Shockwave、Yen Burst。XgSim.cs:257–267 保留失败 Say 与实际解锁／下载。候选不改成功路径。
- 淘货数据入口 TaohuoView.Data.cs:105–117 已根据实际 bool 选择结果，ShopKit.cs:Say 更新六秒状态栏；ShopKit.Btn 保留按压颜色反馈。该入口读取最后一条日志，本次审阅未确认正常现有购买路径出现旧日志冒充当次成功，故不制造额外补丁；它不是本次补丁目标。
- MotionButtonChecks 检查通用输入、动效隔离等基础，不能作为购买结果、下载完成或余额正确性的运行证据。

## 静态检查与待整合

已实际执行 git apply --check（兼容当前换行空白），退出码 0；只检验补丁上下文能匹配，没有应用补丁。手工核对 ShowToast(string,float)、Refresh(bool)、Sim.Why 接口存在。未编译候选 C#，未执行 Unity 测试，未做视觉验收，均不得标 PASS。

整合负责人可采用这一个局部分支补丁；不需要改 XgJuice、XgUi、核心扣款、存档或共用按钮测试。补丁若与其他交付重合，仅保留一次原因提示。

## 待统一验证（全部未运行）

用隔离内存状态／测试宿主，不能使用真实玩家存档：

1. 刷新时买得起一个现有道具，点击前把测试宿主资金降至价格以下；执行实际购买回调。应显示 Sim.Why 当次原因，保留拒绝动效，并立即更新按钮；扣款调用为零、拥有项不增加、无成功金币或成功文案。另测前置条件变化及已经拥有，理由应随真实状态变化。
2. 正常成功道具购买：核对实际扣款一次、实际解锁一次、原强回弹／冲击波／金币仍在；普通点击与 Submit 各测一次，不重复触发业务。
3. 消耗品：库存 MaxStock-pack 能买且恰好达到上限；再多 1 个则拒绝。资金恰好等于价格成功；价格以下超过 1e-9 容差则拒绝。资金报告足够但 Spend 返回 false 时也不得增加库存或播放成功效果。
4. 数据：可买公开包／垃圾包成功后显示真实下载中；未完成时不能显示已下载。前置未满足、重复购买、资金不足、Spend 拒绝均不得新增拥有项；金币成功效果不能在 false 分支触发。加速也只在真实完成后显示完成。
5. 视觉：静音仍能读懂成功／失败；旧 Knock 与 Label 动效共存，失败 toast 不被刷新遮掉，按钮在刷新禁用后无残余位移。减少动效、快速操作、窗口关闭重开和中英文提示由主聊天统一截图验收。

以上只是本任务边界的验收建议，不代表已运行或已满足所有操作的两层反馈。

结束前复核：source-hashes.json 所列 14 个依赖文件 SHA256 均未变化；补丁仍可静态匹配。
