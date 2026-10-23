# 科技升级包覆盖审计

2026-10-20；范围：competition-claude-integration-20261020 当前实际源码，只读审查。以下“有实现”不等于全流程实机验收。路径以工程根为基准。

## 已找到明确映射

|需求|实际节点/入口|证据|
|---|---|---|
|开练、脑窗、AI代答|train.curve / brain.window / label.ai|XingGuang/Core/XgCatalog.cs:466–468|
|练页购买分块|train.holdout、shared.lr、train.checkpoint、train.monitor、train.weights；train.data、记账本|XingGuang/Core/XgTrainingCatalog.cs AddNodes、TrainPart|
|19个题包|19项 XgTeachingPacks.All 与 Resources/LingGuangV05/Teaching/minigame_packs.json 的19项对应；XgPackGames.cs 有具体玩法工厂|XingGuang/Core/XgCatalog.DataPacks.cs；Desktop/MiniGames/XgPackGames.cs|
|题包购买/安装|10个buy=true项生成 pack.<id> 科技节点；购买后仍需安装；9项buy=false按阶段条件开放|XgCatalog.DataPacks.cs AddTeachingPackNodes；XgSim.DataPacks.cs:51–52|
|8性格题包、9标签|persona.<packId>动态注册，加 persona.archive|XgCatalog.PersonaPacks.cs；Resources/.../YY/persona_packs.json；XgSim.YyLearning.cs|
|12对话解锁|JSON有12项；玩家原句全字匹配且成功收到回答后记录，再允许节点|Resources/.../YY/chat_unlocks.json；XgSim.YyLearning.cs RegisterYyReply/ChatNodeReady|
|24道具|XgRelics.cs 实际24个R定义；练页右侧稀有道具按钮→backpack|Desktop/XgTrainPage.Relics.cs；需FeatureOpen(backpack)&&FeatureOpen(home)，不是仅假按钮|
|两次高价扩容、最多12|relic.slots8：阶段3/12000；relic.slots12：阶段4/48000，前置slots8；容量4→8→12|XgTrainingCatalog.cs；XgSim.Relics.cs；XgRelics.Slots=4, MaxSlots=12|
|算术升级|幂根式、微积分、自动打地鼠均有独立节点|XgTrainingCatalog.cs；基础算术题包arith_mole免费阶段1|
|图书馆|books.library 阶段2/500；LibraryOpen=Has；购买记录/完成记录持久化|XgTrainingCatalog.cs；XgSim.BookLibrary.cs；XgBooks.Library*.cs|
|资料文件夹与安装|train.data 节点；CanInstallData=Has；桌面名称资料文件夹；下载指引→练页安装|XgSim.DataFiles.cs；Editor/ChapterOneInstaller.Files.cs；XgGuide.cs|
|对局记录|gamedata 阶段2/500；购买后新AI对局每3局产包，安装后生效|XgSim.BoardRecords.cs|
|逻辑4级|board.logic1–4 与 nextmove；第4级要求gamedata和与AI任意3局|XgSim.BoardLogic.cs；依赖MLP/深度、LeNet、LSTM、gamedata|
|7日常节点|daily.dice/news/surf/bargain/care/slang/rhythm|XgSim.DailyLife.cs；真实事件收据条件，日常节点无主线parent，3局/5新闻/贴吧回复/喵鱼交易/10句/5斗图/3首歌|
|YY题本/性格|YYChatView.Learning.cs 真实两个按钮与抽屉；会跟随存档和记录更新|YY2016/YYChatView.Learning.cs|

以上路径未写 Assets/LingGuangV05/ 前缀的均在该目录内。

## 确定缺口/不一致

1. **三级爬虫科技购买没有对应注册节点。** 当前居民蜘蛛是阶段3数据系统开放后自动介绍；爬网页/阅读已有实现，但未找到购买出现、加速、自动翻页三个技能书节点在 XgCatalog/XgTrainingCatalog 中注册。不要把旧爬虫功能写成三级升级已完成。需用户确认采购门槛后落实。
2. **蜘蛛拖题旧入口断链。** XgResidentSpider 仍指向已移除的 ZhongbaoView.Label/label tab，实际教页已移到 XingGuangView.Teach。详细条件见 upstream-audit.md；此项不靠加科技节点就能自动修好。
3. **“数据回顾”付费节点与YY“数据”右侧页未找到对应完整入口。** 交接文档 Design/ui/exe_UI_v4_交接_给GPT.md 第11条明写该项；当前YY Learning工具只创建题本/性格两个按钮，题本已有持久学习记录检索，但不能冒充已实现另一个付费数据页及拖题考AI入口。需统一是否用现有题本承载，还是补原定付费拓展；不自行合并删除。
4. **19题包不是19个可买节点。** 10个付费节点：make24/headline_swipe/mnist_write/chess_mate1/captcha/meme_word/poem_next/chess_mate2/review_swipe/chinglish；其余按buy字段开放。以实际代码字段为准，不把免费包误判丢失。

逐项统计buy字段为 true 共10项、false共9项。免费包：spam_swipe、arith_mole、minesweeper_yn、odd_one、sequence、danmu_mood、gomoku_win、idiom_chain、spy。

## 覆盖边界

- 这是当前可定位的节点/入口覆盖，不是宣称所有历史策划全部完成。
- 图书馆确有目录与购买实现，本次未逐本统计90中/10英或验收全部书籍许可。
- 主线/日常不互当前置、所有小游戏可玩性和19包在各阶段的自然获得过程仍需运行态验收，不能仅凭节点数组证明。
- 不修改游戏、不新增价格/节点/门槛；待确认项目交主线程询问。

