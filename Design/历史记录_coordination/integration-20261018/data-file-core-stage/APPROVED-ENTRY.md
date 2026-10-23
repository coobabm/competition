> 最新命名确认：中文“数据导入”，英文“Data Import”；内部ID仍为train.data。购买后才出现AI资料文件夹，与安装/网址共用这一权限和一次价格，不增加费用。

# 最新批准规则：数据导入（覆盖此前临时回调方案）

用户已选择A：训练曲线之后购买¥50数据导入。本目录仍仅为候选，生产未改、未编译。

- 新科技节点：`train.data`，前置`train.curve`，基础价格50，stage1研究分支。保留现有合法道具/诊断折扣，按LedgerNode的普通价格规则，不乘StoryResearchPrice=.25。
- `CanInstallData => Has(DataEntryNode)`；`CanDownloadDataUrl`同门槛；`TrainPart("data")`也使用同门槛，即使旧teachingFlow=false也不无条件放开新增入口。
- `DataInstallBlocker()`返回正式双语科技购买提示。InstallDataFile与InstallDataFiles仍在核心最终验证，UI只能读状态/显示原因。
- 已删除临时可赋值的DataInstallAccess回调；正式授权来自持久科技所有权，UI不能把门槛设为true。
- 旧已安装档保留原样本、已开题桌、训练记录、旧免费数据资格和所有使用权，不回退待下载，也不重复收费恢复历史内容。不会因此给所有旧档免费新增train.data；以后新文件安装/网址入口仍购买该新节点。
- 资料文件夹与安装/网址统一受train.data控制：购买后才显示AI资料文件夹，不另收费。该显示接线由资料UI候选实施，本核心仅提供统一所有权门槛。
- 新小入口都科技购买的总规则不在本批随意创造其它节点，避免扩大范围。

## 候选文件
`data-file-core.patch`现含9个文件：DataFiles（新）、XgSim、DataPacks、Downloads、DataSources、LifeEvents、Progression、XgTrainingCatalog、XgTechTreeCopy。以最新生产重生最小补丁，原Profiler标记完整保留，未整文件覆盖。

## 检查更新
DataFileCoreChecks新增PaidEntryRule：
1. 新档无节点，安装/网址/TrainPart("data")均锁。
2. 无curve买入口失败、不扣费。
3. 完成12题的fixture通过真实BuyNode购买curve后，可买入口。
4. 普通价50与合法折扣公式一致；49元购买失败；实扣50一次；重复买不扣。
5. 重载保留已买权限；新未买档重载不赠权限。
6. 核心购买/下载/安装用真实BuyNode买train.data，不再fixture授权回调；下载ready但未买入口时，单包/批量安装都拒绝且不消费文件。
7. 旧已装档无新节点仍保留样本/题桌使用；旧免费资格后买教学包的保留检查仍执行。

真实核心检查代码已暂存；尚未编译/执行，不能记PASS。补丁适用性检查已通过。
