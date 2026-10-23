> 最新规则已覆盖本文早期临时回调/待确认描述：请先读 APPROVED-ENTRY.md。正式train.data节点（curve之后¥50）已在候选落地；DataInstallAccess临时回调已删除；核心门槛直接读持久科技所有权。以下保留前阶段审计过程，非当前接线合同。

# 核心候选/检查代码静态复核

日期：2026-10-18。范围：只读生产定义，对暂存候选校核；没有运行任何编译器或Unity。

## 符号与C#兼容
- XgSim.S为public get/private set；检查仅修改公开XgState字段，不赋值S。
- IXgHost六个业务成员与Wallet完整匹配：Compute/VramMB/Money/Blocker、Spend/Earn/Train。
- XgNode.needs为string[]，测试补空数组保护后使用System.Linq.Concat；生产非null初始化保持。
- XgState带Serializable，partial新增公开List/string/int字段可由JsonUtility序列化；XgDownload带Serializable，新增公开fileId/nodeId/installed兼容。未把Dictionary/HashSet/delegate放入存档。
- DataInstallAccess是XgSim运行时回调，不在XgState；测试明确断言JSON重载未自动带回授权，再仅在fixture绑定true。
- 反射只调用现有private TickDownloads(double)、ApplyLifeChoice(string,string,IXgHost)；参数分别double和实现IXgHost的Wallet，无重载歧义。Invoke返回object用于后者、前者忽略。
- 所有测试引用的stage/stageVision/stageSequence/firstSpecialty/openingFocus/lifeArgKey/batchesBought等为公开字段；NodeCost、Samples、DataNoise、DatasetAvailable、BuyNode、BuyDataOffer等签名存在。
- 新API均由同一候选补丁提供，检查文件位于暂存目录且UNITY_EDITOR包裹，使用UnityEditor.MenuItem、UnityEngine.JsonUtility；需与核心同时进入Editor检查目录，不能提前单独导入。
- 无文件系统、PlayerPrefs、场景或真实存档访问。JSON只处理内存字符串。

## 本轮发现并修复
1. 原简化免费门槛Has(dataset.pack)&&!owned，在老免费授权随后买同dataset教学包时会因owned新增而撤销旧资格。新增retainedFreeData与FreeDataAccess；迁移保存既有免费授权，QueueTeachingData在原免费资格变成owned之前保存。新购买的Dataset节点排除该保留逻辑，不能伪装免费授权。
2. 安装API新增统一默认拒绝gate；UI只能读取CanInstallData/DataInstallBlocker，不自行赋DataInstallAccess=true。正式门槛/价格尚未定，回调由正式运行时统一提供。
3. 检查补LegacyFreeGrantStaysAvailable和无回调/false时单包批量拒绝断言，保证不消费ready文件。
4. 为避免默认JsonUtility重载清掉运行时gate导致测试把权限问题当安装失败，Reload helper明确重新绑定测试权限并写明不证明游戏解锁路径。

## 旧免费授权判定
- 无owned的旧免费Has(dataset.pack)：仍可用，不给样本。
- 已owned且已有下载样本：按迁移后的installed量保持。
- 新购且待下载/待装：owned=true，不满足原免费回退；installed=0，不获得训练数据/新桌。
- 原免费资格再购教学包：retainedFreeData保留原数据集可访问性；原labels/训练记录继续；新增付费整包样本仍等待安装。
- 检查保留原desksOpen，不撤历史已开内容；19玩法和免费题桌不替换。

## 验证边界
`git apply --check --ignore-space-change`通过仅证明当前补丁可应用；符号阅读校核不等于C#编译通过，检查代码未运行。待用户入口规则、资料UI及正式gate绑定齐后，主线程串行编译/运行。核心候选不能单独发布，否则新购买包没有可用安装窗口。
