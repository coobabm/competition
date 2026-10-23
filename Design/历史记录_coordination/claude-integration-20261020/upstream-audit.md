# 上游发布与功能只读审计

记录时间：2026-10-20 20:12（Asia/Singapore）。网络证据为本轮前段已经执行的 GitHub authenticated releases API 与 git ls-remote 只读返回；未重复拉取。API 调用的秒级时间未另存，不将本记录时间冒充取证时间。

## 发布基准

- 最新 API 第一项：v2026.10.20.2，非 draft，published_at=2026-10-20T08:15:17Z。
- target_commitish / tag：142debb109c5e72c24f758dfa350e4f4ce3fe737。
- 附件 MyComputerAlive-Windows.7z.001：1,346,250,217 bytes，另有 READ-ME.txt / SHA256SUMS.txt。
- 当前整合基准 d8e5677b 仅比该发布多 Assets/LocalBuild/Editor/TechPurchaseFeedbackChecks.cs 的16行测试。
- origin/main=3533141a406940d571e064f3797328c7d214235a；origin/lingguang-v05=95c189e3。两者都是 d8e5677b 祖先，未发现这些分支有遗漏的新提交。
- Claude 连对交付 b53e0a66 是另行移植内容，不冒充现有公开包已经包含。

## 科技彩色文字

Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs 的 CardLabel 已将第二、三行包在 size=12 和 XgDark.Muted 中，第一行按锁定状态使用 Muted 或 Ink。XgTechTreeCopy.Card 本身输出三行纯文本。

剩余彩色文字是分支标题 LaneColor、价格/已购MAX/勾选状态、图鉴回顾入口和悬浮卡条件；图标与边框也按状态着色。这不同于“二三行说明仍是彩色”。不能直接把所有状态色抹掉来声称修复灰色说明。

## 蜘蛛完整链与断链

- XgResidentSpider.Attach 在场景加载后创建组件，Find 获取桌面及控制器。
- TickIntro 要求 SystemUnlocked("data")；XgSim.Features 默认 gradualSystems=true，data 对应阶段3。
- 同时要求程序安装完成、开局结束、至少一轮训练、没有其它过场/全屏视频/心理活动、所需页面引用存在；数据页正在显示或至少12秒无鼠标按下时欢迎介绍。
- IntroScene 将 spiderIntroDone=true。Update 的 present 还检查该标志、剧情独占、XgResident.Present（安装/开局/过场/视频）及是否正使用内嵌爬虫。
- XgResidentMind.Tick 根据 present、quiet、题桌状态决定 Hidden/Parked/Roam/Watch；未发现 spider.walk 或其它购买节点前置。
- 正常新档最早阶段3出现，不是阶段1。gradualSystems=false 的兼容旧档可绕过阶段限制。
- 拖动柄仍要求 ZhongbaoView.Visible && Tab=="label" && Label!=null；但 ZhongbaoView.Label 未赋值，旧 label/teach 入口转到 .exe，实际题桌是 XingGuangView.Teach。发布源码中拖动入口断开。
- 旧 Drag/Drop 工作区是 XgLabelPage.Workspace（题卡答题区域），不是资料文件夹或训练数据库。
- SpiderDragBlocker/SpiderAnswerBlocker 要求题桌开放、对应检查点准确率>=60%、未quiet、host未阻塞；排除复核、关机/家庭/法律特殊题。
- XgLabelPage.Spider 另排除 WorkContext、Mode!=0、shownTeachingEvaluation。成功按题卡后 SpiderDragPress 调 Answer，答对用手答倍率、答错断连并适用罚款。

## 待用户确认

1. 恢复拖入 .exe 教页普通题桌自动答题，还是用户所说“数据库”另指资料文件夹等区域？
2. 当前阶段3剧情自动获得蜘蛛，是否应改为阶段3科技购买？保持阶段3本身不构成冲突。
3. 灰色小字要求已落实；是否还希望调整分支标题/价格状态等彩色文字？未经确认不改已有信息颜色。

以上是源码和发布元数据审计，不等于已启动公开EXE逐项验证。审计未更改正式工程。
