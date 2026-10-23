# 终局 A 小型实际验收候选

独立候选；没有写生产，没有启动 Unity/模型/Player。已复用生产里刚通过的 UnifiedAtlasActualUiRunner 串行外壳（场景/临时存档/模型禁用/截图/恢复），改用独立 SessionState key、输出目录及 A 专属 checks，不修改旧 runner 或共享检查类。

补丁：`ending-small-A-ui.patch`，SHA256 `725ee08f65b4931621b12fbad4df06a33755d0fa912fb1071da808870d86ff7a`。

新增四文件：

- `Assets/LocalBuild/Editor/EndingSmallAActualUiRunner.cs` 与 `.meta`
- `Assets/LocalBuild/Editor/EndingSmallAActualUiChecks.cs` 与 `.meta`

入口：`EndingSmallAActualUiRunner.Run`；菜单 `Tools/Local Verification/Ending small A actual UI`。

由主控在当前前台检查退出后串行运行。输出：`LocalVerification/EndingSmallAActual/<UTC timestamp>/report.json`、`passed.txt`、隔离 `Save`、`DesktopProfile` 与图片。成功才生成 passed.txt；异常会保留完整堆栈及整帧截图。既有 `-verificationExit` 退出约定保持。

## 本轮已验证

- 对真实脏生产 `git apply --check` 通过（4个新文件）。
- 隔离 `Assembly-CSharp-Editor` 编译 0 错误，241 个既有警告；全部生产引用指向真实 ScriptAssemblies，不写生产 Library。日志 `verification/editor-build.log`。NuGet 源明确清空，只做本地已缓存引用恢复，没有下载依赖。
- 仅完成源码和编译验证；下述运行时断言尚待主控 Unity 执行，不能提前算通过。

## 计划执行的实际验证

双语×YY/贴吧四组，均使用声明的 stage6、能力列表、合法 YY 联系来源或已有双向贴吧私信夹具。主场景、持久化对象、频道集合和 UI 是生产组件，模型关闭、环境 Updates 暂停。

1. **A3** 通过实际 `TiebaHub.SendZhouFinalApproach` 私有自主发送入口（反射 seam，非伪造消息）调用。实际存档 `.tmp` 路径由测试临时创建为空目录，令 `AtomicSaveStore` 真正 FileStream 写入失败。检查失败来源、marker=false、原消息列表引用/未读数恢复、forum/chat/lab 三段JSON恢复、主存档原始字节不变和2秒重试时间。移除自己创建的空目录、等待2.1秒后再次调用，核对两句精确台词、仅选定渠道增加、其它NPC/群聊不变；真实主存档 payload 中同时存在消息和标记，真实 ReloadSave 后不重复发送。
2. **A2** 通过实际 host 的 `XgEndingPreparationFlow.CheckpointPresentation` 进入真实 Persist，而不是直接调用 Persist（保证 flow 负责状态回滚）。同样注入真实临时文件写失败，核对标记、消息引用、未读、JSON与主存档字节。重试后核对题干经现有过滤器标准化后的精确文本、正确渠道、无其它联系人变化、磁盘消息与标记同一次保存；真实 ReloadSave 后再次 checkpoint 不重复。打开实际 YY/贴吧私信窗口检查最终题干在可见 TMP 控件中并截图。
3. **边界**：stage5 不发；已存在 pendingTrial id 或 attempt 不补发；实际 Unity JSON 缺少 finalApproachSent 字段默认 false；无现有双向私信时 A2 继续 pending、A3 不创建会话交流。
4. **实际提示词**：调用运行时 `LlmPersonas.Conversation`，检查老周早期采用 EarlyPersonaRule、没有否认作者的旧秘密规则、包含不主动点破限制；A3/A2实际保存后采用 FinalPersonaRule。表姐、群聊、老同学保留原匿名限制，不带作者/终局规则。此项验证构造出的实际 YY prompt；没有调用模型，也不冒称模型一定服从。贴吧共享规则已在作者 followup 中静态验证，本 runner 不增加生产提示词截获钩子。
5. **A1/A4/A5**：实际 host.Show 打开生产卷首，两语言截图并查用户原句；复用其现有夜间面板做明确 readOnly 展示夹具，检查错误来源只加一次红色前缀、未知 answer 不冒充玩家教学，两语言截图。A4夹具不安装或保存任何虚构发布/评分凭据。A5 检查当前真实 XgGuide 投影指向 final 页；不声称覆盖 Bodu 点击路径。

## 范围与恢复

不重复六张卷子、210秒上线、终局信或标题重播。A2/SI与联系人历史是明确夹具，不伪称本 runner 自然完成前置剧情。A3测试实际 sender 事务及 stage gates，不覆盖外层 idle/typing/choice 调度的全部组合；A2重试是实际 checkpoint重试，不声称验证自动通知 timer。

磁盘故障只在校验过的本次 LocalVerification SavePath + `.tmp` 创建一个原本不存在的空目录；finally 非递归删除自己创建的该目录。没有替换 SaveNow 委托、改文件权限或触碰用户存档。外壳在任何失败/退出时先关闭磁盘写入，释放自己的测试输入设备，恢复窗口尺寸选择、场景设置、语言与静态 save/profile覆盖。所有改动仅候选文件；由主控整合后再运行。

## 冻结前台修订

UI前等待连续3秒真实Application.isFocused且未最小化；观测帧和截图时失焦直接失败。InputSystem保留原backgroundBehavior，不再IgnoreFocus。纯事务检查不冒称前台交互。420秒总截止仍是启动/故障上限，UI观测窗口从稳定前台后开始。两个meta GUID均32位十六进制。与casino前台小修一起隔离Editor编译0错/241既有警告，未跑Unity。热重载中止可能停留PlayMode，当前主控串行冻结C#规避该已知QA限制。
