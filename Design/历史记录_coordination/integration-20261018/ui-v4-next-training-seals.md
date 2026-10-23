# 下一最小完整功能：练页现有科技模块封印与准确跳转

只读检查当前main源码与最新整合记录，未改生产/编译/Unity。

## 已批准依据

`Design/整合记录.md:121` 明确“封印外观跳转…无需重复确认”；`Design/ui/exe_UI_v4_交接_给GPT.md:13,47,51,115` 要求未买块以深色磨砂贴图、名称、一句话和价格展示，点击定位对应科技且打开说明卡，慢光14秒一圈；明确禁止实时模糊。整合记录:240保留真实六挡学习率/收益/风险公式。上述可直接做。

建议本项严格覆盖已有可购买的5个模块：holdout→train.holdout、rate→shared.lr、checkpoint→train.checkpoint、monitor→train.monitor、weights→train.weights。新增模块节点/其他价格同步另列后续，不把此项标为整个练页v4完成。

## 现状与具体缺口

- `XgTrainPage.Parts.cs:9–38` RefreshTrainingParts直接Show(false)/SetActive(false)隐藏学习率、风险、看它学、回档等；没有封印占位。布局还会伸展曲线/隐藏整侧栏，不能只在已隐藏节点下添加Button。
- `XgTrainingCatalog.cs:27–55` TrainPart是真实门槛：老档 !teachingFlow一律保留可见；新档上述5项走已有科技。其他块next/learning是练3轮、model是多个架构、gear是研究节点、books是有书、ledger是电力开启并练过——尚非全购买门槛。封印应依据TrainPart结果，避免让旧档已可见功能重新收费。
- `XgTrainingCatalog.cs:10–13` 已有4节点60/80/120/100；`XgCatalog.cs:390` shared.lr真实60，不照抄HTML90。价格显示用Sim.NodeCost(node)，与现行购买成本/折扣一致。curve与答卷/训练按钮保持首购曲线后可用。
- `XgTrainPage.cs:678` FocusNode只是切科技再Tree.FocusNode；失败静默FocusFrontier会把人带错节点，不适合封印验收。
- **关键缺口** `XgTechTreePage.cs:1041` FocusNode仅Pin+Focus；`Pin:1008`只画圈/连线，完全不会打开说明卡。真实详情逻辑在:294（设置detailShown、tipExitAt=-1、ShowTooltip）。所以仅复用当前FocusNode不能满足“点击即打开说明”。
- 学习率容器 `XgTrainPage.Rate.cs:19` 高44px；不能在这个高度硬塞三行卡。可用一行横向封印（名字/短句/价）或调整整训练区保留足够高度，需小窗实测。
- `XgTrainPage.Pace.cs:67`看它学按钮和`Parts:28`诊断区/图表高度联动；锁看它学不应把已开放基础曲线与答卷一起蒙住。holdout同理，只封对应新题成绩/图例的能力入口。
- `XgTrainPage.Side.cs:388`每次RefreshSide最后统一执行RefreshTrainingParts，是状态更新入口。封印仅在数据/语言/布局变时刷新文字和图形，不每帧生成贴图/扫描全场。

## 建议实现边界

1. 增加可复用.exe封印卡（一次生成圆角深色贴图/细横线、单张慢光贴图），缓存5卡的模块→科技ID映射。锁卡与真实内容互斥，锁时阻止底下点击；显示名称/≤一句效果/真实当前价，前置未满或钱不足仍可看说明。
2. 新增Tree.FocusNodeDetails(id)或FocusNode可选openDetails，不改变已有FocusNode语义；定位并明确打开该节点卡，拖动/离开行为沿现有详情规则。找不到节点给明确双语提示，不能默认跳前沿假装成功。
3. 点击锁卡只导航不扣钱；购买仍走科技树现有真实交易。买成返回练页自动换成原功能、不重复购买。老档保持现有TrainPart授权；不改XgSim.Rates风险计算和训练状态。
4. 减少特效/低画质时静态封印；切走/隐藏窗口停止14秒旋转，别新增永驻全局Update。
5. 覆盖新开局仅曲线、钱不足/前置未满、逐个购买、老档、恢复存档，中英960/1400窗口。真实LCD点击锁卡必须断言tree tab、目标ID、详情可见；买后断言真实扣款一次、返回原控件与可操作性。视觉截图检查封印不会盖整张曲线/训练按钮；动画采样验证隐藏/减少特效静止。

## 不能混做却必须后续跟进

记账本已获#34隐藏详细资金但保留危险提醒授权，原型¥15明确，但当前目录根本无ledger节点，TrainPart仍按电力/练时自动显示；需要独立把节点、顶栏例外、账本入口和旧档迁移一起做。能力仪表/模型名牌等新增节点虽然文档有临时价格，交接:104又说节点价格以前main为准，不能在只做封印时顺便采用HTML替换已有节点。本批只包装已有5项，不凭原型新增重复收费或删已有功能。
