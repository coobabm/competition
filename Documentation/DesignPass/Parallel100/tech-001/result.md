# tech-001：RNN 文案核对

日期：2026-10-13（Asia/Singapore）。范围仅 rnn，交付未应用生产。

## 结论

正式名称正确；现有通俗解释“边读边记，只记得最近几个字。”可保留，是近似说明，不能展开成“固定只记 8 字”。当前 XgTechTreeCopy 的 Arch 效果是所有架构共用的“加入可用结构……”模板，没有 RNN 独有的实际效果，也未说明采用时机。提交一处精确补充：仅在通用 Arch 分支前增加 rnn 专属效果返回，不更改名称、解释、模拟数值或其他节点。

已核对已有记录 Documentation/DesignPass/Agents/skilltree-task-001.md：其通用文案层已经完成，本交付只填补 RNN 专属效果。既有记录中的编译/测试结论属于整合聊天，不算本任务执行结果。本轮未打开新的策划正文；初次文件检索曾意外命中逐条清单的搜索片段，未据此扩展任务或继续读取清单。

## 可直接采用的中英文文案

名称：循环神经网络（RNN） / Recurrent neural network (RNN)

解释（保持现状）：边读边记，只记得最近几个字。 / Remembers as it reads, but only the last few words.

效果（补丁文本）：

解锁后成为序列训练线的可选结构，无需手动装备；系统在训练轮次之间按数据和显存决定是否采用。采用后，前文信号每隔一个序列位置的基础保留率为 75%，逐步相乘：隔 2 步约剩 56.25%；这是信号强度，不是准确率，也不是固定记忆字数。已生效的技术可能改变此比例；拥有其他结构不会叠加这项效果。继续训练并查看评估成绩，购买不保证立即提分。

Once unlocked, RNN becomes an option for the sequence track; no manual equip step is needed. Between training rounds, the system decides whether to use it based on data and VRAM. While active, an earlier signal retains a base 75% per sequence position, multiplied at each step: 56.25% remains after 2 steps. This is signal strength, not accuracy or a fixed memory length. Active techniques may change this rate; owning other architectures does not stack this effect. Continue training and check evaluation scores; buying it does not guarantee an immediate improvement.

文案不列出未发现的后续技术名称、解锁路线、效果或价格。Name/Note 前置 NodeMystery 与图鉴回退保持不变；不改变 NodeVisible/EntryVisible。RNN 本身无专属神秘占位逻辑，仍沿用阶段可见性。

## 生命周期与证据

以下均为静态源码核对；路径以工程根目录为基准。

- Core/XgCatalog.cs:158–159：rnn 属 Sequence，span=8、maxDepth=2、paramFactor=2、speed=1、instability=.35。:401 的节点为阶段 2、父节点 mlp、原始价 3000；:348、:356 阶段倍率 1.6，再经 XgSim.Progression.cs:132–137 的 .25 与向上取整，当前常规购买价为 ¥1200。目录架构 cost=0 不是节点免费；价格保留由现有 UI 动态显示，补丁不硬编码。
- Core/XgSim.cs:194–205、252–279：已拥有不能重复购买；阶段可见、前置、资金通过才购买；加入 unlocked 后立即尝试 AutoConfigureAll 并 Evaluate。仅变为可购买状态不生效；其他授权/迁移路径若已 Grant rnn 也纳入可选集合，不要求再付款。
- Core/XgSim.AutoModel.cs:166–183、219–247：只在已拥有、适合该训练线的架构中选择，按数据、显存等条件评分；epochActive 时跳过配置。购买不强制装备 RNN，不保证胜过其他已拥有结构；无多架构效果相加。
- Core/XgSim.AutoModel.cs:251–260：真正切换架构时通常重置该区域学习，并重置超过随机基线的进度；已有迁移技术时数值进度保留 60%，概念另按 CarryConcepts 处理。旧版本首次自动接管有 adopt 特例，不应承诺所有购买均清零。此处只解释 RNN 的采用行为，不交付其他词条补丁。
- Core/XgSim.cs:517–527、639–665：购买后的 Evaluate 刷新上限和显示；继续训练后 StepAccuracy 才推进成绩。训练依然要求样本和资源等条件。无需另行装备，已采用 RNN 时相关信号处理立即按当前参数执行，成绩不保证马上上涨。

## 数值、单位与叠加边界

- Core/XgSim.Board.cs:62–68、94–115 将 run.arch=rnn 映射为 Recurrent；Core/XgBoard.cs:185–199、267–277、312–316 按序列特征位置距末尾的步数计算信号强度 0.75^d。d=0 是 100%，d=1 是 75%，d=2 是 56.25%，不是每步扣 25 个百分点。单位是序列位置/特征位置，不能统一冒称中文字符或英文单词数。第八步仍有信号，故没有“第九字全部遗忘”的截断。
- 例外：IdentityLoop 需 identityInit 且 ReLU 且 Recurrent；实际 IdentityKeep 常量位于 Core/XgBoard.cs:491，为 .96（96%），替代 .75，绝非 .75×.96。:187 注释写 .97，与常量不符；本任务以执行代码为准，不修改该注释、不扩展 irnn 词条。玩家文案只提示已生效技术可改变比例，避免提前泄露名称。
- span=8 用于 Core/XgSim.AutoModel.cs:334–348 的结构瓶颈项，与数据 needSpan 比较；不等于真实硬记忆容量。maxDepth=2 是基础结构层数限制，XgSim.cs:406–409 还会计入已拥有技术的 +6；仍受已买深度上限和显存约束，不写“固定最多 2 层”。paramFactor=2 是参数公式系数；instability=.35 是风险计算输入，不是 35% 固定失败概率。
- Core/XgSim.Progression.cs:228–245 的单题 CardAccuracy 对正 distance 另乘 .9^distance。长句挑战中的 distance 来自干扰句数量，不是上述序列位置；这是答题概率修正，不能拿 .9 替代 .75，也不能声称每步准确率减 25%。
- Core/XgSim.Progression.cs:261 的 longtext/crosssentence ×.9^10 位于 ProgressionAccuracy；调用点 XgSim.cs:541 属 LegacyEvaluate，当前 Evaluate 不调用它，故不写成当前训练显示准确率必乘此值。
- Core/XgSim.Board.cs:185–199、Core/XgSim.Stages.cs:56–57：只有 parallel 数据集且串行结构时，每轮原始训练卡量乘 .25，随后取整并限制 4 至 MaxCardsPerEpoch；不能描述成 RNN 对所有任务都“四倍慢”。与其他处理量乘数相乘，不与另一个闲置架构叠加。
- Core/XgSim.Progression.cs:270、Desktop/XgTrainPage.cs:641：RNN 的模拟 GPU 展示利用率为 31%，页面明确是教学示意。它不是实测硬件利用率，也未被 CardsPerEpoch 当作通用吞吐倍率。本补丁不将其包装成硬件性能承诺。

## 补丁与源文件身份

目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs

源文件 SHA256：308fe4cd23d4410107d15670ff04520effc6cf59ab44b6370837053ecddadfe8

changes.patch 是带三行上下文的 unified diff，只增加 RNN 效果分支。source-sha256.json 记录所有核对依赖身份。整合负责人在应用前核验哈希；若其他任务已改同文件，按方法和通用 Arch 分支定位合并，不整体覆盖文件。

## 验证与待整合

本任务只执行静态核对和补丁结构检查；没有编译 C#、运行模拟、启动 Unity/游戏或视觉验收，没有访问玩家存档。不得记为运行 PASS。

待整合：应用 changes.patch（两行新增）；其他源文件无需本任务修改。

待统一验证：

1. 中英文 RNN 提示显示专属效果；其他 Arch 文案保持原样；未来阶段与神秘节点仍走既有可见性/占位流程。
2. 购买时正处训练轮次内，当前架构不被中途替换；轮次结束后仅在自动选择 RNN 时采用。准备另一个架构胜出的状态，确认购买不等于强制装备。
3. 在隔离内存状态构造相同序列线索置于末尾/前一格/前两格，检查贡献比例 1/.75/.5625；第八步不归零；存在上述有效技术组合时按 .96 替换。检查信号强度与最终答题正确率不会被混同。
4. parallel 数据集验证低算力下最小卡数钳制，避免将 .25 宣称为最终卡数精确四分之一；普通数据集无此因子。
5. Unity 统一编译与中英文提示截图：新增效果较长，重点检查窄窗口、段落换行和面板裁切。尚未视觉验收。

不标记策划全局原则、整份文档或其他科技词条完成。

结束前复核：交付快照内 12 个依赖文件 SHA256 均未变化。补丁仅两行新增，未移除已有分支；验证记录见 verification.json。

补丁可应用性检查：首轮因源文件 CRLF 与生成补丁 LF 上下文不一致失败；保留原行尾后重新执行 git apply --check，退出码 0。只检查未应用，最终依赖哈希仍未变化；这不代表编译、运行或视觉验收。
