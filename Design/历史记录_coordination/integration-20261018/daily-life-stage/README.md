# C7 日常条件与真实行为候选

状态：仅coordination候选，未写生产、未启动/编译Unity、未提交git。依据C7交接与整合记录#29/#27补充/定价确认。基线是主代理当前本地main（bf354fdb+已整合事件/C1/C2/C3工作区）；每个既有文件原始hash在manifest。

## 规则

7个节点使用daily.dice/news/surf/bargain/care/slang/rhythm，阶段2/2/2/3/3/3/4，普通价格200/150/120/250/200/180/300，合计1400。节点定义只提供给协调者目录集成，不在候选私自注册科技或改任何主线条件。DailyNodeReady/Blocker给阶段+真实行为门槛；所有已有主线不依赖日常。概率题效果与风险增强分开；DailyRiskEnhancementAvailable必须传入实际风险预报购买状态，不要求先买风险预报才能得到概率题。

持久化字段XgState.dailyReceipts保存condition/eventId/subjectId，稳定eventId重放不再累计。每条件只保存达到阈值需要的凭证，最大36条，这是解锁证据而非完整活动历史；不删除C3历史。game1与games10共享同一证据桶，board独立，先下10局五子棋后再下飞行棋仍可达成board。旧总局数、拥有显卡、gfReplies、开窗口都不补写。查询不修改状态；旧字段缺失按0。没有静态跨档计数。

## 节点→真实事件→字段→效果接口

|节点|完成事件与当前接线|存档condition|效果接口（只查询，不自动结算）|
|---|---|---|---|
|概率直觉|HubGame首个真实落子绑定当前lab及局GUID，正常Finished且私人AI为对手；现有五子棋/围棋/国际象棋提供game1/games10；飞行棋/跳棋/大富翁尚无运行时游戏，board保留明确接口|board、games10|DailyProbabilityQuestions；DailyRiskEnhancementAvailable|
|时事|RecordDailyNewsRead接受实际读完后的文章ID/完成ID；Bodu当前只有展示和转交，不把打开算读完，待root定义并接完成动作|news5（5篇不同文章）|DailyNewsQuestions|
|冲浪|TiebaHub.Receive实际提交他人回复后，且thread作者是玩家；普通他人帖子/私信不计。旧help/ask静态楼层按日期显示不是新Receive，不从刷新补算|tieba|DailyDataLinkHighlight|
|砍价|MiaoyuView中BuyUsedCard实际成功返回后；失败、取消、仅浏览、卖卡、淘货新品不计|miaoyu|DailyBargainingAvailable|
|体贴|YYChatHub.Send或SendAuthoredChoice实际添加玩家文本消息后；晴雯收到的剧情消息不计；空白、贴图、其它联系人不计|gf（10句实际发送）|DailyConsiderateTag、DailyKnownDateReminders|
|网络用语|YYChatView.Faces将真实stickerID传入新增Send三参重载，消息提交时已带贴图属性；仅netbar群计|memes5（5次发送，可同图）|DailySlangHelp|
|节奏|GameMusic已存在自然结束边沿/单曲循环末尾；XgDailyPlayback核对从开头连续进度，跳到尾部不计；切档丢弃当前播放证据|music3（3首不同歌曲）|DailyRhythmFeedback|

## 接线细节

- YYMessage.dailyEventId随YYState保存；只在新的真实发送时生成。已有两参Send保留，三参用于贴图，消除发送后倒序找旧消息再补sticker的歧义。现有Touch标脏；不改变聊天回复、好感、送礼和剧情。
- 游戏绑定首个真实落子的lab引用；读另一档后旧棋局结算不给新档条件。局GUID在本轮结束前不变，Finished后清除；游戏板本身当前不存档，不能将不曾恢复的旧棋局回放为新事件。原游戏奖励/样本代码未改。
- 音乐只记录证据，不控制付费、暂停、训练或播放。仍允许普通听歌。以clip.name作为当前曲目稳定键，名字重复的两个素材会保守算同一首；未读取玩家真实文件内容。进度采样容差为一个真实帧+50ms、至少150ms（音频采样边界），不是任意95%即完成。大跳进度拒绝；正常暂停后接着听可完成。音频设备中断、极端长帧和本机导入同名曲目需要root实际验证。
- 节点查询不自动发通知；root在真实条件跨阈值且当前阶段可购买时接托盘一次提示。购买/目录/题桌/生日纪念日真实日期/砍价按钮/节拍反馈消费由root负责。没有实现未经确认的风险准确度或经济倍率；砍价−5%仍需协调者核实批准口径再接，候选不会自动降价。

## 验证与剩余

`dotnet run --project Checks.csproj`：70项通过，result.json。涵盖真实核心状态往返、重复事件、跨档、旧档不猜补、价格总和、阶段、无主线依赖、无奖励、阈值饱和与有限证据、全曲/暂停/跳进度/切档采样。生产既有DayStats自比较和未使用事件警告未改。

8个候选源文件见manifest及daily-life.patch；桌面源仅静态核对，尚未Unity编译/真实LCD或音频验收。特别是3个新棋类当前没有可运行入口、新闻没有读完动作、静态论坛楼层没有到达事务，不宣称7条件已全部在游戏可达。当前“3首”按不同歌曲保守实现；若产品希望重复同曲3遍也算，应明确调整DistinctSubjects，仅有自然播放完成才可计。

## root科技集成契约

直接以XgDailyLife.Nodes追加普通价格节点，绕开StagePrice，不设主线parent/needs。XgSim.Status在已拥有判定之后、资金判定之前检查DailyNodeBlocker(n.id)；Why显示该双语blocker。节点购买仍使用既有Spend/保存事务。别把NodeReady当成免费拥有，不向已有主线添加daily.*依赖。共享目录由root串行修改，本包没有覆盖目录源码。
