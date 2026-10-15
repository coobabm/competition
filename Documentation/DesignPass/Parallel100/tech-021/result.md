# tech-021：label.coop（置信度分流）

日期：2026-10-13（Asia/Singapore）。仅核对本节点；生产源码只读，补丁未应用。

## 结论

现有正式名称和通俗解释正确，保留。原效果「按把握阈值分流，待复核最多20题，队列满时自动暂停」与代码相符，但遗漏阈值单位与边界、购买后生效时机、全局共享容量，以及此开关同时启用的自动错标噪声记录。交付 changes.patch，仅替换 XgTechTreeCopy.Effect 的 label.coop 一个分支，不改模拟数值、购买路径、其他词条或可见性。

已核对 Documentation/DesignPass/Agents/skilltree-task-001.md：已有实现完成通用文案层，本交付仅补足本节点的实际边界；不重复认领其编译或测试。检索只命中当前分工记录，未读取任何新的策划正文。未创建聊天或子 agent，未向其他聊天发消息。

## 可直接采用的中英文文案

正式名称（不改）：置信度分流 / Confidence-based routing

通俗解释（不改）：有把握的题自动交，拿不准的题留给你复核。

Submit confident answers automatically and send uncertain ones to you for review.

效果（补丁实际文本，20由 ReviewCapacity 动态读取）：

购买后即启用，无需装备或再训练；从下一次自动标注分流起生效。全局把握阈值可设为 50%–99%（初始 80%）：低于阈值交给你复核，达到阈值可自动提交，仍受其他已启用检查影响；把握不是准确率。所有标注台共用 20 个复核名额；满时暂停自动标注，腾出空位且其他运行条件满足后继续。只购一次，不叠加速度或报酬倍率；漏过检查的自动错标会累积噪声、减少有效训练样本。

Enabled on purchase, with no equip step or retraining; applies from the next automatic routing decision. Set a global confidence threshold of 50%–99% (initially 80%): lower confidence goes to review; at or above it, answers may be submitted automatically, subject to other enabled checks. Confidence is not accuracy. All desks share 20 review slots; a full inbox pauses automatic labelling, which resumes when space and other operating conditions allow. Bought once, with no added speed or pay multiplier; uncaught automatic mistakes accumulate noise and reduce effective training samples.

## 生命周期、单位与叠加方式：源码证据

以下路径均相对于 Assets/LingGuangV05/XingGuang/，行号对应交付快照。

- Core/XgCatalog.cs:462 将 XgSim.LabelNodes 加入目录；Core/XgSim.Collaboration.cs:32、41 定义 label.coop 为 Label 类、label 树、前置 label.auto、费用300、最高1级。它不是模型架构。Core/XgCatalog.cs:356 排除 label 树的阶段倍率；Core/XgSim.Progression.cs:132–137 对 label 直接取 LabelNodeCost，因此实际常规价格 ¥300，无阶段/剧情研究折扣。本补丁保持价格由既有UI显示。
- Core/XgSim.Collaboration.cs:146–163：自动标注至少1级才可购买；没钱仍不能买；已拥有就是满级。:179–191 成功扣款后加入 S.unlocked；:20 CollaborationEnabled 直接读取 Has("label.coop")。仅解锁购买条件不启用效果；买到即启用，不需要装备或训练。没有重复购买、独立等级加成、按桌叠加。
- :213–217 EligibleDesk 要求该桌已开放、该桌检查点准确率达到 AutoMinAccuracy（Core/XgCatalog.cs:671 为 .6）、且是有效标注台。:217–220 CollaborationRate 仍受算力、平台阻断、项目、ReviewFull 和 AutomationHeld 限制，速率仍按原有自动等级及接线系数计算。本节点不提高自动正确率、速度或报酬倍率；其收益来自改变处理去向。
- Core/XgCollaborationModels.cs:47 初始 coopThreshold=.8；Core/XgSim.Collaboration.cs:105 有限输入钳制到 .5–.99，非有限输入忽略。:66 为载入修复：非有限或低于.5回到.8，高于.99钳制到.99。购买不会重置已存在的合法阈值，所以文案写「初始80%」而非「购买时必定80%」。Desktop/XgLabelPage.cs:939–943 滑条50–99、wholeNumbers=true，再除100，UI每步1个百分点；内部状态是0–1的比例。
- Core/XgSim.Collaboration.cs:374–409：Route 用严格小于比较，.7999在.8阈值下进入人工队列；恰好.8不因阈值被转人工。已启用的题目识别规则仍可能优先转人工；达到阈值后自动错题还可能被已启用的检查拦下。故写「可自动提交」，不写「达到阈值必提交」。调阈值从下一次 Route 生效，包括尚未分流的预取题，不重分流已经在 S.queue 的题。
- :10、23、142、366–384：ReviewCapacity=20、ReviewFull=S.queue.Count>=20，所有标注台共用一条队列；20是题数，不是每台20、20%、预取数或并发数。入队会清除计时奖励状态与计时；不是无限期保留限时金题奖励。
- :424–429 人工答题从队列移除题目，答错也释放名额；:452–472 自动循环每次重新检查队列与运行条件，空出名额不需要再次购买或按恢复按钮。:217、278、293 的速率/推理派发同样检查 ReviewFull。本节点的暂停指自动标注及关联判断流程，不是暂停游戏或训练；手动/蜘蛛代按不是全部都受该分流路径控制。
- :343–349 允许复核题提供既有判断及置信度；Desktop/XgLabelPage.cs:959–976 购买后在普通标注模式显示阈值、近100题统计、噪声总数及共享待复核数量。:1126–1127 自动记录面板同受 CollaborationEnabled 控制；Core/XgSim.Features.cs:37 的 coop 可见性直接对应本开关。
- :386–409 自动错题未被拦回复核且未被平台抽检发现时，每题 Noise(dataset)+1；未购买时这条噪声记账不执行。高置信度不等于保证正确。:11、108 的有效样本公式为 max(1, Samples − 2 × (Noise + DataNoise))：单位为有效样本计数，一条新噪声在钳制前扣2，最低1，不是准确率直接扣2个百分点，也不是把实际收集样本数扣2；已有噪声与外部数据噪声相加计算。
- Core/XgSim.Spider.cs:73–96：已有蜘蛛代答流程同样在 CollaborationEnabled 时为未被平台发现的错误加1噪声。该流程本身不比较 coopThreshold；不能声称本节点会把所有来源的代答统一按置信度送审。补丁限定「自动标注分流」，且不提前介绍其他系统。
- Core/XgSim.Inbreeding.cs:30–39 现有噪声占比超过10%后另按 .5×超额占比计算上限惩罚；这不是购买时直接扣分或独立购买倍率。本节点只开启上述错误的噪声来源，结果仍取决于实际错误。:73–97 已有旧数据复核入口按队列空位和最多10条限制；不是本节点额外赠送20条样本。这里只记录关系，不扩展其他词条文案。
- :424–449 人工复核按既有人工结算，正确才获得报酬及样本；其他已拥有奖励由其自己的开关决定。label.coop 不赠送其他节点的奖励倍率，不增加新训练样本上限，不要求重新训练现有检查点后才分流。

## 神秘性

Core/XgTechTreeCopy.cs:15、51 在 NodeMystery/图鉴情况下先回退；Core/XgTechTree.cs:127–130 继续使用 NodeVisible。Core/XgSim.cs:250 的神秘占位专用于未发现的 label.auto；Core/XgSim.Progression.cs:144 当前 label.coop 自身可见但受购买条件锁定。不能报告它已有独立未知占位。本补丁不改这些边界，也不在效果里写未发现前置/后续节点的名称、解锁路线或奖励数字。Desktop/XgTechTreePage.cs:758–764 的神秘提示早退保留。

## 补丁身份与整合

目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs

源文件 SHA256：308fe4cd23d4410107d15670ff04520effc6cf59ab44b6370837053ecddadfe8

changes.patch 为带前后各3行上下文的 unified diff，替换原第190行，只含文案。source-sha256.json 记录13个核对依赖文件身份。请整合负责人应用前再次检查 SHA256；若别的任务已编辑共享文案文件，应按 Effect 方法的 label.coop 分支做局部合并，不整体覆盖文件。

## 验证范围与待验证项

本任务仅做静态源码追踪及补丁匹配检查。没有编译C#、执行模拟、启动Unity/游戏或视觉验收，没有读取或修改玩家存档；不得标为运行PASS。原整合记录的验证结果不算本任务执行结果。

待整合：应用本节点文案补丁。名称及通俗解释无需改动，XgCatalog/XgSim/页面无需本任务修改。

由主聊天在隔离内存/场景执行以下真实边界验证（本任务未执行）：

1. 自动等级0时不可购买；1级时299元买失败、300元买成功；成功仅扣一次钱，重复购买失败。购买后不装备、不再训练，下一题即读取阈值；无合格台或算力不足仍不能运行。
2. 固定阈值80%，用79.99%、80%、80.01%三题区分严格边界；禁用其他拦截规则时分别人工、自动、自动。启用已拥有的识别/检查规则后不应承诺后两题必提交。
3. 各台共用19题队列，低置信度新题入队达20；其他台分流也停止且不超20。人工答对或答错释放一格，运行条件满足时下次循环继续；仍有阻断时不得恢复。已排队题不因降低阈值自动离队。
4. 初始80%；滑条50与99端点、1个百分点步长；加载合法旧阈值保留。NaN输入忽略，低于范围的设置钳制，与载入修复回到80%的行为区分验证。
5. 用相同自动错题对比未拥有/拥有节点、被平台发现/未发现、被检查转人工/实际提交：只有代码对应的未拦截路径累计噪声。有效样本已达最低1时不可继续跌破；蜘蛛代答另测其不使用该阈值，避免误将UI说明当成通用路由承诺。
6. 中英文均显示新效果，未发现自动节点仍隐藏名称/价格/效果，未发现图鉴保持占位；已购买节点仍可显示完整效果。长文本在最窄窗口和缩放情况下不裁切。统一编译、真实场景截图与视觉验收由主聊天负责。

不将全局原则、整份策划或其他科技词条标为完成。
结束前静态复核：13/13个源码依赖SHA256未变化；git apply --check退出码0，补丁可匹配当前文件但未应用；补丁范围检查确认为label.coop效果分支1行替换。详情见verification.json。
