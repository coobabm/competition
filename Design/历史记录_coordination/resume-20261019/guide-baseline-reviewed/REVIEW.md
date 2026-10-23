# Guide基线失败：测试夹具遗漏已安装数据

结论：`TheDataStepNamesTheSmsPackThenTheLabelsThatCountInFull` 的已知失败来自测试构造不完整；本次证据不支持修改产品引导。候选仅修改一个测试方法，不改运行时代码、难度、样本门槛或数据导入规则。

## 原因

- `XgSim.DataFiles.cs:27`：当前dataFilesVersion的`InstalledBatches`读取`installedDataBatches`，并受已到货批次数量约束。
- `XgSim.DataSources.cs:312`：`PackSamples`只把已安装批次计入样本。下载/拥有不等于安装。
- `XgSim.Abilities.cs:127`：`TrainedSamples`从样本扣噪声、乘数据权重。
- `XgGuide.cs:492`：只有有效样本不足时进入数据建议。旧测试直接添加spam.pack解锁和owned，却没有installedDataBatches，因而实际尚未达到6000有效样本。其注释声称“full bar”与夹具真实值矛盾。

候选保留购买建议检查，先断言仅拥有时InstalledBatches=0、有效量仍不足，再补齐该“整包已导入”夹具的已安装批次，并显式断言真实有效量达到门槛；最后继续原来无label、加4000噪声后有label、全权重桌子、中英/隐藏门槛等所有断言。没有把产品dataFilesVersion倒退为旧档，也没有关闭功能让测试通过。

这是引导目标引擎单元测试，允许直接构造已安装状态；不是声称实际拖入/安装UI通过。真实安装流程另有现有套件，本次未执行。

## 隔离复现与编译

在独立`verification/Baseline`与`verification/Candidate` .NET项目编译并调用同一真实NUnit测试方法，引用当前导入的两份Core程序集及NUnit；两次实际输出目录的引用DLL SHA256完全相同，见`verification/assembly-hashes.json`。

- 原生产测试拷贝：失败原样复现，Expected null / actual ability.label，原日志`verification/baseline-run.log`保留。
- 候选测试：编译和执行退出0，`TARGET_GUIDE_PASS`，见`verification/candidate-run.log`。
- 只执行这个定向方法，未宣称完整Guide套件或Unity EditMode通过；没有启动Unity/Player，没有写生产。

## 整合

基线测试文件SHA256：`268026dee14fc9fb0431162953b134536d07cf781071a3cf2fd355c4d32b33b8`。

补丁：`patch/guide-installed-data-fixture.patch`，SHA256：`4a92582c385a8bd202bc75ef68bd2b309a7b81fe2022ad89b03e4b1d7dfe22ea`。

更正：生产文件实际为LF（589个LF、0个CRLF），原补丁被Python默认Windows文本写入转换为CRLF，才导致普通apply-check失败。现已用字节写入生成LF补丁，普通`git apply --check`通过；不再需要ignore-space-change。原有CRLF补丁保存在verification/original-crlf.patch.txt，保留失败原因。生产源字节未改。
