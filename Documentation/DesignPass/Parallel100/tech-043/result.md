# tech-043：学习率衰减（lrschedule）

日期：2026-10-13（Asia/Singapore）。状态：只读源码审查完成；文案补丁待整合。未修改生产文件，未启动 Unity/游戏，未读取玩家存档。未读取任何新策划正文，未创建聊天/子 agent，未给其他聊天发消息。

## 结论与交付

当前系数与不叠加说明基本正确，但英文名称仅为缩写 `LR schedule`，解释把真实理论与游戏实现混在一起，效果缺少购买生效时点、装备要求、系数单位以及上限不保证提高的边界。交付 `changes.patch`，只修改 `XgTechTreeCopy.cs` 中本节点的名称、解释和效果，不修改模拟数值或其他词条。

已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md`：此前已处理 0.9/0.85 和不叠加，因此保留这些正确结论。其他条目的补丁中出现本节点只是上下文，不重复整合其内容。本任务开始时专属目录不存在。

补丁目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。
当前源文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`。
补丁含 SHA256、目标路径及三处局部上下文。依赖快照见 `source-hashes.json`。

## 可直接采用的中文文案

名称：学习率衰减

解释：原理是先大步接近答案，再小步调整细节；游戏将它体现为成绩上限的改善。

效果：购买后两条训练线自动生效，无需装备。成绩上限公式中，参数、数据和结构的合计误差项乘 0.85（无单位），不改变数据集本身的最低误差。这会改善可达到的成绩上限，但仍受结构等限制；不是准确率直接增加 15 个百分点。替代学习率搜索的系数，与 AutoML 的同项效果不叠加。购买时重算上限；成绩仍需后续训练和评估，不保证立即提分。

## Ready-to-use English copy

Name: Learning-rate decay

Explanation: The principle is to take larger steps first, then smaller steps to refine the answer; the game represents this through a better score ceiling.

Effect: After purchase, applies automatically to both training tracks; no equipment step is needed. The combined parameter, data and structure error term in the score-ceiling formula is multiplied by 0.85 (a unitless factor), leaving the dataset's minimum error unchanged. This improves the reachable score ceiling, subject to architecture and other limits; it does not add 15 percentage points to accuracy. Replaces the learning-rate search factor and does not stack with AutoML's same effect. The ceiling is recalculated on purchase; score gains still require further training and evaluation and are not immediate or guaranteed.

补丁继续读取 `XgCatalog.LrFinal[4]`，不硬编码 0.85；以上成稿按当前常量呈现。

## 原文件证据与时点

以下路径相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`；行号按本次读取版本。

| 证据 | 结论 |
| --- | --- |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306–310,369–370,425,643–644` | 实际 Research 节点，stage=4、前置 momentum、节点基础价1500；Research 表中的 cost=50不是本科技树购买价。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:131–149` | 实际购买价为 ceil(1500×0.25)=¥375；未拥有时在第4阶段及以后可见，单纯到达阶段并不取得效果。价格继续由现有 UI 显示，补丁不重复硬编码。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188,194–205,252–279` | 购买需前置、可见性及资金检查；成功后加入 S.unlocked，随后 AutoConfigureAll 并 Evaluate 所有 run，无单独装备步骤。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:304` | Has(lrschedule) 或 AutoTrainLevel≥5 时 RateFinal=0.85；否则有学习率搜索时0.9，否则1.0。全局属性，无训练线筛选。 |
| `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:135` | LrFinal[2]=1、[3]=0.9、[4]=0.85，均为无单位系数。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:356–368` | frac=clamp((parameters+data+structure)×RateFinal,0,0.97)；floorError 在其外另加；随后仍有结构限制、数据污染惩罚等约束和最终封顶。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:517–528` | Evaluate 重算 run.ceiling；不按此系数直接给 run.valAcc 加分。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:492–526` | StepAccuracy 在训练结算时先 Evaluate，再以实际学习进度向上限靠近；也可能回落。没有“购买即加15分/15个百分点”的保证。 |
| `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:209–211` 与 `XgSim.cs:568` 起 SafeLr | 自动配置的步幅取 SafeLr；未发现 lrschedule 专属逐轮衰减分支。理论只能作为原理解释。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:13–17,48–53` | Name/Note 均先处理 NodeMystery，补丁位于保护条件之后。 |
| `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:127–129,144–146`；`Desktop/XgTechTreePage.cs:772` | 保留可见性过滤及现有文案调用路径，不修改隐藏节点展示规则、不提前列出其他未发现技术。 |

叠加规则：搜索的0.9被0.85替代，绝不是0.9×0.85；AutoML已提供0.85时，再买本节点不会把该项降到0.85²。其他上限相关机制仍按原公式各自生效，不能把“不叠加”理解成抵消其他道具。本节点没有次数、持续轮数或消耗期限。

## 验证与待整合

- 已执行静态源码核对与 `git apply --check`：退出码0，补丁能够匹配生成时源码；没有应用补丁。
- 未执行 C# 编译、内存模拟或 Unity 测试；没有运行通过声明。
- 未执行界面或视觉验收；中英文长段落的换行、滚动和裁切由整合聊天统一验收。
- 整合时复查源文件哈希；其他聊天可能修改同一文案文件，若哈希不同须按上下文人工合并这三处，不能覆盖整文件。
- 待真实边界验证：阶段3未拥有时不展示；阶段4未买时无0.85；购买后两训练线均取0.85且无需装备；搜索0.9→0.85；AutoML已有0.85时购买不重复增益；结构上限或 frac 封顶时不保证提高；购买后不会无条件增加现有评估准确率；后续训练可能进步或回落。

仅本词条交付完成，不表示整份策划、全局原则、编译或场景验收完成。

结束前依赖复核：source-hashes.json 所列8个源码文件 SHA256 均与快照一致。
