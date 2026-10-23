# 脑子窗口交接（暂存，未合主工程）

依据：《开局流程简化_教练科技.md》第五节。

## 补丁和入口
- brain-window-full.patch：3个新增文件＋4个既有文件的最小修改（Controller安装入口、Wallpaper窗口工厂、Presentation窗口模式、Concepts只读布局旋转）。不整文件覆盖后续main。
- 新XgBrainWindow / XgBrainWindowFeedback 放 XingGuang/Desktop；BrainWindowPlayQa 放 Assets/LocalBuild/Editor。
- B02-knowledge-feedback.patch 是对upstream-b-adaptation版本Lessons草稿的可选接线：首次显示知识卡调用 `XgBrainWindow.KnowledgeShown(Sim,title)`；已有 PhenomenonFound 先接主工程真实现象事件。
- Install入口原 XgBrainWallpaper.Install 改 XgBrainWindow.Install。旧全桌面脑子停止安装，安装阶段清除脚本重载遗留同名层（未购买也清除）；普通系统壁纸保留，不改用户选择。

## 逐项实现与验收状态
| 要求 | 实现 | 状态 |
|---|---|---|
| 购买后才出现 | Has(brain.window)且已进入桌面；未购买不建小窗、不显任务栏按钮 | 已接线，待Unity QA |
| 320×240 | PrologueDesk.Window初始尺寸 | 待QA自动断言 |
| 可拖动 | 标题栏本地坐标拖动，限制在桌面内 | 待QA/真实鼠标验收 |
| 可缩放 | 右下角拖柄，240×180到960×720；原画面虚拟1280×832缩放 | 待QA/真实鼠标验收 |
| 最小化、关闭 | 两个按钮均隐藏窗口，不销毁画面；任务栏入口保留 | 待QA按钮断言 |
| 任务栏重开 | 原脑子置顶专用槽，避免挤占主App图标 | 待QA/视觉验证 |
| 首次在exe右侧 | 取exe世界角点转父坐标，靠右放置并夹在屏幕内 | 待视觉验证，空间不足时有重叠可能 |
| 默认置顶可取消 | 标题栏置顶按钮；仅在确实不是最后一个同层窗口时调整 | 待QA/与其他App交互验证 |
| 玩家名字动态 | 每0.25秒轻量状态观察更新title/taskbar | 待QA改名断言 |
| 沿用原画法 | 原真实Concepts、AnswerPath、Training、Persona、Abilities组件 | 独立编译通过，待画面确认 |
| 答对/答错 | 真实CardAnswered已结算事件；对应区域首个已画节点放大绿光/红光 | 待QA实际Answer事件断言 |
| 连击档位 | ComboTier触发金色边框一次呼吸 | 已接线，待手玩 |
| 训练一轮 | 原EpochStarted训练波＋EpochDone从上向下扫光/原连线加粗 | 已接线，待手玩 |
| 新题库 | NodeBought为数据节点/pack；数据区域轮廓渐长＋题库名 | 已接线，待手玩；轮廓表示题库区域，不伪造已学概念 |
| 学会新概念 | Board.ConceptsChanged非删除事件，从实际ID查真实概念key并浮字 | 已接线，待手玩 |
| 能力涌现 | AbilityEmerged全小窗白闪；只读绘图按能力旋转区域内点阵，连线跟随 | 已接线，待手玩；不改核心概念或权重 |
| 知识卡 | 现有PhenomenonFound红光/标题现象名3秒；B02可选接口接新知识卡 | 现有事件已接；B02等待该单元合入 |
| 隐藏停更新 | 窗口gameObject inactive，所有绘图子组件停Update并解除各自事件订阅 | 待QA MeshBuilds不增长断言 |
| 性能 | 不加Glass扫描层、不加全屏脑子菜单；Presentation.WindowMode直接返回，无全场景窗口扫描；反馈仅事件后1.2秒以30FPS重画 | 独立编译通过，Profiler待运行 |

## 已做验证
- 使用主工程Bee响应文件和真实Unity程序集引用独立编译整个Assembly-CSharp（含全部暂存修改）退出0，compile.log。
- QA脚本独立编译退出0，qa-compile.log。
- 没启动Unity、没跑PlayMode、没生成验收截图。不能标成Unity验收通过。

## 可运行的Unity QA
进入**隔离存档的Play Mode**后调用 `BrainWindowPlayQa.Start()`（菜单 LingGuang/Verification/Brain window）。脚本拒绝正常玩家磁盘存档，只接受useDiskSave=false或LocalVerification下的SavePath；随后在内存新建测试档。
它测试真实BuyNode、按钮onClick、窗口尺寸/拖动约束、关闭后仍存在、任务栏重开、改名、真实答题事件，以及隐藏后原概念绘图MeshBuilds不增长。
结果和两张截图写入 `LocalVerification/BrainWindow/<时间>/`。不会退出Unity；不删除已有文件。画面/拖柄的真实指针交互及其余反馈须按表手动验收，自动入口没有把直接调用MoveResize冒称鼠标端到端测试。

## 待确认/注意
- 现有概念文字本身可能是内部概念key（沿用原绘制源）；精细本地化/自然词映射后续需按实际样本检查。
- 首次窗口空间不足只能夹在桌面内，320宽与exe并排取决于桌面分辨率。
- 老档拥有brain.window会恢复小窗，最小化/关闭状态仅当前会话保留；没有新增存档字段，不丢既有进度。
- “海豚壁纸”按保留现有普通系统壁纸处理，没有强制覆盖玩家自行换过的壁纸。

## 独立运行入口补充
- 可用 `-executeMethod BrainWindowPlayQa.Run`，不传 `-quit`。在 EditMode 打开真实 Main，BeforeSceneLoad 隔离存档和桌面用户目录，禁用本地模型，使用 SetSimulationForTests 与 BootManager/UserManager 完成桌面准备，再跑实际购买和窗口按钮。
- 完成后退出 Play；batch 模式退出码通过 0、失败/中断 1。任何 Error/Exception/Assert 均使报告失败，不吞错误。
- 追加未购买无任务栏、旧全屏脑子根已删除、任务栏文字跟随改名检查；普通壁纸更换仍需手玩。
- 重开是新建的永久任务栏文字入口，不是复用旧置顶切换行为。悬停显示完整名字及“关闭后点这里重新打开”。旧置顶按钮禁用。当前没有另造开始菜单入口，避免其依赖 WindowManager 的搜索路由错误；以任务栏为关闭后可发现入口。
- 更新后再次独立编译 runtime 和 QA，均无编译错误，仍未启动 Unity。
