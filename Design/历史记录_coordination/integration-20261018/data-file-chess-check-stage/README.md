# 棋包检查失败诊断

结论：检查中的“metadata-only chess / expected0”是过时假设，不是当前样本凭空生成。当前XgCatalog.cs:179定义真实chess数据集：5700样本、1MB；此前阶段文档里“棋包没有dataset”的假设不适用于当前生产，应撤回。

真实路径：
1. pack.chess_mate1、pack.chess_mate2的dataset均为chess。
2. QueueTeachingData读取真实XgDataset，两个文件分别有nodeId/fileId，各携整包批次权益。
3. Arrive仅增加已下载批次并封顶，未装不加可用样本。
4. InstallDataFile增加installedDataBatches但上限为BatchesArrived；第一包达到完整5批，第二包仍5批。
5. PackSamples=BatchSamples(1140)*InstalledBatches(5)=5700；没有双倍11400。

窄补丁只改TwoTeachingNodesSameDataset检查，不修改生产结算或目录数值。断言增强为购买/下载两个阶段不增样本→第一次装精确加目录定义的整包→第二次装只开另一桌但样本不增→重复拖放不增→重载仍精确一个整包且保留两个节点。没有把断言改成宽松非负或仅检查不崩。

git apply --check --ignore-space-change通过；未编译、未运行Unity。新的实际检查仍需主线程统一运行。
