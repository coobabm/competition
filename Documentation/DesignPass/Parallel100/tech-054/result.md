# tech-054：perceptron（感知机）词条核对

日期：2026-10-13（Asia/Singapore）。范围仅当前科技词条三项要求；未读取新的策划正文，未扩展其他节点。

## 结论

需要两处仅限文案的修正，交付 changes.patch，未应用到生产源码。

- 正式名称「感知机 / Perceptron」正确，沿用 XgCatalog，不改名称。
- XgTechTreeCopy.Explanation 当前直接取架构 note「只能画一条直线，学不会异或 / One straight line only. No XOR.」。它缺少对外行的解释，并省略当前人工特征路径。改为权重、加总、门槛的通俗说明，不把数学理论当作训练模拟的完整实现。
- Effect 当前所有架构共用「加入可用结构」。感知机实际开局已免费拥有，且两条训练线均默认设置为它，故增加专属生效条件、基础层数与逻辑数据限制。

已核对 Documentation/DesignPass/Agents/skilltree-task-001.md；其通用架构实现和已有验证记录不替代本节点审查。本任务目录此前不存在，没有重复制作已有同节点交付。

## 可直接采用的中英文文案

名称：感知机 / Perceptron

中文解释：给每条线索分配分量，加总后与门槛比较，回答「是」或「否」。基础结构没有隐藏层，需要人先整理线索，才能处理某些组合关系。

中文效果：开局免费拥有，是视觉和序列训练线的默认结构，无需购买或手动装备；系统会按数据和显存自动选择结构。基础深度上限为 1 层，其他已拥有技术可能提高此上限。没有可重复叠加的数值奖励；样本和资源满足要求后，训练并查看评估成绩。未拥有人为整理的组合线索时，逻辑数据的基础准确率上限为 55%（并非增加 55 个百分点，临时效果另算）。

English explanation: Weight each clue, add them up and compare the total with a threshold to answer yes or no. The basic structure has no hidden layer; some combinations need people to prepare the clues first.

English effect: Owned free from the start and the default for both vision and sequence tracks; no purchase or manual equip is needed. The system selects an architecture for the data and VRAM. The base depth cap is 1 layer; other owned techniques may raise it. There is no repeatable stat bonus. Train when samples and resources are sufficient, then check the evaluation. Without engineered clues, the base accuracy ceiling on logic data is 55% (not a 55-percentage-point bonus; temporary effects are applied separately).

补丁与上述文案一致。没有列举尚未发现的技术名称、价格或未来结构；保留 Note/Name 入口原有 NodeMystery 判断和页面 NodeVisible 逻辑。

## 原文件证据

路径以工程根目录为基准；行号以 source-sha256.json 所记录版本为准。

| 文件与行号 | 代码依据与结论 |
| --- | --- |
| Core/XgCatalog.cs:139–140 | id=perceptron，name/nameEn 正确，shared=true、maxDepth=1、span=1、bias=1.1；原说明无条件写 No XOR。bias 是公式系数，不能解释成玩家 +10% 准确率。 |
| Core/XgCatalog.cs:379 | A("perceptron", 1, "trunk", null, 0)：第一阶段主干、无父节点、基础价格 0。 |
| Core/XgSim.Progression.cs:35–44 | 新游戏直接加入 unlocked，视觉与序列均 arch=perceptron、depth=1。不是买后激活的新奖励。 |
| Core/XgSim.cs:194–205、252–279 | 已拥有返回 Owned；BuyNode 只接受 Buyable，因此正常开局不能重复购买叠加。架构没有感知机专属购买增益分支。 |
| Core/XgSim.AutoModel.cs:136–183 | 自动在已拥有且适用当前训练线的架构中比较模型；受深度/宽度/显存约束。默认不等于后续永久使用。 |
| Core/XgSim.AutoModel.cs:219–241 | 有 host 且非训练进行中才进行自动配置；结构变化后再 Evaluate。 |
| Core/XgSim.cs:404–409 | MaxDepth=a.maxDepth+(Has("batchnorm")?6:0)，故感知机不能写成永远最多一层。实际自动深度还取 DepthCap 与 MaxDepth 的较小值。 |
| Core/XgSim.Progression.cs:255–265 | 先加入研究加值；当 arch=perceptron、dataset=logic 且 !FeaturesOwned，再 Math.Min(acc,.55)。该 55% 是比例上限，不是概率提升 55 个百分点，也不是评分 550 分。 |
| Core/XgSim.AutoModel.cs:352–367 | CeilingAcc 经 ProgressionAccuracy 后，还处理污染、随机基线、临时学习率重启和数据集精度上限；所以 UI 明确“基础”及“临时效果另算”。 |
| Core/XgSim.AutoModel.cs:199–211 | run.features=FeaturesOwned && run.arch=="perceptron"；拥有人工特征后由自动配置启用。 |
| Core/XgSim.Board.cs:86–89、108；Core/XgBoard.cs:265、330–344 | 人工特征开关进入 Board；Engineered 为逻辑线索构造组合输入，足以否定原文不带条件的“学不会”表述。 |
| Core/XgSim.cs:603–606、639–675；Core/XgCatalog.cs:658 | 训练须该线存在可用数据且样本至少 4 条，训练前再检查资源；训练结算更新准确率/曲线，并按评估间隔生成评估。初始拥有结构不等于已获得训练成绩。 |
| Core/XgSim.cs:517–527、686–691 | Evaluate 重算 ceiling，不把已存 valAcc 直接截到该上限；Score 是依据数据集随机水平和误差下限归一化的 0–1000 分。不能许诺当前成绩立即变为或恒不超过 55%。 |
| Core/XgTechTreeCopy.cs:13–15、48–52 | 原 mystery/atlas 回退在新增分支之前，不改神秘节点。 |
| Core/XgTechTree.cs:146；Desktop/XgTechTreePage.cs:772 | 节点名称和说明已有文案层调用，不需改接线。 |

以上 Core/ 和 Desktop/ 均位于 Assets/LingGuangV05/XingGuang/。

### 额外边界（仅供整合与验收，勿照搬成全局玩家承诺）

1. CardAccuracy（XgSim.Progression.cs:228–245）对感知机 combo 卡还有单独的 0.55 截断，**不检查 FeaturesOwned**，之后还扣漂移。故“拥有人工特征就解除了所有 55% 限制”不成立。本补丁只说逻辑数据基础准确率条件，没有把卡片答对概率混成训练指标。
2. 新版训练 StepAccuracy（XgSim.AutoModel.cs:492 起）会发生回落；训练不是每轮必然涨分。拥有研究后逻辑基础上限可变，不保证一次训练达到上限。
3. 1 层的单位是游戏模型深度层数；55% 的单位是数据集准确率比例，不是百分比增益；节点本身不提供购买次数、等级或乘法叠加。其他研究有自身规则，本任务不重写。
4. span=1、bias=1.1 属现有架构公式输入，前者用于顺序任务结构损失，后者参与参数误差项，均不等于感知机拥有额外的“记忆力 +1”或“准确率 +10%”。未把它们包装为玩家收益。

## 补丁合同

目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs

当前源文件 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8

changes.patch 仅在 Explanation、Effect 两个方法开头插入 perceptron 专属返回；保留通用架构分支与所有其他词条。包含方法签名及相邻 switch/if 上下文。由于其他聊天也可能改同文件，整合前核对 SHA256；不一致时按节点与方法上下文人工移植，不能覆盖整个源文件。全部依赖散列见 source-sha256.json。

## 已执行与待验证

- 已完成：生产源码静态核对；补丁只读上下文检查 `git apply --check --ignore-space-change` 成功。此检查只证明补丁可定位，不是 C# 编译或游戏运行。
- 未执行：C# 编译、Unity 导入、运行时训练/购买测试、实际场景截图与视觉验收。没有宣称 PASS。
- 待整合：由科技整合负责人核对后应用 changes.patch；未给其他聊天发消息。
- 待统一验证：新建纯内存状态中两条线已拥有/默认感知机且不能重复购买；0/3/4 条样本边界区分“已拥有”和“可以训练”；有/无人工特征下逻辑基础上限与 combo 卡限制分别检查；深度技术与 BatchNorm 有/无时检查基础 1 层与可配置上限区别；已存 valAcc 高于 ceiling 时不误称即时截断；英文/中文长说明在最小窗口不裁切；未发现自动标注仍为原神秘提示。
- 禁止范围均未执行：未改 Assets/ProjectSettings/全局清单，不启动 Unity/游戏，不提交/推送/打包，不读写真实玩家存档。

结束前依赖复核结果见 dependency-recheck.txt。若其显示变化，以变化后的生产实现为准重新核对受影响段落。