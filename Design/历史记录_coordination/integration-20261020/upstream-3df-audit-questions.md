# 上游 3df48181 题库 / 教页只读审计

范围：`c5c63198..3df48181`，对照 `competition-integration-20261020` 当前工作树（含未提交整合改动）。仅审计指定的 XgMemes/Sense、XgLogic、XgBoardData、XgExamBank、XgLabelPage*、XgEraTests；未改工程源文件、未运行 Unity、未提交。记录时间：2026-10-20。

## 结论

不能整文件覆盖或整提交直接合入。可按块移植文案去泄露、2016日期限制、唐诗干扰字和三处界面刷新优化；参数化题库要先修组合及日期问题。必须保留当前教题判题目真值、WorkContext独立众包入口、教学记录中的AI实际答案、AI标识、DeskGameVisible的public/activeInHierarchy语义、增量测试集准备与算术真实输入特征。

两项真实设定差异需要用户选择：正规运营商促销算不算垃圾短信；记忆考题的房租1350/月是否独立虚构数值，还是必须对齐实际账单。172题是已确认要求，不应再次当成可随意增题的授权；现有源码数量不符，需要追查已确认清单，不能默默追加或删题。

## 各文件归类

| 文件 | 上游增量 | 当前状态与建议 |
|---|---|---|
| `Core/XgMemes.cs` | 约35%参数化danmu/spam/headline/review；新增反讽例题；两条10086促销false→true；替换“网吧老板训练AI”标题 | 当前仍与c5基线相同。新增确定性反例、去除“网吧老板训练AI”标题可移植；促销真值是设定变更；模板需修后引入。不要把新的经典二元题强行替换现有教小游戏三选项题库。 |
| `Core/XgMemes.Sense.cs` | 鸟翅膀解释微调；黄河新反例；8条春节/韩剧/电影/奥斯卡题补日期；孙杨英文省略freestyle | 当前已有同义黄河反例与更准确长颈鹿解释，应保留。日期限制可移植；不要重复加黄河题。孙杨当前英文保留freestyle更精确，无需退化。 |
| `Core/XgLogic.cs` | 日历题由全年随机改为2016-05-24前后14天，解释给出推移天数 | 当前未实现。答案仍来自DateTime，不违背题目真值规则，可移植；会降低日期计算跨度，属于难度调整，建议作为明确交付说明，不必因纯实现选择阻塞。英文解释可补同样推移过程。 |
| `Core/XgBoardData.cs` | 静态TestSet缓存；Signature减少分配；HashSet预分配；唐诗采用PoemDistractor | 唐诗和等价Signature/容量优化可按块移植。不能覆盖partial类、DisplayRegion、Profiler标记、TryGetTrainSignatures、算术Input隔离。全局可变List缓存建议不移植，当前已有每sim缓存及可取消分帧准备。 |
| `Core/XgExamBank.cs` | `5.rent`从一天350改每月1350 | 当前仍一天350，但真实经济已每月手付，默认十月账单2450。上游数值也不能自动视为正确，见设定问题。 |
| `Desktop/XgLabelPage.DeskGame.cs` | 小游戏切换按钮只在full/可见性变更时Set | 可移植局部优化，保留当前public `DeskGameVisible` + `activeInHierarchy`。这不是新页面入口，不替换教/众包路由。 |
| `Desktop/XgLabelPage.Side.cs` | 小蜘蛛图标仅away状态改变时Redraw | 可移植。DrawAiIcon仅依赖尺寸/away，无逐帧动画。保留玩家AI名字；初次构建与布局变化仍由Unity Graphic失效触发绘制，验收应检查首帧与拖入拖出。 |
| `Desktop/XgLabelPage.cs` | 红闪计时夹到0、仅红闪期间重绘纸张；FeedWindow固定数组替代ToArray | 可局部移植。Refresh仍会刷新新卡主题，未改卡片计时判定/连击规则。当前新增的learnedAiAnswer捕获、RecordTeachingQuestion参数、commentGenerated、AI内容标识不能被覆盖。 |
| `Tests/Editor/XgEraTests.cs` | 参数化题标记/词汇审计；唐诗错误字不等于正确字 | 可作为基础回归加入，但不能据此宣称语义、日期、干扰字质量已验证；应补下列真实边界。 |

## 参数化文本题：可取内容与必要修正

1. `Pick(... allowTemplates=false)`为向后兼容的新参数；上游只在`XgSim.CreateLabelCard`文本分支显式启用。单独复制XgMemes不会让玩法出现新题。移植时还需局部修改该调用，保留当前`companyWork`隔离和教学/众包独立卡ID。
2. `PickTemplated(... today, ...)`目前完全未使用today，并在Pick的原有日期过滤前提前返回。带“守望先锋”、AlphaGo三连败、引力波、高考“明日上午九点”等内容没有相应日期限制。既定2016语境要求下应给模板日期元数据并过滤，或者改成不声称当天新闻的历史题。现有EraLexicon只是词表，不能验证事件时序。
3. ReviewRealItems/Pros/Cons三数组独立拼接，确实可生成“给出租屋买的台灯到了，玩守望先锋帧数够用，就是风扇转起来有点响”“机械键盘插上电脑一次点亮”等搭配。应按商品类型约束属性组合，或仅使用所有商品都成立的物流/包装细节。无需让用户逐句批准修正。
4. 参数模板的分类标签沿用既有任务定义：弹幕是否夸、短信是否垃圾、标题是否标题党、评论是否刷单；不是把“玩家赞成AI判断”当正确答案。现有`DirectTruthJudgment`和TryAnswerStatement应保持。
5. 当前教小游戏并不全部走这些经典卡：`XgPackDanmu`使用`XgTeachingDanmu`三分类；headline教学优先`XgTeachingHeadlines`；`PickUncertainTeachingPhrase`未启用allowTemplates。上游新增不能被描述为“所有教小游戏已经扩容”。启用范围是经典卡与使用CreateLabelCard的众包卡；二者仍须独立实例、独立提交。
6. BoardData.Make、Vocabulary和增量VocabularyPreparation都保持默认allowTemplates=false。上游说明“核心双字词对齐”不是验证结果；新手工题与固定训练/测试题分布不同。先保留既有测试集语义，不为让新题通过而改变准确率定义；如扩大模型训练词表，另做同seed/同步与增量等价验证。

## 唐诗干扰项

- 必须成组引入`XgCatalog.PoemConfusables/PoemDistractor`、`XgSim.CreateLabelCard`诗词分支、`XgBoardData.Make`诗词分支，以及回归测试；不能只改BoardData，否则手工与模型评测分布仍各用旧/新规则。
- 新算法按常见混淆字或同位置诗字制造错误项，比此前任取诗句首字合理；保持“题目本身是否正确”的语义。
- 上游回归名为“PrefersConfusables”，实际仅检查长度1和不等于正确字，没断言来自混淆池、偏好比例、日期/英文或极端seed。
- helper使用`PoemLines[(p + t) % PoemLines.Length]`，p由`pick & int.MaxValue`得到，p接近int.MaxValue时加t可能溢出为负。移植时先对p取模再加t，补int.MaxValue/int.MinValue边界；这是工程修正，不需要用户设定确认。
- 当前`XgPackPoem`四选一仍用山/月/人等固定候选，每第四题还有原诗/改编题。上游helper只覆盖经典二元卡与BoardData，不能覆盖这套已实现的教小游戏题目。若希望四选一同样升级，可复用helper选三个互异且不等于正确字的选项，并保留当前原诗/改编分支。

## 172常识题约束：现状证据

按文件内实际`S(level, true/false, ...)`构造调用计数：c5基线215，当前216，上游216。当前2016时事段之前166条，时事段50条；不是已核实的“172基础题+44时事”。`SenseBank`直接返回该数组，未见172条筛选。

当前新增“黄河是中国最长的河。”为false；上游新增“黄河比长江还要长。”同为false。朴素合并会成217且新增一个false，破坏当前正反平衡。应只保留当前版本，保留删掉“长城在日本”的结果，保留长颈鹿解释“长脖子是它的显著特征”。当前`XgSenseTests.TheBankIsBalancedAndEveryItemIsPaired`仅要求>=80并检查平衡，不保护172精确数量。

因此本轮不能声称“172条要求已由当前代码满足”；应由根代理核对已确认的172条清单/口径后处理。没有授权自行任删44条以凑数。

## 必须确认的设定差异

### 1. 短信的标签口径

两条具体变更：

- “中国移动充100送20，回复1办理”：当前false，原解释为正规推广不算垃圾；上游true，解释为群发促销也算垃圾。
- 10086“充50送50，回复KT办理”：当前false；上游true。

新模板也把正规电商优惠券归true。需要选择：A，垃圾短信包含正规群发营销（上游口径）；B，仅诈骗/恶意短信算true，正规营销仍false（当前口径）。不能只合入模板而留下两条相反标准答案。无论选择哪项，正常账单、消费提醒与取件通知仍false；分类问题不同于2016词汇合规。

### 2. 记忆考题是否代表玩家实际房租

上游`5.rent`改成“十月起每月1350”；当前`Core/ChapterOneSim.ManualRent.cs:9`为`MonthlyRentFor = RentFor * 7 + extra`，`Core/Models.cs:127`默认十月日基数350，因此默认真实月账单2450（另有减免/涨租）。实际账单每月发YY并手动转账，见Bills.cs:143、ManualRent.cs:40–41。

需要选择：A，题目改成明确的虚构人物/租房广告，1350只作为记忆素材，不代表玩家账单；B，题目描述玩家真实房租，则改为与当前经济规则一致的文本或动态题。不要因同步一行ExamBank顺便修改经济金额、账单频率或用户已确认的手动转账流程。

## 不需要再问、直接保留的既定行为

- 教题答题目真值；模型预测只参与学习与纠错，不反转按钮语义。
- 众包公司题和私人教题保持独立卡片/提交/学习记录。
- 连击不因计时归零而断；计时只决定快答奖励。上游指定页面差异只是绘制优化，不授权改回限时失败。
- 保留当前公开DeskGameVisible及可见层级判断，AI实际回答记录和AI内容提示。
- 未执行Unity测试；以上为git差异、调用路径、源码计数审计，不能替代当前仍运行中的tech-ui验证。
