# C3 完整快照分块存储首版

状态：coordination候选，未写生产、未运行或编译Unity、未提交git。root选择在持久化边界对完整序列化快照分块，不在XgSim内部临时清空列表，不需要记录专用codec。

## 格式与事务

- 原TrySave与v1头保持2 MiB契约。TrySaveSnapshot小快照同v1；大快照使用LINGGUANG-CHAPTER1:2，旧版本读取未来头会保护。
- 每个AtomicSaveStore.Folder独立包含snapshot-archive/objects。128 KiB块，以SHA256小写64hex命名；每manifest最多64引用，最多32级/2048块。主描述符含根hash、UTF-8总长、块数和整体hash，固定有界（实测247字节主档）。
- 先临时写块、Flush(true)、校验、rename为不可变对象；再写manifest，最后走原main/tmp/bak原子替换。引用未提交时故障可留下孤块；不会替换旧主档。已有块先校验再复用，坏块拒绝覆盖。
- 读取严格校验版本、hash格式、计数、路径、精确块长、总长、链深度/循环及整体hash；只允许hash拼接对象路径。缺块/损坏尝试主档备份，保留故障原因；两代均坏保护自动覆盖。未来版本直接保护，不回退。
- UTF-8展开上限256 MiB，写入在输出前拒绝超限；不删记录以挤进容量。不等于无限容量或低内存加载。

## 运行时路径与移动

ChapterOneRuntime.SaveNow、SaveSlot、主档加载/章节捕获写入调用TrySaveSnapshot。每slot写入自己的块集合；加载slot先把完整payload材料化进主目录，不依赖原slot后续存在。restart/reset时间戳文件保留在同目录，可引用现有对象。首版不GC，因此历史引用不会被主动回收。

TryExportSnapshot验证源并在新目录写入自包含快照；目标已有主档/备份则拒绝。TryImportSnapshot先验证源并写本地块后原子提交，保存目标之前主档为备份。可选payloadValidator在导入/导出时核实schema。Runtime ExportSaveSnapshot先SaveNow；ImportSaveSnapshot先ValidArchive及实例构造后提交/Attach。玩家profile不在导出范围。没有新增UI入口。手动搬移必须搬整个存档目录，单独复制v2的.sav不足以还原。

## 实际验证

- 75项StorageChecks：v1迁移、旧TrySave、不可变块复用、五个注入故障点、真实路径失败、缺块/坏块/备份保护、独立slot、restart/reset、导入导出自包含、hash/计数/遍历/链深度/版本拒绝、9 MiB多manifest。
- 12项生产原SaveStoreTests直接调用全部通过；另2项超256 MiB真实UTF-8输入检查通过，无新目录/块/主文件，已有主档字节不变。
- 实际5000记录+5000题图快照，5,442,369字节完整payload往返保留记录/最旧页/时间，主文件247字节。
- 核心787断言。所有以上是独立.NET执行；没有Unity JsonUtility或Play模式证据。LearningSnapshotEditorChecks.Run供root真实Runtime保存/槽/导入导出/重载验收，候选尚未编译。

## 开销与后续边界

每次自动保存仍全量序列化、编码、哈希并检查块。前部内容长度改变可能使后续块移位，每次最坏新增接近整个快照；没有GC会持续增加磁盘占用，不应宣传长期无限。查询只对已在内存的记录索引分页，加载全量展开，CSV仍全量内存预览。大档峰值内存远大于UTF-8容量，主线程耗时、autosave实际频率/性能、可达引用GC与流式CSV另行评估；本次没有擅自重写保存调度。磁盘不足或对象损坏报错并保留先前提交，不保证损坏介质自行恢复。
