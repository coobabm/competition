# C6 内容与年代审计候选 — 2026-10-19

候选目录独立，未改生产、未运行 Unity/编译器/本地模型、未生成图片、未提交 Git。基准提交见 `base-commit.txt`；当前生产为含其他 agent 合入修改的工作树，不能用提交号冒充全部基准。`manifest.json` 记录每个实际原文件与候选的 SHA256，`base/` 保留逐文件原文。

本批完成的是逐项静态审计及已确认规则的局部候选，不等于 C6 整体运行验收、新闻事实来源全部核验、素材全部授权或全部叙事后果已接入。

## 交付与实际检查

- `production.patch`：按原格式保留未改 JSON 行，只有局部内容、回复选择及群日期读取 hunk；不整文件覆盖旧目录内容。
- `candidate/`、`base/`、`manifest.json`：13 个修改文件的可复核快照与校验。
- `ITEM-AUDIT.md` / `item-audit.json`：逐项列出新闻45、事件100、群预设30、性格8包/9标签/47个问题、对话12、阶段21、游戏19、常识216，以及真正的消费者、门槛、静态状态与缺项；总计507行。
- `changes.json`：每个文字/字段/hunk前后值和理由，含英文增量。未修改共享 `Core/Lang.En.cs`，因此没有新字典键覆盖。
- `static-report.json`：`python audit_content.py` 的真实静态检查结果。检查 JSON 重复键、数量/ID、双语字段与数组配对、日期范围、after指向可持久化推送、选项答案下标及现有常识库全体/分级平衡。不把这些称为Unity PASS。
- `old-source-comparison.json`：旧第九至十二轮材料/同名源码与生产差异清单；无同名生产文件明确标缺失，不直接复制。旧第十二轮已有黄河替代题经局部比对采用；好友系统消息、模型发布、终局脚本均没有借此顺手导入。
- `OPEN-ITEMS.md`：剩余内容风险、接线与实机验收清单。

更新候选后用 `refresh_artifacts.py` 重新生成补丁；它先核对生产基线，发现他人修改就停止。`prepare_candidate.py` 只是初次起稿脚本，后续候选已追加修正，**不要重跑或据其重新生成最终结果**。

## 本批实际修正

1. `YYGroup.cs`：删除按月份推断玩家“无业自由职业、每天标图、越来越成功”的旧Regard回复池。公开众包/接单/标注/找工作话题走原有工作话题池；钱/工资仍走原有钱和硬件池；问近况/房租等不再触发成就断言，按普通关键词匹配，未匹配则走现有通用池。原有接话、近期去重、莉莉入群条件不变，无新剧情台词。
2. `LlmPersonas.cs`：群模型允许围绕玩家真实发言讨论公开众包，但不能据此得知私人AI；新增约束有中英版本。房东/老友不再由AI阶段推定已按时交租或已经做出事业，复用原有中性/固定人设提示。AI身份使用 `lab.AiDisplayName`，未命名才走原名回退。
3. `Era/era_events.json`：`dawei_envy`、`aj_boss`、`lili_advice`、`dawei_intern` 4条按日期凭空夸玩家经历的推送，改为同项目已有同角色自述台词；对应英文直接复用。ID/日期/通道保留。`changes.json` 可逐条审查，未改老周给定台词。
4. 群预设13条复用实际YY推送的 `after`：`g01=ow_launch`、`g11=warcraft`、`g12=iceland`、`g15=ge_you`、`g16=volley_wake`、`g17=fu_yuanhui`、`g18=ti6_wings`、`g21=onmyoji`、`g22=lili_westlake`、`g23=lanshou`、`g24=civ6_gone`、`g25=photo_lili_cart`、`g26=steam_winter`。这些ID都由 `EraContent.TakeDue` 写入 `eraDelivered`；不误用只出现在新闻列表而永不推送的ID。
5. `g14/g19/g20` 缺准确的可复用推送，分别加 `minDate=20160707/20160830/20160908`。同补丁含 `YYChatHub.GroupPresets` 的字段+游戏日历读取3行hunk，不能只合JSON。使用已有Pokemon新闻/小目标常识/iPhone新闻日期，未声称联网史料核验。**不触碰此前群协程生命周期修复**。
6. 事件：`e014` 删“卷王”与英文称呼，保留已有笑着离开的叙述；`e033` 最早日由奥运开幕日改为本地女排决赛日8月21日，并删未经支持的“凌晨四点”；`e048` 去未门控驱动版本；`e096` 标题→标注、全年事件不点名未门控的鼠标型号；`e100` 结果中英20题匹配既有samples20，不改效果。最新计时/循环规则仅更新note，调度代码仍归root。
7. 新闻 `n1013` 当天报道不再知道“后面好几天没回应”，正文只保留鲍勃·迪伦当年获奖事实；删除缺来源的“首次给歌手”断言。未改日期/ID/教学奖励。
8. 对话解锁note改为已接入的原句Ordinal精确匹配，取消旧“忽略标点空格大小写”说明；功能代码不改。
9. 常识：禁删题全Assets/Design数据检索未发现；按旧第十二轮已提供的同级黄河题恢复216条、108是/108否及每级平衡；长颈鹿英文解释不再说英文名字本身含“长脖子”。自然常识与算术诗词不硬塞2016字样。

## 真实消费路径（不是只改 Design 副本）

| 数据 | 加载与显示/行为 |
|---|---|
| 新闻45 | `BoduView.News.EnsureNews` Resources → `XgNewsCatalog.Load/Available/Home`按游戏日期 → 新闻主页/归档；`XgTeachingHeadlines`另用于标题题 |
| 事件100 | `XgLifeEventCard.Bind` Resources → `XgSim.LoadLifeData` → `LifeDataEligible`日期/阶段/requires/when → `LifeDataView/ApplyLifeDataChoice`与结果卡 |
| 群30 | `AvailableGroupPresets` Resources → 游戏日期/after/莉莉筛选 → `GroupPresetChoices` → 真实 `SendGroupPreset` → 回复队列 |
| 性格8/9/47 | `XgYyTalk.EnsureLearningData` → `LoadYyLearning` → `YYChatView.Learning`、`AskPersona`、真实回复标签；原有科技/淘货共用拥有状态 |
| 对话12 | 同上加载 → `LearningDelivered` → `RegisterYyReply`原句+真实回答 → `ChatNodeReady/Hint/Answer`；attention_only映射transformer |
| 阶段21 | `XgTrainingLessonResources.Load` → `XgTrainingLessons` → `NoteTrainingPhenomenon`排队 → `XgTrainPage.Lessons`；shift与continues已发现真实交互调用，不照抄旧未接线结论 |
| 游戏19 | `XgTeachingResources.Load` → `XgTeachingPacks.All` → 大厅/科技/`XgPackGames.Create`全部19个ID；题面来自各实现/题库，不把配置fun字段当成执行逻辑 |
| 常识216 | `XgMemes.Bank(SenseDesk)` → `Pick`按since/level筛选 → `XgSim.PickTeachingPhrase`及常识桌 |
| 模型/无模型群聊 | `YYChatHub.cs:404`调用`LlmPersonas.Conversation`；无模型走`YYGroup.Fallback`，两条路径都纳入候选 |

Design事件原文与运行时已有19条不同、群预设g10不同，属于此前已修语义/日期内容。候选对各自基线做同一新增修正，**不拿旧Design整表覆盖运行时**。浏览器/性格/解锁/阶段/游戏表其余原有一致性已检查。

## root串行整合验收

先按manifest核对、`git apply --check --ignore-space-change --whitespace=nowarn production.patch`，再统一编译。现有 `XgSenseTests.TheBankIsBalancedAndEveryItemIsPaired` 应纳入定向回归；我没有执行该Unity测试。检查自定义中英文AI名字真的进入模型请求；普通众包话题在本地模型可用/关闭时都不泄露AI或推断成绩；六月/十月/十二月相同话题不再得到基于月份的成功断言。

群预设：分别在minDate前一天/当天、after未收到/真实收到、莉莉加入前后查看可选项；发出后仍正常每2–4秒回复；g24必须到实际一周缺席话题后才出现。复跑已有LifeGroupPlaybackChecks确保字段hunk未干扰生命周期。实际模型输出、按钮布局、磁盘旧档、玩家物理点击/AltTab均待root跑。
