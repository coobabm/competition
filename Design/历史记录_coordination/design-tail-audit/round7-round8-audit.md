# 第七、八轮设计尾项：只读源码审计

审计日期：2026-10-19。生产目录：`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016`。以下路径均相对此目录。没有修改生产、启动 Unity、运行新测试或将静态检查记作实际 UI 验收。

结论：找到 2 项具体的可选功能缺口，均为 P2；没有从本次范围内找到新的 P0/P1 发布阻塞。第一项有后续用户明确确认，优先补齐。第二项是第八轮原规格的剩余效果，未在已读决策记录中找到明确取消，但不应因此否认已经完成的新闻题桌。

## 1. P2：统一图鉴及能力演示重览未接入两个已确认入口

- 依据：`Design/整合记录.md:246` 要求升阶演示图像可从科技树重览；`:298` 的 #24 明确选择统一图鉴，分能力演示、训练现象、网络结构三类，科技与名字页打开同一收藏；只读重览不重复发奖或剧情。这一确认优先于第七轮原来的六张静态图规格。
- 当前入口：`Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs:450` 的工具栏只有标注、前沿、可买、缩放、适应；`Assets/LingGuangV05/XingGuang/Desktop/XingGuangView.Profile.cs:14` 的名字页菜单在 `:37`–`:39` 只建闪卡、背包、终章入口。
- 当前替代物不等价：`Assets/LingGuangV05/XingGuang/Desktop/XgBoardPage.Atlas.cs:31` 枚举的是结构，`:62` 按已购或训练使用状态点亮；科技页 `XgTechTreePage.cs:1027` 处理的是现象节点，保留了旧收藏，但没有统一三类浏览页。
- 演示机制：`Assets/LingGuangV05/XingGuang/Desktop/XgStageCurtain.cs:23` 仅从待播升阶队列进入，`:34` 构造 `XgStageSequenceDemo`；生产 C# 中该演示类只由升阶过场构造。`Assets/LingGuangV05/XingGuang/Core/XgStagePresentation.cs:13` 播完会移除队列及对应解锁快照，没有重览路径。`Design/data/atlas_stage_images.json` 也未见运行时加载接线。
- 影响：玩家看过或跳过能力演示后，无法按确认方案从科技或名字页重看；现有结构、知识卡收藏不受影响。
- 最小补齐方向：两个入口共用一个三类图鉴界面，能力演示按真实已解锁阶段访问并复用演示组件；不要通过重新入升阶队列实现回看。重览不写奖励/故事状态。此处不是要求恢复已被后续方案替代的旧图片实现。

## 2. P2：时事节点没有把已读新闻接到私人 AI 的 YY 对话上下文

- 依据：`Design/第八轮_桌游训练数据_逻辑判断区_生活分支.md:91` 的时事购买效果包含题桌和“它会在 YY 里聊你读过的新闻”。后续整合记录明确将题桌适配成新闻日期判断，但已读记录中未找到取消后一效果的确认。
- 已实现：`Assets/LingGuangV05/XingGuang/Core/XgStageLessons.cs:10` 注册 `daily_affairs`，需购买 `daily.news`；`Assets/LingGuangV05/DesktopBridge/Bodu/BoduView.News.cs:90`–`:98` 的真实已读按钮提交证据。题桌不是缺失项。
- 缺少消费者：`Assets/LingGuangV05/XingGuang/Core/XgSim.DailyLife.cs:135` 的 `DailyNewsQuestions` 只供题包购买门槛使用；`Assets/LingGuangV05/XingGuang/Desktop/XgYyTalk.cs:393`–`:397` 注入 `PersonaKnowledge`，而 `Assets/LingGuangV05/XingGuang/Core/XgSim.Knowledge.cs:82` 在阶段 3 前直接返回空，`:137` 新闻检索来自按日期过滤的固定 `Rag.Era`，没有消费 `daily.news` 或已读文章 IDs。
- 存档边界：`XgSim.DailyLife.cs:14` 只有解锁凭证 `dailyReceipts`；`:92`–`:99` 达到条件上限便停止收集；`BoduView.News.cs:91`–`:92` 在五篇后关闭新增已读确认。因此不能把现有五篇解锁计数器当作持续新闻阅读历史。
- 现有相近功能不等价：`Assets/LingGuangV05/XingGuang/Core/XgNewsCatalog.cs:54` 的 `TeachNews` 通过玩家主动“转给它看”写入 AI 记忆，但只支持特定科技新闻、与购买时事无关；固定时代知识也不是玩家的已读记录。D028 主动给老周分享是另一条路径，不应改为自动披露。
- 影响：买时事确实解锁新闻题桌，但不会新增能引用玩家已读新闻的 YY 能力；尤其阶段 2 的时事节点与当前阶段 3 才启用的通用知识检索不相接。
- 最小补齐方向：若保留原效果，明确以实际已读/主动交给私人 AI 的文章为来源，并让该节点有对应聊天消费者；需要持续记录时，应与五篇解锁凭证分开。不要让老周或群聊自动获得玩家阅读历史。

## 已核对、没有重复列为缺口

- 软件获得：下载/安装、持久脚本进度、跳过、实际链接、待办均存在；游戏中心预装、私人 AI 对手阶段 2、记录器仅管训练包符合后续确认。没有把旧档保留已获得软件权限当作新档阶段绕过。
- 日常：自己的帖子收到回复、新闻实际阅读、喵鱼购买及折扣、晴雯发送计数/已知日期、群斗图帮助、歌曲完成、棋局完成、概率和新闻题桌均已找到消费者或 UI 接线；旧缺失 daily 分组已经修复。
- 采用最新规则：概率条件为与私人 AI 完成任意棋类共 3 局，装备容量 4→8→12，生活事件 8/15 分钟/关闭；不拿旧轮次数值重复报错。
- 日常提示队列仅内存保存，换档/重载清空，但 `XgDailyFeedbackTests.cs:10` 明确约束旧完成条件不回补通知；本次不把它直接判作未经确认的产品故障。解锁凭证与购买状态本身可保存。
- 飞行棋同格点击候选已由主控合入；不重报旧重叠按钮问题。有限棋盘截图、现实模型导出、长存档性能及主控正在复验的通知/新闻/终局 UI 不属于本报告新增发现。
