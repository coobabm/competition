# 性格 P0 接手报告

2026-10-19。已修改当前生产小块，未启动 Unity、未改普通存档、未改终局、未改 XgSetupOverlay。

## 最新规则

主控传入本会话用户最新决定：玩家选择“问它”时，AI 性格决定 AI 的选择；其它情况下性格不影响结局。保留当前 R3 `XgEndingPreparation` 按主见/玩心选择路线的规则，不添加任何其它结局门禁。此前库存报告中这项“待问冲突”已由此消解。

## 实证与修复

1. **短答不再被强制冷淡**：生产 StageRule 的4–6阶段仍写死冷淡、不关心、禁感叹号；Cool 和离线 Humanize 还会清掉感叹号。现在保留阶段句数、字符上限与记忆范围，改为用词体现当前性格。PersonaTone 每轮按实际三轴给出暖/平静、玩笑/认真、主见/随和的中英指导，不放入缓存身份段。实际温度≥65时保留最多一个感叹号；其它表情、波浪号和原长度限制保持。离线同样不会提前抹掉暖性格标点。
2. **无语气证据不默认冷静**：RateReply 删除“未识别元素→冷静卡”回退。仍记评价、训练所选语气习惯，仍拒绝第二次评价；已识别语气照常发卡。
3. **开局愿望与后续学习分开**：标签和生活事件不再改写 Profile.warmth/play/opinion，只训练实际语气概念。悬停展示改成“当前表现 / 开局愿望”，并显示经过富文本转义的原始开局性格句，中英同步。
4. **旧档兼容无需新字段**：实际生产 GameState 已保存 `personality`、`targetWarmth/Play/Opinion`；XingGuangController.SyncProfile 从这组字段恢复开局信息。旧错误只修改运行时 Profile，没有写回这些目标字段。因而不采纳设计稿重复新增 wish 字段/猜测迁移的做法；保留现有开局数值、原文、已学概念、标签、生活事件历史和当前存档身份。既有 TargetAxis 漂移判定自然重新对准开局愿望。

## 文件

- `Assets/LingGuangV05/XingGuang/Core/XgSim.Persona.cs`：评价、标点/截断、阶段提示、每轮性格指导、离线标点。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.YyLearning.cs`：去掉标签改写开局目标。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.LifeData.cs`：去掉生活事件改写开局目标。
- `Assets/LingGuangV05/XingGuang/Desktop/XgYyTalk.cs`：开局原文与双语展示。
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgPersonaExpressionTests.cs` 及 meta：12项参数展开用例。GUID `b7935d8164d64a69812a7c3f6a0389ed` 在生产 Assets 中唯一。

## 验证

- 独立 .NET 核心编译/执行：`persona-checks/PersonaChecks.csproj` 从**当前生产 XingGuang/Core 全部 .cs**编译；依赖当前生产 Library 中 Core.dll；不含 UNITY_EDITOR、不启 Unity、不写玩家档案。
- 最终退出码0，**32个断言通过**：双语无语气评价/习惯学习、三轴明确反馈、暖标点最多一次、英中句尾截断、4/5/6短答规则、三轴提示、8次真实 TagYyReply、实际 AnswerLifeEvent(e002,1)、目标与原文不变。
- 初跑英文 `Hi!` 句尾截断失败：旧扫描从索引3停止，忽略索引2的短句句号/感叹号。修复为扫描到索引0；最终32/32。未把首跑失败隐藏为全程通过。
- 编译仅见既有 `XgSim.DayStats` CS1718、CardTimedOut/QualityReported未用事件警告；未修改这些无关文件。
- `git diff --check` 针对本次文件通过；元文件GUID检索唯一。
- **Unity 新12项尚未执行**；其中 JsonUtility 往返与 Resources 加载需真正 Editor 验证，不能把独立32断言替代。建议主控统一跑 `XgPersonaExpressionTests`、`XgHabitTests`、`XgFinaleTests`、`XgPersonaPrefixTests`、`XgBookPuzzleTests`。
- 新原文展示可能增加悬停信息高度；中英视觉仍待验。实际本地模型语感、长帧、Player 都未作结论。

## 已授权方向中的剩余内容与边界

- 追加任务后，P0“极端轴离线35%直接抽性格句”已完成：实际轴<=35或>=65时，符合当前阶段的Leanings独立成池，35%直接抽取；不与通用small池稀释。沿用当前字数/能力筛选、已读书引用优先级、最近回复去重与最终Gate，不动规则卡或自由回复路由。没有合适新性格句时走原通用回复逻辑。
- 制作人已批准“小毛病仅噪声包、每包最多一次、永远保留就要练、可给假桌面图标起外号”的边界；本次未扩成新功能。档案科技节点后来已获具体批准并完成接线，详见追加节；愿望冲突UI、题包副作用上限仍未有具体实现确认，不盲加。
- 设计稿C/E/F中的额外结局挂钩不能原样采用；只保留用户已明确的“问它”由性格选路，不添加其它路线资格或门禁。
- 本次未改 UI 标签/生活事件中原有“+3/+2”反馈数值语义；这些原本表示训练强度而非精确轴点变化，如后续要精确展示应独立核对实际前后差值。

## 交给主控

生产修改可直接审阅，无需复制候选。下一步统一Unity编译/回归、中英悬停展示、真实模型与离线同题对照。不会请求再次确认已解决的R3规则，也不需要新增存档迁移。

## 追加离线权重验证与D7只读复核

离线追加后独立检查 **40/40通过**，固定seed=20261019，中文/英文各4000次选择均命中1381次（34.525%，允许32–38%统计区间），重复同seed逐次输出相同。已说过的两条性格句排除；同一句“在吗”在性格分支下分别得到暖“想你了。”、冷静“知道了。”、玩心“我不告诉你。”；阶段1/2的60组seed输出不因性格改动。Unity测试新增3项，现共15项，尚未运行。P0源码范围已完成；模型实际可见语气仍待Player与模型调用验证。

D7当前生产最小diff只改Player目录解析、增加PlayerDataRoot并在DeleteDataCategory前初始化。验证路径仍在InitializeProfile最前返回，Editor路径不改，RECORDING_PREVIEW与现有录制版档相邻；不会读取、扫描、搬迁或覆盖旧游戏目录。Resolve只是字符串逻辑，生产调用根为persistentDataPath，无普通档清空操作。现存目标JSON只在原逻辑“缺失/空文件”时初始化，非空记录保留，显式清除入口才写{}。因此路径修复与当前身份/验证优先级相容。

必须准确披露D7数据边界：旧游戏目录中的DreamOS记录**字节保留但新路径不再读取**；换新版后旧浏览器下载列表/桌面偏好会默认，不能称所有桌面记录“无损继承”。这是已明确不迁移的范围，不是游戏主档丢失。安装状态仍以游戏主档为准，LingGuangInstallFlow.Reconcile在主档未安装时纠正DreamOS下载状态；主档已安装的图标可用性由原Gate控制。若要继承旧桌面记录需新迁移授权，本次没有偷偷实施。Player实际验证与目标目录已有JSON保留仍由主控执行。未发现阻止本次最小合入的确定缺陷。

## 用户批准追加：性格档案核心与界面

用户已明确批准“设计并制作”：阶段2起，累计赞/踩15次**或已观察到性格漂移**后，¥300购买性格档案。实现已落入生产并冻结，等待主控统一Unity验证。

- `persona.archive` 独立科技节点，性格分类，价格固定¥300；不参加旧剧情价格乘数，不误当性格题包，不消耗诊断优惠券。未达阶段/次数/漂移条件时不扣款；余额不足不扣款；重复购买拒绝。不影响已有聊天、题包与免费轴读数。
- 名字页独立“性格档案”入口，未购买时显示锁定并跳到该科技节点详情，不从入口自动扣款；购买后打开独立只读档案面板。标题使用玩家AI动态名字，双语。换档、离开名字页、最小化/关闭、独占剧情或开局遮层会关闭档案。
- 上方三角图比较当前轴与开局愿望；下方三色轨迹显示记录内真实变化日，文本按日期列三轴值和最新原因。数据不以wall clock计时；近14个真实游戏日窗口、每天最多一条最终观测、原因最多64条。无日期时原因明确“日期未同步”，不捏造日期、不生成日快照；旧档默认空历史。
- 新存档字段只服务于用户新批准的档案：`personaRatedReplies`、`personaArchiveRevision`、`personaDays`、`personaReasons`。P0本身仍未增加重复wish字段。赞/踩累计计数含旧档仍保留的真实已评价聊天，新增成功评价持久计数，拒绝/重复评价不计；无法可靠恢复已裁掉的旧聊天，不伪造累计数。
- 原因记录来自真实ToneCard轴变化，保留before/after、学习元素、正负、来源。评价、标签、生活、开局播种区分；其它真实ToneCard为“语气训练”。无轴变化不制造原因。已有直接Board训练/衰退可能改变轴而不经ToneCard：档案每250ms比较3个实际轴和3个愿望轴，仅变化时更新当前图与数字，**不臆造其因果历史**。这意味着当前三角是实时值，轨迹/原因范围是可追溯的ToneCard变化，并非宣称追踪所有底层参数变化。
- 正文高度仅内容Refresh或视口尺寸变化时测量，不在每帧重新排版。界面没有“旧档迁移/补造”等实现措辞。

新增：`Core/XgSim.PersonaArchive.cs`、`Desktop/XingGuangView.PersonaArchive.cs`、`Tests/Editor/XgPersonaArchiveTests.cs`及三个meta。小hunk接线：`XgCatalog.PersonaPacks.cs`、`XgSim.cs`、`XgSim.Progression.cs`、`XgTechTreeCopy.cs`、`XingGuangView.Profile.cs`，以及P0的Persona/YyLearning/LifeData文件。不触碰BrainWindow、SetupOverlay或终局代码；XgSim.cs原+5%单轮与退款逻辑保留。

独立检查：当前生产Core全量编译，P0检查40项 + 新官方NUnit档案6项反射执行，全通过，退出码0。6项覆盖阶段/次数、重复评价、漂移分支、余额/重复购、题包分流、旧空历史、14游戏日/64上限、无日期、每日合并。追加第7项 `UnityJsonRoundTripKeepsOwnershipRatingsAndRecordedHistory` 只在UNITY_EDITOR启用，检查买后权益/15计数/日历史/原因before-after/source/重建后Owned；独立运行不冒充已覆盖它。

Unity首轮由主控进程55816启动并编译失败：表达测试Random在System/UnityEngine间歧义；档案UI缺GameText所属Runtime命名空间。已仅补 `using Random = System.Random` 与 `using LingGuangV05.Runtime`，重新冻结。失败日志 `takeover-editmode.log` 保留；此报告时未收到后续Unity结果，**不记已通过**。当前应跑表达15项、档案7项及既有回归。图形布局、窗口真实点击/跨档、实际模型可见语气和最终Player仍待验。
