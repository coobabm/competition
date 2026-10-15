# lesson-001 — 全新开局首卡真实猜测链路

日期：2026-10-13（Asia/Singapore）
整合负责人：01a130ed-2dd2-7051-915d-dc64ea66a8e2
范围：仅已授权第 13 行中的“无样本、无检查点时第一张卡先猜”。未读取新策划正文，未扩展至壁纸或其他任务。

## 结论

当前实现静态核对正确，无需生产修改，不交付 changes.patch。已核对交接 issues-task-001.md 与 DesignFirstLessonChecks，避免重复实施。

这里的真实猜测是游戏现有概念棋盘 XgBoard 的实际 Predict 返回值；未训练且没有匹配权重时，由该模型自己的 seed 奇偶规则给出稳定猜测。它不是外部大语言模型推理，也不表示模型已经学会。教学路径未按真值取反、未筛选必错题、未用准确率模拟覆盖预测。

## 原文件证据

以下路径相对工程根目录 Assets/LingGuangV05/XingGuang/：

- Desktop/XingGuangController.cs:90–105：无 labState 时 state 为 null，构造 new XgSim(state)，新局选择 spam。
- Core/XgSim.cs:73–91、Core/XgSim.Progression.cs:22–46：新状态进入 PrepareProgression(true)，使用初始 perceptron，当前 desk 为 spam。Core/XgModels.cs:152–173 中 labels、best、models 均为空集合。
- Core/XgSim.Teaching.cs:30–38、64–71：teachMode 与 UseBoard 默认开启；TeachingGuess 对普通 spam 卡直接调用 Board.Predict(FromCard(card), Knobs(...))，没有样本数、训练次数或检查点门槛。首次 AddCheckup 仅记录固定题预测，不训练、不保存模型。
- Core/XgSim.Board.cs:48–54、94–116：Board 按当前内存棋盘构造；Knobs 读取当前训练线配置，不要求部署检查点。
- Core/XgSim.cs:1169–1172、1281–1286、1301–1357：首卡由正常 CreateLabelCard 生成；spam 真值来自题库 p.yes。Core/XgMemes.cs:187–209 的抽题没有调用模型或根据模型输出筛必错题。
- Core/XgSim.Progression.cs:350–387 不改写 spam；Core/XgSim.Stages.cs:195–210 仅种下 logic 区域的初始节点，关机特殊卡仅适用于 logic/arith，故不替换首张 spam 教学卡。
- Core/XgBoardData.cs:141–159、325–331：spam 特征来自题目文本；truth 虽随卡携带，但 Words 不读取它。Core/XgBoard.cs:449–470、516–522：预测通过概念匹配与权重得分，未匹配到有效权重时返回 (card.seed & 1) == 0，预测函数不读取 truth。
- Desktop/XgLabelPage.cs:623–650：未排队的首卡优先 TeachingGuess，早于 Suggestion；shownGuess 保存返回值，教学显示直接使用该值。650、667 行分别显示“猜：是／否”并隐藏教学置信度条，因此临时 confidence = .5 不被展示为测量结果。
- Core/XgSim.cs:1361–1375 的普通 Suggestion 存在按准确率和 truth 生成建议的旧路径；当前首卡教学分支先成功返回，不会落入该路径。不能将普通建议路径误作本任务缺陷。

## 验证状态与待验证

- 已完成：只读静态调用链、条件分支、真值流向及现有检查入口审阅。
- 未执行：C# 编译、DesignFirstLessonChecks.Run、Unity、游戏运行与视觉截图；本交付不声称运行 PASS。
- 现有检查入口已覆盖 new XgSim 首卡可猜，以及猜测两方向 × 真值两方向 × 玩家评价两方向。其 Fixture 使用 9137/9138 构造测试方向，只存在于测试，不是生产首卡强制出错逻辑；无需复制整套检查。
- 主聊天统一运行时应补充/确认以下真实边界：新局调用前后 Samples("spam") == 0、best/models 均空、训练和手工作答计数不变；TeachingGuess 与同一棋盘直接 Predict 结果一致；重复刷新同一卡不改变卡 id、真值和猜测、不产生训练或检查点。
- 建议用同 seed、同文本、只改变 truth 的内存配对卡确认预测不随真值改变，并验证至少存在自然猜对与猜错两类结果。不要在生产路径改 seed、真值或预测来凑纠错流程。
- 视觉验收：全新局首次卡片在任何作答之前显示猜测，点击前无需训练；截图和宽窄布局由主聊天统一验证。壁纸事件及闪亮效果不属于本交付。

## 待整合

只需整合本审查结论，无生产补丁和新增脚本。第 13 行整体及文档整体仍不能标为完成。源码有并行修改风险，整合时以以下 SHA256 复核，若变化则重新审阅对应链路。

## 审阅基线 SHA256

| 文件（工程相对路径） | SHA256 |
| --- | --- |
| Assets/LingGuangV05/XingGuang/Core/XgSim.cs | C4F1998E95B717C93AD4195D5C9FBB43616614D5C4A88837CB3EBCF44DE9966B |
| Assets/LingGuangV05/XingGuang/Core/XgModels.cs | 4461CA0A8857A8BA78EAD346C869B5C125AB160411CF4EAAFB9F631E40197953 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs | D997BB22368619936B2CC7D3144EFE82519D0114A792B945BA7A9ADE40ABB838 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs | B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs | CF6FC6F55FD84843E96E2B2516D591F2F9AF304C0D6D4321AABA92B73A474AE7 |
| Assets/LingGuangV05/XingGuang/Core/XgBoard.cs | DAF725A9B24E7E8C7DD303C8A32ECD4B87D3B1AE301E0AB867AD503B2F75E963 |
| Assets/LingGuangV05/XingGuang/Core/XgBoardData.cs | 914B8F2523827EF473098100EB97CA9AACC923B4FFA31682DC08E3B4ABA3ED51 |
| Assets/LingGuangV05/XingGuang/Desktop/XgLabelPage.cs | A9A4034835455A081FACB33388AEB1155814AB835146694381DFE7AB571ABA89 |
| Assets/LingGuangV05/XingGuang/Desktop/XingGuangController.cs | 5C20DA1967F47EA3E5241613C69D1FEBDA3E5633BAB924499EB1AAAED1E222B6 |
| Assets/LocalBuild/Editor/DesignFirstLessonChecks.cs | C6716EFA175D5659BF1EA9C0BAFBAE9BE3315421F9B820B581CEB0DFCDF9D5A1 |
| Documentation/DesignPass/Agents/issues-task-001.md | C1F0ABF07714DC13B73A795C975D9EEDD48A9E59A418A96343E86E47F545B971 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Stages.cs | CE64E64CCFB1CC7C24ADAD73C771650F367DAD6E4F55E30F762AB8BDC34BEE98 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.DataSources.cs | 3623A45A332B1D8D7F000D26174F3F24FFDD2C467F44821C1348B94DB6DCC1C5 |
| Assets/LingGuangV05/XingGuang/Core/XgMemes.cs | 33CB756DFFCF838412A86769789EBAE2C4C2286334E877D530BD5F75DDFFF07C |

结束前重新计算以上 14 个依赖文件的 SHA256，均与审阅基线一致。此结果仅证明两次哈希之间未发现内容变化，不代替运行或视觉验收。

