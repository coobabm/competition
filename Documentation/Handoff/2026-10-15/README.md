# 《我的电脑活了》合并交接 · 2026-10-15

本次交付把我们此前的命名、桌面窗口与录制版本开发，以及截至 2026.10.15.2 的实际源码合在一条分支，供主项目大更新整合。功能开发目前按用户要求暂停。

## 合并基线与方式

- 目标仓库：coobabm/competition，目标分支 main。
- 核对时 main：60d7fef（2026-10-11）。本分支以该提交为祖先，包含 fbcf470、8f815ad、e9fc18e 三次之前的提交及此次源码汇总。
- 建议合并本 PR 的最终差异；若整合到另一个大更新分支，请以最终文件状态为准，不要只拿早期 e9fc18e 的全屏开机行为。
- 没有代替维护者合并其他尚未批准的 PR，也没有直接改写 main。
- Assets 是实际生效实现；Documentation/DesignPass/Parallel100 与 Resume 为历史候选档案，不是待批量执行的补丁。

## 已实现的最终行为

| 范围 | 当前结果 | 主要代码位置（Assets/LingGuangV05 下） |
|---|---|---|
| 名称与应用 | 游戏名《我的电脑活了》，AI 自定义名字同步应用身份；原有命名验证沿用 | Core、DesktopBridge 中既有命名代码；历史提交 8f815ad |
| 开关机 | 保留实体显示器、边框、房间背景的最初开机表现；保留逆向关机并回到标题 | CaseMenu/*、DesktopBridge/MainSceneEntry.cs、GameExit.cs |
| 桌面大脑 | 常驻壁纸上，真实概念、脑区、能力与性格展示；答题光路、纠正反馈、训练光波和概念生灭；更换壁纸仍保留 | XingGuang/Desktop/XgBrainWallpaper*、XgWallpaper*；Core/XgBoard、XgSim.AnswerTrace |
| 窗口 | 合适的默认应用尺寸、不透明底板，应用遮住桌面和下层窗口 | HongmengOS、Desktop/XgBrainWindowGlass.cs |
| 导航 | 主入口为“教、练、科技、AI名字”；科技页标题“成长 · 科技” | XingGuangView.Frame、Profile、XgTechTreePage* |
| 页面合并 | 数据来源与爬虫进入练页；模型仓库收为回档操作；卡册与终章进入AI页面；旧入口转到对应新页面 | XingGuangView、XgTrainPage*、XgDataPage、XgCrawlerPanel |
| 教学 | 全部13类客观题直接判断题目对错，不判断AI猜测；删除重复备注，仍保留真实学习和答后反馈 | Core/XgSim.Teaching、Desktop/XgLabelPage*、XgTeachCompanion |
| 众包工作 | 公司数据标注兼职，独立题卡和工作统计；不消耗私有教学卡、不增加私有样本；平台验证只在众包显示 | Core/XgSim.Work、DesktopBridge/Zhongbao/*、XgLabelPage* |
| 群聊隐私 | 群友不知道玩家私有AI及训练包；修复固定台词、模型提示、接收过滤和旧聊天记录迁移 | YY2016/YYGroup、YYChatHub、LocalLLM/LlmPersonas |
| 灵宠 | 第三阶段后开放；修复场景变化后文字残影对象失效、屏幕外位置和显示状态；新增叫回灵宠 | Desktop/XgResidentSpider.cs |
| 操作反馈与引导 | 已合入通用按压、首分钟引导、词条展示与教学/大脑反馈等已完成项 | XgButtonPressFeedback、XgJuice、XgFirstLessonWindow、XgTechTreeCopy 等 |

## 后来的用户决定覆盖旧方案

1. 不再要求开机拉到屏幕全覆盖：保留实体显示器。关机逆向动画保留。
2. 应用窗口不透明，桌面大脑不置顶盖住应用；早期玻璃/置顶方案不应恢复。
3. 所有客观题是“题目说得对不对”，不能恢复“AI猜得对不对”的按钮逻辑。家人短信、法律及关机等剧情选择保持主观选择语义。
4. 灵宠第三阶段以后开放，不要提前到第一阶段。
5. 导航要实际删除和合并，不以灰色禁用旧栏目代替；左侧必须能直接看到“科技”。
6. 群友不应获得玩家的私有AI状态；众包是公司兼职，不是玩家自己的训练界面。

## 存档与构建注意

- XgState 新增 workDesk、workCards、workSubmitted、workCorrect、workIncome；旧档缺字段时兼容初始化。工作计数从独立记录开始，不伪造旧教学记录为公司工作。
- 群聊迁移仅替换旧的违规群友文本；玩家文字、正常聊天、时间、数量、未读状态保留。
- RECORDING_PREVIEW 是专用录制构建宏：实际进度位于包旁 UserData；--recording-smoke 使用 VerificationData 副本，普通构建不采用这一保存路径。
- Unity 版本 6000.4.5f1。RecordingPlayerBuild.Run 构建标题和主场景，当前录制版本号 2026.10.15.2；现有存档存在时不会重新生成演示档。
- ProjectSettings 默认产品版本仍是此前正式分支的 2026.10.12.2，录制构建临时设为 2026.10.15.2 后还原；对方做大更新发布时应自行统一正式版本号。
- 模型权重、llama运行文件、个人存档、LocalVerification、Build和Unity恢复场景不进入代码PR。构建源码不会自动下载模型；测试自由对话需按仓库既有方式准备模型运行文件。

## 验证证据与复现

本地最终代码已通过：

- DesignFirstLessonChecks.Run：全部13类客观题判定、学习事件、防重复提交、公司工作结算/罚款/题卡隔离、JSON往返和旧档兼容。
- DesignCrowdPrivacyChecks.Run：默认与自定义AI名、泛指私有训练包、公开AI新闻/兼职不误伤、旧聊天迁移及幂等。
- RecordingPlayerBuild.Run：Unity Windows构建成功。
- 实际录制包 --recording-smoke：四项导航、科技文字、当前解锁题型一致按钮、私有教页不显示众包验证、真实点击公司任务后计数增加且私有题卡/样本不变、页面合并、壁纸与灵宠检查 PASS。
- 实际玩家存档构建及验收前后SHA256一致；所有场景验收使用副本。
- 历史首分钟教学场景有93项通过记录；本次未重跑全部历史场景，也未重新评测Qwen回答质量。

复现构建：Unity命令行使用 -batchmode -automated -quit -projectPath <仓库目录> -executeMethod RecordingPlayerBuild.Run -logFile <日志路径>。实际包测试前复制合适的第三阶段以后档到 VerificationData/chapter-one.sav，再带 --recording-smoke 启动。该测试会退出游戏，勿将其当普通游玩启动参数。

![众包公司任务](company-work.png)
![直接判断算式](statement-judgment.png)
![科技入口](technology.png)

## 与对方现有更新的交叉点

核对时仓库开放PR包括 #1、#2、#3、#4；本PR不表示这些已经合入。

- #2 涉及训练、科技、经济和来历等大量同模块文件：合并时重点保留本表最终交互和最新用户决定，不用旧页面整体覆盖新的四入口与独立公司任务。
- #3 是参数/数据主线规划文件：不因代码功能相似就把规划清单全部标完成。
- #4 “答错题目扣除该题答对可得的报酬”修改 XgSim.cs、XgCatalog、XgLabelPage*、YYChatHub 等；本次新增的 TryAnswerWork 使用 ManualPayFor / ChargeHandFine。若采用该PR新的罚款政策，必须同时检查公司任务与私人教学两条结算路径，避免仅改旧 Answer 导致规则不一致。
- 开关机与窗口修改也涉及 DreamOS 的 UserManager 和 HongmengOS 的 LCD/菜单适配；不要只挑选 XingGuang 文件夹而遗漏依赖。

## 未完成与暂停边界

历史策划并未全部完成。四页设计中的完整习惯表、接线任务向YY迁移，以及逐条清单尚未勾选的内容仍需后续处理；新入队文档没有全部实施。旧档案里“候选”“待验收”不等于完成。开发目前暂停，本次仅汇总上传供对方整合。
