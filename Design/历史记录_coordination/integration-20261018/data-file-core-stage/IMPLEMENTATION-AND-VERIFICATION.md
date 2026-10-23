> 最新规则已覆盖本文早期临时回调/待确认描述：请先读 APPROVED-ENTRY.md。正式train.data节点（curve之后¥50）已在候选落地；DataInstallAccess临时回调已删除；核心门槛直接读持久科技所有权。以下保留前阶段审计过程，非当前接线合同。

# 虚拟资料安装链：可执行分步方案与核心候选

## 状态
只在本暂存目录写候选与patch。生产未改；未编译、未运行Unity。`git apply --check --ignore-space-change`通过。数据入口门槛尚待用户选择；本补丁不新增datatab，不改变购买节点/价格，也不在安装API内硬编码¥50门槛。最终安装检查使用统一运行时授权回调，未绑定默认拒绝。

## 第一批候选（data-file-core.patch，6个现有文件+1个新核心文件）
- XgSim.DataFiles.cs：持久已下载文件库存、已安装批次/垃圾包/题桌节点、版本迁移，单文件与批量安装API。
- XgSim.cs：数据节点购买排下载；Owns和特殊数据可用性按安装；待安装的传统桌不再提示重复购买。
- XgSim.DataPacks.cs：购买19包对应付费节点后排队，待装门槛只作用新购付费包；免费题桌不受影响。
- XgSim.Downloads.cs：唯一fileId/nodeId、完成进库存、不再结算样本/开桌；保留原队列进度和磁盘统计。
- XgSim.DataSources.cs：样本/噪声/额外包拥有/开关均按安装状态。
- XgSim.LifeEvents.cs：坏道事件只选已安装数据，同时扣安装量，防后来下载未装就恢复被损坏的样本。
- XgSim.Progression.cs：修复Has(dataset.pack)即开放桌的旁路，保留原无owned的免费里程碑资格。



## 新状态与UI接口
`XgDownload`新增：fileId（稳定虚拟文件ID）、nodeId（购买来源节点）、installed。现有dataset/offer/batch/batches/sizeMB/doneMB/samples/campus/ownDrive仍使用。
`XgState`新增：dataFilesVersion、retainedFreeData（原免费授权）、dataFiles（下载完的文件，包含已安装文件）、installedDataBatches、installedDataOffers、installedTeachingNodes。
- 下载中：S.downloadQueue；DownloadQueue()副本新增fileId/nodeId。
- 已下载：S.dataFiles；原download对象移入，原fileId保留。
- `DataFileReady(id)`：已下载且未装。
- `InstallDataFile(id)`：成功true；未知/下载中/已装false。
- `InstallDataFiles(IEnumerable<string>)`：输入去重、固定选集，返回安装成功个数。
- `InstalledBatches(dataset)`、`DataOfferInstalled(offer)`、`TeachingNodeInstalled(node)`。
- `DataFileInstalled(fileId)`事件：状态已提交后触发，UI可刷新。
- `DataInstallAccess`运行时授权回调、`CanInstallData`、`DataInstallBlocker()`：未绑定/false默认拒绝，单个/批量安装最终都检查。正式运行时按用户批准规则绑定；资料UI只能读取门槛，不得自己赋true。
资料窗口仅使用这些游戏内ID；不接受操作系统路径、不读真实桌面。

## 结算和兼容原则
1. downloaded(batchesArrived)继续代表磁盘字节与下载进度；installedDataBatches才代表训练可用量。下载完成不加样本、不改题桌。
2. 普通Dataset购买保留当前“整包”权益和价格，不悄悄改回更贵的旧分批首包。后续旧存档仍可继续BuyBatch；已有宽带/磁盘/加速渠道保留。
3. 付费小游戏节点必须自身文件装完才可用；多个节点共享同dataset时，总样本上限仍为原整包，不乘重复份数。无需编造额外小游戏收益。
4. 旧档版本0迁移：已有batchesArrived照原量记作installed；无批次数老整包仍按原整包；下载中垃圾包不记已装、已到垃圾包保持；旧已买pack.*节点记已装，原desksOpen从不撤销。旧队列doneMB与购买价格不改，迁移后首次完成需拖装。
5. 旧免费里程碑只有Has(dataset.pack)但没有owned的资格继续有效；新购买会设置owned，因此不再借该通道绕安装。
6. 文件重复拖放和重复批量ID不重复奖励；新旧同dataset整包到达/安装都封顶。已安装文件保留状态便于资料查看。
7. 迁移不虚构历史下载文件；老已装包可由UI按原owned/batches记录显示“原存档已安装”，不能编造下载时间。
8. 坏道损失不会被“新批次下载完成”提前恢复；需要再次安装才恢复样本。未装包不作为本轮坏道损失目标。

## 旁路逐项审计
| 路径 | 第一批处理 |
|---|---|
| BuyNode(Dataset)直接InstallFullTeachingPack | 改QueueTeachingData，先获取原下载量再设置owned，避免新owned无计数被旧回退规则当整包 |
| ApplyTeachingPackPurchase直接开桌/填批次 | 改队列+nodeId，TeachingPackBlocker加安装检查 |
| Arrive下载即Evaluate/CheckDesks | 只存入文件库存；安装时才Evaluate/CheckDesks |
| PackSamples公共包 | 按InstalledBatches |
| 垃圾包下载完成即计样本/噪声 | DataOfferInstalled检查 |
| PackNoiseRows公共/故事包 | 按已装批次 |
| Owns、OwnsExtraPack | 已装才可用于训练；购买授权仍由原owned/Has检查防重购 |
| PackSwitchBlocker | 未装不能切入训练 |
| ProgressionDeskAvailable Has(.pack) | 仅保留无owned的旧免费授权；新购买依赖Owns |
| translate/crosssentence特殊可用性 | 同上，保留老里程碑 |
| imagenet前置读取owned | 改Owns，下载未装不满足 |
| StoredDataMB/BatchesArrived/UI进度 | 故意保留已下载口径，待装文件仍占磁盘 |
| 旧故事数据/垃圾数据购买 | 沿用队列，完成统一进入库存；不改原主动贴吧触发/付费来源 |
| 坏道损失 | 同时扣安装计数，避免新下载把旧损失自动补回 |
| 教学答题、手写、普通小游戏、ExtraSamples、书籍、真实对话 | 保留原收益；不是下载文件，不可统一拦截误删收益 |
| 旧蜘蛛CrawlGrab/ResidentRead即时收益 | 未在本批改写；新采集包接线需单独按已确认蜘蛛设计处理，不能同时保留即时收益再加安装包重复结算 |

## 后续执行顺序
1. 主线程审阅本候选并加核心检查（下表），按Unity串行节奏编译；补齐边界后提交核心模型。未接UI前不能对用户宣称完成或发可玩包，因为新购买文件没有生产安装入口。
2. 引入分档模拟时长策略：小包数秒、大包15–30秒；当前候选刻意仍保留旧计时，不假称节奏已实现。教学包无真实dataset（如chess）当前sizeMB=0只表示没有已定义样本/文件尺寸，UI接线前必须用独立体验时长保证可见下载，不能捏造真实文件尺寸。
3. 用户确认入口后映射来源栏门槛；加入Win7资料窗口/桌面与任务栏入口、AI命名、目录预览、真实日志/检查点读取。旧已装文件以明确迁移来源展示。
4. 接单选/Ctrl多选/全选可装，整练页Drop overlay调用安装API；在UI与核心共同确定来源栏解锁检查，不能仅视觉置灰导致绕过。
5. 虚拟URL白名单与能力前置，进入同一Enqueue/DataFiles生命周期；任何文本不发真实网络请求。下载完成任务栏闪与提示音。
6. 核对老下载提示文案、网页/淘货/宽带tip中的“直接在教使用”等旧说法与全部本地化；现阶段仅改核心中冲突反馈。
7. 真实隔离Main验收、存读档、错误路径、截图，更新整合记录，再进入可运行包交付。

## 核心验证方案（必须实际运行后才能记PASS）
使用纯模拟独立state与内存Wallet，不读真实存档；同一套检查另做JsonUtility往返。
1. 新购实际Dataset节点：购买扣费一次；样本在购买/下载期间/下载完成未装三个点均不增加；ready true；安装后增加原整包样本，重复安装false且资金/样本不变。
2. 每个19包付费节点：原stage/needs合法时购买，在TeachingPackAvailable保持false；下载后仍false；安装后true；所有免费包原门槛结果不变，玩法类型未替换。
3. 同dataset两个付费题桌：分别下载/安装，两桌各自按nodeId开启；整包样本只到原上限。
4. 公共旧分批/故事分批：第1批下载后0可用，装后只该批；第2批下载不增，装后再加。公共/故事不会同时购买相同授权。
5. 垃圾包：下载完成前后未装均0额外样本/噪声；安装后按既有samples/noise生效；开关排除仍正确。
6. 混合批量：ready A、ready B、重复A、下载中C、未知ID、已装D，只成功2，收益等于A+B且逐ID仅一次；回调改变UI选集不影响当前固定批量。
7. 原无下载系统旧档：拥有整包、现有题桌、原收益/模型进度完全相等；新schema重载再次相等，无回退待下载。
8. 原有批次旧档：已到2/买3，第三批doneMB=37%；迁移已装2、第三批进度37%；完成仍2，安装才3；重复读档不重新扣款。
9. 旧垃圾包完成与下载中各1；已完收益原样，未完不获收益；旧已买小游戏节点仍可用，旧免费translate/crosssentence仍可训练现有样本。
10. 购买→中途存档→完成→未装存档→安装→已装存档五点往返：fileId稳定；无重复下载/收费/样本；库存不重复。
11. 正常损坏一批：安装量下降；重买下载完成仍下降；安装恢复一次；待装文件不误选作损坏已训练数据。
12. 关闭包开关/训练进行中：样本与噪声开关按原规则；是否允许安装影响活动轮次需主线程沿现有下载期更改样本行为验收，若需禁止应加明确规则，不能偷偷改训练结算。
13. Invalid save：null集合、重复fileId、无效file对象/负数尺寸均不崩；不能通过重复JSON文件条目制造二次结算。既有可识别的未知未来内容不擅自迁移为可用样本。
14. 购买价格/NodeCost/19种玩法/既有labels和ExtraSamples不变的对照检查。

边界：未编译的候选不等于可运行；数据文件接口已有默认拒绝的统一授权检查，尚未绑定来源栏正式购买规则，必须与后续UI门槛同时验收。没有用户权限/价格确认时继续做此不依赖决定的模型与检查，不自行新增付费规则。

## 同数据集与版本0专项推演（静态，待实际检查）
- 旧买3批/到2批/1批在途，再买同dataset付费教学节点：QueueTeachingData先读取到达2，再将购买权益提升整包，保留旧队列、不清已到2；旧在途到达变3，新全包到达封顶all。两文件可任意顺序安装，每次InstalledBatches也封顶all，不可2+1+all重复累计。
- 两个教学节点同dataset：各文件nodeId不同，安装A不能开启B；dataset收益封顶，故A/B均完成不会双倍样本。batchesBought=all是保留现有整包权益，并非追加收费。
- 非pack.*的旧dataset.pack节点：迁移按原batchesArrived保留installedDataBatches；同时该节点存在且已到至少一批则记录安装节点标记。只有Has但无owned的旧免费里程碑沿原特殊通道保留，不给整包样本。
- 新档version0集合为空：迁移只复制既有量，不写任何凭空owned/已装节点或批次；后续初始免费桌保持原来逻辑。
- 新schema存档只凭installedDataBatches/installedDataOffers提供收益，不重新从owned推导整包；因此下载未装重载不会自动获得收益。
- 核心安装方法使用CanInstallData gate，当前用户尚未回答的入口规则由后续正式运行时提供DataInstallAccess回调；未绑定默认拒绝。UI只显示同一检查原因，不能每个窗口自己各写一份检查，也不得UI自己赋true授权。

## 已暂存可执行核心检查代码
DataFileCoreChecks.cs（Tools/Local Verification/Data file core / DataFileCoreChecks.Run）已写，未编译未运行。只构造内存XgSim与Wallet，JSON字符串往返；不加载真实存档。
- 真实BuyNode与重复购买扣费校验；真实TickDownloads→Arrive路径；未装不加收益、重复安装不加收益。
- 混合批选重复ID去重、每文件仅一次通知、原免费桌不受门槛影响。
- 老整包/旧在途37%进度/已到2批再买同dataset整包，分别安装不超过原总样本。
- 同dataset的chess_mate1/chess_mate2分别安装，不能互相提前开桌，不编造chess样本。
- 垃圾包实际购买、噪声/样本延迟到安装；重复JSON文件ID和null记录修复。
- 坏道调用真实ApplyLifeChoice结算函数（明确不测试随机事件调度），下载不自动恢复、安装才加回一批。

资格fixture通过设置阶段与直接授予前置来隔离核心安装行为；不是自然成长通关证据。测试下载时反射调用真实TickDownloads，以免无关日历/房租/剧情扰动该核心验证；不反射调用Arrive伪造下载完成。尚待主线程编译后运行，当前无PASS计数。

## 静态复核后修正
- 旧免费数据授权增加retainedFreeData持久保留：原translate等仅有dataset.pack免费授权，随后买对应教学节点造成owned新增时，不能撤掉旧训练资格。只保资格，不产生付费样本。新增LegacyFreeGrantStaysAvailable覆盖迁移→新购买→pending→重载→下载完成。
- 安装gate已落在核心方法最后入口：未绑定/false，单包/批量均拒绝且不消费ready文件；只有正式规则配置的运行时可以绑定授权，UI不自行赋值。检查代码显式true仅是测试fixture，不证明真实解锁路径；Reload明确断言回调未序列化并重新授权。
