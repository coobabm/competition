# tech-007 · TextCNN 文案核对

结论：当前 `XgTechTreeCopy` 对 textcnn 的名称、通俗解释和效果表述与生产实现相符，无需修改生产源码，不提交 changes.patch。下面提供可直接采用的中英文详版，供整合负责人按版面需要选用；不是必须修复的问题。

范围：仅《技能树词条策划》已授权第 3 行对应的 textcnn。只读生产源码与已有任务记录；未读取新的策划正文，未修改模拟数值，未启动 Unity/游戏、访问玩家存档、提交或联系其他聊天。查阅既有队列的相关命中，确认该文档保留原队列、不重复实施；开始时本任务专属目录无既有交付。未将整份文档或全局原则标为完成。

## 可直接采用的三段文案

### 中文

正式名称：文本卷积网络（TextCNN）

通俗解释：像拿一个小窗口沿句子滑动，用同一套办法识别相邻词语的组合；局部线索能一起处理，相隔很远的线索仍受限制。

真实效果：第四阶段、拥有 RNN 后可购买。购买后加入序列训练线的可用架构，由系统按当前数据、模型上限和显存自动选用，无需手动装备。选用时，每轮训练处理量公式中的架构倍率为 ×1.3，与优化器及其他训练倍率相乘；其他已购买架构的速度倍率不叠加。实际每轮处理的卡数还受模型大小、取整和 4–320 张范围限制。购买本身不保证提分，训练结算后查看成绩；切换架构可能降低当前学习进度。

### English

Formal name: Text Convolutional Network (TextCNN)

Plain explanation: Slide a small window along a sentence and reuse the same rules to recognise groups of neighbouring words. Local clues can be processed together, while distant connections remain limited.

Actual effect: Available to buy from stage 4 after RNN. Buying adds an architecture to the sequence track; the system selects it automatically according to the current data, model limits and available VRAM. No manual equipping is needed. When selected, its architecture multiplier in the cards-per-epoch formula is ×1.3. This multiplies with the optimizer and other training factors, but speed multipliers from other owned architectures do not stack. Actual cards per epoch also depend on model size, rounding and the 4–320-card bounds. Buying does not guarantee a higher score; check the score after training settles. Switching architectures may reduce current learning progress.

以上数字描述正常在线训练；离线模拟直接使用固定 12 张/轮，见下表。价格应继续显示由 `NodeCost` 生成的现有动态价格，不另行写死到词条正文。未知阶段不要提前显示详版。

## 现有文案为何无需改动

- `Core/XgTechTreeCopy.cs:24`：名称为“文本卷积网络 / Text convolutional network”，是正确专业名称；加 TextCNN 缩写属于可选补充，不是错误修复。
- `Core/XgTechTreeCopy.cs:73`：架构解释读取 `XgCatalog.Arch(node.target).note/noteEn`。`Core/XgCatalog.cs:168–169` 为“用看图的办法读句子，快，但看不远。/ Reads sentences like pictures. Fast, but short-sighted.”。实际使用局部共享连线，并有速度与跨度参数支持该简述。
- `Core/XgTechTreeCopy.cs` 的 `Effect` 架构分支：“加入可用结构，系统按数据和显存选择合适的模型；训练后再看评估成绩。”该分支描述的是所有架构共用的实际购买作用，未承诺购买必装备、立即提分或固定提升幅度，因此正确。
- `Core/XgTechTree.cs` 的 `Name` 与 `Desktop/XgTechTreePage.cs:772` 已分别接入名称和说明，无须为本节点重复接线。

## 生效条件、单位与叠加证据

下列路径均相对 `Assets/LingGuangV05/XingGuang/`；行号是本次快照中的导航提示，以方法和哈希为准。

| 项目 | 实际实现与文案边界 | 源码证据 |
| --- | --- | --- |
| 节点、购买 | `textcnn`，序列线，第 4 阶段，父节点 `rnn`，一次购买，非等级堆叠。阶段门槛、前置和经费同时满足才可买。 | `Core/XgCatalog.cs:300` ArchNode；`:422` A("textcnn", 4, "sequence", "rnn", 2500)；`Core/XgSim.cs:194–205` Status |
| 价格单位 | 实际现价为 `ceil(round(2500 × 0.7) × 0.25) = ¥438`。架构定义 `cost=100` 不是科技节点现价，2500 也不是最终收费。 | `Core/XgCatalog.cs:348–356` StagePrice/Add；`Core/XgSim.Progression.cs:131–137` NodeCost |
| 购买后 | `S.unlocked.Add(n.id)` 后调用 `AutoConfigureAll`。只使其成为候选，不保证系统选中。 | `Core/XgSim.cs:252–279` BuyNode |
| 自动选用 | 仅从符合训练线且已拥有的架构中选择；依次比较预测等级、参数量、预测成绩和 bias。每条训练线只有一个 `run.arch`。 | `Core/XgSim.Progression.cs:20` ArchitectureFits；`Core/XgSim.AutoModel.cs:165–184` BestAutoModel |
| 轮次中购买 | `AutoConfigure` 遇到 `epochActive` 返回；轮次外才改配置。但结算路径先清除活动标志，再在 `TrainEpoch` 内配置，因此正在进行的一轮可能在结算时采用新架构。不要承诺“绝不影响当前轮”或“一定从下一轮开始”。 | `Core/XgSim.AutoModel.cs:219–240`；`Core/XgSim.EpochAction.cs:40–47`；`Core/XgSim.cs:650–668` |
| 速度 | `speed=1.3` 是无量纲的处理量系数，不是准确率增加 30 个百分点。正常 `CardsPerEpoch` 中乘 `a.speed`、最快已拥有优化器的倍率、研究和进度倍率、手动连击或自动轮倍率，再除模型大小项，最后取整并限制在 4–320 张/轮。实际可能完全不增加卡数（上下限/取整），也不能直接宣称比 RNN 快 30%，因为自动配置会改变模型大小等条件。 | `Core/XgCatalog.cs:168`；`Core/XgSim.Board.cs:191–200`；`Core/XgSim.cs:319–347` Optimizer/OptSpeed/SpeedResearch |
| 离线与耗时 | 离线模拟固定 12 张/轮，绕过 ×1.3。TextCNN 没有缩短训练轮时长的专门分支；基础轮时长仍 0.6 秒，实际进度受资源影响。 | `Core/XgSim.Board.cs:41–42,193`；`Core/XgSim.cs:24`；`Core/XgSim.EpochAction.cs:10,38` |
| 串行限制 | 局部共享不属于串行连线；在 `parallel` 数据集，不乘串行训练的 0.25 系数。这只是此数据集上的系数条件，不是所有任务固定 4 倍加速或真实 GPU 并行保证。 | `Core/XgSim.Board.cs:66,185–199`；`Core/XgSim.Stages.cs:56–57` |
| 层数 | 架构基础层数上限 6 层；拥有 BatchNorm 后 `MaxDepth` 为 12。实际配置还取已解锁层数上限与架构上限的较小者，并受显存影响。不是购买就立刻变成 6 层。 | `Core/XgCatalog.cs:168`；`Core/XgSim.cs:404–410`；`Core/XgSim.AutoModel.cs:135–155` AutoShape |
| 参数规模 | `paramFactor=3`，`ParamsK = depth × width² × 3 / 1000`，单位为千参数。它进入显存、模型选择和误差上限公式，不是准确率乘 3；其他架构参数系数不叠加。 | `Core/XgSim.cs:351–367` ParamsK/VramNeedMB |
| 短距离限制 | `span=5` 是模型上限公式使用的跨度参数；对顺序敏感数据且 `needSpan>5`，结构误差分量加 `0.35 × (1 − 5/needSpan)`。该值是误差分量，不是最终准确率直接扣同等百分点。没有 seq2seq 标志，数据要求生成映射时还会有结构误差分量。 | `Core/XgSim.AutoModel.cs:334–366` CeilingParts/CeilingAcc |
| 实际局部连线 | `WiringOf("textcnn") = LocalShared`；底层共享词语特征标识（位置编码会改变此标识），`Reach` 只连同序列中横纵坐标各相距不超过 1 的元素。不得将 `span=5` 写成底层严格“5 个字卷积窗口”，也不得承诺真实训练了某套学术 TextCNN 网络。 | `Core/XgSim.Board.cs:62–74,94–117`；`Core/XgBoard.cs:284–287,384` |
| 学习成绩 | `bias=.7` 缩放参数误差分量，不等于提高 30% 准确率。实际成绩在 `StepAccuracy` 里按上限与卡数推进，也可能回落；`Evaluate` 本身主要刷新上限与派生指标。 | `Core/XgSim.AutoModel.cs:342,356–370,491–530`；`Core/XgSim.cs:517–528` |
| 换架构 | 新训练模式下切换会 `KeepProgress`：无迁移学习保留系数 0，有迁移学习为 0.6（相对随机水平以上的进度，不是最终准确率保留 60%）。兼容旧训练版本首次接管有专门豁免。不能把自动装备写成无损提分。 | `Core/XgSim.AutoModel.cs:224–232,249–284` |

## 保留未发现状态

`NodeVisible` 对未拥有的 TextCNN 使用阶段门槛（第 4 阶段），`XgTechTree.Visible` 传递该判断，科技页 `Refresh` 在 `Desktop/XgTechTreePage.cs:428–431` 隐藏不可见节点并取消其固定提示。TextCNN 本身没有额外的“？？？”发现机制；不要人为添加或绕过既有阶段隐藏。

`XgTechTreeCopy.Name/Note` 的 `NodeMystery` 和图鉴早返回保持原样。科技页 `:758–765` 保留未知节点提示；本交付不扩展任何其他词条，也不在未显示的节点上揭露信息。

## 验证与待整合

- 已完成：源码静态阅读与交叉追踪，覆盖未购买、买入但未选中、轮中购买、取整/卡数上下限、离线固定卡数、换架构进度、阶段隐藏等真实边界。以上是静态结论，不标记运行 PASS。
- 未进行：C# 编译、Unity/游戏运行、实际训练数值对比、鼠标悬停、中文/英文截图和视觉验收。统一编译与场景验收由主聊天负责。
- 待整合：无需必须合入的补丁；若负责人需要更具体的玩家说明，可采用上面的双语详版，继续沿用既有隐藏与价格逻辑。长文版的排版由实际界面验收决定。
- 待验证：若采用详版，在阶段 3/4、已购未选用/已选用、活动轮次买入、两种语言下验证显示及换模型提示。对倍率做运行验证时须控制算力、模型形状、优化器、连击、数据集和其他倍率，不能比较两次自动配置后不同模型便宣称恰好增加 30%。
- 结束复核：依赖文件的 SHA256 首次值和结束值见 `source-evidence.json`；若随后整合窗口改变这些文件，应按方法重新核对，不能盲用旧行号。
