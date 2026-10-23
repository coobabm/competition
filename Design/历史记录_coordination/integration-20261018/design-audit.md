# 2026-10-18 设计与数据审计（只读）

来源：Play/Main-NoModel-20261003/Design 的七份指定设计文档与 data 九份 JSON。七文档、九数据全部存在并可解析。以下是实现验收清单，不是已完成功能。最终规则优先级：本轮用户正文 > 第六轮 > 第五轮 > 配图 > 第四轮 > 第三轮 > 开局流程 > 练页教学。

## 已验证数据

| 文件 | 根数组/实际数量 | 结构重点 |
|---|---|---|
| life_events_100 | events / 100 | id, stage=[min,max], month=[YYYYMM,YYYYMM], when, requires, who/En,title/En,body/En,choices；choice有label/En,result/En,effects |
| yy_group_presets | presets / 30 | id,text/En,replies/En,可选month/after |
| persona_packs | tags / 9, packs / 8 | tags:id,name/En,axis,delta；packs:id,name/En,price,stage,desc/En,questions[q/qEn/stage] |
| chat_unlocks | unlocks / 12 | node,stage,phrase/En,hint/En,answer/En；没有替代解锁价格字段 |
| stage_quiz | quizzes / 21 | id,stage,trigger,q/En,options/En,answer(0基),explain/En,node |
| browser_news | articles / **43** | id,date,kind,title/En,body/En,brief；与文档45有偏差 |
| minigame_packs | packs / 19 | id,name/En,dataset,stage,needs,buy,status,play/En,fun；**existing11/new8**，不是新9 |
| chess_mates | puzzles / 7 | fen,side,solution(允许的首步),line(完整UCI),name/En,hint/En |
| chess_mates_lichess | mateIn1 / 25, mateIn2 / 25 | id,rating,toMove,solution(UCI完整线路),url,pgn(SAN从初始局面起) |

事件效果实际只有 money/samples/noise/pause/boost/persona/flag 七种。注意 persona 是[中文轴名,数值]，其它字段须按真实类型读，不要全用字典字符串转换。

## 冲突与待确认

1. 2016-only 是用户硬规则，旧文档的 Transformer(2017)、现代 SFT/CoT 等名称不能直接照搬为2016已公开技术；保留玩法与后期探索意图，使用时代合理的实验描述，不自行删终局功能。需要在待确认写明术语处理。
2. 用户说代答在题卡上方，开局文档说题卡和按钮之间；按用户正文上方实现。
3. 用户指定对话必须完全相同；第三轮/data注释允许去空格标点、全半角/大小写归一。按用户正文严格匹配（语言对应原句），不可模糊相似匹配；归一尺度若不确定写待确认。
4. 最新开局覆盖旧文档4题开练/首练开科技：12题开科技，买train.curve才开练；全四栏可显示但锁定，仅教可点。
5. 原知识卡存成就/名字页、书架入口均被新方案覆盖为科技图鉴；默认练页无虚线灰框，无“未解锁”文字，只曲线/答卷/训练按钮。
6. 第六轮租金阶段3前免租、分期人工付；e001阶段1–2“今天转”直接money-300不兼容，必须改写成提示/当前账单动作，不能再另一套扣款。e051涨租仍直接-200/-100，应用下一期租金且按欠租记录影响议价；e083缺“准时交满5期”和冬至日期前置，需补。
7. 老周相关数据仍有 e071主动索要翻译题库、chat wordvec提示老周回答，可能违背此前“除了玩家没人知道应用”。本轮明确禁止群友知道AI，允许论坛玩家自己发帖；安全方案将e071限定已主动分享相应资料，避免他无缘无故知道，勿直接删功能。
8. 新闻43≠45、新小游戏8≠9：应按19个明确包全部实现；是否补第9个新包/2条新闻列待确认，不凭空造新玩法。
9. 所有运行时中文包括fun等字段若展示都需英译；JSON note是文档注释不必显示。chat_unlocks.wordvec.hintEn及life e071 whoEn仍有中文“老周”，应英文化。

## 第一批：性能、拖动、文字、开局、教逻辑

- [ ] Profiler关闭Deep Profile，教连点/练自动/YY贴吧切换各30秒，保存前后录制和CPU/GC/Canvas前10热点；不能将静态扫描冒充Profiler结果。
- [ ] 缓存全场景查找；脏标记+不可见不刷新；缓存字符串/文本比较；动态文本固定字号；桌面/每应用/提示/托盘/动态图形拆Canvas。
- [ ] 自定义图形数据变才重绘；脑子限15Hz且隐藏停；低画质曲线10Hz；Update避免LINQ/new list；存档后台快照或空闲5分钟；LLM异步串行、本人AI高于群/贴吧；浮字池且最多8个。
- [ ] 画面高低、30/60帧设置，默认60并vSync=0，存档/设置兼容。
- [ ] 科技拖动仅改位置/比例，松手刷新一次；线条合并MaskableGraphic，购买解锁才Dirty；拖动暂停hover；装饰不raycast；独立Canvas+RectMask2D+惯性与200像素回弹。
- [ ] 首帧任务大厅可见且宽度>0后延帧刷新与ForceMeshUpdate；同查科技名/答卷/YY联系人。
- [ ] 开局12题解锁科技，独白“做完了……然后呢？”+便签“去科技看看”；初入科技仅第一行实体其余轮廓，“还能买东西？”；训练曲线价格保证首套可购。
- [ ] 三句用户指定独白精确保留且仅一次；首次答卷取玩家最先亲手教的4题；首次全对独白“全对了。……可这几道我都告诉过它答案啊。”
- [ ] 开心辞典爬梯/安全点/求助换题放弃/继续按钮删除；12题仅套数，连击无限跨套；对题0.25s下一题，错题理由1s后下一题；倒计时只快答奖励不超时判错。
- [ ] 对题绿闪+钱+音效+弹跳，错题红抖+扣钱+理由+连击破碎；10/25/50/100档金粒子，倍率最高2，半时间内快答+50%；右栏连对、档位、套数、当天题数/钱。

## 第二批：核心

- [ ] 所有题桌/小游戏在教；众包删标注台实例，阶段4后只企业订单/自动提交/分包/结算/信用。
- [ ] 19数据包条件全读取：stage1短信/算术/扫雷(免费)、24点(买)；stage2找不同/规律/弹幕/五子棋免费，标题党/一步杀/数字买，数字needs vision；stage3成语免费但memory、验证码/表情买vision、唐诗买memory；stage4卧底免费、评论/两步杀买(lstm)；stage5中式英语买。
- [ ] 8个明确新实现：扫雷、24点、找不同、规律、五子棋、一步杀、两步杀、成语。每一步SettIeMiniAction样本计数，经典对错模式可切换。
- [ ] 数据购买科技数据分支、训练与教同题库、买后新题桌置顶红“新”+可跳转托盘；删除练数据来源/拖GPU/下载进度/分批购买UI。
- [ ] 练分块：train.curve基础；holdout新题线；3轮事件能力进度/它学会了吗；shared.lr三档；5轮平台天花板；checkpoint回档红点；monitor风险；weights可视化；第二结构模型行；首技巧硬件装备；首电费账本；第二训练线视觉文字；Adam自动学习率。
- [ ] 21阶段小题按trigger单次，选对+20且任意答复显示解析/节点跳转；收科技图鉴。支持stage_enter/plateau/gap/nan/deep_flat/shift/long_fail/deep_worse/translate_fail/serial/noise/continues/flatter。
- [ ] 9知识卡：过拟合(差15pp)、NaN、退步、平台、稳档5轮不涨、噪声>20、顿悟+10pp、阶段2深>4不涨、首涌现；暂停自动训练，收下单次标记/图鉴与独白。
- [ ] 新阶段主推题型：对错→四选一(1–4键)→图配6词/输入→找错字/没错→翻译三档评分/点错词→两答择优；旧题桌自己的题型保留。首次新阶段说明只一次。
- [ ] 小工具按购买：代答、偏置门槛、sigmoid把握/排序、增强“变”、清洗疑似错题、难例复核标签。
- [ ] 真图：手写28²放大224²网格；4位验证码干扰扭曲；九宫格8类×10小图；程序表情配字；照片每类5张；来源许可记录。
- [ ] 练答卷48²缩略图、逐轮绿红边闪；可视化49块遮挡推理只开窗算并缓存；MLP散/卷积笔画/草地捷径有可见区别。
- [ ] label.ai前置train.curve+24题+1轮，无次数限制，每题常驻名字/答案/把握，<55%不知道，答后自身正确标记。
- [ ] brain.window早期购买，320×240拖拽缩放最小化关闭重开、默认置顶可取消；题对错/连击/训练/新题库/概念/涌现/知识卡有分别反馈；壁纸普通海豚。
- [ ] 科技中文+缩写+年份/≤12字/▶≤14字、效果实时数字，原理只详情≤3句；禁止免责声明、内部公式。
- [ ] 升阶独占队列：停训练自动化关闭hover，暗70%；月字幕2.5s→960×540演示→720宽首话→最多6行解锁→淡出按1.5s排队提醒；空格/点击继续，不叠窗。
- [ ] 科技滚轮鼠标中心50–150%每格10%；−＋/适应；<70%隐藏价格；空白双击100%居中下一可买节点；比例保存。

## 第三批：对话

- [ ] YY题本260高抽屉，不挡最后3行，按题库每组最多显示50、存最近200，搜索/随机；题目+玩家/AI当时答案；发送原题按阶段作答且与教过结果比较。
- [ ] 性格8包9标签，日常免费，科技/淘货同购买状态；每回复0–2标签推轴2–3，实时飘字；每标签5条样本进模型上下文；truth/flattery计数供终章；重复提问允许、风格副作用实际生效。
- [ ] 12对话节点指定句+AI回复后解锁，记录回答到卡背；暗藏句子真实可找到；主线有更贵/后阶段备用路径。
- [ ] 阶段4每日主动问并记长记忆；委托验收；月份梗教学玩心+2；阶段3称呼；连续3退步安慰影响下一轮训练。
- [ ] 命名后所有UI/故事/模型/小游戏/科技/便签/托盘中指代AI的灵光与英文LingGuang动态名字；仅原名剧情与命名前保留。

## 第四批：世界与旧代码移植

- [ ] 主人公上班两年独租；晴雯在读研究生室友允许；群只游戏配置显卡球工作，无AI/接单/房租、无网友私聊。
- [ ] 100事件+旧9条整改；每阶段≤2，首次3–8分钟、两次隔≥6分钟；升阶/考试/验证码/剧情不弹；同事件单次，所有when/requires/month/stage过滤；七种效果统一入口。
- [ ] 群3气泡按月份/话题，每日刷、发过不再来、回复2–4秒；自由输入仍模型回复；每周至少2次冷场。
- [ ] 13张新群图按文档对应文件名：chat_lili_grad/gulangyu/qinghai/westlake,chat_ajie_1080,chat_xg_tv,chat_dw_desk/cafe/iphone,chat_xg_dawn,chat_lili_parcels/ticket,chat_ajie_noodles；geyou已有不重画。2016随拍800×600，毕业600×800，无可辨脸/商标；PHOTO_CREDITS记生成作者与许可。
- [ ] 房租账单/YY转账卡+工具栏+文本识别只房东会话；可部分付款、橙气泡、流水；到期不自动扣，提醒→警告→拉闸(手动教仍赚)→押电脑；迟付不罚；三次准时台词/五期饺子。
- [ ] 礼物到本人家，晴雯聊天按钮或“送你+具体礼名”才寄出，AI代回无送礼权限。
- [ ] 新闻本地首页按游戏日期、每页8、最新先/高阶段科技优先，推广水文、brief只标题不点，正文相关3条/转给AI看，阶段6热搜，无真实网络导航。
- [ ] 贴吧数据仅自己发帖后回复，ImageNet/联合国语料移植，表姐请本人翻译，无未发生事件的回帖。
- [ ] 国际象棋先7入门再50，难度排序黑方翻盘、两步杀自动对方应手、任何同样将杀着法认可；Credits Lichess CC0。
- [ ] 第四部分逐文件意图移植：Sense+100；YY/LlmPersonas/Subcontract/MarketRelay/Wiring/众包；四故事json+StoryBridge+StoryText；DataSources/QualityTraps/Forum/Tieba/DataUi/Lang；YY图/era8图消息；Girlfriend五处+Taohuo。

## 第五批：交付

- [ ] 每完成一项向Design/整合记录.md写做什么/涉及文件/未做原因/待确认；逐项Unity编译证据。
- [ ] 中英覆盖、无重复字典键；新字段默认值、旧存档保留进度；LocalBuild/Editor检查；可运行包含Qwen与WindowsD3D11闪退修复；GitHub更新记录并吸收最新main。

## 棋题实证

已执行 `.coordination/integration-20261018/audit-chess.ps1`，直接编译并调用现有 `competition-latest-20261016/Assets/LingGuangV05/Core/Games/ChessGame.cs`，无需Unity。
- 57/57通过：SAN合法复盘、执棋方一致、非走棋方未被将军、每步UCI合法、线路终点将杀。
- 所有两步杀进一步穷举首步后对方所有合法应手，都存在下一步将杀。
- `.coordination/integration-20261018/chess-audit.json` 保存57个id/valid/fen/rating，可供运行时FEN缓存输入。
- 尚未验证玩家UI交互、SAN解析迁入C#、不同但正确着法的运行时评分，不能据此声称棋玩法已完成。
