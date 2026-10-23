# LocalBuild 检查适用性审计（2026-10-18）

本报告为源码审计，不是执行通过报告。主线仍须在 Unity 执行适用检查。不得为了变绿删除有效断言，也不得恢复已被新设计取代的产品行为。以下路径相对 Assets/LocalBuild/Editor。

## 可直接作为当前验收入口

| 入口 | 范围与运行前提 |
|---|---|
| IntegrationProfilerCapture.Run | 三段实际 Profiler 场景采集；与静态编译、池大小断言分开记录，不用后者声称帧率改善。 |
| TechTreeUiPlayQa.Run | 当前科技树真实交互；核对缩放、鼠标锚点、拖动不刷新、独立 Canvas 等。 |
| TeachingPackInteractionChecks.Run | 当前教页数据包及题桌实际交互。 |
| TrainingLessonInteractionChecks.Run | 当前练页阶段题、知识卡等实际交互。 |
| VisualRasterInteractionChecks.Run | 真实图像题画面与输入。 |
| SheetAttentionPlayModeChecks.Run | 原题缩略图和真实遮挡评估热力图。 |
| StageCurtainPlayQa.Run | 升阶串行独占流程。 |
| SidebarLayoutChecks.Run | 当前侧栏显示与布局。 |
| BrainWindowPlayQa.Run | 当前购买后脑子窗口、关闭/隐藏及普通壁纸；替代旧壁纸入口验收，不能替代旧脑区全部细节断言。 |
| WorldYyGiftRoutingChecks.RunInteractions / RunPhotos | 礼物/租金真实 YY 路由与资源图片；照片入口必须等真实素材到齐，禁止假资源过测。 |
| WorldYyLearningChecks.Run（本次补丁新增） | 实际 Send→Heard→Deliver、题本/性格按钮、严格对话门槛、continuation 来源；目前暂存 yy-learning-fix-check，待主线放入 Editor 并运行。 |
| MusicInteractionChecks.Run | 音乐交互；不依赖旧开局/壁纸设计。 |
| UiEffectBudgetChecks.Run | 30/60 设置、效果池预算、隐藏停止、图表脏更新；同步检查，不等于实际帧率测量。 |
| MotionIntegrationChecks.Run | 汇总 MotionButtonChecks、Motion003PointerChecks、Motion008HoldChecks、LabelBaselineChecks、ReducedSwitchChecks；要求空场景无 EventSystem，保留禁用输入、指针归属、取消、减少动态效果、恢复基线断言。 |
| MotionButtonPerfProbe.Run | 单独的分配量测量入口；不能与实际游戏 Profiler 三段互相替代。 |
| NamingChecks.Run | 名称合法性、16 字限制/敏感或保留名、原名回退、旧存档身份及进度保存。第61行 productName 为存档身份断言，不能仅为标题美化而改成显示标题。 |
| NamingDesktopSmoke.Run / RunMonitor | 源码虽保留 FullscreenOnly 命名，235–278 行现在实际断言物理屏幕、无全屏覆盖、原开机镜头到位后输入和普通壁纸。不能因文件旧名字误判为过时；仍保留真实重命名/窗口大小/存档切换验收。 |
| ShutdownReturnSmoke.Run | 当前关机返回标题及动画。 |

## 必须迁移的混合旧检查

| 文件/定位 | 过时部分 | 迁移后必须保留的有效目的 |
|---|---|---|
| SkillTreeCopyChecks.cs:116–118,147–158,174–177,215–223,258 | 要求 Note 中大段“效果”双换行说明、下载批次/ADSL 时间、标注台每秒速率和操作限制；不符合三行卡、详情 ≤3 句、数据包立即可教。 | 新增卡名/短句/确定效果及详情字数/句数断言；保留只读快照、完整节点覆盖、中英语言、链等级/满级无下一价、真实 ReLU/augment 数值和标签不变、TooltipGeometry。数据包改为购买立即出现在教、无下载安装步骤。不要抹掉经济或增强机制的实际数值检查。 |
| PlayabilityChecks.cs:94,121,162–169 | 4题自动解锁练；开局电力免费；旧时钟自动收租。 | 改为12题科技→买训练曲线→练，电力正常计费，房东账单/部分转账/未付阶段升级。保留纠正影响真实模型、存档往返、读取去重、旧结局迁移、研究款不重复、完整通关可达与时长测量。自动通关脚本须购买训练曲线并完成所有对话门槛，不能把状态全部强设绕过。 |
| PlayabilityDesktopSmoke.cs:100–106 | 4题后直接 BeginEpoch；后续 ShowTab("board") 默认认为脑已开放。 | 用实际教输入12题、实际购买训练曲线后开练；脑子节点购买再打开。保留真实本地模型就绪/回复检查、YY恰好一问一答、窗口/贴吧交互。模型缺失应明确阻塞实机模型验收而非假回复成功。 |
| DesignFirstLessonChecks.cs:99–131 | 新鲜存档即可在公司做标注工作的 fixture，与阶段4众包只留订单冲突。 | 保留旧工作状态/队列存档兼容、不重复记账/样本、不接受锁定任务；当前玩法入口改测教页题桌，不能把旧公司标注台重新摆回UI。 |
| DesignFirstLessonChecks.cs:237–241 | 超时导致不得分/无学习信号断言与“倒计时只算快答奖励”冲突（若此API仍是现用教路由）。 | 改测超时仍接受正确答案、增加样本、只失去快答奖励；保留过时卡ID防重、并发预测变化、指针捕获防错题提交、主观题不伪造客观正确率。 |
| DesignFirstLessonChecks.cs:170–180 与后续 stale-evaluation 子类 | 核心旧“判断AI说得对不对”接口测试。该接口本身可能仍用于兼容，不能冒充当前教页测试。 | 当前UI必须测 TryAnswerStatement / 阶段实际题型；兼容API可单独保留，并在报告标清不代表现用界面。 |
| DesignWallpaperChecks.Run | setup后直接渐显覆盖壁纸。 | 改为普通壁纸不变；购买脑子窗口后内部显现流畅，不重启、不污染存档。若 XgWallpaperReveal 不再被当前入口使用，可留为旧帮助类单测但不得算产品验收。 |
| DesignWallpaperSmoke.cs 全部子入口及 partials | 共享 fixture 查全局壁纸脑；默认开局可见、覆盖壁纸、桌面右键脑菜单。FirstMinute.cs:152/176/259 还断言首分钟壁纸反馈及4题后练/脑可点。 | 迁移 fixture 到已购买且打开的脑子窗口；保留概念/连线/区域/种子/输出/性格/能力/规则/hover/图鉴交互、只读保护、命中层级、隐藏更新、真实训练反馈等。FirstMinute 改测12题和购买路线；“脑购买前无窗口”应是新断言。旧截图和自定义色值不代表新布局验收。 |
| DesignCrowdPrivacyChecks.cs:22 | 断言群里摆渡众包兼职是允许文案，违反最新群只聊兴趣/上班、不提众包规则。 | 私有实验室泄漏过滤器测试仍有效；新增独立群话题范围/实际所有作者发言数据验证。不要把泛用 RevealsPrivateLab 误改成所有公共工作话题都是“私有实验室”，应测实际群路由与迁移的输出。保留玩家自己的历史发言、时间/unread、私人AI聊天和幂等迁移。 |
| LatestRequestChecks.cs:42 | 绑定群提示词必须含“不知道玩家电脑”这个旧字面。 | 换为语义覆盖及实际路由：人类不自称AI、不知道玩家AI、不获样本/阶段私有数据。保留真实收费、断电/缴费、账本幂等、邮件去老周检查。 |
| ClaudeHandoffChecks.Run / Validate | Run里大部分原目的仍有效；Validate链会自动调用旧 PlayabilityChecks，不能作为“一次全部通过”入口。50/58固定题量与将来扩题需核对，不能因为增加题就删去年代和真假均衡。 | 保留升阶确认防双击、存档、主观题不改客观统计、2016日期过滤、存档槽隔离；按当前100事件/172常识不误混题库。房东押电脑与旧 ConfirmComputerSale 的精确交互需核对最新设计，不凭旧测试强行恢复规则。 |

## 不是回归检查的入口

PrepareRecordingSave.Run 会准备录制档，不应在“跑全部检查”时批量执行；LatestDownloadedBuild.Run、RecordingPlayerBuild.Run、WindowsReleaseBuild.Build 是生成构建的入口，单列构建结果；构建通过不代替交互验收。多个 PlayMode 自动检查均有 EditorApplication.Exit/场景切换，需逐个隔离执行，不能同时运行。

## 建议执行顺序

1. 当前 Unity 编译与核心 NUnit。
2. 空场景 MotionIntegration、名称/账本/隐私等同步检查（先迁移具体旧断言）。
3. 现用新教/练/科技/升阶/YY/图像/脑子检查，逐个隔离 PlayMode。
4. 实际三段 Profiler 与开关机/重命名窗口实景、脑子细节迁移后 smoke。
5. 本地模型可用时的真实完整聊天、完整流程时长与最终构建。

禁止把编译、读取数据、反射调用或自动截图单独声称为完整手动游玩通过。检查失败先区分真实回归、fixture没走新门槛和旧设计期望三类；后两类修检查前必须明确保留它原本保护的行为。
