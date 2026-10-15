# tech-009：Sigmoid 文案核查交付

日期：2026-10-13（Asia/Singapore）。仅核对 sigmoid，生产源码只读，只写本目录。

## 结论

需补全效果文案。现有专业名称和通俗解释可沿用；Effect 将 Sigmoid 与 Step 合并，只说解锁激活函数，遗漏生产评分路径真实存在的准确率上限加成，也未说明选择优先级及层间衰减。changes.patch 仅拆出 Sigmoid 效果，Step 的原字符串保持不变。未修改模拟数值、购买逻辑或保密分支。

先核对 Documentation/DesignPass/Agents/skilltree-task-001.md，确认已有统一文案层及整合负责人的检查记录；本目录此前不存在。记录未覆盖此次精确公式问题。未打开新策划文件；初次范围偏宽的关键词检索意外命中了既有逐条清单中的 Sigmoid 一行，之后排除该清单，所有结论均以生产源码为依据。

## 可直接采用的中英文文案

名称（保留）：
- 中文：Sigmoid 激活函数
- English: Sigmoid activation function

通俗解释（保留）：
- 中文：把突然开关改成平滑变化，让细小的误差也能传回去。
- English: Replace an abrupt switch with a smooth curve so small errors can travel backward.

效果（补丁使用此完整文本）：
- 中文：购买后无需装备：两条训练线的准确率上限公式各增加 1 个百分点，与其他同类加成相加，再受结构限制和数据集上限约束；不是立即给当前成绩加分。系统自动配置时，未拥有 ReLU 就采用 Sigmoid，激活函数不叠加；训练后再看评估成绩。采用时，概念板层间误差基础保留 25%，前向信号基础保留 80%，逐层相乘，相关技术可改变这些系数。
- English: No equipment step is needed after purchase. Add 1 percentage point to each track's accuracy-ceiling formula, alongside other additive bonuses, before architecture limits and the dataset cap; this does not instantly raise the current score. Automatic configuration uses Sigmoid unless ReLU is owned; activation functions do not stack. Train and evaluate to see the result. While selected, the concept board retains a base 25% of the error and 80% of the forward signal per layer, multiplying across layers; related techniques can change these factors.

“平滑曲线”是概念解释，未宣称游戏整体改用真实 Sigmoid 神经网络。概念板使用简化传递系数，权重更新误差仍用 Math.Tanh(score)；XgTinyNet 的真实 Sigmoid 函数不能视为购买本节点才获得的效果。

## 时机、单位、叠加和代码证据

下列 Core 均指 Assets/LingGuangV05/XingGuang/Core，Desktop 指同级 Desktop。行号为阅读定位，应用前按符号及上下文核对。

| 证据 | 事实 |
| --- | --- |
| Core/XgCatalog.cs:388、617 | R("sigmoid", "mlp", 120, 1)，是研究节点，不是架构。第1阶段，前置 MLP；基础价120。 |
| Core/XgSim.Progression.cs:129 起 | StoryResearchPrice=.25；NodeCost 向上取整，因此当前标准实际支付 ¥30。文案不重复硬编码价格，沿用页面报价。 |
| Core/XgSim.cs:188、194–206、252–279 | Has 查询 unlocked；已拥有不能重复购买。成功付款加入 unlocked，然后自动配置、Evaluate。仅满足前置不产生效果；无需装备。 |
| Core/XgSim.AutoModel.cs:198–246；Core/XgSim.Board.cs:78–81 | AutoTechniques 设 act=-1；BestActivation 优先 ReLU，其次 Sigmoid，最后 Step。一次只采用一种激活函数。进行中轮次跳过 AutoConfigure，不能承诺购买瞬间中途重配。 |
| Core/XgSim.EpochAction.cs:43 起；Core/XgSim.cs:651 起 | 轮次结束先清 epochActive，再 TrainEpoch；训练结算前重新自动配置，因此新选择用于随后实际训练计算。 |
| Core/XgSim.Progression.cs:255–266 | ProgressionAccuracy 加 .01；与 bias 的 .02、step 的 .01 相加。单位是准确率百分点：.70+.01=.71，即70%→71%，不是70%×1.01。拥有即计入，不依赖当前选用 Sigmoid。仅引用其他节点作叠加证据，不改其文案。 |
| Core/XgSim.AutoModel.cs:356–369 | 生产 CeilingAcc 调用 ProgressionAccuracy，随后处理近亲繁殖惩罚、临时加成及数据集上界。原始加法项为1个百分点，最终上限未必恰涨1个百分点；它并非只在遗留路径有效。 |
| Core/XgSim.cs:517–527、651–668；Core/XgSim.AutoModel.cs:492–523 | Evaluate 刷新 ceiling，不直接给存储 valAcc 加 .01；训练按进度朝上限推进且可能回落。购买引发模型重配也可能改变成绩，不能承诺购买立即提分或下一轮固定+1个百分点。 |
| Core/XgBoard.cs:150–181、550–569 | Sigmoid G=.25；无跳连时 BatchNorm 加 .05，跳连令 G=1。更新乘 G 的层间距离次方，非全局速度×.25。depth>1、g>0 等条件允许配对概念形成链接；购买本身不立即生成概念。 |
| Core/XgBoard.cs:167–181、465 | 前向基础 RelayKeep=.8；无跳连时 BatchNorm 加 .02，跳连则为1。RelayLeft=.8 的层间距离次方；仍叠有 RelayGarble 噪声。80%是原信号保留系数，不是准确率。RelayOn 当前默认true。 |
| Core/XgTechTreeCopy.cs:16、50；Desktop/XgTechTreePage.cs:758 起 | Name/Note 的 NodeMystery/atlas 回退及 UI 神秘节点提前返回均保留。Sigmoid 当前第1阶段可见且没有专属 mystery 标记。补丁不新增其他未发现节点内容。 |

## 补丁与依赖

目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs。

生成补丁时 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。

changes.patch 包含前后 case 上下文；依赖完整路径及 SHA256 见 dependencies.json。并行整合可能修改同文件，若摘要不同，重新核对本节点 case，仅合并 Sigmoid 文案，不覆盖整个文件。

## 验证与待整合

已执行：源码静态核对；git apply --check 检查补丁可适用，退出码0，未应用生产文件。未运行 C# 检查、Unity、游戏或视觉验收，没有读取真实玩家存档；不得将未运行项目标为 PASS。整合聊天此前检查不等于本补丁已验证。

主聊天待完成：

1. 对照最新源码合并并统一编译；若相邻 case 已改变，按目标字符串整合。
2. 独立内存状态边界：仅满足前置尚未购买无加成；成功购买一次；重复购买不叠加；已拥有 ReLU 时仍选择 ReLU，但 Sigmoid 公式加法项保留。
3. 固定其余条件验证原始70%项变71%；perceptron+logic 的55%上限可使收益为0；rnn+longtext 的差值先成为 .01×.9^10；数据集封顶时收益可为0。不能断言“固定提升1%”。
4. 无模型重配、无额外惩罚变化时，购买前后 valAcc 不直接加 .01，ceiling 重算；再训练观察变化。覆盖进行中购买、缺资源不能训练、临时加成与上限共存。
5. 概念板跨两层：基础误差保留 .25²=.0625，前向 .8²=.64；BatchNorm 为 .30²、.82²；跳连为1。这些不是成绩百分比。
6. 实际中英文科技页截图检查长文本换行、最小窗口可读性及神秘提示。文本较长，若需压缩应保留生效时机、百分点、约束和选择不叠加，并保留独立数值说明。

结束前复核：dependencies.json 中10个生产依赖 SHA256 均未变化，记录见 dependency-recheck.json；再次执行补丁适用性检查，退出码0。无需刷新本次补丁基线。

本结论仅交付 Sigmoid；不标记全局原则、整份策划或其他词条完成。
