# tech-011 · irnn（单位矩阵初始化）

结论：需要一处效果文案修正；正式名称和通俗解释可保留。本任务仅交付补丁，未改生产源码、模拟数值、全局清单或玩家存档，未启动 Unity/游戏。

## 交付与范围

- changes.patch：仅替换 XgTechTreeCopy.Effect 中 irnn 的中英文文案，保留前后上下文。记忆比例读取真实常量 XgBoard.IdentityKeep，避免写成过时注释中的 97%。
- 补丁目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs，第 171 行。
- 目标原文件 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。
- dependencies.json：9 个实际依赖源码文件的基线 SHA256。结束复核均未变化。
- 已核对进度.md 与队列.md 中当前科技树首项的任务记录，未发现 irnn 的既有专项交付；专属目录此前无产物。本结论仅对应已授权的三项文案契约，不代表其他策划条目或整份文档完成。

## 可直接采用的中英文文案

正式名称：
- 中文：单位矩阵初始化
- English: Identity matrix initialization

通俗解释（现文保留，解释初始化思想，不声称代码真实重建了循环矩阵）：
- 中文：让循环网络初始时尽量原样传递记忆，少丢掉前面的字。
- English: Start a recurrent network with wiring that passes memory onward with less loss.

真实效果（补丁输出，当前常量为 96%）：
- 中文：购买后无需装备，仅在模型采用普通 RNN＋ReLU 时，于下一次训练或评估生效。每跨过一个后续字，记忆信号由保留 75% 改为保留 96%：替换原比例，逐字相乘，不重复叠加购买效果。梯度裁剪是购买前置，自动配置会开启；若未开启，同配置下失稳步长阈值再减半。准确率仍取决于学到的内容与题目。
- English: No equipment step is needed after purchase. Applies at the next training or evaluation only with a plain RNN and ReLU. For each subsequent word, memory signal retention changes from 75% to 96%, replacing the old rate and multiplying once per word; purchasing adds no repeatable bonus. Gradient clipping is a purchase prerequisite and automatic configuration enables it. Without clipping, the unstable-step threshold is halved again with all other settings equal. Accuracy still depends on learned content and the question.

## 原文件证据与生效合同

下列源码路径均以工程的 Assets/LingGuangV05/XingGuang/ 为基准；只以执行代码为准。

| 来源 | 证据及结论 |
| --- | --- |
| Core/XgCatalog.cs:414、623–625 | irnn 是研究项 ManualPay，阶段 3，直接前置 gradclip；不是独立可选架构。原效果中的“梯度裁剪需另行解锁”会误导购买顺序。 |
| Core/XgCatalog.cs:348–370；Core/XgSim.Progression.cs:131–148；Core/XgSim.cs:194–205 | 可见不等于拥有；阶段门槛、前置与资金均须满足。当前实际费用为 ceil(round(1200×1.4)×0.25)=¥420 游戏经费，并非目录研究条目的基础值 ¥1200。价格由现有 UI 动态显示，不在效果中重复硬编码。 |
| Core/XgSim.cs:188、252–275 | 成功购买将 id 写入 unlocked；已拥有时不能重复购买，随后调用 AutoConfigureAll。不存在 irnn 装备槽或等级叠加。 |
| Core/XgSim.Board.cs:62–81、94–115 | rnn 映射 Recurrent；identityInit 每次按 Has("irnn") 构建；有效激活函数为 ReLU 才满足条件。ReLU 本身不是 irnn 购买前置，因此“已买但尚无 ReLU”是真实边界。 |
| Core/XgSim.AutoModel.cs:198–239 | AutoTechniques 自动选已拥有的最佳激活函数，并设 run.clip=ClipOwned；自动配置在轮次间进行，epochActive 时跳过。不能要求玩家执行当前 UI 不存在的装备流程，也不能承诺购买一定切换成 RNN。 |
| Core/XgBoard.cs:185–195、491 | IdentityLoop 必须同时满足已拥有、Recurrent、Relu。SequenceDecay 从 0.75 替换为 IdentityKeep=0.96，不是 0.75×0.96，也不是“准确率+21%”。Sigmoid、阶跃、门控及其他架构不会得到本项效果。注释第 187 行写 0.97，已过时，本任务不改该注释。 |
| Core/XgBoard.cs:263–277、312–316 | fromEnd=序列长度−1−当前位置，信号强度=pow(SequenceDecay, fromEnd)。单位是每个后续序列位置/字，非秒、轮次或固定最大字数。多步相乘，仍衰减。没有“记忆完全不漏”或硬编码“只能记住 N 字”的保证。 |
| Core/XgBoard.cs:130–139、535–553 | 同一模型配置中，循环且无裁剪先乘 0.75；IdentityLoop 且无裁剪再乘 0.5。失稳判断是 lr×abs(error)>TearAt，严格大于；阈值减半不等于崩溃概率翻倍或准确率减半。 |
| Core/XgSim.Board.cs:211–224、247–248；Core/XgBoard.cs:516–529 | 训练与评估按当前 Knobs 使用该比例；无需先完成额外训练来“激活”购买。已有显示成绩需等评估刷新；训练学到的权重及题目仍决定成绩。不是购买时重置全部模型权重。 |
| Core/XgTechTreeCopy.cs:15、51；Core/XgTechTree.cs:127–130；Desktop/XgTechTreePage.cs:428、772 | 名称/说明保留原 mystery 分支；科技树显示继续由 NodeVisible 控制。补丁只改已显示节点效果，不提前展示未来阶段的名称、条件或收益。 |

## 核查状态

- 静态检查：已沿目录、购买、自动配置、训练/评估、信号计算及显示调用链人工核对。
- 补丁检查：已执行 git apply --check --ignore-space-change，未报告错误；没有实际应用补丁。
- 结束复核：9 个依赖文件 SHA256 均与 dependencies.json 一致。
- 实际运行：未运行 C#、Unity 或游戏；不声明运行测试 PASS。
- 视觉验收：未进行。中英文效果比旧文长，整合后须检查详情面板换行、滚动和裁切。

## 待整合与待验证

整合负责人只采用 changes.patch 中 irnn 的单行变更；如源文件 SHA256 已变化，先对照上下文与常量重新核对，勿覆盖其他聊天修改。

供统一验证使用的真实边界（均为待运行，不是已通过）：
1. 阶段未到且未拥有时，节点仍不可见；阶段到但未买 gradclip 时不能买 irnn；只解锁可见状态不改变记忆。
2. 已有 RNN 与 gradclip、尚无 ReLU 时，购买 irnn 不产生 96% 保留效果；采用 ReLU 后才启用。换用 Sigmoid/阶跃或其他架构，不能残留该效果。
3. 同一固定卡片中末字信号应为 1；距末尾一字为 0.96，十字为约 0.664833（原普通循环约 0.056314）；应能检出“固定字数记忆”“叠乘 0.75”“误用 0.97”等错误。
4. 无裁剪且其他配置相同，irnn 的 TearAt 应为普通 ReLU RNN 的一半；等于阈值不触发，略大于才触发。开启裁剪后不应再应用 irnn 的额外减半。该项通过构造隔离状态验证，不要求产品提供手动关闭 UI。
5. 购买后无需装备；下一次实际 Board 训练/预测/评估读取当前状态，自动配置不要在活动轮次中改变形状。购买不承诺自动模型必为 RNN，也不保证分数上涨。
6. 中英文节点标题、详情、购买前置提示和隐藏节点均检查；文本足够长时仍可完整阅读。
