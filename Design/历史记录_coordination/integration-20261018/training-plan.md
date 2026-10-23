# B 系统实施规划（只读阶段，2026-10-18）

## 文件所有权

建议本agent负责：
- 新 Core/XgSim.TrainingLessons.cs：新存档字段、模块解锁判定、阶段小题触发/奖励/已答、知识卡发现/收下与队列、旧档兼容。
- 新 Core/XgTrainingLessons.cs：21题JSON DTO加载桥接、知识卡双语内容与节点映射，避免改已有字典形成重复键。
- 新 Desktop/XgTrainPage.Lessons.cs：曲线上方小题卡/单次知识卡展示与收下；跳节点使用view.Tree.FocusNode(id)。
- Desktop/XgTrainPage.cs/Side.cs/Rate.cs/Sheet.cs：默认三件套布局、按节点/事件显隐、三档学习率、缩略图；不再创建数据来源UI、GPU拖放、下载进度。
- Core/XgSim.Sheet.cs：首轮用前4个亲教题（需要与教页agent协调记录钩子）。
- 科技图鉴复用Core/XgSim.Collection.cs的IsAtlas/AtlasLit（和主agent科技树工作协调）；后续需要图鉴节点点击显示完整内容，不只tooltip。

不碰：XgLabelPage/Hall/DeskGame、XgMiniGames、XgCatalog.cs（教页agent）、XgTechTreePage/Copy（主agent）、玩法数据包购买（教页agent）。

## 新节点接入约定

已有train.curve=10，brain.window=15，label.ai=20。
新增train.holdout=60（train.curve），train.checkpoint=80（shared.lr），train.monitor=120（train.checkpoint），train.weights=100（train.curve）。shared.lr保持原ID移到train分支，不再新造同功能节点。
独立XgTrainingCatalog.AddNodes(List<XgNode>)构造节点；由拥有XgCatalog的agent在LayoutStages前调用，lane/train注册需和主科技树agent同步。知识卡节点lesson.{id}与小题quiz.{id}在atlas分支，状态以lessonCollected/quizAnswered而非购买；避免伪装Phenomenon但读不到标记。

## TrainPart映射

curve/sheet/button：train.curve（旧档保留已开练）
holdout：train.holdout
next/learning：总轮数>=3
rate：shared.lr
ceiling：连续5轮无涨事件记入trainParts
checkpoint：train.checkpoint
monitor：train.monitor
weights：train.weights
model：首次第二结构（新增已见结构列表避免来回切换判错）
gear：拥有任意技巧/硬件
ledger：SystemUnlocked(power)
tracks：两线均已可训练
rate-auto：Adam节点
books：原书架功能须保留但不做知识卡归宿；仅拥有书才显示，知识卡统一科技图鉴。

旧档处理：teachingFlow=false代表已有存档，不重锁已用模块；新字段默认空列表；新档须真正经历购买。新规则不删除已买包/书/原数据。新档默认不显示占位框。

## 小题与知识卡

触发应接EpochEnded/StageAdvanced模拟事件，不靠UI Refresh判定以免隐藏窗口漏记；显示才依可见页/过场锁。阶段小题21道严格按JSONtrigger。首阶段题等练页开后才展示，避免开局12题提前讲AI。
知识卡9张：overfit/nan/regress/ceiling/small_step/noise/grokking/vanishing/emergence。仅第一次弹出，暂停训练包括自动请求，收下恢复；若同时遇到多个要排队。阶段升级独占过场优先于知识卡。
quiz答对+20仅一次、错不扣；两者都有解释和同节点链接；已答题进图鉴可重看，不再发钱。

## 真图和热力图风险

当前XgTinyLook为8×8数字微网，captcha/meme/围棋用替身数字，且寻找或混合图使网络答案符合模拟答卷；不能作为新增真图验收。需统一VisualQuestionAsset按题id/seed提供同一张图和推理输入。
缩略图缓存按题目/变体seed（不是每Refresh生成Texture），图上绿红边反馈按epoch变化。
49块遮挡热图仅开窗计算并缓存，缓存key包含模型revision+图id；关闭停计算。程序生成题可直接像素；照片必须真实素材或生成素材，不能拿数字冒充。

## 教流程协作审读

已读XgSim.TeachingFlow：teachingFlow只新档BeginOpening启用，旧档不重锁，是兼容优点。TeachingAnswers=handCorrect+handWrong包含MiniPay与普通题，因此12/24条件不依赖单一UI；label.ai前置训练曲线由Catalog链确保。需要额外验收：跨套连击、答错扣款不会绕过首12科技可买、已有新档在只做了少数题时被当旧档迁入的语义。
