# 上游语义整改暂停记录

收到 root 转达用户“完成当前项暂停”后立即停止；未改 main。工作目录：upstream-semantic-stage。基线：upstream-7da28342-audit（已应用 context-only 重基的候选）。

## 已暂存但尚未编译/验收，不能应用为完成项

- XgSim.PhoneSort.cs：保留 psStep/psPicks/psFixed/psIds/psAlibi 与小游戏算法；结尾改玩家私下检查自己的备份，日记不再叙述替阿杰做事。
- AjiePhoneSort.cs：开场改找到自己的旧U盘，移除群聊派单与网友进屋；结束用本地复盘窗口代替群里追问；保留分类/纠错/敏感照选择。部分英文描述、旧注释和素材身份仍待逐条核对；尚未运行流程，尤其窗口关闭/重新打开与 morning 状态需要检查。
- XgSim.ZhouChat.cs：自学阶段便签、三段自我假设/证据复盘；保留 confess1/2/3 与旧字段但 ZhouApply 不再设置 zhouKnows，新增 selfStudyReflections/selfStudyResolved。旧历史字段不删除。新增字段旧档迁移尚需测试。
- LaoZhouYY.cs：同组件名改私有自学便签窗口，保留 Install/Lesson 接口，不再往老周YY发训练指导。**该文件刚完成初稿，未编译；应先检查外部调用和便签队列/多选按钮/过场焦点/窗口尺寸，再应用。**
- LlmPersonas.cs：删新增知情提示分支和实时 ZhouLabLine/GuideState 注入，老周只讨论玩家主动问的公开概念；旧 zhouKnows 不赋予人类模型知情权。
- XgSim.CardLoan.cs：不再根据 zhouKnows 或实时显卡数量说私事；约电脑城交接，保留借卡资源挑战和拒绝。
- YYGirlfriend.cs：packet>0 时不先调用 ArcHeard，避免红包被开放支线吞掉。**有效亲手送礼仍可能关闭支线，尚未修；需要独立交易优先测试。**
- QingwenArc.cs：两条虚构玩家旧承诺/昨晚行为改当下请求。
- XgSim.YesNoNight.cs：job 保留id，文本改“我能升职吗”；已完成旧档回顾是否保留原文本尚待验证。

## 未完成

尚未生成语义分层补丁；未编译、未独立行为测试、未Unity验收；six_stage_lines新增hook仍有网友串门文本待改；StageOneTutor在私有便签组件不存在时的老周私信fallback未迁；LaoZhouYY自学摘录部分旧科普口气尚需润色；新归纳字段、既有zhoKnows旧档、支线交易、PhoneSort完整状态链均需测试。当前目录仅是中途工作，不可称为已完成。

恢复后先读本记录和 upstream-7da28342-conflicts.md，从当前暂存继续，不重复拷贝覆盖 main，不将 context-only 补丁误作已解决所有语义冲突。
