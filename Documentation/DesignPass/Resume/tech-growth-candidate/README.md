# 4:004 成长页整合候选

此目录保留 Assets 外的候选与审阅补丁。主聊天确认53退出后，已按当前三源身份落地3份小补丁及两份新文件/meta。实施目标仅为 `4:004` 的「成长」承载页，必要引用为《合并删减策划》§3.2；不将该引用整节或后续节点格式条目标为完成。

## 候选内容

- `XgTechTreePage.Growth.cs`：新 partial。六项真实能力按顺序展示，直接读取现有参数/有效样本门槛和进度；右侧消耗品货架直接调用原购买接口。窄窗口默认收起货架，旧 items 路由或货架按钮可重新打开。
- `XgTechTreePage.patch`：既有页改为 partial，能力头代替重复的能力图鉴方块；道具按实际折扣目标能力归列，其余科技延续目录阶段关系。既有节点显隐、科技词条、购买、提示框、缩放/平移复用。秘籍移到所属列末端，沿用已有自悟金色状态。
- `XingGuangView.patch`：仅3个局部片段。items / abilities / growth 兼容转到 tree；items 打开货架，abilities 聚焦下一能力；旧页面对象保留供已有公共引用使用，路由不会显示它们。
- `XingGuangView.Frame.patch`：仅5个局部片段。显示单个成长入口，隐藏重复导航，旧导航定位也返回成长；可买数量包括同图的道具和科技。
- `DesignWallpaperSmoke.Growth.cs`：独立候选场景入口 `RunGrowth()`。检查兼容路由、六能力顺序、真实折扣归列、中英文 TMP 与动态门槛、读取不改保存状态、未来科技仍隐藏，以及真实 LCD 买 MLP 后门槛更新、货架买一包后的库存和精确扣费。

英文长文本补强：能力头按实际TMP高度布局，折扣前门槛另起一行；卡片高度变化时同步下移节点、连线及其聚焦坐标。货架标题、名称、效果、库存和购买按钮均按实测高度排布，保留12–20字号，不用缩小正文换取通过。关闭按钮处于滚动区外。场景检查新增双语840/240/160实际页宽、长当前参数及真实折扣组合、行宽/行高与不重叠、关闭/导航命中、滚轮到底及关闭后重开；所有窗口和隔离状态都在finally恢复。

未改玩法核心、价格、门槛、库存或存档模式。未迁移存档、未读取玩家存档。没有重新实现后续节点卡格式，也没有改核心 UnlockPath 文案。

## 整合方法

1. 已核对 `source-manifest.json` 与当前源字节SHA全部相同。保留的 `build_candidates.py` 是落地前基线工具，应用后不能直接重复执行。
2. 已检查并应用3份 `.patch`；View/Frame仅3/5个片段。新增 partial 位于 `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.Growth.cs`，场景检查位于 `Assets/LocalBuild/Editor/DesignWallpaperSmoke.Growth.cs`；各自meta已生成。
3. 主聊天在公共 smoke setup 的 mode 分流处为 `growth` 设置 `step = 220`，并在主 switch 前加 `mode == "growth" && step >= 220` 时调用 `TickGrowth(controller, runtime, brain)`。公共 `Start("growth")` 的其他隔离、退出和报错逻辑复用。
4. 统一 Unity 编译后运行 `RunGrowth()`。科技页布局有变化，还需复跑 `RunTech()`，不可沿用42轮代替此候选的提示框回归。

## 当前核验结果

- 当前源文件身份记录、25/3/5个替换片段的唯一性检查通过。
- 3份 Git 小补丁的 `git apply --check` 通过后应用；限定源文件 `git diff --check` 通过。
- 候选及部署后的运行时/Editor均用Unity自带Roslyn独立编译，退出码0。日志在 `LocalVerification/DesignPass/GrowthCompile/`，重新核验使用 `python Documentation/DesignPass/Resume/tech-growth-candidate/typecheck.py --deployed`。此过程没有启动Unity，也没有写Library输出。
- 已独立读当前 API 链确认：MLP真实参数折扣、消费品钱包/库存/totalSpent、能力门槛及学习状态、真实购买事件刷新、旧路由导航与图鉴目标兼容。
- 主聊天54已运行，最终货架交易失败，且图验发现玻璃效果使货架正文透叠。修正详见 `../tech.md` 的54章节；尚无成长场景PASS，本agent不启动Unity。新增生产修复仅为 `XgBrainWindowGlass` 保护货架及恢复已treated底板alpha；新fixture用真实滚轮回顶后购买，保留精确交易断言。
- 新证据输出 `growth-purchase.txt`（滚动/命中/库存/余额/累计支出）、`growth-backing.txt`（实际底板alpha）及 `growth-before-shelf-purchase` 截图；顶部/底部和最终整页截图继续保留。修正后需要重跑及实际图验，不能沿用54的TMP尺寸通过替代可读性验收。
- 主聊天报告56成长场景PASS。其后按4:004补齐View.FeatureOpen的items/abilities/growth→tree权限别名；追加新局旧core门槛为false时的权限等价、首页BuyItems实际LCD点击、训练补货栏可见及无资金购买仍禁用检查。新增代码已独立编译通过，但需重新场景验收，不能沿用56覆盖本次变更。
- 最终正常页截图改为先检查年代卡Continue中英文刷新，并通过真实LCD点击关闭，再拍 `growth-ability-item-and-shelf`；生产按钮按当前语言刷新，叙事内容和8秒展示时序保持不变。GrowthGuide别名检查由lesson代理独立partial提供，本入口只接一次子迭代器。
