# C3 真实学习记录与完整快照归档候选：待 root Unity 整合验收

基准：main `bf354fdbce3745a81056fec6d17452ca91b15f21` + 当前事件core/host工作区。仅coordination候选；未写生产、未启动/编译Unity、未提交git。已实现 root 选择的方案 B：持久化边界对完整快照分块，主档只保存有界描述符。纯 .NET 存储检查已通过；详见 STORAGE.md。256 MiB 是完整 UTF-8 快照的安全上限，不是无限历史，尚未完成 Unity/真实 UI 验收。

## 模型与接口

- XgAskedEntry增加稳定long id、originalId、gameDate、gameSecondsOfDay、sourceKind、eventKey、questionEn、correction、teachingNoteId。保留原bankId/question/playerAnswer/aiAnswer/at字段兼容旧JSON；旧at是独立耗时，绝不拿它推算日期。
- `RepairLearningRecords`为旧记录修复唯一seq，先取现有最大值，新seq从max+1。旧缺日期保持0/-1并显示“未记录 / Not recorded”。同刻seq倒序，已知时间按游戏日期/时间倒序。
- `LearningClock: Func<XgLearningTime>`由共享Controller hunk从GameCalendar.Now(当前house.S)提供；没有回调或非法日期/秒即未知，不使用DateTime.Now/UtcNow。回调按每次BindSave的新sim绑定。
- `QueryLearningRecords(XgLearningQuery)` 支持sourceKind/sourceId/text、retestableOnly/textOnly、offset/limit。limit是显示窗口，1–200，默认50；total/HasMore供分页。首次索引一次排序、后续二分插入；分页只复制窗口，不每次整库排序/复制。搜索仍需线性扫描索引，当前仍是全量内存索引；分块保存不提供磁盘懒加载或磁盘分页。
- sourceKind目前为question/train/chat/book/retest/correction，sourceId沿用bankId；每条题图记录关联原teachingNoteId。`QueryTeachingNotes`单独保留所有旧快照、不对旧askedLog凭文本猜配对；新记录精确关联。旧图片若缺日志关联，日期仍未知。
- `RecordBookCompletion(bookId,title,titleEn,completionKey) -> long`：C4只在真实完成事务后调用，key如book.complete:<稳定id>。同非空key重放/读档后返回原seq，不重复新增，无奖励无通知；空id/key返回0。
- `RecordLearningRetest(originalId,deliveredAnswer,deliveryKey)`与`RecordLearningCorrection(originalId,explicitCorrection,deliveryKey)`只写实际交付结果、幂等、保留原答案，不给任何奖励。旧记录无真实AI答案时保持空，不回算/填标准答案。
- `LearningRetestInput`仅给原问题/英文/记录ID/原图ID/是否需要图，明确不包含truth、playerAnswer、aiAnswer、correction或why。实际图片识别调用者只能渲染输入图，不能把标签随请求交给识别器。

## UI与桥接候选

- YYChatView.Learning：题本全局最新优先、游戏时间、50条分页与总数、搜索不永久截断。随机题从全过滤结果挑选，排除需要图片的文字路径。保留性格题包权限/价格。
- XgFilesWindow.Records：CSV和预览显示游戏时间/未知，题图与笔记最新优先、50条分页，现有图片门槛和图片渲染引用不变。CSV文本仍为全量内存预览；大档导出峰值内存仍需后续处理，当前不能声称流式导出。
- XgYyTalk.Learning：已交付message.learningReplyId防重复回调，复考实际回复入历史；保留yyReplies最近50条交互缓存与每标签5条LLM短上下文，另留chat耐久记录。旧原图题不再伪装成纯文字复考；UI可查看原图引用，真正图片拖入YY/识别路径仍由root接线，候选会拒绝缺原图的文字发送。
- XgSim.DataPacks：保留所有原始快照，修复过时note计数器不重号，数组/笔迹位置继续复制。新增可选correction参数只接受调用者提供的实际纠正，不从truth伪造。

共享hunk单列在 `shared/` 与 `shared.patch`，不夹进主patch：
1. XingGuangController.BindSave：游戏时钟回调（基线包含事件工作区）。
2. XgLabelPage：提交前捕获真实shownGuess或有效binaryReview答案，传入RecordTeachingQuestion，不用truth当AI答案；不改答题结算/奖励。

## 执行证据

- `dotnet run --project Checks.csproj`：787项通过，真实XgSim核心，System.Text.Json字段往返。包括旧seq/未知时间/2016游戏日期/同刻顺序、跨页不重复和全部可达、旧200/256边界、短缓存保持、原图快照、重载幂等、复考不发钱、无答案标签的输入DTO、分页复用索引。结果core-result.json。不是Unity JsonUtility、Play或图像识别运行证明。
- 生产XgSim.DayStats自比较、两处未使用事件的既有编译警告保留，未顺手改动。
- capacity-result.json给出5000条示例的UTF8与压缩测量；2500条已超过2MB。STORAGE-OPTIONS.md比较单档压缩与分块归档的迁移/故障/查询/上限。
- 原XgTeachingSnapshotTests的256上限断言更新为所有300条都保留；其Unity测试候选未运行。

## 未完成与整合门槛

1. 完整快照分块、主档/slot/备份/故障恢复与自包含导入导出已给候选，75项存储检查、12项原测试及2项超容量检查通过。root 串行编译并执行 LearningSnapshotEditorChecks.Run，核实真实 Runtime/JsonUtility 路径后再判整合完成。
2. Unity编译、JsonUtility旧档回归、双语真实字体/翻页鼠标、图片YY复考和label不得传识别器，由root实际验收。
3. 付费YY数据回顾既有节点/价格/权限不改；查询API可给未来独立窗口消费，C3没有新增收费或开放门槛。
4. C4真实读完调用及旧书事实迁移由C4负责，本包没有修改任何Books/BookPuzzles文件，不凭拥有状态补完成历史。
5. 未接线的纠正来源、额外训练事件、未来图片识别来源需调用明确记录接口，不声称尚未触发的资料已全量覆盖。

prepare.py / prepare_bridges.py / prepare_storage.py 是一次性构建脚本，后续候选已手工细化，**不要重新运行覆盖候选**。finalize.py仅检查生产基线、生成patch与hash，可重复运行。

## 存储执行证据与限制

- `dotnet run --project StorageChecks.csproj`：75 项真实文件系统检查通过，`storage-result.json`。
- `dotnet run --project LegacyStorageChecks.csproj`：直接编译、调用生产原始 SaveStoreTests.cs 的全部12项测试（源码未改）；另外2项真实268,435,458字节UTF-8输入拒绝检查通过，`legacy-storage-result.json`。输入超过上限时先计算UTF-8长度，在分配编码副本和创建目录前拒绝；无块/manifest/main输出、旧主档保持不变。原测试临时证据保留，未调用删除目录的Cleanup。
- `record-storage-result.json`：5000条实际学习记录与5000快照，完整最小house payload 5,442,369字节经真实存储往返全部恢复；主文件247字节。
- Runtime主保存、slot、章节快照写入改用 TrySaveSnapshot；旧 TrySave 保留2 MiB协议。导入导出重建本地完整引用，详见STORAGE.md。
- 首版无GC：每次autosave仍完整序列化、UTF-8编码、哈希和检查全部块；相同块可复用，但前部字段长度变化可能使后续块整体移位，单次最坏新增约完整快照大小。1分钟一次、10 MiB快照且每次块全变化的示例约600 MiB/小时（仅说明量级，不是当前autosave频率测量），备份/手动槽/导出也占空间。
- 读档一次性展开全量字符串，内存同时可能持有UTF-8缓冲、MemoryStream/复制数组、UTF-16文本及反序列化对象，明显大于文件容量；256 MiB上限不表示目标设备内存性能已验收。保存调度、大档序列化耗时和引用GC由root后续评估，本包不扩大重写。
- 已存在同名块校验失败时拒绝覆盖该不可变对象，是保护行为；不会忽略坏块或伪造缺失历史。加载尝试完整备份，两份均不可用则保护。保留原文件不代表损坏档自动修好。

## root实际Unity反馈同步

主代理报告实际JsonUtility核心787检查通过。真实Runtime大档首次失败发现Windows Mono路径长度差异：原hash.chunk.tmp-GUID32路径到260字符，.NET10测试没有复现。候选同步root修复：同objects目录使用write-GUID16.tmp短临时名（最终hash命名不变），75项.NET存储故障/恢复复验通过；root继续真实slot/export验证，本包不冒认最终Unity通过。manifest/patch在保留的原base上重新生成，只刷新coordination产物，未覆盖正在整合的生产源码。localization-addon.patch独立后应用，8项本地化检查通过。
