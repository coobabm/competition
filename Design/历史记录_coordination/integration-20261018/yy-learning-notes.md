# YY 题本 / 性格 / 严格对话解锁暂存

不改 main、不启动 Unity，依赖前面已交付的旧工作移植与世界单元（YYChatHub/YYChatView基线已包括租金和群聊预设）。文件在 legacy-stage；补丁 yy-learning.patch；基线 yy-learning-base。

## 已实现
- 新增纯核 XgSim.YyLearning：8 题包、9 标签、12 解锁条目 JSON；每日免费包；购买入口、阶段和余额检查。
- 题本 askedLog 每题库200，YY分组最多50、搜索、随机、点击重问。独立于 teachingNotebook，后者保留最初四题和现有训练用途。
- YY 对AI新增题本/性格按钮及260高抽屉，展开后将聊天区域上推，不覆盖原聊天；未购包灰色入口去科技。购买本身由科技/淘货接相同纯核接口。
- 在原 XgYyTalk 的模型/阶段语法/排队管线上增加hook；不另做回复管线。使用原始 YYMessage.text 严格 Ordinal 匹配（空格、标点、大小写均不可替换），仅 Deliver 真正写入AI消息之后登记，空文本/未实际回话不登记。现有阶段兜底是实际显示的回答，同样视为回答；模型失败但游戏生成有效离线回复的情况仍视为它已经回答（若需只认模型回复，请主代理裁定，不暗改阶段玩法）。
- 对话解锁保存它当时的答案，事件只触发一次；YY提示；科技树动画/可买条件由主代理消费接口，未接入不算该条完成。
- 性格题回复下9标签，单回复0–2个、去重、每标签最多5样本；推轴，飘字，honest/flatter计数；样本与题包风格加入已有动态提示词，不覆盖原AI系统提示词。回答标签及题本对比元数据保存于YYMessage。
- 所有新显示文本成对中英。

## 对接边界
教页代理 performance_opening_audit 的 teaching-stage/XgSim.DataPacks.cs 已提供 RecordTeachingQuestion 最后可选 answerText/aiAnswer，整合时在该方法一次性调用 RecordAsked(dataset,本地化题目,实际选择文本/玩家是非答案,aiAnswer)。不可将标准正确性写成玩家答案。
练页 design_inventory 已收到 RecordAsked 约定，需在答卷生成事件写入，不在 Refresh。
科技/淘货：OwnsPersonaPack(id)、PersonaPackBlocker(id,host)、BuyPersonaPack(id,host)。
科技对话条件：ChatNodeReady(node)、ChatNodeHint(node)、ChatNodeAnswer(node)，ChatNodeUnlocked事件供科技动效。注意12个数据node与现有目录id需要映射审核。最新提示词高于旧文档，不添加晚一阶段自动绕过。
终章/底线卡后续需消费 honestTags/flatterTags；实际谄媚反馈可接练页 NoteTrainingPhenomenon("flatter")。本补丁不将未发生事件伪造成触发。

## 检查
YYCheck：完整核心叠加编译+严格匹配正反例、失败/空回复、阶段、一次触发、题库上限、标签上限、样本上限、老档字段默认检查通过。
Unity自带Roslyn + 主项目真实Unity程序集/源码叠加（所有前述世界暂存一起）0错误，日志 yy-compile/result.log。该步骤是静态编译，不是 Unity 编辑器验收。

## 未完成，不能标全完成
Unity实际编译/交互/抽屉小窗口布局/切语言/老存档运行验收待许可证恢复；教/练日志生产hook、科技/淘货购买和节点实际门槛仍待整合；题本复杂答案的语义同义比较目前为保守字面对比（是/否/对/错中英统一）；它问你/委托验收/教新词/起外号/安慰等扩展内容未在此单元实现；没有改音乐与语音。
