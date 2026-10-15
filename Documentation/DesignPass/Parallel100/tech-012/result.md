# tech-012：position（位置编码）文案核对

结论：正式名称正确，保留「位置编码 / Positional encoding」。建议仅修改 XgTechTreeCopy 的 position 通俗解释与效果两处字符串，补齐适用结构、自动启用时机及不叠加规则。现文案「非循环结构也能使用字的位置线索」范围过宽：该开关没有改变全连接、循环、编码器—解码器及普通 Attention 分支；实际作用集中于文本卷积与 AnyToAny 分支。不能把真实 AI 理论中的位置编码收益写成游戏评分增益。

仅写入本专属目录；未修改生产源码、数值或其他节点，未启动 Unity/游戏，未接触玩家存档。未继续阅读策划正文。初次定位搜索意外返回逐条清单的一条匹配，未将其用作效果依据，后续检索限制在源码及 Parallel100 任务记录。Parallel100 既有记录中未发现 position 专项交付，本目录原无交付。

## 可直接采用的中英文三行文案

**中文**

1. 正式专业名称：位置编码
2. 外行能懂的解释：给字加上位置线索，让支持的网络区分它在句子中的位置。
3. 玩家看得见的真实效果：解锁后自动启用，无需装备；训练中的模型在本轮结束后配置。支持的结构在大脑训练中可区分距句尾的位置、识别句首或对齐两句中同位置的词，具体取决于结构。需继续训练学习这些线索；不直接增加评分或预训练规模，一次解锁、不叠加。

**English**

1. Professional name: Positional encoding
2. Plain explanation: Give words position clues so supported networks can tell where they sit in a sentence.
3. Visible effect: Enabled automatically after unlock; no equipment step. Models in an active round are configured after it ends. Depending on the architecture, brain training can use distance from the sentence end, identify the opening word, or align words at matching positions in two sentences. Further training learns from these clues. No direct score or pre-training scale bonus; one unlock, no stacking.

该玩家文案不点名未发现的后续架构，也不宣称所有结构同时获得三种效果。下面的实现明细供整合者核查，不应无条件展示在未发现节点上。

## 生效时机、单位与叠加

- **发现／购买条件**：XgCatalog.cs:430 配置 stage=5、parent=attention、目录成本 6000。XgSim.Progression.cs:131–148 的实际费用为 `Ceiling(cost × .25)`，因此当前支付 **¥1,500 游戏经费**；未拥有且阶段不足时不可见。购买还须满足前置节点与经费条件（XgSim.cs:193–205）。价格已有独立 UI，不在效果文案重复硬编码。
- **购买不是装备**：BuyNode 在成功扣款后加入 `S.unlocked`，立即调用 AutoConfigureAll（XgSim.cs:252–280）。AutoTechniques 把 `run.position` 设置为 PositionOwned；Knobs 最终用 `Has("position") && run.position`（XgSim.AutoModel.cs:198–211；XgSim.Board.cs:84、106）。闲置模型在购买时配置；`epochActive` 模型跳过，轮次结束后由常规 Step／下轮 BeginEpoch 自动配置（AutoModel:219–240；XgSim.cs:651、1507–1510）。需要有效 host，不宣称只改存档的解锁标志就已完成配置。
- **何时训练有效**：配置改变后，后续 Board.Train 和使用 Knobs 的预测读取新线索；已学概念不会被该购买自动重训。BeginEpoch 在 AutoConfigure 后调用 TrainBoard（XgSim.cs:651–657；XgSim.Board.cs:211–225）。旧概念是否能命中新命名须由后续训练验证，不能保证即时提高答对率。
- **单位**：位置是离散字／词位置索引；距离 `fromEnd = len - 1 - x`，句尾为 0、前一字为 1。双句对齐使用相同 `x`，没有百分比、百分点或速度单位。
- **叠加**：一次拥有的布尔开关，没有等级或重复购买加成；Owned 状态不能再次走 Buyable 成功购买（XgSim.cs:198、257）。与结构分支协同生效，不是加法或乘法增益。ScaleItems 中没有 position（XgSim.Finale.cs:75–84），不提供预训练规模倍率。
- **其他解锁路径**：旧项目完成方法 CompleteTransformer 也 Grant("position")（XgSim.Progression.cs:587–596）；该方法本身未调用 AutoTechniques，仍依赖随后的自动配置。普通购买 transformer 的 ApplyProgressionNode 只赠 multihead/layernorm/residual（同文件:206），不能宣传“购买 Transformer 必送位置编码”。不把这些后续路径写进当前玩家文案。

## 实际作用与原文件依据

| 分支 | position 开启后的差异 | 代码依据 |
| --- | --- | --- |
| LocalShared 且卡片 region=sequence | 特征名附加距句尾索引 `@-fromEnd`；非 sequence 卡片不受此处影响 | XgBoard.cs:271、284–287 |
| AnyToAny、单句、非多头 | 特征名附加距句尾索引 | XgBoard.cs:289–299 |
| AnyToAny、单句、多头 | 保留词本身并给 x=0 额外添加 `^词` 句首特征，不给每个词建立完整位置编号 | XgBoard.cs:293–299；Knobs 的 multiHead 来自 arch=transformer（XgSim.Board.cs:112） |
| AnyToAny、双句／多句 | 跨句组合只允许相同 x；同句仍保持原可达性 | XgBoard.cs:293、373–383；提前返回的无位置特征、made 特征例外仍适用 |
| Full、Recurrent、GatedRecurrent、EncoderDecoder、Attention | Elements／Reach 的这些分支不读取 position；不应承诺通用增益 | XgBoard.cs:281–282、301–317、379–388；WiringOf 映射见 XgSim.Board.cs:62–72 |

附带影响：CarryConcepts 在单头 AnyToAny+position 或 sequence 的 LocalShared+position 时判定位置绑定，换结构时不迁移底层概念（XgSim.cs:417–434）。这属于换结构时的条件行为，不是购买 position 立即清空大脑，也不改写迁移学习词条。

评分依据：XgSim.cs:656 明确区分大脑学习与评分；Evaluate（517–527）及 CeilingParts（XgSim.AutoModel.cs:334–349）没有 position 加成。因此此次文案仅承诺大脑训练线索，不承诺消除数值评分瓶颈、提高准确率或增加预训练规模。实际画面是否足够清楚仍待视觉验收。

神秘性：XgTechTreeCopy.Name／Note 开头保留 NodeMystery／IsAtlas 分流（14–15、47–50），本补丁不动；XgTechTree.Visible 使用 NodeVisible（127–130），页面用它控制节点显示（XgTechTreePage.cs:428），悬浮信息保留 mystery 分支（758–764）。position 本身依 stage 隐藏，NodeMystery 当前仅针对 label.auto；不擅自更改既有发现机制。

## 补丁、源版本与整合

- 文件：`changes.patch`，仅两个 position case 的字符串替换，包含目标文件与相邻上下文；尚未应用。
- 目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
- 生成时目标 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
- 十个已读依赖的基线 SHA256 见 `source-hashes.json`；结束前复核结果见 `verification.txt`。
- 整合时先复核源版本；若其他窗口改了相邻词条，按旧 position case 精确合并这两处，不覆盖整文件。保留 Name、谜面／可见性逻辑和其他词条。

## 验证状态及待验证边界

静态检查：已核对购买→自动配置→Knobs→Elements/Reach→TrainBoard 链路、评分路径、倍率表、可见性路径。补丁可应用性与源文件复核单独记录于 verification.txt。没有编造运行 PASS。

实际运行：**未运行**；Unity 编译：**未运行**；视觉验收：**未进行**。此项为纯文案变更，不新增仅复述实现的测试脚本。以下真实边界交给整合主聊天使用临时内存模型统一验证，禁止真实存档：

1. 阶段 4 未拥有时节点隐藏；阶段 5 缺 attention／经费不足时不可买；刚好 ¥1,500 成功一次，再买无叠加或二次扣款。
2. 空闲模型购买后位置开关生效；epochActive 模型购买中途保持本轮配置，结束后、下轮训练前开启；不要求手动装备。
3. 同一词分别出现在句首与后面：多头单句仅句首获得附加线索；单头／TextCNN 按距句尾位置区分。同样的图像输入在 LocalShared 分支中不因 position 多出文本位置标记。
4. 两句中 x 相同／不同的词分别测试；只让跨句可达性发生预期差异，同句不被误限制。额外测无位置特征与 made 特征，避免把前置返回规则误读成普通词对齐。
5. 相同模型、数据、已有权重的受控对照，不应因仅解锁 position 获得固定评分或规模倍率增益。后续概念学习及实际答题结果单独观察，不用一次随机结果断言准确率提升。
6. 中英文悬浮卡检查长效果文本、滚动与裁切；阶段未发现时不出现名称或效果泄露。视觉通过后也只勾本节点，不扩大到整份策划。
