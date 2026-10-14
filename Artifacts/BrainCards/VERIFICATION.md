# 大脑卡片拼装验证

2026-10-07，活动项目 `/Users/kk/competition`，Unity 6000.4.5f1。

## 范围
- 大脑默认新增卡片拼装视图，原概念板／皮层拓扑／接法图鉴保留。
- 结构、激活、五类增强卡；两条实际训练线；草稿暂存、确认应用。
- 不新增存档格式，不替换「接线」工作台，不修改既有模拟实现。

## 自动测试
- 新卡片规则：26 项 RED → GREEN；.NET 26/26。
- Unity EditMode 独立运行：26/26，无跳过。
- 全量 .NET：622 项，593 通过、29 失败。排除新增两文件的基线复跑复现 27 项失败；另 2 项长流程未复跑。不能宣称全量绿色，见 `core-validation.md`。

## Play 验证（LCD 原生鼠标事件路径）
- 架构/激活/裁剪卡拖入正确槽位：修改草稿，正式 Sim 序列化前后相同。
- 卡片拖入错误类别槽位：不创建或改变草稿，不改变正式配置。
- 点击卡片可放入；增强卡可通过移除按钮移除。
- 卡库原生滚轮可滚动至下方卡片；未解锁预热卡拒绝放入。
- 首次应用仅进入确认态；立即再次调用不能绕过 0.5 秒防双击门槛。
- 间隔后的显式确认应用真实改变结构为 MLP，草稿清空，训练仍停止；正常内存存档 Host VRAM=8192 MB。
- 非空序列草稿在两条线之间切换后保持独立；撤销非空草稿不改变正式 Sim。
- 训练中应用按钮禁用；改变 Today 后过期草稿无法应用。
- 真实拖动生成 ghost；切换离开大脑页清除 ghost，未生成草稿。
- 显式「去训练」导航到所选序列轨道；仅选拼装轨道不会改变全局选择。
- 原三视图通过原生按钮切换后均可回到 Assembly。

## 视觉检查
- 默认大小：Assembly 1049.95 × 543.03，RectTransform 无负尺寸/NaN。
- 修复首轮检查中中文卡名、中央箭头、卡库提示被裁切的问题。
- 卡库长说明采用有意省略，完整内容保留在现有 UiTip；不是所有文本零省略。
- 原生窗口 resizeAnchor=Disabled，不宣称支持任意拖小窗口。

## 边界与恢复
- 所有测试模拟内容均在 useDiskSave=false 的运行态，真实玩家数据未注入测试 fixture。
- 测试运行器会恢复场景序列化值，所以每次进入 QA Play 前重新检查并设置 disk/tick=false。
- 首次测试后曾正常加载真实存档（尚未注入 fixture），检测到 disk=true 后立即停用。保存哈希基线在该正常加载之后建立；不宣称整个会话的存档字节绝对不变。
- QA 中脚本热重载使已禁用 Controller 的非序列化 View/Sim 丢失，重新进入隔离 Play 后复测；不将这一开发态异常当成游戏交互通过。
- 最终恢复编辑态 useDiskSave/tickEnabled=true、原 ComputerCaseMenu；未保存场景资产，未提交或推送。

## 成本
- 卡片目录固定小规模，查找 O(C)；展示 O(C + 7)，C 为卡数。
- 草稿沿用现有快照/池拷贝；其指纹检查 O(概念 + 连接 + 标签 + 解锁项)，按每条轨道至多 0.75 秒一次缓存，动作时再校验。
- 没有新依赖、付费请求或实际模型推理。

## 最后一轮
- Unity EditMode 复跑仍为 26/26：job `dde2f291c0bc4e67a915688ed8e8b0b4`，明细 `unity-focused.json`。
- 最终增强卡已装配画面：`assembly-final.png`。增强页 59 段文本、15 个按钮，`layout-final.json`：invalid=[]、overflow=[]。
- 修复增强卡移除按钮的 × 被裁切；在同一 Play 画面按源代码相同的字体/边距设置验证，随后刷新编译源文件。
- 最大化尝试中原生窗口 Animator 把 CanvasGroup 写回不可交互。没有判定最大化通过，也未更改 DreamOS；最终预览在隔离 fixture 内关闭该 Animator、恢复默认窗口尺寸。正常默认窗口的拖放/点击此前已单独通过。

## 最终编译阻塞（当前状态，优先于之前的通过记录）
最后刷新时，工作区中另一个新增文件报错：
`Assets/LingGuangV05/XingGuang/Desktop/XgEraAdvantageBridge.cs(144,44): CS1061: TiebaView does not contain a definition for OpenPrivate`。
该文件未出现在本任务初始清单，修改时间为 2026-10-07 02:20:51，本任务未写入它；现时源文件中的 TiebaView 未定义 OpenPrivate。
Unity `EditorUtility.scriptCompilationFailed=True`。因此不能宣称当前整个工作区可编译/可重新运行。
卡片功能的 26/26 Unity 测试及 Play 输入验证在此报错前已完成；最终删除按钮的两行字号/边距修正已在相同运行画面验证，但最终整体重新编译被上述文件阻塞。
原 ComputerCaseMenu 已恢复，Play=False、scene dirty=False；没有保存测试场景。
