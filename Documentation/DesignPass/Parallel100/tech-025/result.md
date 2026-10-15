# tech-025：label.parallel（并发推理）

日期：2026-10-13（Asia/Singapore）。范围仅本节点。已核对已有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`；未读取新策划正文，未修改生产源码、启动 Unity/游戏或访问玩家存档。

## 结论

确认需修正。`XgTechTreeCopy` 的专业名称「并发推理 / Concurrent inference」和通俗解释正确；效果中的「本地推理并发上限从 1 增至 4」混淆了服务总槽位、后台判题上限以及 QA 模式。当前正式游戏自启服务总槽位为 **3→4**，同时执行的后台判题为 **1→2 题**，不是四题，也不保证吞吐四倍。`XgTechTreePage` 本节点的 `×4` 字形同样误导。

交付 `changes.patch`，只改两个文件内 label.parallel 各一行：效果文案及字形 `×4`→`1→2`。未改名称、解释、模拟数值、购买条件或其他词条。补丁尚未应用。

## 可直接采用的中英文文案

| 项目 | 中文 | English |
| --- | --- | --- |
| 名称（保留） | 并发推理 | Concurrent inference |
| 通俗解释（保留） | 让本地模型同时处理几项判断请求。 | Let the local model handle several judgment requests at once. |
| 效果（替换） | 第四阶段起，拥有本地模型推理后可购买一次，不可叠加；无需装备或训练。由游戏启动的本地服务在空闲重启完成后，同时判题上限从 1 题增至 2 题；聊天或输入时暂停派发新判题。外部服务按已有容量运行，不自动扩容；实际速度取决于本机。 | From stage 4, buy once after Local model inference; no stacking, equipment or training required. After the game-started local service becomes idle and finishes restarting, simultaneous card judgments increase from 1 to 2. New judgments wait while you chat or type. External services keep their existing capacity; actual speed depends on the computer. |
| 节点字形 | 1→2 | 1→2 |

## 原文件证据和完整语义

下列路径均相对于工程根目录；行号以 `source-hashes.json` 的快照为准。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:462`：目录由 `XgSim.LabelNodes` 加入本节点；不能只根据旧目录说明认定实际效果。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Collaboration.cs:36, 148–192`：parent=`label.brain`，cost=3000，maxLevel=1；购买花费 3000 游戏货币并加入 `S.unlocked`，已拥有不再购买。效果是拥有标记开关，不是每级 +1、不与自动标注等级相乘、无需装备或再次训练。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:140–145`：未拥有的本节点第四阶段才可见；`XgTechTree.cs:127–130` 和 `Desktop/XgTechTreePage.cs:428` 沿用可见性过滤。前置脑节点本身要求第四阶段。没有改动隐藏规则，没有新增未来节点预览。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15, 51`：名称/说明的 `NodeMystery` 回退保留。`XgSim.cs:250` 的现有 mystery 特判仅针对 label.auto；不能声称 label.parallel 自身有独立「？？？」机制。
- `Assets/LingGuangV05/XingGuang/Desktop/XgBrain.cs:35–95`：购买后，在下一次允许供电、有算力、未暂停、非离线模拟且不处于研发的更新中，将拥有标记传给 `SetParallelJudging`。真实判题还要求本地模型就绪、拥有 label.brain、复核队列未满和有可用题目。pending 上限 1→3 是排队及在途请求数，不等于同时执行数。
- `Assets/LingGuangV05/LocalLLM/LocalLlm.cs:445–451, 497–505, 608–618`：拥有的服务等待在途工作清空后重启到目标槽位；失败/离线不保证生效。外部服务不自动重启或改变容量。`Boot` 的外部探测位于 166–175 行；正式自启路由位于 190–192 行。
- `Assets/LingGuangV05/Core/Chat/LlmScheduling.cs:25, 36–40, 51–73`：正式总槽位 3→4；固定角色槽位 0、1 不用于后台判题，后台可用槽位从 2 增至 3、2，因此实际判题上限 1→2。QA 默认槽位 1，QA 四槽不使用正式固定路由，其判题上限 3；不能拿 QA 的 1/4 配置作玩家效果。
- `Assets/LingGuangV05/LocalLLM/LocalLlm.cs:507–534`：可见聊天先派发；可见聊天排队/进行中或玩家输入时，不派发新的后台工作。已在途判题不是购买后立即全部取消。判题与其他后台工作共用容量，因此上限不等于持续满速。
- 外部服务边界：1/2 槽时判题上限仍为 1；3 槽固定路由时仍为 1；4 槽在购买后可到 2；更大服务还受 XgBrain pending≤3 限制。补丁不承诺所有外部服务购买后都获得 1→2。
- `XgSim.Collaboration.cs:229–244`：只对支持本地脑的文字题走真实推理；离线模拟/脑不可用回退检查点评分。此节点没有训练准确率、单题收益或训练速度倍率。单位是「同时判题数」，不是题/秒；无需等待一次训练或评估完成。

## 补丁基线

`changes.patch` 含目标路径、原行及相邻上下文。原文件 SHA256：

- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`
- `Assets/LingGuangV05/XingGuang/Desktop/XgTechTreePage.cs`：`F27CC730C4871776FC3E29FB95D765C099C02007889648334C42E3491CA8A523`

全部十个依赖文件哈希见 `source-hashes.json`。其他窗口可能继续更新，请整合时再次核对，只合入本节点两行，不覆盖整文件。旧 `XgSim.LabelNodes` 说明仍有 1→4，但科技页经 Copy 覆盖；本交付不修改共享目录原文，后续若其他页面直接显示该旧文案，由整合负责人单独处理。

## 验证分级与待整合

- 静态核对：上述目录→购买标记→适配器→本地服务→调度链已逐项核对。补丁 `git apply --check --ignore-space-change` 检查成功，未应用。
- 实际执行（仅纯策略）：运行 `verify-policy.ps1`，直接编译生产 `LlmScheduling.cs` 并调用，15 项断言通过；输出见 `verification.txt`。覆盖正式/QA 差异、外部 1/2 槽、两后台槽占满后不得借用空闲角色槽，以及聊天排队/进行中/输入各阻断条件。没有复制生产公式作为测试实现。
- 未执行：Unity 导入/整体编译、XgSim 购买集成、真实模型重启/判题、游戏场景及中英文视觉截图。纯策略结果不代表这些项目通过。
- 待整合：采用两行补丁，统一编译。保留 `Name/Note` 神秘节点回退及现有可见性过滤。
- 待主聊天验证：第四阶段前不显示；缺前置/资金不足不能买；正常购买后等级 1 且不能叠加；有在途任务时等待、重启后有题可供且无聊天时至多两题执行；聊天/输入阻止新判题；外部服务不被重启；断电、研发、离线、复核满时遵循既有回退/暂停；长文案和 `1→2` 在中英文小窗口无裁切。

本交付仅完成 tech-025 的核对和修正建议，不表示整份策划或全局文案原则已完成。结束前依赖复核结果见 `dependency-recheck.txt`。
