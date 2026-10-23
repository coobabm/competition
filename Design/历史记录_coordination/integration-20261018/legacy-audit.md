# 旧分支未提交改动审查与移植候选（2026-10-18）

## 状态
- 旧工作区 competition-20261011 的 30 个修改文件未动；私人的未跟踪「更新到最新版.bat」也未动。
- 原样完整备份：legacy-uncommitted.patch；逐文件备份 legacy-01.patch 至 legacy-30.patch。
- 对当前 main 30fbe6e4 的意图移植候选：legacy-port-main-30fbe6.patch；已通过 git apply --check。
- 编辑仅在 legacy-stage；没有应用到游戏工作区、没有启动 Unity、没有发布。**尚未编译或实机验收，不应标记完成。**
- legacy-applicability.json / *.main-diff.txt 是 main 同步前的只读初查；移植候选依据同步后的 main。

## 按系统的移植结果
1. SenseBank 增加旧分支 100 道双语题，保留 main 原题。目前实际静态条目计数为 216，不是提示词说的 172；不能为了凑数删题。
2. 群聊：删阿杰众包引流；群提示词改工作两年/独居/网友群。保留 main 提示布局，移除 Regard 按 AI 阶段凭空推断玩家私生活的群和表姐态度；分包通知改到众包弹窗，接线问话仅接线页。
3. 故事：旧群友代操作片段改独白；waitSignal 对应改 wait 避免永远等群消息。保留 main 其他剧情新增。表姐请玩家本人翻译，英文补齐。
4. 贴吧：玩家显式发帖才建立 asked 标志，60 游戏秒后 answered 解锁回复和数据购买；旧已买/已收到的故事数据通过 RepairDataSources 兼容为已请求/已回复，保留进度。
5. 群图：移植 GroupPhotos/PhotoLabel，保留 main 新增事件；8条新配图消息和两条现有消息加标签。重写 main 中六条知道玩家众包/房租/失业/私生活的群消息。图片本身尚未生成。
6. 礼物：下单到自己家，收货后手动送；0/1/2旧礼物状态仍保留含义，新增3待送、4运往自己；heldDay默认0，不破坏旧订单。保留 main 已删除的AI代回，不重新启用它；检查最后一条玩家消息 byAi，防将来代回误触寄礼物。

## 额外修掉旧补丁缺陷
- 单件礼物不再允许仅说「礼物」就寄出，必须说出礼物名。
- 奶茶面板英文改成明确礼物名及送礼动词；中文「请你喝」加入动词表，解决按钮未寄出。
- 礼物列表上限由6到8，避免两种礼物永久不可见。
- 兼容 main 晴雯已不是住校设定：送达写她住处/她楼下，避免回退成学校。
- 保留 main 的 TickBroadbandBill、分批下载实现、无举报版分包提示和双语 firstChoicePick。

## 应用后必要验证与待确认
- 按第六部分顺序，到世界/旧改动移植阶段再应用；Sense补丁可提前单独用于教页。每项由主agent写 Design/整合记录.md。
- 每项移植在 Unity 编译后验证；候选跨30文件依赖较多，不应只应用一半（贴吧flags/模型/界面、礼物Core/UI互相依赖）。
- 新请求将公共数据下载改为科技直接购买到教页，main 的旧下载实现仍存在；本候选故意只加「自己发帖再回复」约束，交由数据系统负责人统一收口，不能整文件覆盖其新工作。
- main 的晴雯AI代回已整体删除；用户只规定「AI不能代送」，因此保留 main 行为且加防护，不凭空复活代回。若需要恢复代回作为单独功能应记待确认。
- main 中 Lili 的配图主题已从旅游换成散伙饭/出租屋等，旧 GroupPhotos 部分标签与 main 新事件不匹配；缺图退回文本，最终13张需按最新配图文档与事件标签统一。
- 本次只覆盖指定30文件。全项目「室友/应届/灵光」扫描、100事件、人设、世界新功能、13张生成图及其署名、性能和其他章节仍未做。
- 给礼物的否定句（如「不送你手机」）仍可能命中现有简单关键字识别；需后续交互验收考虑明确拒绝语句防误送。

## 文件清单与初查
| 文件 | 初查（同步前） | 候选 |
|---|---|---|
| Assets/LingGuangV05/Core/Forum/Forum.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Core/Girlfriend/GirlfriendRules.Logic.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Core/Girlfriend/GirlfriendRules.Money.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Core/Girlfriend/GirlfriendState.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Core/Lang.En.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/DesktopBridge/Taohuo/TaohuoView.Gifts.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/DesktopBridge/Tieba/TiebaHub.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/DesktopBridge/Tieba/TiebaView.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/DesktopBridge/Zhongbao/ZhongbaoPages.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/LocalLLM/LlmPersonas.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Era/era_events.json | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Forum/forum.json | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Story/era_advantage_lines.json | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Story/era_advantage_story.json | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Story/six_stage_lines.json | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/LingGuangV05/Story/six_stage_story.json | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/Resources/Localization/StoryText.json | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Core/XgMemes.Sense.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.DataSources.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.QualityTraps.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Core/XgSim.Subcontract.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Desktop/XgDataUi.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Desktop/XgMarketRelay.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Desktop/XgStoryBridge.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Desktop/XgWiringPage.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/XingGuang/Desktop/XgWiringRelay.cs | 可直接补丁 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/YY2016/YYChatHub.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/YY2016/YYChatView.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/YY2016/YYGirlfriend.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
| Assets/LingGuangV05/YY2016/YYGirlfriendUi.cs | 需按意图合并 | 已在 staging 合并，待 Unity 验证 |
