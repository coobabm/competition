# 机箱主菜单

## 打开
- 场景：`Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity`。
- Unity 中打开后 Play；Build Settings 已将其设为第一场景，并保留原列表。
- 重新生成：`Tools > LingGuang > Build Computer Case Menu`。所有已打开场景必须先保存。

## 当前行为
1. 提前 Additive 加载真正的 `Main.unity` 并初始化原游戏服务，不复制游戏逻辑。
2. 原生 LCD 源相机预先渲染，但实体屏幕保持黑色，故事、计时和桌面输入等待玩家开始。
3. 机箱电源按钮：悬停音、机械下压/释放音、指示环和局部确认光效；只在同一按钮内释放才确认，移出释放取消。
4. 确认开机后：屏幕出现系统标志、四阶段启动文字和进度条，配置启动动画时长 4.2 秒；同一台摄像机连续推向真实显示器。减少动态效果时也必须等待启动完成才能操作桌面。动画采用限步时钟避免卡顿后跳跃，低帧率/截图采集会延长实际耗时。
5. 没有遮黑、截图替换、第二次场景加载或相机切换。到位后开放原生桌面操作。
6. 设置：总音量、减少动态效果、全屏。支持鼠标与 TAB / 方向键 / ENTER / SPACE / ESC；有触屏输入代码，未做真机触屏验证。

## 美术与音效
- 原创建模：倒角机箱、侧板缝、光驱位、USB 3 接口、耳机孔、螺丝、进气网、橡胶脚、线缆、键鼠、台灯、木桌；补充咖啡杯、三本技术书、代码/流程图笔记本、铅笔及便签。
- 墙面三张不同风格的原创生成海报：三体、图灵、冯·诺依曼；采用不同尺寸、倾角、纸边与胶带。图灵人物图为生成的照片风格创作，并非档案照片。选用 ThreeBody-v2.png / Turing-v2.png / VonNeumann.png；内置 ImageGen 生成，原图与提示词保留，见 Posters/。
- 装饰全部无 Collider；相机初始 43°，扩大向上的取景以纳入海报，落点与同镜头流程不变。
- 复用项目已有 LCD 造型、CJK 字体、Feel 和 Reach 音频，无新增付费资源或外部 API。
- 新场景静态统计 51,374 三角面（包含预览 LCD），重复细节按材质合并；实际运行用 Main 的 LCD 替换预览实例。
- 菜单 Bloom 强度 0.12、clamp 2，显式禁用此菜单的 Neutral Tonemapping，避免近距离金属高光大面积泛白；未修改原 Main 的渲染管线资产。
- 风扇开机前停止；启动后音量/音高快速升至 0.22/1.5，短暂高转后回落至 0.032/0.9 并保持循环。复用原 Fan1 素材的有效片段制作 1.48 秒无缝循环，去掉原片尾 1 秒静音；见 Audio/FanLoop.md。Play 已核实声源参数，主观听感由用户试听确认。
- 每帧仅一次物理按钮射线查询、两个按钮反馈更新和常量规模镜头插值；新交互逻辑时间/额外空间为 O(1)，不含 Unity 渲染和 Main 本身。

## 验证（2026-10-05）
- 最新开机/风扇专项 EditMode：41/41 通过（包含原 26 项 + 启动 5 项 + 风扇 10 项）。
- 最新 Editor Play：79/79 检查通过，日志采集期 0 errors；包含屏内启动和风扇包络。配置启动时长 4.2 秒，截图/采样这次实际观察完成约 8.49 秒，不能把配置值等同于低帧率下的墙钟时间。
- 真实验证：预载黑屏、屏幕源渲染、计时暂停、鼠标按压/取消/释放、混合输入隔离、键盘操作、设置音量/静音、减弱动效、重复开机防重入、25/50/85% 连续镜头、开屏、9 个游戏服务、重复初始化、单活动 Listener/EventSystem、通过 LCD 点击打开计算器。
- 全项目 EditMode：1052/1056 通过，4 失败：`EraCalendarTests.TheDateOnlyMovesForwardAndOldMomentsKeepTheirDay`、`XgBoardTests.PlainLoopsForgetLongSentencesAndGatesKeepThem`、`XgChanceHintTests.ThePlaytestLeNetThatStaysAtChanceIsToldToWiden`、`XgPhenomenaTests.VanishingMemorisationAndAmnesiaAreDetected`。失败涉及未由本次修改的日历/训练模块；没有借此改动现有玩法。未取得本次修改前的全量基线，因此不声称证明这些失败早已存在。
- 完整结果：`QA/editmode-full.xml`；Play 结果及截图：`QA/20261005-225550-655/`。
- 已按最新一次 Play 验证前的备份恢复实际存档，目录逐文件比较相同；QA 前后快照保留在 `/tmp/lingguang-case-menu-before-additive-qa` 和 `/tmp/lingguang-case-menu-after-qa`。
- 当前 Unity 留在新菜单场景的 Edit 模式。独立 Player 构建、音效主观听感和移动设备未认证。
- 开机/风扇这一轮另有存档快照：`/tmp/lingguang-before-boot-qa-20261005` 与 `/tmp/lingguang-after-boot-qa-20261005`；验证后观察到再次手动进入/退出 Play 的状态，未回滚最新状态，避免覆盖玩家操作。

## 修改边界
- 开始前已将 Main 的未保存编辑备份为 `Assets/_Recovery/Main-BeforeCaseMenu-20261005.unity`，随后保存原 Main，未丢弃其内容。
- 9 个既有初始化入口只开放同程序集调用；YY/邮件补幂等防护。其余既有 dirty gameplay 改动保留。
- 运行时对原 Main 显示器的重定位、暂停和相机绑定仅作用于场景实例，不写回 Main。
