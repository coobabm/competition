# Editor 探针程序集与 AddComponent 核对

## 2026-10-19 实际运行更正（优先于下方历史结论）

**下方“当前无需迁移”的静态/先例推论已被实际证据推翻，不再适用。** 先例报告只能证明其各自运行，不能证明这些匹配文件名、由Unity识别的独立Editor脚本也可添加；本轮据先例降低风险并建议保持原位不充分。保留下方原判断和依据以便追溯，不将失败归给输入H1。

已读取 `.coordination/integration-20261018/claude-c2-reading-focused-20261019.log:1729`：

> Can't add script behaviour 'PublicReadingHeldPointerProbe' because it is an editor script. To attach a script it needs to be outside the 'Editor' folder.

随后构造器访问返回的空组件发生 NullReferenceException。实际 `LocalVerification/SidebarLayout/20261019-104714-080/report.json` 为 failed，失败点是 HeldPointerTrace 构造；没有形成可供H1判读的按住trace。前一轮104334-262在真实前台门禁前失败，同样不是H1证据。

主控负责最小更正：将两个probe及其meta迁到 `Assets/LocalBuild/Diagnostics`，保留GUID与 `#if UNITY_EDITOR`；两者只有System/UnityEngine和Action回调，不依赖Editor trace类型，因此迁移不产生runtime→Editor反向类型依赖；runner仍留Editor目录。补AddComponent空值断言，避免导入错误被后续NullReference掩盖。此迁移只修诊断自身可挂载性，不改输入维护、产品或严格断言，不能称公共阅读已修复。

本agent仅更新记录，未改生产、未运行Unity、未扩审或采用conditional fix。等待主控迁移后的真实trace再分析H1。下文为原始历史审查。

## 历史审查（原样保留，结论已由上方更正替代）

结论：当前没有证据要求迁移或改写两个探针。可保留现有位置，等待主控首轮 C2 日志确认实际 Update/LateUpdate 与顺序。没有修改生产、运行 Unity 或采用 conditional fix。

## 程序集依赖

当前生成的 `Assembly-CSharp-Editor.csproj:238,298` 已将两个探针列入 Editor 程序集；同目录 runner 与它们在同一程序集。Editor 程序集引用 `Assembly-CSharp` / firstpass，反方向没有新增引用。`Assets/LocalBuild` 下未发现 asmdef/asmref 改写这条默认归属。

两个探针是文件名匹配的 public sealed MonoBehaviour，只有 System.Action<string>、UnityEngine 和回调；不存在运行时程序集反向引用 HeldPointerTrace 的问题。`#if UNITY_EDITOR` 和 Editor 目录让它们不进入普通 Player。运行方式是 PlayMode 临时 GameObject 上 AddComponent<T>，不依赖 Inspector 拖入或场景序列化。

## 现有实际运行先例已确认

不再仅以“仓库存在类似代码”为依据：

- `Assets/LocalBuild/Editor/DownloadRegistryLifecycleRunner.cs:62` 在 EnteredPlayMode 以 AddComponent<DownloadRegistryLifecycleDriver>() 启动协程；Driver 在同一 Editor 文件 :101。实际报告 `C:/Users/xomem/AppData/Local/Temp/LingGuang-RegistryBatch-QA-b6b4db08ad064743958a633bea999d7e/report.json` 为 passed、Unity6000.4.5f1、errors=[]，包含有效 detailReport。
- 同样模式的窗口索引 runner 实际报告 `C:/Users/xomem/AppData/Local/Temp/LingGuang-WindowRegistryBatch-QA-2a3019fdbd61435186f7e546c6ec12b4/report.json` 也是 passed、6000.4.5f1、errors=[]。
- TechPurchaseFeedbackRunner 同样在 Editor 文件声明 MonoBehaviour Driver；`C:/Users/xomem/AppData/Local/Temp/LingGuang-TechFeedback-a8f50e1146504dc99bfa90cf5d479261/report.json` 的35条通过记录含跨渲染帧动效检查。

这些证据支持“本项目/本Unity版本在PlayMode程序化添加Editor程序集MonoBehaviour并执行协程”的做法可用。不能因为路径含 Editor 就断言 AddComponent 一定失败，也没有必要预先迁出Editor目录。

## 剩余最小运行确认

上述先例不替代两个新探针本身的回调和执行顺序验证。首轮 trace 应存在 early-update、pre-notifier、late-update，同一玩家帧可关联，并检查 orders 字段的导入值/声明值。若phase缺失、AddComponent实际异常或顺序异常，再针对该项给最小修正；不要因怀疑先迁文件、扩大程序集或改产品。现有构造失败清理和外层finally保留。

此前 REPORT 的“Editor程序集探针可否AddComponent未运行”应理解为这两个新探针自身未验，并非缺少该项目同类方式运行先例。此核对只消除此处过度疑虑；H1仍待真实读值。
