# 复核扩容 Unity NUnit 验收

目标：`Assets/LingGuangV05/XingGuang/Tests/Editor/XgReviewCapacityTests.cs`，使用现有 `LingGuangV05.XingGuang.Tests.asmdef`；无需新 asmdef 或改引用，该程序集已经引用 XingGuang.Core 与 Core。对应 patch 同目录。

共 11 个 NUnit 用例（含参数化）：真实 BuyNode 扣 3000；20满后第21题保留在prefetch，购买后原题继续，60满后第61题不入且不丢；不可重复购买；钱不足不赋权益；旧档通过 Unity JsonUtility 真正序列化/反序列化后保持60题ID顺序与题目内容；无权益30题溢出转prefetch，已有prefetch在前，33题全体ID不丢不重；噪声复核只占剩余5格；聊天排队、聊天执行、玩家输入拦后台；单槽策略对所有seat/lane阻止第二请求。

**边界**：单槽策略用例不冒充 `LocalLlm.Pump` 的多槽总闸集成测试。真实生产串行条件在 Assembly-CSharp，需沿用其专门的 active/native-in-flight QA。本文件明确写出这一点，没有修改生产调度器。

已完成 Unity 6000.4.5f1 自带 Roslyn 编译，0错误（compile.log）。未启动 Unity、未改 main、未宣称 NUnit 已运行。

合入后运行 EditMode Test Runner，过滤 `LingGuangV05.XingGuang.Tests.XgReviewCapacityTests`；命令行可加 `-runTests -testPlatform EditMode -testFilter LingGuangV05.XingGuang.Tests.XgReviewCapacityTests -testResults <绝对路径.xml>`。
