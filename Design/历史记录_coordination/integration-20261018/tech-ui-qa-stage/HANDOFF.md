# 科技页真实 UI QA（暂存，未运行）

1. 主工程三行词条合入后，应用 instrumentation.patch：PaintPasses计数只在非移动Refresh真正绘制时递增；连线MeshBuilds只在OnPopulateMesh递增。无功能开关或行为改变。
2. TechTreeUiPlayQa.cs 放 Assets/LocalBuild/Editor，运行 `-executeMethod TechTreeUiPlayQa.Run`，不加 -quit。

真实 Main，BeforeSceneLoad 存档及桌面用户目录隔离，模型关闭；已解锁六阶段明确测试档。没有用TestMode的0余额做购买测试，也不冒充购买路径。

验证内容：
- 中英文对所有当前可见、含三行的真实TMP组件 ForceMeshUpdate；记录textInfo行数、字符数、isTextTruncated、isTextOverflowing、preferred尺寸、实际框尺寸；逐个可见字形顶点检查矩形边界。超过20个实际卡片才满足覆盖要求。不是只查源字符串长度。
- 找实际详情Button并调用onClick，下一帧确认详情可见且生成文本mesh。
- 实际XgPan PointerEventData回调：鼠标锚点缩放不漂、50%–150%边界、双击100%；实际toolbar按钮：适应/加/减。
- 持续跨帧拖动，显式请求Refresh，断言PaintPasses和连线MeshBuilds不增长；拖动时详情隐藏。
- JSON记录每项通过/失败和每个文本mesh参数；两张截图；错误导致失败退出。只有纯UnityEditor.Search.SearchDatabase基础设施异常分开记录，Assets错误仍失败。

限制：这是实际UI事件处理器注入，不是操作系统物理鼠标端到端；没有声称真实帧耗时改善。尚未运行Unity。QA脚本独立Roslyn编译无error。
